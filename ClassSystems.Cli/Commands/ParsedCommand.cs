namespace ClassSystems.Cli.Commands;

/// <summary>
/// A command line split into its parts: <c>selector arg1 arg2 --option value --flag</c>.
/// </summary>
/// <param name="Selector">The word that chooses which command to run.</param>
/// <param name="Arguments">Positional values in the order they were given.</param>
/// <param name="Options">Named values; a flag without a value is stored as an empty string.</param>
public record ParsedCommand(
    string Selector,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string> Options)
{
    /// <summary>
    /// Returns the argument at the given position, or <c>null</c> if it was not given.
    /// </summary>
    public string? Argument(int index) => index < Arguments.Count ? Arguments[index] : null;

    /// <summary>
    /// Returns the value of an option, or <c>null</c> if the option was not given.
    /// </summary>
    public string? Option(string name) => Options.TryGetValue(name, out string? value) ? value : null;

    /// <summary>
    /// Whether the option or flag was given.
    /// </summary>
    public bool HasOption(string name) => Options.ContainsKey(name);
}
