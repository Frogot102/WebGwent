using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
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

// Prefer env Jwt__Key / JWT_KEY in Docker; appsettings is the local default only.
var jwtKey = builder.Configuration["Jwt:Key"]
             ?? Environment.GetEnvironmentVariable("JWT_KEY")
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WebGwent API",
        Version = "v1",
        Description = "REST backend for WebGwent. Authorize with JWT from POST /api/auth/login (admin / admin123)."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste JWT only (Swagger adds Bearer prefix). Get token via /api/auth/login.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

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

if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "WebGwent API v1");
        options.DocumentTitle = "WebGwent API";
        options.DisplayRequestDuration();
    });
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    service = "WebGwent.Api",
    hubs = "/hubs/game",
    swagger = "/swagger"
}));
app.MapWebGwentApi();
app.MapHub<GameHub>("/hubs/game");

app.Run();

// Expose for WebApplicationFactory integration tests.
public partial class Program;
