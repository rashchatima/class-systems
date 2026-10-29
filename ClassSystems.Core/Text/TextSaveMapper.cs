using ClassSystems.Core.Text.Models;
using ClassSystems.Infra.Data;
using ClassSystems.Infra.Data.Models;

namespace ClassSystems.Core.Text;

/// <summary>
/// Converts a document to a save and back.
/// </summary>
public static class TextSaveMapper
{
    /// <summary>
    /// The key the saves of the text system are stored under in the repository.
    /// </summary>
    public const string System = "text";

    /// <summary>
    /// Makes a save of the document.
    /// </summary>
    public static TextSave ToSave(Document document, string name)
        => new() { SaveName = name, TextRoot = ToData(document) };

    /// <summary>
    /// Restores the document from a save.
    /// </summary>
    /// <exception cref="InvalidOperationException">The save does not hold a document.</exception>
    public static Document FromSave(TextSave save)
        => (save.TextRoot != null ? FromData(save.TextRoot) : null) as Document
           ?? throw new InvalidOperationException($"The save \"{save.SaveName}\" does not contain a document.");

    private static TextNodeData ToData(ITextElement element) => element switch
    {
        ITextSection section => new TextNodeData(
            TextTree.KindOf(section), section.Title, Children: section.Children.Select(ToData).ToList()),
        Paragraph paragraph => new TextNodeData("paragraph", paragraph.Content),
        Link link => new TextNodeData("link", link.Label, link.Url),
        Image image => new TextNodeData("image", image.Description, image.Path),
        _ => throw new NotSupportedException($"Unknown text element {element.GetType().Name}."),
    };

    private static ITextElement FromData(TextNodeData node)
    {
        List<ITextElement> Children() => (node.Children ?? new List<TextNodeData>()).Select(FromData).ToList();

        return node.Kind switch
        {
            "document" => new Document(node.Text, Children()),
            "heading" => new Heading(node.Text, Children()),
            "paragraph" => new Paragraph(node.Text),
            "link" => new Link(node.Text, node.Target ?? string.Empty),
            "image" => new Image(node.Text, node.Target ?? string.Empty),
            _ => throw new InvalidOperationException($"Unknown element kind \"{node.Kind}\" in the save."),
        };
    }
}
