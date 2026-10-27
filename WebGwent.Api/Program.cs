using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WebGwent.Api.Accounts;
using WebGwent.Api.Endpoints;
using WebGwent.Api.Hubs;
using WebGwent.Api.Services;
using WebGwent.Game.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

var jwtKey = builder.Configuration["Jwt:Key"]
             ?? "WebGwent-Dev-Signing-Key-Change-Me-32chars!";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "WebGwent",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "WebGwent",
            IssuerSigningKey = signingKey
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // SignalR sends the JWT as ?access_token=
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(
            builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
            ?? ["https://localhost:7191", "http://localhost:5097", "https://localhost:5097"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services.AddSingleton(_ => WebGwent.Game.Catalog.CardCatalog.LoadEmbedded());
builder.Services.AddSingleton(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var path = Path.Combine(env.ContentRootPath, "App_Data", "custom-content.json");
    return new CustomContentStore(sp.GetRequiredService<WebGwent.Game.Catalog.CardCatalog>(), path);
});
builder.Services.AddSingleton(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var decksPath = Path.Combine(env.ContentRootPath, "App_Data", "custom-decks.json");
    return new GameSessionManager(decksPath, sp.GetRequiredService<WebGwent.Game.Catalog.CardCatalog>());
});
builder.Services.AddSingleton<AccountStore>();
builder.Services.AddSingleton<InviteStore>();
builder.Services.AddSingleton<InviteRequestStore>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<BotMatchService>();
builder.Services.AddHostedService<RoomWatchService>();
builder.Services.AddSignalR(o =>
{
    o.ClientTimeoutInterval = TimeSpan.FromSeconds(120);
    o.HandshakeTimeout = TimeSpan.FromSeconds(30);
    o.KeepAliveInterval = TimeSpan.FromSeconds(15);
    o.MaximumReceiveMessageSize = 2 * 1024 * 1024;
}).AddJsonProtocol(o =>
{
    o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

_ = app.Services.GetRequiredService<CustomContentStore>();
_ = app.Services.GetRequiredService<AccountStore>();
_ = app.Services.GetRequiredService<InviteStore>();
_ = app.Services.GetRequiredService<InviteRequestStore>();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "WebGwent.Api", hubs = "/hubs/game" }));
app.MapWebGwentApi();
app.MapHub<GameHub>("/hubs/game");

app.Run();
