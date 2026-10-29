using ClassSystems.Core.Text.Models;

namespace ClassSystems.Core.Text;

/// <summary>
/// Operations on the tree of a text: adding, moving and removing elements, searching in the tree.
/// </summary>
/// <remarks>
/// Models are records, and records compare by value: two paragraphs with the same text are "equal".
/// The tree must work with concrete objects, so all searches here compare references.
/// </remarks>
public static class TextTree
{
    /// <summary>
    /// Appends an element to the end of a section.
    /// </summary>
    public static void Add(ITextSection section, ITextElement child) => Insert(section, section.Children.Count, child);

    /// <summary>
    /// Places an element at the given position of a section.
    /// </summary>
    /// <exception cref="InvalidOperationException">A section would end up inside itself.</exception>
    public static void Insert(ITextSection section, int index, ITextElement child)
    {
        if (child is ITextSection nested && (ReferenceEquals(nested, section) || Contains(nested, section)))
            throw new InvalidOperationException($"\"{nested.Title}\" cannot be placed inside itself.");

        section.Children.Insert(index, child);
    }

    /// <summary>
    /// Moves an element to another position within the same section.
    /// </summary>
    public static void Move(ITextSection section, int fromIndex, int toIndex)
    {
        ITextElement child = section.Children[fromIndex];
        section.Children.RemoveAt(fromIndex);
        section.Children.Insert(toIndex, child);
    }

    /// <summary>
    /// Removes exactly this element (not just an equal one) from the section.
    /// </summary>
    /// <returns><c>true</c> if the element was found and removed.</returns>
    public static bool Remove(ITextSection section, ITextElement child)
    {
        int index = section.Children.FindIndex(element => ReferenceEquals(element, child));
        if (index < 0)
            return false;

        section.Children.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Whether the element lies somewhere inside the section.
    /// </summary>
    public static bool Contains(ITextSection section, ITextElement element)
        => section.Children.Any(child => ReferenceEquals(child, element)
                                         || (child is ITextSection nested && Contains(nested, element)));

    /// <summary>
    /// The sections from the root down to the given one, both included.
    /// </summary>
    /// <returns>The path, or <c>null</c> if the section is not in the tree.</returns>
    public static List<ITextSection>? PathTo(ITextSection root, ITextSection section)
    {
        if (ReferenceEquals(root, section))
            return new List<ITextSection> { root };

        foreach (ITextSection child in root.Children.OfType<ITextSection>())
        {
            List<ITextSection>? path = PathTo(child, section);
            if (path != null)
            {
                path.Insert(0, root);
                return path;
            }
        }

        return null;
    }

    /// <summary>
    /// The section and all sections nested in it, in document order.
    /// </summary>
    public static IEnumerable<ITextSection> Sections(ITextSection root)
    {
        yield return root;
        foreach (ITextSection child in root.Children.OfType<ITextSection>())
        foreach (ITextSection nested in Sections(child))
            yield return nested;
    }

    /// <summary>
    /// The short name of an element: the title of a section or the content of a leaf.
    /// </summary>
    public static string NameOf(ITextElement element) => element switch
    {
        ITextSection section => section.Title,
        Paragraph paragraph => paragraph.Content,
        Link link => link.Label,
        Image image => image.Description,
        _ => element.GetType().Name,
    };

    /// <summary>
    /// The kind of an element in lower case: document, heading, paragraph, link, image.
    /// </summary>
    public static string KindOf(ITextElement element) => element.GetType().Name.ToLowerInvariant();
}
