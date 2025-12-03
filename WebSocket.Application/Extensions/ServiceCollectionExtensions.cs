using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Web.Api.Toolkit.Ws.Application.Channels;
using Web.Api.Toolkit.Ws.Application.Workers;

namespace Web.Api.Toolkit.Ws.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWebSocketChannels<TWorker>(this IServiceCollection services)  where TWorker : WebSocketClientWorker
        {
            return AddWebSocketChannels<TWorker>(services, ServiceLifetime.Scoped);
        }

        public static IServiceCollection AddWebSocketChannels<TWorker>(this IServiceCollection services, ServiceLifetime lifetime) where TWorker : WebSocketClientWorker
        {
            return AddWebSocketChannels<TWorker>(services, lifetime, AppDomain.CurrentDomain.GetAssemblies());
        }

        public static IServiceCollection AddWebSocketChannels<TWorker>(this IServiceCollection services, ServiceLifetime lifetime, params Assembly[] assemblies) where TWorker : WebSocketClientWorker
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }

            var workerType = typeof(TWorker);
            var baseChannelType = typeof(WebSocketChannelBase<>).MakeGenericType(workerType);

            var channelTypes = assemblies
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        return Array.Empty<Type>();
                    }
                })
                .Where(t => baseChannelType.IsAssignableFrom(t) 
                    && !t.IsAbstract 
                    && !t.IsInterface
                    && t.IsClass)
                .Distinct()
                .ToList();

            foreach (var channelType in channelTypes)
            {
                services.Add(new ServiceDescriptor(channelType, channelType, lifetime));
            }

            return services;
        }

        // Removed non-generic AddWebSocketChannels methods since WebSocketChannelBase now requires a generic type parameter
        // Use AddWebSocketChannels<TWorker>() instead
    }
}

