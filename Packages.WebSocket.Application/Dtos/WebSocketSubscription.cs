namespace Packages.Ws.Application.Dtos
{
    public class WebSocketSubscription
    {
        public string Type { get; set; }
        public WebSocketHandler Handler { get; set; }
        public WebSocketHandlerError? HandlerError { get; set; }
    }
}