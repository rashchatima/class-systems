using ClassSystems.Core.Characters.Models;
using ClassSystems.Remote.Models;

namespace ClassSystems.Core.Characters.Remote;

/// <summary>
/// Translates data of the remote game database into the classes of the game.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>a character keeps its background as a <see cref="CharacterProfile"/>; stats come from <see cref="StatRoller"/>;</item>
/// <item>every talent becomes an ability; the kind of the talent chooses the rule of the fight;</item>
/// <item>a weapon becomes an item of its weapon type; its secondary stat gives a bonus and its passive
/// effect becomes an ability granted by the item.</item>
/// </list>
/// </remarks>
public static class RemoteMapper
{
    // Longer texts of the database are cut, so the character sheet stays readable.
    private const int MaxDescriptionLength = 220;

    // The database writes "-" when a weapon has no secondary stat or passive effect.
    private const string Nothing = "-";

    /// <summary>
    /// Creates a character from the database entry.
    /// </summary>
    /// <param name="name">A name for the new character; the name from the database by default.</param>
    public static Character ToCharacter(RemoteCharacter data, string? name = null)
    {
        var profile = new CharacterProfile(data.Title, data.Vision, data.Weapon, data.Nation, data.Rarity, data.Description);
        var character = new Character(name ?? data.Name, data.Name, StatRoller.For(data.Id, data.Rarity), profile);

        foreach (RemoteTalent talent in data.SkillTalents ?? Array.Empty<RemoteTalent>())
            character.LearnedList.Add(ToAbility(talent));

        return character;
    }

    /// <summary>
    /// Turns a talent into an ability: a normal attack hits hard, an elemental skill ignores armor,
    /// an elemental burst drains life.
    /// </summary>
    public static Ability ToAbility(RemoteTalent talent)
    {
        string effect = talent.Type switch
        {
            "ELEMENTAL_SKILL" => "fireball",
            "ELEMENTAL_BURST" => "life-drain",
            _ => "power-strike",
        };
        return new Ability(talent.Name, effect, Shorten(talent.Description));
    }

    /// <summary>
    /// Creates an item from a weapon of the database.
    /// </summary>
    public static Item ToItem(RemoteWeapon weapon)
    {
        int health = 0, armor = 0, attack = Math.Max(1, weapon.BaseAttack / 4);

        // The secondary stat adds a bonus of its kind; stats without an analogue in our game add attack.
        switch (weapon.SubStat)
        {
            case null or Nothing:
                break;
            case "HP":
                health += 15;
                break;
            case "DEF":
                armor += 3;
                break;
            default:
                attack += 2;
                break;
        }

        Ability? passive = HasValue(weapon.PassiveName)
            ? new Ability(weapon.PassiveName!, weapon.Type == "Catalyst" ? "fireball" : "power-strike", Shorten(weapon.PassiveDesc))
            : null;

        string description = $"{Stars(weapon.Rarity)} {weapon.Type}, base ATK {weapon.BaseAttack}"
                             + (HasValue(weapon.SubStat) ? $", secondary stat {weapon.SubStat}" : "");

        return new Item(weapon.Name, health, armor, attack, passive, weapon.Type, description);
    }

    /// <summary>
    /// Rarity as stars, for example ★★★★★.
    /// </summary>
    public static string Stars(int rarity) => new('★', Math.Clamp(rarity, 0, 5));

    private static bool HasValue(string? text) => !string.IsNullOrWhiteSpace(text) && text != Nothing;

    // Keeps the first meaningful line (talents often start with a short caption such as "Normal Attack")
    // and cuts it to a readable length.
    private static string? Shorten(string? text)
    {
        if (!HasValue(text))
            return null;

        string[] lines = text!.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string line = lines.FirstOrDefault(candidate => candidate.Contains('.')) ?? lines[0];
        return line.Length <= MaxDescriptionLength ? line : line[..(MaxDescriptionLength - 1)].TrimEnd() + "…";
    }
}
