using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using WebGwent.Server.Components;
using WebGwent.Server.Localization;
using WebGwent.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        // ngrok + large GameBoard diffs: default 30s / 32KB is too tight for remote friends
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(120);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.MaximumReceiveMessageSize = 2 * 1024 * 1024;
    });

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // ngrok / reverse proxies terminate TLS and forward HTTP locally
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddScoped<AccountAuthService>();
builder.Services.AddScoped<LocaleService>();
builder.Services.AddSingleton(_ => WebGwent.Game.Catalog.CardCatalog.LoadEmbedded());
builder.Services.AddSingleton<DeckUiService>();
// Auth header is applied inside WebGwentApiClient from the circuit-scoped TokenStore.
// Do not use AddHttpMessageHandler with TokenStore — HttpClientFactory caches handlers
// outside the Blazor circuit scope, so the JWT would be missing after login.
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5180";
builder.Services.AddHttpClient<WebGwentApiClient>((_, client) =>
{
    client.BaseAddress = new Uri(apiBase.TrimEnd('/') + "/");
});
builder.Services.AddHttpClient("WebGwent.Api", client =>
{
    client.BaseAddress = new Uri(apiBase.TrimEnd('/') + "/");
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// Don't force HTTPS locally — ngrok already serves https externally and forwards http.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAntiforgery();

// Same-origin avatar proxy so remote/ngrok browsers don't hit localhost:5180
app.MapGet("/api/avatars/{userId:guid}", async (Guid userId, IHttpClientFactory httpFactory, HttpRequest req, CancellationToken ct) =>
{
    var http = httpFactory.CreateClient("WebGwent.Api");
    var qs = req.QueryString.HasValue ? req.QueryString.Value : "";
    using var res = await http.GetAsync($"api/avatars/{userId}{qs}", ct);
    if (!res.IsSuccessStatusCode)
        return Results.NotFound();
    var bytes = await res.Content.ReadAsByteArrayAsync(ct);
    var contentType = res.Content.Headers.ContentType?.ToString() ?? "image/png";
    return Results.File(bytes, contentType);
}).AllowAnonymous();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
