namespace ClassSystems.Core.Text.Models;

// Pure models of the text system: they only hold data.
// Rendering lives in TextDto and the formats, operations on the tree in TextTree, saving in TextSaveMapper.

/// <summary>
/// Any element of a structured text.
/// </summary>
/// <remarks>
/// <b>Prototype pattern.</b> Every element can make a deep copy of itself with <see cref="DeepCopy"/>.
/// The tree compares elements by reference, so a copy must be a new object, and a copied section must get
/// its own children; otherwise editing the copy would change the original. The code that copies
/// (the <c>copy</c> command, <see cref="Templates.TemplateLibrary"/>) does not need to know the concrete type.
/// The method is not called Clone because records reserve that name.
/// </remarks>
public interface ITextElement
{
    /// <summary>
    /// A new element equal to this one; for a section, with copies of all nested elements.
    /// </summary>
    ITextElement DeepCopy();
}

/// <summary>
/// An element that has a title and contains other elements: the document and its headings.
/// </summary>
public interface ITextSection : ITextElement
{
    string Title { get; }

    List<ITextElement> Children { get; }
}

/// <summary>
/// The whole text: the top of the tree.
/// </summary>
public record Document(string Title, List<ITextElement> Children) : ITextSection
{
    public Document(string title)
        : this(title, new List<ITextElement>())
    {
    }

    /// <inheritdoc cref="ITextElement.DeepCopy"/>
    public Document DeepCopy() => new(Title, CopyAll(Children));

    ITextElement ITextElement.DeepCopy() => DeepCopy();

    internal static List<ITextElement> CopyAll(IEnumerable<ITextElement> children)
        => children.Select(child => child.DeepCopy()).ToList();
}

/// <summary>
/// A section of the text. Its level is not stored: it follows from how deep the section is nested.
/// </summary>
public record Heading(string Title, List<ITextElement> Children) : ITextSection
{
    public Heading(string title)
        : this(title, new List<ITextElement>())
    {
    }

    /// <inheritdoc cref="ITextElement.DeepCopy"/>
    public Heading DeepCopy() => new(Title, Document.CopyAll(Children));

    ITextElement ITextElement.DeepCopy() => DeepCopy();
}

/// <summary>
/// A block of plain text.
/// </summary>
public record Paragraph(string Content) : ITextElement
{
    /// <inheritdoc cref="ITextElement.DeepCopy"/>
    public Paragraph DeepCopy() => this with { };

    ITextElement ITextElement.DeepCopy() => DeepCopy();
}

/// <summary>
/// A reference to an external resource.
/// </summary>
public record Link(string Label, string Url) : ITextElement
{
    /// <inheritdoc cref="ITextElement.DeepCopy"/>
    public Link DeepCopy() => this with { };

    ITextElement ITextElement.DeepCopy() => DeepCopy();
}

/// <summary>
/// A picture embedded into the text.
/// </summary>
public record Image(string Description, string Path) : ITextElement
{
    /// <inheritdoc cref="ITextElement.DeepCopy"/>
    public Image DeepCopy() => this with { };

    ITextElement ITextElement.DeepCopy() => DeepCopy();
}
