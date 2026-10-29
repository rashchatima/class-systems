using ClassSystems.Core.Characters.Models;

namespace ClassSystems.Core.Characters.Rules;

/// <summary>
/// All kinds of abilities and what each of them does.
/// </summary>
/// <remarks>
/// An <see cref="Ability"/> only stores the key of its effect, which makes it easy to save.
/// A new kind of ability is added with one entry in the dictionary.
/// </remarks>
public static class AbilityCatalog
{
    private const int PowerStrikeMultiplier = 2;
    private const int HolyLightMultiplier = 3;

    // The share of the dealt damage that Life Drain returns to the user.
    private const double DrainRatio = 0.5;

    private record Effect(string Name, Func<Ability, Character, Character, string> Apply);

    private static readonly Dictionary<string, Effect> Effects = new(StringComparer.OrdinalIgnoreCase)
    {
        ["power-strike"] = new("Power Strike", (ability, user, target) =>
        {
            int damage = Combat.TakeDamage(target, user.AttackPower * PowerStrikeMultiplier);
            return $"{user.Name} smashes {target.Name} with {ability.Name} for {damage} damage.";
        }),
        ["fireball"] = new("Fireball", (ability, user, target) =>
        {
            int damage = Combat.TakeDamage(target, user.AttackPower, ignoreArmor: true);
            return $"{user.Name} casts {ability.Name} at {target.Name} for {damage} damage, ignoring armor.";
        }),
        ["holy-light"] = new("Holy Light", (ability, user, target) =>
        {
            int restored = Combat.RestoreHealth(target, user.HealPower * HolyLightMultiplier);
            return $"{user.Name} calls {ability.Name} upon {target.Name}, restoring {restored} health.";
        }),
        ["life-drain"] = new("Life Drain", (ability, user, target) =>
        {
            int damage = Combat.TakeDamage(target, user.AttackPower);
            int restored = Combat.RestoreHealth(user, (int)(damage * DrainRatio));
            return $"{user.Name} uses {ability.Name} on {target.Name} for {damage} damage and restores {restored} health.";
        }),
    };

    /// <summary>
    /// Keys of all known effects, for example <c>fireball</c>.
    /// </summary>
    public static IReadOnlyCollection<string> Keys => Effects.Keys;

    /// <summary>
    /// Creates a new ability with the given effect.
    /// </summary>
    /// <exception cref="ArgumentException">There is no such effect.</exception>
    public static Ability Create(string effect) => new(Find(effect).Name, effect.ToLowerInvariant());

    /// <summary>
    /// Applies the effect of the ability.
    /// </summary>
    /// <returns>Description of what has happened.</returns>
    internal static string Apply(Ability ability, Character user, Character target)
        => Find(ability.Effect).Apply(ability, user, target);

    private static Effect Find(string effect)
        => Effects.TryGetValue(effect, out Effect? found)
            ? found
            : throw new ArgumentException($"Unknown ability effect \"{effect}\". Available: {string.Join(", ", Effects.Keys)}.");
}
