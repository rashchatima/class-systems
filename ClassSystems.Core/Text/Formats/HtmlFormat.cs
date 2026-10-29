using System.Net;
using ClassSystems.Core.Text.Models;

namespace ClassSystems.Core.Text.Formats;

/// <summary>
/// Concrete factory of the HTML family: headings <c>h1–h6</c>, paragraphs, links, figures and a full page.
/// </summary>
/// <remarks>
/// <b>Singleton</b> for the same reason as <see cref="MarkdownFormat"/>. All text is encoded,
/// so characters such as &lt; or &amp; in the document cannot break the page.
/// </remarks>
public sealed class HtmlFormat : ITextFormat
{
    // HTML has only six levels of headings; deeper sections use the last one.
    private const int MaxHeadingLevel = 6;

    private HtmlFormat()
    {
    }

    /// <summary>
    /// The only instance of the format.
    /// </summary>
    public static HtmlFormat Instance { get; } = new();

    public string Name => "html";

    public IReadOnlyList<string> Extensions { get; } = new[] { "html", "htm" };

    public ITitleRenderer CreateTitleRenderer() => new Title();

    public IBlockRenderer<Paragraph> CreateParagraphRenderer() => new ParagraphBlock();

    public IBlockRenderer<Link> CreateLinkRenderer() => new LinkBlock();

    public IBlockRenderer<Image> CreateImageRenderer() => new ImageBlock();

    public IDocumentLayout CreateLayout() => new PageLayout();

    private static string Encode(string text) => WebUtility.HtmlEncode(text);

    private sealed class Title : ITitleRenderer
    {
        public string Render(ITextSection section, int depth)
        {
            int level = section is Document ? 1 : Math.Min(depth + 1, MaxHeadingLevel);
            return $"<h{level}>{Encode(section.Title)}</h{level}>";
        }
    }

    private sealed class ParagraphBlock : IBlockRenderer<Paragraph>
    {
        public string Render(Paragraph element) => $"<p>{Encode(element.Content)}</p>";
    }

    private sealed class LinkBlock : IBlockRenderer<Link>
    {
        public string Render(Link element) => $"<p><a href=\"{Encode(element.Url)}\">{Encode(element.Label)}</a></p>";
    }

    private sealed class ImageBlock : IBlockRenderer<Image>
    {
        public string Render(Image element)
            => $"<figure><img src=\"{Encode(element.Path)}\" alt=\"{Encode(element.Description)}\">"
               + $"<figcaption>{Encode(element.Description)}</figcaption></figure>";
    }

    private sealed class PageLayout : IDocumentLayout
    {
        public string Join(IEnumerable<string> blocks) => string.Join(Environment.NewLine, blocks);

        public string Wrap(string body, Document document) => string.Join(Environment.NewLine,
            "<!DOCTYPE html>",
            "<html>",
            $"<head><meta charset=\"utf-8\"><title>{Encode(document.Title)}</title></head>",
            "<body>",
            body,
            "</body>",
            "</html>");
    }
}
