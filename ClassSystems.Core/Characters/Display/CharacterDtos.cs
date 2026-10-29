using System.Text;
using ClassSystems.Core.Characters.Models;
using ClassSystems.Infra.Display;

namespace ClassSystems.Core.Characters.Display;

/// <summary>
/// Short state of a character for reports of the game.
/// </summary>
public class CharacterDto(Character character) : IDisplayable
{
    public string Name { get; } = character.Name;

    public int Health { get; } = character.Health;

    public int MaxHealth { get; } = character.MaxHealth;

    public int Armor { get; } = character.Armor;

    public int AttackPower { get; } = character.AttackPower;

    /// <inheritdoc/>
    public string Render() => Health > 0
        ? $"{Name}: {Health}/{MaxHealth} HP, {Armor} armor, {AttackPower} attack"
        : $"{Name}: out of the fight";
}

/// <summary>
/// The state of all characters of a game, one per line, framed by empty lines.
/// </summary>
public class StatusReportDto(IEnumerable<Character> characters) : IDisplayable
{
    private readonly List<CharacterDto> _characters = characters.Select(character => new CharacterDto(character)).ToList();

    /// <inheritdoc/>
    public string Render()
    {
        var text = new StringBuilder();
        text.AppendLine();
        foreach (CharacterDto character in _characters)
            text.AppendLine($"  {character.Render()}");

        return text.ToString();
    }
}
