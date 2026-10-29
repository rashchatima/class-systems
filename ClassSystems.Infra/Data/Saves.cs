using System.Text.Json.Serialization;
using ClassSystems.Infra.Data.Models;

namespace ClassSystems.Infra.Data;

/// <summary>
/// A named snapshot of one of the systems.
/// </summary>
/// <remarks>
/// The attributes let the JSON serializer write the concrete type of a save into the file
/// (property <c>$type</c>) and create the right class when reading it back.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(CharacterSave), "chars")]
[JsonDerivedType(typeof(TextSave), "text")]
public abstract class SaveBase
{
    /// <summary>
    /// The name the user gave to the save.
    /// </summary>
    public string? SaveName { get; set; }

    /// <summary>
    /// When the save was written.
    /// </summary>
    public DateTime LastModified { get; set; }
}

/// <summary>
/// The state of the game world: characters, items, abilities and the links between them.
/// </summary>
public class CharacterSave : SaveBase
{
    public List<CharacterData>? Characters { get; set; }

    public List<AbilityData>? Abilities { get; set; }

    public List<ItemData>? Items { get; set; }

    public List<CharacterItems>? CharacterItems { get; set; }

    public List<CharacterAbilities>? CharacterAbilities { get; set; }
}

/// <summary>
/// A whole document of the text system.
/// </summary>
public class TextSave : SaveBase
{
    public TextNodeData? TextRoot { get; set; }
}

/// <summary>
/// Everything the program has saved, grouped by system ("text", "chars").
/// </summary>
/// <remarks>
/// The whole repository is serialized to one JSON file, so the state of every system can be restored later.
/// </remarks>
public class Repository
{
    public DateTime LastModified { get; set; }

    public Dictionary<string, List<SaveBase>>? Saves { get; set; }
}
