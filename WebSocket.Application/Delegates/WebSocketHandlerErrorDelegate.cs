using System.Net.WebSockets;

namespace Web.Api.Toolkit.Web.Api.Toolkit.Ws.Application.Delegates
{
    public delegate Task WebSocketHandlerError(
        WebSocket ws,
        Exception req
    );
}