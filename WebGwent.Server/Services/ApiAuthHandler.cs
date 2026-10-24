using System.Net.Http.Headers;

namespace WebGwent.Server.Services;

/// <summary>Unused backup — auth is applied in <see cref="WebGwentApiClient"/> from the circuit-scoped store.</summary>
public sealed class ApiAuthHandler(TokenStore tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokens.GetAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
