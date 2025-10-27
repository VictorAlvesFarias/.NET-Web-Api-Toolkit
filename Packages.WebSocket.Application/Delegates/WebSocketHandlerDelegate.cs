using System.Net.WebSockets;

namespace Packages.Ws.Application.Dtos
{
    public delegate Task WebSocketHandler(
        WebSocket ws,
        WebSocketRequest req
    );
}