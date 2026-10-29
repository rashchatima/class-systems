# Практичне заняття 9. Генеративні шаблони: деревовидна структура

У систему структурованого тексту впроваджено **Абстрактну фабрику** (формати Markdown / HTML / plain text),
**Одинака** (єдині екземпляри фабрик форматів) і **Прототип** (глибоке копіювання елементів, бібліотека шаблонів).
Що, де і чому — у файлі **[PATTERNS.md](PATTERNS.md)**.

Нові команди режиму `text`:

| Команда | Що робить |
|---|---|
| `print [--whole] --format <md\|html\|text>` | показати текст у вибраному форматі |
| `export <файл> [--whole] [--format …]` | записати у файл; формат за розширенням (`.html`, `.txt`, `.md`) |
| `copy <назва або #id>` | вставити глибоку копію елемента поруч з ним |
| `template list` / `template add <назва>` / `template save <назва> [<елемент>]` | шаблони-прототипи |

Тести: **146, усі пройдено** (`dotnet test`).

---

# Практичне заняття 7. Практика TDD

Фінальна ітерація проєкту першого модуля. До проєкту з практичних 5–6 додано:

1. **Модуль `ClassSystems.Remote`** (інфраструктура) — запити до віддаленого сервісу через стандартний `HttpClient`.
   Використано рекомендований API **https://genshin.jmp.blue** (github.com/genshindev/api): персонажі й зброя гри
   Genshin Impact у форматі JSON.
   - `IGameDatabase` — абстракція бази; `GenshinApiClient` — реалізація через HTTP;
     `CachedGameDatabase` — декоратор, що завантажує дані один раз за запуск.
   - Помилки мережі, 404 і зламаний JSON перетворюються на `RemoteDataException`, яку оболонка команд
     показує як звичайну помилку команди.
2. **Трансляція даних у класи гри** (`Core/Characters/Remote`):
   - структуру персонажа адаптовано: `CharacterProfile` (титул, стихія, тип зброї, регіон, рідкість, опис);
     у предмета з'явився тип зброї (`Kind`) і опис, у здібності — опис;
   - в API немає базових статів, тому `StatRoller` генерує їх випадково з діапазону рідкості
     (5★: 100–130 HP, 4★: 80–110 HP …); генератор ініціалізується id персонажа, тож той самий персонаж
     завжди отримує ті самі стати;
   - таланти стають здібностями (звичайна атака → power-strike, елементальний навик → fireball,
     вибух стихії → life-drain); зброя стає предметом: базова атака / 4 + бонус від побічного стату,
     пасивний ефект — здібність, яку дає предмет;
   - нове правило гри: персонаж з профілем може взяти лише зброю свого типу (Diluc — тільки Claymore).
3. **Представлення персонажа структурованим текстом** — `CharacterSheet` будує `Document` з розділами
   Profile / Stats / Equipment / Abilities (вкладені заголовки для предметів і здібностей) і виводить його через `TextDto`.
4. **Нові команди** режиму `chars`:

| Команда | Що робить |
|---|---|
| `db chars [--find <текст>]` | список персонажів бази (фільтр за id, ім'ям, стихією, зброєю, регіоном) |
| `db weapons [--find <текст>]` | список зброї бази |
| `recruit <id з бази> [--name <ім'я>]` | створити віртуального персонажа з бази й додати в гру |
| `arm <персонаж> <id зброї з бази>` | екіпірувати на персонажі зброю з бази |
| `sheet <персонаж>` | повний стан персонажа у вигляді структурованого тексту |

Приклад: `dotnet run --project ClassSystems.Cli -- --chars < scenarios/05-remote-database.txt`
(скриншоти — `Скриншот роботи 1.png`, `Скриншот роботи 2.png`).

## Як застосовано TDD

Для кожної фічі спочатку записувався список сценаріїв, потім тест на сценарій, потім найпростіший код,
що проходить тест, і рефакторинг. Сценарії стали тестами в `ClassSystems.Tests/Remote`:

- **GenshinApiClientTests** — розбір персонажа, зброї й списків, нормалізація id, 404, мережа недоступна,
  зламаний JSON, помилка сервера 500;
- **CachedGameDatabaseTests** — список завантажується один раз, окремий об'єкт береться зі списку, помилки не кешуються;
- **RemoteMapperTests** — профіль, ім'я, таланти → здібності, діапазони статів, стабільність статів, зброя → предмет;
- **RemoteGameTests** — правило типу зброї, `CharacterSheet` (структура документа й Markdown), збереження профілю
  у JSON, команди `db / recruit / arm / sheet` від початку до кінця через `CommandShell`.

Тести не ходять в Інтернет: `GenshinApiFixture` (фікстура xUnit, `IClassFixture`) один раз завантажує збережені
справжні відповіді API з `Remote/Samples/*.json`, а `StubHttpHandler` підставляє їх замість мережі й записує запити.
`ScriptedTerminal` «вводить» команди для перевірки інтерфейсу.

**Результат: 117 тестів, усі пройдено** (`Скриншот тестів.png`).

---

Далі — опис тестового модуля з практичної 6.

## Тести (практична 6)

```bash
dotnet test
```

```
ClassSystems.Tests
├── Doubles/                     тестові дублери замість справжніх залежностей
│   ├── Displayers.cs            FakeDisplayer (нічого не робить), MockDisplayer (запам'ятовує й рахує виклики)
│   ├── Storages.cs              StubStorage (репозиторій у пам'яті, може «ламатися» при записі),
│   │                            MockStorage (стаб + лічильники Load/Save)
│   └── Arena.cs                 готова гра з трьома персонажами для кроку Arrange
├── Core/
│   ├── CombatTests.cs           атака, мінімальна шкода, захист, лікування, перемога, хто не може діяти
│   ├── AbilityTests.cs          каталог здібностей і ефект кожної здібності
│   ├── EquipmentTests.cs        бонуси предметів, власник предмета, здібність від предмета
│   ├── GameTests.cs             класи персонажів, стан гри, звіт, заміна IDisplayer
│   ├── GameSaveMapperTests.cs   гра -> таблиці збереження -> гра, зламане збереження
│   └── TextTests.cs             TextFactory, TextTree, рендеринг Markdown і змісту, TextSaveMapper
├── Infra/
│   ├── SaveStoreTests.cs        додати / знайти / замінити / видалити збереження (стаб, мок, фіксований час)
│   └── StorageAndDisplayTests.cs  JsonFileStorage на тимчасових файлах, FileDisplayer, MemoryDisplayer
└── Cli/
    └── CommandParserTests.cs    розбір команд: аргументи, опції, лапки, від'ємні числа, помилки
```

Кожен тест побудовано за схемою **AAA** (Arrange / Act / Assert). Тести перевіряють класи як «чорний ящик»:
внутрішні правила `Combat` перевіряються через публічний фасад `Game`, а залежності (`IDisplayer`,
`IRepositoryStorage`, годинник `SaveStore`) підміняються дублерами. Однакові випадки з різними даними
записано як `[Theory]` з `[InlineData]`.

Код програми для тестів змінювати не довелося: залежності вже передавалися через інтерфейси
й конструктори (практична 5), тому їх легко підмінити.

---

Далі — опис проєкту з практичної 5.

## Структура

```
ClassSystems.sln
├── ClassSystems.Cli        CLI
│   ├── Commands/           інтерпретація терміналних команд (парсер, реєстр, цикл оболонки)
│   ├── Presenter/          ввід і вивід у консоль: ITerminal, ConsoleTerminal, Dialog, картки-DTO з id
│   ├── Modes/              команди режимів text і chars, спільні команди save/load (SaveCommands)
│   ├── Support/            id для користувача (IdRegistry, WorldIds), готові дані --sample
│   ├── Demos/              демонстрації попередніх практичних (--demo)
│   └── Program.cs          єдине місце, де обираються конкретні реалізації
├── ClassSystems.Core       Core
│   ├── Text/               система структурованого тексту
│   │   ├── Models/         чисті моделі: record Document, Heading, Paragraph, Link, Image
│   │   ├── Display/        DTO: TextDto (Markdown), TableOfContentsDto
│   │   ├── TextTree.cs     операції над деревом (додати, перемістити, видалити, знайти шлях)
│   │   ├── TextFactory.cs  фасад: побудова документа й вивід через IDisplayer
│   │   └── TextSaveMapper.cs  документ <-> TextSave
│   └── Characters/         система гри
│       ├── Models/         record Stats, Ability, Item, CharacterClass; стан Character
│       ├── Rules/          правила: Combat (шкода, лікування), AbilityCatalog (ефекти), CharacterClasses
│       ├── Display/        DTO: CharacterDto, StatusReportDto
│       ├── Game.cs         фасад: світ гри, дії, вивід через IDisplayer
│       └── GameSaveMapper.cs  світ гри <-> CharacterSave
├── ClassSystems.Tests      Tests (xUnit) — див. розділи про тести вище
├── ClassSystems.Remote     Infra: клієнт віддаленої бази персонажів і зброї (HttpClient)
└── ClassSystems.Infra      Infra
    ├── Display/            IDisplayable, IDisplayer, ConsoleDisplayer, FileDisplayer, MemoryDisplayer
    └── Data/
        ├── Models/         моделі даних як таблиці БД: CharacterData, ItemData, AbilityData,
        │                   CharacterItems, CharacterAbilities (many-to-many), TextNodeData
        ├── Saves.cs        SaveBase, CharacterSave, TextSave, Repository
        ├── IRepositoryStorage.cs, JsonFileStorage.cs   де зберігається репозиторій
        └── SaveStore.cs    додати / знайти / видалити збереження
```

Залежності йдуть в один бік: `Cli -> Core -> Infra`. Infra не знає нічого про текст чи гру.

## Що змінилося в практичній 5

**Моделі.** Класи тексту стали чистими `record` без логіки. Рендеринг перенесено в `TextDto`,
операції над деревом — у `TextTree`. Підкласи `Warrior/Mage/Priest` відрізнялися лише числами, тому стали
даними (`CharacterClass` у `CharacterClasses`). Здібність — `record Ability(Name, Effect)`, а що вона робить,
визначає `AbilityCatalog`. `Character` лише зберігає стан; змінює його тільки `Game` (сетери `internal`).

**Вивід.** `Game` і `TextFactory` отримують `IDisplayer` у конструкторі (і можуть замінити його через
`SetDisplayer`) замість прямих викликів консолі. Об'єкти показуються через DTO, що реалізують `IDisplayable`.
`ITerminal` CLI розширює `IDisplayer`, тому той самий термінал передається системам як вивід.
Команда `export` показує заміну реалізації: той самий `TextDto` іде у `FileDisplayer` замість консолі.

**Збереження.** Стан системи перетворюється мапером у модель збереження (`TextSave`, `CharacterSave`),
та додається до `Repository`, який серіалізується в JSON (`System.Text.Json`). У збереженні гри об'єкти
пов'язані через id, як у реляційній БД, а при завантаженні знову стають посиланнями.

Поведінку не змінено: вивід `--demo` збігається з практичною 4 рядок у рядок.

## Запуск

```bash
dotnet run --project ClassSystems.Cli -- --text
dotnet run --project ClassSystems.Cli -- --chars --sample
dotnet run --project ClassSystems.Cli -- --chars --load battle
dotnet run --project ClassSystems.Cli -- --demo
```

Опції: `--text`, `--chars`, `--mode <text|chars>`, `--sample`, `--data <file>` (файл збережень,
за замовчуванням `classsystems-saves.json`), `--load <name>`, `--demo`.

## Нові команди

| Режим | Команда | Дія |
|---|---|---|
| обидва | `save <name>` | зберегти поточний стан (однакова назва — перезапис) |
| обидва | `load <name>` | замінити поточний стан збереженим |
| обидва | `saves` | список збережень режиму |
| обидва | `unsave <name>` | видалити збереження |
| text | `new [<title>]` | новий порожній документ |
| text | `export <file> [--whole]` | записати поточний розділ або весь документ у Markdown-файл |
| text | `print --toc` | зміст документа |
| chars | `status` | стан усіх персонажів |
| chars | `act defend <actor>` | захисна стійка |

Решта команд — як у практичній 4 (`help` показує всі).

## Сценарії використання

У папці `scenarios/` — файли з введенням користувача; `run-scenarios.ps1` запускає їх по черзі
й записує вивід програми в `scenarios/output/`:

1. `01-text-create-and-save` — створення документа, друк, зміст, `save notes`, `export notes.md`.
2. `02-text-load-and-edit` — `load notes`, редагування, перезапис збереження, помилка для неіснуючого.
3. `03-chars-fight-and-save` — створення персонажів, здібності й предмета, бій, `save battle`.
4. `04-chars-load-and-continue` — новий запуск програми, `load battle`, стан відновлено, бій триває.

```bash
powershell -ExecutionPolicy Bypass -File scenarios/run-scenarios.ps1
```
