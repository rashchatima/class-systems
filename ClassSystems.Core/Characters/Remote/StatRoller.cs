using ClassSystems.Core.Characters.Models;

namespace ClassSystems.Core.Characters.Remote;

/// <summary>
/// Generates stats for characters of the game database, which has no numbers, only descriptions.
/// </summary>
/// <remarks>
/// Stats are random within the range of the rarity, but the generator is seeded with the id of the character,
/// so the same character always gets the same stats (on every run and in tests).
/// </remarks>
public static class StatRoller
{
    /// <summary>
    /// Ranges for 5-star characters (inclusive).
    /// </summary>
    public static readonly StatRange FiveStar = new(Health: (100, 130), Armor: (5, 9), Attack: (16, 22), Heal: (6, 10));

    /// <summary>
    /// Ranges for characters of lower rarity (inclusive).
    /// </summary>
    public static readonly StatRange FourStar = new(Health: (80, 110), Armor: (3, 6), Attack: (12, 17), Heal: (4, 8));

    /// <summary>
    /// Stats of the character with the given id and rarity.
    /// </summary>
    public static Stats For(string id, int rarity)
    {
        StatRange range = rarity >= 5 ? FiveStar : FourStar;
        var random = new Random(StableSeed(id));
        int Roll((int Min, int Max) bounds) => random.Next(bounds.Min, bounds.Max + 1);

        return new Stats(Roll(range.Health), Roll(range.Armor), Roll(range.Attack), Roll(range.Heal));
    }

    // string.GetHashCode changes between runs, so a simple FNV-1a hash is used instead.
    private static int StableSeed(string id)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (char symbol in id.ToLowerInvariant())
                hash = (hash ^ symbol) * 16777619;
            return (int)hash;
        }
    }
}

/// <summary>
/// Inclusive ranges of the stats for one rarity.
/// </summary>
public record StatRange((int Min, int Max) Health, (int Min, int Max) Armor, (int Min, int Max) Attack, (int Min, int Max) Heal);
