using System.Collections.Concurrent;
using System.Net;

namespace Packages.Ws.Application.Dtos
{
    public class WebSocketInstance
    {
        public string InstanceId { get; set; }
        public string Url { get; set; }
        public int Port { get; set; }
        public HttpListener Listener { get; set; }
        public ConcurrentDictionary<Guid, WebSocketClient> Clients { get; set; }
        public int MaxConnections { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}