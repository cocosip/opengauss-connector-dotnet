using System;
using System.Threading.Tasks;
using OpenGaussConnector.Logging;
using OpenGaussConnector.Util;

namespace OpenGaussConnector.Internal
{
    /// <summary>
    /// Identifies the openGauss product version from the raw <c>SELECT version()</c> result and compares it with
    /// the <c>Server Version</c> setting, so that a connection never silently uses a capability path that was
    /// verified for a different product version.
    /// </summary>
    static class OpenGaussServerVersion
    {
        const string ProductMarker = "openGauss";

        /// <summary>
        /// Extracts the openGauss product version from a <c>SELECT version()</c> result. Returns null when the
        /// result carries no recognizable openGauss marker, which is the documented "unknown" state.
        /// </summary>
        /// <remarks>
        /// Both forms appear in the wild:
        /// <c>openGauss 6.0.6 (openGauss 6.0.6 build 25f7a4d0) compiled at ...</c> and
        /// <c>PostgreSQL 9.2.4 (openGauss 6.0.6 build 25f7a4d0) ...</c>.
        /// </remarks>
        internal static Version? TryParseProductVersion(string? longVersion)
        {
            if (string.IsNullOrEmpty(longVersion))
                return null;

            var marker = longVersion.IndexOf(ProductMarker, StringComparison.OrdinalIgnoreCase);
            if (marker == -1)
                return null;

            var index = marker + ProductMarker.Length;
            while (index < longVersion.Length && longVersion[index] == ' ')
                index++;

            var start = index;
            while (index < longVersion.Length && (char.IsDigit(longVersion[index]) || longVersion[index] == '.'))
                index++;

            if (index == start)
                return null;

            var text = longVersion.Substring(start, index - start).TrimEnd('.');
            return Version.TryParse(text, out var version) ? version : null;
        }

        /// <summary>
        /// Parses the <c>Server Version</c> setting. <c>Auto</c>, an empty value and null all mean "identify the
        /// server", and a version must have a major and a minor component.
        /// </summary>
        /// <exception cref="ArgumentException">The value is not <c>Auto</c> and not a version number.</exception>
        internal static Version? ParseConfigured(string? value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var text = value.Trim();
            if (string.Equals(text, "Auto", StringComparison.OrdinalIgnoreCase))
                return null;

            // Version.Parse accepts a single component; a product version always names at least major.minor.
            if (text.IndexOf('.') == -1)
                throw new ArgumentException(
                    $"Server Version '{value}' must be 'Auto' or a version such as 6.0.6.", parameterName);

            if (!Version.TryParse(text, out var version))
                throw new ArgumentException(
                    $"Server Version '{value}' must be 'Auto' or a version such as 6.0.6.", parameterName);

            return version;
        }

        /// <summary>
        /// Fails the connection when the configured <c>Server Version</c> contradicts the version the server
        /// reports, and records the decision otherwise.
        /// </summary>
        internal static void EnsureCompatible(
            OpenGaussConnectionStringBuilder settings, OpenGaussDatabaseInfo databaseInfo, OpenGaussLogger log,
            int connectorId)
        {
            if (settings.ServerVersionParsed is not { } configured)
                return;

            var productVersion = databaseInfo.ProductVersion;
            if (productVersion is null)
            {
                // The server did not identify itself, so the configuration selects a verified candidate path.
                log.Trace(
                    $"Server Version {configured} was configured; the server did not identify an openGauss " +
                    $"product version (SELECT version(): {databaseInfo.LongServerVersion ?? "<none>"}), using it as the assumption.",
                    connectorId);
                return;
            }

            if (IsCompatible(configured, productVersion))
            {
                log.Trace($"Server Version {configured} matches the identified openGauss {productVersion}.", connectorId);
                return;
            }

            throw new OpenGaussException(
                $"Server Version {configured} was configured, but the server reports openGauss {productVersion} " +
                $"(SELECT version(): {databaseInfo.LongServerVersion}). Remove the Server Version setting to identify " +
                $"the server automatically, or set it to {productVersion}.");
        }

        /// <summary>
        /// Whether a configured version can describe a server whose product version is <paramref name="productVersion"/>.
        /// An unknown product version accepts any configuration, because the caller then selects the candidate path.
        /// </summary>
        internal static bool IsCompatible(Version configured, Version? productVersion)
        {
            if (productVersion is null)
                return true;

            if (configured.Major != productVersion.Major || configured.Minor != productVersion.Minor)
                return false;

            return configured.Build < 0 || configured.Build == Math.Max(productVersion.Build, 0);
        }

        /// <summary>
        /// Reads the database compatibility mode the first time a database is described. It must run after the
        /// database info is bound to the connector's type mapper, because executing a command resolves handlers
        /// through it. Servers without the parameter, or connections without permission for it, stay unknown
        /// instead of failing.
        /// </summary>
        internal static async ValueTask EnsureCompatibilityModeKnown(
            PhysicalConnector connector, OpenGaussDatabaseInfo database, OpenGaussTimeout timeout, bool async)
        {
            if (database.ProductVersion is null || database.CompatibilityMode is not null)
                return;

            try
            {
                using var command = connector.CreateCommand("SHOW sql_compatibility");
                if (timeout.IsSet)
                    command.CommandTimeout = (int)timeout.CheckAndGetTimeLeft().TotalSeconds;

                database.CompatibilityMode = (async
                    ? await command.ExecuteScalarAsync()
                    : command.ExecuteScalar()) as string;
            }
            catch (PostgresException e)
            {
                Log.Trace($"SHOW sql_compatibility was refused ({e.SqlState}); the compatibility mode stays unknown.", connector.Id);
            }
            catch (OpenGaussException e)
            {
                Log.Trace($"SHOW sql_compatibility failed ({e.Message}); the compatibility mode stays unknown.", connector.Id);
            }

            if (database.CompatibilityMode is { } mode)
                Log.Trace($"Identified openGauss {database.ProductVersion} in compatibility mode {mode}.", connector.Id);
        }

        static readonly OpenGaussLogger Log = OpenGaussLogManager.CreateLogger(nameof(OpenGaussServerVersion));
    }
}
