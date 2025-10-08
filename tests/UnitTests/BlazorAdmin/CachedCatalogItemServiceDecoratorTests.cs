using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using BlazorAdmin.Services;
using BlazorShared.Interfaces;
using BlazorShared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.BlazorAdmin
{
    public class CachedCatalogItemServiceDecoratorTests
    {
        private class TestHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
            public int Calls { get; private set; } = 0;

            public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Calls++;
                return Task.FromResult(_responder(request));
            }
        }

        private CatalogItemService BuildCatalogItemService(TestHttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            var options = Substitute.For<IOptions<BaseUrlConfiguration>>();
            options.Value.Returns(new BaseUrlConfiguration { ApiBase = "http://localhost/" });
            var toast = new ToastService();
            var httpService = new HttpService(httpClient, options, toast);

            var brands = Substitute.For<ICatalogLookupDataService<CatalogBrand>>();
            var types = Substitute.For<ICatalogLookupDataService<CatalogType>>();
            brands.List().Returns(new List<CatalogBrand>());
            types.List().Returns(new List<CatalogType>());

            var logger = Substitute.For<ILogger<CatalogItemService>>();

            return new CatalogItemService(brands, types, httpService, logger);
        }

        private static HttpResponseMessage JsonResponse<T>(T obj)
        {
            var json = JsonSerializer.Serialize(obj);
            var msg = new HttpResponseMessage(HttpStatusCode.OK);
            msg.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return msg;
        }

        [Fact]
        public async Task List_ReturnsCachedValue_WhenCacheNotExpired()
        {
            var items = new List<CatalogItem> { new CatalogItem { Id = 1, Name = "A" } };
            var paged = new PagedCatalogItemResponse { CatalogItems = items };

            var handler = new TestHttpMessageHandler(_ => JsonResponse(paged));
            var inner = BuildCatalogItemService(handler);

            var storage = Substitute.For<ILocalStorageService>();
            var logger = Substitute.For<ILogger<CachedCatalogItemServiceDecorator>>();

            // Prime cache with not-expired entry
            var entry = new CacheEntry<List<CatalogItem>>(items) { DateCreated = DateTime.UtcNow };
            storage.GetItemAsync<CacheEntry<List<CatalogItem>>>("items").Returns(entry);

            var sut = new CachedCatalogItemServiceDecorator(storage, inner, logger);

            var result = await sut.List();

            Assert.Same(items, result);
            Assert.Equal(0, handler.Calls); // no HTTP call due to cache hit
        }

        [Fact]
        public async Task List_FetchesAndStores_WhenCacheMissing()
        {
            var items = new List<CatalogItem> { new CatalogItem { Id = 2, Name = "B" } };
            var paged = new PagedCatalogItemResponse { CatalogItems = items };

            var handler = new TestHttpMessageHandler(_ => JsonResponse(paged));
            var inner = BuildCatalogItemService(handler);

            var storage = Substitute.For<ILocalStorageService>();
            storage.GetItemAsync<CacheEntry<List<CatalogItem>>>("items").Returns((CacheEntry<List<CatalogItem>>)null);

            var logger = Substitute.For<ILogger<CachedCatalogItemServiceDecorator>>();
            var sut = new CachedCatalogItemServiceDecorator(storage, inner, logger);

            var result = await sut.List();

            Assert.Equal(1, handler.Calls);
            await storage.Received(1).SetItemAsync("items", Arg.Any<CacheEntry<List<CatalogItem>>>());
            Assert.Equal(2, result[0].Id);
        }

        [Fact]
        public async Task List_RemovesExpiredAndRefreshes()
        {
            var items = new List<CatalogItem> { new CatalogItem { Id = 3, Name = "C" } };
            var paged = new PagedCatalogItemResponse { CatalogItems = items };

            var handler = new TestHttpMessageHandler(_ => JsonResponse(paged));
            var inner = BuildCatalogItemService(handler);

            var storage = Substitute.For<ILocalStorageService>();
            var expired = new CacheEntry<List<CatalogItem>>(new List<CatalogItem>()) { DateCreated = DateTime.UtcNow.AddMinutes(-10) };
            storage.GetItemAsync<CacheEntry<List<CatalogItem>>>("items").Returns(expired);

            var logger = Substitute.For<ILogger<CachedCatalogItemServiceDecorator>>();
            var sut = new CachedCatalogItemServiceDecorator(storage, inner, logger);

            var result = await sut.List();

            await storage.Received(1).RemoveItemAsync("items");
            Assert.Equal(1, handler.Calls);
            Assert.Single(result);
            Assert.Equal(3, result[0].Id);
        }
    }
}
