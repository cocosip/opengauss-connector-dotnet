using System;

namespace OpenGaussConnector.Logging
{
    /// <summary>
    /// Manages logging for OpenGauss, used to set the logging provider.
    /// </summary>
    public static class OpenGaussLogManager
    {
        /// <summary>
        /// The logging provider used for logging in OpenGauss.
        /// </summary>
        public static IOpenGaussLoggingProvider Provider
        {
            get
            {
                _providerRetrieved = true;
                return _provider;
            }
            set
            {
                if (_providerRetrieved)
                    throw new InvalidOperationException("The logging provider must be set before any OpenGauss action is taken");

                _provider = value ?? throw new ArgumentNullException(nameof(value));
                _providerExplicitlySet = true;
            }
        }

        /// <summary>
        /// Installs <paramref name="provider"/> unless the application assigned <see cref="Provider"/> itself.
        /// Loggers are created on demand from <see cref="Provider"/>, so a provider installed by an integration
        /// (such as the dependency injection package) takes effect even after the driver has been used; integrations
        /// may replace each other, and the most recently resolved one wins process-wide.
        /// </summary>
        /// <returns><see langword="true"/> when the provider was installed.</returns>
        public static bool TrySetProvider(IOpenGaussLoggingProvider provider)
        {
            if (provider is null)
                throw new ArgumentNullException(nameof(provider));

            if (_providerExplicitlySet)
                return false;

            _provider = provider;
            return true;
        }

        /// <summary>
        /// Determines whether parameter contents will be logged alongside SQL statements - this may reveal sensitive information.
        /// Defaults to false.
        /// </summary>
        public static bool IsParameterLoggingEnabled { get; set; }

        static IOpenGaussLoggingProvider _provider = new NoOpLoggingProvider();
        static bool _providerRetrieved;
        static bool _providerExplicitlySet;

        internal static OpenGaussLogger CreateLogger(string name) => Provider.CreateLogger("OpenGaussConnector." + name);
    }
}
