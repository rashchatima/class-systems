using ClassSystems.Core.Characters.Models;
using ClassSystems.Core.Characters.Rules;
using ClassSystems.Tests.Doubles;

namespace ClassSystems.Tests.Core;

/// <summary>
/// The catalog of abilities and the effect of every ability in a fight.
/// </summary>
public class AbilityTests
{
    [Theory]
    [InlineData("power-strike", "Power Strike")]
    [InlineData("fireball", "Fireball")]
    [InlineData("holy-light", "Holy Light")]
    [InlineData("life-drain", "Life Drain")]
    [InlineData("FIREBALL", "Fireball")]
    public void Create_KnownEffect_ReturnsNamedAbility(string effect, string expectedName)
    {
        // Act
        Ability ability = AbilityCatalog.Create(effect);

        // Assert
        Assert.Equal(expectedName, ability.Name);
        Assert.Equal(effect.ToLowerInvariant(), ability.Effect);
    }

    [Fact]
    public void Create_UnknownEffect_Throws()
    {
        var error = Assert.Throws<ArgumentException>(() => AbilityCatalog.Create("teleport"));
        Assert.Contains("teleport", error.Message);
    }

    [Fact]
    public void AbilitiesWithEqualData_AreDifferentObjects()
    {
        // Arrange
        Ability first = AbilityCatalog.Create("fireball");
        Ability second = AbilityCatalog.Create("fireball");

        // Assert
        Assert.NotEqual(first, second);
        Assert.Equal(first, first);
    }

    [Fact]
    public void PowerStrike_DealsDoubleAttackReducedByArmor()
    {
        // Arrange
        var arena = new Arena();

        // Act
        arena.Game.UseAbility(arena.Warrior, "Power Strike", arena.Mage);

        // Assert: 16 * 2 - 2 = 30
        Assert.Equal(40, arena.Mage.Health);
    }

    [Fact]
    public void Fireball_IgnoresArmor()
    {
        // Arrange
        var arena = new Arena();

        // Act
        arena.Game.UseAbility(arena.Mage, "Fireball", arena.Warrior);

        // Assert: full 20 damage despite 8 armor
        Assert.Equal(100, arena.Warrior.Health);
        Assert.Contains("ignoring armor", arena.Output.LastLine);
    }

    [Fact]
    public void HolyLight_RestoresOnlyMissingHealth()
    {
        // Arrange
        var arena = new Arena();
        arena.Game.Attack(arena.Mage, arena.Warrior); // 20 - 8 = 12 damage

        // Act
        arena.Game.UseAbility(arena.Priest, "Holy Light", arena.Warrior);

        // Assert
        Assert.Equal(120, arena.Warrior.Health);
        Assert.Contains("restoring 12 health", arena.Output.LastLine);
    }

    [Fact]
    public void LifeDrain_DamagesTargetAndHealsUserByHalf()
    {
        // Arrange
        var arena = new Arena();
        arena.Game.Teach(arena.Mage, AbilityCatalog.Create("life-drain"));
        arena.Game.Attack(arena.Warrior, arena.Mage); // mage: 70 -> 56

        // Act
        arena.Game.UseAbility(arena.Mage, "Life Drain", arena.Priest);

        // Assert: 20 - 4 = 16 damage, half of it (8) goes back to the mage
        Assert.Equal(85 - 16, arena.Priest.Health);
        Assert.Equal(56 + 8, arena.Mage.Health);
    }

    [Fact]
    public void UseAbility_UnknownName_IsReported()
    {
        // Arrange
        var arena = new Arena();

        // Act
        arena.Game.UseAbility(arena.Warrior, "Fireball", arena.Mage);

        // Assert
        Assert.Equal(70, arena.Mage.Health);
        Assert.Equal("Conan does not know how to use Fireball.", arena.Output.LastLine);
    }

    [Fact]
    public void UseAbility_ForeignAbilityObject_IsRejected()
    {
        // Arrange
        var arena = new Arena();
        Ability foreign = AbilityCatalog.Create("power-strike"); // same name, but not the warrior's own

        // Act
        arena.Game.UseAbility(arena.Warrior, foreign, arena.Mage);

        // Assert
        Assert.Equal(70, arena.Mage.Health);
        Assert.Equal("Conan does not have the ability Power Strike.", arena.Output.LastLine);
    }

    [Fact]
    public void Teach_SameAbilityTwice_IsReportedOnce()
    {
        // Arrange
        var arena = new Arena();
        Ability drain = AbilityCatalog.Create("life-drain");

        // Act
        arena.Game.Teach(arena.Warrior, drain);
        arena.Game.Teach(arena.Warrior, drain);

        // Assert
        Assert.Equal(2, arena.Warrior.LearnedAbilities.Count); // innate Power Strike + Life Drain
        Assert.Equal("Conan already knows this Life Drain.", arena.Output.LastLine);
    }
}
