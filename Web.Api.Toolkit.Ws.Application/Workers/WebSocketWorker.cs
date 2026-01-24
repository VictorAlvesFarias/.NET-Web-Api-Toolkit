using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Web.Api.Toolkit.Ws.Application.Workers
{
    using global::Web.Api.Toolkit.Ws.Application.Dtos;
    using System;

    public class WebSocketWorker : BackgroundService
    {
        private readonly ILogger<WebSocketWorker> _logger;
        private readonly ConcurrentDictionary<string, WebSocketInstance> _instances;
        private readonly ConcurrentDictionary<Guid, (WebSocketClient Client, WebSocketInstance Instance)> _clientMap;
        private readonly int _maxConnectionsPerInstance;
        private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        public WebSocketWorker(
            ILogger<WebSocketWorker> logger,
            int maxConnectionsPerInstance = 100
        )
        {
            _logger = logger;
            _instances = new ConcurrentDictionary<string, WebSocketInstance>();
            _clientMap = new ConcurrentDictionary<Guid, (WebSocketClient, WebSocketInstance)>();
            _maxConnectionsPerInstance = maxConnectionsPerInstance;

            _logger.LogInformation(
                "WebSocketWorker constructed (IIS Compatible + Optimized): maxConnectionsPerInstance={MaxConnections}",
                _maxConnectionsPerInstance
            );
        }


        #region Utilitaries 

        public ConcurrentDictionary<Guid, WebSocketClient> GetClients()
        {
            // ✅ Usa mapa global direto - muito mais rápido
            var allClients = new ConcurrentDictionary<Guid, WebSocketClient>();

            foreach (var kvp in _clientMap)
            {
                if (kvp.Value.Instance.IsActive)
                {
                    allClients.TryAdd(kvp.Key, kvp.Value.Client);
                }
            }

            return allClients;
        }

        public WebSocketInstanceStatistics GetStatistics()
        {
            return new WebSocketInstanceStatistics
            {
                TotalInstances = _instances.Count,
                ActiveInstances = _instances.Values.Count(i => i.IsActive),
                TotalClients = _instances.SelectMany(e => e.Value.Clients).Count(),
                PendingInvites = 0,
                Instances = _instances.Values.Select(i => new
                {
                    i.InstanceId,
                    Port = 0,
                    CurrentConnections = i.Clients.Count,
                    i.MaxConnections,
                    i.IsActive,
                    i.CreatedAt
                })
            };
        }

        #endregion

        #region Background Service Methods

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.CreateNewInstance();

            _ = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    this.CleanIdleInstances();
                }
            }, stoppingToken);

            _logger.LogInformation("WebSocket Orchestrator iniciado (IIS Compatible)");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        #endregion

        #region Virtual Methods

        public virtual async Task AcceptWebSocketAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            var client = new WebSocketClient
            {
                Id = Guid.NewGuid(),
                Socket = null,
                HttpContext = httpContext
            };

            await HandleClientAsync(client, cancellationToken);
        }

        public virtual async Task SendAsync(Guid clientId, WebSocketRequest payload)
        {
            if (!_clientMap.TryGetValue(clientId, out var tuple))
            {
                _logger.LogWarning("SendAsync: client {ClientId} not found.", clientId);
                return;
            }

            var (client, instance) = tuple;

            if (client.Socket.State != WebSocketState.Open)
            {
                _logger.LogWarning("SendAsync: client {ClientId} socket is not open. CurrentState={State}", clientId, client.Socket.State);

                return;
            }

            try
            {
                var bufferWriter = new ArrayBufferWriter<byte>();

                using (var writer = new Utf8JsonWriter(bufferWriter))
                {
                    JsonSerializer.Serialize(writer, payload);
                }

                var bytesWritten = bufferWriter.WrittenCount;

                await client.Socket.SendAsync(
                    bufferWriter.WrittenMemory.Slice(0, bytesWritten),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );

                _logger.LogDebug("SendAsync: message sent to client {ClientId}. Event={Event}, Bytes={Bytes}", clientId, payload?.Event, bytesWritten);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SendAsync: error sending WebSocket message to client {ClientId}. Event={Event}", clientId, payload?.Event);
            }
        }

        public virtual async Task BroadcastAsync(WebSocketRequest payload, Func<WebSocketClient, WebSocketRequest, bool>? q = null)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();

            using (var writer = new Utf8JsonWriter(bufferWriter))
            {
                JsonSerializer.Serialize(writer, payload);
            }

            var sharedBuffer = bufferWriter.WrittenMemory;
            var bytesWritten = bufferWriter.WrittenCount;
            var clientCount = 0;
            var errors = 0;

            foreach (var (client, instance) in _clientMap.Values)
            {
                if (!instance.IsActive)
                    continue;

                if (q != null && !q(client, payload))
                    continue;

                if (client.Socket.State != WebSocketState.Open)
                    continue;

                clientCount++;

                try
                {
                    // ✅ Sem Task.Run - WebSocket.SendAsync já é async eficiente
                    // ✅ Usa Memory direto sem ToArray()
                    await client.Socket.SendAsync(
                        sharedBuffer.Slice(0, bytesWritten),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning(ex, "BroadcastAsync: error sending to client {ClientId}", client.Id);
                }
            }

            if (errors > 0)
            {
                _logger.LogWarning("BroadcastAsync: completed with {Errors}/{Total} errors. Event={Event}", errors, clientCount, payload?.Event);
            }
            else
            {
                _logger.LogDebug("BroadcastAsync: sent to {Count} clients. Event={Event}, Bytes={Bytes}", clientCount, payload?.Event, bytesWritten);
            }
        }

        protected virtual async Task<string> GetUrlAsync()
        {
            return "/ws";
        }

        protected virtual WebSocketAuthResponse Authentication(WebSocketClient client)
        {
            _logger.LogDebug("Authentication: client {ClientId} connecting", client.Id);

            return new WebSocketAuthResponse
            {
                Success = true,
                Message = "Authentication successful"
            };
        }

        protected virtual Task OnMessageReceived(WebSocketClient client, string message)
        {
            _logger.LogDebug("Message received from client {ClientId}: {Message}", client.Id, message);
            return Task.CompletedTask;
        }

        protected virtual Task OnClientConnectedAsync(WebSocketClient client)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnClientDisconnectedAsync(WebSocketClient client)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Helpers 

        private WebSocketInstance CreateNewInstance()
        {
            var instanceId = Guid.NewGuid().ToString();

            _logger.LogInformation("Creating new logical WebSocket instance {InstanceId}", instanceId);

            var instance = new WebSocketInstance
            {
                InstanceId = instanceId,
                Url = "/ws", // URL relativa - mesma porta do app
                Port = 0, // Não há mais porta dedicada
                Listener = null, // Não há mais HttpListener
                Clients = new ConcurrentDictionary<Guid, WebSocketClient>(),
                MaxConnections = _maxConnectionsPerInstance,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _instances[instanceId] = instance;

            _logger.LogInformation("Logical instance {InstanceId} created successfully", instanceId);

            return instance;
        }

        private async Task HandleClientAsync(WebSocketClient client, CancellationToken cancellationToken)
        {
            var buffer = _bufferPool.Rent(4 * 1024);

            try
            {
                var authResponse = Authentication(client);

                if (!authResponse.Success)
                {
                    _logger.LogWarning("Client {ClientId} authentication failed: {Message}", client.Id, authResponse.Message);
                    return;
                }

                // Seleciona automaticamente a instância com menos conexões
                var instance = _instances.Values
                    .Where(i => i.IsActive && i.Clients.Count < i.MaxConnections)
                    .OrderBy(i => i.Clients.Count)
                    .FirstOrDefault();

                // Se não houver instância disponível, cria uma nova
                if (instance == null)
                {
                    instance = CreateNewInstance();
                }

                _logger.LogInformation("Client {ClientId} assigned to instance {InstanceId}", client.Id, instance.InstanceId);

                client.Socket = await client.HttpContext.WebSockets.AcceptWebSocketAsync();
                instance.Clients[client.Id] = client;
                _clientMap[client.Id] = (client, instance);

                await OnClientConnectedAsync(client);

                var messageBuffer = new ArrayBufferWriter<byte>();

                while (client.Socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    messageBuffer.Clear();

                    WebSocketReceiveResult result;
                    int totalBytesRead = 0;

                    do
                    {
                        result = await client.Socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                        if (result.Count > 0)
                        {
                            messageBuffer.Write(buffer.AsSpan(0, result.Count));
                            totalBytesRead += result.Count;
                        }
                    }
                    while (!result.EndOfMessage && !result.CloseStatus.HasValue);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text && totalBytesRead > 0)
                    {

                        var message = Encoding.UTF8.GetString(messageBuffer.WrittenSpan);

                        await OnMessageReceived(client, message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Client {ClientId} connection cancelled", client.Id);
            }
            catch (WebSocketException ex)
            {
                _logger.LogWarning(ex, "WebSocket error for client {ClientId}", client.Id);
            }
            finally
            {
                _bufferPool.Return(buffer);

                if (_clientMap.TryRemove(client.Id, out var tuple))
                {
                    var instance = tuple.Instance;

                    instance.Clients.TryRemove(client.Id, out _);

                    _logger.LogInformation(
                        "Client {ClientId} disconnected from instance {InstanceId}. RemainingConnections={Connections}",
                        client.Id,
                        instance.InstanceId,
                        instance.Clients.Count
                    );

                    await OnClientDisconnectedAsync(client);
                }

                if (client.Socket is not null)
                {
                    if (client.Socket.State == WebSocketState.Open)
                    {
                        try
                        {
                            await client.Socket.CloseAsync(
                                WebSocketCloseStatus.NormalClosure,
                                "Closed by server",
                                CancellationToken.None
                            );
                        }
                        catch { }
                    }

                    client.Socket.Dispose();
                }
            }
        }

        private void CleanIdleInstances()
        {
            var idleThreshold = TimeSpan.FromMinutes(10);
            var instancesToRemove = new List<string>();

            foreach (var instance in _instances.Values)
            {
                if (_instances.Count <= 1)
                {
                    break;
                }

                if (instance.Clients.Count == 0 &&
                    DateTime.UtcNow - instance.CreatedAt > idleThreshold)
                {
                    instancesToRemove.Add(instance.InstanceId);
                }
            }

            foreach (var instanceId in instancesToRemove)
            {
                if (_instances.TryRemove(instanceId, out var removedInstance))
                {
                    removedInstance.IsActive = false;

                    _logger.LogInformation(
                        "Removed idle instance {InstanceId}. Age={Age} minutes",
                        instanceId,
                        (DateTime.UtcNow - removedInstance.CreatedAt).TotalMinutes
                    );
                }
            }
        }

        #endregion
    }
}