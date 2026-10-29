using ClassSystems.Core.Characters;
using ClassSystems.Core.Characters.Models;
using ClassSystems.Tests.Doubles;

namespace ClassSystems.Tests.Core;

/// <summary>
/// Regular attacks, healing and defence (rules of Combat, checked through the Game facade).
/// </summary>
public class CombatTests
{
    [Fact]
    public void Attack_ReducesHealthByAttackMinusArmor()
    {
        // Arrange
        var arena = new Arena();

        // Act
        arena.Game.Attack(arena.Warrior, arena.Mage);

        // Assert: 16 attack - 2 armor = 14 damage
        Assert.Equal(70 - 14, arena.Mage.Health);
        Assert.Equal("Conan attacks Merlin for 14 damage.", arena.Output.LastLine);
    }

    [Fact]
    public void Attack_AlwaysDealsAtLeastOneDamage()
    {
        // Arrange
        var arena = new Arena();
        var tank = new Character("Tank", "Tank", new Stats(MaxHealth: 100, Armor: 50, AttackPower: 1, HealPower: 0));
        arena.Game.Join(tank);

        // Act
        arena.Game.Attack(arena.Priest, tank);

        // Assert
        Assert.Equal(99, tank.Health);
    }

    [Fact]
    public void Defend_HalvesOnlyTheNextHit()
    {
        // Arrange
        var arena = new Arena();
        arena.Game.Defend(arena.Mage);

        // Act
        arena.Game.Attack(arena.Warrior, arena.Mage);
        int afterFirstHit = arena.Mage.Health;
        arena.Game.Attack(arena.Warrior, arena.Mage);

        // Assert: 14 / 2 = 7 for the first hit, then the full 14
        Assert.Equal(63, afterFirstHit);
        Assert.Equal(49, arena.Mage.Health);
        Assert.False(arena.Mage.IsDefending);
    }

    [Fact]
    public void Attack_HealthNeverGoesBelowZero_AndTheWinnerIsAnnounced()
    {
        // Arrange
        var output = new MockDisplayer();
        var game = new Game(output);
        var mage = CharacterFactory.Mage();
        var dummy = new Character("Dummy", "Dummy", new Stats(5, 0, 1, 0));
        game.Join(mage);
        game.Join(dummy);

        // Act
        game.Attack(mage, dummy);

        // Assert
        Assert.Equal(0, dummy.Health);
        Assert.False(dummy.IsAlive);
        Assert.True(game.IsOver);
        Assert.Same(mage, game.Winner);
        Assert.Equal(
            new[] { "Merlin attacks Dummy for 5 damage.", "Dummy is defeated.", "Merlin wins the fight!" },
            output.Lines);
    }

    [Fact]
    public void Heal_RestoresHealthButNotAboveMaximum()
    {
        // Arrange
        var arena = new Arena();
        arena.Game.Attack(arena.Warrior, arena.Mage); // 70 -> 56

        // Act
        arena.Game.Heal(arena.Priest, arena.Mage);    // +14 -> 70
        arena.Game.Heal(arena.Priest, arena.Mage);    // already full

        // Assert
        Assert.Equal(70, arena.Mage.Health);
        Assert.Equal("Tuck heals Merlin for 0 health.", arena.Output.LastLine);
    }

    [Fact]
    public void DefeatedCharacter_CannotAct()
    {
        // Arrange
        var output = new MockDisplayer();
        var game = new Game(output);
        var mage = CharacterFactory.Mage();
        var dummy = new Character("Dummy", "Dummy", new Stats(5, 0, 1, 0));
        game.Join(mage);
        game.Join(dummy);
        game.Attack(mage, dummy);

        // Act
        game.Attack(dummy, mage);

        // Assert
        Assert.Equal(70, mage.Health);
        Assert.Equal("Dummy is defeated and cannot act.", output.LastLine);
    }

    [Fact]
    public void CharacterOutsideTheFight_CannotAct()
    {
        // Arrange
        var arena = new Arena();
        var stranger = Arena.Outsider();

        // Act
        arena.Game.Attack(stranger, arena.Warrior);

        // Assert
        Assert.Equal(120, arena.Warrior.Health);
        Assert.Equal("Stranger does not take part in this fight.", arena.Output.LastLine);
    }
}
