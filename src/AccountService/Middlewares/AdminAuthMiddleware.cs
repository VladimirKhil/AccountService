using AccountService.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

namespace AccountService.Middlewares;

public sealed class AdminAuthMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IOptions<AdminOptions> options)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1/admin", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var expectedSecret = options.Value.ClientSecret;

        if (string.IsNullOrWhiteSpace(expectedSecret))
        {
            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            await context.Response.WriteAsync("Admin secret is not configured");
            return;
        }

        if (!context.Request.Headers.TryGetValue("Authorization", out var headerValue))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }

        var auth = headerValue.ToString();

        if (!auth.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }

        var encoded = auth[6..].Trim();

        try
        {
            var raw = Encoding.ASCII.GetString(Convert.FromBase64String(encoded));

            if (!string.Equals(raw, $"admin:{expectedSecret}", StringComparison.Ordinal))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }
        }
        catch
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }

        await next(context);
    }
}
