using ClassSystems.Core.Text.Models;

namespace ClassSystems.Core.Text.Formats;

/// <summary>
/// <b>Abstract Factory</b> of a text representation (Markdown, HTML, plain text…).
/// </summary>
/// <remarks>
/// Every element of a text has to be shown differently in every format. Without the factory the rendering
/// code would contain a <c>switch</c> over formats inside a <c>switch</c> over elements, and a new format
/// would mean editing all of them. Now a format is one family of products — a renderer for every kind of
/// element plus the layout of the whole document — and <see cref="Display.TextDto"/> works only with
/// these interfaces. A new format (for example LaTeX) is a new class implementing this interface;
/// no existing code changes (open/closed principle), and renderers of different formats cannot be mixed.
/// </remarks>
public interface ITextFormat
{
    /// <summary>
    /// The name used in commands, for example <c>html</c>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// File extensions of the format without a dot, for example <c>md</c>.
    /// </summary>
    IReadOnlyList<string> Extensions { get; }

    /// <summary>
    /// Creates the renderer of titles of the document and of headings.
    /// </summary>
    ITitleRenderer CreateTitleRenderer();

    /// <summary>
    /// Creates the renderer of paragraphs.
    /// </summary>
    IBlockRenderer<Paragraph> CreateParagraphRenderer();

    /// <summary>
    /// Creates the renderer of links.
    /// </summary>
    IBlockRenderer<Link> CreateLinkRenderer();

    /// <summary>
    /// Creates the renderer of images.
    /// </summary>
    IBlockRenderer<Image> CreateImageRenderer();

    /// <summary>
    /// Creates the layout that joins blocks and frames a whole document.
    /// </summary>
    IDocumentLayout CreateLayout();
}

/// <summary>
/// Product of a format: shows the title line of a section.
/// </summary>
public interface ITitleRenderer
{
    /// <param name="depth">How deep the section is nested; the document has depth 0.</param>
    string Render(ITextSection section, int depth);
}

/// <summary>
/// Product of a format: shows one kind of leaf element.
/// </summary>
public interface IBlockRenderer<in TElement>
    where TElement : ITextElement
{
    string Render(TElement element);
}

/// <summary>
/// Product of a format: joins rendered blocks and frames a whole document.
/// </summary>
public interface IDocumentLayout
{
    /// <summary>
    /// Joins the blocks of a section (its title and its children) into one text.
    /// </summary>
    string Join(IEnumerable<string> blocks);

    /// <summary>
    /// Adds what a complete file of the format needs around the body (for HTML — the page itself).
    /// </summary>
    string Wrap(string body, Document document);
}
