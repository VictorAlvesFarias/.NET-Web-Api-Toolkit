using System.Net.WebSockets;

namespace Packages.Ws.Application.Dtos
{
    public delegate Task WebSocketHandlerError(
        WebSocket ws,
        Exception req
    );
}