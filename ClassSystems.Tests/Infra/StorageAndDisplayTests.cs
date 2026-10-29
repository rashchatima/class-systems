using ClassSystems.Infra.Data;
using ClassSystems.Infra.Data.Models;
using ClassSystems.Infra.Display;

namespace ClassSystems.Tests.Infra;

/// <summary>
/// The JSON file storage and the displayers. These tests use real temporary files.
/// </summary>
public sealed class StorageAndDisplayTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ClassSystemsTests_" + Guid.NewGuid().ToString("N"));

    public StorageAndDisplayTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string FilePath(string name) => Path.Combine(_folder, name);

    [Fact]
    public void JsonStorage_MissingFile_GivesAnEmptyRepository()
    {
        Repository repository = new JsonFileStorage(FilePath("none.json")).Load();

        Assert.Null(repository.Saves);
    }

    [Fact]
    public void JsonStorage_RoundTrip_KeepsConcreteSaveTypes()
    {
        // Arrange
        var storage = new JsonFileStorage(FilePath("saves.json"));
        var repository = new Repository
        {
            Saves = new()
            {
                ["text"] = new() { new TextSave { SaveName = "Чернетка", TextRoot = new TextNodeData("document", "Звіт") } },
                ["chars"] = new() { new CharacterSave { SaveName = "fight" } },
            },
        };

        // Act
        storage.Save(repository);
        Repository loaded = storage.Load();

        // Assert
        var text = Assert.IsType<TextSave>(Assert.Single(loaded.Saves!["text"]));
        Assert.Equal("Чернетка", text.SaveName);
        Assert.Equal("Звіт", text.TextRoot!.Text);
        Assert.IsType<CharacterSave>(Assert.Single(loaded.Saves["chars"]));
        Assert.Contains("Чернетка", File.ReadAllText(storage.Path)); // Ukrainian text stays readable
    }

    [Fact]
    public void JsonStorage_DamagedFile_ThrowsAReadableError()
    {
        // Arrange
        string path = FilePath("broken.json");
        File.WriteAllText(path, "{ not json");

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => new JsonFileStorage(path).Load());

        // Assert
        Assert.Contains("damaged", error.Message);
    }

    [Fact]
    public void FileDisplayer_WritesEveryCallOnItsOwnLine()
    {
        // Arrange
        string path = FilePath("out.txt");

        // Act
        using (var displayer = new FileDisplayer(path))
        {
            displayer.Display("first");
            displayer.Display(new Fixed("second"));
        }

        // Assert
        Assert.Equal(new[] { "first", "second" }, File.ReadAllLines(path));
    }

    [Fact]
    public void MemoryDisplayer_KeepsLinesInOrder()
    {
        var displayer = new MemoryDisplayer();

        displayer.Display("one");
        displayer.Display(new Fixed("two"));

        Assert.Equal(new[] { "one", "two" }, displayer.Lines);
    }

    // The simplest displayable object: always renders the same text.
    private sealed record Fixed(string Text) : IDisplayable
    {
        public string Render() => Text;
    }
}
