using System.Text;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace OpenEoClientLib.Models;

public class BasicAuthenticationProvider : IAuthenticationProvider
{
    private readonly string _authorizationHeader;

    public BasicAuthenticationProvider(string username, string password)
    {
        var parameter = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        _authorizationHeader = $"Basic {parameter}";
    }

    public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        if(!request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", _authorizationHeader);
        }
        return Task.CompletedTask;
    }
}