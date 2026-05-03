# AmigaBASIC Common Patterns & Idioms

## Program Structure Template

```basic
10  ' ============================================================
20  ' Program: MyDemo
30  ' Requires: AmigaOS 1.3+, 512KB chip RAM
40  ' Chip RAM layout: $50000-$5FFFF sprite data
50  ' ============================================================
60  ON ERROR GOTO 9000
70  GOSUB 1000  ' init
80  GOSUB 2000  ' main loop
90  GOSUB 8000  ' cleanup
100 SYSTEM

1000 ' --- INIT ---
1010 SCREEN 1, 320, 200, 5, 0   ' 32-colour lowres
1020 WINDOW 1, "Demo", (0,0)-(319,199), 0, 1
1030 RETURN

2000 ' --- MAIN LOOP ---
2010 WHILE NOT done%
2020   GOSUB 3000  ' update
2030   GOSUB 4000  ' render
2040 WEND
2050 RETURN

8000 ' --- CLEANUP ---
8010 SCREEN CLOSE 1
8020 RETURN

9000 ' --- ERROR HANDLER ---
9010 PRINT "Error"; ERR; "at line"; ERL
9020 GOSUB 8000
9030 SYSTEM
```

## Word-Wide PEEK/POKE

AmigaBASIC PEEK/POKE are byte-only. Hardware registers are 16-bit big-endian.

```basic
' Read 16-bit register (big-endian)
FUNCTION PeekWord%(addr&)
  PeekWord% = PEEK(addr&) * 256 + PEEK(addr& + 1)
END FUNCTION

' Write 16-bit register (big-endian)
SUB PokeWord(addr&, val%)
  POKE addr&,     (val% AND &HFF00) \ 256
  POKE addr& + 1, (val% AND &HFF)
END SUB

' Read 32-bit long
FUNCTION PeekLong&(addr&)
  PeekLong& = PEEK(addr&)   * 16777216& + _
              PEEK(addr&+1) * 65536&    + _
              PEEK(addr&+2) * 256&      + _
              PEEK(addr&+3)
END FUNCTION
```

## Double-Buffered Display

Prevents tearing by swapping which bitplane the display shows vs which one is being drawn to.

```basic
100 DIM plane&(1)        ' chip RAM base addresses for two planes
110 plane&(0) = &H50000
120 plane&(1) = &H58000
130 active% = 0

' In render loop:
200 drawPlane& = plane&(1 - active%)   ' draw to back buffer
210 ' ... draw into drawPlane& ...
220 ' Swap: point copper/hardware to drawPlane&
230 PokeWord &HDFF0E0, (drawPlane& AND &HFFFF0000&) \ 65536   ' BPL1PTH
240 PokeWord &HDFF0E2, drawPlane& AND &HFFFF&                  ' BPL1PTL
250 active% = 1 - active%
```

## Waiting for Vertical Blank

Synchronise to the display beam to avoid tearing and get consistent 50/60 Hz timing.

```basic
' Busy-wait on VPOSR bit 0 (long frame indicator toggles each field)
' More reliable: poll INTREQR bit 5 (VERTB — vertical blank interrupt flag)

SUB WaitVBlank()
  WHILE (PeekWord%(&HDFF01E) AND &H20) = 0 : WEND  ' wait for VERTB set
  PokeWord &HDFF09C, &H0020   ' clear VERTB flag (write 0 to set in INTREQ)
END SUB
```

## Machine-Code Stub in DATA

For operations BASIC can't do directly (word POKE, privileged access), embed a short 68000 stub:

```basic
1000 ' Allocate stub space in chip RAM
1010 DIM stub%(16)
1020 stubAddr& = VARPTR(stub%(0))
1030 ' MOV.W D0,(A0) — write word in D0 to address in A0, then RTS
1040 DATA &H3080, &H4E75   ' 3080 = MOVE.W D0,(A0); 4E75 = RTS
1050 FOR i% = 0 TO 1
1060   READ w%
1070   POKE stubAddr& + i%*2,     (w% AND &HFF00) \ 256
1080   POKE stubAddr& + i%*2 + 1, (w% AND &HFF)
1090 NEXT i%

' Call: CALL stubAddr&(value%, regAddr&)
' Loads value% into D0, regAddr& into A0, executes stub
```

## Palette Fade

```basic
SUB FadePalette(steps%)
  DIM r%(31), g%(31), b%(31)
  ' Save current palette
  FOR c% = 0 TO 31
    w% = PeekWord%(&HDFF180 + c%*2)
    r%(c%) = (w% AND &H0F00) \ 256
    g%(c%) = (w% AND &H00F0) \ 16
    b%(c%) = (w% AND &H000F)
  NEXT c%
  ' Fade to black
  FOR s% = steps% TO 0 STEP -1
    FOR c% = 0 TO 31
      nr% = r%(c%) * s% \ steps%
      ng% = g%(c%) * s% \ steps%
      nb% = b%(c%) * s% \ steps%
      PokeWord &HDFF180 + c%*2, nr%*256 + ng%*16 + nb%
    NEXT c%
    WaitVBlank
  NEXT s%
END SUB
```

## Audio Sample Playback

```basic
SUB PlaySample(chan%, sampleAddr&, lenWords%, period%, volume%)
  ' chan%: 0–3; sampleAddr& must be in chip RAM
  DIM base&
  base& = &HDFF0A0 + chan% * 16&
  ' Stop channel first
  PokeWord &HDFF096, 1 + chan%   ' clear DMA for this channel
  WaitVBlank                      ' let DMA stop cleanly
  ' Set pointer
  PokeWord base&,     (sampleAddr& AND &HFFFF0000&) \ 65536
  PokeWord base& + 2, sampleAddr& AND &HFFFF&
  ' Length, period, volume
  PokeWord base& + 4, lenWords%
  PokeWord base& + 6, period%
  PokeWord base& + 8, volume%
  ' Enable DMA for this channel + master
  PokeWord &HDFF096, &H8200 OR (1 + chan%)
END SUB
```

## Performance Notes

| Pattern | Slow | Fast |
|---------|------|------|
| Loop counter type | `i` (float) | `i%` (integer) |
| Array subscript | `a(i)` | `a%(i%)` |
| Multiply by power of 2 | `x * 8` | `x% * 8` (int mul) or blitter fill |
| Screen clear | `CLS` | Blitter fill direct to chip RAM |
| Sprite movement | BASIC sprite cmds | Direct blitter + copper |
| String concat in loop | `s$ = s$ + c$` | Use fixed `STRING * n` buffers |

The 68000 divides and floating-point operations are very slow (~50–100 cycles vs ~4 for integer add). Multiply is ~70 cycles for 16×16. Shift via `\2`, `\4` etc. compiles to shifts in the interpreter.

## Debugging

AmigaBASIC has no built-in debugger. Techniques:
- `TRON`/`TROFF` — turn on/off line-number tracing to current output window
- `STOP` — enters interactive STOP mode (inspect variables with `? varname`)
- `CONT` — resume from STOP
- Print intermediate values to a dedicated debug window:
  ```basic
  WINDOW 2, "Debug", (0,0)-(319,50), 1   ' create debug overlay
  WINDOW OUTPUT 2
  PRINT "x%="; x%; " y%="; y%
  WINDOW OUTPUT 1                          ' back to main window
  ```
- Check `ERR` / `ERL` in the error handler for crash context.
- For hardware bugs: use a second Amiga or UAE emulator with hardware logging to verify register writes.
