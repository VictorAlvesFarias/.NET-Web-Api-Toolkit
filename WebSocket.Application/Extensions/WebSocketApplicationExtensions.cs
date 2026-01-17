using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using Web.Api.Toolkit.Ws.Application.Workers;

namespace Web.Api.Toolkit.Ws.Application.Extensions
{
    public static class WebSocketApplicationExtensions
    {
        public static IServiceCollection AddWebSocketWorker(
            this IServiceCollection services,
            int maxConnectionsPerInstance = 100,
            int inviteExpirationMinutes = 5
        )
        {
            services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<WebSocketWorker>>();
                return new WebSocketWorker(logger, maxConnectionsPerInstance, inviteExpirationMinutes);
            });

            services.AddHostedService(sp => sp.GetRequiredService<WebSocketWorker>());

            return services;
        }

        public static IApplicationBuilder UseWebSocketEndpoint<T>(this IApplicationBuilder app) where T : WebSocketWorker
        {
            app.Use(async (context, next) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var serviceProvider = context.RequestServices.GetRequiredService<T>();

                    await serviceProvider.AcceptWebSocketAsync(
                        context,
                        context.RequestAborted
                    );

                    return;
                }

                await next();
            });

            return app;
        }
    }
}
