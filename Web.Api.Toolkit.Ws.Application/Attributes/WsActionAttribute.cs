using System;
using Web.Api.Toolkit.Ws.Application.Contexts;

namespace Web.Api.Toolkit.Ws.Application.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class WsActionAttribute : WsActionFilterAttribute
    {
        public WsActionAttribute(string @event)
        {
            if (string.IsNullOrWhiteSpace(@event))
                throw new ArgumentException("The event name is required.", nameof(@event));

            Event = @event;
        }

        public string Event { get; }
    }
}


