using System.Net.WebSockets;
using Ws.Application.Dtos;

namespace Ws.Application.Delegates
{
    public delegate Task WebSocketHandler(
        WebSocket ws,
        WebSocketRequest req
    );
}