using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace OpenEoClientLib.Models;

public class BearerAuthenticationProvider(string token) : IAuthenticationProvider
{
    private readonly string _authorizationHeader = $"Bearer {token}";
    public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        if (!request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", _authorizationHeader);
        }
        return Task.CompletedTask;
    }
}