using ClassSystems.Infra.Display;

namespace ClassSystems.Tests.Doubles;

/// <summary>
/// Fake: the simplest implementation that ignores everything it is given.
/// Used when a test does not care about the output at all.
/// </summary>
public class FakeDisplayer : IDisplayer
{
    public void Display(string line)
    {
    }

    public void Display(IDisplayable displayable)
    {
    }
}

/// <summary>
/// Mock: remembers every call, so a test can check what was reported and how many times.
/// </summary>
public class MockDisplayer : IDisplayer
{
    private readonly List<string> _lines = new();

    public IReadOnlyList<string> Lines => _lines;

    public int TextCalls { get; private set; }

    public int DisplayableCalls { get; private set; }

    public string LastLine => _lines.Count > 0 ? _lines[^1] : string.Empty;

    public void Display(string line)
    {
        TextCalls++;
        _lines.Add(line);
    }

    public void Display(IDisplayable displayable)
    {
        DisplayableCalls++;
        _lines.Add(displayable.Render());
    }

    public bool Contains(string fragment) => _lines.Any(line => line.Contains(fragment));
}
