using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebGwent.Game.Models;
using WebGwent.Game.Services;
using WebGwent.Server.Accounts;

namespace WebGwent.Server.Services;

public sealed class WebGwentApiClient(HttpClient http, TokenStore tokens)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string BaseAddress => http.BaseAddress?.ToString()?.TrimEnd('/') ?? "";

    /// <summary>
    /// HttpClientFactory handlers are not circuit-scoped, so we attach the JWT here
    /// from the Blazor-scoped <see cref="TokenStore"/>.
    /// </summary>
    private async Task ApplyAuthAsync()
    {
        http.DefaultRequestHeaders.Authorization = null;
        var token = await tokens.GetAsync();
        if (!string.IsNullOrEmpty(token))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<(bool Ok, string? Error, AccountDto? Account)> LoginAsync(string username, string password)
    {
        http.DefaultRequestHeaders.Authorization = null;
        var res = await http.PostAsJsonAsync("/api/auth/login", new { username, password });
        if (!res.IsSuccessStatusCode)
            return (false, "bad", null);
        var body = await res.Content.ReadFromJsonAsync<AuthResponse>(Json);
        if (body is null || string.IsNullOrWhiteSpace(body.Token)) return (false, "bad", null);
        await tokens.SetAsync(body.Token);
        return (true, null, body.Account);
    }

    public async Task<(bool Ok, string? Error, AccountDto? Account)> RegisterAsync(
        string username, string password, string passwordConfirm, string displayName)
    {
        http.DefaultRequestHeaders.Authorization = null;
        var res = await http.PostAsJsonAsync("/api/auth/register",
            new { username, password, passwordConfirm, displayName });
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadFromJsonAsync<ErrorBody>(Json);
            return (false, err?.Error ?? "fail", null);
        }
        var body = await res.Content.ReadFromJsonAsync<AuthResponse>(Json);
        if (body is null || string.IsNullOrWhiteSpace(body.Token)) return (false, "fail", null);
        await tokens.SetAsync(body.Token);
        return (true, null, body.Account);
    }

    public async Task LogoutAsync() => await tokens.SetAsync(null);

    public async Task<AccountDto?> MeAsync()
    {
        try
        {
            await ApplyAuthAsync();
            var res = await http.GetAsync("/api/auth/me");
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<AccountDto>(Json);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<DeckDto>> GetDecksAsync()
    {
        await ApplyAuthAsync();
        return await http.GetFromJsonAsync<List<DeckDto>>("/api/decks", Json) ?? [];
    }

    public async Task<MatchBoardDto> CreateBotMatchAsync(
        Guid yourDeckId, Guid botDeckId, BotDifficulty difficulty, string? yourName, string? botName)
    {
        await ApplyAuthAsync();
        var res = await http.PostAsJsonAsync("/api/matches/bot",
            new { yourDeckId, botDeckId, difficulty, yourName, botName });
        if (res.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new InvalidOperationException("PlusRequired");
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<MatchBoardDto>(Json))!;
    }

    public async Task<MatchBoardDto> GetBotMatchAsync(Guid id)
    {
        await ApplyAuthAsync();
        return (await http.GetFromJsonAsync<MatchBoardDto>($"/api/matches/bot/{id}", Json))!;
    }

    public Task<MatchBoardDto> BotMulliganAsync(Guid id, Guid cardId) =>
        PostBoard($"/api/matches/bot/{id}/mulligan", new { cardId });

    public Task<MatchBoardDto> BotFinishMulliganAsync(Guid id) =>
        PostBoard($"/api/matches/bot/{id}/mulligan/finish", new { });

    public Task<MatchBoardDto> BotPlayAsync(Guid id, Guid cardId, CombatRow? row, Guid? targetId) =>
        PostBoard($"/api/matches/bot/{id}/play", new { cardId, row, targetId });

    public Task<MatchBoardDto> BotPassAsync(Guid id) =>
        PostBoard($"/api/matches/bot/{id}/pass", new { });

    public Task<MatchBoardDto> BotLeaderAsync(Guid id) =>
        PostBoard($"/api/matches/bot/{id}/leader", new { });

    public Task<MatchBoardDto> BotPendingAsync(Guid id, Guid? targetId, CombatRow? row) =>
        PostBoard($"/api/matches/bot/{id}/pending", new { targetId, row });

    public Task<MatchBoardDto> BotChooseStartAsync(Guid id, int startingPlayer) =>
        PostBoard($"/api/matches/bot/{id}/choose-start", new { startingPlayer });

    public Task<MatchBoardDto> BotContinueRoundAsync(Guid id) =>
        PostBoard($"/api/matches/bot/{id}/continue-round", new { });

    public async Task<BotStepResult> BotStepAsync(Guid id)
    {
        await ApplyAuthAsync();
        var res = await http.PostAsJsonAsync($"/api/matches/bot/{id}/bot-step", new { });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<BotStepResult>(Json))!;
    }

    public async Task<AccountDto> BuyPlusAsync(int months)
    {
        await ApplyAuthAsync();
        var res = await http.PostAsJsonAsync("/api/plus/buy", new { months });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<AccountDto>(Json))!;
    }

    public async Task<(int Days, AccountDto Account)> RedeemInviteAsync(string code)
    {
        await ApplyAuthAsync();
        var res = await http.PostAsJsonAsync("/api/plus/redeem", new { code });
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadFromJsonAsync<ErrorBody>(Json);
            throw new InvalidOperationException(err?.Error ?? "InviteInvalid");
        }
        var body = await res.Content.ReadFromJsonAsync<RedeemResponse>(Json);
        return (body!.Days, body.Account);
    }

    public async Task<List<InviteRecord>> ListInvitesAsync()
    {
        await ApplyAuthAsync();
        return await http.GetFromJsonAsync<List<InviteRecord>>("/api/plus/invites", Json) ?? [];
    }

    public async Task<InviteRecord> CreateInviteAsync(int durationDays, int maxActivations)
    {
        await ApplyAuthAsync();
        var res = await http.PostAsJsonAsync("/api/plus/invites", new { durationDays, maxActivations });
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadFromJsonAsync<ErrorBody>(Json);
            throw new InvalidOperationException(err?.Error ?? "InviteLimit");
        }
        return (await res.Content.ReadFromJsonAsync<InviteRecord>(Json))!;
    }

    public async Task DeleteInviteAsync(string id)
    {
        await ApplyAuthAsync();
        (await http.DeleteAsync($"/api/plus/invites/{id}")).EnsureSuccessStatusCode();
    }

    public async Task<List<MatchHistoryEntry>> HistoryAsync()
    {
        await ApplyAuthAsync();
        return await http.GetFromJsonAsync<List<MatchHistoryEntry>>("/api/account/history", Json) ?? [];
    }

    public async Task<int> FavoriteCountAsync()
    {
        await ApplyAuthAsync();
        var body = await http.GetFromJsonAsync<CountBody>("/api/account/favorite-count", Json);
        return body?.Count ?? 0;
    }

    public async Task<AccountDto> UpdateProfileAsync(string displayName, string avatarId)
    {
        await ApplyAuthAsync();
        var res = await http.PutAsJsonAsync("/api/account/profile", new { displayName, avatarId });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<AccountDto>(Json))!;
    }

    public async Task<AccountDto> UploadAvatarAsync(Stream stream, string fileName, string contentType)
    {
        await ApplyAuthAsync();
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);
        var res = await http.PostAsync("/api/account/avatar", content);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<AccountDto>(Json))!;
    }

    public async Task<SavedDeck?> SaveDeckAsync(SavedDeck deck)
    {
        await ApplyAuthAsync();
        var res = await http.PostAsJsonAsync("/api/decks", new
        {
            id = deck.Id,
            name = deck.Name,
            faction = deck.Faction,
            leaderId = deck.LeaderId,
            cardIds = deck.CardIds
        });
        res.EnsureSuccessStatusCode();
        var dto = await res.Content.ReadFromJsonAsync<DeckDto>(Json);
        return dto is null ? null : new SavedDeck
        {
            Id = dto.Id,
            Name = dto.Name,
            Faction = dto.Faction,
            LeaderId = dto.LeaderId,
            CardIds = dto.CardIds,
            IsPreset = dto.IsPreset
        };
    }

    public async Task DeleteDeckAsync(Guid id)
    {
        await ApplyAuthAsync();
        await http.DeleteAsync($"/api/decks/{id}");
    }

    private async Task<MatchBoardDto> PostBoard(string url, object body)
    {
        await ApplyAuthAsync();
        var res = await http.PostAsJsonAsync(url, body);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadFromJsonAsync<ErrorBody>(Json);
            throw new InvalidOperationException(err?.Error ?? res.ReasonPhrase ?? "Error");
        }
        return (await res.Content.ReadFromJsonAsync<MatchBoardDto>(Json))!;
    }

    private sealed record ErrorBody(string? Error);
    private sealed record AuthResponse(string Token, AccountDto Account);
    private sealed record RedeemResponse(int Days, AccountDto Account);
    private sealed record CountBody(int Count);
}

public record BotStepResult(bool Stepped, MatchBoardDto Board);

public record AccountDto(
    Guid Id,
    string Username,
    string DisplayName,
    string AvatarId,
    long AvatarVersion,
    AccountRole Role,
    bool HasPlus,
    DateTime? PlusUntilUtc,
    int Rating,
    Faction FavoriteFaction,
    int MatchesVsPlayers,
    int WinsVsPlayers,
    int LossesVsPlayers,
    int DrawsVsPlayers,
    int MatchesVsBot,
    int WinsVsBot,
    int LossesVsBot)
{
    public PlayerAccount ToAccount() => new()
    {
        Id = Id,
        Username = Username,
        DisplayName = DisplayName,
        AvatarId = AvatarId,
        AvatarVersion = AvatarVersion,
        Role = Role,
        PlusUntilUtc = PlusUntilUtc,
        Rating = Rating,
        FavoriteFaction = FavoriteFaction,
        MatchesVsPlayers = MatchesVsPlayers,
        WinsVsPlayers = WinsVsPlayers,
        LossesVsPlayers = LossesVsPlayers,
        DrawsVsPlayers = DrawsVsPlayers,
        MatchesVsBot = MatchesVsBot,
        WinsVsBot = WinsVsBot,
        LossesVsBot = LossesVsBot
    };
}

public record DeckDto(Guid Id, string Name, Faction Faction, string LeaderId, List<string> CardIds, bool IsPreset);
public record MatchBoardDto(MatchView View, BoardProfile You, BoardProfile Opponent, long Revision = 0);
