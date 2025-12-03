using System;
using Web.Api.Toolkit.Ws.Application.Contexts;
using Web.Api.Toolkit.Ws.Application.Workers;

namespace Web.Api.Toolkit.Ws.Application.Channels
{
    /// <summary>
    /// Base class for WebSocket channels without worker association.
    /// For channels associated with a specific worker, use WebSocketChannelBase&lt;TWorker&gt;.
    /// </summary>
    public abstract class WebSocketChannelBase
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

    /// <summary>
    /// Base class for WebSocket channels associated with a specific WebSocketClientWorker type.
    /// This allows multiple WebSocket workers to have their own set of channels.
    /// </summary>
    /// <typeparam name="TWorker">The type of WebSocketClientWorker this channel is associated with.</typeparam>
    public abstract class WebSocketChannelBase<TWorker> : WebSocketChannelBase
        where TWorker : WebSocketClientWorker
    {
    }
}


