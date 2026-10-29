using ClassSystems.Infra.Display;

namespace ClassSystems.Cli.Presenter;

/// <summary>
/// Terminal that works with the system console: output comes from <see cref="ConsoleDisplayer"/>,
/// input is read from the keyboard (or from a redirected file with a scenario).
/// </summary>
public class ConsoleTerminal : ConsoleDisplayer, ITerminal
{
    /// <inheritdoc/>
    public string? ReadLine(string prompt)
    {
        Console.Write(prompt);
        string? line = Console.ReadLine();

        // When the input comes from a file, the typed text is not visible, so it is echoed.
        if (line != null && Console.IsInputRedirected)
            Console.WriteLine(line);

        return line;
    }
}
