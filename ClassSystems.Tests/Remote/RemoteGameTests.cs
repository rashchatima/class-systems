using ClassSystems.Cli.Commands;
using ClassSystems.Cli.Modes;
using ClassSystems.Cli.Presenter;
using ClassSystems.Core.Characters;
using ClassSystems.Core.Characters.Display;
using ClassSystems.Core.Characters.Models;
using ClassSystems.Core.Characters.Remote;
using ClassSystems.Core.Characters.Rules;
using ClassSystems.Core.Text.Models;
using ClassSystems.Infra.Data;
using ClassSystems.Remote;
using ClassSystems.Tests.Doubles;

namespace ClassSystems.Tests.Remote;

/// <summary>
/// Characters from the remote database in the game: weapon rules, the character sheet, saving
/// and the commands of the terminal.
/// </summary>
public class RemoteGameTests(GenshinApiFixture fixture) : IClassFixture<GenshinApiFixture>
{
    private (Game Game, MockDisplayer Output, Character Diluc) DilucInGame()
    {
        var output = new MockDisplayer();
        var game = new Game(output);
        Character diluc = RemoteMapper.ToCharacter(fixture.Character("diluc"));
        game.Join(diluc);
        return (game, output, diluc);
    }

    // ---- weapon rule -------------------------------------------------------------------------

    [Fact]
    public void Equip_WeaponOfTheRightType()
    {
        // Arrange
        var (game, _, diluc) = DilucInGame();
        int attackBefore = diluc.AttackPower;
        Item claymore = RemoteMapper.ToItem(fixture.Weapon("wolf-s-gravestone"));

        // Act
        game.Equip(diluc, claymore);

        // Assert
        Assert.Same(claymore, Assert.Single(diluc.Equipment));
        Assert.Equal(attackBefore + 13, diluc.AttackPower);
        Assert.Contains(claymore.GrantedAbility!, diluc.Abilities);
    }

    [Fact]
    public void Equip_WeaponOfAnotherType_IsRejected()
    {
        // Arrange
        var (game, output, diluc) = DilucInGame();

        // Act
        game.Equip(diluc, RemoteMapper.ToItem(fixture.Weapon("amos-bow")));

        // Assert
        Assert.Empty(diluc.Equipment);
        Assert.Equal("Diluc can only use Claymore weapons, not Bow.", output.LastLine);
    }

    [Fact]
    public void Equip_ItemsWithoutTypeAndCharactersWithoutProfile_AreNotLimited()
    {
        // Arrange
        var (game, _, diluc) = DilucInGame();
        Character warrior = CharacterClasses.Create("warrior", "Conan");
        game.Join(warrior);
        var ring = new Item("Ring", ArmorBonus: 1);

        // Act
        game.Equip(diluc, ring);
        game.Equip(warrior, RemoteMapper.ToItem(fixture.Weapon("amos-bow")));

        // Assert
        Assert.Single(diluc.Equipment);
        Assert.Single(warrior.Equipment);
    }

    // ---- character sheet ---------------------------------------------------------------------

    [Fact]
    public void Sheet_IsADocumentWithFourSections()
    {
        // Arrange
        var (game, _, diluc) = DilucInGame();
        game.Equip(diluc, RemoteMapper.ToItem(fixture.Weapon("wolf-s-gravestone")));

        // Act
        Document sheet = new CharacterSheet(diluc).Build();

        // Assert
        Assert.Equal("Diluc — The Dark Side of Dawn", sheet.Title);
        Assert.Equal(new[] { "Profile", "Stats", "Equipment", "Abilities" }, sheet.Children.Cast<ITextSection>().Select(s => s.Title));
        var equipment = (Heading)sheet.Children[2];
        Assert.Equal("Wolf's Gravestone", ((Heading)equipment.Children.Single()).Title);
        var abilities = (Heading)sheet.Children[3];
        Assert.Equal(4, abilities.Children.Count); // 3 talents + the passive of the weapon
    }

    [Fact]
    public void Sheet_RendersAsMarkdown()
    {
        // Arrange
        var (game, _, diluc) = DilucInGame();
        game.Equip(diluc, RemoteMapper.ToItem(fixture.Weapon("wolf-s-gravestone")));

        // Act
        string text = new CharacterSheet(diluc).Render();

        // Assert
        Assert.StartsWith("# Diluc — The Dark Side of Dawn", text);
        Assert.Contains("## Stats", text);
        Assert.Contains("### Wolf's Gravestone", text);
        Assert.Contains("Class: Diluc · Element: Pyro · Weapon: Claymore · Nation: Mondstadt · Rarity: ★★★★★", text);
        Assert.Contains($"Health: {diluc.Health}/{diluc.MaxHealth} · Armor: {diluc.Armor} · Attack: {diluc.AttackPower}", text);
        Assert.Contains("Effect in a fight: power-strike (from Wolf's Gravestone)", text);
    }

    [Fact]
    public void Sheet_OfAHandMadeCharacter()
    {
        // Arrange
        var game = new Game(new FakeDisplayer());
        Character warrior = CharacterClasses.Create("warrior", "Conan");
        game.Join(warrior);
        game.Attack(warrior, warrior); // 16 - 8 = 8 damage to itself

        // Act
        string text = new CharacterSheet(warrior).Render();

        // Assert
        Assert.StartsWith("# Conan" + Environment.NewLine, text);
        Assert.Contains("Class: Warrior" + Environment.NewLine, text);
        Assert.DoesNotContain("Element", text);
        Assert.Contains("No equipment.", text);
        Assert.Contains("Health: 112/120", text);
    }

    // ---- saving ------------------------------------------------------------------------------

    [Fact]
    public void Save_KeepsProfileWeaponTypeAndDescriptions()
    {
        // Arrange
        var (game, _, diluc) = DilucInGame();
        game.Equip(diluc, RemoteMapper.ToItem(fixture.Weapon("wolf-s-gravestone")));
        var output = new MockDisplayer();
        var restored = new Game(output);

        // Act
        GameSaveMapper.Restore(restored, GameSaveMapper.ToSave(game, "slot"));

        // Assert
        Character copy = restored.Characters.Single();
        Assert.Equal(diluc.Profile, copy.Profile);
        Assert.Equal(diluc.BaseStats, copy.BaseStats);
        Assert.Equal("Claymore", copy.Equipment.Single().Kind);
        Assert.Equal(diluc.LearnedAbilities[0].Description, copy.LearnedAbilities[0].Description);
        Assert.Equal(new CharacterSheet(diluc).Render(), new CharacterSheet(copy).Render());

        restored.Equip(copy, RemoteMapper.ToItem(fixture.Weapon("amos-bow")));
        Assert.Equal("Diluc can only use Claymore weapons, not Bow.", output.LastLine);
    }

    // ---- terminal commands -------------------------------------------------------------------

    private string RunCommands(IGameDatabase? database, params string[] lines)
    {
        var terminal = new ScriptedTerminal(lines);
        var mode = new CharactersMode(terminal, new Dialog(terminal), new Game(terminal), new SaveStore(new StubStorage()), database);
        var registry = new CommandRegistry();
        mode.RegisterCommands(registry);
        new CommandShell(terminal, registry, new CommandParser()).Run();
        return terminal.Output;
    }

    [Fact]
    public void Commands_RecruitArmAndShowACharacter()
    {
        // Act
        string output = RunCommands(new CachedGameDatabase(fixture.CreateClient().Client),
            "recruit diluc --name Ragnvindr",
            "arm Ragnvindr wolf-s-gravestone",
            "arm Ragnvindr amos-bow",
            "sheet Ragnvindr");

        // Assert
        Assert.Contains("Recruited character #1 Ragnvindr:", output);
        Assert.Contains("Ragnvindr equips Wolf's Gravestone.", output);
        Assert.Contains("Ragnvindr can only use Claymore weapons, not Bow.", output);
        Assert.Contains("# Ragnvindr — The Dark Side of Dawn", output);
        Assert.Contains("### Wolfish Tracker", output);
    }

    [Fact]
    public void Commands_ListTheDatabaseWithAFilter()
    {
        string output = RunCommands(new CachedGameDatabase(fixture.CreateClient().Client),
            "db chars",
            "db weapons --find claymore");

        Assert.Contains("diluc", output);
        Assert.Contains("Xiangling ★★★★ — Pyro, Polearm, Liyue", output);
        Assert.Contains("Wolf's Gravestone ★★★★★ — Claymore, base ATK 46", output);
        Assert.DoesNotContain("Amos' Bow", output);
    }

    [Fact]
    public void Commands_UnknownIdIsReportedAndTheShellGoesOn()
    {
        string output = RunCommands(new CachedGameDatabase(fixture.CreateClient().Client),
            "recruit nobody",
            "recruit xiangling");

        Assert.Contains("Error: \"characters/nobody\" was not found in the game database.", output);
        Assert.Contains("Recruited character #1 Xiangling:", output);
    }

    [Fact]
    public void Commands_WithoutDatabase_AreNotAvailable()
    {
        string output = RunCommands(null, "db chars");

        Assert.Contains("Unknown command \"db\"", output);
    }
}
