namespace ClassSystems.Infra.Data;

/// <summary>
/// The medium a <see cref="Repository"/> is kept in: a file, a database, memory.
/// </summary>
public interface IRepositoryStorage
{
    /// <summary>
    /// Reads the repository, or returns an empty one if nothing has been saved yet.
    /// </summary>
    Repository Load();

    /// <summary>
    /// Writes the whole repository.
    /// </summary>
    void Save(Repository repository);
}
