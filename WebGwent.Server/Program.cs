using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
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
builder.Services.AddSingleton<CustomFactionRegistry>();
builder.Services.AddSingleton<DeckUiService>();
builder.Services.AddSingleton<SitePresenceService>();
builder.Services.AddScoped<CircuitHandler, SitePresenceCircuitHandler>();

var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5180";
builder.Services.AddHttpClient<WebGwentApiClient>((_, client) =>
{
    client.BaseAddress = new Uri(apiBase.TrimEnd('/') + "/");
});
builder.Services.AddHttpClient("WebGwent.Api", client =>
{
    client.BaseAddress = new Uri(apiBase.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(100);
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapGet("/api/site/online-count", (SitePresenceService presence) =>
    Results.Ok(new { count = presence.OnlineCount })).AllowAnonymous();

// Same-origin REST proxy: friend only needs the Server URL (ngrok / Radmin).
app.Map("/api/{**path}", async (HttpContext ctx, IHttpClientFactory httpFactory) =>
{
    var path = ctx.Request.Path.Value ?? "/api";
    if (path.StartsWith("/api/site/", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    var qs = ctx.Request.QueryString.HasValue ? ctx.Request.QueryString.Value : "";
    var http = httpFactory.CreateClient("WebGwent.Api");
    using var req = new HttpRequestMessage(new HttpMethod(ctx.Request.Method), path.TrimStart('/') + qs);
    if (ctx.Request.ContentLength > 0 || ctx.Request.Headers.ContainsKey("Content-Type"))
        req.Content = new StreamContent(ctx.Request.Body);
    foreach (var h in ctx.Request.Headers)
    {
        if (h.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;
        if (h.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
        if (!req.Headers.TryAddWithoutValidation(h.Key, h.Value.ToArray()) && req.Content is not null)
            req.Content.Headers.TryAddWithoutValidation(h.Key, h.Value.ToArray());
    }
    using var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ctx.RequestAborted);
    ctx.Response.StatusCode = (int)res.StatusCode;
    foreach (var h in res.Headers)
        ctx.Response.Headers[h.Key] = h.Value.ToArray();
    foreach (var h in res.Content.Headers)
        ctx.Response.Headers[h.Key] = h.Value.ToArray();
    ctx.Response.Headers.Remove("transfer-encoding");
    await res.Content.CopyToAsync(ctx.Response.Body, ctx.RequestAborted);
}).AllowAnonymous();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
