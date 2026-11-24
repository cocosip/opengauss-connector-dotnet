using System;

namespace OpenGaussConnector
{
    /// <summary>
    /// Builds the <see cref="ConnectorSource"/> for a connection string. Shared by the
    /// process-wide pool (<see cref="PoolManager"/>) and by an <c>OpenGaussDataSource</c>,
    /// which owns its source instead of registering it globally.
    /// </summary>
    static class ConnectorSourceFactory
    {
        /// <summary>
        /// Validates the host/port shape and splits a single-host <c>host:port</c> value.
        /// Must run before the canonical connection string is computed.
        /// </summary>
        internal static void NormalizeSettings(OpenGaussConnectionStringBuilder settings)
        {
            var hostsSeparator = settings.Host?.IndexOf(',');
            if (hostsSeparator.HasValue && hostsSeparator != -1)
                return;

            if (settings.TargetSessionAttributesParsed is not null &&
                settings.TargetSessionAttributesParsed != TargetSessionAttributes.Any)
            {
                throw new NotSupportedException("Target Session Attributes other then Any is only supported with multiple hosts");
            }

            if (!OpenGaussConnectionStringBuilder.IsUnixSocket(settings.Host!, settings.Port, out _) &&
                OpenGaussConnectionStringBuilder.TrySplitHostPort(settings.Host!.AsSpan(), out var newHost, out var newPort))
            {
                settings.Host = newHost;
                settings.Port = newPort;
            }
        }

        internal static ConnectorSource Create(OpenGaussConnectionStringBuilder settings, string canonical)
        {
            var hostsSeparator = settings.Host?.IndexOf(',');

            if (hostsSeparator.HasValue && hostsSeparator != -1)
            {
                if (settings.Multiplexing)
                    throw new NotSupportedException("Multiplexing is not supported with multiple hosts");
                if (settings.ReplicationMode != ReplicationMode.Off)
                    throw new NotSupportedException("Replication is not supported with multiple hosts");
                return new MultiHostConnectorPool(settings, canonical);
            }

            if (settings.Multiplexing)
                return new MultiplexingConnectorPool(settings, canonical);

            return settings.Pooling
                ? new ConnectorPool(settings, canonical)
                : new UnpooledConnectorSource(settings, canonical);
        }

        /// <summary>
        /// The multi-host pool is keyed on the canonical string, which drops TargetSessionAttributes,
        /// so a connection (or data source) that specifies them gets its own wrapper.
        /// </summary>
        internal static ConnectorSource WrapForConnection(
            OpenGaussConnectionStringBuilder settings, string connectionString, ConnectorSource source)
            => source is MultiHostConnectorPool multiHost
                ? new MultiHostConnectorPoolWrapper(settings, connectionString, multiHost)
                : source;
    }
}
