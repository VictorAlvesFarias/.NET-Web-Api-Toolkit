using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Web.Api.Toolkit.Ws.Application.Workers;

namespace Web.Api.Toolkit.Ws.Application.Extensions
{
    /// <summary>
    /// Extensões para configurar WebSocket no ASP.NET Core (IIS Compatible)
    /// </summary>
    public static class WebSocketApplicationExtensions
    {
        /// <summary>
        /// Adiciona o WebSocketWorker aos serviços.
        /// 
        /// Uso no Program.cs:
        /// builder.Services.AddWebSocketWorker(maxConnectionsPerInstance: 100);
        /// </summary>
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

        /// <summary>
        /// Configura o endpoint WebSocket no pipeline ASP.NET Core.
        /// 
        /// Uso no Program.cs (antes de app.Run()):
        /// app.UseWebSocketEndpoint("/ws");
        /// 
        /// ✅ Funciona no IIS
        /// ✅ Funciona no Azure App Service
        /// ✅ Funciona com HTTPS
        /// ✅ Uma única porta, multiplexação lógica
        /// </summary>
        public static IApplicationBuilder UseWebSocketEndpoint(
            this IApplicationBuilder app,
            string path = "/ws"
        )
        {
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(30)
            });

            app.Map(path, builder =>
            {
                builder.Run(async context =>
                {
                    if (!context.WebSockets.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("WebSocket connection required");
                        return;
                    }

                    var worker = context.RequestServices.GetRequiredService<WebSocketWorker>();
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                    await worker.AcceptWebSocketAsync(context, webSocket, context.RequestAborted);
                });
            });

            return app;
        }

        /// <summary>
        /// Configuração avançada com custom options.
        /// 
        /// Uso:
        /// app.UseWebSocketEndpoint("/ws", options =>
        /// {
        ///     options.KeepAliveInterval = TimeSpan.FromSeconds(60);
        /// });
        /// </summary>
        public static IApplicationBuilder UseWebSocketEndpoint(
            this IApplicationBuilder app,
            string path,
            Action<WebSocketOptions> configureOptions
        )
        {
            var options = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(30)
            };

            configureOptions(options);

            app.UseWebSockets(options);

            app.Map(path, builder =>
            {
                builder.Run(async context =>
                {
                    if (!context.WebSockets.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("WebSocket connection required");
                        return;
                    }

                    var worker = context.RequestServices.GetRequiredService<WebSocketWorker>();
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                    await worker.AcceptWebSocketAsync(context, webSocket, context.RequestAborted);
                });
            });

            return app;
        }
    }
}
