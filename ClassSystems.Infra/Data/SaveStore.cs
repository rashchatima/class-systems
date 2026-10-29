namespace ClassSystems.Infra.Data;

/// <summary>
/// Adds, finds and removes named saves of a system and writes every change to the storage.
/// </summary>
/// <remarks>
/// Works only with <see cref="SaveBase"/>, so it does not depend on what exactly a system saves.
/// </remarks>
public class SaveStore
{
    private readonly IRepositoryStorage _storage;
    private readonly Func<DateTime> _clock;

    /// <param name="storage">Where the repository is kept.</param>
    /// <param name="clock">Source of the current time; replaceable for tests.</param>
    public SaveStore(IRepositoryStorage storage, Func<DateTime>? clock = null)
    {
        _storage = storage;
        _clock = clock ?? (() => DateTime.Now);
    }

    /// <summary>
    /// Saves of the system, from the oldest to the newest.
    /// </summary>
    public IReadOnlyList<SaveBase> List(string system)
        => SavesOf(_storage.Load(), system).OrderBy(save => save.LastModified).ToList();

    /// <summary>
    /// Finds a save by name (case-insensitive).
    /// </summary>
    /// <returns>The save, or <c>null</c> if there is no save of this type with this name.</returns>
    public TSave? Find<TSave>(string system, string name) where TSave : SaveBase
        => SavesOf(_storage.Load(), system).OfType<TSave>().FirstOrDefault(save => HasName(save, name));

    /// <summary>
    /// Stores a save under its name, replacing an older save with the same name.
    /// </summary>
    /// <returns><c>true</c> if an older save was replaced.</returns>
    public bool Put(string system, SaveBase save)
    {
        string name = save.SaveName ?? throw new ArgumentException("A save must have a name.");
        DateTime now = _clock();
        save.LastModified = now;

        Repository repository = _storage.Load();
        List<SaveBase> saves = SavesOf(repository, system);
        bool replaced = saves.RemoveAll(old => HasName(old, name)) > 0;
        saves.Add(save);

        repository.LastModified = now;
        _storage.Save(repository);
        return replaced;
    }

    /// <summary>
    /// Removes a save.
    /// </summary>
    /// <returns><c>true</c> if the save existed.</returns>
    public bool Delete(string system, string name)
    {
        Repository repository = _storage.Load();
        if (SavesOf(repository, system).RemoveAll(save => HasName(save, name)) == 0)
            return false;

        repository.LastModified = _clock();
        _storage.Save(repository);
        return true;
    }

    // Returns the list of saves of the system, creating missing parts of the repository.
    private static List<SaveBase> SavesOf(Repository repository, string system)
    {
        repository.Saves ??= new Dictionary<string, List<SaveBase>>();
        if (!repository.Saves.TryGetValue(system, out List<SaveBase>? saves))
        {
            saves = new List<SaveBase>();
            repository.Saves[system] = saves;
        }

        return saves;
    }

    private static bool HasName(SaveBase save, string name)
        => string.Equals(save.SaveName, name, StringComparison.OrdinalIgnoreCase);
}
