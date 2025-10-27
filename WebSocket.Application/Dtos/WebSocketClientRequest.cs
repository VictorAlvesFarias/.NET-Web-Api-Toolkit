using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Packages.Ws.Application.Dtos
{
    public class WebSocketClientRequest
    {
        public string Event { get; set; }
        public JsonElement? Body { get; set; }
        public T Deserialize<T>() => Body.Value.Deserialize<T>();
    }
}
