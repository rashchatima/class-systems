using ClassSystems.Cli.Commands;
using ClassSystems.Cli.Presenter;
using ClassSystems.Cli.Support;
using ClassSystems.Core.Text;
using ClassSystems.Core.Text.Display;
using ClassSystems.Core.Text.Formats;
using ClassSystems.Core.Text.Models;
using ClassSystems.Core.Text.Templates;
using ClassSystems.Infra.Data;
using ClassSystems.Infra.Display;

namespace ClassSystems.Cli.Modes;

/// <summary>
/// Commands for working with a structured text: moving around the tree, printing, adding and removing
/// elements, saving and loading documents, exporting them to a file.
/// </summary>
/// <remarks>
/// The mode remembers the path from the document to the current section, similar to the current directory
/// in a file system: new elements are added to the current section, and <c>pwd</c>, <c>cd</c> and <c>up</c>
/// move along the path.
/// </remarks>
public class TextMode : IMode
{
    private static readonly string BlockSeparator = Environment.NewLine + Environment.NewLine;

    private readonly ITerminal _terminal;
    private readonly Dialog _dialog;
    private readonly SaveCommands<TextSave> _saves;

    // Element types the user can add. A new type is supported by adding one line here.
    private readonly Dictionary<string, Func<ITextSection>> _containerTypes;
    private readonly Dictionary<string, Func<ITextElement>> _leafTypes;

    // Prototypes for new elements (the Prototype pattern, see TemplateLibrary).
    private readonly TemplateLibrary _templates;

    private Document _document;
    private IdRegistry<ITextElement> _ids = new();

    // Sections from the document down to the current one; the document is always the first.
    private List<ITextSection> _path;

    /// <param name="templates">Templates for <c>template add</c>; the standard ones by default.</param>
    public TextMode(ITerminal terminal, Dialog dialog, SaveStore store, Document document, TemplateLibrary? templates = null)
    {
        _terminal = terminal;
        _dialog = dialog;
        _templates = templates ?? TemplateLibrary.WithDefaults();
        _document = document;
        _path = new List<ITextSection> { document };
        _saves = new SaveCommands<TextSave>(terminal, store, TextSaveMapper.System,
            name => TextSaveMapper.ToSave(_document, name),
            save => Open(TextSaveMapper.FromSave(save)));

        _containerTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["heading"] = () => new Heading(_dialog.AskText("Heading title: ")),
        };
        _leafTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["paragraph"] = () => new Paragraph(_dialog.AskText("Paragraph text: ")),
            ["link"] = () => new Link(_dialog.AskText("Link label: "), _dialog.AskText("Link address: ")),
            ["image"] = () => new Image(_dialog.AskText("Image description: "), _dialog.AskText("Image path: ")),
        };
    }

    /// <inheritdoc/>
    public string Name => "text";

    /// <inheritdoc/>
    public string Description => "structured text: headings, paragraphs, links and images";

    /// <summary>
    /// Save and load commands of this mode; also used for the <c>--load</c> option.
    /// </summary>
    public SaveCommands<TextSave> Saves => _saves;

    private ITextSection Current => _path[^1];

    /// <inheritdoc/>
    public void RegisterCommands(CommandRegistry registry)
    {
        registry.Register("pwd", "pwd", "show the path of the current element", _ => PrintPath());
        registry.Register("ls", "ls", "list the elements of the current container with their ids", _ => List());
        registry.Register("print", "print [--whole] [--id] [--toc] [--format <md|html|text>]", "print the current element or the whole document; --id adds ids, --toc prints the table of contents", Print);
        registry.Register("export", "export <file> [--whole] [--format <md|html|text>]", "write the current element or the whole document to a file; the format follows the extension", Export);
        registry.Register("copy", "copy <name or #id>", "put a copy of an element of the current container right after it", Copy);
        registry.Register("template", "template <list|add|save> [<name>] [<element name or #id>]", "list templates, add an element from a template, or save an element as a template", Template);
        registry.Register("add", "add <container|leaf> <type>", "create a new element in the current container", Add);
        registry.Register("rm", "rm [<name or #id>]", "remove the named element, or the current one and go up", Remove);
        registry.Register("up", "up", "go to the parent container", _ => Up());
        registry.Register("cd", "cd <path> | cd --id <container id>", "go to a container by path (/a/b, .., name) or by id", ChangeContainer);
        registry.Register("new", "new [<title>]", "start a new empty document", NewDocument);
        _saves.Register(registry);
    }

    // Makes the document the one being edited; ids start again because all objects are new.
    private void Open(Document document)
    {
        _document = document;
        _path = new List<ITextSection> { document };
        _ids = new IdRegistry<ITextElement>();
    }

    private void NewDocument(ParsedCommand command)
    {
        Open(new Document(command.Argument(0) ?? _dialog.AskText("Document title: ")));
        _terminal.WriteLine($"New document \"{_document.Title}\".");
    }

    private void PrintPath() => _terminal.WriteLine(PathText(_path));

    private void List()
    {
        if (Current.Children.Count == 0)
        {
            _terminal.WriteLine("(empty)");
            return;
        }

        foreach (ITextElement child in Current.Children)
            _terminal.WriteLine($"  {"#" + _ids.IdOf(child),-5} {TextTree.KindOf(child),-9} {TextTree.NameOf(child)}");
    }

    private void Print(ParsedCommand command)
    {
        bool whole = command.HasOption("whole");
        ITextSection target = whole ? _document : Current;
        int depth = whole ? 0 : _path.Count - 1;

        if (command.HasOption("toc"))
            _terminal.Display(new TableOfContentsDto(target));
        else if (command.HasOption("id"))
            _terminal.WriteLine(string.Join(BlockSeparator, BlocksWithIds(target, depth)));
        else
            _terminal.Display(new TextDto(target, depth, format: FormatOf(command) ?? TextFormats.Default));
    }

    // The same text as print, but another displayer: a file instead of the terminal.
    private void Export(ParsedCommand command)
    {
        string file = command.Argument(0) ?? throw new ArgumentException("Give the name of the file.");
        bool whole = command.HasOption("whole");
        ITextFormat format = FormatOf(command) ?? TextFormats.ForFile(file);

        try
        {
            using (var displayer = new FileDisplayer(file))
                displayer.Display(new TextDto(whole ? _document : Current, whole ? 0 : _path.Count - 1, format: format));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Cannot write {file}: {error.Message}", error);
        }

        _terminal.WriteLine($"Written to {Path.GetFullPath(file)} ({format.Name})");
    }

    private static ITextFormat? FormatOf(ParsedCommand command)
        => command.Option("format") is string name ? TextFormats.Find(name) : null;

    // Prototype: the element copies itself, whatever its type is.
    private void Copy(ParsedCommand command)
    {
        ITextElement original = FindChild(command.Argument(0) ?? _dialog.AskText("Element name or #id: "));
        ITextElement copy = original.DeepCopy();
        TextTree.Insert(Current, Current.Children.FindIndex(child => ReferenceEquals(child, original)) + 1, copy);
        _terminal.WriteLine($"Copied {TextTree.KindOf(original)} \"{TextTree.NameOf(original)}\" as #{_ids.IdOf(copy)}");
    }

    private void Template(ParsedCommand command)
    {
        string action = command.Argument(0) ?? _dialog.AskChoice("Action", new[] { "list", "add", "save" });

        switch (action.ToLowerInvariant())
        {
            case "list":
                foreach (string name in _templates.Names)
                {
                    ITextElement prototype = _templates.Peek(name);
                    _terminal.WriteLine($"  {name,-12} {TextTree.KindOf(prototype),-9} {TextTree.NameOf(prototype)}");
                }
                break;

            case "add":
                ITextElement element = _templates.Create(command.Argument(1) ?? _dialog.AskChoice("Template", _templates.Names));
                TextTree.Add(Current, element);
                _terminal.WriteLine($"Added {TextTree.KindOf(element)} #{_ids.IdOf(element)} \"{TextTree.NameOf(element)}\" to {PathText(_path)}");
                break;

            case "save":
                string templateName = command.Argument(1) ?? _dialog.AskText("Template name: ");
                ITextElement source = command.Argument(2) is string key ? FindChild(key) : Current;
                _templates.Register(templateName, source);
                _terminal.WriteLine($"Saved {TextTree.KindOf(source)} \"{TextTree.NameOf(source)}\" as template \"{templateName}\".");
                break;

            default:
                throw new ArgumentException($"Unknown action \"{action}\". Use list, add or save.");
        }
    }

    private ITextElement FindChild(string key)
        => _ids.Resolve(key, Current.Children, TextTree.NameOf)
           ?? throw new ArgumentException($"There is no element \"{key}\" in {PathText(_path)}.");

    private void Add(ParsedCommand command)
    {
        string category = command.Argument(0) ?? _dialog.AskChoice("Category", new[] { "container", "leaf" });

        ITextElement element;
        if (category.Equals("container", StringComparison.OrdinalIgnoreCase))
            element = Create(_containerTypes, command.Argument(1));
        else if (category.Equals("leaf", StringComparison.OrdinalIgnoreCase))
            element = Create(_leafTypes, command.Argument(1));
        else
            throw new ArgumentException($"Unknown category \"{category}\". Use container or leaf.");

        TextTree.Add(Current, element);
        _terminal.WriteLine($"Added {TextTree.KindOf(element)} #{_ids.IdOf(element)} to {PathText(_path)}");
    }

    private T Create<T>(Dictionary<string, Func<T>> types, string? typeName)
    {
        string type = typeName ?? _dialog.AskChoice("Type", types.Keys.ToList());
        if (!types.TryGetValue(type, out Func<T>? create))
            throw new ArgumentException($"Unknown type \"{type}\". Available: {string.Join(", ", types.Keys)}.");

        return create();
    }

    private void Remove(ParsedCommand command)
    {
        string? name = command.Argument(0);

        if (name != null)
        {
            ITextElement element = _ids.Resolve(name, Current.Children, TextTree.NameOf)
                ?? throw new ArgumentException($"There is no element \"{name}\" in {PathText(_path)}.");

            if (_dialog.Confirm($"Remove {TextTree.KindOf(element)} \"{TextTree.NameOf(element)}\"?"))
            {
                TextTree.Remove(Current, element);
                _terminal.WriteLine("Removed.");
            }
            return;
        }

        if (_path.Count == 1)
            throw new InvalidOperationException("The document itself cannot be removed.");

        if (_dialog.Confirm($"Remove the current element \"{Current.Title}\" with everything inside?"))
        {
            ITextSection removed = Current;
            _path.RemoveAt(_path.Count - 1);
            TextTree.Remove(Current, removed);
            _terminal.WriteLine($"Removed. Now in {PathText(_path)}");
        }
    }

    private void Up()
    {
        if (_path.Count == 1)
            throw new InvalidOperationException("Already at the top of the document.");

        _path.RemoveAt(_path.Count - 1);
        _terminal.WriteLine(PathText(_path));
    }

    private void ChangeContainer(ParsedCommand command)
    {
        string? id = command.Option("id");
        if (id != null)
        {
            var section = _ids.Resolve(id, TextTree.Sections(_document), TextTree.NameOf) as ITextSection
                ?? throw new ArgumentException($"There is no container with id {id}.");
            _path = TextTree.PathTo(_document, section)!;
        }
        else
        {
            string path = command.Argument(0) ?? throw new ArgumentException("Give a path or --id <container id>.");
            _path = ResolvePath(path);
        }

        _terminal.WriteLine(PathText(_path));
    }

    // Walks a path such as "/Modern Chess/Rules", "../Origins" or "Rules" from the current container.
    private List<ITextSection> ResolvePath(string path)
    {
        List<ITextSection> position = path.StartsWith('/') ? new() { _document } : new(_path);

        foreach (string part in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == ".")
                continue;

            if (part == "..")
            {
                if (position.Count == 1)
                    throw new ArgumentException("The path goes above the document.");
                position.RemoveAt(position.Count - 1);
                continue;
            }

            ITextSection next = position[^1].Children.OfType<ITextSection>()
                .FirstOrDefault(child => child.Title.Equals(part, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"There is no container \"{part}\" in {PathText(position)}.");
            position.Add(next);
        }

        return position;
    }

    private IEnumerable<string> BlocksWithIds(ITextElement element, int depth)
    {
        yield return $"[#{_ids.IdOf(element)}] {new TextDto(element, depth, titleOnly: true).Render()}";

        if (element is ITextSection section)
        {
            foreach (ITextElement child in section.Children)
            foreach (string block in BlocksWithIds(child, depth + 1))
                yield return block;
        }
    }

    private static string PathText(List<ITextSection> path)
        => "/" + string.Join("/", path.Skip(1).Select(section => section.Title));
}
