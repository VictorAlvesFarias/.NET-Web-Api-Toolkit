using System.Text.Json;

namespace Packages.Ws.Application.Dtos
{
    public class WebSocketRequest
    {
        public string Event { get; set; }

        private JsonElement? _body;
        public object Body
        {
            get => _body;
            set
            {
                if (value == null)
                {
                    _body = null;
                    return;
                }

                var json = System.Text.Json.JsonSerializer.Serialize(value);
                using var doc = JsonDocument.Parse(json);
                _body = doc.RootElement.Clone();
            }
        }

        public T Serialize<T>()
        {
            if (_body == null)
                throw new InvalidOperationException("Body is null");

            return System.Text.Json.JsonSerializer.Deserialize<T>(_body.Value.GetRawText());
        }
    }
}