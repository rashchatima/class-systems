using ClassSystems.Core.Characters;
using ClassSystems.Core.Characters.Models;
using ClassSystems.Core.Characters.Rules;
using ClassSystems.Infra.Data;
using ClassSystems.Infra.Data.Models;
using ClassSystems.Tests.Doubles;

namespace ClassSystems.Tests.Core;

/// <summary>
/// Saving the world of the game to tables and restoring it back.
/// </summary>
public class GameSaveMapperTests
{
    private static (Arena Arena, Item Staff) PlayedGame()
    {
        var arena = new Arena();
        var staff = new Item("Staff", AttackBonus: 2, GrantedAbility: AbilityCatalog.Create("fireball"));
        arena.Game.Equip(arena.Warrior, staff);
        arena.Game.Teach(arena.Mage, AbilityCatalog.Create("life-drain"));
        arena.Game.Attack(arena.Warrior, arena.Mage); // 18 - 2 = 16 -> 54
        arena.Game.Defend(arena.Mage);
        return (arena, staff);
    }

    [Fact]
    public void ToSave_WritesEveryObjectAsATableRow()
    {
        // Arrange
        var (arena, _) = PlayedGame();

        // Act
        CharacterSave save = GameSaveMapper.ToSave(arena.Game, "slot");

        // Assert
        Assert.Equal("slot", save.SaveName);
        Assert.Equal(3, save.Characters!.Count);
        Assert.Single(save.Items!);
        Assert.Equal(5, save.Abilities!.Count);           // 3 innate + life drain + fireball of the staff
        Assert.Single(save.CharacterItems!);
        Assert.Equal(4, save.CharacterAbilities!.Count);  // granted abilities are not stored as learned
        Assert.NotNull(save.Items![0].AbilityId);
    }

    [Fact]
    public void Restore_RebuildsTheSameWorld()
    {
        // Arrange
        var (arena, _) = PlayedGame();
        CharacterSave save = GameSaveMapper.ToSave(arena.Game, "slot");
        var restored = new Game(new FakeDisplayer());

        // Act
        GameSaveMapper.Restore(restored, save);

        // Assert
        Character warrior = restored.Characters.Single(c => c.Name == "Conan");
        Character mage = restored.Characters.Single(c => c.Name == "Merlin");
        Assert.Equal(54, mage.Health);
        Assert.True(mage.IsDefending);
        Assert.Equal(2, mage.LearnedAbilities.Count);
        Assert.Equal(18, warrior.AttackPower);
        Assert.Same(restored.Items.Single(), warrior.Equipment.Single());
        Assert.Contains(warrior.Equipment.Single().GrantedAbility!, warrior.Abilities);
    }

    [Fact]
    public void Restore_BrokenLink_Throws()
    {
        // Arrange
        var save = new CharacterSave
        {
            SaveName = "broken",
            Characters = new() { new CharacterData(1, "Conan", "Warrior", 120, 8, 16, 5, 120, false) },
            CharacterItems = new() { new CharacterItems(1, 1, ItemId: 99) },
        };

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => GameSaveMapper.Restore(new Game(new FakeDisplayer()), save));

        // Assert
        Assert.Contains("item #99", error.Message);
    }

    [Fact]
    public void Restore_HealthAboveMaximum_IsCut()
    {
        // Arrange
        var save = new CharacterSave
        {
            Characters = new() { new CharacterData(1, "Conan", "Warrior", 120, 8, 16, 5, Health: 999, false) },
        };
        var game = new Game(new FakeDisplayer());

        // Act
        GameSaveMapper.Restore(game, save);

        // Assert
        Assert.Equal(120, game.Characters.Single().Health);
    }
}
