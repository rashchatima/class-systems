using ClassSystems.Infra.Display;

namespace ClassSystems.Cli.Presenter;

/// <summary>
/// Source of user input and destination of output for the command interface.
/// </summary>
/// <remarks>
/// A terminal is an <see cref="IDisplayer"/> that can also read, so the same object is given
/// to the systems (as their output) and to the commands (as input and output).
/// Commands depend on this abstraction instead of <see cref="Console"/>,
/// so they can be driven by a script or a test as easily as by a person.
/// </remarks>
public interface ITerminal : IDisplayer
{
    /// <summary>
    /// Shows a prompt and reads one line.
    /// </summary>
    /// <returns>The entered line, or <c>null</c> when the input has ended.</returns>
    string? ReadLine(string prompt);

    /// <summary>
    /// Writes a line of text.
    /// </summary>
    void WriteLine(string text = "") => Display(text);
}
