using ClassSystems.Core.Text;
using ClassSystems.Core.Text.Models;
using ClassSystems.Infra.Display;

namespace ClassSystems.Cli.Demos;

/// <summary>
/// Demonstration of the text system: builds an article and prints it with its table of contents.
/// </summary>
public static class TextDemo
{
    public static void Run(IDisplayer displayer)
    {
        // The factory keeps track of the current section, so the nesting follows the order of calls.
        var factory = new TextFactory("The History of Chess", displayer)
            .AddParagraph("Chess is one of the oldest board games that are still played today.")
            .AddHeading("Origins")
                .AddParagraph("The game appeared in India around the 6th century and was called chaturanga.")
                .AddImage("An ancient chaturanga board", "images/chaturanga.png")
                .Up()
            .AddHeading("Modern Chess")
                .AddHeading("Computers")
                    .AddParagraph("In 1997 the Deep Blue computer defeated the world champion Garry Kasparov.")
                    .Up()
                .AddHeading("Rules")
                    .AddParagraph("The current rules were formed in Europe by the end of the 15th century.")
                    .Up()
                .Up()
            .AddParagraph("TODO: add a section about famous players.")
            .AddLink("Read more on Wikipedia", "https://en.wikipedia.org/wiki/History_of_chess");

        // Editing the built article: "Rules" goes before "Computers", the draft note is removed.
        Document article = factory.Build();
        Heading modernChess = article.Children.OfType<Heading>().First(heading => heading.Title == "Modern Chess");
        TextTree.Move(modernChess, fromIndex: 1, toIndex: 0);

        Paragraph draftNote = article.Children.OfType<Paragraph>().First(paragraph => paragraph.Content.StartsWith("TODO"));
        TextTree.Remove(article, draftNote);

        displayer.Display("========== ARTICLE ==========");
        factory.Print();
        displayer.Display(string.Empty);
        displayer.Display("===== TABLE OF CONTENTS =====");
        factory.PrintTableOfContents();
    }
}
