#if NET8_0_OR_GREATER

using System;

namespace OpenGaussConnector
{
    /// <summary>
    /// Builds an <see cref="OpenGaussDataSource"/>. The data source owns its connection pool, so several
    /// data sources built from the same connection string keep separate pools.
    /// </summary>
    public sealed class OpenGaussDataSourceBuilder
    {
        /// <summary>
        /// Creates a builder with an empty connection string.
        /// </summary>
        public OpenGaussDataSourceBuilder() : this(string.Empty) {}

        /// <summary>
        /// Creates a builder for the given connection string.
        /// </summary>
        public OpenGaussDataSourceBuilder(string? connectionString)
            => ConnectionString = connectionString ?? string.Empty;

        /// <summary>
        /// The connection string used for connections created by the built data source.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Creates the data source. The returned instance owns its pool and must be disposed.
        /// </summary>
        public OpenGaussDataSource Build() => OpenGaussDataSource.Create(ConnectionString);
    }
}

#endif
