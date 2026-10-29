using ClassSystems.Core.Characters.Models;
using ClassSystems.Core.Characters.Rules;
using ClassSystems.Tests.Doubles;

namespace ClassSystems.Tests.Core;

/// <summary>
/// Putting items on and taking them off.
/// </summary>
public class EquipmentTests
{
    private static Item Armor() => new("Armor of Light", HealthBonus: 20, ArmorBonus: 3, AttackBonus: 5);

    [Fact]
    public void Equip_AddsAllBonuses_AndFillsExtraHealth()
    {
        // Arrange
        var arena = new Arena();
        Item armor = Armor();

        // Act
        arena.Game.Equip(arena.Warrior, armor);

        // Assert
        Assert.Equal(140, arena.Warrior.MaxHealth);
        Assert.Equal(140, arena.Warrior.Health);
        Assert.Equal(11, arena.Warrior.Armor);
        Assert.Equal(21, arena.Warrior.AttackPower);
        Assert.Same(arena.Warrior, arena.Game.OwnerOf(armor));
        Assert.Contains(armor, arena.Game.Items);
    }

    [Fact]
    public void Equip_ItemWornBySomeoneElse_IsRejected()
    {
        // Arrange
        var arena = new Arena();
        Item armor = Armor();
        arena.Game.Equip(arena.Warrior, armor);

        // Act
        arena.Game.Equip(arena.Mage, armor);

        // Assert
        Assert.Empty(arena.Mage.Equipment);
        Assert.Equal("Armor of Light is already equipped by Conan.", arena.Output.LastLine);
    }

    [Fact]
    public void TwoEqualItems_CanBeWornTogether()
    {
        // Arrange
        var arena = new Arena();
        var first = new Item("Sword", AttackBonus: 5);
        var second = new Item("Sword", AttackBonus: 5);

        // Act
        arena.Game.Equip(arena.Warrior, first);
        arena.Game.Equip(arena.Warrior, second);

        // Assert
        Assert.NotEqual(first, second);
        Assert.Equal(26, arena.Warrior.AttackPower);
    }

    [Fact]
    public void GrantedAbility_IsAvailableOnlyWhileTheItemIsWorn()
    {
        // Arrange
        var arena = new Arena();
        Ability fireball = AbilityCatalog.Create("fireball");
        var staff = new Item("Staff", GrantedAbility: fireball);
        arena.Game.Equip(arena.Warrior, staff);

        // Act
        arena.Game.UseAbility(arena.Warrior, "Fireball", arena.Mage);
        arena.Game.Unequip(arena.Warrior, staff);

        // Assert: fireball takes 16 from the mage and disappears with the staff
        Assert.Equal(54, arena.Mage.Health);
        Assert.DoesNotContain(fireball, arena.Warrior.Abilities);
        Assert.Contains(fireball, arena.Game.Abilities);
    }

    [Fact]
    public void Unequip_CutsHealthToTheNewMaximum()
    {
        // Arrange
        var arena = new Arena();
        Item armor = Armor();
        arena.Game.Equip(arena.Warrior, armor);

        // Act
        arena.Game.Unequip(arena.Warrior, armor);

        // Assert
        Assert.Equal(120, arena.Warrior.MaxHealth);
        Assert.Equal(120, arena.Warrior.Health);
        Assert.Null(arena.Game.OwnerOf(armor));
    }

    [Fact]
    public void Unequip_ItemThatIsNotWorn_IsReported()
    {
        // Arrange
        var arena = new Arena();

        // Act
        arena.Game.Unequip(arena.Mage, Armor());

        // Assert
        Assert.Equal("Merlin does not have Armor of Light equipped.", arena.Output.LastLine);
    }
}
