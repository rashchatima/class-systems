using ClassSystems.Core.Characters.Display;
using ClassSystems.Core.Characters.Models;
using ClassSystems.Core.Characters.Rules;
using ClassSystems.Infra.Display;

namespace ClassSystems.Core.Characters;

/// <summary>
/// The facade of the game: keeps the world (characters, items, abilities), performs actions
/// and reports what happens.
/// </summary>
/// <remarks>
/// Reports go to an <see cref="IDisplayer"/> given in the constructor and replaceable with
/// <see cref="SetDisplayer"/>, so the game does not know whether it writes to a console, a file or a test.
/// </remarks>
public class Game
{
    private readonly List<Character> _characters = new();
    private readonly List<Item> _items = new();
    private readonly List<Ability> _abilities = new();

    private IDisplayer _displayer;

    /// <param name="displayer">Receives the description of every event in the game.</param>
    public Game(IDisplayer displayer)
    {
        _displayer = displayer;
    }

    /// <summary>
    /// Characters taking part in the fight.
    /// </summary>
    public IReadOnlyList<Character> Characters => _characters;

    /// <summary>
    /// Every item of the world, worn or not.
    /// </summary>
    public IReadOnlyList<Item> Items => _items;

    /// <summary>
    /// Every ability of the world: created ones, known by characters and granted by items.
    /// </summary>
    public IEnumerable<Ability> Abilities => _abilities
        .Concat(_characters.SelectMany(character => character.Abilities))
        .Concat(_items.Select(item => item.GrantedAbility).OfType<Ability>())
        .Distinct();

    /// <summary>
    /// Whether the fight has ended: no more than one character is left standing.
    /// </summary>
    public bool IsOver => _characters.Count(character => character.IsAlive) <= 1;

    /// <summary>
    /// The last character standing, or <c>null</c> while the fight goes on.
    /// </summary>
    public Character? Winner => IsOver ? _characters.FirstOrDefault(character => character.IsAlive) : null;

    /// <summary>
    /// Replaces the output of the game while the program runs.
    /// </summary>
    public void SetDisplayer(IDisplayer displayer) => _displayer = displayer;

    /// <summary>
    /// Adds a character to the fight.
    /// </summary>
    public void Join(Character character)
    {
        if (!_characters.Contains(character))
            _characters.Add(character);
    }

    /// <summary>
    /// Adds an item to the world so that it can be given to a character later.
    /// </summary>
    public void AddItem(Item item)
    {
        if (!_items.Contains(item))
            _items.Add(item);
    }

    /// <summary>
    /// Adds an ability to the world so that it can be taught or granted by an item later.
    /// </summary>
    public void AddAbility(Ability ability)
    {
        if (!_abilities.Contains(ability))
            _abilities.Add(ability);
    }

    /// <summary>
    /// The character who wears the item, or <c>null</c>.
    /// </summary>
    public Character? OwnerOf(Item item) => _characters.FirstOrDefault(character => character.Equipment.Contains(item));

    /// <summary>
    /// Hits the target with a regular attack.
    /// </summary>
    public void Attack(Character attacker, Character target)
    {
        if (!CanAct(attacker))
            return;

        int damage = Combat.TakeDamage(target, attacker.AttackPower);
        _displayer.Display($"{attacker.Name} attacks {target.Name} for {damage} damage.");
        ReportIfDefeated(target);
    }

    /// <summary>
    /// Restores health of the target. A character can heal itself as well.
    /// </summary>
    public void Heal(Character healer, Character target)
    {
        if (!CanAct(healer))
            return;

        int restored = Combat.RestoreHealth(target, healer.HealPower);
        _displayer.Display($"{healer.Name} heals {target.Name} for {restored} health.");
    }

    /// <summary>
    /// Makes the character take a defensive stance that weakens the next incoming hit.
    /// </summary>
    public void Defend(Character character)
    {
        if (!CanAct(character))
            return;

        character.IsDefending = true;
        _displayer.Display($"{character.Name} takes a defensive stance.");
    }

    /// <summary>
    /// Uses one of the abilities currently available to the character, chosen by name.
    /// </summary>
    public void UseAbility(Character user, string abilityName, Character target)
    {
        Ability? ability = user.Abilities.FirstOrDefault(known => known.Name == abilityName);

        if (ability == null)
        {
            if (CanAct(user))
                _displayer.Display($"{user.Name} does not know how to use {abilityName}.");
            return;
        }

        UseAbility(user, ability, target);
    }

    /// <summary>
    /// Uses a specific ability of the character. Useful when several abilities share a name.
    /// </summary>
    public void UseAbility(Character user, Ability ability, Character target)
    {
        if (!CanAct(user))
            return;

        if (!user.Abilities.Contains(ability))
        {
            _displayer.Display($"{user.Name} does not have the ability {ability.Name}.");
            return;
        }

        _displayer.Display(AbilityCatalog.Apply(ability, user, target));
        ReportIfDefeated(target);
    }

    /// <summary>
    /// Teaches the character an ability that does not depend on the equipment.
    /// </summary>
    public void Teach(Character character, Ability ability)
    {
        if (!CanAct(character))
            return;

        AddAbility(ability);
        if (character.LearnedList.Contains(ability))
        {
            _displayer.Display($"{character.Name} already knows this {ability.Name}.");
            return;
        }

        character.LearnedList.Add(ability);
        _displayer.Display($"{character.Name} learns {ability.Name}.");
    }

    /// <summary>
    /// Puts an item on the character. An item can be worn by one character at a time,
    /// and a weapon only by a character that uses this kind of weapon.
    /// </summary>
    public void Equip(Character character, Item item)
    {
        if (!CanAct(character))
            return;

        string? weaponType = character.Profile?.WeaponType;
        if (weaponType != null && item.Kind != null && !weaponType.Equals(item.Kind, StringComparison.OrdinalIgnoreCase))
        {
            _displayer.Display($"{character.Name} can only use {weaponType} weapons, not {item.Kind}.");
            return;
        }

        Character? owner = OwnerOf(item);
        if (owner != null)
        {
            _displayer.Display($"{item.Name} is already equipped by {owner.Name}.");
            return;
        }

        AddItem(item);
        character.EquipmentList.Add(item);

        // Extra maximum health comes filled, otherwise the bonus would give nothing in a fight.
        character.Health += item.HealthBonus;
        _displayer.Display($"{character.Name} equips {item.Name}.");
    }

    /// <summary>
    /// Takes an item off the character.
    /// </summary>
    public void Unequip(Character character, Item item)
    {
        if (!CanAct(character))
            return;

        if (!character.EquipmentList.Remove(item))
        {
            _displayer.Display($"{character.Name} does not have {item.Name} equipped.");
            return;
        }

        character.Health = Math.Min(character.Health, character.MaxHealth);
        _displayer.Display($"{character.Name} takes off {item.Name}.");
    }

    /// <summary>
    /// Reports the current state of every character.
    /// </summary>
    public void ReportStatus() => _displayer.Display(new StatusReportDto(_characters));

    /// <summary>
    /// Replaces the whole world, for example with one loaded from a save.
    /// </summary>
    internal void Restore(IEnumerable<Character> characters, IEnumerable<Item> items, IEnumerable<Ability> abilities)
    {
        _characters.Clear();
        _characters.AddRange(characters);
        _items.Clear();
        _items.AddRange(items);
        _abilities.Clear();
        _abilities.AddRange(abilities);
    }

    // Checks that the character takes part in the fight and is still able to act; otherwise reports why not.
    private bool CanAct(Character actor)
    {
        if (!_characters.Contains(actor))
        {
            _displayer.Display($"{actor.Name} does not take part in this fight.");
            return false;
        }

        if (!actor.IsAlive)
        {
            _displayer.Display($"{actor.Name} is defeated and cannot act.");
            return false;
        }

        return true;
    }

    // Reports the defeat of the character and the end of the fight, if they have happened.
    private void ReportIfDefeated(Character target)
    {
        if (target.IsAlive)
            return;

        _displayer.Display($"{target.Name} is defeated.");

        if (Winner != null)
            _displayer.Display($"{Winner.Name} wins the fight!");
    }
}
