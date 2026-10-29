using ClassSystems.Core.Characters;
using ClassSystems.Core.Characters.Models;
using ClassSystems.Core.Characters.Rules;

namespace ClassSystems.Tests.Doubles;

/// <summary>
/// Prepares a game with three standard characters, so tests do not repeat the same Arrange step.
/// </summary>
/// <remarks>
/// Stats: Warrior 120 HP / armor 8 / attack 16; Mage 70 / 2 / 20; Priest 85 / 4 / 9, heal 14.
/// </remarks>
public class Arena
{
    public Arena()
    {
        Output = new MockDisplayer();
        Game = new Game(Output);
        Warrior = CharacterClasses.Create("warrior", "Conan");
        Mage = CharacterClasses.Create("mage", "Merlin");
        Priest = CharacterClasses.Create("priest", "Tuck");
        Game.Join(Warrior);
        Game.Join(Mage);
        Game.Join(Priest);
    }

    public MockDisplayer Output { get; }

    public Game Game { get; }

    public Character Warrior { get; }

    public Character Mage { get; }

    public Character Priest { get; }

    /// <summary>
    /// A character that has not joined the game.
    /// </summary>
    public static Character Outsider() => new("Stranger", "Nobody", new Stats(50, 0, 10, 0));
}
