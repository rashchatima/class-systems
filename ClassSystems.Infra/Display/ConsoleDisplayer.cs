namespace ClassSystems.Infra.Display;

/// <summary>
/// Shows information in the system console.
/// </summary>
public class ConsoleDisplayer : IDisplayer
{
    /// <inheritdoc/>
    public void Display(string line) => Console.WriteLine(line);

    /// <inheritdoc/>
    public void Display(IDisplayable displayable) => Console.WriteLine(displayable.Render());
}
