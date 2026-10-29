using ClassSystems.Core.Text.Models;

namespace ClassSystems.Core.Text.Templates;

/// <summary>
/// <b>Prototype</b> registry: named sample elements from which new elements are made by copying.
/// </summary>
/// <remarks>
/// Building a typical part of a document (a chapter with standard paragraphs, a figure with a caption)
/// by hand takes many steps. Instead, a ready element is kept as a prototype and every new one is its
/// <see cref="ITextElement.DeepCopy"/>. The library does not know the concrete types of the prototypes, and
/// the user can add new ones while the program runs (<c>template save</c>) without any new classes.
/// <para>
/// A prototype is copied both when it is registered and when it is used, so later changes of the original
/// element do not change the template, and changes of a created element do not change the prototype.
/// </para>
/// </remarks>
public class TemplateLibrary
{
    private readonly Dictionary<string, ITextElement> _prototypes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Names of all templates in alphabetical order.
    /// </summary>
    public IReadOnlyList<string> Names => _prototypes.Keys.Order(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>
    /// A library with the standard templates: chapter, figure and reference.
    /// </summary>
    public static TemplateLibrary WithDefaults()
    {
        var library = new TemplateLibrary();
        library.Register("chapter", new Heading("New chapter", new List<ITextElement>
        {
            new Paragraph("What this chapter is about."),
            new Heading("Details"),
            new Paragraph("Summary of the chapter."),
        }));
        library.Register("figure", new Heading("Figure", new List<ITextElement>
        {
            new Image("Figure", "images/figure.png"),
            new Paragraph("Caption: what the figure shows."),
        }));
        library.Register("reference", new Link("Source", "https://example.com"));
        return library;
    }

    /// <summary>
    /// Stores a copy of the element as a template, replacing a template with the same name.
    /// </summary>
    public void Register(string name, ITextElement prototype)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A template must have a name.");

        _prototypes[name.Trim()] = prototype.DeepCopy();
    }

    /// <summary>
    /// The prototype itself, for showing it; must not be inserted into a document.
    /// </summary>
    public ITextElement Peek(string name) => Find(name);

    /// <summary>
    /// Makes a new element from the template.
    /// </summary>
    /// <exception cref="ArgumentException">There is no such template.</exception>
    public ITextElement Create(string name) => Find(name).DeepCopy();

    private ITextElement Find(string name)
        => _prototypes.TryGetValue(name.Trim(), out ITextElement? prototype)
            ? prototype
            : throw new ArgumentException($"Unknown template \"{name}\". Available: {string.Join(", ", Names)}.");
}
