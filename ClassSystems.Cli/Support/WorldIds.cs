using ClassSystems.Core.Characters.Models;

namespace ClassSystems.Cli.Support;

/// <summary>
/// Identifiers the command interface gives to the objects of the game world.
/// </summary>
/// <remarks>
/// After a save is loaded the objects are new, so the numbering starts again with <see cref="Reset"/>.
/// </remarks>
public class WorldIds
{
    public IdRegistry<Character> Characters { get; private set; } = new();

    public IdRegistry<Item> Items { get; private set; } = new();

    public IdRegistry<Ability> Abilities { get; private set; } = new();

    /// <summary>
    /// Forgets all identifiers.
    /// </summary>
    public void Reset()
    {
        Characters = new IdRegistry<Character>();
        Items = new IdRegistry<Item>();
        Abilities = new IdRegistry<Ability>();
    }
}
