using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebGwent.Tests;

public class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public ApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Root_And_Leaderboard_Are_Public()
    {
        var root = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, root.StatusCode);

        var lb = await _client.GetAsync("/api/account/leaderboard");
        Assert.Equal(HttpStatusCode.OK, lb.StatusCode);
        var rows = await lb.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(JsonValueKind.Array, rows.ValueKind);
    }

    [Fact]
    public async Task Anonymous_Decks_Are_Presets_Only()
    {
        var res = await _client.GetAsync("/api/decks");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var decks = await res.Content.ReadFromJsonAsync<List<DeckRow>>(Json) ?? [];
        Assert.NotEmpty(decks);
        Assert.All(decks, d => Assert.True(d.IsPreset));
    }

    [Fact]
    public async Task Register_Login_Save_Deck_Is_Owned()
    {
        var user = "t_" + Guid.NewGuid().ToString("N")[..8];
        var reg = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = user,
            password = "pass1234",
            passwordConfirm = "pass1234",
            displayName = "Tester"
        });
        Assert.Equal(HttpStatusCode.OK, reg.StatusCode);
        var auth = await reg.Content.ReadFromJsonAsync<AuthRow>(Json);
        Assert.False(string.IsNullOrEmpty(auth?.Token));

        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/decks");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);
        var list = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var decks = await list.Content.ReadFromJsonAsync<List<DeckRow>>(Json) ?? [];
        var preset = decks.First(d => d.IsPreset);

        using var save = new HttpRequestMessage(HttpMethod.Post, "/api/decks");
        save.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
        save.Content = JsonContent.Create(new
        {
            id = Guid.Empty,
            name = "Mine",
            faction = preset.Faction,
            leaderId = preset.LeaderId,
            cardIds = preset.CardIds
        });
        var saved = await _client.SendAsync(save);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
    }

    private sealed record AuthRow(string Token);
    private sealed record DeckRow(Guid Id, string Name, string Faction, string LeaderId, List<string> CardIds, bool IsPreset);
}
