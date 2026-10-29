using ClassSystems.Infra.Data;
using ClassSystems.Tests.Doubles;

namespace ClassSystems.Tests.Infra;

/// <summary>
/// The store of named saves, tested with in-memory storages and a controlled clock.
/// </summary>
public class SaveStoreTests
{
    private const string System = "text";

    private DateTime _now = new(2026, 10, 1, 12, 0, 0);

    private SaveStore Store(IRepositoryStorage storage) => new(storage, () => _now);

    private static TextSave Save(string name) => new() { SaveName = name };

    [Fact]
    public void Put_ThenFind_ReturnsTheSave_IgnoringCase()
    {
        // Arrange
        SaveStore store = Store(new StubStorage());

        // Act
        store.Put(System, Save("Draft"));

        // Assert
        TextSave? found = store.Find<TextSave>(System, "draft");
        Assert.NotNull(found);
        Assert.Equal(_now, found.LastModified);
        Assert.Null(store.Find<CharacterSave>(System, "draft")); // wrong type
        Assert.Null(store.Find<TextSave>("chars", "draft"));     // wrong system
    }

    [Fact]
    public void Put_SameName_ReplacesTheOlderSave()
    {
        // Arrange
        SaveStore store = Store(new StubStorage());
        bool first = store.Put(System, Save("draft"));

        // Act
        bool second = store.Put(System, Save("DRAFT"));

        // Assert
        Assert.False(first);
        Assert.True(second);
        Assert.Single(store.List(System));
    }

    [Fact]
    public void List_IsSortedFromOldestToNewest()
    {
        // Arrange
        SaveStore store = Store(new StubStorage());
        store.Put(System, Save("b"));
        _now = _now.AddMinutes(-10);
        store.Put(System, Save("a"));

        // Act
        var names = store.List(System).Select(save => save.SaveName);

        // Assert
        Assert.Equal(new[] { "a", "b" }, names);
    }

    [Fact]
    public void Put_WritesTheRepositoryOnce()
    {
        // Arrange
        var storage = new MockStorage();
        SaveStore store = Store(storage);

        // Act
        store.Put(System, Save("draft"));

        // Assert
        Assert.Equal(1, storage.SaveCalls);
        Assert.Equal(_now, storage.Load().LastModified);
    }

    [Fact]
    public void Delete_MissingSave_DoesNotTouchTheStorage()
    {
        // Arrange
        var storage = new MockStorage();
        SaveStore store = Store(storage);

        // Act
        bool deleted = store.Delete(System, "nothing");

        // Assert
        Assert.False(deleted);
        Assert.Equal(0, storage.SaveCalls);
    }

    [Fact]
    public void Delete_ExistingSave_RemovesIt()
    {
        // Arrange
        SaveStore store = Store(new StubStorage());
        store.Put(System, Save("draft"));

        // Act
        bool deleted = store.Delete(System, "Draft");

        // Assert
        Assert.True(deleted);
        Assert.Empty(store.List(System));
    }

    [Fact]
    public void Put_SaveWithoutName_Throws()
    {
        SaveStore store = Store(new StubStorage());

        Assert.Throws<ArgumentException>(() => store.Put(System, new TextSave()));
    }

    [Fact]
    public void Put_BrokenStorage_PassesTheErrorOn()
    {
        SaveStore store = Store(new StubStorage { FailOnSave = true });

        Assert.Throws<IOException>(() => store.Put(System, Save("draft")));
    }
}
