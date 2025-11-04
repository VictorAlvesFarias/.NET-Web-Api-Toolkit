using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Web.Api.Toolkit.Ws.Application.Workers
{
    using global::Web.Api.Toolkit.Ws.Application.Delegates;
    using global::Web.Api.Toolkit.Ws.Application.Dtos;
    using System;

    public class WebSocketWorker : BackgroundService
    {
        private readonly ILogger<WebSocketWorker> _logger;
        private readonly HttpListener _listener;
        private readonly ConcurrentDictionary<string, List<WebSocketSubscription>> _subscriptions;
        private readonly ConcurrentDictionary<string, WebSocketInstance> _instances;
        private readonly ConcurrentDictionary<string, ConnectionInvite> _pendingInvites;
        private readonly int _basePort;
        private readonly int _maxConnectionsPerInstance;
        private readonly int _inviteExpirationMinutes;
        private readonly string _baseUrl;

        public WebSocketWorker(
            ILogger<WebSocketWorker> logger,
            string prefix = "http://localhost:8081/ws/",
            bool isOrchestrator = true,
            int maxConnectionsPerInstance = 1,
            int inviteExpirationMinutes = 5,
            string baseUrl = "ws://localhost"
        )
        {
            _logger = logger;
            _listener = new HttpListener();
            _subscriptions = new ConcurrentDictionary<string, List<WebSocketSubscription>>();
            _instances = new ConcurrentDictionary<string, WebSocketInstance>();
            _pendingInvites = new ConcurrentDictionary<string, ConnectionInvite>();
            _basePort = ExtractPortFromPrefix(prefix);
            _maxConnectionsPerInstance = maxConnectionsPerInstance;
            _inviteExpirationMinutes = inviteExpirationMinutes;
            _baseUrl = baseUrl;

            _listener.Prefixes.Add(prefix);
            _logger.LogInformation("WebSocketWorker constructed: prefix={Prefix}, basePort={BasePort}, baseUrl={BaseUrl}, maxConnectionsPerInstance={MaxConnections}, inviteExpirationMinutes={InviteMinutes}",
                prefix, _basePort, _baseUrl, _maxConnectionsPerInstance, _inviteExpirationMinutes);
        }

        #region BackgroundService Methods

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            CreateNewInstance();

            _ = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    CleanExpiredInvites();
                }
            }, stoppingToken);

            _logger.LogInformation("WebSocket Orchestrator iniciado");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        #endregion

        #region Service Methods

        public ConcurrentDictionary<Guid, WebSocketClient> GetClients()
        {
            var allClients = new ConcurrentDictionary<Guid, WebSocketClient>();

            foreach (var instance in _instances.Values.Where(i => i.IsActive))
            {
                foreach (var client in instance.Clients)
                {
                    allClients.TryAdd(client.Key, client.Value);
                }
            }

            return allClients;
        }

        public ConnectionInfo GetAvailableInstance(Guid clientId)
        {
            CleanExpiredInvites();

            var availableInstance = _instances.Values
                .Where(i => i.IsActive && i.Clients.Count < i.MaxConnections)
                .OrderBy(i => i.Clients.Count)
                .FirstOrDefault();

            if (availableInstance == null)
            {
                availableInstance = CreateNewInstance();
            }

            var token = Guid.NewGuid().ToString();
            var invite = new ConnectionInvite
            {
                Token = token,
                InstanceId = availableInstance.InstanceId,
                ClientId = clientId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_inviteExpirationMinutes),
                IsUsed = false
            };

            _pendingInvites[token] = invite;

            _logger.LogInformation("Invite generated for client {ClientId} on instance {InstanceId}. Token={Token}, ExpiresAt={ExpiresAt}",
                clientId, availableInstance.InstanceId, token, invite.ExpiresAt);

            return new ConnectionInfo
            {
                Url = availableInstance.Url,
                Token = token,
                ExpiresAt = invite.ExpiresAt
            };
        }

        public async Task Subscribe(string type, WebSocketHandler handler, WebSocketHandlerError? errorHandler = null)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentException("The type param is required.", nameof(type));
            }

            var list = _subscriptions.GetOrAdd(type, _ => new List<WebSocketSubscription>());

            lock (list)
            {
                list.Add(new WebSocketSubscription()
                {
                    Type = type,
                    Handler = handler,
                    HandlerError = errorHandler
                });
                _logger.LogInformation("Subscribed handler for event type '{EventType}'. HandlerCount={HandlerCount}", type, list.Count);
            }
        }

        public async Task SendAsync(Guid clientId, WebSocketRequest payload)
        {
            WebSocketClient client = null;
            WebSocketInstance instance = null;

            foreach (var inst in _instances.Values.Where(i => i.IsActive))
            {
                if (inst.Clients.TryGetValue(clientId, out var foundClient))
                {
                    client = foundClient;
                    instance = inst;

                    break;
                }
            }

            if (client == null)
            {
                _logger.LogWarning("SendAsync: client {ClientId} not found in any active instance.", clientId);
                return;
            }

            if (client.Socket.State != WebSocketState.Open)
            {
                _logger.LogWarning("SendAsync: client {ClientId} socket is not open. CurrentState={State}", clientId, client.Socket.State);
                return;
            }

            var json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            var seg = new ArraySegment<byte>(bytes);

            try
            {
                await client.Socket.SendAsync(seg, WebSocketMessageType.Text, true, CancellationToken.None);
                _logger.LogDebug("SendAsync: message sent to client {ClientId}. Event={Event}", clientId, payload?.Event);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SendAsync: error sending WebSocket message to client {ClientId}. Event={Event}", clientId, payload?.Event);
            }
        }

        public async Task BroadcastAsync(WebSocketRequest payload, Func<WebSocketClient, WebSocketRequest, bool>? q = null)
        {
            foreach (var instance in _instances.Values.Where(i => i.IsActive))
            {
                foreach (var client in instance.Clients.Values)
                {
                    if (q != null && !q(client, payload))
                        continue;

                    if (client.Socket.State != WebSocketState.Open)
                        continue;

                    try
                    {
                        var json = JsonSerializer.Serialize(payload);
                        var bytes = Encoding.UTF8.GetBytes(json);
                        var seg = new ArraySegment<byte>(bytes);
                        await client.Socket.SendAsync(seg, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "BroadcastAsync: error sending WebSocket message to client {ClientId}. Event={Event}", client.Id, payload?.Event);
                    }
                }
            }
        }

        #endregion

        #region Orquestration Methods

        private int ExtractPortFromPrefix(string prefix)
        {
            var uri = new Uri(prefix);

            return uri.Port;
        }

        private WebSocketInstance CreateNewInstance()
        {
            var instanceId = Guid.NewGuid().ToString();
            var port = _basePort + _instances.Count;
            var url = $"{_baseUrl}:{port}/ws/";

            _logger.LogInformation("Creating new WebSocket instance {InstanceId} on port {Port} with url {Url}", instanceId, port, url);

            var listener = new HttpListener();
            listener.Prefixes.Add($"http://+:{port}/ws/");

            var instance = new WebSocketInstance
            {
                InstanceId = instanceId,
                Url = url,
                Port = port,
                Listener = listener,
                Clients = new ConcurrentDictionary<Guid, WebSocketClient>(),
                MaxConnections = _maxConnectionsPerInstance,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _instances[instanceId] = instance;

            // Start instance in background
            _ = Task.Run(async () => await RunInstanceAsync(instance));

            _logger.LogInformation("Instance {InstanceId} created and run task queued. Port={Port}, Url={Url}", instanceId, port, url);

            return instance;
        }

        private async Task RunInstanceAsync(WebSocketInstance instance)
        {
            try
            {
                instance.Listener.Start();
                _logger.LogInformation("Instance {InstanceId} started and listening on port {Port}", instance.InstanceId, instance.Port);

                while (instance.IsActive)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await instance.Listener.GetContextAsync();
                    }
                    catch (HttpListenerException)
                    {
                        break;
                    }

                    if (!context.Request.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                        continue;
                    }

                    _ = Task.Run(async () =>
                    {
                        var wsContext = await context.AcceptWebSocketAsync(null);
                        var client = new WebSocketClient()
                        {
                            Headers = context.Request.Headers.AllKeys.ToDictionary(k => k, k => context.Request.Headers[k]),
                            Id = Guid.NewGuid(),
                            Socket = wsContext.WebSocket,
                            Cookies = context.Request.Cookies.Cast<Cookie>().ToDictionary(c => c.Name, c => c.Value)
                        };

                        _logger.LogInformation("Accepted WebSocket connection. Instance={InstanceId}, ClientId={ClientId}, RemoteEndPoint={Remote}", instance.InstanceId, client.Id, context.Request.RemoteEndPoint);
                        _logger.LogDebug("Connection details: HeaderCount={HeaderCount}, CookieCount={CookieCount}", client.Headers.Count, client.Cookies.Count);
                        _logger.LogInformation("Client {ClientId} added to instance {InstanceId}. CurrentConnections={Connections}", client.Id, instance.InstanceId, instance.Clients.Count);

                        await HandleClientAsync(client, instance);
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na instância {InstanceId}", instance.InstanceId);

                instance.IsActive = false;
            }
            finally
            {
                try
                {
                    instance.Listener.Stop();
                }
                catch { }
            }
        }

        private async Task HandleClientAsync(WebSocketClient handleClientAsyncParams, WebSocketInstance instance)
        {
            var buffer = new byte[4 * 1024];
            var authResponse = Authentication(handleClientAsyncParams.Socket, handleClientAsyncParams.Headers, handleClientAsyncParams.Cookies);

            if (!authResponse.Success)
            {
                await handleClientAsyncParams.Socket.CloseAsync(
                    WebSocketCloseStatus.PolicyViolation,
                    authResponse.Message,
                    CancellationToken.None
                );

                handleClientAsyncParams.Socket.Dispose();

                return;
            }

            var validateInviteToken = ValidateInviteToken(authResponse.Token);

            if (!validateInviteToken.Valid)
            {
                await handleClientAsyncParams.Socket.CloseAsync(
                    WebSocketCloseStatus.PolicyViolation,
                    validateInviteToken.Message,
                    CancellationToken.None
                );

                handleClientAsyncParams.Socket.Dispose();

                return;
            }

            instance.Clients[handleClientAsyncParams.Id] = handleClientAsyncParams;

            MarkInviteAsUsed(validateInviteToken.Invite.Token);

            while (handleClientAsyncParams.Socket.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await handleClientAsyncParams.Socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage && !result.CloseStatus.HasValue);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (instance != null)
                    {
                        instance.Clients.TryRemove(handleClientAsyncParams.Id, out _);
                    }

                    await handleClientAsyncParams.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);

                    break;
                }

                ms.Seek(0, SeekOrigin.Begin);

                var message = Encoding.UTF8.GetString(ms.ToArray());

                RedirectQueues(message, handleClientAsyncParams);
            }

            handleClientAsyncParams.Socket.Dispose();
        }

        #endregion

        #region Health Methods

        public WebSocketInstanceStatistics GetStatistics()
        {
            return new WebSocketInstanceStatistics
            {
                TotalInstances = _instances.Count,
                ActiveInstances = _instances.Values.Count(i => i.IsActive),
                TotalClients = _instances.SelectMany(e => e.Value.Clients).Count(),
                PendingInvites = _pendingInvites.Count,
                Instances = _instances.Values.Select(i => new
                {
                    i.InstanceId,
                    i.Port,
                    CurrentConnections = i.Clients.Count,
                    i.MaxConnections,
                    i.IsActive,
                    i.CreatedAt
                })
            };
        }

        #endregion

        #region Virtual Methods

        public virtual void RedirectQueues(string message, WebSocketClient client)
        {
            _logger.LogDebug("RedirectQueues: received message: {Message}", message);

            using var doc = JsonDocument.Parse(message);

            if (!doc.RootElement.TryGetProperty("event", out var typeProp))
            {
                _logger.LogWarning("RedirectQueues: message does not contain 'event' property");

                return;
            }

            var type = typeProp.GetString();

            if (string.IsNullOrWhiteSpace(type))
            {
                _logger.LogWarning("RedirectQueues: event type is null or whitespace in message");

                return;
            }

            _logger.LogDebug("RedirectQueues: routing event '{EventType}' to handlers", type);

            if (_subscriptions.TryGetValue(type, out var handlers))
            {
                foreach (var h in handlers)
                {
                    try
                    {
                        var webSocketRequest = JsonSerializer.Deserialize<WebSocketRequest>(message);

                        _ = Task.Run(() => h.Handler(client.Socket, webSocketRequest));

                        _logger.LogDebug("RedirectQueues: handler queued for event '{EventType}'", type);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "RedirectQueues: handler for event '{EventType}' threw an exception", type);

                        if (h.HandlerError != null)
                        {
                            _ = Task.Run(() => h.HandlerError(client.Socket, ex));
                        }
                    }
                }
            }
        }

        public virtual ValidateInviteTokenResult ValidateInviteToken(string token)
        {
            var invitereq = GetAvailableInstance(Guid.Parse(token));

            if (!_pendingInvites.TryGetValue(invitereq.Token, out var invite))
            {
                _logger.LogWarning("ValidateInviteToken: token not found: {Token}", token);
                return new ValidateInviteTokenResult(false, "Token not found.", null);
            }

            if (invite.IsUsed)
            {
                _logger.LogWarning("ValidateInviteToken: token already used: {Token}", token);
                return new ValidateInviteTokenResult(false, $"Token already used: \"{token}\".", null);
            }

            if (DateTime.UtcNow > invite.ExpiresAt)
            {
                _pendingInvites.TryRemove(token, out _);
                _logger.LogWarning("ValidateInviteToken: token expired and removed: {Token}", token);

                return new ValidateInviteTokenResult(false, $"Token expired: \"{token}\".", null);
            }

            _logger.LogDebug("ValidateInviteToken: token valid: {Token}", token);
            return new ValidateInviteTokenResult(true, null, invite);
        }

        public virtual WebSocketAuthResponse Authentication(WebSocket ws, Dictionary<string, string> headers, Dictionary<string, string> cookies)
        {
            var token = "";

            if (headers.ContainsKey("id"))
            {
                token = headers["id"];
                _logger.LogDebug("Authentication: found token in Authorization header");
            }

            if (cookies.ContainsKey("id"))
            {
                token = cookies["id"];
                _logger.LogDebug("Authentication: found token in id cookie");
            }

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Authentication: failed to find token in headers or cookies. HeadersContainsAuthorization={HasAuth}, CookiesContainId={HasId}",
                    headers.ContainsKey("Authorization"),
                    cookies.ContainsKey("id"));

                return new WebSocketAuthResponse()
                {
                    Success = false,
                    Message = "Authentication error: No token found",
                    Token = token
                };
            }

            _logger.LogInformation("Authentication: successful with token from {Source}",

            headers.ContainsKey("Authorization") ? "Authorization header" : "id cookie");

            return new WebSocketAuthResponse()
            {
                Success = true,
                Message = "Authentication successful",
                Token = token
            };

        }

        #endregion

        #region Helpers

        private void MarkInviteAsUsed(string token)
        {
            if (_pendingInvites.TryGetValue(token, out var invite))
            {
                invite.IsUsed = true;
            }
        }


        private void CleanExpiredInvites()
        {
            var expiredTokens = _pendingInvites.Where(kv => DateTime.UtcNow > kv.Value.ExpiresAt).Select(kv => kv.Key).ToList();

            foreach (var token in expiredTokens)
            {
                _pendingInvites.TryRemove(token, out _);
            }

            if (expiredTokens.Any())
            {
                _logger.LogInformation("Removidos {Count} convites expirados", expiredTokens.Count);
            }
        }

        #endregion
    }
}
