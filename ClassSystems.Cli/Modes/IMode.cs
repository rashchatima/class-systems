using ClassSystems.Cli.Commands;

namespace ClassSystems.Cli.Modes;

/// <summary>
/// A working mode of the program: a set of commands for one of the systems.
/// </summary>
/// <remarks>
/// Each mode registers its own commands, so adding a new system means adding a new mode class
/// without changing the shell or the other modes.
/// </remarks>
public interface IMode
{
    /// <summary>
    /// Name used to choose the mode, for example in <c>--mode text</c>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// One line describing the mode.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Adds the commands of this mode to the registry.
    /// </summary>
    void RegisterCommands(CommandRegistry registry);
}
