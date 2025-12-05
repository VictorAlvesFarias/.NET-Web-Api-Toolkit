namespace Web.Api.Toolkit.Ws.Application.Contexts
{
    public interface IWebSocketRequestContextAccessor
    {
        WebSocketRequestContext? Context { get; set; }
    }
}
