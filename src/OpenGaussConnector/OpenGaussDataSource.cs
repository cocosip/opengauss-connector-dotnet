#if NET8_0_OR_GREATER

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OpenGaussConnector
{
    /// <summary>
    /// A factory for <see cref="OpenGaussConnection"/> instances that owns its own connector source
    /// (and therefore its own connection pool) instead of using the process-wide pool keyed by
    /// connection string.
    /// </summary>
    /// <remarks>
    /// Connections created here are closed when handed out and must be disposed by the caller; disposing
    /// them returns the physical connection to this data source's pool. Disposing the data source releases
    /// the pool. Use <see cref="OpenGaussDataSourceBuilder"/> to create an instance.
    /// </remarks>
    public sealed class OpenGaussDataSource : DbDataSource
    {
        internal ConnectorSource Source { get; }

        bool _disposed;

        /// <summary>
        /// The connection string as configured, including the password. Exposed so that a connection created
        /// here can rebuild the same settings (for example when cloned).
        /// </summary>
        internal string OriginalConnectionString { get; }

        internal OpenGaussDataSource(ConnectorSource source, string originalConnectionString)
        {
            Source = source;
            OriginalConnectionString = originalConnectionString;
        }

        /// <inheritdoc />
        public override string ConnectionString => Source.UserFacingConnectionString;

        OpenGaussConnection NewConnection() => new(Source, OriginalConnectionString, ConnectionString);

        /// <inheritdoc />
        protected override DbConnection CreateDbConnection()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return NewConnection();
        }

        /// <inheritdoc />
        protected override DbConnection OpenDbConnection()
        {
            var connection = NewConnection();
            try
            {
                connection.Open();
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        protected override async ValueTask<DbConnection> OpenDbConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = NewConnection();
            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return connection;
            }
            catch
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Not supported: a command executes on a connection, and the ported command needs a connection to
        /// obtain one from this data source's pool. Create a connection first.
        /// </summary>
        protected override DbCommand CreateDbCommand(string? cmdText)
            => throw new NotSupportedException(
                "Create a connection from this data source and use its CreateCommand; a data source cannot own a command.");

        /// <summary>
        /// Not supported: see <see cref="CreateDbCommand"/>.
        /// </summary>
        protected override DbBatch CreateDbBatch()
            => throw new NotSupportedException(
                "Create a connection from this data source and use its CreateBatch; a data source cannot own a batch.");

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                Source.Dispose();
            }

            base.Dispose(disposing);
        }

        internal static OpenGaussDataSource Create(string connectionString)
        {
            var settings = new OpenGaussConnectionStringBuilder(connectionString);
            settings.Validate();
            ConnectorSourceFactory.NormalizeSettings(settings);

            var source = ConnectorSourceFactory.Create(settings, settings.ConnectionStringForMultipleHosts);
            source = ConnectorSourceFactory.WrapForConnection(settings, connectionString, source);

            return new OpenGaussDataSource(source, connectionString);
        }
    }
}

#endif
