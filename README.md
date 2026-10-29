# ClassSystems

A layered C# (.NET 8) console application that grew through a whole OOP course: a **structured-text editor**
and a small **turn-based character game**, both driven by one command-line shell, with persistence,
a remote game database and **146 xUnit tests**.

[Українська версія](README.uk.md) · design notes: [PATTERNS.md](PATTERNS.md)

## Highlights

- **Clean layering:** `Cli → Core → Infra / Remote`. The core depends only on interfaces
  (`IDisplayer`, `IRepositoryStorage`, `IGameDatabase`), so every dependency can be replaced in tests.
- **Structured text:** a tree of documents, headings, paragraphs, links and images; rendering to
  **Markdown, HTML (encoded) and plain text** through an Abstract Factory of formats; deep-copy prototypes
  and a template library.
- **Game:** characters, items and abilities with combat rules; characters and weapons imported from the
  public [Genshin Impact API](https://genshin.jmp.blue) via `HttpClient`, with caching, error handling and
  deterministic stat generation.
- **Persistence:** the whole state is saved to JSON (polymorphic saves, relational-style link tables).
- **Testing:** xUnit with hand-written fakes, stubs and mocks, a fixture with recorded API answers (no network
  needed), end-to-end tests of the command shell.

## Design patterns used

Abstract Factory · Singleton · Prototype · Builder · Facade · Decorator (caching database) · Composite (text tree) ·
Strategy-like rule catalogs · Repository-style save store.

## Project structure

```
ClassSystems.Cli      command shell, modes (text / chars), parser, terminal abstraction
ClassSystems.Core     text system (models, tree, formats, templates) and game (models, rules, mappers)
ClassSystems.Infra    displayers, JSON storage, save store, data tables
ClassSystems.Remote   HTTP client of the remote game database + caching decorator
ClassSystems.Tests    146 xUnit tests
scenarios/            example command scripts
```

## Run

```bash
dotnet test                                              # 146 tests
dotnet run --project ClassSystems.Cli -- --text          # structured-text editor
dotnet run --project ClassSystems.Cli -- --chars         # game (needs Internet for db / recruit / arm)
dotnet run --project ClassSystems.Cli -- --chars < scenarios/05-remote-database.txt
```

Example commands: `template add chapter`, `print --whole --format html`, `export notes.html --whole`,
`db chars --find pyro`, `recruit diluc`, `arm diluc wolf-s-gravestone`, `sheet diluc`, `save slot1`, `load slot1`.

---

Author: **Tymofii Rashchenko** — student of Cybersecurity and Information Protection (F5).
