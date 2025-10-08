using System;
using BlazorAdmin.Services;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.BlazorAdmin.Services;

public class ToastServiceTests
{
    [Fact]
    public void ShowToast_RaisesOnShow()
    {
        var svc = new ToastService();
        string? message = null;
        ToastLevel? level = null;

        svc.OnShow += (m, l) => { message = m; level = l; };

        svc.ShowToast("hello", ToastLevel.Success);

        Assert.Equal("hello", message);
        Assert.Equal(ToastLevel.Success, level);
    }
}
