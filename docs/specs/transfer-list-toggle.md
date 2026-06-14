# Specification: Transfer-List Toggle from the Squad Screen

## Overview

Add the ability to transfer-list (or un-list) a player directly from the squad screen
by entering `T` followed by the player's squad number. A transfer-listed player's name
is highlighted in red in the squad table, mirroring the original BASIC behaviour.

## BASIC reference

In `FOOT.BAS`, the squad screen menu (line 1907) offers `TRAN` as one of the options.
Selecting it (`IN$="T"`, line 1275/1909 → `GOTO 1925`) prompts:

```
1925 INPUT "WHICH PLAYER    ";I
1926 IF I<1 OR I>20 THEN 1905
1927 TEMP$=V$(I):GOSUB 64900
     IF flag OR E(1,I)<0 OR J(I)=82 OR J(I)=76 OR J(I)=42 THEN 1925
1928 G(I)=G(I)+ABS(G(I)<0)*(ABS(G(I))*2)-ABS(G(I)>0)*(G(I)*2)
1929 GOTO 1902
```

- `G(I)` is the player's age. Line 1928 flips its sign — this is the transfer-list
  toggle (already documented on `Player.Age`: *"positive = settled, negative =
  transfer-listed"*).
- Line 1927 rejects the toggle (loops back to the prompt) if:
  - `flag` — slot is empty / invalid (from `GOSUB 64900`)
  - `E(1,I)<0` — player is on the retirement track (`Player.IsRetiring`, see
    `docs/specs/player-retirement.md`)
  - `J(I)=82` — `Retiring`
  - `J(I)=76` — `LoanUnavailable`
  - `J(I)=42` — `OnLoan`
- When listing/printing the squad, `IF G(I)<0 THEN CALL pa2` (lines 1243, 1397) switches
  to the red colour before printing the player's name.

---

## Squad screen input

`SquadScreen.Show` (`TheManager.Console/Screens/SquadScreen.cs`) currently only accepts
two numbers to swap slots (`TryParseSwap`). Extend the input parsing to also accept:

```
T<number>   e.g. "T9" or "T 9"
```

- Case-insensitive (`t9` also works).
- `<number>` must be a valid squad slot **1–20** (first team, sub, reserves —
  matches the BASIC's `1 TO 20` range; transfer-target slots 21–28 are not eligible).
- The slot must contain a player (`state.Squad[n] is not null`).

On a valid `T<number>` command, toggle that player's transfer-listed state and redraw
the screen — do not exit the loop (same as a successful swap).

This is a toggle, not a one-way action: if the player at that slot is already
transfer-listed, entering `T<number>` again **removes** them from the transfer list
(`Player.Age` flips back to positive) and clears the red highlight. The same `T<number>`
command is used for both listing and un-listing — no separate "remove" command is
needed.

### Validation / error messages

Reuse the existing `error` mechanism (shown in red above the prompt):

| Condition | Message |
|---|---|
| Number outside 1–20 | `"Enter a player number between 1 and 20, e.g. \"T9\""` |
| Slot is empty | `"There is no player in slot {n}"` |
| `player.IsRetiring` | `"{Name} cannot be transfer-listed"` |

> The `J(I)=76/42` checks (`LoanUnavailable`, `OnLoan`) from the BASIC cannot currently
> be enforced — `Player` has no populated `PlayerStatus` field today (see "What does NOT
> need implementing" below). The `J(I)=82` (`Retiring`) check is covered by
> `player.IsRetiring` (see `docs/specs/player-retirement.md`). Only the `IsRetiring`
> check and the empty-slot check are implemented now.

### Updated prompt hint

```
Enter two slot numbers to swap (e.g. 3 9), T<number> to transfer-list a player
(e.g. T9), or press Enter to go back:
```

---

## Model changes

### `TheManager.Models/Player.cs`

Add a derived property alongside `DisplayAge`, formalising the existing sign
convention documented on `Age`:

```csharp
/// <summary>True if the player is transfer-listed (Age is negative). Corresponds to G(I)&lt;0.</summary>
public bool IsTransferListed => Age < 0;
```

No change to the underlying `Age` storage — the sign convention already exists and is
used by `DisplayAge`.

---

## Service changes

### `TheManager.Services/PlayerService.cs`

Add a toggle method alongside `RecalculateStatus`:

```csharp
/// <summary>
/// Toggles a player's transfer-listed flag by flipping the sign of their age.
/// Corresponds to BASIC line 1928: G(I) = -G(I).
/// </summary>
/// <returns>False if the player is retiring and cannot be listed; true if the toggle
/// was applied.</returns>
public static bool ToggleTransferListed(Player player)
{
    if (player.IsRetiring)
        return false;

    player.Age = -player.Age;
    return true;
}
```

`SquadScreen` calls this method; if it returns `false`, show the
"`{Name} cannot be transfer-listed`" error instead of toggling.

---

## Display changes

### `TheManager.Console/Screens/SquadScreen.cs` — `AddSection`

Currently:

```csharp
string name = player is null ? "[dim]—[/]"
            : player.IsStar  ? $"[yellow]{player.Name}[/]"
            : player.Name;
```

Update so transfer-listed takes precedence over the star colouring (matching the BASIC,
where `pa2` — red — is called unconditionally before printing the name when `G(I)<0`,
overriding any other colour):

```csharp
string name = player is null            ? "[dim]—[/]"
            : player.IsTransferListed    ? $"[red]{player.Name}[/]"
            : player.IsStar              ? $"[yellow]{player.Name}[/]"
            : player.Name;
```

---

## Worked example

1. Player presses Enter on the squad screen with input `T7`.
2. Slot 7 contains a midfielder who is not retiring (`IsRetiring == false`).
3. `PlayerService.ToggleTransferListed(squad[7])` flips `Age` to negative, returns `true`.
4. Screen redraws; the player's name in row 7 now renders as `[red]NAME[/]`.
5. Entering `T7` again flips `Age` back to positive, removing the red highlight.

---

## What does NOT need implementing

- The `J(I)=76/42` (`LoanUnavailable` / `OnLoan`) exclusion checks from BASIC line 1927.
  `Player` has no populated status field to check against today, and `PlayerStatus` is
  currently unused dead code. Adding a full status-tracking system is out of scope for
  this feature — only the `IsRetiring` (covers `J(I)=82`) and empty-slot checks are
  ported.
- A separate "TRAN" menu/screen — the BASIC's multi-step menu (`MENU CHANGE SACK EDIT
  TRAN FIELD` → prompt → toggle) is collapsed into a single inline `T<number>` command
  on the existing squad screen prompt.
- Any change to how transfer-listed players are surfaced on the transfer market /
  AI club offers — this spec covers only the squad-screen toggle and highlight.

---

## Affected files

| File | Change |
|---|---|
| `TheManager.Models/Player.cs` | Add `IsTransferListed` derived property |
| `TheManager.Services/PlayerService.cs` | Add `ToggleTransferListed(Player)` |
| `TheManager.Console/Screens/SquadScreen.cs` | Parse `T<number>` input, call toggle, update prompt hint and error messages, red-highlight transfer-listed names in `AddSection` |
