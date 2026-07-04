using AccountService.Configuration;
using AccountService.Contracts;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AccountService.Services;

public sealed class SteamAuthorizationService(
    HttpClient httpClient,
    IOptions<SteamOptions> options,
    ILogger<SteamAuthorizationService> logger) : ISteamAuthorizationService
{
    private readonly SteamOptions _options = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<SteamAuthorizationResult> AuthorizeAsync(string authTicket, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || _options.AppId == 0)
        {
            logger.LogError("Steam authorization is not configured");
            return new SteamAuthorizationResult(false, Error: "Steam authorization is not configured", IsServiceError: true);
        }

        if (string.IsNullOrWhiteSpace(authTicket))
        {
            return new SteamAuthorizationResult(false, Error: "Steam auth ticket is empty");
        }

        try
        {
            var authResult = await AuthenticateTicketAsync(authTicket, cancellationToken);

            if (!authResult.IsSuccess)
            {
                return authResult;
            }

            var profile = await GetProfileAsync(authResult.SteamId!, cancellationToken);

            if (profile == null || string.IsNullOrWhiteSpace(profile.PersonaName))
            {
                return new SteamAuthorizationResult(false, Error: "Steam profile was not found", IsServiceError: true);
            }

            var avatarBytes = await LoadAvatarAsync(profile.AvatarFull, cancellationToken);

            return new SteamAuthorizationResult(
                true,
                authResult.SteamId,
                profile.PersonaName,
                avatarBytes);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Steam authorization request failed");
            return new SteamAuthorizationResult(false, Error: ex.Message, IsServiceError: true);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Steam authorization request timed out");
            return new SteamAuthorizationResult(false, Error: ex.Message, IsServiceError: true);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Steam authorization response parsing failed");
            return new SteamAuthorizationResult(false, Error: ex.Message, IsServiceError: true);
        }
    }

    private async Task<SteamAuthorizationResult> AuthenticateTicketAsync(string authTicket, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"ISteamUserAuth/AuthenticateUserTicket/v1/?key={Uri.EscapeDataString(_options.ApiKey)}&appid={_options.AppId}&ticket={Uri.EscapeDataString(authTicket)}&identity={Uri.EscapeDataString(_options.Identity)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException($"Steam authentication request failed with status code {(int)response.StatusCode}");
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<AuthenticateUserTicketPayload>(stream, JsonOptions, cancellationToken);

        if (payload?.Response?.Error != null)
        {
            var error = payload.Response.Error;
            var message = $"Steam authentication failed with error {error.ErrorCode}: {error.ErrorDescription}";
            logger.LogWarning("{Message}", message);

            return new SteamAuthorizationResult(false, Error: message);
        }

        if (payload?.Response?.Params?.Result == "OK" && !string.IsNullOrWhiteSpace(payload.Response.Params.SteamId))
        {
            return new SteamAuthorizationResult(true, payload.Response.Params.SteamId);
        }

        return new SteamAuthorizationResult(false, Error: "Steam ticket is invalid");
    }

    private async Task<SteamPlayer?> GetProfileAsync(string steamId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"ISteamUser/GetPlayerSummaries/v2/?key={Uri.EscapeDataString(_options.ApiKey)}&steamids={Uri.EscapeDataString(steamId)}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<GetPlayerSummariesPayload>(stream, JsonOptions, cancellationToken);

        return payload?.Response?.Players?.FirstOrDefault();
    }

    private async Task<byte[]?> LoadAvatarAsync(string? avatarUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            return null;
        }

        try
        {
            return await httpClient.GetByteArrayAsync(avatarUrl, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Failed to load Steam avatar");
            return null;
        }
    }

    private sealed class AuthenticateUserTicketPayload
    {
        [JsonPropertyName("response")]
        public AuthenticateUserTicketResponse? Response { get; set; }
    }

    private sealed class AuthenticateUserTicketResponse
    {
        [JsonPropertyName("params")]
        public AuthenticateUserTicketParams? Params { get; set; }

        [JsonPropertyName("error")]
        public SteamError? Error { get; set; }
    }

    private sealed class AuthenticateUserTicketParams
    {
        public string? Result { get; set; }

        [JsonPropertyName("steamid")]
        public string? SteamId { get; set; }
    }

    private sealed class SteamError
    {
        [JsonPropertyName("errorcode")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("errordesc")]
        public string? ErrorDescription { get; set; }
    }

    private sealed class GetPlayerSummariesPayload
    {
        [JsonPropertyName("response")]
        public GetPlayerSummariesResponse? Response { get; set; }
    }

    private sealed class GetPlayerSummariesResponse
    {
        [JsonPropertyName("players")]
        public SteamPlayer[]? Players { get; set; }
    }

    private sealed class SteamPlayer
    {
        [JsonPropertyName("personaname")]
        public string? PersonaName { get; set; }

        [JsonPropertyName("avatarfull")]
        public string? AvatarFull { get; set; }
    }
}
