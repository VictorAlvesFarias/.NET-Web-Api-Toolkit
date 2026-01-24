using System.Threading;

namespace Web.Api.Toolkit.Ws.Application.Contexts
{
    public class WebSocketRequestContextAccessor : IWebSocketRequestContextAccessor
    {
        private static readonly AsyncLocal<WebSocketRequestContext?> _context = new();

        public WebSocketRequestContext? Context
        {
            get => _context.Value;
            set => _context.Value = value;
        }
    }
}
