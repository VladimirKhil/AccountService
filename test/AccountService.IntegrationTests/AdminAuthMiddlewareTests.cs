using AccountService.Configuration;
using AccountService.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text;

namespace AccountService.IntegrationTests;

public sealed class AdminAuthMiddlewareTests
{
    [Fact]
    public async Task NonAdminPath_ShouldPassThrough()
    {
        var called = false;
        var middleware = new AdminAuthMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/account/me";

        await middleware.InvokeAsync(context, Options.Create(new AdminOptions { ClientSecret = "secret" }));

        called.Should().BeTrue();
    }

    [Fact]
    public async Task AdminPath_WithMissingAuth_ShouldReturnUnauthorized()
    {
        var middleware = new AdminAuthMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/admin/validate";

        await middleware.InvokeAsync(context, Options.Create(new AdminOptions { ClientSecret = "secret" }));

        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task AdminPath_WithValidBasicAuth_ShouldPassThrough()
    {
        var called = false;
        var middleware = new AdminAuthMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/admin/validate";

        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes("admin:secret"));
        context.Request.Headers.Authorization = $"Basic {token}";

        await middleware.InvokeAsync(context, Options.Create(new AdminOptions { ClientSecret = "secret" }));

        called.Should().BeTrue();
    }
}
