namespace ClassSystems.Cli.Commands;

/// <summary>
/// The set of commands available in the current mode, found by their selectors.
/// </summary>
/// <remarks>
/// New functionality is added by registering new commands; the registry itself never changes,
/// which keeps the command system open for extension and closed for modification.
/// </remarks>
public class CommandRegistry
{
    private readonly Dictionary<string, Command> _commands = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// All registered commands in the order of their selectors.
    /// </summary>
    public IEnumerable<Command> Commands => _commands.Values.OrderBy(command => command.Selector);

    /// <summary>
    /// Adds a command.
    /// </summary>
    /// <exception cref="InvalidOperationException">A command with the same selector already exists.</exception>
    public void Register(Command command)
    {
        if (!_commands.TryAdd(command.Selector, command))
            throw new InvalidOperationException($"Command \"{command.Selector}\" is already registered.");
    }

    /// <summary>
    /// Shortcut for registering a command from its parts.
    /// </summary>
    public void Register(string selector, string usage, string description, Action<ParsedCommand> execute)
        => Register(new Command(selector, usage, description, execute));

    /// <summary>
    /// Finds a command by its selector.
    /// </summary>
    public bool TryFind(string selector, out Command command)
        => _commands.TryGetValue(selector, out command!);
}
