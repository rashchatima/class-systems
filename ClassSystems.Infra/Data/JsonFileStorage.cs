using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassSystems.Infra.Data;

/// <summary>
/// Keeps the repository in a JSON file using the serializer of the standard library.
/// </summary>
public class JsonFileStorage : IRepositoryStorage
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Keeps non-Latin text (for example Ukrainian) readable in the file.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <param name="path">The JSON file; it is created on the first save.</param>
    public JsonFileStorage(string path)
    {
        Path = path;
    }

    /// <summary>
    /// The JSON file the repository is kept in.
    /// </summary>
    public string Path { get; }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">The file exists but is not a valid repository.</exception>
    public Repository Load()
    {
        if (!File.Exists(Path))
            return new Repository();

        try
        {
            return JsonSerializer.Deserialize<Repository>(File.ReadAllText(Path), Options) ?? new Repository();
        }
        catch (JsonException error)
        {
            throw new InvalidOperationException($"The file {Path} is damaged: {error.Message}", error);
        }
    }

    /// <inheritdoc/>
    public void Save(Repository repository)
        => File.WriteAllText(Path, JsonSerializer.Serialize(repository, Options));
}
