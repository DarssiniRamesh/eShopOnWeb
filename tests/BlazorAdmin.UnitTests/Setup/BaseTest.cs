using System;
using Bunit;

namespace BlazorAdmin.UnitTests.Setup;

/// <summary>
/// Base class for bUnit tests that ensures a TestContext is created and disposed.
/// </summary>
public abstract class BaseTest : IDisposable
{
    protected TestContext Ctx { get; }

    protected BaseTest()
    {
        Ctx = TestContextFactory.Create();
    }

    public void Dispose()
    {
        Ctx.Dispose();
        GC.SuppressFinalize(this);
    }
}
