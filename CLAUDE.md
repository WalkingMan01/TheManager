# FootballBoss

A C# (.NET 10) port of Football Director II — a football management game originally written in AmigaBASIC by D&H Games (1988). The original source is at `Original Code/FOOT.BAS`.

## Solution Structure

```
FootballBoss.slnx
├── FootballBos.Models/       — domain models (Player, Club, GameState, etc.)
├── FootballBoss.Services/    — all game logic
└── FootballBoss/             — entry point / UI layer (placeholder, not yet built)
```

Dependency direction: `FootballBoss` → `FootballBoss.Services` → `FootballBos.Models`

No external NuGet packages except `System.Text.Json` (in-box with .NET 10).

## Build & Run

```
dotnet build FootballBoss.slnx
```

The main `FootballBoss` project has no `Program.cs` yet — it is a placeholder pending UI implementation.

## Architecture

**Models layer** (`FootballBos.Models`) — pure POCOs, no logic. All enums live in a single `Enums.cs`.

**Services layer** (`FootballBoss.Services`) — stateless business logic implemented as static methods or classes with injected `Random`. Services never reference UI.

**Presentation layer** (`FootballBoss`) — not yet implemented. Target UI is TBD (console, WinForms, or web).

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
- Enums all live in `FootballBos.Models/Enums.cs`, not separate files

### Service patterns
- Prefer static methods for pure logic (`PlayerService.RecalculateStatus(player)`)
- Inject `Random` for anything randomised (`MatchEngine(Random? rng = null)`)
- Use input objects for multi-parameter operations (`MatchSetupInput`)

## Original Source Reference
When implementing or verifying logic, cross-reference `Original Code/FOOT.BAS`. The file is ~2,000 lines of AmigaBASIC. Line numbers in comments refer to that file.

## No Tests Yet
There are no test projects. When adding tests, prefer xUnit and integration-style tests over heavy mocking — mock only at system boundaries (e.g., file I/O).
