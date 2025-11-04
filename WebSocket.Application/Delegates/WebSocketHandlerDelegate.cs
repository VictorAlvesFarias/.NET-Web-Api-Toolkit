using System.Net.WebSockets;
using Web.Api.Toolkit.Ws.Application.Dtos;

namespace Web.Api.Toolkit.Ws.Application.Delegates
{
    public delegate Task WebSocketHandler(
        WebSocket ws,
        WebSocketRequest req
    );
}