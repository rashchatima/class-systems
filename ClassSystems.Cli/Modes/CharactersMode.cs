using ClassSystems.Cli.Commands;
using ClassSystems.Cli.Presenter;
using ClassSystems.Cli.Support;
using ClassSystems.Core.Characters;
using ClassSystems.Core.Characters.Display;
using ClassSystems.Core.Characters.Models;
using ClassSystems.Core.Characters.Remote;
using ClassSystems.Core.Characters.Rules;
using ClassSystems.Infra.Data;
using ClassSystems.Remote;

namespace ClassSystems.Cli.Modes;

/// <summary>
/// Commands for the game: creating characters, items and abilities, equipping them, acting,
/// saving and loading the world.
/// </summary>
/// <remarks>
/// The world and all game rules are in <see cref="Game"/>; this mode only translates commands into calls to it.
/// Characters, items and abilities can be chosen either by name or by id.
/// </remarks>
public class CharactersMode : IMode
{
    private readonly ITerminal _terminal;
    private readonly Dialog _dialog;
    private readonly Game _game;
    private readonly WorldIds _ids = new();
    private readonly SaveCommands<CharacterSave> _saves;
    private readonly IGameDatabase? _database;

    /// <param name="database">The remote database of characters and weapons; without it the db commands are not available.</param>
    public CharactersMode(ITerminal terminal, Dialog dialog, Game game, SaveStore store, IGameDatabase? database = null)
    {
        _terminal = terminal;
        _dialog = dialog;
        _game = game;
        _database = database;
        _saves = new SaveCommands<CharacterSave>(terminal, store, GameSaveMapper.System,
            name => GameSaveMapper.ToSave(_game, name),
            save =>
            {
                GameSaveMapper.Restore(_game, save);
                _ids.Reset();
            });
    }

    /// <inheritdoc/>
    public string Name => "chars";

    /// <inheritdoc/>
    public string Description => "game: characters, items and abilities";

    /// <summary>
    /// Save and load commands of this mode; also used for the <c>--load</c> option.
    /// </summary>
    public SaveCommands<CharacterSave> Saves => _saves;

    /// <inheritdoc/>
    public void RegisterCommands(CommandRegistry registry)
    {
        registry.Register("create", "create <char|item|ability>", "create a new object in a dialog", Create);
        registry.Register("add", "add <item|ability> [--char_id <id/name>] [--id <id/name>]", "give an item or an ability to a character", Add);
        registry.Register("act", "act <attack|heal|defend|ability> <actor> [<target>] [--id <ability id/name>]", "perform an action", Act);
        registry.Register("ls", "ls <char|item|ability> [--id <id/name>]", "list objects, or show one of them in full", List);
        registry.Register("status", "status", "show the state of all characters", _ => _game.ReportStatus());
        registry.Register("sheet", "sheet <character>", "show the full sheet of a character (structured text)", Sheet);
        _saves.Register(registry);

        if (_database == null)
            return;

        registry.Register("db", "db <chars|weapons> [--find <text>]", "list characters or weapons of the game database", Database);
        registry.Register("recruit", "recruit <db character id> [--name <name>]", "create a character from the game database", Recruit);
        registry.Register("arm", "arm <character> <db weapon id>", "equip a weapon from the game database", Arm);
    }

    private void Sheet(ParsedCommand command)
        => _terminal.Display(new CharacterSheet(FindCharacter(command.Argument(0) ?? _dialog.AskText("Character id/name: "))));

    private void Database(ParsedCommand command)
    {
        string kind = command.Argument(0) ?? _dialog.AskChoice("What to list", new[] { "chars", "weapons" });
        string? find = command.Option("find");
        bool Matches(params string?[] fields)
            => find == null || fields.Any(field => field?.Contains(find, StringComparison.OrdinalIgnoreCase) == true);

        switch (kind.ToLowerInvariant())
        {
            case "chars":
                PrintAll(Wait(_database!.GetCharactersAsync())
                        .Where(c => Matches(c.Id, c.Name, c.Vision, c.Weapon, c.Nation))
                        .OrderBy(c => c.Id),
                    c => $"{c.Id,-22} {c.Name} {RemoteMapper.Stars(c.Rarity)} — {c.Vision}, {c.Weapon}, {c.Nation}");
                break;

            case "weapons":
                PrintAll(Wait(_database!.GetWeaponsAsync())
                        .Where(w => Matches(w.Id, w.Name, w.Type))
                        .OrderBy(w => w.Id),
                    w => $"{w.Id,-34} {w.Name} {RemoteMapper.Stars(w.Rarity)} — {w.Type}, base ATK {w.BaseAttack}");
                break;

            default:
                throw new ArgumentException($"Unknown kind \"{kind}\". Use chars or weapons.");
        }
    }

    private void Recruit(ParsedCommand command)
    {
        string id = command.Argument(0) ?? _dialog.AskText("Database character id (see \"db chars\"): ");
        Character character = RemoteMapper.ToCharacter(Wait(_database!.GetCharacterAsync(id)), command.Option("name"));
        _game.Join(character);
        _terminal.WriteLine($"Recruited character #{_ids.Characters.IdOf(character)} {new CharacterDto(character).Render()}");
    }

    private void Arm(ParsedCommand command)
    {
        Character character = FindCharacter(command.Argument(0) ?? _dialog.AskText("Character id/name: "));
        string id = command.Argument(1) ?? _dialog.AskText("Database weapon id (see \"db weapons\"): ");
        Item weapon = RemoteMapper.ToItem(Wait(_database!.GetWeaponAsync(id)));
        _game.Equip(character, weapon);
    }

    // The command interface is synchronous, so remote calls are waited for here.
    private static T Wait<T>(Task<T> task) => task.GetAwaiter().GetResult();

    private IEnumerable<Character> Characters => _game.Characters;

    private void Create(ParsedCommand command)
    {
        string kind = command.Argument(0) ?? _dialog.AskChoice("What to create", new[] { "char", "item", "ability" });

        switch (kind.ToLowerInvariant())
        {
            case "char":
                string className = _dialog.AskChoice("Class", CharacterClasses.Keys);
                Character character = CharacterClasses.Create(className, _dialog.AskText("Name: "));
                _game.Join(character);
                _terminal.WriteLine($"Created character #{_ids.Characters.IdOf(character)} {new CharacterDto(character).Render()}");
                break;

            case "ability":
                Ability ability = AbilityCatalog.Create(_dialog.AskChoice("Ability", AbilityCatalog.Keys));
                _game.AddAbility(ability);
                _terminal.WriteLine($"Created ability #{_ids.Abilities.IdOf(ability)} {ability.Name}");
                break;

            case "item":
                string name = _dialog.AskText("Item name: ");
                int health = _dialog.AskNumber("Health bonus");
                int armor = _dialog.AskNumber("Armor bonus");
                int attack = _dialog.AskNumber("Attack bonus");
                string abilityKey = _dialog.AskOptionalText("Granted ability id/name (empty for none): ");
                Ability? granted = abilityKey.Length == 0 ? null : FindAbility(abilityKey, _game.Abilities);
                var item = new Item(name, health, armor, attack, granted);
                _game.AddItem(item);
                _terminal.WriteLine($"Created item {new ItemCard(item, _ids).Render()}");
                break;

            default:
                throw new ArgumentException($"Unknown kind \"{kind}\". Use char, item or ability.");
        }
    }

    private void Add(ParsedCommand command)
    {
        string kind = command.Argument(0) ?? _dialog.AskChoice("What to add", new[] { "item", "ability" });
        Character character = FindCharacter(command.Option("char_id") ?? _dialog.AskText("Character id/name: "));
        string key = command.Option("id") ?? _dialog.AskText($"{kind} id/name: ");

        if (kind.Equals("item", StringComparison.OrdinalIgnoreCase))
            _game.Equip(character, FindItem(key));
        else if (kind.Equals("ability", StringComparison.OrdinalIgnoreCase))
            _game.Teach(character, FindAbility(key, _game.Abilities));
        else
            throw new ArgumentException($"Unknown kind \"{kind}\". Use item or ability.");
    }

    private void Act(ParsedCommand command)
    {
        string action = command.Argument(0) ?? throw new ArgumentException("Choose an action: attack, heal, defend or ability.");
        Character actor = FindCharacter(command.Argument(1) ?? throw new ArgumentException("Give the actor."));

        if (action.Equals("defend", StringComparison.OrdinalIgnoreCase))
        {
            _game.Defend(actor);
            return;
        }

        Character target = FindCharacter(command.Argument(2) ?? throw new ArgumentException("Give the target."));

        switch (action.ToLowerInvariant())
        {
            case "attack":
                _game.Attack(actor, target);
                break;

            case "heal":
                _game.Heal(actor, target);
                break;

            case "ability":
                List<Ability> available = actor.Abilities.ToList();
                string? key = command.Option("id");
                Ability ability = key != null
                    ? FindAbility(key, available)
                    : available.Count == 1
                        ? available[0]
                        : throw new ArgumentException($"{actor.Name} has several abilities; choose one with --id.");
                _game.UseAbility(actor, ability, target);
                break;

            default:
                throw new ArgumentException($"Unknown action \"{action}\". Use attack, heal, defend or ability.");
        }
    }

    private void List(ParsedCommand command)
    {
        string kind = command.Argument(0) ?? throw new ArgumentException("Choose what to list: char, item or ability.");
        string? key = command.Option("id");

        switch (kind.ToLowerInvariant())
        {
            case "char":
                if (key != null)
                    _terminal.Display(new CharacterCard(FindCharacter(key), _ids));
                else
                    PrintAll(Characters, character => $"#{_ids.Characters.IdOf(character)} {new CharacterDto(character).Render()}");
                break;

            case "item":
                if (key != null)
                {
                    Item item = FindItem(key);
                    _terminal.Display(new ItemCard(item, _ids));
                    _terminal.WriteLine($"  equipped by: {_game.OwnerOf(item)?.Name ?? "none"}");
                }
                else
                {
                    PrintAll(_game.Items, item => $"#{_ids.Items.IdOf(item)} {item.Name} — equipped by: {_game.OwnerOf(item)?.Name ?? "none"}");
                }
                break;

            case "ability":
                if (key != null)
                {
                    Ability ability = FindAbility(key, _game.Abilities);
                    _terminal.WriteLine($"#{_ids.Abilities.IdOf(ability)} {ability.Name} ({ability.Effect})");
                    _terminal.WriteLine($"  can be used by: {Users(ability)}");
                    _terminal.WriteLine($"  granted by items: {Cards.Join(_game.Items.Where(item => item.GrantedAbility == ability).Select(item => item.Name))}");
                }
                else
                {
                    PrintAll(_game.Abilities, ability => $"#{_ids.Abilities.IdOf(ability)} {ability.Name} — can be used by: {Users(ability)}");
                }
                break;

            default:
                throw new ArgumentException($"Unknown kind \"{kind}\". Use char, item or ability.");
        }
    }

    private void PrintAll<T>(IEnumerable<T> objects, Func<T, string> describe)
    {
        List<string> lines = objects.Select(describe).ToList();
        if (lines.Count == 0)
            _terminal.WriteLine("(none)");
        foreach (string line in lines)
            _terminal.WriteLine("  " + line);
    }

    private Character FindCharacter(string key)
        => _ids.Characters.Resolve(key, Characters, character => character.Name)
           ?? throw new ArgumentException($"There is no character \"{key}\".");

    private Item FindItem(string key)
        => _ids.Items.Resolve(key, _game.Items, item => item.Name)
           ?? throw new ArgumentException($"There is no item \"{key}\".");

    private Ability FindAbility(string key, IEnumerable<Ability> candidates)
        => _ids.Abilities.Resolve(key, candidates, ability => ability.Name)
           ?? throw new ArgumentException($"There is no ability \"{key}\".");

    private string Users(Ability ability)
        => Cards.Join(Characters.Where(character => character.Abilities.Contains(ability)).Select(character => character.Name));
}
