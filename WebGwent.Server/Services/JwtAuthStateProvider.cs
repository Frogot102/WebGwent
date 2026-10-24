using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace WebGwent.Server.Services;

public sealed class JwtAuthStateProvider(TokenStore tokens) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await tokens.EnsureHydratedAsync();
        var token = await tokens.GetAsync();
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous();

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
                return Anonymous();

            var jwt = handler.ReadJwtToken(token);
            if (jwt.ValidTo < DateTime.UtcNow.AddMinutes(-1))
            {
                await tokens.SetAsync(null);
                return Anonymous();
            }

            var identity = new ClaimsIdentity(jwt.Claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            // Do not clear localStorage on parse glitches — would log the user out.
            return Anonymous();
        }
    }

    public void NotifyChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static AuthenticationState Anonymous() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}
