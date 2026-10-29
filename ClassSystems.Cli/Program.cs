using ClassSystems.Cli.Commands;
using ClassSystems.Cli.Demos;
using ClassSystems.Cli.Modes;
using ClassSystems.Cli.Presenter;
using ClassSystems.Cli.Support;
using ClassSystems.Core.Characters;
using ClassSystems.Core.Text.Models;
using ClassSystems.Infra.Data;
using ClassSystems.Remote;

// Program options:
//   --text | --chars | --mode <text|chars>   choose the mode (without them a dialog asks)
//   --sample                                 start with ready sample data
//   --data <file>                            JSON file with the saves (default: classsystems-saves.json)
//   --load <name>                            start from a save
//   --demo                                   run the demonstrations of the previous practical works
var parser = new CommandParser(new Dictionary<string, string>
{
    ["w"] = "whole",
    ["i"] = "id",
    ["c"] = "char_id",
    ["m"] = "mode",
    ["s"] = "sample",
    ["d"] = "data",
    ["l"] = "load",
});

ParsedCommand options = parser.Parse(args);

// The only place where concrete implementations are chosen; everything else gets them as interfaces.
ITerminal terminal = new ConsoleTerminal();

if (options.HasOption("demo"))
{
    TextDemo.Run(terminal);
    terminal.WriteLine();
    GameDemo.Run(terminal);
    return;
}

var dialog = new Dialog(terminal);
var store = new SaveStore(new JsonFileStorage(options.Option("data") ?? "classsystems-saves.json"));
bool sample = options.HasOption("sample");
string? saveToLoad = options.Option("load");

string? modeName = options.HasOption("text") ? "text"
    : options.HasOption("chars") ? "chars"
    : options.Option("mode");

try
{
    if (modeName == null)
    {
        string answer = dialog.AskOptionalText("Choose a mode: text or chars [text]: ");
        modeName = answer.Length == 0 ? "text" : answer;
    }

    IMode mode;
    switch (modeName.ToLowerInvariant())
    {
        case "text":
            Document document = sample ? SampleData.Article(terminal)
                : saveToLoad != null ? new Document("Untitled")
                : new Document(dialog.AskText("Document title: "));
            var textMode = new TextMode(terminal, dialog, store, document);
            if (saveToLoad != null)
                textMode.Saves.Load(saveToLoad);
            mode = textMode;
            break;

        case "chars":
            var game = new Game(terminal);
            if (sample)
                SampleData.Fill(game);
            // Remote game database (https://genshin.jmp.blue), downloaded at most once per run.
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            IGameDatabase database = new CachedGameDatabase(new GenshinApiClient(http));
            var charactersMode = new CharactersMode(terminal, dialog, game, store, database);
            if (saveToLoad != null)
                charactersMode.Saves.Load(saveToLoad);
            mode = charactersMode;
            break;

        default:
            terminal.WriteLine($"Unknown mode \"{modeName}\". Use text or chars.");
            return;
    }

    terminal.WriteLine($"Mode: {mode.Name} — {mode.Description}");

    var registry = new CommandRegistry();
    mode.RegisterCommands(registry);
    new CommandShell(terminal, registry, parser).Run();
}
catch (InputEndedException)
{
    terminal.WriteLine();
}
catch (Exception error) when (error is ArgumentException or InvalidOperationException)
{
    terminal.WriteLine($"Error: {error.Message}");
}
