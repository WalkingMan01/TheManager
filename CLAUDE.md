# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# TheManager

A C# (.NET 10) port of Football Director II — a football management game originally written in AmigaBASIC by D&H Games (1988). The original source is at `Original Code/FOOT.BAS`.

## Solution Structure

```
TheManager.slnx
├── TheManager.Models/       — domain models (Player, Club, GameState, etc.)
├── TheManager.Services/     — all game logic
├── TheManager.Console/      — Spectre.Console terminal UI (net10.0); the active UI target
├── TheManager.MatchHarness/ — standalone WinForms tool for testing match simulation
└── TheManager.Tests/        — xUnit test suite
```

Dependency direction: `TheManager.Console` → `TheManager.Services` → `TheManager.Models`

No external NuGet packages except `System.Text.Json` (in-box with .NET 10) and `Spectre.Console` (used by `TheManager.Console`).

## Build & Run

```
dotnet build TheManager.slnx              # build everything
dotnet run --project TheManager.Console   # launch the console UI
dotnet test TheManager.Tests              # run the xUnit test suite
```

`TheManager.MatchHarness` targets `net10.0-windows` and requires `UseWindowsForms=true` — it won't build on non-Windows CI.

## Architecture

**Models layer** (`TheManager.Models`) — pure POCOs, no logic. All enums live in a single `Enums.cs`. `GameState` is the root aggregate that holds everything needed to save/restore a game.

**Services layer** (`TheManager.Services`) — stateless business logic. Services never reference UI. Key services:
- `GameService` — top-level game loop: weekly ticks, match scheduling, season progression
- `MatchEngine` — pre-computes goal events before kick-off; handles injuries, red cards, subs
- `LeagueService` / `FixtureSchedulerService` — standings and fixture calendar
- `TransferService` / `FinanceService` / `PlayerService` — transfer, money, skill recalc
- `InitializationService` — new-game setup
- `SeasonService` / `CupService` — end-of-season and cup competition logic
- `SaveLoadService` — `System.Text.Json` serialisation of `GameState`

**Presentation layer** (`TheManager.Console`) — Spectre.Console terminal UI on .NET 10. `Program.cs` drives a screen loop; each screen lives in `TheManager.Console/Screens/` (e.g. `TitleScreen`, `WeekHubScreen`, `SquadScreen`, `PlayMatchScreen`, `FixturesScreen`, `LeagueTableScreen`).

## Key Conventions

### BASIC-to-C# mapping
Every public model property maps to a variable in FOOT.BAS. Always document this in the XML `<summary>`:
```csharp
/// <summary>Club name. Corresponds to Z$ in FOOT.BAS.</summary>
public string Name { get; set; } = "";
```

Service methods reference the original BASIC line numbers:
```csharp
/// <summary>Recalculates player status. BASIC lines 523–527, subroutine 3957.</summary>
```

### Squad array indexing
Players are stored as `Player?[29]`, mirroring BASIC's 1-based array:
- `[0]` — unused
- `[1–11]` — first team (1=GK, 2–5=DEF, 6–8=MID, 9–11=ATK)
- `[12]` — substitute
- `[13–20]` — reserves
- `[21–23]` — transfer targets (buying)
- `[24–26]` — transfer targets (selling)
- `[27–28]` — temporary negotiation slots

### Code style
- Nullable reference types are enabled — use `?` annotations correctly
- Implicit usings are enabled
- Section dividers inside model classes:
  ```csharp
  // ── Identity ──────────────────────────────────────────────────────────────
  ```
- XML doc comments on all public members — always include a `<summary>`
- Enums all live in `TheManager.Models/Enums.cs`, not separate files

### Service patterns
- Prefer static methods for pure logic (`PlayerService.RecalculateStatus(player)`)
- Inject `Random` for anything randomised (`MatchEngine(Random? rng = null)`) — never use `Random.Shared` or `new Random()`
- Use input objects for multi-parameter operations (`MatchSetupInput`)

## Original Source Reference
When implementing or verifying logic, cross-reference `Original Code/FOOT.BAS`. The file is ~2,000 lines of AmigaBASIC. Line numbers in comments refer to that file.

## Tests
`TheManager.Tests` is the xUnit suite — run it with `dotnet test TheManager.Tests`. Prefer xUnit and integration-style tests over heavy mocking — mock only at system boundaries (e.g., file I/O).
