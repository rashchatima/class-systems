using ClassSystems.Core.Characters.Models;
using ClassSystems.Core.Characters.Rules;

namespace ClassSystems.Tests.Core;

/// <summary>
/// Shortcuts for single characters used by tests that need their own game.
/// </summary>
internal static class CharacterFactory
{
    public static Character Mage(string name = "Merlin") => CharacterClasses.Create("mage", name);
}
