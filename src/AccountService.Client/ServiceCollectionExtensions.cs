using AccountService.Contract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace AccountService.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccountServiceClient(this IServiceCollection services, IConfiguration configuration)
    {
        var optionsSection = configuration.GetSection(AccountServiceClientOptions.ConfigurationSectionName);
        services.Configure<AccountServiceClientOptions>(optionsSection);

        var options = optionsSection.Get<AccountServiceClientOptions>();

        if (options?.ServiceUri != null)
        {
            services.AddHttpClient<IAccountServiceClient, AccountServiceClient>(
                client =>
                {
                    client.BaseAddress = new Uri(options.ServiceUri, "api/v1/");
                    client.Timeout = options.Timeout;
                    client.DefaultRequestVersion = HttpVersion.Version20;

                    SetAuthSecret(options, client);
                });
        }

        return services;
    }

    private static void SetAuthSecret(AccountServiceClientOptions options, HttpClient client)
    {
        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            return;
        }

        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"admin:{options.ClientSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
    }
}
