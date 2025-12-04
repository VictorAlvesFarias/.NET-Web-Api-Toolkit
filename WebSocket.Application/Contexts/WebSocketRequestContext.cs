using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Web.Api.Toolkit.Ws.Application.Dtos;
using Web.Api.Toolkit.Ws.Application.Workers;

namespace Web.Api.Toolkit.Ws.Application.Contexts
{
    public class WebSocketRequestContext
    {
        public WebSocketRequestContext(
            string request,
            IServiceProvider services,
            CancellationToken cancellationToken,
            WebSocketClientWorker clientWorker)
        {
            Request = request;
            Services = services;
            CancellationToken = cancellationToken;
            ClientWorker = clientWorker;
        }

        public string Request { get; }
        public IServiceProvider Services { get; }
        public CancellationToken CancellationToken { get; }
        public WebSocketClientWorker ClientWorker { get; }

        public async Task SendAsync<TPayload>(TPayload payload, CancellationToken token = default)
        {
            var effectiveToken = token == default ? CancellationToken : token;
            await ClientWorker.SendAsync(payload, effectiveToken);
        }

        public TService GetService<TService>() where TService : notnull
        {
            return Services.GetRequiredService<TService>();
        }
    }
}


