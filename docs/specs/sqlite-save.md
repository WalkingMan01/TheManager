# Specification: JSON File Save / Load

## Overview

Replace the single-file JSON save (`FDII.json`) with a folder of per-slot JSON
files that supports multiple named save slots. On first launch the title screen
offers "Continue" when any save exists. A "Save Game" option on the weekly hub
screen writes the current state into the chosen slot.

No new NuGet dependencies are needed — `System.Text.Json` is already in use
for serialisation and the existing `SaveLoadService` serialiser options are
reused directly.

---

## Save folder location

```
<executable folder>\saves\
```

`AppContext.BaseDirectory` — the same directory the game binary sits in.
This matches the original BASIC behaviour (`FDII.SAV` was written to the
current directory) and the existing `SaveLoadService.DefaultSavePath`.
The folder travels with the game when the directory is copied or zipped.

The `saves\` subfolder is created on first save if it does not exist.
Expose the resolved path via `JsonFileSaveService.SavesFolder` so the UI can
display it in help text if needed.

---

## File format

Each save slot is a single JSON file in the `saves\` folder:

```
saves\
  quick-save.json
  my-arsenal-save.json
  auto-save.json
```

Each file is a JSON object with two top-level sections:

```json
{
  "header": {
    "slotName":  "My Arsenal save",
    "clubName":  "Arsenal",
    "division":  1,
    "manager":   "Steve",
    "season":    3,
    "week":      22,
    "savedAt":   "2026-06-06T14:32:00.0000000Z"
  },
  "gameState": { ... full GameState ... }
}
```

`savedAt` is always written with `DateTime.UtcNow.ToString("o")` (the
round-trip format). Using the same format string for every file ensures
sorting by `savedAt` is correct without any date parsing.

### File naming

Each file is named with a random GUID:

```
saves\
  3f2a1b4c-8e71-4d02-a9f3-c1d2e5f60718.json
  a7c19d35-22ab-4f10-b841-09e3d7f52c94.json
```

The GUID is generated once when a new slot is first saved and reused for
subsequent overwrites. The canonical slot name is stored exclusively in
`header.slotName` — never inferred from the file name. This avoids any
collision between slot names that differ only in case or punctuation.

`Save` scans existing files to find one whose `header.slotName` matches
the requested slot name (case-insensitive). If found it overwrites that
file; if not found it creates a new `{Guid.NewGuid()}.json` file. User
input is never used as a file name.

---

## New service: `ISaveService` + `JsonFileSaveService`

Unlike the other services in this codebase (`LeagueService`,
`FinancialCrisisService`, etc.), which are static classes because they are
pure functions, the save service has implicit state (the folder path) and I/O
side effects. A static class cannot be substituted in tests — every test would
hit real files on disk.

The solution is an interface and a non-static implementation.

### `ISaveService` interface

**File:** `TheManager.Services/ISaveService.cs`

```csharp
public interface ISaveService
{
    /// <summary>
    /// Writes (or overwrites) a save slot. Creates the slot if it does not
    /// exist; replaces it silently if it does.
    /// </summary>
    void Save(string slotName, GameState state);

    /// <summary>
    /// Returns all save-slot summaries ordered by saved_at descending.
    /// Reads only the header section of each file — does not deserialise
    /// the full game state.
    /// </summary>
    IReadOnlyList<SaveSlotSummary> ListSlots();

    /// <summary>
    /// Loads and deserialises a save slot by name.
    /// Throws <see cref="KeyNotFoundException"/> if the slot does not exist.
    /// </summary>
    GameState Load(string slotName);

    /// <summary>Deletes a save slot. No-op if the slot does not exist.</summary>
    void Delete(string slotName);

    /// <summary>True when at least one save slot exists.</summary>
    bool AnySaveExists();
}
```

### `JsonFileSaveService` implementation

**File:** `TheManager.Services/JsonFileSaveService.cs`

```csharp
public class JsonFileSaveService : ISaveService
{
    public string SavesFolder { get; }

    /// <summary>
    /// Creates the service. The saves folder is created on first save if it
    /// does not already exist. Pass <see cref="DefaultSavesFolder"/> for
    /// production use.
    /// </summary>
    public JsonFileSaveService(string savesFolder)
    {
        SavesFolder = savesFolder;
    }

    public static string DefaultSavesFolder =>
        Path.Combine(AppContext.BaseDirectory, "saves");

    public void Save(string slotName, GameState state)   { ... }
    public IReadOnlyList<SaveSlotSummary> ListSlots()    { ... }
    public GameState Load(string slotName)               { ... }
    public void Delete(string slotName)                  { ... }
    public bool AnySaveExists()                          { ... }

    // Finds the path of the file whose header.slotName matches (case-insensitive),
    // or returns null if no match exists.
    private string? FindExistingSlotPath(string slotName) { ... }

    private record SaveFile(SaveSlotSummary Header, JsonElement GameState);
}
```

### `SaveSlotSummary` model

**File:** `TheManager.Models/SaveSlotSummary.cs`

```csharp
public record SaveSlotSummary(
    string   SlotName,
    string   ClubName,
    int      Division,
    string   Manager,
    int      Season,
    int      Week,
    DateTime SavedAt);
```

### Testability

**Unit tests** — inject a hand-written stub to verify that screens call the
right methods without touching disk:

```csharp
var fake = new FakeSaveService();
SaveGameScreen.Show(fake, state);
Assert.Equal("Quick save", fake.LastSlotName);
```

**Integration tests** — construct a real `JsonFileSaveService` against a temp
folder to verify the full round-trip:

```csharp
var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
var svc    = new JsonFileSaveService(folder);
svc.Save("test", state);
var loaded = svc.Load("test");
Assert.Equal(state.Club.Name, loaded.Club.Name);
Directory.Delete(folder, recursive: true);
```

### Implementation notes

- `Save` calls `Directory.CreateDirectory(SavesFolder)` (no-op if it exists),
  then calls `FindExistingSlotPath(slotName)` to locate a file to overwrite.
  If none is found it generates `Path.Combine(SavesFolder, $"{Guid.NewGuid()}.json")`.
  It serialises a `SaveFile` wrapper — `{ header, gameState }` — using
  `SaveLoadService.SerializerOptions` and writes it atomically via
  `File.WriteAllText`.
- `ListSlots` returns an empty list when `SavesFolder` does not exist. Otherwise
  it calls `Directory.GetFiles(SavesFolder, "*.json")`, and for each file uses
  `JsonDocument.Parse(File.ReadAllText(path))` to extract only
  `root.GetProperty("header")`, binding it to `SaveSlotSummary` via
  `JsonSerializer.Deserialize<SaveSlotSummary>(...)`. The `gameState` property
  is never touched. Results are returned sorted by `SavedAt` descending.
- `Load` calls `FindExistingSlotPath(slotName)` and throws
  `KeyNotFoundException` if null. It then reads the file, deserialises the
  full `SaveFile` wrapper, and passes `wrapper.GameState.GetRawText()` to
  the existing `Deserialize` helper on `SaveLoadService`. Using `JsonElement`
  for the `GameState` field in `SaveFile` avoids deserialising the game state
  twice during a `ListSlots` call.
- `FindExistingSlotPath` scans all `*.json` files in `SavesFolder`, reads
  each `header.slotName` via `JsonDocument`, and returns the path of the
  first case-insensitive match. Returns `null` if not found or if the folder
  does not exist.
- `AnySaveExists` uses `Directory.Exists(SavesFolder) &&
  Directory.EnumerateFiles(SavesFolder, "*.json").Any()` — avoids reading
  file contents.
- `SaveLoadService.SerializerOptions` and `Deserialize` must both be changed
  from `private` to `internal` so `JsonFileSaveService` can access them.

---

## Console app changes

### 1. `TitleScreen` — startup flow

**File:** `TheManager.Console/Screens/TitleScreen.cs`

`TitleScreen.Show()` currently returns `void`. Change it to accept
`ISaveService` and return a `TitleChoice` enum:

```csharp
internal enum TitleChoice { NewGame, Continue, Quit }

internal static class TitleScreen
{
    public static TitleChoice Show(ISaveService saveService) { ... }
}
```

The screen calls `saveService.AnySaveExists()` to decide which options to
display:

| Condition | Options shown |
|-----------|--------------|
| No saves exist | New Game · Quit |
| At least one save exists | Continue · New Game · Quit |

Return the selected choice to `Program.cs`; do not call `Environment.Exit`
inside the screen — let `Program.cs` handle the Quit branch.

### 2. New `LoadGameScreen`

**File:** `TheManager.Console/Screens/LoadGameScreen.cs`

Called when the player chooses "Continue" at the title screen. Receives
`ISaveService` as a parameter so it can be tested without touching disk.

```csharp
internal static class LoadGameScreen
{
    public static GameState? Show(ISaveService saveService) { ... }
}
```

- Calls `saveService.ListSlots()` and displays each slot as a row:
  ```
  ❯  My Arsenal save   Arsenal  Div 1  Season 3  Week 22  (saved 2026-06-06 14:32)
     Quick save        Bury     Div 4  Season 1  Week 7   (saved 2026-06-04 09:15)
  ```
- Adds a "← Back" option at the bottom so the player can return to the title.
- Returns `GameState?` — `null` if the player goes back, otherwise calls
  `saveService.Load(slotName)` and returns the result.

### 3. `WeekHubScreen` — "Save Game" option

**File:** `TheManager.Console/Screens/WeekHubScreen.cs`

Add `SaveGame` to the `WeekAction` enum and insert "Save Game" into the
choices list between "Difficulty" and "Sack Myself":

```csharp
internal enum WeekAction { ..., Difficulty, SaveGame, SackMyself, Quit }
```

```
Difficulty
Save Game       ← new
Sack Myself
Quit
```

### 4. New `SaveGameScreen`

**File:** `TheManager.Console/Screens/SaveGameScreen.cs`

Called when the player picks "Save Game" from the hub. Receives `ISaveService`
as a parameter so it can be tested without touching disk.

```csharp
internal static class SaveGameScreen
{
    public static void Show(ISaveService saveService, GameState state) { ... }
}
```

- Calls `saveService.ListSlots()` to show existing slot names so the player
  can recognise them.
- Prompts for a slot name with `AnsiConsole.Prompt<string>`. A default of
  `"Quick save"` pre-fills the field.
- If the typed name matches an existing slot, ask:
  `"Overwrite '[name]'? (Y/N)"` — default N.
- On confirm, calls `saveService.Save(slotName, state)`.
- Shows a brief `"  [green]Saved.[/]"` confirmation, then `Ui.Pause()`.

### 5. `Program.cs` — wiring

Construct the save service once at startup and pass it to every screen that
needs it:

```csharp
ISaveService saveService = new JsonFileSaveService(JsonFileSaveService.DefaultSavesFolder);

GameService? gameService = null;

while (gameService == null)
{
    var choice = TitleScreen.Show(saveService);

    switch (choice)
    {
        case TitleChoice.Quit:
            return;

        case TitleChoice.Continue:
            var loaded = LoadGameScreen.Show(saveService);
            if (loaded != null)
                gameService = GameService.FromSave(loaded);
            break;

        case TitleChoice.NewGame:
            var (teamName, division, managerName) = TeamSelectionScreen.Show();
            gameService = new GameService
            {
                Team     = teamName,
                Division = division,
                Manager  = managerName
            };
            gameService.StartGame();
            break;
    }
}
```

Add the `SaveGame` case to the main `switch`:

```csharp
case WeekAction.SaveGame:
    SaveGameScreen.Show(saveService, gameService.State);
    break;
```

---

## `GameService` — factory method for loaded games

Rather than adding a second constructor (which creates a footgun — a developer
who calls `StartGame()` after it silently resets the loaded state), add a
static factory method that makes the two creation paths structurally distinct:

```csharp
public static GameService FromSave(GameState loadedState)
{
    var svc    = new GameService();   // private/existing default constructor
    svc._random    = new Random();
    svc._gameState = loadedState;
    svc._engine    = new MatchEngine(svc._random);
    return svc;
}
```

`_random` is initialised before `MatchEngine` — the same order as `StartGame`.
`StartGame()` is never called on this path; the state is already fully
initialised. The public API is unambiguous: `new GameService()` + `StartGame()`
for a new game, `GameService.FromSave(loaded)` for a loaded one.

---

## Migration from the existing JSON save file

The existing `FDII.json` file is not migrated automatically. On first run
after the update it will simply be ignored — `TitleScreen` reads from the
`saves\` folder only. Users who want to carry over an existing save can do so
via a one-time helper (out of scope for this spec) or by starting a new game.

---

## What is out of scope

- Auto-save on match completion (a future enhancement; requires no spec
  changes, just a call to `saveService.Save("Auto save", state)` inside
  `GameService.PlayMatch`).
- Save-file encryption or anti-cheat.
- Cloud sync.
- The BASIC's `OY` save-slot counter mechanic (the slot limit is not
  replicated; the player may create as many named slots as they like).

---

## Affected files

| File | Change |
|------|--------|
| `TheManager.Services/ISaveService.cs` | New — save service interface |
| `TheManager.Services/JsonFileSaveService.cs` | New — JSON file save/load implementation |
| `TheManager.Services/SaveLoadService.cs` | Make `Deserialize` and `SerializerOptions` `internal` |
| `TheManager.Models/SaveSlotSummary.cs` | New — metadata record |
| `TheManager.Console/Program.cs` | Replace linear startup with choice loop; add `SaveGame` case |
| `TheManager.Console/Screens/TitleScreen.cs` | Add Continue option; return `TitleChoice` |
| `TheManager.Console/Screens/LoadGameScreen.cs` | New — slot picker |
| `TheManager.Console/Screens/SaveGameScreen.cs` | New — slot naming and overwrite confirm |
| `TheManager.Console/Screens/WeekHubScreen.cs` | Add `SaveGame` to enum and menu |
| `TheManager.Services/GameService.cs` | Add `GameService.FromSave(GameState)` factory method |
