using System.Text.Json.Serialization;

namespace ClassSystems.Infra.Data.Models;

// Data models of the game, organised like tables of a relational database:
// objects refer to each other by ids, and many-to-many relations are separate link tables.

/// <summary>
/// A row of the characters table: base stats without equipment plus the current state.
/// The last columns hold the profile of a character imported from the game database (empty otherwise).
/// </summary>
public record CharacterData(
    int Id,
    string Name,
    string Class,
    int MaxHealth,
    int Armor,
    int AttackPower,
    int HealPower,
    int Health,
    bool IsDefending,
    string? Title = null,
    string? Element = null,
    string? WeaponType = null,
    string? Nation = null,
    int Rarity = 0,
    string? Description = null)
{
    /// <summary>
    /// Whether any column of the profile is filled. Computed, so it is not written to the file.
    /// </summary>
    [JsonIgnore]
    public bool HasProfile => Title != null || Element != null || WeaponType != null
                              || Nation != null || Rarity > 0 || Description != null;
}

/// <summary>
/// A row of the abilities table. <see cref="Effect"/> names the rule that is applied when the ability is used.
/// </summary>
public record AbilityData(
    int Id,
    string Name,
    string Effect,
    string? Description = null);

/// <summary>
/// A row of the items table. <see cref="AbilityId"/> refers to the ability the item grants, if any.
/// </summary>
public record ItemData(
    int Id,
    string Name,
    int HealthBonus,
    int ArmorBonus,
    int AttackBonus,
    int? AbilityId,
    string? Kind = null,
    string? Description = null);

/// <summary>
/// Link table "character wears item" (many-to-many).
/// </summary>
public record CharacterItems(
    int Id,
    int CharacterId,
    int ItemId);

/// <summary>
/// Link table "character knows ability" (many-to-many); abilities granted by items are not stored here.
/// </summary>
public record CharacterAbilities(
    int Id,
    int CharacterId,
    int AbilityId);
