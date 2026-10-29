using ClassSystems.Core.Characters.Models;

namespace ClassSystems.Core.Characters.Rules;

/// <summary>
/// Classes a character can be created from. A new class is added with one entry in the dictionary.
/// </summary>
/// <remarks>
/// Before, every class was a subclass of Character. The classes differed only in data,
/// so now they are data: a <see cref="CharacterClass"/> template per class.
/// </remarks>
public static class CharacterClasses
{
    private static readonly Dictionary<string, CharacterClass> Classes = new(StringComparer.OrdinalIgnoreCase)
    {
        // A tough melee fighter with heavy armor.
        ["warrior"] = new("Warrior", new Stats(MaxHealth: 120, Armor: 8, AttackPower: 16, HealPower: 5), "power-strike"),
        // A fragile spellcaster with high damage.
        ["mage"] = new("Mage", new Stats(MaxHealth: 70, Armor: 2, AttackPower: 20, HealPower: 6), "fireball"),
        // A support character that is weak in attack but strong in healing.
        ["priest"] = new("Priest", new Stats(MaxHealth: 85, Armor: 4, AttackPower: 9, HealPower: 14), "holy-light"),
    };

    /// <summary>
    /// Keys of all classes, for example <c>warrior</c>.
    /// </summary>
    public static IReadOnlyCollection<string> Keys => Classes.Keys;

    /// <summary>
    /// Creates a character of the class with its innate ability.
    /// </summary>
    /// <exception cref="ArgumentException">There is no such class.</exception>
    public static Character Create(string className, string name)
    {
        if (!Classes.TryGetValue(className, out CharacterClass? template))
            throw new ArgumentException($"Unknown class \"{className}\". Available: {string.Join(", ", Classes.Keys)}.");

        var character = new Character(name, template.Name, template.Stats);
        character.LearnedList.Add(AbilityCatalog.Create(template.InnateEffect));
        return character;
    }
}
