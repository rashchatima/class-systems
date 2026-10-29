namespace ClassSystems.Remote.Models;

// The data exactly as the remote game database sends it (only the fields the program uses).
// Converting them to the classes of the game is the job of the core, not of this module.

/// <summary>
/// A talent (active skill) of a character.
/// </summary>
/// <param name="Type">NORMAL_ATTACK, ELEMENTAL_SKILL or ELEMENTAL_BURST.</param>
public record RemoteTalent(string Name, string? Unlock, string? Description, string? Type);

/// <summary>
/// A character of the game database. The database has no numeric stats, only descriptions.
/// </summary>
/// <param name="Id">The key used in addresses, for example <c>diluc</c>.</param>
/// <param name="Vision">The element of the character, for example Pyro.</param>
/// <param name="Weapon">The weapon type the character uses, for example Claymore.</param>
/// <param name="Rarity">4 or 5 stars.</param>
public record RemoteCharacter(
    string Id,
    string Name,
    string? Title,
    string? Vision,
    string? Weapon,
    string? Nation,
    int Rarity,
    string? Description,
    IReadOnlyList<RemoteTalent>? SkillTalents);

/// <summary>
/// A weapon of the game database.
/// </summary>
/// <param name="Type">Sword, Claymore, Polearm, Bow or Catalyst.</param>
/// <param name="SubStat">The secondary stat, for example ATK, HP or DEF; "-" if there is none.</param>
/// <param name="PassiveName">The name of the passive effect; "-" if there is none.</param>
public record RemoteWeapon(
    string Id,
    string Name,
    string? Type,
    int Rarity,
    int BaseAttack,
    string? SubStat,
    string? PassiveName,
    string? PassiveDesc);
