using System.Collections.Concurrent;
using System.Net;

namespace Web.Api.Toolkit.Ws.Application.Dtos
{
    /// <summary>
    /// Representa uma instância lógica de WebSocket.
    /// Não é mais vinculada a uma porta/HttpListener específico.
    /// </summary>
    public class WebSocketInstance
    {
        public string InstanceId { get; set; }
        public string Url { get; set; }
        public int Port { get; set; } // Deprecated: sempre 0 na versão IIS-compatible
        public HttpListener? Listener { get; set; } // Deprecated: sempre null na versão IIS-compatible
        public ConcurrentDictionary<Guid, WebSocketClient> Clients { get; set; }
        public int MaxConnections { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}