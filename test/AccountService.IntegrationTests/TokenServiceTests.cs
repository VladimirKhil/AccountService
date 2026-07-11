using AccountService.Configuration;
using AccountService.Services;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AccountService.IntegrationTests;

public sealed class TokenServiceTests
{
    private static TokenService CreateDevelopmentTokenService(JwtOptions options)
        => new(Options.Create(options), new TestHostEnvironment());

    [Fact]
    public void IssueAndValidate_ShouldReturnValidPrincipal()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpiresMinutes = 30,
        });

        var service = CreateDevelopmentTokenService(options.Value);
        var userId = Guid.NewGuid();
        var jwtId = Guid.NewGuid().ToString("N");

        var issued = service.Issue(userId, "alice", jwtId);
        var result = service.Validate(issued.Token);

        result.IsValid.Should().BeTrue();
        result.Principal.Should().NotBeNull();
        result.JwtId.Should().Be(jwtId);
        result.Principal!.Identity!.Name.Should().Be("alice");
    }

    [Fact]
    public void Validate_WithWrongAudience_ShouldBeInvalid()
    {
        var issuer = "same-issuer";

        var tokenService = CreateDevelopmentTokenService(new JwtOptions
        {
            Issuer = issuer,
            Audience = "aud-a",
            ExpiresMinutes = 30,
        });

        var token = tokenService.Issue(Guid.NewGuid(), "bob", Guid.NewGuid().ToString("N")).Token;

        var validator = CreateDevelopmentTokenService(new JwtOptions
        {
            Issuer = issuer,
            Audience = "aud-b",
            ExpiresMinutes = 30,
        });

        var result = validator.Validate(token);
        result.IsValid.Should().BeFalse();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "AccountService.IntegrationTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
