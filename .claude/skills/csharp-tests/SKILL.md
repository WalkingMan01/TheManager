---
name: csharp-tests
description: Writes xUnit unit tests for TheManager services, following project conventions for attributes, naming, structure, and coverage.
allowed-tools: Read Grep Glob Bash Edit Write
---

# C# Unit Tests — TheManager

You are writing xUnit unit tests for the TheManager project (.NET 10). The test project is `TheManager.Tests`. Tests cover services in `TheManager.Services`; models are POCOs and do not need their own tests.

## Before Writing Any Tests

1. Read the service file being tested in full — understand every public method, its inputs, outputs, and edge cases.
2. Read the existing test file for that service (e.g. `TheManager.Tests/LeagueServiceTests.cs`) to avoid duplicating tests that already exist.
3. If no test file exists yet, create one — see **File structure** below.

## Test Project Facts

- Framework: **xUnit 2.9.3**
- `using Xunit;` is a **global using** — do not add it explicitly
- Always add `using TheManager.Models;` and `using TheManager.Services;`
- Add any other `using` statements needed for types used in the tests
- Nullable reference types are **enabled** — use `?` correctly in test helpers

## File Structure

```csharp
using TheManager.Models;
using TheManager.Services;

namespace TheManager.Tests;

public class LeagueServiceTests
{
    // ── MethodName ────────────────────────────────────────────────────────────

    [Fact]
    public void MethodName_Scenario_ExpectedOutcome()
    {
        ...
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SomeType MakeSomething(...) { ... }
}
```

- File: `TheManager.Tests/{ServiceName}Tests.cs`
- Class: `public class {ServiceName}Tests` — one class per service file
- No test fixtures or `IClassFixture` — services are static; no shared state to set up
- Helper methods go at the bottom under a `// ── Helpers ──` divider

## Naming Convention

```
MethodName_Scenario_ExpectedOutcome
```

**Name the observable behaviour, not the implementation detail.**

The scenario and outcome segments should describe *what the system does from the caller's perspective*, not *how it does it internally*.

| Avoid (implementation) | Prefer (behaviour) |
|---|---|
| `GenerateSeasonFixtures_UsesCircleMethod` | `GenerateSeasonFixtures_EachOpponentFacedOnceHomeOnceAway` |
| `DetermineNewDivision_CallsIncrementDivision` | `DetermineNewDivision_Bottom3_Relegates` |
| `RecordResult_UpdatesEntryFields` | `RecordResult_HomeWin_OnlyHomeTeamGainsPoints` |
| `AdvanceWeek_IncrementsInternalCounter` | `AdvanceWeek_MatchesRemainingDecrements` |

A good test name completes the sentence: *"Given [scenario], the system [expected outcome]."* If the name requires knowing how the method is implemented to make sense, rewrite it.

Other naming rules:
- Avoid vague terms like `Works`, `IsCorrect`, `Test1`, `Handles`
- Boundary tests name the boundary value, not the branch: `AwardLeaguePrizeMoney_ExactlyFourthPlace_NoMoney` not `AwardLeaguePrizeMoney_FailsBranchCheck`
- Negative tests state what does *not* happen: `NoPromotionFromDivision1`, `ClubNeverListedAsOpponent`

## xUnit Attributes

### `[Fact]` — single test case
Use for any test that has fixed inputs.

```csharp
[Fact]
public void AwardLeaguePrizeMoney_FourthPlace_NoMoney()
{
    var finances = new Finances { BankBalance = 50_000 };
    SeasonService.AwardLeaguePrizeMoney(finances, finalLeaguePosition: 4, Division.One);
    Assert.Equal(50_000, finances.BankBalance);
}
```

### `[Theory]` + `[InlineData]` — parameterised test
Use when the same assertion holds for multiple input values. Each `[InlineData]` row becomes a separate test run.

```csharp
[Theory]
[InlineData(Division.One,   1)]
[InlineData(Division.Two,  21)]
[InlineData(Division.Three,41)]
[InlineData(Division.Four, 61)]
public void GetDivisionStartIndex_ReturnsCorrectIndex(Division division, int expected)
{
    Assert.Equal(expected, FixtureSchedulerService.GetDivisionStartIndex(division));
}
```

Only use `[InlineData]` — do not use `[MemberData]` or `[ClassData]`.

### When to choose `[Fact]` vs `[Theory]`
- Same assertion, different primitive inputs → `[Theory]`
- Different assertion logic per case → separate `[Fact]` methods
- More than ~4 cases → consider whether a loop inside a `[Fact]` is clearer

## Assert Methods

Use the most specific assertion available:

| Situation | Use |
|-----------|-----|
| Exact value match | `Assert.Equal(expected, actual)` |
| Boolean | `Assert.True(condition)` / `Assert.False(condition)` |
| Null / not null | `Assert.Null(x)` / `Assert.NotNull(x)` |
| Collection: all pass predicate | `Assert.All(collection, item => Assert.Equal(...))` |
| Collection: contains item | `Assert.Contains(expected, collection)` |
| Collection: does not contain | `Assert.DoesNotContain(collection, predicate)` |
| Value in range | `Assert.InRange(value, low, high)` |
| Values differ | `Assert.NotEqual(unexpected, actual)` |
| Throws | `Assert.Throws<ExceptionType>(() => ...)` |

**Important**: `Assert.NotEqual` does **not** accept a message argument. To include a failure message, use:
```csharp
Assert.True(actual != unexpected, $"Expected values to differ but both were {actual}");
```

**Do not** use `Assert.Equal(true, someCondition)` — use `Assert.True(someCondition)` instead.

## What to Test

For each public method, cover:

1. **Happy path** — typical inputs produce the expected output
2. **Boundary values** — the exact values at each if/else threshold (e.g. position == 17 vs 18 for relegation)
3. **Edge cases** — minimum/maximum inputs, empty collections, zero values
4. **All enum values** — use `[Theory]` when behaviour varies by `Division`, `PlayerPosition`, etc.
5. **Invariants** — properties that must hold regardless of input (e.g. a fixture list always has exactly 38 entries)

Do **not** test:
- Private methods (test them through the public API)
- Framework behaviour (e.g. that `List<T>.Count` works)
- Logic inside model constructors or property setters

## Arrange / Act / Assert Pattern

Keep each test short and focused on one behaviour. Separate the three phases with a blank line when all three are non-trivial; omit the blank line for one-liner asserts.

```csharp
[Fact]
public void RecordResult_HomeWin_UpdatesBothTeams()
{
    var table = LeagueService.InitialiseTable(Division.One, MakeTeamNames());

    LeagueService.RecordResult(table, homeTeam: table.Entries[0].TeamName, homeScore: 2,
                                      awayTeam: table.Entries[1].TeamName, awayScore: 0);

    Assert.Equal(1, table.Entries[0].Won);
    Assert.Equal(1, table.Entries[1].Played);
    Assert.Equal(0, table.Entries[1].Won);
}
```

Do **not** assert multiple unrelated behaviours in a single test — split them.

## Randomised Services

Some services take a `Random` parameter. Always seed it for deterministic tests:

```csharp
var rng = new Random(42);
var result = SomeService.DoRandomThing(rng);
```

If you need to test that something happens "with nonzero probability", run multiple seeds and assert the result appears at least once — do **not** rely on a single seed.

## Helper Methods

Extract repeated setup into private static helpers. Place them in a `// ── Helpers ──` section at the bottom of the class. Name them `Make…` or `Build…`.

```csharp
private static string[] MakeTeamNames()
{
    var names = new string[107];
    for (int i = 1; i <= 80; i++) names[i] = $"Team{i:D2}";
    return names;
}

private static (string[] names, string clubName) MakeTeamNamesWithClub(Division division, int clubIndex)
{
    var names    = new string[107];
    for (int i = 1; i <= 107; i++) names[i < 107 ? i : 106] = $"Team{i:D2}";
    int divStart = (int)division * 20 - 19;
    return (names, names[divStart + clubIndex]);
}
```

## Running Tests

After writing tests, always run them to confirm they pass:

```
dotnet test TheManager.Tests
```

If a test fails, read the failure message and fix either the test (if the expectation was wrong) or the service code (if a real bug was found). Never skip or comment out a failing test.

## Style Rules

- No comments explaining *what* the test does — the name and assertions are self-documenting
- No XML doc comments on test methods
- `var` for local variables where the type is inferable
- Use named arguments (`finalLeaguePosition: 4`) when parameter names are not obvious from context
- Do not add a trailing summary comment at the end of the file
