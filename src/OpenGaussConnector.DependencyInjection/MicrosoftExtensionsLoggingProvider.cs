using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenGaussConnector.Logging;

namespace OpenGaussConnector.DependencyInjection
{
    /// <summary>
    /// Bridges the driver's own logging abstraction to <see cref="ILoggerFactory"/>, so a driver log entry appears
    /// in the application's logging pipeline with its category, level and connection id.
    /// </summary>
    /// <remarks>
    /// The driver's logging entry point is process-wide while a container can be disposed, so the bridge resolves
    /// each category lazily and stays silent once the factory is gone instead of failing driver code.
    /// </remarks>
    sealed class MicrosoftExtensionsLoggingProvider : IOpenGaussLoggingProvider
    {
        readonly ILoggerFactory _loggerFactory;
        readonly ConcurrentDictionary<string, OpenGaussLogger> _loggers = new();

        internal MicrosoftExtensionsLoggingProvider(ILoggerFactory? loggerFactory)
            => _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

        public OpenGaussLogger CreateLogger(string name) => _loggers.GetOrAdd(name, n => new Logger(_loggerFactory, n));

        sealed class Logger : OpenGaussLogger
        {
            readonly ILoggerFactory _factory;
            readonly string _name;
            ILogger? _logger;
            bool _unavailable;

            internal Logger(ILoggerFactory factory, string name) => (_factory, _name) = (factory, name);

            public override bool IsEnabled(OpenGaussLogLevel level)
                => Resolve()?.IsEnabled(ToLogLevel(level)) ?? false;

            public override void Log(OpenGaussLogLevel level, int connectorId, string msg, Exception? exception = null)
            {
                if (Resolve() is not { } logger)
                    return;

                var logLevel = ToLogLevel(level);
                try
                {
                    // The category already names the driver component; the connection id identifies the physical
                    // connection, and 0 means "not tied to one".
                    if (connectorId == 0)
                        logger.Log(logLevel, exception, "{Message}", msg);
                    else
                        logger.Log(logLevel, exception, "[{ConnectionId}] {Message}", connectorId, msg);
                }
                catch (ObjectDisposedException)
                {
                    _unavailable = true;
                }
            }

            ILogger? Resolve()
            {
                if (_logger is not null || _unavailable)
                    return _logger;

                try
                {
                    return _logger = _factory.CreateLogger(_name);
                }
                catch (ObjectDisposedException)
                {
                    _unavailable = true;
                    return null;
                }
            }

            static LogLevel ToLogLevel(OpenGaussLogLevel level) => level switch
            {
                OpenGaussLogLevel.Trace => LogLevel.Trace,
                OpenGaussLogLevel.Debug => LogLevel.Debug,
                OpenGaussLogLevel.Info  => LogLevel.Information,
                OpenGaussLogLevel.Warn  => LogLevel.Warning,
                OpenGaussLogLevel.Error => LogLevel.Error,
                OpenGaussLogLevel.Fatal => LogLevel.Critical,
                _                       => LogLevel.None
            };
        }
    }
}
