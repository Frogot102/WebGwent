using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using WebGwent.Game.Services;
using WebGwent.Server.Accounts;
using WebGwent.Server.Components;
using WebGwent.Server.Hubs;
using WebGwent.Server.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "WebGwent.Auth";
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization(options =>
{
    // Everything requires login unless marked [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddSingleton<GameSessionManager>();
builder.Services.AddSingleton<AccountStore>();
builder.Services.AddSingleton<InviteStore>();
builder.Services.AddScoped<IdentityRevalidatingAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<IdentityRevalidatingAuthStateProvider>());
builder.Services.AddScoped<AccountAuthService>();
builder.Services.AddScoped<LocaleService>();
builder.Services.AddSignalR();

var app = builder.Build();

// Ensure seed admin exists (AccountStore ctor already does this; touch singleton early).
_ = app.Services.GetRequiredService<AccountStore>();
_ = app.Services.GetRequiredService<InviteStore>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Static CSS/JS + Blazor circuit must stay public for the login screen to work.
app.MapStaticAssets().AllowAnonymous();
app.MapAuthEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AllowAnonymous();

app.MapHub<GameHub>("/hubs/game");

app.Run();
