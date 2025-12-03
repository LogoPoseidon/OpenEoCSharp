using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Kiota.Abstractions.Authentication;

namespace OpenEoClientLib.Models;

public class ClientCredentialsAccessTokenProvider(
    string tokenUrl,
    string clientId,
    string clientSecret,
    string providerId,
    string scope = "openid")
    : IAccessTokenProvider
{
    private static readonly HttpClient HttpClient = new();
    
    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    private readonly SemaphoreSlim _lock = new(1, 1);

    public AllowedHostsValidator AllowedHostsValidator { get; } = new()
    {
        AllowedHosts = []
    };

    public async Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? ctx = null,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken != null && DateTimeOffset.UtcNow < _expiresAt)
                return $"oidc/{providerId}/{_cachedToken}";

            await RefreshTokenAsync(cancellationToken);
            return $"oidc/{providerId}/{_cachedToken}";
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task RefreshTokenAsync(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

        var authString = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "scope", scope }
        });

        var response = await HttpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Token fetch failed: {response.StatusCode} - {responseText}");

        var json = JsonDocument.Parse(responseText);

        if (!json.RootElement.TryGetProperty("access_token", out var tokenProp))
            throw new Exception("Token endpoint did not return access_token");

        _cachedToken = tokenProp.GetString();

        if (json.RootElement.TryGetProperty("expires_in", out var expiresProp))
        {
            var expiresIn = expiresProp.GetInt32();
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 30);
        }
        else
        {
            _expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        }
    }
}
