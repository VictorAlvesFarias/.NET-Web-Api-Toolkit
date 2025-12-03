using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Web.Api.Toolkit.Ws.Application.Channels;
using Web.Api.Toolkit.Ws.Application.Workers;

namespace Web.Api.Toolkit.Ws.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all WebSocket channels associated with a specific WebSocketClientWorker type.
        /// Only channels that inherit from WebSocketChannelBase&lt;TWorker&gt; will be registered.
        /// </summary>
        /// <typeparam name="TWorker">The type of WebSocketClientWorker to register channels for.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWebSocketChannels<TWorker>(this IServiceCollection services)
            where TWorker : WebSocketClientWorker
        {
            return AddWebSocketChannels<TWorker>(services, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Registers all WebSocket channels associated with a specific WebSocketClientWorker type with a custom lifetime.
        /// </summary>
        /// <typeparam name="TWorker">The type of WebSocketClientWorker to register channels for.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime for channels.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWebSocketChannels<TWorker>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            where TWorker : WebSocketClientWorker
        {
            return AddWebSocketChannels<TWorker>(services, lifetime, AppDomain.CurrentDomain.GetAssemblies());
        }

        /// <summary>
        /// Registers all WebSocket channels associated with a specific WebSocketClientWorker type from specified assemblies.
        /// </summary>
        /// <typeparam name="TWorker">The type of WebSocketClientWorker to register channels for.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">The assemblies to scan for channels.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWebSocketChannels<TWorker>(
            this IServiceCollection services,
            params Assembly[] assemblies)
            where TWorker : WebSocketClientWorker
        {
            return AddWebSocketChannels<TWorker>(services, ServiceLifetime.Scoped, assemblies);
        }

        /// <summary>
        /// Registers all WebSocket channels associated with a specific WebSocketClientWorker type from specified assemblies with a custom lifetime.
        /// </summary>
        /// <typeparam name="TWorker">The type of WebSocketClientWorker to register channels for.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime for channels.</param>
        /// <param name="assemblies">The assemblies to scan for channels.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWebSocketChannels<TWorker>(
            this IServiceCollection services,
            ServiceLifetime lifetime,
            params Assembly[] assemblies)
            where TWorker : WebSocketClientWorker
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
        /// <summary>
        /// Registers all WebSocket channels found in all loaded assemblies.
        /// Channels are registered as scoped services by default.
        /// Similar to AddControllers() in MVC, this automatically discovers all channels.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWebSocketChannels(this IServiceCollection services)
        {
            return AddWebSocketChannels(services, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Registers all WebSocket channels found in all loaded assemblies with a custom lifetime.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime for channels.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWebSocketChannels(
            this IServiceCollection services,
            ServiceLifetime lifetime)
        {
            var channelTypes = AppDomain.CurrentDomain.GetAssemblies()
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
                .Where(t => typeof(WebSocketChannelBase).IsAssignableFrom(t) 
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

        /// <summary>
        /// Registers all WebSocket channels found in the specified assemblies.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">The assemblies to scan for channels.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWebSocketChannels(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            return AddWebSocketChannels(services, ServiceLifetime.Scoped, assemblies);
        }

        /// <summary>
        /// Registers all WebSocket channels found in the specified assemblies with a custom lifetime.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime for channels.</param>
        /// <param name="assemblies">The assemblies to scan for channels.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWebSocketChannels(
            this IServiceCollection services,
            ServiceLifetime lifetime,
            params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                return AddWebSocketChannels(services, lifetime);
            }

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
                .Where(t => typeof(WebSocketChannelBase).IsAssignableFrom(t) 
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
    }
}

