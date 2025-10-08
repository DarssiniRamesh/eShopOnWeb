using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PublicApiIntegrationTests.HealthChecks;

[TestClass]
public class HealthEndpointsTest
{
    [TestMethod]
    public async Task HealthEndpointRespondsOk()
    {
        var client = ProgramTest.NewClient;

        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }
}
