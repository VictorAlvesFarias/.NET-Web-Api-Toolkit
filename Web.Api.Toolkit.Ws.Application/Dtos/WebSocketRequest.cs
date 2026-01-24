using System.Text.Json;

namespace Web.Api.Toolkit.Ws.Application.Dtos
{
    public class WebSocketRequest
    {
        private JsonElement? _body;

        public Dictionary<string, string>? Headers { get; set; }
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

        public WebSocketRequest()
        {
            Event = string.Empty;
            Headers = new Dictionary<string, string>();
        }

        public string SerializeBody()
        {
            return JsonSerializer.Serialize(this);
        }

        public T DeserializeBody<T>()
        {
            if (_body == null)
                return default;

            return JsonSerializer.Deserialize<T>(_body.Value.GetRawText());
        }
    }
}