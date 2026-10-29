using ClassSystems.Core.Text.Display;
using ClassSystems.Core.Text.Models;
using ClassSystems.Infra.Display;

namespace ClassSystems.Core.Text;

/// <summary>
/// The facade of the text system: builds a document step by step and shows it.
/// </summary>
/// <remarks>
/// The factory remembers the section that is being filled: <see cref="AddHeading"/> opens a nested section,
/// <see cref="Up"/> closes it. Every method returns the factory, so calls can be chained.
/// Output goes to the <see cref="IDisplayer"/> given in the constructor, not directly to the console.
/// </remarks>
public class TextFactory
{
    private readonly Document _document;

    // The open sections from the document down to the current one.
    private readonly Stack<ITextSection> _open = new();

    private IDisplayer _displayer;

    /// <param name="title">The title of the document.</param>
    /// <param name="displayer">Where <see cref="Print"/> and <see cref="PrintTableOfContents"/> show the text.</param>
    public TextFactory(string title, IDisplayer displayer)
    {
        _document = new Document(title);
        _open.Push(_document);
        _displayer = displayer;
    }

    /// <summary>
    /// Replaces the output of the factory while the program runs.
    /// </summary>
    public void SetDisplayer(IDisplayer displayer) => _displayer = displayer;

    /// <summary>
    /// Adds a section to the current one and moves inside it.
    /// </summary>
    public TextFactory AddHeading(string title)
    {
        var heading = new Heading(title);
        TextTree.Add(_open.Peek(), heading);
        _open.Push(heading);
        return this;
    }

    /// <summary>
    /// Adds a paragraph to the current section.
    /// </summary>
    public TextFactory AddParagraph(string content) => Add(new Paragraph(content));

    /// <summary>
    /// Adds a link to the current section.
    /// </summary>
    public TextFactory AddLink(string label, string url) => Add(new Link(label, url));

    /// <summary>
    /// Adds a picture to the current section.
    /// </summary>
    public TextFactory AddImage(string description, string path) => Add(new Image(description, path));

    /// <summary>
    /// Closes the current section: following elements go to its parent.
    /// </summary>
    /// <exception cref="InvalidOperationException">The factory is already at the top of the document.</exception>
    public TextFactory Up()
    {
        if (_open.Count == 1)
            throw new InvalidOperationException("Already at the top of the document.");

        _open.Pop();
        return this;
    }

    /// <summary>
    /// Returns the built document.
    /// </summary>
    public Document Build() => _document;

    /// <summary>
    /// Shows the whole document.
    /// </summary>
    public TextFactory Print()
    {
        _displayer.Display(new TextDto(_document));
        return this;
    }

    /// <summary>
    /// Shows the table of contents of the document.
    /// </summary>
    public TextFactory PrintTableOfContents()
    {
        _displayer.Display(new TableOfContentsDto(_document));
        return this;
    }

    private TextFactory Add(ITextElement element)
    {
        TextTree.Add(_open.Peek(), element);
        return this;
    }
}
