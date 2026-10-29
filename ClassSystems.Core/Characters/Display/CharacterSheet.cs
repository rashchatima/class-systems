using ClassSystems.Core.Characters.Models;
using ClassSystems.Core.Characters.Remote;
using ClassSystems.Core.Text;
using ClassSystems.Core.Text.Display;
using ClassSystems.Core.Text.Models;
using ClassSystems.Infra.Display;

namespace ClassSystems.Core.Characters.Display;

/// <summary>
/// The full state of a character written with the structured text system.
/// </summary>
/// <remarks>
/// The sheet builds a <see cref="Document"/> (sections Profile, Stats, Equipment, Abilities with nested
/// headings) and renders it with <see cref="TextDto"/>, so it looks like any other text of the program
/// and can be saved or exported the same way.
/// </remarks>
public class CharacterSheet(Character character) : IDisplayable
{
    /// <summary>
    /// Builds the document that describes the character.
    /// </summary>
    public Document Build()
    {
        CharacterProfile? profile = character.Profile;
        var document = new Document(profile?.Title != null ? $"{character.Name} — {profile.Title}" : character.Name);

        var about = Section(document, "Profile");
        Paragraph(about, Join(
            $"Class: {character.ClassName}",
            profile?.Element != null ? $"Element: {profile.Element}" : null,
            profile?.WeaponType != null ? $"Weapon: {profile.WeaponType}" : null,
            profile?.Nation != null ? $"Nation: {profile.Nation}" : null,
            profile is { Rarity: > 0 } ? $"Rarity: {RemoteMapper.Stars(profile.Rarity)}" : null));
        if (profile?.Description != null)
            Paragraph(about, profile.Description);

        var stats = Section(document, "Stats");
        Paragraph(stats, Join(
            $"Health: {character.Health}/{character.MaxHealth}",
            $"Armor: {character.Armor}",
            $"Attack: {character.AttackPower}",
            $"Heal power: {character.HealPower}"));
        Paragraph(stats, $"State: {(character.IsAlive ? "ready to fight" : "defeated")}{(character.IsDefending ? ", defending" : "")}");

        var equipment = Section(document, "Equipment");
        if (character.Equipment.Count == 0)
            Paragraph(equipment, "No equipment.");
        foreach (Item item in character.Equipment)
        {
            var heading = Section(equipment, item.Name);
            Paragraph(heading, Join(
                item.HealthBonus != 0 ? $"+{item.HealthBonus} HP" : null,
                item.ArmorBonus != 0 ? $"+{item.ArmorBonus} armor" : null,
                item.AttackBonus != 0 ? $"+{item.AttackBonus} attack" : null,
                item.GrantedAbility != null ? $"grants {item.GrantedAbility.Name}" : null) is { Length: > 0 } bonuses
                    ? bonuses
                    : "No bonuses.");
            if (item.Description != null)
                Paragraph(heading, item.Description);
        }

        var abilities = Section(document, "Abilities");
        foreach (Ability ability in character.Abilities)
        {
            Item? source = character.Equipment.FirstOrDefault(item => item.GrantedAbility == ability);
            var heading = Section(abilities, ability.Name);
            Paragraph(heading, $"Effect in a fight: {ability.Effect}" + (source != null ? $" (from {source.Name})" : ""));
            if (ability.Description != null)
                Paragraph(heading, ability.Description);
        }

        return document;
    }

    /// <inheritdoc/>
    public string Render() => new TextDto(Build()).Render();

    private static Heading Section(ITextSection parent, string title)
    {
        var heading = new Heading(title);
        TextTree.Add(parent, heading);
        return heading;
    }

    private static void Paragraph(ITextSection parent, string content) => TextTree.Add(parent, new Paragraph(content));

    private static string Join(params string?[] parts) => string.Join(" · ", parts.Where(part => part != null));
}
