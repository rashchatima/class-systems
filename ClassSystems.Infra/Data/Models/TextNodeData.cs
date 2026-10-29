namespace ClassSystems.Infra.Data.Models;

/// <summary>
/// One element of a saved text. The tree of the document is stored as nested nodes.
/// </summary>
/// <param name="Kind">Type of the element: document, heading, paragraph, link or image.</param>
/// <param name="Text">Title of a container or content of a leaf.</param>
/// <param name="Target">Address of a link or path of an image; <c>null</c> for other elements.</param>
/// <param name="Children">Nested elements of a container; <c>null</c> for leaves.</param>
public record TextNodeData(
    string Kind,
    string Text,
    string? Target = null,
    List<TextNodeData>? Children = null);
