using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;

namespace Web.Api.Toolkit.Ws.Application.Dtos
{
    public class WebSocketClient
    {
        public HttpContext HttpContext { get; set; }
        public Dictionary<string, string> Context { get; set; }
        public WebSocket Socket { get; set; }
        public Guid Id { get; set; }
    }
}