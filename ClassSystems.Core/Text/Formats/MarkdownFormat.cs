using ClassSystems.Core.Text.Models;

namespace ClassSystems.Core.Text.Formats;

/// <summary>
/// Concrete factory of the Markdown family: <c># Title</c>, <c>[label](url)</c>, <c>![description](path)</c>.
/// This is the format the program has always used, so the output did not change.
/// </summary>
/// <remarks>
/// <b>Singleton.</b> The factory has no state, so one object is enough for the whole program:
/// the constructor is private and the only instance is <see cref="Instance"/>.
/// </remarks>
public sealed class MarkdownFormat : ITextFormat
{
    private MarkdownFormat()
    {
    }

    /// <summary>
    /// The only instance of the format.
    /// </summary>
    public static MarkdownFormat Instance { get; } = new();

    public string Name => "md";

    public IReadOnlyList<string> Extensions { get; } = new[] { "md", "markdown" };

    public ITitleRenderer CreateTitleRenderer() => new Title();

    public IBlockRenderer<Paragraph> CreateParagraphRenderer() => new ParagraphBlock();

    public IBlockRenderer<Link> CreateLinkRenderer() => new LinkBlock();

    public IBlockRenderer<Image> CreateImageRenderer() => new ImageBlock();

    public IDocumentLayout CreateLayout() => new BlankLineLayout();

    // A document is always a first-level title; the number of '#' of a heading grows with its nesting.
    private sealed class Title : ITitleRenderer
    {
        public string Render(ITextSection section, int depth)
            => section is Document ? $"# {section.Title}" : $"{new string('#', depth + 1)} {section.Title}";
    }

    private sealed class ParagraphBlock : IBlockRenderer<Paragraph>
    {
        public string Render(Paragraph element) => element.Content;
    }

    private sealed class LinkBlock : IBlockRenderer<Link>
    {
        public string Render(Link element) => $"[{element.Label}]({element.Url})";
    }

    private sealed class ImageBlock : IBlockRenderer<Image>
    {
        public string Render(Image element) => $"![{element.Description}]({element.Path})";
    }
}

/// <summary>
/// Separates blocks with an empty line and adds nothing around the document.
/// Shared by the text formats.
/// </summary>
internal sealed class BlankLineLayout : IDocumentLayout
{
    private static readonly string Separator = Environment.NewLine + Environment.NewLine;

    public string Join(IEnumerable<string> blocks) => string.Join(Separator, blocks);

    public string Wrap(string body, Document document) => body;
}
