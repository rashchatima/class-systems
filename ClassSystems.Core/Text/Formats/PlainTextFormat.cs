using ClassSystems.Core.Text.Models;

namespace ClassSystems.Core.Text.Formats;

/// <summary>
/// Concrete factory of the plain-text family: titles are underlined, links and images are written in words.
/// Useful where Markdown symbols would only get in the way, for example in a simple message.
/// </summary>
/// <remarks>
/// <b>Singleton</b> for the same reason as <see cref="MarkdownFormat"/>.
/// </remarks>
public sealed class PlainTextFormat : ITextFormat
{
    private PlainTextFormat()
    {
    }

    /// <summary>
    /// The only instance of the format.
    /// </summary>
    public static PlainTextFormat Instance { get; } = new();

    public string Name => "text";

    public IReadOnlyList<string> Extensions { get; } = new[] { "txt" };

    public ITitleRenderer CreateTitleRenderer() => new Title();

    public IBlockRenderer<Paragraph> CreateParagraphRenderer() => new ParagraphBlock();

    public IBlockRenderer<Link> CreateLinkRenderer() => new LinkBlock();

    public IBlockRenderer<Image> CreateImageRenderer() => new ImageBlock();

    public IDocumentLayout CreateLayout() => new BlankLineLayout();

    // The document is underlined with '=', first-level sections with '-', deeper ones are written as they are.
    private sealed class Title : ITitleRenderer
    {
        public string Render(ITextSection section, int depth)
        {
            char? underline = section is Document ? '=' : depth <= 1 ? '-' : null;
            return underline is char symbol
                ? section.Title + Environment.NewLine + new string(symbol, section.Title.Length)
                : section.Title;
        }
    }

    private sealed class ParagraphBlock : IBlockRenderer<Paragraph>
    {
        public string Render(Paragraph element) => element.Content;
    }

    private sealed class LinkBlock : IBlockRenderer<Link>
    {
        public string Render(Link element) => $"{element.Label} ({element.Url})";
    }

    private sealed class ImageBlock : IBlockRenderer<Image>
    {
        public string Render(Image element) => $"[Image: {element.Description} — {element.Path}]";
    }
}
