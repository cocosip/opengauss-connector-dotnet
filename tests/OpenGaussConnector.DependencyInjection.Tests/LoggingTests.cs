using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenGaussConnector.Logging;
using Xunit;

namespace OpenGaussConnector.DependencyInjection.Tests
{
    /// <summary>
    /// The driver keeps its own logging abstraction; these tests cover the bridge that routes it into the
    /// container's ILoggerFactory.
    /// </summary>
    public class LoggingTests
    {
        const string ConnectionString = "Host=127.0.0.1;Port=5432;Database=test;Username=test;Password=secret";

        [Fact]
        public void AddOpenGaussLogging_routes_driver_logs_into_the_container_pipeline()
        {
            var capture = new CapturingProvider();

            using var provider = new ServiceCollection()
                .AddLogging(logging => logging.AddProvider(capture).SetMinimumLevel(LogLevel.Trace))
                .AddOpenGaussLogging()
                .AddOpenGaussConnection(ConnectionString)
                .BuildServiceProvider();

            provider.GetRequiredService<OpenGaussConnection>();

            OpenGaussLogManager.CreateLogger("Probe").Info("hello driver", 42);

            Assert.Contains(capture.Entries, entry =>
                entry.Category == "OpenGaussConnector.Probe" &&
                entry.Message.Contains("hello driver", StringComparison.Ordinal) &&
                entry.Message.Contains("42", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData(OpenGaussLogLevel.Trace, LogLevel.Trace)]
        [InlineData(OpenGaussLogLevel.Debug, LogLevel.Debug)]
        [InlineData(OpenGaussLogLevel.Info, LogLevel.Information)]
        [InlineData(OpenGaussLogLevel.Warn, LogLevel.Warning)]
        [InlineData(OpenGaussLogLevel.Error, LogLevel.Error)]
        [InlineData(OpenGaussLogLevel.Fatal, LogLevel.Critical)]
        public void Maps_every_driver_level(OpenGaussLogLevel driverLevel, LogLevel expected)
        {
            var capture = new CapturingProvider();
            using var factory = LoggerFactory.Create(logging => logging.AddProvider(capture).SetMinimumLevel(LogLevel.Trace));
            var logger = new MicrosoftExtensionsLoggingProvider(factory).CreateLogger("OpenGaussConnector.Levels");

            logger.Log(driverLevel, 0, "message");

            var entry = Assert.Single(capture.Entries);
            Assert.Equal(expected, entry.Level);
            Assert.Equal("message", entry.Message);
        }

        [Fact]
        public void Installing_a_null_provider_is_rejected()
            => Assert.Throws<ArgumentNullException>(() => OpenGaussLogManager.TrySetProvider(null!));

        sealed class CapturingProvider : ILoggerProvider
        {
            internal ConcurrentQueue<Entry> Entries { get; } = new();

            public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, Entries);

            public void Dispose() { }

            internal sealed record Entry(string Category, LogLevel Level, string Message);

            sealed class CapturingLogger : ILogger
            {
                readonly string _category;
                readonly ConcurrentQueue<Entry> _entries;

                internal CapturingLogger(string category, ConcurrentQueue<Entry> entries)
                    => (_category, _entries) = (category, entries);

                public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(
                    LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                    Func<TState, Exception?, string> formatter)
                    => _entries.Enqueue(new Entry(_category, logLevel, formatter(state, exception)));
            }
        }
    }
}
