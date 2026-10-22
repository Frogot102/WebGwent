using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

namespace WebGwent.Server.Accounts;

public sealed class AccountAuthService
{
    public const string DisplayNameClaim = "display_name";
    public const string AvatarClaim = "avatar";

    private readonly AccountStore _store;
    private readonly IHttpContextAccessor _http;
    private readonly AuthenticationStateProvider _authState;

    public AccountAuthService(
        AccountStore store,
        IHttpContextAccessor http,
        AuthenticationStateProvider authState)
    {
        _store = store;
        _http = http;
        _authState = authState;
    }

    public async Task<PlayerAccount?> GetCurrentAsync()
    {
        var state = await _authState.GetAuthenticationStateAsync();
        var id = state.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var guid))
            return null;
        return _store.FindById(guid);
    }

    public async Task SignInAsync(PlayerAccount account)
    {
        var http = _http.HttpContext
                   ?? throw new InvalidOperationException("No HttpContext for sign-in.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.Username),
            new(ClaimTypes.Role, account.Role.ToString()),
            new(DisplayNameClaim, account.DisplayName),
            new(AvatarClaim, account.AvatarId)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });

        if (_authState is IdentityRevalidatingAuthStateProvider revalidating)
            revalidating.NotifyChanged();
    }

    public async Task SignOutAsync()
    {
        var http = _http.HttpContext;
        if (http is not null)
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (_authState is IdentityRevalidatingAuthStateProvider revalidating)
            revalidating.NotifyChanged();
    }
}

/// <summary>
/// Simple cascading auth state that can be refreshed after cookie sign-in/out.
/// </summary>
public sealed class IdentityRevalidatingAuthStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _http;

    public IdentityRevalidatingAuthStateProvider(IHttpContextAccessor http) => _http = http;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _http.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        return Task.FromResult(new AuthenticationState(user));
    }

    public void NotifyChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
