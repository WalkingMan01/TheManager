# Specification: Player Contract Renegotiation

## Overview

Players whose contracts have 10 or fewer weeks remaining can be offered a new contract
directly from the squad screen. The player states their demands up front — weekly wage,
signing-on fee, and contract length — giving the manager full visibility before
committing. The manager can accept the stated terms outright or make a counter-offer.
The player's actual minimum acceptance thresholds are the stated figures minus a random
factor of up to 10%, so a well-judged counter-offer below the stated demands may still
succeed.

---

## BASIC reference

In `FOOT.BAS` the equivalent flow is driven from the re-negotiate option (lines
359–366, `HF=3`). The key difference from the original is direction: BASIC asks the
manager to enter terms blindly and then silently checks them against hidden thresholds
(lines 2610–2615). This spec flips the flow so the player's demands are shown first.

The **demand formulas** (BASIC lines 2610–2615, renewal path `HF=3` where `HG=AP` so
no cross-division penalty applies):

```
Wage    HU = (1 + INT(RND*20) + 50) × INT(skill)
             + ABS(skill > 9.6) × 1000
             ÷ MAX(1, age − 27)
             floored at 50

Fee     HV = (1000 × INT(skill)) ÷ division

Length  IA = MIN(MAX(1, age − 23) [+ 1 with 50% chance] × 53,
              MAX(53, (35 − age) × 53))   weeks   ← capped at weeks until age 35
```

`IA` is the **maximum** contract length the player will sign — the check
`IF IA <= HR THEN REJECT` (line 2615) means offering *more* weeks than IA causes a
rejection. Young players (low `age − 23`) accept only short deals; older players
tolerate longer ones.

In the original BASIC these values were hidden minimums that the manager had to guess.
Here they become the player's opening stated demands, and the actual minimums are
derived from them by applying a random reduction of 0–10%.

---

## Eligibility

A renegotiation attempt is valid when:

- The squad slot 1–20 contains a player (`squad[n] is not null`).
- `player.ContractWeeks <= 10` (includes 0 = already expired).
- The player is not retiring (`player.IsRetiring == false`).

Slots 21–28 (transfer targets, negotiation temporaries) are never eligible.

---

## Step 1 — Highlight expiring contracts on the squad screen

### `TheManager.Console/Screens/SquadScreen.cs` — `AddSection`

The contract cell currently shows `[red]exp[/]` only when `ContractWeeks == 0`.
Extend the red highlight to cover the full ≤ 10-week warning window:

```csharp
string contract = player is null              ? "[dim]—[/]"
                : player.ContractWeeks == 0   ? "[red bold]exp[/]"
                : player.ContractWeeks <= 10  ? $"[red]{player.ContractWeeks}w[/]"
                : $"{player.ContractWeeks}w";
```

No other display changes are needed in this step — the squad table already shows the
`Ctr` column on every row.

---

## Step 2 — `R<number>` input command

### `TheManager.Console/Screens/SquadScreen.cs` — input parsing

Add a third parse branch alongside the existing `TryParseSwap` and
`TryParseTransferListToggle`:

```
R<number>   e.g. "R7" or "R 7"
```

- Case-insensitive.
- `<number>` must be 1–20.

```csharp
private static bool TryParseRenegotiate(string input, out int slot)
{
    slot = 0;
    var trimmed = input.Trim();
    if (trimmed.Length < 2 || (trimmed[0] != 'R' && trimmed[0] != 'r'))
        return false;
    return int.TryParse(trimmed[1..].Trim(), out slot);
}
```

Validation errors use the existing `error` mechanism (shown in red above the prompt):

| Condition | Error message |
|---|---|
| Slot outside 1–20 | `"Enter a player number between 1 and 20, e.g. \"R7\""` |
| Slot empty | `"There is no player in slot {n}"` |
| Contract > 10 weeks remaining | `"{Name}'s contract does not expire for {n} weeks"` |
| Player is retiring | `"{Name} is retiring and cannot be offered a contract"` |

Update the input hint:

```
Enter two slot numbers to swap (e.g. 3 9), T<number> to transfer-list (e.g. T9),
R<number> to renegotiate a contract (e.g. R7), or press Enter to go back:
```

---

## Step 3 — Demand calculation

### `TheManager.Services/ContractService.cs`

Add a new method `GetPlayerDemands` and a result record `PlayerContractDemand`.

The method computes the player's **stated demands** (what they ask for) and their
**hidden minimums** (what they will actually accept). Stated demands are derived from
the BASIC formulas. Hidden minimums are the stated values reduced by a random 0–10%
per term, computed once at demand-generation time and stored on the record.

```csharp
/// <summary>
/// Computes the player's opening contract demands and the hidden minimums they
/// will accept. Stated demands use the formulas from BASIC lines 2610–2615
/// (renewal path HF=3, so no cross-division penalty). Hidden minimums are the
/// stated values reduced by a random 0–10%.
/// </summary>
public static PlayerContractDemand GetPlayerDemands(
    Player   player,
    Division division,
    Random   rng)
{
    // Wage (BASIC line 2610)
    double wageBase = (1 + rng.Next(20) + 50) * (int)player.Skill
                      + (player.Skill > 9.6 ? 1_000 : 0);
    int ageDivisor  = Math.Max(1, player.DisplayAge - 27);
    int statedWage  = (int)Math.Max(50, wageBase / ageDivisor);

    // Signing-on fee (BASIC line 2612, renewal: HG=AP so no division-gap term)
    int statedFee = (1_000 * (int)player.Skill) / (int)division;

    // Contract length (BASIC lines 2614–2615)
    int statedWeeks = Math.Max(1, player.DisplayAge - 23);
    statedWeeks    += rng.Next(2);   // 50% chance +1
    statedWeeks    *= 53;

    // Hidden minimums: stated minus 0–10% (random, independent per term)
    int minWage = (int)(statedWage * (1.0 - rng.Next(11) / 100.0));
    int minFee  = (int)(statedFee  * (1.0 - rng.Next(11) / 100.0));

    return new PlayerContractDemand
    {
        StatedWeeklyWage    = statedWage,
        StatedSigningFee    = statedFee,
        StatedContractWeeks = statedWeeks,
        MinimumWeeklyWage   = minWage,
        MinimumSigningFee   = minFee,
    };
}
```

Add an offer evaluation helper:

```csharp
/// <summary>
/// Returns true if the offered terms meet or beat the player's hidden minimums.
/// Contract weeks must be less than or equal to the player's stated maximum —
/// offering too long a contract causes rejection (BASIC line 2615: IF IA &lt;= HR THEN REJECT).
/// </summary>
public static bool EvaluateOffer(
    PlayerContractDemand demand,
    int offeredWage,
    int offeredFee,
    int offeredWeeks)
    => offeredWage  >= demand.MinimumWeeklyWage
    && offeredFee   >= demand.MinimumSigningFee
    && offeredWeeks <= demand.StatedContractWeeks;
```

#### `PlayerContractDemand` record

```csharp
public record PlayerContractDemand
{
    /// <summary>Weekly wage the player is asking for (shown to manager).</summary>
    public int StatedWeeklyWage    { get; init; }

    /// <summary>Signing-on fee the player is asking for (shown to manager).</summary>
    public int StatedSigningFee    { get; init; }

    /// <summary>
    /// Maximum contract length (weeks) the player will accept (shown to manager).
    /// Offering more weeks than this causes rejection.
    /// </summary>
    public int StatedContractWeeks { get; init; }

    /// <summary>Minimum weekly wage the player will accept (hidden from manager).</summary>
    public int MinimumWeeklyWage   { get; init; }

    /// <summary>Minimum signing fee the player will accept (hidden from manager).</summary>
    public int MinimumSigningFee   { get; init; }
}
```

---

## Step 4 — Negotiation UI

The negotiation runs inline on the squad screen (no separate screen). After a valid
`R<number>` command is parsed, `SquadScreen.Show` calls a private helper
`RunNegotiation(state, slot)` and then redraws.

### Layout

```
╭────────────────────────────────────────────────────────╮
│  JOHN  ·  MID  ·  Skill 7  ·  Age 28                  │
│  Contract expiring in 6 weeks                          │
│                                                        │
│  He is asking for:                                     │
│    Weekly wage     £340                                │
│    Signing-on fee  £700                                │
│    Contract        160 weeks                           │
│                                                        │
│  Accept (A), Counter-offer (C), Cancel (Enter):        │
╰────────────────────────────────────────────────────────╯
```

**Accept (A)** — immediately applies the stated terms (guaranteed to meet the hidden
minimums since stated ≥ minimum by construction).

**Counter-offer (C)** — prompts the manager for each term in turn:

```
  Weekly wage (£): >
  Signing-on fee (£): >
  Contract length (weeks, max 160): >
```

Each prompt is re-displayed (with the entered value shown) before moving to the next.
After all three are entered the offer is evaluated:

- **Accepted**: `"{Name} agrees to sign"` — apply and redraw.
- **Rejected**: `"{Name} rejects your offer"` — show error, redraw (manager may retry
  by entering `R<number>` again).

**Cancel (Enter / any other key)** — return to the squad screen without change.

### Signing fee affordability

Before accepting a counter-offer, validate that the manager can afford the signing fee:

```csharp
if (offeredFee > state.Finances.BankBalance)
    error = $"You cannot afford a signing-on fee of £{offeredFee}";
```

Mirror BASIC line 2608: `IF HT<0 OR (HT+HI)>AI THEN 2608`. (`HI` is a pending fee
from a previous deal in the same session — no equivalent in the current session model,
so only a simple balance check is needed.)

### Applying a signed contract

On acceptance (stated terms or successful counter-offer):

1. Update the player's contract:

   ```csharp
   player.WeeklyWage    = offeredWage;
   player.ContractWeeks = offeredWeeks;
   ```

2. Deduct the signing fee (existing `ContractService.ApplyRenewal`):

   ```csharp
   ContractService.ApplyRenewal(state.Finances, offeredFee);
   ```

3. Recalculate the weekly wage bill (already done on each weekly tick, no immediate
   action needed beyond updating `player.WeeklyWage`).

---

## Worked example

1. Slot 7 contains JONES, MID, Skill 7, Age 28, 6 weeks remaining.
2. Manager enters `R7`.
3. `ContractService.GetPlayerDemands` computes:
   - Stated wage £340, stated fee £700, stated weeks 160.
   - Hidden minimums: wage £323 (5% reduction), fee £644 (8% reduction).
4. Squad screen shows JONES's demands.
5. Manager presses `C` and enters £310 / £600 / 104.
6. `EvaluateOffer`: £310 < £323 → **rejected**. Screen shows "JONES rejects your offer."
7. Manager retries `R7`, this time enters £325 / £650 / 104.
8. `EvaluateOffer`: £325 ≥ £323, £650 ≥ £644, 104 ≤ 160 → **accepted**.
9. JONES's `WeeklyWage` = 325, `ContractWeeks` = 104. £650 deducted from bank.

---

## Step 5 — Contract expiry: player leaves automatically

When a player's `ContractWeeks` reaches 0 at the end of the weekly tick they leave the
club immediately — the squad slot is cleared and the manager is notified.

### `TheManager.Services/WeeklyTickService.cs` — contract decrement loop

Replace the current `foreach` (which iterates player objects and cannot null a slot)
with a `for` loop over slot indices 1–20:

```csharp
// Decrement each player's contract (BASIC line 188: V(2,I)=V(2,I)+(V(2,I)>0)).
// Players whose contract reaches 0 leave the club immediately.
var departed = new List<string>();
for (int i = 1; i <= 20; i++)
{
    var p = gameState.Squad[i];
    if (p is null) continue;
    if (p.ContractWeeks > 0) p.ContractWeeks--;
    if (p.ContractWeeks == 0)
    {
        departed.Add(p.Name.Trim());
        gameState.Squad[i] = null;
    }
}
```

### `TheManager.Services/WeeklyTickService.cs` — `WeeklyTickResult`

Add `DepartedPlayers` to the result record so callers know who left:

```csharp
public record WeeklyTickResult(
    WeeklyReport       FinanceReport,
    CrisisResult       Crisis,
    List<RandomEvent>  Events,
    string?            Resignation,
    double             Attendance,
    double             GateMoney,
    List<ScoutFinding> ScoutFindings,
    List<string>       DepartedPlayers);   // ← new
```

Pass `departed` as the final argument when constructing the result:

```csharp
return new WeeklyTickResult(report, crisis, events, resign, attendance, gateMoney,
                            scoutResult.Findings, departed);
```

### `TheManager.Models/MatchResult.cs`

Add a matching property so the console layer can display the departures:

```csharp
/// <summary>Players who left the club this week due to expired contracts.</summary>
public List<string> DepartedPlayers { get; set; } = new();
```

### `TheManager.Services/GameService.cs`

Propagate from the tick result to `MatchResult`:

```csharp
return new MatchResult
{
    // ... existing fields ...
    DepartedPlayers = tick.DepartedPlayers
};
```

### `TheManager.Console/Screens/PlayMatchScreen.cs`

Display after the scout news block, before the pause:

```csharp
if (result.DepartedPlayers.Count > 0)
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("  [bold dim]CONTRACT EXPIRIES[/]");
    foreach (var name in result.DepartedPlayers)
        AnsiConsole.MarkupLine($"  [red]{Markup.Escape(name)}[/] has left the club — contract expired");
}
```

---

## What does NOT need implementing

- The original BASIC's **blind offer flow** (manager enters terms, player silently
  accepts or rejects without ever stating demands). This spec replaces that with
  demand-first negotiation.
- **Re-negotiating mid-contract** — only contracts ≤ 10 weeks are eligible. If a
  manager wants to improve a player's terms earlier, that is out of scope.
- **End-of-week auto-expiry prompt** — BASIC lines 359–366 loop through all players at
  the end of a week and prompt for renewal of any with `V(2,F)=0`. That auto-renewal
  loop is a separate feature; this spec covers only the on-demand `R<number>` command.
- **Multiple simultaneous negotiations** — after a signing, the screen redraws normally;
  the manager re-enters `R<number>` for the next player. No batching.
- **The division-drop wage penalty** (BASIC line 2111, `HG-AP`) — not applicable for
  renewals (the player is already at the club, so `HG = AP` and the penalty term is
  zero). Only relevant for incoming transfers from higher-division clubs.

---

## Affected files

| File | Change |
|---|---|
| `TheManager.Services/ContractService.cs` | Add `GetPlayerDemands`, `EvaluateOffer`, `PlayerContractDemand` record |
| `TheManager.Services/WeeklyTickService.cs` | Replace `foreach` with `for` loop; collect departed players; add `DepartedPlayers` to `WeeklyTickResult` |
| `TheManager.Models/MatchResult.cs` | Add `DepartedPlayers` property |
| `TheManager.Services/GameService.cs` | Propagate `tick.DepartedPlayers` to `MatchResult` |
| `TheManager.Console/Screens/SquadScreen.cs` | Red highlight for `ContractWeeks <= 10`; parse `R<number>`; inline `RunNegotiation` helper; updated prompt hint and error messages |
| `TheManager.Console/Screens/PlayMatchScreen.cs` | Display contract expiry notifications after scout news |
