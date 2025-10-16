using System;
using System.IO;
using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.eShopWeb.Web.Security
{
    public static class DataProtectionExtensions
    {
        // PUBLIC_INTERFACE
        public static IServiceCollection AddConfiguredDataProtection(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
        {
            /** Configures ASP.NET Core Data Protection to persist keys to file system and optionally encrypt with Azure Key Vault. */
            var dp = services.AddDataProtection().SetApplicationName("eShopOnWeb");

            var dpKeysPath = configuration["DP_KEYS_PATH"];
            if (!string.IsNullOrWhiteSpace(dpKeysPath))
            {
                Directory.CreateDirectory(dpKeysPath);
                dp.PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));
            }
            else if (!isDevelopment)
            {
                var defaultPath = Path.Combine(AppContext.BaseDirectory, "dpkeys");
                Directory.CreateDirectory(defaultPath);
                dp.PersistKeysToFileSystem(new DirectoryInfo(defaultPath));
            }

            var kvKeyIdentifier = configuration["KeyVault:KeyIdentifier"];
            if (!string.IsNullOrWhiteSpace(kvKeyIdentifier))
            {
                dp.ProtectKeysWithAzureKeyVault(new Uri(kvKeyIdentifier), new DefaultAzureCredential());
            }

            return services;
        }
    }
}
