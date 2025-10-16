using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.eShopWeb.PublicApi.Middleware
{
    // PUBLIC_INTERFACE
    public class SecurityHeadersMiddleware
    {
        /** Adds security headers (CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy) to API responses. */
        private readonly RequestDelegate _next;
        public SecurityHeadersMiddleware(RequestDelegate next) { _next = next; }

        public async Task Invoke(HttpContext context)
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer-when-downgrade";
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            // Allow Swagger resources when ENABLE_SWAGGER is true (dev-only)
            var enableSwagger = string.Equals(Environment.GetEnvironmentVariable("ENABLE_SWAGGER"), "true", StringComparison.OrdinalIgnoreCase);
            var scriptSrc = enableSwagger ? "script-src 'self' 'unsafe-inline' 'unsafe-eval'" : "script-src 'self'";
            var styleSrc = "style-src 'self' 'unsafe-inline'";
            var imgSrc = enableSwagger ? "img-src 'self' data: blob:" : "img-src 'self' data:";
            var connectSrc = enableSwagger ? "connect-src 'self' ws: wss:" : "connect-src 'self'";
            var csp = string.Join("; ",
                "default-src 'self'",
                imgSrc,
                styleSrc,
                scriptSrc,
                connectSrc,
                "object-src 'none'",
                "frame-ancestors 'none'"
            );
            headers["Content-Security-Policy"] = new StringValues(csp);

            await _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        // PUBLIC_INTERFACE
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            /** Adds the SecurityHeadersMiddleware to the API pipeline. */
            return app.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
