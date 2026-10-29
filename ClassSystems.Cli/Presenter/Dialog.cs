namespace ClassSystems.Cli.Presenter;

/// <summary>
/// Typical questions to the user built on top of a terminal: text, numbers, choices, confirmation.
/// </summary>
/// <remarks>
/// Every question repeats until the answer is valid, so commands never receive broken input.
/// When the input ends, <see cref="InputEndedException"/> is thrown to stop the current dialog.
/// </remarks>
public class Dialog
{
    private readonly ITerminal _terminal;

    public Dialog(ITerminal terminal)
    {
        _terminal = terminal;
    }

    /// <summary>
    /// Asks for a non-empty line of text.
    /// </summary>
    public string AskText(string question)
    {
        while (true)
        {
            string answer = Read(question).Trim();
            if (answer.Length > 0)
                return answer;

            _terminal.WriteLine("The value cannot be empty.");
        }
    }

    /// <summary>
    /// Asks for a line of text that may be left empty.
    /// </summary>
    public string AskOptionalText(string question) => Read(question).Trim();

    /// <summary>
    /// Asks for a whole number; an empty answer gives the default value.
    /// </summary>
    public int AskNumber(string question, int defaultValue = 0)
    {
        while (true)
        {
            string answer = Read($"{question} [{defaultValue}]: ").Trim();
            if (answer.Length == 0)
                return defaultValue;
            if (int.TryParse(answer, out int value))
                return value;

            _terminal.WriteLine("Please enter a whole number.");
        }
    }

    /// <summary>
    /// Asks to pick one of the options (case-insensitive).
    /// </summary>
    public string AskChoice(string question, IReadOnlyCollection<string> options)
    {
        while (true)
        {
            string answer = Read($"{question} ({string.Join("/", options)}): ").Trim();
            string? match = options.FirstOrDefault(option => option.Equals(answer, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;

            _terminal.WriteLine($"Choose one of: {string.Join(", ", options)}.");
        }
    }

    /// <summary>
    /// Asks a yes/no question.
    /// </summary>
    public bool Confirm(string question)
    {
        while (true)
        {
            string answer = Read($"{question} (y/n): ").Trim().ToLowerInvariant();
            if (answer is "y" or "yes")
                return true;
            if (answer is "n" or "no")
                return false;

            _terminal.WriteLine("Please answer y or n.");
        }
    }

    private string Read(string prompt)
        => _terminal.ReadLine(prompt) ?? throw new InputEndedException();
}

/// <summary>
/// Thrown when the user input ends in the middle of a dialog.
/// </summary>
public class InputEndedException : Exception
{
    public InputEndedException()
        : base("The input has ended.")
    {
    }
}
