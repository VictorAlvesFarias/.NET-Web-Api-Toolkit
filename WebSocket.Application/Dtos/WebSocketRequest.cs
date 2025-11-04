using System.Text.Json;

namespace Web.Api.Toolkit.Ws.Application.Dtos
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

                var json = JsonSerializer.Serialize(value);
                using var doc = JsonDocument.Parse(json);
                _body = doc.RootElement.Clone();
            }
        }

        public T Serialize<T>()
        {
            if (_body == null)
                throw new InvalidOperationException("Body is null");

            return JsonSerializer.Deserialize<T>(_body.Value.GetRawText());
        }
    }
}