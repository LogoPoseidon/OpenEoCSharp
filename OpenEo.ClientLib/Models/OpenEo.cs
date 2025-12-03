using System.Text.Json;
using Client.Generated;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace OpenEoClientLib.Models;

public static class OpenEo
{
    public static string ClientVersion => "1.0.0";


    public static async Task<Connection> Connect(string url)
    {
        var finalBaseUrl = await GetFinalUrl(url);
        var newClient = new OpenEoClient(new HttpClientRequestAdapter(new AnonymousAuthenticationProvider())
            { BaseUrl = finalBaseUrl });
        return new Connection(newClient);
    }

    public static async Task<Connection> Connect(string url, string clientId, string clientSecret)
    {
        var finalBaseUrl = await GetFinalUrl(url);

        var (issuerUrl, providerId) = await GetOidcProviderDetails(finalBaseUrl);

        var tokenEndpoint = await GetTokenEndpointFromIssuer(issuerUrl);
        
        var tokenProvider = new ClientCredentialsAccessTokenProvider(
            tokenUrl: tokenEndpoint,
            clientId: clientId,
            clientSecret: clientSecret,
            providerId: providerId
        );

        if (Uri.TryCreate(finalBaseUrl, UriKind.Absolute, out var apiUri))
        {
            tokenProvider.AllowedHostsValidator.AllowedHosts = [];
        }

        var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);
        
        var adapter = new HttpClientRequestAdapter(authProvider)
        {
            BaseUrl = finalBaseUrl
        };
        
        var client = new OpenEoClient(adapter);
        return new Connection(client);
    }

    private static async Task<string> GetFinalUrl(string url)
    {
        var anonymousAuth = new AnonymousAuthenticationProvider();
        var discoveryAdapter = new HttpClientRequestAdapter(anonymousAuth) { BaseUrl = url };
        var discoveryClient = new OpenEoClient(discoveryAdapter);

        var finalBaseUrl = url;
        try
        {
            var wellKnown = await discoveryClient.WellKnown.Openeo.GetAsOpeneoGetResponseAsync();
            var newLink = wellKnown?.Versions?
                .Where(v => v.ApiVersion != null && v.ApiVersion.StartsWith('1'))
                .OrderByDescending(v => v.ApiVersion)
                .FirstOrDefault();

            if (newLink?.Url != null)
            {
                finalBaseUrl = newLink.Url;
            }
        }
        catch
        {
            // ignore
        }

        return finalBaseUrl;
    }

    private static async Task<(string IssuerUrl, string ProviderId)> GetOidcProviderDetails(string apiBaseUrl)
    {
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider()) { BaseUrl = apiBaseUrl };
        var client = new OpenEoClient(adapter);

        var oidcResponse = await client.Credentials.Oidc.GetAsOidcGetResponseAsync();

        if (oidcResponse?.Providers == null || oidcResponse.Providers.Count == 0)
        {
            throw new Exception("No OIDC providers advertised by this OpenEO backend.");
        }
        var provider = oidcResponse.Providers.First();

        if (string.IsNullOrEmpty(provider.Issuer) || string.IsNullOrEmpty(provider.Id))
            throw new Exception("OIDC Provider found, but Issuer URL or Provider ID is missing.");

        return (provider.Issuer, provider.Id);
    }

    private static async Task<string> GetTokenEndpointFromIssuer(string issuerUrl)
    {
        using var http = new HttpClient();
        var configUrl = issuerUrl.TrimEnd('/') + "/.well-known/openid-configuration";

        try
        {
            var json = await http.GetStringAsync(configUrl);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("token_endpoint", out var endpoint))
            {
                return endpoint.GetString()!;
            }

            throw new Exception("token_endpoint not found in OIDC config");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to discover token endpoint from {configUrl}: {ex.Message}");
        }
    }
}