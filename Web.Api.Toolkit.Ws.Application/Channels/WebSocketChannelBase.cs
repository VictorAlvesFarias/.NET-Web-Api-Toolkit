using System;
using Web.Api.Toolkit.Ws.Application.Contexts;
using Web.Api.Toolkit.Ws.Application.Workers;

namespace Web.Api.Toolkit.Ws.Application.Channels
{
    public abstract class WebSocketChannelBase<TWorker> where TWorker : WebSocketClientWorker
    {
        private WebSocketRequestContext? _context;

        protected internal WebSocketRequestContext Context
        {
            get
            {
                if (_context == null)
                    throw new InvalidOperationException($"{nameof(Context)} is only available during an event execution.");

                return _context;
            }
        }

        internal void SetContext(WebSocketRequestContext context)
        {
            _context = context;
        }
    }
}