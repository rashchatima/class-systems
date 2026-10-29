using System.Text;

namespace ClassSystems.Infra.Display;

/// <summary>
/// Writes information to a text file instead of the screen.
/// </summary>
/// <remarks>
/// The file is created anew (or overwritten) when the displayer is created and closed by <see cref="Dispose"/>.
/// </remarks>
public sealed class FileDisplayer : IDisplayer, IDisposable
{
    private readonly StreamWriter _writer;

    /// <param name="path">The file to write to.</param>
    public FileDisplayer(string path)
    {
        _writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <inheritdoc/>
    public void Display(string line) => _writer.WriteLine(line);

    /// <inheritdoc/>
    public void Display(IDisplayable displayable) => _writer.WriteLine(displayable.Render());

    /// <summary>
    /// Saves everything written and closes the file.
    /// </summary>
    public void Dispose() => _writer.Dispose();
}
