using System.Text.Json;

namespace Web.Api.Toolkit.Ws.Application.Dtos
{
    public class WebSocketRequest
    {
        private JsonElement? _body;
        
        public string Event { get; set; }
        public object? Body
        {
            get => _body;
            set
            {
                if (value == null)
                {
                    _body = null;
                    return;
                }

                var json = JsonSerializer.Serialize(value);

                using var doc = JsonDocument.Parse(json);
               
                _body = doc.RootElement.Clone();
            }
        }

        public string SerializeBody()
        {
            var bodyText = _body?.GetRawText() ?? "null";

            return JsonSerializer.Serialize(new
            {
                Event,
                Body = _body.HasValue ? JsonSerializer.Deserialize<object>(bodyText) : null
            });
        }
    }
}