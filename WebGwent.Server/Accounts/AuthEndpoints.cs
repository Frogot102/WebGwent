using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace WebGwent.Server.Accounts;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", (Delegate)LoginAsync).AllowAnonymous().DisableAntiforgery();
        app.MapPost("/api/auth/register", (Delegate)RegisterAsync).AllowAnonymous().DisableAntiforgery();
        app.MapGet("/api/auth/logout", (Delegate)LogoutAsync).AllowAnonymous().DisableAntiforgery();
        app.MapPost("/api/auth/logout", (Delegate)LogoutAsync).AllowAnonymous().DisableAntiforgery();
        app.MapGet("/api/avatars/{userId:guid}", (Delegate)GetAvatarAsync).AllowAnonymous();
    }

    private static IResult GetAvatarAsync(Guid userId, AccountStore store)
    {
        var file = store.FindAvatarFile(userId);
        if (file is null)
            return Results.NotFound();
        return Results.File(file.Value.Path, file.Value.ContentType, enableRangeProcessing: true);
    }

    private static async Task<IResult> LoginAsync(HttpContext http, AccountStore store, [FromForm] string? username, [FromForm] string? password, [FromForm] string? returnUrl)
    {
        var acc = store.TryLogin(username ?? "", password ?? "");
        if (acc is null)
            return Results.Redirect("/account/login?error=bad");

        await SignInCookieAsync(http, acc);
        return Results.Redirect(SafeReturn(returnUrl) ?? "/home");
    }

    private static async Task<IResult> RegisterAsync(
        HttpContext http,
        AccountStore store,
        [FromForm] string? username,
        [FromForm] string? password,
        [FromForm] string? passwordConfirm,
        [FromForm] string? displayName)
    {
        if (!string.Equals(password, passwordConfirm, StringComparison.Ordinal))
            return Results.Redirect("/account/register?error=mismatch");

        try
        {
            var acc = store.Register(username ?? "", password ?? "", displayName ?? "");
            await SignInCookieAsync(http, acc);
            return Results.Redirect("/home");
        }
        catch (InvalidOperationException ex)
        {
            var code = ex.Message switch
            {
                "UsernameTaken" => "taken",
                "UsernameLength" => "ulen",
                "UsernameChars" => "uchars",
                "PasswordLength" => "plen",
                _ => "fail"
            };
            return Results.Redirect($"/account/register?error={code}");
        }
    }

    private static async Task<IResult> LogoutAsync(HttpContext http)
    {
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/account/login");
    }

    private static async Task SignInCookieAsync(HttpContext http, PlayerAccount account)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.Username),
            new(ClaimTypes.Role, account.Role.ToString()),
            new(AccountAuthService.DisplayNameClaim, account.DisplayName),
            new(AccountAuthService.AvatarClaim, account.AvatarId)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });
    }

    private static string? SafeReturn(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var path = raw.Trim();
        if (!path.StartsWith('/') || path.StartsWith("//", StringComparison.Ordinal))
            return null;
        if (path.StartsWith("/account/login", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/account/register", StringComparison.OrdinalIgnoreCase))
            return null;
        return path;
    }
}
