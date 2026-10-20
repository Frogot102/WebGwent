namespace WebGwent.Server.Accounts;

/// <summary>Lightweight side panel identity for the battlefield (player or bot).</summary>
public sealed class BoardProfile
{
    public Guid? AccountId { get; init; }
    public string DisplayName { get; init; } = "";
    public string AvatarId { get; init; } = "wolf";
    public long AvatarVersion { get; init; }
    public int? Rating { get; init; }
    public bool IsBot { get; init; }

    public static BoardProfile FromAccount(PlayerAccount account) => new()
    {
        AccountId = account.Id,
        DisplayName = account.DisplayName,
        AvatarId = account.AvatarId,
        AvatarVersion = account.AvatarVersion,
        Rating = account.Rating,
        IsBot = false
    };

    public static BoardProfile Bot(string name) => new()
    {
        DisplayName = name,
        AvatarId = "skull",
        IsBot = true
    };

    public static BoardProfile Guest(string name) => new()
    {
        DisplayName = name,
        AvatarId = "sword",
        IsBot = false
    };

    public static BoardProfile FromAccountId(AccountStore store, Guid? accountId, string fallbackName, bool isBot = false)
    {
        if (isBot)
            return Bot(fallbackName);
        if (accountId is Guid id && store.FindById(id) is { } acc)
            return FromAccount(acc);
        return Guest(fallbackName);
    }
}
