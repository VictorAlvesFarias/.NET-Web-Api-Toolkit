using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Web.Api.Toolkit.Ws.Application.Workers;

namespace Web.Api.Toolkit.Ws.Application.Extensions
{
    public static class WebSocketApplicationExtensions
    {
        public static IServiceCollection AddWebSocketWorker(
            this IServiceCollection services,
            int maxConnectionsPerInstance = 100
        )
        {
            services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<WebSocketWorker>>();
                return new WebSocketWorker(logger, maxConnectionsPerInstance);
            });

            services.AddHostedService(sp => sp.GetRequiredService<WebSocketWorker>());

            return services;
        }

        public static IApplicationBuilder UseWebSocketEndpoint<T>(this IApplicationBuilder app, string path = "/ws") where T : WebSocketWorker
        {
            app.Map(path, builder =>
            {
                builder.Use(async (context, next) =>
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
            });

            return app;
        }
    }
}
