using Web.Api.Toolkit.Web.Api.Toolkit.Ws.Application.Delegates;

namespace Web.Api.Toolkit.Web.Api.Toolkit.Ws.Application.Dtos
{
    public class WebSocketSubscription
    {
        public string Type { get; set; }
        public WebSocketHandler Handler { get; set; }
        public WebSocketHandlerError? HandlerError { get; set; }
    }
}