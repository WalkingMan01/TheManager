# Amiga Hardware Reference for AmigaBASIC

## Memory Map (OCS/ECS)

| Range | Description |
|-------|-------------|
| `$000000`–`$07FFFF` | Chip RAM (512 KB base; up to 1 MB ECS) |
| `$200000`–`$9FFFFF` | Fast RAM (expansion) |
| `$A00000`–`$BFFFFF` | CIA registers + slow RAM (trapdoor) |
| `$C00000`–`$DFFFFF` | Reserved / slow RAM |
| `$DFF000`–`$DFFFFF` | Custom chip registers |
| `$F80000`–`$FFFFFF` | Kickstart ROM |

## ExecBase

`ExecBase` pointer is always at address `$4` (4). Read with a long-word PEEK:

```basic
' AmigaBASIC PEEK is byte-wide; build a long:
FUNCTION PeekLong&(addr&)
  PeekLong& = PEEK(addr&) * 16777216& + PEEK(addr&+1) * 65536& + _
              PEEK(addr&+2) * 256& + PEEK(addr&+3)
END FUNCTION

execBase& = PeekLong&(4)
```

## Key Custom Chip Registers

### DMA Control (`$DFF096` DMACON write, `$DFF002` DMACONR read)

| Bit | Name | Meaning |
|-----|------|---------|
| 15 | SET/CLR | 1=set bits, 0=clear bits (write only) |
| 9 | DMAEN | Master DMA enable |
| 8 | BPLEN | Bitplane DMA |
| 7 | COPEN | Copper DMA |
| 6 | BLTEN | Blitter DMA |
| 5 | SPREN | Sprite DMA |
| 4 | DSKEN | Disk DMA |
| 3–0 | AUD3–AUD0 | Audio channel DMA |

Enable copper + bitplane + master:
```basic
POKE &HDFF096, &H8380   ' SET | DMAEN | BPLEN | COPEN  (word write needed!)
```

> AmigaBASIC POKE is byte-only. For 16-bit register writes, either use a machine-code stub or write both bytes explicitly (high byte first for big-endian 68000).

### Bitplane Control

| Register | Addr | Purpose |
|----------|------|---------|
| BPLCON0 | `$DFF100` | Bitplane depth, HAM, EHB, genlock |
| BPLCON1 | `$DFF102` | Horizontal scroll per bitplane |
| BPLCON2 | `$DFF104` | Sprite/playfield priority |
| BPL1MOD | `$DFF108` | Odd-bitplane modulo (inter-row skip bytes) |
| BPL2MOD | `$DFF10A` | Even-bitplane modulo |
| BPL1PTH/L | `$DFF0E0/E2` | Bitplane 1 pointer high/low |

### Colour Registers

`$DFF180`–`$DFF1BE` = COLOR00–COLOR31. Each is a 12-bit RGB word: `0x0RGB`.

```basic
' Set colour 0 (background) to dark blue
' $0025 = R:0, G:2, B:5
POKE &HDFF181, &H02   ' high byte (nibble: R=0, G=2)
POKE &HDFF180, &H25   ' wait — addr is $DFF180, big-endian
' Better: use a word-poke SUB
```

### Copper

Copper instructions are 32-bit (two 16-bit words):

| Type | Word 1 | Word 2 | Meaning |
|------|--------|--------|---------|
| MOVE | register offset (even, <$200) | value | Write value to custom reg |
| WAIT | `VVVVVVVH HHHHHH1` | `VVVVVVVM HHHHHHHM` | Wait for beam position |
| SKIP | same format as WAIT + bit 0 of W1 | same + bit 0 of W2=1 | Conditional skip |

End-of-list sentinel: two words of `$FFFFFFFE`.

Minimal copper list to change background colour at line 100:
```
; wait for line 100, any hpos
$6401, $FFFE   (wait V=100/$64, H=1 with mask $FFFE)
; write to COLOR00 ($DFF180 → offset $180)
$0180, $0025   (MOVE $0025 → COLOR00)
; end of list
$FFFF, $FFFE
$FFFF, $FFFE
```

To activate: write list base address to COP1LCH (`$DFF080`) + COP1LCL (`$DFF082`) and ensure copper DMA is on.

### Blitter Registers

| Register | Addr | Purpose |
|----------|------|---------|
| BLTCON0 | `$DFF040` | Channel use, minterm logic operation |
| BLTCON1 | `$DFF042` | Fill mode, line mode, shift |
| BLTAFWM | `$DFF044` | First-word mask for A |
| BLTALWM | `$DFF046` | Last-word mask for A |
| BLTCPTH/L | `$DFF048/4A` | Channel C source pointer |
| BLTBPTH/L | `$DFF04C/4E` | Channel B source pointer |
| BLTAPTH/L | `$DFF050/52` | Channel A source pointer |
| BLTDPTH/L | `$DFF054/56` | Destination pointer |
| BLTSIZE | `$DFF058` | Start blit: height (bits 15–6) × width in words (bits 5–0) |
| BLTCMOD | `$DFF060` | Modulo for C |
| BLTBMOD | `$DFF062` | Modulo for B |
| BLTAMOD | `$DFF064` | Modulo for A |
| BLTDMOD | `$DFF066` | Modulo for D |

**Always poll blitter-busy before starting a new blit:**
```basic
WHILE (PEEK(&HDFF002) AND &H40) <> 0 : WEND  ' wait for bit 6 of DMACONR
```

BLTCON0 logic: bits 3–0 are the minterm (boolean op on A, B, C). Common values:
- `$09F0` — copy A to D (A→D, fill=0, no shift, all channels used)
- `$0BF0` — A AND B → D
- `$0DF0` — A OR B → D
- `$0FF0` — fill with constant (useful with BLTCON1 fill mode)

### Paula — Audio Channels

Each channel has registers at base + offset:

| Offset | Register | Purpose |
|--------|----------|---------|
| `+00/02` | AUDxLCH/L | Sample pointer (chip RAM) |
| `+04` | AUDxLEN | Sample length in words |
| `+06` | AUDxPER | Period (clock ticks per sample, min 124) |
| `+08` | AUDxVOL | Volume 0–64 |

Channel bases: AUD0=`$DFF0A0`, AUD1=`$DFF0B0`, AUD2=`$DFF0C0`, AUD3=`$DFF0D0`.

Enable audio DMA (channel 0 + master): `DMACON |= $8201`.

Period → frequency: `freq = 3546895 / period` (PAL); `3579545 / period` (NTSC).

## CIA Chips

**CIA-A** (`$BFEC01` base, odd byte addresses only):
- `$BFEC01` — Port A (parallel/control)
- `$BFEE01` — Port B (parallel data)
- Timer A/B for timing and serial rates

**CIA-B** (`$BFD000` base, even byte addresses only):
- `$BFD000` — Port A (disk motor/select)
- `$BFD100` — Port B (disk data)

CIA registers are at 1 KB intervals; byte-wide, upper nibble is don't-care. Access with PEEK/POKE using the exact address.

## Intuition — OS-Level Integration

AmigaBASIC handles Intuition calls via its SCREEN/WINDOW/MENU statements. For direct library calls:

```basic
' Typical pattern to call an OS function:
' 1. Get library base from ExecBase's library list (or use OpenLibrary)
' 2. Add the LVO (negative offset from lib base)
' 3. Set up registers via CALL / machine-code stub

' LVO offsets (selected):
' exec.library: OpenLibrary = -552, CloseLibrary = -414, AllocMem = -198, FreeMem = -210
' intuition.library: OpenScreen = -198, CloseScreen = -66, OpenWindow = -204
' graphics.library: BltBitMap = -30, WaitBlit = -228, ScrollRaster = -396
```

Direct OS calls from AmigaBASIC require a CALL to a short machine-code stub (typically stored in chip RAM via DATA/READ loops) that loads registers and executes JSR (ea).

## Useful Addresses Summary

| Address | Description |
|---------|-------------|
| `$4` | ExecBase pointer |
| `$DFF002` | DMACONR (read DMA status) |
| `$DFF096` | DMACON (write enable/disable) |
| `$DFF100` | BPLCON0 |
| `$DFF180` | COLOR00 |
| `$DFF080` | COP1LCH (copper list 1 high) |
| `$DFF082` | COP1LCL (copper list 1 low) |
| `$DFF040` | BLTCON0 |
| `$DFF058` | BLTSIZE (triggers blit) |
| `$DFF0A0` | AUD0LCH |
| `$BFE001` | CIA-A Port A |
