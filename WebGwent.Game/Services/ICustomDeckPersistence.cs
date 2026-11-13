namespace WebGwent.Game.Services;

/// <summary>Persistence for user decks (PostgreSQL / file).</summary>
public interface ICustomDeckPersistence
{
    List<SavedDeck> Load();
    void SaveAll(IReadOnlyList<SavedDeck> decks);
}
