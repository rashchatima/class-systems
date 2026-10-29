using ClassSystems.Core.Characters;
using ClassSystems.Core.Characters.Models;
using ClassSystems.Core.Characters.Rules;
using ClassSystems.Infra.Display;

namespace ClassSystems.Cli.Demos;

/// <summary>
/// Demonstration of the game system: a scripted fight between several characters.
/// </summary>
public static class GameDemo
{
    public static void Run(IDisplayer displayer)
    {
        var game = new Game(displayer);

        Character warrior = CharacterClasses.Create("warrior", "Bjorn");
        Character mage = CharacterClasses.Create("mage", "Elara");
        Character priest = CharacterClasses.Create("priest", "Tomas");

        game.Join(warrior);
        game.Join(mage);
        game.Join(priest);

        var shield = new Item("Tower Shield", HealthBonus: 20, ArmorBonus: 6);
        var dagger = new Item("Vampiric Dagger", AttackBonus: 4, GrantedAbility: AbilityCatalog.Create("life-drain"));

        displayer.Display("=========== BATTLE ===========");
        game.ReportStatus();

        game.Attack(warrior, mage);
        game.UseAbility(mage, "Fireball", warrior);
        game.Heal(priest, mage);

        // Items change the stats and can give an ability the character did not have.
        game.UseAbility(mage, "Life Drain", warrior);
        game.Equip(warrior, shield);
        game.Equip(mage, dagger);
        game.ReportStatus();

        game.Defend(mage);
        game.UseAbility(warrior, "Power Strike", mage);
        game.UseAbility(mage, "Life Drain", warrior);
        game.UseAbility(priest, "Holy Light", mage);
        game.Attack(warrior, priest);
        game.Unequip(warrior, shield);
        game.ReportStatus();
    }
}
