using ClassSystems.Core.Text;
using ClassSystems.Core.Text.Display;
using ClassSystems.Core.Text.Models;
using ClassSystems.Infra.Data;
using ClassSystems.Tests.Doubles;

namespace ClassSystems.Tests.Core;

/// <summary>
/// The text system: building a document, operations on its tree, rendering and saving.
/// </summary>
public class TextTests
{
    private static readonly string Nl = Environment.NewLine;

    // Report
    //   Intro: paragraph
    //   Body: paragraph, Details: link, image
    private static Document SampleDocument() => new TextFactory("Report", new FakeDisplayer())
        .AddHeading("Intro").AddParagraph("Hello").Up()
        .AddHeading("Body").AddParagraph("Main text")
        .AddHeading("Details").AddLink("Docs", "https://example.com").AddImage("Logo", "logo.png")
        .Build();

    [Fact]
    public void Factory_BuildsNestedSections()
    {
        // Act
        Document document = SampleDocument();

        // Assert
        Assert.Equal(new[] { "Intro", "Body" }, document.Children.Cast<ITextSection>().Select(s => s.Title));
        var body = (Heading)document.Children[1];
        var details = (Heading)body.Children[1];
        Assert.IsType<Link>(details.Children[0]);
        Assert.IsType<Image>(details.Children[1]);
    }

    [Fact]
    public void Factory_UpAtTheTop_Throws()
    {
        var factory = new TextFactory("Empty", new FakeDisplayer());

        Assert.Throws<InvalidOperationException>(() => factory.Up());
    }

    [Fact]
    public void Factory_PrintAndTableOfContents_UseTheDisplayer()
    {
        // Arrange
        var output = new MockDisplayer();
        var factory = new TextFactory("Report", output).AddHeading("Intro").AddParagraph("Hello");

        // Act
        factory.Print().PrintTableOfContents();

        // Assert
        Assert.Equal(2, output.DisplayableCalls);
        Assert.Equal(0, output.TextCalls);
        Assert.Equal($"# Report{Nl}{Nl}## Intro{Nl}{Nl}Hello", output.Lines[0]);
        Assert.Equal($"Report{Nl}  Intro", output.Lines[1]);
    }

    [Fact]
    public void TextDto_RendersMarkdownWithLevelsByNesting()
    {
        // Act
        string markdown = new TextDto(SampleDocument()).Render();

        // Assert
        Assert.Contains($"# Report{Nl}", markdown);
        Assert.Contains($"## Body{Nl}", markdown);
        Assert.Contains($"### Details{Nl}", markdown);
        Assert.Contains("[Docs](https://example.com)", markdown);
        Assert.EndsWith("![Logo](logo.png)", markdown);
    }

    [Fact]
    public void TextDto_TitleOnly_ShowsJustTheHeadingLine()
    {
        var body = (Heading)SampleDocument().Children[1];

        Assert.Equal("### Body", new TextDto(body, depth: 2, titleOnly: true).Render());
    }

    [Fact]
    public void TableOfContents_IndentsNestedSections()
    {
        string toc = new TableOfContentsDto(SampleDocument()).Render();

        Assert.Equal($"Report{Nl}  Intro{Nl}  Body{Nl}    Details", toc);
    }

    [Fact]
    public void Insert_SectionIntoItsOwnChild_Throws()
    {
        // Arrange
        var outer = new Heading("Outer");
        var inner = new Heading("Inner");
        TextTree.Add(outer, inner);

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => TextTree.Add(inner, outer));
        Assert.Throws<InvalidOperationException>(() => TextTree.Add(outer, outer));
    }

    [Fact]
    public void Remove_DeletesExactlyThisObject_NotAnEqualOne()
    {
        // Arrange
        var section = new Heading("Section");
        var first = new Paragraph("Same");
        var second = new Paragraph("Same");
        TextTree.Add(section, first);
        TextTree.Add(section, second);

        // Act
        bool removed = TextTree.Remove(section, second);

        // Assert
        Assert.True(removed);
        Assert.Same(first, Assert.Single(section.Children));
        Assert.False(TextTree.Remove(section, second));
    }

    [Fact]
    public void Move_ChangesTheOrderOfChildren()
    {
        // Arrange
        Document document = SampleDocument();

        // Act
        TextTree.Move(document, 1, 0);

        // Assert
        Assert.Equal("Body", TextTree.NameOf(document.Children[0]));
    }

    [Fact]
    public void ContainsAndPathTo_FindDeepSections()
    {
        // Arrange
        Document document = SampleDocument();
        var body = (Heading)document.Children[1];
        var details = (Heading)body.Children[1];

        // Act
        List<ITextSection>? path = TextTree.PathTo(document, details);

        // Assert
        Assert.True(TextTree.Contains(document, details));
        Assert.False(TextTree.Contains(details, body));
        Assert.Equal(new ITextSection[] { document, body, details }, path);
        Assert.Null(TextTree.PathTo(details, body));
        Assert.Equal(4, TextTree.Sections(document).Count());
    }

    [Theory]
    [InlineData(0, "heading")]
    [InlineData(1, "heading")]
    public void KindOf_ReturnsTheLowerCaseTypeName(int index, string expected)
    {
        Assert.Equal(expected, TextTree.KindOf(SampleDocument().Children[index]));
        Assert.Equal("document", TextTree.KindOf(SampleDocument()));
    }

    [Fact]
    public void SaveMapper_RoundTrip_KeepsTheWholeDocument()
    {
        // Arrange
        Document original = SampleDocument();

        // Act
        TextSave save = TextSaveMapper.ToSave(original, "draft");
        Document restored = TextSaveMapper.FromSave(save);

        // Assert
        Assert.Equal("draft", save.SaveName);
        Assert.Equal(new TextDto(original).Render(), new TextDto(restored).Render());
    }

    [Fact]
    public void SaveMapper_SaveWithoutDocument_Throws()
    {
        var empty = new TextSave { SaveName = "empty" };

        var error = Assert.Throws<InvalidOperationException>(() => TextSaveMapper.FromSave(empty));
        Assert.Contains("empty", error.Message);
    }
}
