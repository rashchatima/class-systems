using ClassSystems.Core.Characters;
using ClassSystems.Core.Characters.Models;
using ClassSystems.Core.Characters.Rules;
using ClassSystems.Tests.Doubles;

namespace ClassSystems.Tests.Core;

/// <summary>
/// Character classes and the state of the game as a whole.
/// </summary>
public class GameTests
{
    [Theory]
    [InlineData("warrior", "Warrior", 120, 8, 16, "power-strike")]
    [InlineData("mage", "Mage", 70, 2, 20, "fireball")]
    [InlineData("priest", "Priest", 85, 4, 9, "holy-light")]
    [InlineData("WARRIOR", "Warrior", 120, 8, 16, "power-strike")]
    public void CharacterClasses_CreateCharacterWithStatsAndInnateAbility(
        string key, string className, int health, int armor, int attack, string innateEffect)
    {
        // Act
        Character hero = CharacterClasses.Create(key, "Hero");

        // Assert
        Assert.Equal(className, hero.ClassName);
        Assert.Equal(health, hero.Health);
        Assert.Equal(armor, hero.Armor);
        Assert.Equal(attack, hero.AttackPower);
        Assert.Equal(innateEffect, Assert.Single(hero.LearnedAbilities).Effect);
    }

    [Fact]
    public void CharacterClasses_UnknownClass_Throws()
    {
        Assert.Throws<ArgumentException>(() => CharacterClasses.Create("rogue", "Hero"));
    }

    [Fact]
    public void Join_SameCharacterTwice_AddsItOnce()
    {
        // Arrange
        var arena = new Arena();

        // Act
        arena.Game.Join(arena.Warrior);

        // Assert
        Assert.Equal(3, arena.Game.Characters.Count);
    }

    [Fact]
    public void NewFight_IsNotOverAndHasNoWinner()
    {
        var arena = new Arena();

        Assert.False(arena.Game.IsOver);
        Assert.Null(arena.Game.Winner);
    }

    [Fact]
    public void ReportStatus_SendsOneReportObjectToTheDisplayer()
    {
        // Arrange
        var arena = new Arena();
        arena.Game.Attack(arena.Warrior, arena.Mage);
        int textCallsBefore = arena.Output.TextCalls;

        // Act
        arena.Game.ReportStatus();

        // Assert
        Assert.Equal(1, arena.Output.DisplayableCalls);
        Assert.Equal(textCallsBefore, arena.Output.TextCalls);
        Assert.Contains("Merlin: 56/70 HP, 2 armor, 20 attack", arena.Output.LastLine);
    }

    [Fact]
    public void SetDisplayer_RedirectsFurtherOutput()
    {
        // Arrange
        var arena = new Arena();
        var second = new MockDisplayer();

        // Act
        arena.Game.SetDisplayer(second);
        arena.Game.Defend(arena.Warrior);

        // Assert
        Assert.Empty(arena.Output.Lines);
        Assert.Equal("Conan takes a defensive stance.", Assert.Single(second.Lines));
    }

    [Fact]
    public void Abilities_ListsEveryAbilityOfTheWorldOnce()
    {
        // Arrange
        var game = new Game(new FakeDisplayer());
        Character warrior = CharacterClasses.Create("warrior", "Conan");
        game.Join(warrior);
        Ability drain = AbilityCatalog.Create("life-drain");
        Ability fireball = AbilityCatalog.Create("fireball");

        // Act
        game.AddAbility(drain);
        game.Teach(warrior, drain);
        game.AddItem(new Item("Staff", GrantedAbility: fireball));

        // Assert: Power Strike (innate) + Life Drain (once) + Fireball (from the item)
        Assert.Equal(3, game.Abilities.Count());
        Assert.Contains(fireball, game.Abilities);
    }
}
