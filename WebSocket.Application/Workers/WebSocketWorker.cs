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

    /// <summary>
    /// WebSocketWorker - IIS Compatible Version (Performance Optimized)
    /// ✅ Funciona no IIS / Azure App Service / Cloud
    /// ✅ Um único endpoint WebSocket com multiplexação lógica
    /// ✅ Mantém 100% da lógica de invite/instance/authentication
    /// ✅ Instâncias são conceitos lógicos, não portas físicas
    /// ✅ ArrayPool para reduzir alocações
    /// ✅ Mapa global de clientes para lookup O(1)
    /// ✅ Serialização JSON otimizada
    /// </summary>
    public class WebSocketWorker : BackgroundService
    {
        private readonly ILogger<WebSocketWorker> _logger;
        private readonly ConcurrentDictionary<string, WebSocketInstance> _instances;
        private readonly ConcurrentDictionary<string, ConnectionInvite> _pendingInvites;
        private readonly ConcurrentDictionary<Guid, (WebSocketClient Client, WebSocketInstance Instance)> _clientMap;
        private readonly int _maxConnectionsPerInstance;
        private readonly int _inviteExpirationMinutes;
        private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        public WebSocketWorker(
            ILogger<WebSocketWorker> logger,
            int maxConnectionsPerInstance = 100,
            int inviteExpirationMinutes = 5
        )
        {
            _logger = logger;
            _instances = new ConcurrentDictionary<string, WebSocketInstance>();
            _pendingInvites = new ConcurrentDictionary<string, ConnectionInvite>();
            _clientMap = new ConcurrentDictionary<Guid, (WebSocketClient, WebSocketInstance)>();
            _maxConnectionsPerInstance = maxConnectionsPerInstance;
            _inviteExpirationMinutes = inviteExpirationMinutes;

            _logger.LogInformation(
                "WebSocketWorker constructed (IIS Compatible + Optimized): maxConnectionsPerInstance={MaxConnections}, inviteExpirationMinutes={InviteMinutes}",
                _maxConnectionsPerInstance, _inviteExpirationMinutes
            );
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Cria a primeira instância lógica
            CreateNewInstance();

            // Background task para limpar convites expirados E instâncias vazias
            _ = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    CleanExpiredInvites();
                    CleanIdleInstances();
                }
            }, stoppingToken);

            _logger.LogInformation("WebSocket Orchestrator iniciado (IIS Compatible)");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        /// <summary>
        /// Método principal para aceitar conexão WebSocket do ASP.NET Core.
        /// Este método deve ser chamado do seu middleware/controller.
        /// </summary>
        public async Task AcceptWebSocketAsync(
            HttpContext httpContext,
            WebSocket webSocket,
            CancellationToken cancellationToken = default
        )
        {
            var client = new WebSocketClient
            {
                Id = Guid.NewGuid(),
                Socket = webSocket,
                Headers = httpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                Cookies = httpContext.Request.Cookies.ToDictionary(c => c.Key, c => c.Value)
            };

            _logger.LogInformation(
                "Accepted WebSocket connection. ClientId={ClientId}, RemoteIp={RemoteIp}",
                client.Id,
                httpContext.Connection.RemoteIpAddress
            );

            await HandleClientAsync(client, cancellationToken);
        }

        protected virtual ValidateInviteTokenResult ValidateInviteToken(string token)
        {
            // ✅ Apenas valida, SEM criar invite novo
            if (!_pendingInvites.TryGetValue(token, out var invite))
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

        protected virtual WebSocketAuthResponse Authentication(Dictionary<string, string> headers, Dictionary<string, string> cookies)
        {
            var token = "";

            if (headers.ContainsKey("x-token-invite"))
            {
                token = headers["x-token-invite"];
                _logger.LogDebug("Authentication: found token in x-token-invite header");
            }

            if (cookies.ContainsKey("x-token-invite"))
            {
                token = cookies["x-token-invite"];
                _logger.LogDebug("Authentication: found token in x-token-invite cookie");
            }

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning(
                    "Authentication: failed to find token in headers or cookies. HeadersContainsToken={HasToken}, CookiesContainToken={HasTokenCookie}",
                    headers.ContainsKey("x-token-invite"),
                    cookies.ContainsKey("x-token-invite")
                );

                return new WebSocketAuthResponse
                {
                    Success = false,
                    Message = "Authentication error: No token found",
                    Token = token
                };
            }

            _logger.LogInformation(
                "Authentication: successful with token from {Source}",
                headers.ContainsKey("x-token-invite") ? "x-token-invite header" : "x-token-invite cookie"
            );

            return new WebSocketAuthResponse
            {
                Success = true,
                Message = "Authentication successful",
                Token = token
            };
        }

        public virtual async Task SendAsync(Guid clientId, WebSocketRequest payload)
        {
            // ✅ O(1) lookup com mapa global
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
                // ✅ ArrayBufferWriter cria seu próprio buffer interno (não precisa ArrayPool aqui)
                var bufferWriter = new ArrayBufferWriter<byte>();

                using (var writer = new Utf8JsonWriter(bufferWriter))
                {
                    JsonSerializer.Serialize(writer, payload);
                }

                var bytesWritten = bufferWriter.WrittenCount;

                // ✅ Envia direto do WrittenMemory sem ToArray()
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
            // ✅ Serializa uma vez só
            var bufferWriter = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(bufferWriter))
            {
                JsonSerializer.Serialize(writer, payload);
            }
            var sharedBuffer = bufferWriter.WrittenMemory;
            var bytesWritten = bufferWriter.WrittenCount;

            // ✅ Envia sequencial - WebSocket já é async, não precisa Task.Run
            // ⚠️ NOTA: Em escala extrema (10k+ clientes), considerar:
            //    - Channel<T> com workers dedicados
            //    - SemaphoreSlim(32) para paralelismo controlado
            //    - Fila por instância para evitar head-of-line blocking
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
                    Port = 0, // Não há mais portas dedicadas - instâncias são lógicas
                    CurrentConnections = i.Clients.Count,
                    i.MaxConnections,
                    i.IsActive,
                    i.CreatedAt
                })
            };
        }

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
            // ✅ Buffer do pool - reutilizável
            var buffer = _bufferPool.Rent(4 * 1024);

            try
            {
                // Autenticação
                var authResponse = Authentication(client.Headers, client.Cookies);

                if (!authResponse.Success)
                {
                    await client.Socket.CloseAsync(
                        WebSocketCloseStatus.PolicyViolation,
                        authResponse.Message,
                        cancellationToken
                    );
                    return;
                }

                // Validação do token de convite
                var validateInviteToken = ValidateInviteToken(authResponse.Token);

                if (!validateInviteToken.Valid)
                {
                    await client.Socket.CloseAsync(
                        WebSocketCloseStatus.PolicyViolation,
                        validateInviteToken.Message,
                        cancellationToken
                    );
                    return;
                }

                // Encontra a instância correta
                if (!_instances.TryGetValue(validateInviteToken.Invite.InstanceId, out var instance))
                {
                    await client.Socket.CloseAsync(
                        WebSocketCloseStatus.InternalServerError,
                        "Instance not found",
                        cancellationToken
                    );
                    return;
                }

                // Adiciona o cliente à instância E ao mapa global
                instance.Clients[client.Id] = client;
                _clientMap[client.Id] = (client, instance);

                // Marca o convite como usado
                if (_pendingInvites.TryGetValue(validateInviteToken.Invite.Token, out var invite))
                {
                    invite.IsUsed = true;
                }

                _logger.LogInformation(
                    "Client {ClientId} connected to instance {InstanceId}. CurrentConnections={Connections}",
                    client.Id,
                    instance.InstanceId,
                    instance.Clients.Count
                );

                // ✅ ArrayBufferWriter reutilizável ao invés de MemoryStream
                var messageBuffer = new ArrayBufferWriter<byte>();

                // Loop de recebimento de mensagens
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

                    // Hook para processar mensagens recebidas
                    if (result.MessageType == WebSocketMessageType.Text && totalBytesRead > 0)
                    {
                        // ✅ Decodifica direto do buffer sem ToArray()
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
                // ✅ Devolve buffer ao pool
                _bufferPool.Return(buffer);

                // ✅ Remove do mapa global E recupera a instância correta
                if (_clientMap.TryRemove(client.Id, out var tuple))
                {
                    var instance = tuple.Instance;
                    
                    // Remove da instância
                    instance.Clients.TryRemove(client.Id, out _);

                    _logger.LogInformation(
                        "Client {ClientId} disconnected from instance {InstanceId}. RemainingConnections={Connections}",
                        client.Id,
                        instance.InstanceId,
                        instance.Clients.Count
                    );
                }

                // Fecha a conexão se ainda estiver aberta
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

        /// <summary>
        /// Override este método para processar mensagens recebidas dos clientes
        /// </summary>
        protected virtual Task OnMessageReceived(WebSocketClient client, string message)
        {
            _logger.LogDebug("Message received from client {ClientId}: {Message}", client.Id, message);
            return Task.CompletedTask;
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

        private void CleanIdleInstances()
        {
            // Remove instâncias lógicas vazias e ociosas (exceto a primeira)
            var idleThreshold = TimeSpan.FromMinutes(10);
            var instancesToRemove = new List<string>();

            foreach (var instance in _instances.Values)
            {
                // Mantém pelo menos 1 instância sempre ativa
                if (_instances.Count <= 1)
                    break;

                // Se vazia e antiga
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
    }
}