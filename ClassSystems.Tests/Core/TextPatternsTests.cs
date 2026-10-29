using ClassSystems.Cli.Commands;
using ClassSystems.Cli.Modes;
using ClassSystems.Cli.Presenter;
using ClassSystems.Core.Text;
using ClassSystems.Core.Text.Display;
using ClassSystems.Core.Text.Formats;
using ClassSystems.Core.Text.Models;
using ClassSystems.Core.Text.Templates;
using ClassSystems.Infra.Data;
using ClassSystems.Tests.Doubles;

namespace ClassSystems.Tests.Core;

/// <summary>
/// Generative patterns of the text system: Abstract Factory of formats, Singleton factories,
/// Prototype elements and the template library.
/// </summary>
public class TextPatternsTests
{
    private static readonly string Nl = Environment.NewLine;

    // Notes & Co
    //   Intro: "A < B"
    //     Docs: link, image
    private static Document Sample() => new TextFactory("Notes & Co", new FakeDisplayer())
        .AddHeading("Intro").AddParagraph("A < B")
        .AddHeading("Docs").AddLink("Site", "https://example.com/?a=1&b=2").AddImage("Logo", "logo.png")
        .Build();

    // ---- Abstract Factory --------------------------------------------------------------------

    [Fact]
    public void Markdown_IsTheDefaultAndRendersAsBefore()
    {
        string expected = string.Join(Nl + Nl,
            "# Notes & Co", "## Intro", "A < B", "### Docs", "[Site](https://example.com/?a=1&b=2)", "![Logo](logo.png)");

        Assert.Equal(expected, new TextDto(Sample()).Render());
        Assert.Equal(expected, new TextDto(Sample(), format: MarkdownFormat.Instance).Render());
    }

    [Fact]
    public void Html_RendersAFullEncodedPage()
    {
        string html = new TextDto(Sample(), format: HtmlFormat.Instance).Render();

        Assert.Equal(string.Join(Nl,
            "<!DOCTYPE html>",
            "<html>",
            "<head><meta charset=\"utf-8\"><title>Notes &amp; Co</title></head>",
            "<body>",
            "<h1>Notes &amp; Co</h1>",
            "<h2>Intro</h2>",
            "<p>A &lt; B</p>",
            "<h3>Docs</h3>",
            "<p><a href=\"https://example.com/?a=1&amp;b=2\">Site</a></p>",
            "<figure><img src=\"logo.png\" alt=\"Logo\"><figcaption>Logo</figcaption></figure>",
            "</body>",
            "</html>"), html);
    }

    [Fact]
    public void Html_SectionIsRenderedWithoutThePageFrame()
    {
        var intro = (Heading)Sample().Children[0];

        string html = new TextDto(intro, depth: 1, format: HtmlFormat.Instance).Render();

        Assert.StartsWith("<h2>Intro</h2>", html);
        Assert.DoesNotContain("<html>", html);
    }

    [Fact]
    public void Html_DeepHeadingsStopAtLevelSix()
    {
        var deep = new Heading("Deep");

        Assert.Equal("<h6>Deep</h6>", new TextDto(deep, depth: 9, titleOnly: true, format: HtmlFormat.Instance).Render());
    }

    [Fact]
    public void PlainText_UnderlinesTitlesAndWritesLinksInWords()
    {
        string text = new TextDto(Sample(), format: PlainTextFormat.Instance).Render();

        Assert.StartsWith($"Notes & Co{Nl}=========={Nl}{Nl}Intro{Nl}-----", text);
        Assert.Contains($"{Nl}{Nl}Docs{Nl}{Nl}", text); // third level is not underlined
        Assert.Contains("Site (https://example.com/?a=1&b=2)", text);
        Assert.EndsWith("[Image: Logo — logo.png]", text);
    }

    [Fact]
    public void EveryFormat_ProvidesTheWholeFamilyOfProducts()
    {
        foreach (ITextFormat format in TextFormats.All)
        {
            Assert.NotNull(format.CreateTitleRenderer());
            Assert.NotNull(format.CreateParagraphRenderer());
            Assert.NotNull(format.CreateLinkRenderer());
            Assert.NotNull(format.CreateImageRenderer());
            Assert.NotNull(format.CreateLayout());
            Assert.NotEmpty(new TextDto(Sample(), format: format).Render());
        }
    }

    [Fact]
    public void NewFormat_WorksWithoutChangingTheRenderingCode()
    {
        // A format written only for this test: upper-case titles, everything else as in Markdown.
        string text = new TextDto(Sample(), format: new ShoutingFormat()).Render();

        Assert.StartsWith($"NOTES & CO{Nl}{Nl}INTRO{Nl}{Nl}A < B", text);
    }

    // ---- Singleton ---------------------------------------------------------------------------

    [Fact]
    public void Formats_AreSingletons()
    {
        Assert.Same(MarkdownFormat.Instance, MarkdownFormat.Instance);
        Assert.Same(HtmlFormat.Instance, TextFormats.Find("html"));
        Assert.Same(TextFormats.Default, TextFormats.Find("MD"));
        Assert.DoesNotContain(typeof(HtmlFormat).GetConstructors(), constructor => constructor.IsPublic);
    }

    [Theory]
    [InlineData("md", "md")]
    [InlineData(".htm", "html")]
    [InlineData("TEXT", "text")]
    [InlineData("txt", "text")]
    public void Find_ByNameOrExtension(string key, string expected)
    {
        Assert.Equal(expected, TextFormats.Find(key).Name);
    }

    [Fact]
    public void Find_UnknownFormat_Throws()
    {
        var error = Assert.Throws<ArgumentException>(() => TextFormats.Find("pdf"));
        Assert.Contains("md, html, text", error.Message);
    }

    [Theory]
    [InlineData("out/report.html", "html")]
    [InlineData("notes.TXT", "text")]
    [InlineData("readme.md", "md")]
    [InlineData("no-extension", "md")]
    [InlineData("data.json", "md")]
    public void ForFile_ChoosesTheFormatByExtension(string file, string expected)
    {
        Assert.Equal(expected, TextFormats.ForFile(file).Name);
    }

    // ---- Prototype ---------------------------------------------------------------------------

    [Fact]
    public void DeepCopy_OfASection_CopiesTheWholeSubtree()
    {
        // Arrange
        Document original = Sample();

        // Act
        Document copy = original.DeepCopy();

        // Assert
        Assert.NotSame(original, copy);
        Assert.Equal(new TextDto(original).Render(), new TextDto(copy).Render());
        var originalIntro = (Heading)original.Children[0];
        var copiedIntro = (Heading)copy.Children[0];
        Assert.NotSame(originalIntro, copiedIntro);
        Assert.NotSame(originalIntro.Children, copiedIntro.Children);
        Assert.NotSame(originalIntro.Children[0], copiedIntro.Children[0]);
    }

    [Fact]
    public void DeepCopy_ChangingTheCopyDoesNotChangeTheOriginal()
    {
        // Arrange
        Document original = Sample();
        Document copy = original.DeepCopy();

        // Act
        TextTree.Add((Heading)copy.Children[0], new Paragraph("only in the copy"));
        copy.Children.RemoveAt(0);

        // Assert
        Assert.Single(original.Children);
        Assert.Equal(2, ((Heading)original.Children[0]).Children.Count); // the paragraph and "Docs", as before
        Assert.DoesNotContain("only in the copy", new TextDto(original).Render());
    }

    [Fact]
    public void DeepCopy_ThroughTheInterfaceKeepsTheType()
    {
        ITextElement[] elements = { new Paragraph("p"), new Link("l", "u"), new Image("i", "p"), new Heading("h"), new Document("d") };

        foreach (ITextElement element in elements)
        {
            ITextElement copy = element.DeepCopy();
            Assert.IsType(element.GetType(), copy);
            Assert.NotSame(element, copy);
        }
    }

    // ---- Template library (prototype registry) ----------------------------------------------

    [Fact]
    public void Templates_DefaultsAreAvailable()
    {
        Assert.Equal(new[] { "chapter", "figure", "reference" }, TemplateLibrary.WithDefaults().Names);
    }

    [Fact]
    public void Templates_EveryCreatedElementIsNew()
    {
        // Arrange
        TemplateLibrary library = TemplateLibrary.WithDefaults();

        // Act
        var first = (Heading)library.Create("chapter");
        var second = (Heading)library.Create("Chapter");
        first.Children.Clear();

        // Assert
        Assert.NotSame(first, second);
        Assert.Equal(3, second.Children.Count);
        Assert.Equal(3, ((Heading)library.Create("chapter")).Children.Count);
    }

    [Fact]
    public void Templates_ChangingTheSourceAfterRegistrationDoesNotChangeTheTemplate()
    {
        // Arrange
        var library = new TemplateLibrary();
        var section = new Heading("Mine", new List<ITextElement> { new Paragraph("first") });
        library.Register("mine", section);

        // Act
        section.Children.Add(new Paragraph("added later"));

        // Assert
        Assert.Single(((Heading)library.Create("mine")).Children);
    }

    [Fact]
    public void Templates_UnknownOrEmptyName_Throws()
    {
        TemplateLibrary library = TemplateLibrary.WithDefaults();

        Assert.Throws<ArgumentException>(() => library.Create("poem"));
        Assert.Throws<ArgumentException>(() => library.Register(" ", new Paragraph("x")));
    }

    // ---- terminal commands -------------------------------------------------------------------

    private static (string Output, Document Document) Run(params string[] lines)
    {
        var terminal = new ScriptedTerminal(lines);
        var document = new Document("Report");
        var mode = new TextMode(terminal, new Dialog(terminal), new SaveStore(new StubStorage()), document);
        var registry = new CommandRegistry();
        mode.RegisterCommands(registry);
        new CommandShell(terminal, registry, new CommandParser()).Run();
        return (terminal.Output, document);
    }

    [Fact]
    public void Commands_TemplateAddCopyAndPrintAsHtml()
    {
        // Act
        var (output, document) = Run(
            "template add chapter",
            "copy \"New chapter\"",
            "template list",
            "print --whole --format html");

        // Assert
        Assert.Equal(2, document.Children.Count);
        Assert.NotSame(document.Children[0], document.Children[1]);
        Assert.Contains("Added heading #1 \"New chapter\" to /", output);
        Assert.Contains("Copied heading \"New chapter\" as #2", output);
        Assert.Contains("reference    link      Source", output);
        Assert.Contains("<h1>Report</h1>", output);
        Assert.Contains("<h2>New chapter</h2>", output);
    }

    [Fact]
    public void Commands_SaveAnElementAsANewTemplate()
    {
        var (output, document) = Run(
            "template save note",
            "template add note");

        Assert.Contains("Saved document \"Report\" as template \"note\".", output);
        Assert.IsType<Document>(Assert.Single(document.Children));
    }

    [Fact]
    public void Commands_ExportChoosesTheFormatByExtension()
    {
        // Arrange
        string file = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.html");

        try
        {
            // Act
            var (output, _) = Run("template add figure", $"export \"{file}\" --whole");

            // Assert
            Assert.Contains("(html)", output);
            Assert.StartsWith("<!DOCTYPE html>", File.ReadAllText(file));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Commands_UnknownFormatIsReported()
    {
        var (output, _) = Run("print --format pdf");

        Assert.Contains("Error: Unknown format \"pdf\"", output);
    }

    // A format created only in the tests: shows that a new family plugs in without changing TextDto.
    private sealed class ShoutingFormat : ITextFormat
    {
        public string Name => "shout";

        public IReadOnlyList<string> Extensions => Array.Empty<string>();

        public ITitleRenderer CreateTitleRenderer() => new UpperTitle();

        public IBlockRenderer<Paragraph> CreateParagraphRenderer() => MarkdownFormat.Instance.CreateParagraphRenderer();

        public IBlockRenderer<Link> CreateLinkRenderer() => MarkdownFormat.Instance.CreateLinkRenderer();

        public IBlockRenderer<Image> CreateImageRenderer() => MarkdownFormat.Instance.CreateImageRenderer();

        public IDocumentLayout CreateLayout() => MarkdownFormat.Instance.CreateLayout();

        private sealed class UpperTitle : ITitleRenderer
        {
            public string Render(ITextSection section, int depth) => section.Title.ToUpperInvariant();
        }
    }
}
