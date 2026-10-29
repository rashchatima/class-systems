using ClassSystems.Cli.Presenter;

namespace ClassSystems.Cli.Commands;

/// <summary>
/// The interpreter loop: reads lines after the <c>&gt;</c> prompt, parses them and runs the commands.
/// </summary>
/// <remarks>
/// The shell adds two universal commands, <c>help</c> and <c>exit</c>; everything else comes from the mode.
/// An error in one command is reported and does not stop the shell.
/// </remarks>
public class CommandShell
{
    private readonly ITerminal _terminal;
    private readonly CommandRegistry _registry;
    private readonly CommandParser _parser;
    private bool _running;

    public CommandShell(ITerminal terminal, CommandRegistry registry, CommandParser parser)
    {
        _terminal = terminal;
        _registry = registry;
        _parser = parser;

        _registry.Register("help", "help", "show the list of commands", _ => PrintHelp());
        _registry.Register("exit", "exit", "leave the program", _ => _running = false);
    }

    /// <summary>
    /// Runs the loop until the user types <c>exit</c> or the input ends.
    /// </summary>
    public void Run()
    {
        _running = true;
        _terminal.WriteLine("Type \"help\" to see the commands.");

        while (_running)
        {
            string? line = _terminal.ReadLine("> ");
            if (line == null)
                break;

            try
            {
                Execute(line);
            }
            catch (InputEndedException)
            {
                break;
            }
            catch (Exception error) when (error is FormatException or ArgumentException or InvalidOperationException)
            {
                _terminal.WriteLine($"Error: {error.Message}");
            }
        }
    }

    /// <summary>
    /// Parses and runs a single line.
    /// </summary>
    public void Execute(string line)
    {
        ParsedCommand? command = _parser.ParseLine(line);
        if (command == null)
            return;

        if (_registry.TryFind(command.Selector, out Command found))
            found.Execute(command);
        else
            _terminal.WriteLine($"Unknown command \"{command.Selector}\". Type \"help\" to see the commands.");
    }

    private void PrintHelp()
    {
        int width = _registry.Commands.Max(command => command.Usage.Length);
        foreach (Command command in _registry.Commands)
            _terminal.WriteLine($"  {command.Usage.PadRight(width)}  {command.Description}");
    }
}
