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
            var csp = string.Join("; ",
                "default-src 'self'",
                "img-src 'self' data:",
                "style-src 'self' 'unsafe-inline'",
                "script-src 'self'",
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
