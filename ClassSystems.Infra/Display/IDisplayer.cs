namespace ClassSystems.Infra.Display;

/// <summary>
/// A place where the program shows information: the console, a file, memory of a test, and so on.
/// </summary>
/// <remarks>
/// The systems of the program depend only on this interface, so the output can be replaced
/// without touching their code.
/// </remarks>
public interface IDisplayer
{
    /// <summary>
    /// Shows a line of text.
    /// </summary>
    void Display(string line);

    /// <summary>
    /// Shows an object that can render itself.
    /// </summary>
    void Display(IDisplayable displayable);
}
