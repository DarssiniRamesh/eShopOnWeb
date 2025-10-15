using BlazorAdmin.Services;
using BlazorShared;
using BlazorShared.Interfaces;
using BlazorShared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BlazorAdmin.UnitTests.Setup;

/// <summary>
/// Factory methods to create a configured bUnit TestContext for BlazorAdmin tests.
/// Sets JSInterop to loose mode and registers basic service fakes to satisfy DI.
/// </summary>
public static class TestContextFactory
{
    // PUBLIC_INTERFACE
    public static TestContext Create()
    {
        var ctx = new TestContext();

        // JS interop: allow unknown calls to be ignored by default
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var services = ctx.Services;
        services.AddLogging();
        services.AddOptions();

        // Basic config for HttpService consumers
        services.AddSingleton<IOptions<BaseUrlConfiguration>>(
            Options.Create(new BaseUrlConfiguration { ApiBase = "http://localhost/" }));

        // Toast service singleton (no-op in tests)
        services.AddSingleton<ToastService>();

        // Fakes for Lookup and Catalog services used by components
        services.AddSingleton<ICatalogLookupDataService<CatalogBrand>, FakeBrandLookupService>();
        services.AddSingleton<ICatalogLookupDataService<CatalogType>, FakeTypeLookupService>();
        services.AddSingleton<ICatalogItemService, FakeCatalogItemService>();

        return ctx;
    }

    private sealed class FakeBrandLookupService : ICatalogLookupDataService<CatalogBrand>
    {
        public Task<List<CatalogBrand>> List() => Task.FromResult(new List<CatalogBrand>());
    }

    private sealed class FakeTypeLookupService : ICatalogLookupDataService<CatalogType>
    {
        public Task<List<CatalogType>> List() => Task.FromResult(new List<CatalogType>());
    }

    private sealed class FakeCatalogItemService : ICatalogItemService
    {
        public Task<CatalogItem> Create(CreateCatalogItemRequest catalogItem) =>
            Task.FromResult<CatalogItem>(null);

        public Task<string> Delete(int catalogItemId) =>
            Task.FromResult("OK");

        public Task<CatalogItem> Edit(CatalogItem catalogItem) =>
            Task.FromResult(catalogItem);

        public Task<CatalogItem> GetById(int id) =>
            Task.FromResult<CatalogItem>(null);

        public Task<List<CatalogItem>> List() =>
            Task.FromResult(new List<CatalogItem>());

        public Task<List<CatalogItem>> ListPaged(int pageSize) =>
            Task.FromResult(new List<CatalogItem>());
    }
}
