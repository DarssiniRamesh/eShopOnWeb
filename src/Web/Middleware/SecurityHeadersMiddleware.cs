using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;

namespace Microsoft.eShopWeb.Web.Middleware
{
    // PUBLIC_INTERFACE
    public class SecurityHeadersMiddleware
    {
        /** Middleware adding common security headers including CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, and Permissions-Policy. */
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var headers = context.Response.Headers;

            // X-Content-Type-Options
            headers["X-Content-Type-Options"] = "nosniff";

            // X-Frame-Options - deny all framing
            headers["X-Frame-Options"] = "DENY";

            // Referrer-Policy
            headers["Referrer-Policy"] = "no-referrer-when-downgrade";

            // Permissions-Policy - minimal
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            // Content-Security-Policy
            // Note: keep 'unsafe-inline' off for scripts; styles allow inline for legacy bootstrap
            // Allow Swagger resources when ENABLE_SWAGGER is true (dev/Docker only)
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
            /** Adds the SecurityHeadersMiddleware to the pipeline. */
            return app.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
