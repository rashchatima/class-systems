using ClassSystems.Cli.Presenter;

namespace ClassSystems.Tests.Doubles;

/// <summary>
/// A terminal for tests: "types" the given lines one by one and keeps everything the program shows.
/// </summary>
public class ScriptedTerminal(params string[] input) : MockDisplayer, ITerminal
{
    private readonly Queue<string> _input = new(input);

    public string? ReadLine(string prompt) => _input.Count > 0 ? _input.Dequeue() : null;

    public string Output => string.Join(Environment.NewLine, Lines);
}
