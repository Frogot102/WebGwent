using WebGwent.Game.Models;

namespace WebGwent.Game.Services;

/// <summary>Lightweight side panel identity for the battlefield (player or bot).</summary>
public sealed class BoardProfile
{
    public Guid? AccountId { get; init; }
    public string DisplayName { get; init; } = "";
    public string AvatarId { get; init; } = "wolf";
    public long AvatarVersion { get; init; }
    public int? Rating { get; init; }
    public bool IsBot { get; init; }
    public BotDifficulty? BotDifficulty { get; init; }

    public static BoardProfile Bot(string name, BotDifficulty? difficulty = null) => new()
    {
        DisplayName = name,
        AvatarId = "skull",
        IsBot = true,
        BotDifficulty = difficulty
    };

    public static BoardProfile Guest(string name) => new()
    {
        DisplayName = name,
        AvatarId = "sword",
        IsBot = false
    };

    public static BoardProfile Player(
        Guid accountId,
        string displayName,
        string avatarId,
        long avatarVersion,
        int? rating) => new()
    {
        AccountId = accountId,
        DisplayName = displayName,
        AvatarId = avatarId,
        AvatarVersion = avatarVersion,
        Rating = rating,
        IsBot = false
    };
}
