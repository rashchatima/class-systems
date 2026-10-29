using System.Text;
using ClassSystems.Cli.Support;
using ClassSystems.Core.Characters.Models;
using ClassSystems.Infra.Display;

namespace ClassSystems.Cli.Presenter;

// DTOs of the command interface. Unlike the DTOs of the core, they know the ids the interface gives to objects.

/// <summary>
/// Full information about a character for <c>ls char --id</c>.
/// </summary>
public class CharacterCard(Character character, WorldIds ids) : IDisplayable
{
    /// <inheritdoc/>
    public string Render()
    {
        var text = new StringBuilder();
        text.AppendLine($"#{ids.Characters.IdOf(character)} {character.Name} ({character.ClassName})");
        text.AppendLine($"  health: {character.Health}/{character.MaxHealth}, armor: {character.Armor}, attack: {character.AttackPower}, heal power: {character.HealPower}");
        text.AppendLine($"  alive: {(character.IsAlive ? "yes" : "no")}, defending: {(character.IsDefending ? "yes" : "no")}");
        text.AppendLine($"  equipment: {Cards.Join(character.Equipment.Select(item => $"#{ids.Items.IdOf(item)} {item.Name}"))}");
        text.Append($"  abilities: {Cards.Join(character.Abilities.Select(ability => $"#{ids.Abilities.IdOf(ability)} {ability.Name}"))}");
        return text.ToString();
    }
}

/// <summary>
/// One line about an item: its id, name and bonuses.
/// </summary>
public class ItemCard(Item item, WorldIds ids) : IDisplayable
{
    /// <inheritdoc/>
    public string Render()
    {
        string ability = item.GrantedAbility != null
            ? $", grants #{ids.Abilities.IdOf(item.GrantedAbility)} {item.GrantedAbility.Name}"
            : "";
        return $"#{ids.Items.IdOf(item)} {item.Name} (+{item.HealthBonus} HP, +{item.ArmorBonus} armor, +{item.AttackBonus} attack{ability})";
    }
}

internal static class Cards
{
    /// <summary>
    /// Joins values with commas, or gives "none" for an empty list.
    /// </summary>
    public static string Join(IEnumerable<string> values)
    {
        string text = string.Join(", ", values);
        return text.Length > 0 ? text : "none";
    }
}
