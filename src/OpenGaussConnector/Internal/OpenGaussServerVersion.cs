using System;
using System.Globalization;
using OpenGaussConnector.Logging;

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

            return configured.Build <= 0 || configured.Build == Math.Max(productVersion.Build, 0);
        }

        internal static string Format(Version version) =>
            string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", version.Major, version.Minor, Math.Max(version.Build, 0));
    }
}
