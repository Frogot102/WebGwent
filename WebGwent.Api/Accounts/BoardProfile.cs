using WebGwent.Game.Services;

namespace WebGwent.Api.Accounts;

public static class BoardProfileFactory
{
    public static BoardProfile FromAccount(PlayerAccount account, int? ratingBefore = null) =>
        BoardProfile.Player(account.Id, account.DisplayName, account.AvatarId, account.AvatarVersion, account.Rating, ratingBefore);

    public static BoardProfile FromAccountId(AccountStore store, Guid? accountId, string fallbackName, bool isBot = false)
    {
        if (isBot)
            return BoardProfile.Bot(fallbackName);
        if (accountId is Guid id && store.FindById(id) is { } acc)
            return FromAccount(acc);
        return BoardProfile.Guest(fallbackName);
    }

    /// <summary>Attach pre-match Elo when this online match was already recorded.</summary>
    public static BoardProfile FromAccountIdForMatch(
        AccountStore store, Guid? accountId, string fallbackName, Guid matchId)
    {
        if (accountId is not Guid id || store.FindById(id) is not { } acc)
            return FromAccountId(store, accountId, fallbackName);

        var hist = store.GetMatchForPlayer(matchId, id);
        if (hist is null || !string.Equals(hist.Mode, "Online", StringComparison.OrdinalIgnoreCase))
            return FromAccount(acc);

        return FromAccount(acc, ratingBefore: hist.RatingBefore);
    }
}
