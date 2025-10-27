using System.Net.WebSockets;

namespace Ws.Application.Dtos
{
    public class WebSocketClient
    {
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, string> Cookies { get; set; }
        public WebSocket Socket { get; set; }
        public string InstanceId { get; set; }
        public Guid Id { get; set; }
    }
}