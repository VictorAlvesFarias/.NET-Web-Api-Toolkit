using System.Text.Json;

namespace Web.Api.Toolkit.Ws.Application.Dtos
{
    public class WebSocketRequest
    {
        public string Event { get; set; }
        public JsonElement? Body { get; set; }
    }
}