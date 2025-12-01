using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Kiota.Abstractions.Authentication;

namespace OpenEoClientLib.Models;

public class ClientCredentialsAccessTokenProvider(string tokenUrl, string clientId, string clientSecret)
    : IAccessTokenProvider
{
    public AllowedHostsValidator AllowedHostsValidator { get; } = new();

    public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();

        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

        var form = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "scope", "openid" } 
        };

        request.Content = new FormUrlEncodedContent(form);

        var response = await client.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Token fetch failed: {response.StatusCode} - {errorContent}");
        }

        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        var json = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);

        return json.RootElement.TryGetProperty("access_token", out var tokenProp) ? tokenProp.GetString()! : throw new Exception("Response did not contain an access_token");
    }
}