namespace ClassSystems.Infra.Display;

/// <summary>
/// Keeps everything shown in a list. Useful for tests and for checking what a system has reported.
/// </summary>
public class MemoryDisplayer : IDisplayer
{
    private readonly List<string> _lines = new();

    /// <summary>
    /// Everything shown so far, one entry per call.
    /// </summary>
    public IReadOnlyList<string> Lines => _lines;

    /// <inheritdoc/>
    public void Display(string line) => _lines.Add(line);

    /// <inheritdoc/>
    public void Display(IDisplayable displayable) => _lines.Add(displayable.Render());
}
