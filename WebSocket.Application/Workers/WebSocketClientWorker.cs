using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Web.Api.Toolkit.Ws.Application.Attributes;
using Web.Api.Toolkit.Ws.Application.Channels;
using Web.Api.Toolkit.Ws.Application.Contexts;
using Web.Api.Toolkit.Ws.Application.Dtos;

namespace Web.Api.Toolkit.Ws.Application.Workers
{
    public class WebSocketClientWorker : BackgroundService
    {
        private readonly ILogger<WebSocketClientWorker> _logger;
        private ClientWebSocket _socket;
        private readonly ConcurrentDictionary<string, Func<WebSocketClientRequest, CancellationToken, Task>> _handlers;
        private readonly TimeSpan _reconnectDelay;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly JsonSerializerOptions _serializerOptions;
        private bool _channelsRegistered;

        public WebSocketClientWorker(
            ILogger<WebSocketClientWorker> logger,
            IServiceScopeFactory scopeFactory,
            TimeSpan? reconnectDelay = null)
        {
            _logger = logger;
            _handlers = new ConcurrentDictionary<string, Func<WebSocketClientRequest, CancellationToken, Task>>();
            _socket = new ClientWebSocket();
            _reconnectDelay = reconnectDelay ?? TimeSpan.FromSeconds(3);
            _scopeFactory = scopeFactory;
            _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };
        }

        protected virtual string GetUrl()
        {
            return "ws://localhost:5000/ws";
        }

        protected virtual Dictionary<string, string> GetHeaders()
        {
            return new();
        }

        protected virtual CookieContainer GetCookies()
        {
            return new();
        }

        protected virtual TimeSpan GetReconnectDelay()
        {
            return _reconnectDelay;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Registrar channels automaticamente na primeira execução
            if (!_channelsRegistered)
            {
                RegisterChannels();
                _channelsRegistered = true;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _socket = new ClientWebSocket();

                    var url = GetUrl();

                    foreach (var header in GetHeaders())
                    {
                        _socket.Options.SetRequestHeader(header.Key, header.Value);
                    }

                    _socket.Options.Cookies = GetCookies();

                    await _socket.ConnectAsync(new Uri(url), stoppingToken);

                    _logger.LogInformation("Cliente WS conectado: {0}", url);

                    await ReceiveLoop(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro na conexão WS, reconectando em {0}s...", GetReconnectDelay().TotalSeconds);

                    await Task.Delay(GetReconnectDelay(), stoppingToken);
                }
            }
        }

        public void Subscribe(string eventType, Func<WebSocketClientRequest, CancellationToken, Task> handler)
        {
            Subscribe(eventType, (_, request, token) => handler(request, token));
        }

        public void Subscribe(string eventType, Func<IServiceProvider, WebSocketClientRequest, CancellationToken, Task> handler)
        {
            if (string.IsNullOrWhiteSpace(eventType))
                throw new ArgumentException("The event name cannot be empty.", nameof(eventType));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlers[eventType] = async (request, token) =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                await handler(scope.ServiceProvider, request, token);
            };
        }

        public void RegisterChannel<TChannel>() where TChannel : WebSocketChannelBase
        {
            RegisterChannel(typeof(TChannel));
        }

        public void RegisterChannel(Type channelType)
        {
            if (channelType == null)
                throw new ArgumentNullException(nameof(channelType));

            if (!typeof(WebSocketChannelBase).IsAssignableFrom(channelType))
                throw new ArgumentException($"Channel type must inherit from {nameof(WebSocketChannelBase)}", nameof(channelType));

            var methods = channelType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(m => new
                {
                    Method = m,
                    Attribute = m.GetCustomAttribute<WsActionAttribute>()
                })
                .Where(x => x.Attribute != null)
                .ToList();

            if (!methods.Any())
                throw new InvalidOperationException($"No WsAction methods found in channel {channelType.FullName}.");

            foreach (var method in methods)
            {
                var descriptor = new WebSocketChannelActionDescriptor(
                    method.Attribute!.Event,
                    channelType,
                    method.Method);

                Subscribe(descriptor.EventName, async (services, request, token) =>
                {
                    await InvokeChannel(descriptor, services, request, token);
                });
            }
        }

        /// <summary>
        /// Registers all WebSocket channels associated with this worker type.
        /// Automatically discovers channels that inherit from WebSocketChannelBase&lt;TWorker&gt; where TWorker is this worker's type,
        /// or channels that inherit from WebSocketChannelBase (for backward compatibility).
        /// </summary>
        public void RegisterChannels()
        {
            var thisWorkerType = GetType();
            var baseChannelTypeForThisWorker = typeof(WebSocketChannelBase<>).MakeGenericType(thisWorkerType);

            var channelTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        return Array.Empty<Type>();
                    }
                })
                .Where(t =>
                {
                    if (t.IsAbstract || t.IsInterface || !t.IsClass)
                        return false;

                    // Register channels specifically associated with this worker type
                    if (baseChannelTypeForThisWorker.IsAssignableFrom(t))
                        return true;

                    // For backward compatibility: register channels without worker association
                    // only if they inherit directly from WebSocketChannelBase (not WebSocketChannelBase<>)
                    if (typeof(WebSocketChannelBase).IsAssignableFrom(t))
                    {
                        var baseType = t.BaseType;
                        while (baseType != null && baseType != typeof(object))
                        {
                            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(WebSocketChannelBase<>))
                            {
                                // This channel is associated with a different worker, skip it
                                return false;
                            }
                            baseType = baseType.BaseType;
                        }
                        // This is a channel without worker association (direct inheritance from WebSocketChannelBase)
                        return true;
                    }

                    return false;
                })
                .Distinct()
                .ToList();

            foreach (var channelType in channelTypes)
            {
                try
                {
                    RegisterChannel(channelType);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to register channel {ChannelType}", channelType.FullName);
                }
            }
        }

        /// <summary>
        /// Registers all WebSocket channels found in the specified assemblies.
        /// </summary>
        /// <param name="assemblies">The assemblies to scan for channels.</param>
        public void RegisterChannels(params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                RegisterChannels();
                return;
            }

            var channelTypes = assemblies
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        return Array.Empty<Type>();
                    }
                })
                .Where(t => typeof(WebSocketChannelBase).IsAssignableFrom(t) 
                    && !t.IsAbstract 
                    && !t.IsInterface
                    && t.IsClass)
                .Distinct()
                .ToList();

            foreach (var channelType in channelTypes)
            {
                RegisterChannel(channelType);
            }
        }

        public async Task SendAsync<T>(T payload, CancellationToken token = default)
        {
            if (_socket.State != WebSocketState.Open)
                return;

            var json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_socket != null && _socket.State == WebSocketState.Open)
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Service stopping.", cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro na conexão WS, reconectando em {0}s...", GetReconnectDelay().TotalSeconds);
            }

            _socket?.Dispose();

            await base.StopAsync(cancellationToken);
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[8 * 1024];

            while (_socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                WebSocketReceiveResult result;

                try
                {
                    result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro na conexão WS, reconectando em {0}s...", GetReconnectDelay().TotalSeconds);

                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server.", token);

                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                ProcessMessage(message, token);
            }

            throw new Exception("WebSocket connection is ended.");
        }

        private void ProcessMessage(string message, CancellationToken token)
        {
            try
            {
                var req = JsonSerializer.Deserialize<WebSocketClientRequest>(message, _serializerOptions);
                if (req == null || string.IsNullOrWhiteSpace(req.Event))
                    return;

                if (_handlers.TryGetValue(req.Event, out var handler))
                    _ = Task.Run(() => handler(req, token), token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error message proccesing.");
            }
        }
        private async Task InvokeChannel(
            WebSocketChannelActionDescriptor descriptor,
            IServiceProvider services,
            WebSocketClientRequest request,
            CancellationToken token)
        {
            var channel = (WebSocketChannelBase)services.GetRequiredService(descriptor.ChannelType);
            var context = new WebSocketRequestContext(request, services, token, this);
            channel.SetContext(context);

            var args = BuildArguments(descriptor, context);

            var result = descriptor.MethodInfo.Invoke(channel, args);
            if (result is Task task)
            {
                await task;
            }
        }

        private object?[] BuildArguments(WebSocketChannelActionDescriptor descriptor, WebSocketRequestContext context)
        {
            var parameters = descriptor.MethodInfo.GetParameters();
            if (parameters.Length == 0)
                return Array.Empty<object>();

            var nonContextParameters = parameters.Count(p => !typeof(WebSocketRequestContext).IsAssignableFrom(p.ParameterType));
            if (nonContextParameters > 1)
            {
                throw new InvalidOperationException($"Channel method '{descriptor.MethodInfo.Name}' on '{descriptor.ChannelType.Name}' can have at most one parameter besides {nameof(WebSocketRequestContext)}.");
            }

            var args = new object?[parameters.Length];

            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                if (typeof(WebSocketRequestContext).IsAssignableFrom(parameter.ParameterType))
                {
                    args[index] = context;
                    continue;
                }

                if (context.Request.Body is null)
                    throw new InvalidOperationException($"Unable to deserialize message body for channel '{descriptor.ChannelType.Name}' action '{descriptor.MethodInfo.Name}'.");

                args[index] = context.Request.Body.Value.Deserialize(parameter.ParameterType, _serializerOptions);
            }

            return args;
        }

        private sealed record WebSocketChannelActionDescriptor(string EventName, Type ChannelType, MethodInfo MethodInfo);
    }
}
