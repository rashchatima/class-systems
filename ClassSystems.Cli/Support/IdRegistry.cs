namespace ClassSystems.Cli.Support;

/// <summary>
/// Gives objects short unique numbers so the user can point at them from the terminal.
/// </summary>
/// <remarks>
/// Identifiers are stored here and not in the model classes: they matter only for the command
/// interface, and the libraries stay free of interface concerns. An identifier is given the first
/// time an object is seen and never changes or repeats.
/// </remarks>
/// <typeparam name="T">Type of the objects.</typeparam>
public class IdRegistry<T> where T : class
{
    private readonly Dictionary<T, int> _ids = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<int, T> _objects = new();
    private int _nextId = 1;

    /// <summary>
    /// All objects that already have an identifier, in the order they got it.
    /// </summary>
    public IEnumerable<T> Objects => _objects.OrderBy(pair => pair.Key).Select(pair => pair.Value);

    /// <summary>
    /// Returns the identifier of the object, giving a new one if needed.
    /// </summary>
    public int IdOf(T item)
    {
        if (_ids.TryGetValue(item, out int id))
            return id;

        id = _nextId++;
        _ids[item] = id;
        _objects[id] = item;
        return id;
    }

    /// <summary>
    /// Finds an object by its identifier.
    /// </summary>
    public T? Find(int id) => _objects.GetValueOrDefault(id);

    /// <summary>
    /// Finds an object by an identifier (<c>3</c> or <c>#3</c>) or by a name.
    /// </summary>
    /// <param name="key">What the user typed.</param>
    /// <param name="candidates">Objects to search among.</param>
    /// <param name="nameOf">How to get the name of an object.</param>
    /// <returns>The object, or <c>null</c> if nothing matches.</returns>
    public T? Resolve(string key, IEnumerable<T> candidates, Func<T, string> nameOf)
    {
        List<T> list = candidates.ToList();
        string trimmed = key.TrimStart('#');

        if (int.TryParse(trimmed, out int id))
        {
            T? byId = list.FirstOrDefault(candidate => IdOf(candidate) == id);
            if (byId != null)
                return byId;
        }

        return list.FirstOrDefault(candidate => nameOf(candidate).Equals(key, StringComparison.OrdinalIgnoreCase));
    }
}
