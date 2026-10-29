using ClassSystems.Core.Text.Formats;
using ClassSystems.Core.Text.Models;
using ClassSystems.Infra.Display;

namespace ClassSystems.Core.Text.Display;

/// <summary>
/// A text element rendered in some format, ready to be shown by any <see cref="IDisplayer"/>.
/// </summary>
/// <remarks>
/// The DTO is the client of the <see cref="ITextFormat"/> abstract factory: it walks the tree and asks the
/// products of the chosen format to show every element, without knowing which format it is.
/// Markdown is used when no format is given, so the old calls work as before.
/// </remarks>
/// <param name="element">The element to show.</param>
/// <param name="depth">How deep the element is nested; the document itself has depth 0.</param>
/// <param name="titleOnly">For a section, show only its own title line, without the children.</param>
/// <param name="format">The representation; Markdown by default.</param>
public class TextDto(ITextElement element, int depth = 0, bool titleOnly = false, ITextFormat? format = null) : IDisplayable
{
    private readonly ITextFormat _format = format ?? TextFormats.Default;

    /// <inheritdoc/>
    public string Render()
    {
        var renderer = new Renderer(_format);

        if (titleOnly && element is ITextSection section)
            return renderer.Title.Render(section, depth);

        string body = renderer.Render(element, depth);
        return element is Document document ? renderer.Layout.Wrap(body, document) : body;
    }

    // The products of one format, created once per rendering.
    private sealed class Renderer(ITextFormat format)
    {
        public ITitleRenderer Title { get; } = format.CreateTitleRenderer();

        public IDocumentLayout Layout { get; } = format.CreateLayout();

        private readonly IBlockRenderer<Paragraph> _paragraph = format.CreateParagraphRenderer();
        private readonly IBlockRenderer<Link> _link = format.CreateLinkRenderer();
        private readonly IBlockRenderer<Image> _image = format.CreateImageRenderer();

        public string Render(ITextElement element, int depth) => element switch
        {
            ITextSection section => Layout.Join(
                section.Children.Select(child => Render(child, depth + 1)).Prepend(Title.Render(section, depth))),
            Paragraph paragraph => _paragraph.Render(paragraph),
            Link link => _link.Render(link),
            Image image => _image.Render(image),
            _ => throw new NotSupportedException($"Unknown text element {element.GetType().Name}."),
        };
    }
}

/// <summary>
/// Table of contents of a section: titles of all nested sections, indented by nesting.
/// </summary>
public class TableOfContentsDto(ITextSection root) : IDisplayable
{
    // Number of spaces added per nesting level.
    private const int IndentSize = 2;

    /// <inheritdoc/>
    public string Render()
    {
        var entries = new List<string>();
        Collect(root, 0, entries);
        return string.Join(Environment.NewLine, entries);
    }

    private static void Collect(ITextSection section, int depth, List<string> entries)
    {
        entries.Add(new string(' ', depth * IndentSize) + section.Title);
        foreach (ITextSection child in section.Children.OfType<ITextSection>())
            Collect(child, depth + 1, entries);
    }
}
