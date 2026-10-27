using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using WebGwent.Api.Accounts;
using WebGwent.Api.Services;
using WebGwent.Game.Catalog;
using WebGwent.Game.Models;
using WebGwent.Game.Services;

namespace WebGwent.Api.Endpoints;

public static class ApiEndpoints
{
    public static void MapWebGwentApi(this WebApplication app)
    {
        app.MapPost("/api/auth/login", Login).AllowAnonymous();
        app.MapPost("/api/auth/register", Register).AllowAnonymous();
        app.MapGet("/api/auth/me", Me).RequireAuthorization();
        app.MapGet("/api/avatars/{userId:guid}", GetAvatar).AllowAnonymous();

        app.MapGet("/api/decks", ListDecks).AllowAnonymous();
        app.MapGet("/api/decks/{id:guid}", GetDeck).AllowAnonymous();
        app.MapPost("/api/decks", SaveDeck).RequireAuthorization();
        app.MapDelete("/api/decks/{id:guid}", DeleteDeck).RequireAuthorization();

        app.MapPost("/api/matches/bot", CreateBotMatch).AllowAnonymous();
        app.MapGet("/api/matches/bot/{id:guid}", GetBotMatch).AllowAnonymous();
        app.MapPost("/api/matches/bot/{id:guid}/mulligan", BotMulligan).AllowAnonymous();
        app.MapPost("/api/matches/bot/{id:guid}/mulligan/finish", BotFinishMulligan).AllowAnonymous();
        app.MapPost("/api/matches/bot/{id:guid}/play", BotPlay).AllowAnonymous();
        app.MapPost("/api/matches/bot/{id:guid}/pass", BotPass).AllowAnonymous();
        app.MapPost("/api/matches/bot/{id:guid}/leader", BotLeader).AllowAnonymous();
        app.MapPost("/api/matches/bot/{id:guid}/pending", BotPending).AllowAnonymous();
        app.MapPost("/api/matches/bot/{id:guid}/choose-start", BotChooseStart).AllowAnonymous();
        app.MapPost("/api/matches/bot/{id:guid}/continue-round", BotContinueRound).AllowAnonymous();
        app.MapPost("/api/matches/bot/{id:guid}/bot-step", BotStep).AllowAnonymous();

        // Real card payment is not wired — UI goes to a checkout stub.
        app.MapPost("/api/plus/buy", () => Results.StatusCode(StatusCodes.Status410Gone)).RequireAuthorization();
        app.MapPost("/api/plus/redeem", RedeemInvite).RequireAuthorization();
        app.MapGet("/api/plus/invites", ListInvites).RequireAuthorization();
        app.MapPost("/api/plus/invites", CreateInvite).RequireAuthorization();
        app.MapDelete("/api/plus/invites/{id}", DeleteInvite).RequireAuthorization();

        app.MapGet("/api/plus/invite-requests/mine", MyInviteRequest).RequireAuthorization();
        app.MapPost("/api/plus/invite-requests", CreateInviteRequest).RequireAuthorization();
        app.MapGet("/api/plus/invite-requests", ListInviteRequests).RequireAuthorization();
        app.MapPost("/api/plus/invite-requests/{id}/approve", ApproveInviteRequest).RequireAuthorization();
        app.MapPost("/api/plus/invite-requests/{id}/reject", RejectInviteRequest).RequireAuthorization();

        app.MapGet("/api/account/history", History).RequireAuthorization();
        app.MapGet("/api/account/favorite-count", FavoriteCount).RequireAuthorization();
        app.MapPut("/api/account/profile", UpdateProfile).RequireAuthorization();
        app.MapPost("/api/account/avatar", UploadAvatar).RequireAuthorization().DisableAntiforgery();

        app.MapGet("/api/catalog/custom", GetCustomContent).AllowAnonymous();
        app.MapPut("/api/catalog/custom", PutCustomContent).RequireAuthorization();
    }

    private static Guid? TryUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static Guid RequireUserId(ClaimsPrincipal user) =>
        TryUserId(user) ?? throw new UnauthorizedAccessException("Unauthorized");

    private static IResult GetAvatar(Guid userId, AccountStore store)
    {
        var file = store.FindAvatarFile(userId);
        return file is null
            ? Results.NotFound()
            : Results.File(file.Value.Path, file.Value.ContentType, enableRangeProcessing: true);
    }

    private static IResult Login(LoginRequest req, AccountStore store, JwtTokenService jwt)
    {
        var acc = store.TryLogin(req.Username ?? "", req.Password ?? "");
        if (acc is null)
            return Results.Unauthorized();
        return Results.Ok(new AuthResponse(jwt.CreateToken(acc), AccountDto.From(acc)));
    }

    private static IResult Register(RegisterRequest req, AccountStore store, JwtTokenService jwt)
    {
        if (!string.Equals(req.Password, req.PasswordConfirm, StringComparison.Ordinal))
            return Results.BadRequest(new { error = "mismatch" });
        try
        {
            var acc = store.Register(req.Username ?? "", req.Password ?? "", req.DisplayName ?? "");
            return Results.Ok(new AuthResponse(jwt.CreateToken(acc), AccountDto.From(acc)));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static IResult Me(ClaimsPrincipal user, AccountStore store)
    {
        var id = RequireUserId(user);
        var acc = store.FindById(id);
        return acc is null ? Results.Unauthorized() : Results.Ok(AccountDto.From(acc));
    }

    private static IResult ListDecks(GameSessionManager sessions) =>
        Results.Ok(sessions.Decks.GetAll().Select(DeckDto.From));

    private static IResult GetDeck(Guid id, GameSessionManager sessions)
    {
        var deck = sessions.Decks.Get(id);
        return deck is null ? Results.NotFound() : Results.Ok(DeckDto.From(deck));
    }

    private static IResult SaveDeck(SavedDeckDto body, GameSessionManager sessions)
    {
        var saved = new SavedDeck
        {
            Id = body.Id == Guid.Empty ? Guid.NewGuid() : body.Id,
            Name = body.Name,
            Faction = body.Faction,
            LeaderId = body.LeaderId,
            CardIds = body.CardIds.ToList(),
            IsPreset = false
        };
        sessions.Decks.Save(saved);
        return Results.Ok(DeckDto.From(saved));
    }

    private static IResult DeleteDeck(Guid id, GameSessionManager sessions)
    {
        sessions.Decks.Delete(id);
        return Results.NoContent();
    }

    private static IResult CreateBotMatch(CreateBotMatchRequest req, BotMatchService bots, ClaimsPrincipal user)
    {
        try
        {
            var dto = bots.Create(
                TryUserId(user),
                req.YourDeckId,
                req.BotDeckId,
                req.Difficulty,
                req.YourName,
                string.IsNullOrWhiteSpace(req.BotName) ? "Bot" : req.BotName!);
            return Results.Ok(dto);
        }
        catch (InvalidOperationException ex) when (ex.Message == "PlusRequired")
        {
            return Results.Json(new { error = "PlusRequired" }, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static IResult GetBotMatch(Guid id, BotMatchService bots, ClaimsPrincipal user)
    {
        try { return Results.Ok(bots.Get(id, TryUserId(user))); }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static IResult BotMulligan(Guid id, CardActionRequest req, BotMatchService bots, ClaimsPrincipal user) =>
        Act(id, user, bots, (m, e) => e.Mulligan(m, 0, req.CardId));

    private static IResult BotFinishMulligan(Guid id, BotMatchService bots, ClaimsPrincipal user) =>
        Act(id, user, bots, (m, e) => e.FinishMulligan(m, 0));

    private static IResult BotPlay(Guid id, PlayCardRequest req, BotMatchService bots, ClaimsPrincipal user) =>
        Act(id, user, bots, (m, e) => e.PlayCard(m, 0, req.CardId, req.Row, req.TargetId));

    private static IResult BotPass(Guid id, BotMatchService bots, ClaimsPrincipal user) =>
        Act(id, user, bots, (m, e) => e.Pass(m, 0));

    private static IResult BotLeader(Guid id, BotMatchService bots, ClaimsPrincipal user) =>
        Act(id, user, bots, (m, e) => e.UseLeader(m, 0));

    private static IResult BotPending(Guid id, PendingRequest req, BotMatchService bots, ClaimsPrincipal user) =>
        Act(id, user, bots, (m, e) => e.ResolvePending(m, 0, req.TargetId, req.Row));

    private static IResult BotChooseStart(Guid id, ChooseStartRequest req, BotMatchService bots, ClaimsPrincipal user) =>
        Act(id, user, bots, (m, e) => e.ChooseWhoStarts(m, 0, req.StartingPlayer));

    private static IResult BotContinueRound(Guid id, BotMatchService bots, ClaimsPrincipal user)
    {
        try { return Results.Ok(bots.ContinueAfterRound(id, TryUserId(user))); }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static IResult BotStep(Guid id, BotMatchService bots, ClaimsPrincipal user)
    {
        try
        {
            var stepped = bots.StepBot(id, TryUserId(user));
            var board = bots.Get(id, TryUserId(user));
            return Results.Ok(new BotStepResponse(stepped, board));
        }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static IResult Act(Guid id, ClaimsPrincipal user, BotMatchService bots, Action<MatchState, Game.Engine.RulesEngine> action)
    {
        try
        {
            return Results.Ok(bots.Act(id, TryUserId(user), action, autoBot: false));
        }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }

    private static IResult GetCustomContent(CustomContentStore content) =>
        Results.Ok(content.Snapshot());

    private static IResult PutCustomContent(CustomContentDocument body, ClaimsPrincipal user, AccountStore store, CustomContentStore content)
    {
        var acc = store.FindById(RequireUserId(user));
        if (acc?.Role != AccountRole.Admin) return Results.Forbid();
        try
        {
            content.ReplaceAll(body);
            return Results.Ok(content.Snapshot());
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception)
        {
            return Results.BadRequest(new { error = "InvalidContent" });
        }
    }

    private static IResult RedeemInvite(RedeemRequest req, ClaimsPrincipal user, InviteStore invites, AccountStore store)
    {
        try
        {
            var id = RequireUserId(user);
            var acc = store.FindById(id) ?? throw new InvalidOperationException("NotFound");
            var days = invites.Redeem(req.Code ?? "", id, acc.Username);
            acc = store.GrantPlusDays(id, days);
            return Results.Ok(new { days, account = AccountDto.From(acc) });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static IResult ListInvites(ClaimsPrincipal user, AccountStore store, InviteStore invites)
    {
        var acc = store.FindById(RequireUserId(user));
        if (acc?.Role != AccountRole.Admin) return Results.Forbid();
        return Results.Ok(invites.List());
    }

    private static IResult CreateInvite(CreateInviteRequest req, ClaimsPrincipal user, AccountStore store, InviteStore invites)
    {
        var acc = store.FindById(RequireUserId(user));
        if (acc?.Role != AccountRole.Admin) return Results.Forbid();
        try
        {
            return Results.Ok(invites.Create(acc.Id, req.DurationDays, req.MaxActivations));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static IResult DeleteInvite(string id, ClaimsPrincipal user, AccountStore store, InviteStore invites)
    {
        var acc = store.FindById(RequireUserId(user));
        if (acc?.Role != AccountRole.Admin) return Results.Forbid();
        invites.Delete(id);
        return Results.NoContent();
    }

    private static IResult MyInviteRequest(ClaimsPrincipal user, InviteRequestStore requests) =>
        Results.Ok(requests.GetLatestFor(RequireUserId(user)));

    private static IResult CreateInviteRequest(ClaimsPrincipal user, AccountStore store, InviteRequestStore requests)
    {
        try
        {
            var id = RequireUserId(user);
            var acc = store.FindById(id) ?? throw new InvalidOperationException("NotFound");
            if (acc.HasPlus)
                throw new InvalidOperationException("AlreadyPlus");
            var req = requests.Create(acc.Id, acc.Username, acc.DisplayName, days: 1);
            return Results.Ok(req);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static IResult ListInviteRequests(ClaimsPrincipal user, AccountStore store, InviteRequestStore requests)
    {
        var acc = store.FindById(RequireUserId(user));
        if (acc?.Role != AccountRole.Admin) return Results.Forbid();
        return Results.Ok(requests.ListAll());
    }

    private static IResult ApproveInviteRequest(
        string id, ClaimsPrincipal user, AccountStore store, InviteStore invites, InviteRequestStore requests)
    {
        var admin = store.FindById(RequireUserId(user));
        if (admin?.Role != AccountRole.Admin) return Results.Forbid();
        try
        {
            var pending = requests.ListAll().FirstOrDefault(r => r.Id == id)
                          ?? throw new InvalidOperationException("InviteRequestNotFound");
            if (pending.Status != InviteRequestStatus.Pending)
                throw new InvalidOperationException("InviteRequestResolved");

            var invite = invites.Create(admin.Id, pending.RequestedDays, maxActivations: 1);
            var target = store.FindById(pending.AccountId)
                         ?? throw new InvalidOperationException("NotFound");
            var days = invites.Redeem(invite.Code, target.Id, target.Username);
            store.GrantPlusDays(target.Id, days);
            var resolved = requests.Approve(id, admin.Id, invite.Code);
            return Results.Ok(resolved);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static IResult RejectInviteRequest(string id, ClaimsPrincipal user, AccountStore store, InviteRequestStore requests)
    {
        var admin = store.FindById(RequireUserId(user));
        if (admin?.Role != AccountRole.Admin) return Results.Forbid();
        try
        {
            return Results.Ok(requests.Reject(id, admin.Id));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static IResult History(ClaimsPrincipal user, AccountStore store) =>
        Results.Ok(store.GetMatchesFor(RequireUserId(user)));

    private static IResult FavoriteCount(ClaimsPrincipal user, AccountStore store) =>
        Results.Ok(new { count = store.FavoriteFactionMatchCount(RequireUserId(user)) });

    private static IResult UpdateProfile(UpdateProfileRequest req, ClaimsPrincipal user, AccountStore store)
    {
        try
        {
            var acc = store.UpdateProfile(RequireUserId(user), req.DisplayName ?? "", req.AvatarId ?? "wolf");
            return Results.Ok(AccountDto.From(acc));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UploadAvatar(ClaimsPrincipal user, AccountStore store, HttpRequest request)
    {
        if (!request.HasFormContentType) return Results.BadRequest();
        var file = request.Form.Files.GetFile("file");
        if (file is null) return Results.BadRequest(new { error = "NoFile" });
        await using var stream = file.OpenReadStream();
        try
        {
            var acc = store.SaveCustomAvatar(RequireUserId(user), stream, file.ContentType);
            return Results.Ok(AccountDto.From(acc));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}

public record LoginRequest(string? Username, string? Password);
public record RegisterRequest(string? Username, string? Password, string? PasswordConfirm, string? DisplayName);
public record AuthResponse(string Token, AccountDto Account);
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
    public static AccountDto From(PlayerAccount a) => new(
        a.Id, a.Username, a.DisplayName, a.AvatarId, a.AvatarVersion, a.Role, a.HasPlus, a.PlusUntilUtc,
        a.Rating, a.FavoriteFaction, a.MatchesVsPlayers, a.WinsVsPlayers, a.LossesVsPlayers, a.DrawsVsPlayers,
        a.MatchesVsBot, a.WinsVsBot, a.LossesVsBot);
}

public record DeckDto(Guid Id, string Name, Faction Faction, string LeaderId, List<string> CardIds, bool IsPreset)
{
    public static DeckDto From(SavedDeck d) => new(d.Id, d.Name, d.Faction, d.LeaderId, d.CardIds.ToList(), d.IsPreset);
}

public record SavedDeckDto(Guid Id, string Name, Faction Faction, string LeaderId, List<string> CardIds);
public record CreateBotMatchRequest(Guid YourDeckId, Guid BotDeckId, BotDifficulty Difficulty, string? YourName, string? BotName);
public record CardActionRequest(Guid CardId);
public record PlayCardRequest(Guid CardId, CombatRow? Row, Guid? TargetId);
public record PendingRequest(Guid? TargetId, CombatRow? Row);
public record ChooseStartRequest(int StartingPlayer);
public record BotStepResponse(bool Stepped, MatchBoardDto Board);
public record BuyPlusRequest(int Months);
public record RedeemRequest(string? Code);
public record CreateInviteRequest(int DurationDays, int MaxActivations);
public record UpdateProfileRequest(string? DisplayName, string? AvatarId);
