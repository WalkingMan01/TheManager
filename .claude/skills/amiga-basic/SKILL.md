---
name: amiga-basic
description: Amiga BASIC development and code review skill for senior developers. Use when the user asks to write, explain, debug, or review AmigaBASIC code, or asks about Amiga-specific hardware programming (copper lists, blitter, Paula sound, custom chips, chip RAM vs fast RAM, Intuition). Also triggers on file extensions .bas, .BASIC in Amiga context, or mentions of AmigaOS 1.x/2.x BASIC.
version: 1.0.0
---

# Amiga BASIC Development

You are assisting a **senior developer** working with AmigaBASIC — the BASIC dialect that shipped with AmigaOS 1.x and 2.x on Commodore Amiga computers (1985–1993). The developer understands modern software engineering deeply; explanations should connect Amiga concepts to modern analogues rather than over-explaining fundamentals.

Reference the detailed guides in this skill's `references/` directory when needed:
- [syntax-reference.md](references/syntax-reference.md) — language syntax, data types, control flow
- [amiga-hardware.md](references/amiga-hardware.md) — custom chips, memory map, hardware registers
- [common-patterns.md](references/common-patterns.md) — idioms, gotchas, performance patterns

---

## Core Orientation

AmigaBASIC is an **interpreted** BASIC with:
- Line numbers (required — they define execution order and are GOTO targets)
- Weakly typed variables: numeric (`x`, `x!`, `x#`) or string (`x$`)
- Subroutines via `GOSUB`/`RETURN` and named `SUB`/`END SUB` blocks
- Direct hardware access through `PEEK`/`POKE` and `CALL` into OS libraries
- AmigaOS Intuition integration for windowed I/O (`WINDOW`, `SCREEN`, `MENU`)

Think of it as: Python-level interactivity, C-level hardware access, with 1980s memory and CPU constraints (68000 @ 7.14 MHz, typically 512 KB chip RAM).

---

## Addressing the Developer

- Skip basic language theory — they know what a loop is.
- **Do** explain Amiga-specific hardware behaviour, register names, and OS library quirks.
- **Do** call out differences from modern BASIC dialects (QBasic, VB, etc.).
- **Do** flag memory type constraints (chip vs fast RAM) when they affect correctness.
- When reviewing code, note both correctness issues and performance traps specific to the 68000 / custom chip pipeline.

---

## Key Technical Areas

### Memory Architecture
- **Chip RAM** (up to 512 KB OCS, 1 MB ECS): shared between CPU and custom chips (Agnus DMA). Sprites, bitplanes, copper lists, audio samples, and blitter work data **must** be in chip RAM.
- **Fast RAM** (expansion, 0–8 MB): CPU-only, no DMA contention — faster for code and non-DMA data.
- AmigaBASIC allocates from chip RAM by default. PEEK/POKE to hardware registers requires addresses in chip RAM range (`$00000000`–`$001FFFFF`).

### Custom Chip Set (OCS/ECS)
| Chip | Role | Key registers |
|------|------|---------------|
| **Agnus** | DMA controller, copper, blitter | `DMACONR` `$DFF002`, `DMACON` `$DFF096` |
| **Denise** | Graphics output (sprites, playfields) | `BPLCON0` `$DFF100`, `COLOR00` `$DFF180` |
| **Paula** | Audio (4 channels) + serial/disk I/O | `AUD0LCH` `$DFF0A0`, `ADKCON` `$DFF09E` |
| **CIA-A/B** | Timers, keyboard, parallel/serial | `$BFEC01`–`$BFFE01` |

### Copper
The copper is a co-processor that executes a simple instruction list synchronised to the video beam. AmigaBASIC cannot write copper lists directly — you must `POKE` the list into chip RAM and point `COP1LCH/COP1LCL` (`$DFF080/82`) at it, then enable copper DMA.

### Blitter
Hardware accelerated block copy/fill/line-draw. Initiates via BLTCON0/1, source/dest pointers, and modulo registers. Always poll `DMACONR` bit 14 (blitter busy) before starting a new blit from AmigaBASIC.

### Intuition / OS Libraries
AmigaBASIC uses the Intuition library for all windowed output. `WINDOW`, `SCREEN`, `MENU`, and `PATTERN` statements wrap Intuition calls. Direct `CALL` into exec.library or other OS libraries requires knowing the library base address from `PEEK` on `$4` (ExecBase) and computing the LVO (Library Vector Offset).

---

## Common Gotchas

1. **Line number gaps** — leave gaps (10, 20, 30…) for later insertions; renumbering large programs is painful.
2. **Integer vs float** — unadorned names are single-precision float (`!`). Append `%` for integer: `x% = 42`. Integer arithmetic is ~3× faster on the 68000.
3. **String length** — `DIM a$ AS STRING * 40` for fixed-length; dynamic strings heap-allocate on every assignment.
4. **PEEK/POKE word vs byte** — hardware registers are often 16-bit. AmigaBASIC's `POKE` is byte-wide; use `DEC HEX$` tricks or write a `CALL` wrapper for word-wide writes.
5. **Supervisor mode** — some hardware registers require supervisor mode. AmigaBASIC runs in user mode; attempts to write protected registers silently fail or cause a guru meditation (system crash).
6. **SCREEN vs WINDOW** — `SCREEN` opens a custom Intuition screen; `WINDOW` opens a window on the existing screen. Mixing them carelessly fragments chip RAM.
7. **END vs STOP vs SYSTEM** — `END` closes the program; `SYSTEM` returns to Workbench/CLI and frees all resources. Prefer `SYSTEM` in production code.

---

## Code Review Checklist

When reviewing AmigaBASIC code, check:

- [ ] Chip RAM requirement met for any data accessed by custom chips
- [ ] Blitter busy check before sequential blits
- [ ] Integer types (`%`) used for counters and array indices (speed)
- [ ] `CLOSE` called for all opened files and channels
- [ ] `SCREEN CLOSE` / `WINDOW CLOSE` called on exit to return chip RAM
- [ ] No direct writes to supervisor-mode registers from user mode
- [ ] `ON ERROR GOTO` present for file I/O paths
- [ ] Copper list terminated with `$FFFFFFFE` (wait for end-of-frame) + `$FFFFFFFE` (end)

---

## Writing New Code

When writing new AmigaBASIC code:

1. Start with resource cleanup at the top via `ON ERROR GOTO cleanup` and a `cleanup:` label at the end.
2. Use `%` integer variables for all loop counters, array indices, and bitmask operations.
3. Place hardware register constants as `CONST` or comment-labelled variables at the top of the file.
4. For copper / blitter work, isolate the chip RAM setup into a `SUB` with a clear contract on what RAM range it owns.
5. Always document the chip RAM layout as a comment block: start address, size, purpose.
