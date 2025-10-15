using System.Net.Http;
using BlazorAdmin.Services;
using BlazorShared;
using BlazorShared.Interfaces;
using BlazorShared.Models;
using Microsoft.Extensions.Options;

namespace BlazorAdmin.UnitTests.Services;

public sealed class CatalogItemService_Smoke
{
    private sealed class NoopLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => NullDisposable.Instance;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose() { }
        }
    }

    private sealed class FakeLookupService<T> : ICatalogLookupDataService<T> where T : LookupData, new()
    {
        public Task<List<T>> List() => Task.FromResult(new List<T>());
    }

    [Fact]
    public void CatalogItemService_Construct_Succeeds()
    {
        // Arrange: basic HttpService with default HttpClient, options, and toast
        var httpClient = new HttpClient();
        var options = Options.Create(new BaseUrlConfiguration { ApiBase = "http://localhost/" });
        var toast = new ToastService();
        var httpService = new HttpService(httpClient, options, toast);

        var brandService = new FakeLookupService<CatalogBrand>();
        var typeService = new FakeLookupService<CatalogType>();
        var logger = new NoopLogger<CatalogItemService>();

        // Act
        var svc = new CatalogItemService(brandService, typeService, httpService, logger);

        // Assert
        Assert.NotNull(svc);
    }
}
