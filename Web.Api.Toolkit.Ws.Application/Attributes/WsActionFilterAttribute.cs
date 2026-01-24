using Web.Api.Toolkit.Ws.Application.Contexts;

namespace Web.Api.Toolkit.Ws.Application.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public abstract class WsActionFilterAttribute : Attribute
    {
        public int Order { get; set; }

        public async virtual Task OnActionExecutingAsync(WebSocketRequestContext context)
        {
            return;
        }

        public async virtual Task OnActionExecuted(WebSocketRequestContext context)
        {
            return;
        }
    }
}
