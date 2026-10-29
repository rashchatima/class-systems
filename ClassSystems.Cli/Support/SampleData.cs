using ClassSystems.Core.Characters;
using ClassSystems.Core.Characters.Models;
using ClassSystems.Core.Characters.Rules;
using ClassSystems.Core.Text;
using ClassSystems.Core.Text.Models;
using ClassSystems.Infra.Display;

namespace ClassSystems.Cli.Support;

/// <summary>
/// Ready objects for trying the command interface without creating everything by hand (<c>--sample</c>).
/// </summary>
public static class SampleData
{
    /// <summary>
    /// The chess article from the previous practical works.
    /// </summary>
    public static Document Article(IDisplayer displayer) => new TextFactory("The History of Chess", displayer)
        .AddParagraph("Chess is one of the oldest board games that are still played today.")
        .AddHeading("Origins")
            .AddParagraph("The game appeared in India around the 6th century and was called chaturanga.")
            .AddImage("An ancient chaturanga board", "images/chaturanga.png")
            .Up()
        .AddHeading("Modern Chess")
            .AddHeading("Rules")
                .AddParagraph("The current rules were formed in Europe by the end of the 15th century.")
                .Up()
            .AddHeading("Computers")
                .AddParagraph("In 1997 the Deep Blue computer defeated the world champion Garry Kasparov.")
                .Up()
            .Up()
        .AddLink("Read more on Wikipedia", "https://en.wikipedia.org/wiki/History_of_chess")
        .Build();

    /// <summary>
    /// Three characters and two items from the previous practical works.
    /// </summary>
    public static void Fill(Game game)
    {
        game.Join(CharacterClasses.Create("warrior", "Bjorn"));
        game.Join(CharacterClasses.Create("mage", "Elara"));
        game.Join(CharacterClasses.Create("priest", "Tomas"));

        game.AddItem(new Item("Tower Shield", HealthBonus: 20, ArmorBonus: 6));
        game.AddItem(new Item("Vampiric Dagger", AttackBonus: 4, GrantedAbility: AbilityCatalog.Create("life-drain")));
    }
}
