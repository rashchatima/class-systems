using System.Runtime.CompilerServices;

namespace ClassSystems.Core.Characters.Models;

// Pure models of the game: data without behaviour. The rules of the fight live in Game and the Rules folder.

/// <summary>
/// Base characteristics of a character before any equipment.
/// </summary>
public record Stats(int MaxHealth, int Armor, int AttackPower, int HealPower);

/// <summary>
/// A special action. <see cref="Effect"/> names the rule from <see cref="Rules.AbilityCatalog"/> that it applies.
/// </summary>
/// <remarks>
/// Two abilities with the same name are still two different objects (for example, one learned and one
/// granted by an item), so equality compares references instead of values.
/// </remarks>
/// <param name="Description">What the ability does in words; filled for abilities from the game database.</param>
public sealed record Ability(string Name, string Effect, string? Description = null)
{
    public bool Equals(Ability? other) => ReferenceEquals(this, other);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}

/// <summary>
/// A piece of equipment that changes the stats of its owner and can grant an ability.
/// </summary>
/// <remarks>
/// Two swords with the same stats are still two swords, so equality compares references.
/// </remarks>
/// <param name="Kind">The weapon type (Sword, Claymore, Bow…); <c>null</c> for items anyone can wear.</param>
/// <param name="Description">A text about the item, for example the passive effect of a weapon.</param>
public sealed record Item(
    string Name,
    int HealthBonus = 0,
    int ArmorBonus = 0,
    int AttackBonus = 0,
    Ability? GrantedAbility = null,
    string? Kind = null,
    string? Description = null)
{
    public bool Equals(Item? other) => ReferenceEquals(this, other);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}

/// <summary>
/// A template a character is created from: the name of the class, its stats and its innate ability.
/// </summary>
public record CharacterClass(string Name, Stats Stats, string InnateEffect);

/// <summary>
/// Background of a character imported from the game database: who it is, not how strong it is.
/// </summary>
/// <param name="Title">An epithet, for example "The Dark Side of Dawn".</param>
/// <param name="Element">The element of the character, for example Pyro.</param>
/// <param name="WeaponType">The only kind of weapon the character can use, for example Claymore.</param>
/// <param name="Rarity">4 or 5 stars.</param>
public record CharacterProfile(
    string? Title,
    string? Element,
    string? WeaponType,
    string? Nation,
    int Rarity,
    string? Description);

/// <summary>
/// The state of a character: stats, health, equipment and abilities.
/// </summary>
/// <remarks>
/// The class holds data and derives totals from it, but does not act. Everything that changes
/// a character (damage, healing, equipping) is done by <see cref="Game"/>, so setters are internal.
/// </remarks>
public sealed class Character
{
    /// <param name="name">The name of the character.</param>
    /// <param name="className">The class the character was created from, for example Warrior.</param>
    /// <param name="baseStats">Stats without equipment; the character starts with full health.</param>
    /// <param name="profile">Background from the game database; <c>null</c> for characters created by hand.</param>
    public Character(string name, string className, Stats baseStats, CharacterProfile? profile = null)
    {
        Name = name;
        ClassName = className;
        BaseStats = baseStats;
        Profile = profile;
        Health = baseStats.MaxHealth;
    }

    public string Name { get; }

    public string ClassName { get; }

    public Stats BaseStats { get; }

    /// <summary>
    /// Background of the character, if it was imported from the game database.
    /// </summary>
    public CharacterProfile? Profile { get; }

    /// <summary>
    /// Current health. The character is defeated when it drops to zero.
    /// </summary>
    public int Health { get; internal set; }

    /// <summary>
    /// Whether the next incoming hit will be weakened.
    /// </summary>
    public bool IsDefending { get; internal set; }

    /// <summary>
    /// Items the character currently wears.
    /// </summary>
    public IReadOnlyList<Item> Equipment => EquipmentList;

    /// <summary>
    /// Abilities the character knows regardless of the equipment.
    /// </summary>
    public IReadOnlyList<Ability> LearnedAbilities => LearnedList;

    /// <summary>
    /// All abilities available right now: learned ones and those granted by the equipment.
    /// </summary>
    public IEnumerable<Ability> Abilities
        => LearnedList.Concat(EquipmentList.Select(item => item.GrantedAbility).OfType<Ability>());

    public int MaxHealth => BaseStats.MaxHealth + EquipmentList.Sum(item => item.HealthBonus);

    public int Armor => BaseStats.Armor + EquipmentList.Sum(item => item.ArmorBonus);

    public int AttackPower => BaseStats.AttackPower + EquipmentList.Sum(item => item.AttackBonus);

    public int HealPower => BaseStats.HealPower;

    public bool IsAlive => Health > 0;

    internal List<Item> EquipmentList { get; } = new();

    internal List<Ability> LearnedList { get; } = new();
}
