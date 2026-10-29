using ClassSystems.Core.Characters.Models;
using ClassSystems.Core.Characters.Remote;
using ClassSystems.Remote.Models;
using ClassSystems.Tests.Doubles;

namespace ClassSystems.Tests.Remote;

/// <summary>
/// Translation of the remote data into the classes of the game.
/// </summary>
public class RemoteMapperTests(GenshinApiFixture fixture) : IClassFixture<GenshinApiFixture>
{
    [Fact]
    public void ToCharacter_KeepsTheBackgroundInTheProfile()
    {
        // Act
        Character diluc = RemoteMapper.ToCharacter(fixture.Character("diluc"));

        // Assert
        Assert.Equal("Diluc", diluc.Name);
        Assert.Equal("Diluc", diluc.ClassName);
        Assert.Equal(new CharacterProfile("The Dark Side of Dawn", "Pyro", "Claymore", "Mondstadt", 5,
            "The tycoon of a winery empire in Mondstadt, unmatched in every possible way."), diluc.Profile);
        Assert.Equal(diluc.MaxHealth, diluc.Health);
    }

    [Fact]
    public void ToCharacter_CanGiveAnotherName()
    {
        Character hero = RemoteMapper.ToCharacter(fixture.Character("diluc"), "Ragnvindr");

        Assert.Equal("Ragnvindr", hero.Name);
        Assert.Equal("Diluc", hero.ClassName);
    }

    [Fact]
    public void ToCharacter_TalentsBecomeAbilities()
    {
        Character diluc = RemoteMapper.ToCharacter(fixture.Character("diluc"));

        Assert.Equal(new[] { "Tempered Sword", "Searing Onslaught", "Dawn" }, diluc.LearnedAbilities.Select(a => a.Name));
        Assert.Equal(new[] { "power-strike", "fireball", "life-drain" }, diluc.LearnedAbilities.Select(a => a.Effect));
        Assert.Equal("Performs up to 4 consecutive strikes.", diluc.LearnedAbilities[0].Description);
    }

    [Theory]
    [InlineData("NORMAL_ATTACK", "power-strike")]
    [InlineData("ELEMENTAL_SKILL", "fireball")]
    [InlineData("ELEMENTAL_BURST", "life-drain")]
    [InlineData(null, "power-strike")]
    public void ToAbility_TalentTypeChoosesTheEffect(string? type, string effect)
    {
        Ability ability = RemoteMapper.ToAbility(new RemoteTalent("Talent", null, "Does something.", type));

        Assert.Equal(effect, ability.Effect);
    }

    [Fact]
    public void ToAbility_LongDescriptionIsCut()
    {
        Ability ability = RemoteMapper.ToAbility(new RemoteTalent("Long", null, new string('a', 500) + ".", "ELEMENTAL_BURST"));

        Assert.Equal(220, ability.Description!.Length);
        Assert.EndsWith("…", ability.Description);
    }

    [Theory]
    [InlineData("diluc")]
    [InlineData("xiangling")]
    public void Stats_StayWithinTheRangeOfTheRarity(string id)
    {
        // Arrange
        RemoteCharacter data = fixture.Character(id);
        StatRange range = data.Rarity >= 5 ? StatRoller.FiveStar : StatRoller.FourStar;

        // Act
        Stats stats = RemoteMapper.ToCharacter(data).BaseStats;

        // Assert
        Assert.InRange(stats.MaxHealth, range.Health.Min, range.Health.Max);
        Assert.InRange(stats.Armor, range.Armor.Min, range.Armor.Max);
        Assert.InRange(stats.AttackPower, range.Attack.Min, range.Attack.Max);
        Assert.InRange(stats.HealPower, range.Heal.Min, range.Heal.Max);
    }

    [Fact]
    public void Stats_AreTheSameForTheSameCharacterEveryTime()
    {
        Assert.Equal(StatRoller.For("diluc", 5), StatRoller.For("Diluc", 5));
        Assert.Equal(StatRoller.For("xiangling", 4), RemoteMapper.ToCharacter(fixture.Character("xiangling")).BaseStats);
    }

    [Fact]
    public void ToItem_WeaponWithPassive()
    {
        // Act
        Item claymore = RemoteMapper.ToItem(fixture.Weapon("wolf-s-gravestone"));

        // Assert: 46 / 4 = 11, +2 for the ATK secondary stat
        Assert.Equal("Wolf's Gravestone", claymore.Name);
        Assert.Equal("Claymore", claymore.Kind);
        Assert.Equal(13, claymore.AttackBonus);
        Assert.Equal("Wolfish Tracker", claymore.GrantedAbility!.Name);
        Assert.Equal("power-strike", claymore.GrantedAbility.Effect);
        Assert.Equal("★★★★★ Claymore, base ATK 46, secondary stat ATK", claymore.Description);
    }

    [Fact]
    public void ToItem_WeaponWithoutPassive()
    {
        Item sword = RemoteMapper.ToItem(fixture.Weapon("dull-blade"));

        Assert.Null(sword.GrantedAbility);
        Assert.Equal(5, sword.AttackBonus);
        Assert.Equal("★ Sword, base ATK 23", sword.Description);
    }

    [Theory]
    [InlineData("HP", 15, 0)]
    [InlineData("DEF", 0, 3)]
    public void ToItem_SecondaryStatGivesItsBonus(string subStat, int health, int armor)
    {
        Item item = RemoteMapper.ToItem(new RemoteWeapon("w", "W", "Sword", 4, 40, subStat, "-", "-"));

        Assert.Equal(health, item.HealthBonus);
        Assert.Equal(armor, item.ArmorBonus);
        Assert.Equal(10, item.AttackBonus);
    }

    [Fact]
    public void ToItem_CatalystPassiveIsAFireball()
    {
        Item book = RemoteMapper.ToItem(new RemoteWeapon("b", "Book", "Catalyst", 4, 42, "-", "Magic", "Burns."));

        Assert.Equal("fireball", book.GrantedAbility!.Effect);
    }
}
