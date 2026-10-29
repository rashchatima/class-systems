namespace ClassSystems.Core.Text.Formats;

/// <summary>
/// All known formats: finding a format by its name or by the extension of a file.
/// </summary>
/// <remarks>
/// The list holds the single instances of the format factories, so the rest of the program never creates them.
/// A new format is registered with one line here.
/// </remarks>
public static class TextFormats
{
    /// <summary>
    /// Every format, the default one first.
    /// </summary>
    public static IReadOnlyList<ITextFormat> All { get; } = new ITextFormat[]
    {
        MarkdownFormat.Instance,
        HtmlFormat.Instance,
        PlainTextFormat.Instance,
    };

    /// <summary>
    /// The format used when nothing else is chosen.
    /// </summary>
    public static ITextFormat Default => MarkdownFormat.Instance;

    /// <summary>
    /// Finds a format by its name or one of its extensions (case-insensitive, with or without a dot).
    /// </summary>
    /// <exception cref="ArgumentException">There is no such format.</exception>
    public static ITextFormat Find(string nameOrExtension)
    {
        string key = nameOrExtension.Trim().TrimStart('.');
        return All.FirstOrDefault(format => format.Name.Equals(key, StringComparison.OrdinalIgnoreCase)
                                            || format.Extensions.Contains(key, StringComparer.OrdinalIgnoreCase))
               ?? throw new ArgumentException(
                   $"Unknown format \"{nameOrExtension}\". Available: {string.Join(", ", All.Select(format => format.Name))}.");
    }

    /// <summary>
    /// Chooses the format by the extension of the file; unknown or missing extensions give the default format.
    /// </summary>
    public static ITextFormat ForFile(string path)
    {
        string extension = Path.GetExtension(path).TrimStart('.');
        return All.FirstOrDefault(format => format.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) ?? Default;
    }
}
