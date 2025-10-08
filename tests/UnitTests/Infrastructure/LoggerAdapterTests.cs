using System;
using System.Collections.Concurrent;
using Microsoft.eShopWeb.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.Infrastructure
{
    public class LoggerAdapterTests
    {
        private class TestLoggerProvider : ILoggerProvider
        {
            public ConcurrentQueue<string> Infos { get; } = new();
            public ConcurrentQueue<string> Warnings { get; } = new();

            public ILogger CreateLogger(string categoryName) => new TestLogger(Infos, Warnings);
            public void Dispose() { }

            private class TestLogger : ILogger
            {
                private readonly ConcurrentQueue<string> _infos;
                private readonly ConcurrentQueue<string> _warnings;

                public TestLogger(ConcurrentQueue<string> infos, ConcurrentQueue<string> warnings)
                {
                    _infos = infos;
                    _warnings = warnings;
                }

                public IDisposable BeginScope<TState>(TState state) => null!;
                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception?, string> formatter)
                {
                    var msg = formatter(state, exception);
                    if (logLevel == LogLevel.Information) _infos.Enqueue(msg);
                    if (logLevel == LogLevel.Warning) _warnings.Enqueue(msg);
                }
            }
        }

        private class Sample {}

        [Fact]
        public void LogInformation_ForwardsToUnderlyingLogger()
        {
            var provider = new TestLoggerProvider();
            using var factory = LoggerFactory.Create(builder => builder.AddProvider(provider));

            var adapter = new LoggerAdapter<Sample>(factory);
            adapter.LogInformation("Hello {0}", "World");

            Assert.True(provider.Infos.TryDequeue(out var logged));
            Assert.Contains("Hello World", logged);
        }

        [Fact]
        public void LogWarning_ForwardsToUnderlyingLogger()
        {
            var provider = new TestLoggerProvider();
            using var factory = LoggerFactory.Create(builder => builder.AddProvider(provider));

            var adapter = new LoggerAdapter<Sample>(factory);
            adapter.LogWarning("Warn {0}", 123);

            Assert.True(provider.Warnings.TryDequeue(out var logged));
            Assert.Contains("Warn 123", logged);
        }
    }
}
