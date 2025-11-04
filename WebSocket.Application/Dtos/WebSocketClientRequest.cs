using System.Text.Json;

namespace Web.Api.Toolkit.Ws.Application.Dtos
{
    public class WebSocketClientRequest
    {
        public string Event { get; set; }
        public JsonElement? Body { get; set; }
        public T Deserialize<T>() => Body.Value.Deserialize<T>();
    }
}
