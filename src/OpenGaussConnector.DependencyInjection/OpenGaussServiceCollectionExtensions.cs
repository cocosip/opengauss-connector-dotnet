using System;
using System.Data.Common;
using System.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OpenGaussConnector;
using OpenGaussConnector.DependencyInjection;
using OpenGaussConnector.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Registers OpenGaussConnector services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class OpenGaussServiceCollectionExtensions
    {
        /// <summary>
        /// Registers closed, transient <see cref="OpenGaussConnection"/> instances. Every resolution yields a
        /// connection owned by the resolving scope. If no <see cref="DbConnection"/> is registered, an alias is
        /// added so that ADO.NET consumers can resolve the base type.
        /// </summary>
        public static IServiceCollection AddOpenGaussConnection(this IServiceCollection services, string connectionString)
        {
            Validate(services, connectionString);

            if (Has(services, typeof(OpenGaussConnection)))
                throw new InvalidOperationException("An OpenGauss connection is already registered.");

            var addAlias = !Has(services, typeof(DbConnection));
            services.AddTransient(sp => { InstallLogging(sp); return new OpenGaussConnection(connectionString); });
            if (addAlias)
                services.AddTransient<DbConnection>(sp => sp.GetRequiredService<OpenGaussConnection>());

            return services;
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// Registers a singleton <see cref="OpenGaussDataSource"/> owned by the container. The data source keeps
        /// its own pool; if no <see cref="DbDataSource"/> is registered, an alias is added for the base type.
        /// </summary>
        public static IServiceCollection AddOpenGaussDataSource(
            this IServiceCollection services,
            string connectionString,
            Action<IServiceProvider, OpenGaussDataSourceBuilder>? configure = null)
        {
            Validate(services, connectionString);

            if (Has(services, typeof(OpenGaussDataSource)))
                throw Conflict();

            var addAlias = !Has(services, typeof(DbDataSource));
            services.AddSingleton(sp => Build(sp, connectionString, configure));
            if (addAlias)
                services.AddSingleton<DbDataSource>(sp => sp.GetRequiredService<OpenGaussDataSource>());

            return services;
        }

        /// <summary>
        /// Registers a keyed singleton <see cref="OpenGaussDataSource"/> for <paramref name="serviceKey"/>. Each key
        /// owns an independent data source and connection pool, and resolves to the same instance through the
        /// concrete type and through <see cref="DbDataSource"/>.
        /// </summary>
        public static IServiceCollection AddKeyedOpenGaussDataSource(
            this IServiceCollection services,
            object? serviceKey,
            string connectionString,
            Action<IServiceProvider, OpenGaussDataSourceBuilder>? configure = null)
        {
            Validate(services, connectionString);

            if (serviceKey is null)
                throw new ArgumentNullException(nameof(serviceKey));
            if (Equals(serviceKey, KeyedService.AnyKey))
                throw new ArgumentException("A concrete service key is required.", nameof(serviceKey));
            if (Has(services, typeof(OpenGaussDataSource), serviceKey) || Has(services, typeof(DbDataSource), serviceKey))
                throw Conflict();

            services.AddKeyedSingleton<OpenGaussDataSource>(serviceKey, (sp, _) => Build(sp, connectionString, configure));
            services.AddKeyedSingleton<DbDataSource>(serviceKey, (sp, key) => sp.GetRequiredKeyedService<OpenGaussDataSource>(key));

            return services;
        }

        static OpenGaussDataSource Build(
            IServiceProvider services, string connectionString, Action<IServiceProvider, OpenGaussDataSourceBuilder>? configure)
        {
            InstallLogging(services);

            var builder = new OpenGaussDataSourceBuilder(connectionString);
            configure?.Invoke(services, builder);
            return builder.Build();
        }
#endif

        /// <summary>
        /// Routes the driver's own log entries into the container's <see cref="ILoggerFactory"/>. The first
        /// registration wins for the process: the driver's logging entry point is process-wide, so a provider
        /// already assigned through <c>OpenGaussLogManager.Provider</c> is left alone.
        /// </summary>
        public static IServiceCollection AddOpenGaussLogging(this IServiceCollection services)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<IOpenGaussLoggingProvider>(
                sp => new MicrosoftExtensionsLoggingProvider(sp.GetService<ILoggerFactory>()));
            return services;
        }

        /// <summary>
        /// Installs the container's logging provider, if one was registered, before the first connection is built.
        /// </summary>
        static void InstallLogging(IServiceProvider services)
        {
            if (services.GetService<IOpenGaussLoggingProvider>() is { } provider)
                OpenGaussLogManager.TrySetProvider(provider);
        }

        static bool Has(IServiceCollection services, Type serviceType, object? serviceKey = null)
            => services.Any(d => d.ServiceType == serviceType &&
                                 (serviceKey is null
                                     ? !d.IsKeyedService
                                     : d.IsKeyedService && Equals(d.ServiceKey, serviceKey)));

        static InvalidOperationException Conflict()
            => new("A data source is already registered for this service key.");

        static void Validate(IServiceCollection services, string connectionString)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("A connection string is required.", nameof(connectionString));
        }
    }
}
