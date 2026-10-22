using WebGwent.Game.Bot;
using WebGwent.Game.Catalog;
using WebGwent.Game.Engine;
using WebGwent.Game.Models;
using Xunit.Abstractions;

namespace WebGwent.Tests.Bots;

/// <summary>
/// The bot against the original heuristic bot: every faction against every faction, both seats,
/// each side going first equally often. Seeds are fixed, so the result is reproducible.
/// </summary>
public class BotBenchmarkTests(ITestOutputHelper output)
{
    private readonly CardCatalog _catalog = CardCatalog.LoadEmbedded();

    [Fact]
    public void Bot_Beats_The_Legacy_Bot_Across_All_Matchups()
    {
        var (wins, draws, losses) = (0, 0, 0);
        var seed = 1000;
        foreach (var botDeck in PresetDecks.All())
        foreach (var oppDeck in PresetDecks.All())
        {
            for (var botSeat = 0; botSeat < 2; botSeat++)
            {
                var engine = new RulesEngine(new Random(seed));
                var bot = new BotAI(engine, new Random(seed));
                var legacy = new LegacyBot(engine, new Random(seed));
                var starter = (seed / 2) % 2;

                var match = botSeat == 0
                    ? MatchRunner.Play(_catalog, engine, botDeck, oppDeck, bot.TakeTurn, legacy.TakeTurn, starter)
                    : MatchRunner.Play(_catalog, engine, oppDeck, botDeck, legacy.TakeTurn, bot.TakeTurn, starter);
                seed++;

                Assert.Equal(MatchPhase.MatchEnd, match.Phase);
                if (match.Winner == botSeat) wins++;
                else if (match.Winner is null) draws++;
                else losses++;
            }
        }

        var score = (wins + 0.5 * draws) / (wins + draws + losses);
        output.WriteLine($"Bot vs legacy: {wins} wins, {draws} draws, {losses} losses — score {score:P0}");
        Assert.True(score >= 0.75, $"Expected the bot to score at least 75%, got {score:P0} ({wins}/{draws}/{losses}).");
    }

    [Fact]
    public void Bot_Plays_Itself_To_The_End_In_Every_Matchup()
    {
        var seed = 77;
        foreach (var d0 in PresetDecks.All())
        foreach (var d1 in PresetDecks.All())
        {
            var engine = new RulesEngine(new Random(seed));
            var bot = new BotAI(engine, new Random(seed));
            var match = MatchRunner.Play(_catalog, engine, d0, d1, bot.TakeTurn, bot.TakeTurn, seed++ % 2);
            Assert.Equal(MatchPhase.MatchEnd, match.Phase);
            Assert.InRange(match.RoundNumber, 2, 3);
        }
    }
}
