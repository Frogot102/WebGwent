using WebGwent.Game.Services;

namespace WebGwent.Api.Accounts;

public static class BoardProfileFactory
{
    public static BoardProfile FromAccount(PlayerAccount account) =>
        BoardProfile.Player(account.Id, account.DisplayName, account.AvatarId, account.AvatarVersion, account.Rating);

    public static BoardProfile FromAccountId(AccountStore store, Guid? accountId, string fallbackName, bool isBot = false)
    {
        if (isBot)
            return BoardProfile.Bot(fallbackName);
        if (accountId is Guid id && store.FindById(id) is { } acc)
            return FromAccount(acc);
        return BoardProfile.Guest(fallbackName);
    }
}
