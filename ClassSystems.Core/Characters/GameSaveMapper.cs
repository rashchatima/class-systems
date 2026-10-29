using ClassSystems.Core.Characters.Models;
using ClassSystems.Infra.Data;
using ClassSystems.Infra.Data.Models;

namespace ClassSystems.Core.Characters;

/// <summary>
/// Converts the world of a game to a save (tables linked by ids) and back (objects linked by references).
/// </summary>
public static class GameSaveMapper
{
    /// <summary>
    /// The key the saves of the game are stored under in the repository.
    /// </summary>
    public const string System = "chars";

    /// <summary>
    /// Makes a save of the whole world of the game.
    /// </summary>
    public static CharacterSave ToSave(Game game, string name)
    {
        // Ids exist only in the save: they are numbers of the objects in the lists below.
        List<Ability> abilities = game.Abilities.ToList();
        List<Item> items = game.Items.ToList();
        List<Character> characters = game.Characters.ToList();

        int IdOf<T>(List<T> list, T value) => list.IndexOf(value) + 1;

        var characterItems = new List<CharacterItems>();
        var characterAbilities = new List<CharacterAbilities>();

        foreach (Character character in characters)
        {
            foreach (Item item in character.Equipment)
                characterItems.Add(new CharacterItems(characterItems.Count + 1, IdOf(characters, character), IdOf(items, item)));

            foreach (Ability ability in character.LearnedAbilities)
                characterAbilities.Add(new CharacterAbilities(characterAbilities.Count + 1, IdOf(characters, character), IdOf(abilities, ability)));
        }

        return new CharacterSave
        {
            SaveName = name,
            Abilities = abilities.Select(ability => new AbilityData(
                IdOf(abilities, ability), ability.Name, ability.Effect, ability.Description)).ToList(),
            Items = items.Select(item => new ItemData(
                IdOf(items, item), item.Name, item.HealthBonus, item.ArmorBonus, item.AttackBonus,
                item.GrantedAbility != null ? IdOf(abilities, item.GrantedAbility) : null,
                item.Kind, item.Description)).ToList(),
            Characters = characters.Select(character => new CharacterData(
                IdOf(characters, character), character.Name, character.ClassName,
                character.BaseStats.MaxHealth, character.BaseStats.Armor, character.BaseStats.AttackPower, character.BaseStats.HealPower,
                character.Health, character.IsDefending,
                character.Profile?.Title, character.Profile?.Element, character.Profile?.WeaponType,
                character.Profile?.Nation, character.Profile?.Rarity ?? 0, character.Profile?.Description)).ToList(),
            CharacterItems = characterItems,
            CharacterAbilities = characterAbilities,
        };
    }

    /// <summary>
    /// Replaces the world of the game with the one from the save.
    /// </summary>
    /// <exception cref="InvalidOperationException">The save refers to an object that is not in it.</exception>
    public static void Restore(Game game, CharacterSave save)
    {
        Dictionary<int, Ability> abilities = (save.Abilities ?? new()).ToDictionary(
            data => data.Id, data => new Ability(data.Name, data.Effect, data.Description));

        Dictionary<int, Item> items = (save.Items ?? new()).ToDictionary(
            data => data.Id,
            data => new Item(data.Name, data.HealthBonus, data.ArmorBonus, data.AttackBonus,
                data.AbilityId is int abilityId ? Find(abilities, abilityId, "ability") : null,
                data.Kind, data.Description));

        Dictionary<int, Character> characters = (save.Characters ?? new()).ToDictionary(
            data => data.Id,
            data => new Character(data.Name, data.Class, new Stats(data.MaxHealth, data.Armor, data.AttackPower, data.HealPower),
                data.HasProfile
                    ? new CharacterProfile(data.Title, data.Element, data.WeaponType, data.Nation, data.Rarity, data.Description)
                    : null));

        foreach (CharacterItems link in save.CharacterItems ?? new())
            Find(characters, link.CharacterId, "character").EquipmentList.Add(Find(items, link.ItemId, "item"));

        foreach (CharacterAbilities link in save.CharacterAbilities ?? new())
            Find(characters, link.CharacterId, "character").LearnedList.Add(Find(abilities, link.AbilityId, "ability"));

        // The current state is set last: equipment must already be on for the maximum health to be right.
        foreach (CharacterData data in save.Characters ?? new())
        {
            characters[data.Id].Health = Math.Clamp(data.Health, 0, characters[data.Id].MaxHealth);
            characters[data.Id].IsDefending = data.IsDefending;
        }

        game.Restore(characters.Values, items.Values, abilities.Values);
    }

    private static T Find<T>(Dictionary<int, T> table, int id, string what)
        => table.TryGetValue(id, out T? value)
            ? value
            : throw new InvalidOperationException($"The save refers to {what} #{id}, which is not in it.");
}
