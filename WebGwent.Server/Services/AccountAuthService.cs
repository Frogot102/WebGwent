using WebGwent.Server.Accounts;

namespace WebGwent.Server.Services;

/// <summary>UI-facing auth helper backed by the WebGwent API JWT.</summary>
public sealed class AccountAuthService(WebGwentApiClient api, JwtAuthStateProvider authState, TokenStore tokens)
{
    public async Task<PlayerAccount?> GetCurrentAsync()
    {
        await tokens.EnsureHydratedAsync();
        var dto = await api.MeAsync();
        return dto?.ToAccount();
    }

    public async Task SignInWithPasswordAsync(string username, string password)
    {
        var (ok, _, _) = await api.LoginAsync(username, password);
        if (!ok) throw new InvalidOperationException("bad");
        authState.NotifyChanged();
    }

    public async Task RegisterAsync(string username, string password, string passwordConfirm, string displayName)
    {
        var (ok, error, _) = await api.RegisterAsync(username, password, passwordConfirm, displayName);
        if (!ok) throw new InvalidOperationException(error ?? "fail");
        authState.NotifyChanged();
    }

    public async Task SignOutAsync()
    {
        await api.LogoutAsync();
        authState.NotifyChanged();
    }
}
