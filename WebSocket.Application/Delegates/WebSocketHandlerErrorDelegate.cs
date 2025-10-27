using System.Net.WebSockets;

namespace Ws.Application.Delegates
{
    public delegate Task WebSocketHandlerError(
        WebSocket ws,
        Exception req
    );
}