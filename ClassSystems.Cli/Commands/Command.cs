namespace ClassSystems.Cli.Commands;

/// <summary>
/// A terminal command: its selector, a short help and the code that runs it.
/// </summary>
/// <param name="Selector">The word the user types to run the command.</param>
/// <param name="Usage">How to call the command, shown in the help.</param>
/// <param name="Description">What the command does, shown in the help.</param>
/// <param name="Execute">The code of the command; receives the parsed line.</param>
public record Command(string Selector, string Usage, string Description, Action<ParsedCommand> Execute);
