using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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

    // Minimal fake handler to intercept HttpClient requests used by HttpService
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
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

    [Fact]
    public async Task CatalogItemService_List_UsesMockedHttpClient_ReturnsItems()
    {
        // Arrange: HttpClient with a fake handler that returns a simple payload for GET catalog-items
        var payload = new PagedCatalogItemResponse
        {
            CatalogItems = new List<CatalogItem>
            {
                new CatalogItem
                {
                    Id = 1,
                    CatalogBrandId = 1,
                    CatalogTypeId = 1,
                    Name = "Test Item",
                    Description = "Desc",
                    Price = 9.99m,
                    PictureUri = ""
                }
            },
            PageCount = 1
        };

        var handler = new FakeHttpMessageHandler(req =>
        {
            // Only handle GET requests that our service issues; default OK with payload
            if (req.Method == HttpMethod.Get && req.RequestUri != null && req.RequestUri.ToString().Contains("catalog-items"))
            {
                var json = JsonSerializer.Serialize(payload);
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                return response;
            }

            // Return 404 for anything unexpected to surface issues
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("", Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var options = Options.Create(new BaseUrlConfiguration { ApiBase = "http://localhost/" });
        var toast = new ToastService();
        var httpService = new HttpService(httpClient, options, toast);

        var brandService = new FakeLookupService<CatalogBrand>();
        var typeService = new FakeLookupService<CatalogType>();
        var logger = new NoopLogger<CatalogItemService>();
        var svc = new CatalogItemService(brandService, typeService, httpService, logger);

        // Act
        var result = await svc.List();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Test Item", result[0].Name);
    }
}
