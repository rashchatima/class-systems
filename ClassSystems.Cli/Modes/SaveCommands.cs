using ClassSystems.Cli.Commands;
using ClassSystems.Cli.Presenter;
using ClassSystems.Infra.Data;

namespace ClassSystems.Cli.Modes;

/// <summary>
/// The commands <c>save</c>, <c>load</c>, <c>saves</c> and <c>unsave</c>, shared by all modes.
/// </summary>
/// <remarks>
/// A mode only says how to turn its state into a save and back; storing, listing and deleting saves
/// is the same for every system.
/// </remarks>
/// <typeparam name="TSave">The type of save of the system.</typeparam>
public class SaveCommands<TSave> where TSave : SaveBase
{
    private readonly ITerminal _terminal;
    private readonly SaveStore _store;
    private readonly string _system;
    private readonly Func<string, TSave> _capture;
    private readonly Action<TSave> _restore;

    /// <param name="terminal">Where the results are shown.</param>
    /// <param name="store">Where the saves are kept.</param>
    /// <param name="system">Key of the system in the repository, for example "text".</param>
    /// <param name="capture">Makes a save of the current state with the given name.</param>
    /// <param name="restore">Replaces the current state with the one from a save.</param>
    public SaveCommands(ITerminal terminal, SaveStore store, string system, Func<string, TSave> capture, Action<TSave> restore)
    {
        _terminal = terminal;
        _store = store;
        _system = system;
        _capture = capture;
        _restore = restore;
    }

    /// <summary>
    /// Adds the save commands to the registry.
    /// </summary>
    public void Register(CommandRegistry registry)
    {
        registry.Register("save", "save <name>", "save the current state under a name (replaces a save with the same name)", Save);
        registry.Register("load", "load <name>", "replace the current state with a saved one", Load);
        registry.Register("saves", "saves", "list the saves of this mode", _ => List());
        registry.Register("unsave", "unsave <name>", "delete a save", Delete);
    }

    /// <summary>
    /// Loads a save by name; used for the <c>--load</c> option of the program.
    /// </summary>
    public void Load(string name)
    {
        TSave save = _store.Find<TSave>(_system, name) ?? throw new ArgumentException($"There is no save \"{name}\".");
        _restore(save);
        _terminal.WriteLine($"Loaded \"{save.SaveName}\" from {save.LastModified:yyyy-MM-dd HH:mm}.");
    }

    private void Save(ParsedCommand command)
    {
        string name = command.Argument(0) ?? throw new ArgumentException("Give a name for the save.");
        bool replaced = _store.Put(_system, _capture(name));
        _terminal.WriteLine(replaced ? $"Save \"{name}\" updated." : $"Saved as \"{name}\".");
    }

    private void Load(ParsedCommand command)
        => Load(command.Argument(0) ?? throw new ArgumentException("Give the name of a save."));

    private void List()
    {
        IReadOnlyList<SaveBase> saves = _store.List(_system);
        if (saves.Count == 0)
        {
            _terminal.WriteLine("(no saves)");
            return;
        }

        foreach (SaveBase save in saves)
            _terminal.WriteLine($"  {save.LastModified:yyyy-MM-dd HH:mm}  {save.SaveName}");
    }

    private void Delete(ParsedCommand command)
    {
        string name = command.Argument(0) ?? throw new ArgumentException("Give the name of a save.");
        _terminal.WriteLine(_store.Delete(_system, name) ? $"Save \"{name}\" deleted." : $"There is no save \"{name}\".");
    }
}
