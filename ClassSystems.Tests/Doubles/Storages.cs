using ClassSystems.Infra.Data;

namespace ClassSystems.Tests.Doubles;

/// <summary>
/// Stub: keeps the repository in memory instead of a file. It can be configured to fail on saving,
/// which lets tests check how the code behaves when the storage is broken.
/// </summary>
public class StubStorage : IRepositoryStorage
{
    private Repository _repository = new();

    public bool FailOnSave { get; init; }

    public virtual Repository Load() => _repository;

    public virtual void Save(Repository repository)
    {
        if (FailOnSave)
            throw new IOException("The disk is full.");

        _repository = repository;
    }
}

/// <summary>
/// Mock: a stub that also counts how many times the storage was read and written.
/// </summary>
public class MockStorage : StubStorage
{
    public int LoadCalls { get; private set; }

    public int SaveCalls { get; private set; }

    public override Repository Load()
    {
        LoadCalls++;
        return base.Load();
    }

    public override void Save(Repository repository)
    {
        SaveCalls++;
        base.Save(repository);
    }
}
