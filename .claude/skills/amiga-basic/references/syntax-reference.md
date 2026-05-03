# AmigaBASIC Syntax Reference

## Data Types

| Suffix | Type | Size | Range |
|--------|------|------|-------|
| (none) or `!` | Single-precision float | 4 bytes | ±3.4e38, ~7 sig. digits |
| `#` | Double-precision float | 8 bytes | ±1.7e308, ~15 sig. digits |
| `%` | Integer | 2 bytes | –32768 to 32767 |
| `$` | String | variable | up to 32767 chars |

`DEFINT A-Z` at the top of a module makes all un-suffixed variables integers — useful for performance-critical sections.

## Variables & Declaration

```basic
DIM a%(100)          ' integer array, 101 elements (0..100)
DIM b!(50, 50)       ' 2D float array
DIM s$ AS STRING * 8 ' fixed-length string (faster, no heap alloc)
SHARED x%, y%        ' make module-level vars visible inside SUBs
```

## Control Flow

```basic
' Conditional
IF x% > 10 THEN PRINT "big" ELSE PRINT "small"

IF x% > 10 THEN
  PRINT "big"
ELSEIF x% > 5 THEN
  PRINT "medium"
ELSE
  PRINT "small"
END IF

' Loops
FOR i% = 1 TO 100 STEP 2
  NEXT i%

WHILE condition
  WEND

DO WHILE condition   ' preferred — supports EXIT DO
  EXIT DO
LOOP

DO
  LOOP UNTIL condition

' Branching
GOTO 1000
GOSUB 2000 : RETURN  ' call/return — classic subroutine pattern

ON x% GOTO 100, 200, 300   ' computed jump (x%=1 → 100, etc.)
ON x% GOSUB 100, 200, 300  ' computed call
```

## Procedures

```basic
' Named subroutine (preferred over GOSUB for new code)
SUB DrawSprite(x%, y%, frame%)
  SHARED spriteData%()
  ' body
END SUB

CALL DrawSprite(10, 20, 0)
DrawSprite 10, 20, 0       ' CALL keyword optional

' Function returning a value
FUNCTION Distance#(x1#, y1#, x2#, y2#)
  Distance# = SQR((x2#-x1#)^2 + (y2#-y1#)^2)
END FUNCTION
```

## String Operations

```basic
LEN(s$)               ' length
LEFT$(s$, n%)         ' leftmost n chars
RIGHT$(s$, n%)        ' rightmost n chars  
MID$(s$, start%, len%) ' substring (1-based start)
MID$(s$, start%) = t$ ' in-place replace
INSTR(s$, t$)         ' find t$ in s$ (0 = not found)
STR$(n)               ' number to string
VAL(s$)               ' string to number
CHR$(n%)              ' ASCII code to char
ASC(s$)               ' first char to ASCII code
UCASE$(s$) / LCASE$(s$)
```

## Math & Bitwise

```basic
INT(x)   FIX(x)   ABS(x)   SGN(x)
SQR(x)   EXP(x)   LOG(x)
SIN(x)   COS(x)   TAN(x)   ATN(x)   ' radians

' Bitwise (integer operands)
x% AND y%
x% OR  y%
x% XOR y%
NOT x%            ' bitwise NOT

' Hex / octal literals
x% = &HFF00       ' hex
x% = &O777        ' octal
HEX$(x%)          ' integer → hex string
OCT$(x%)          ' integer → octal string
```

## I/O

```basic
PRINT "text"; x%; " more"   ' ; suppresses newline
PRINT "text", x%             ' , tab-separated
INPUT "prompt: ", x%
LINE INPUT "prompt: ", s$    ' read whole line including spaces

OPEN "file.txt" FOR INPUT AS #1
OPEN "out.txt"  FOR OUTPUT AS #2
OPEN "data.bin" FOR RANDOM AS #3 LEN = 32   ' fixed record
LINE INPUT #1, s$
PRINT #2, x%
CLOSE #1 : CLOSE #2

' File test
IF DIR$("myfile.txt") = "" THEN PRINT "not found"
```

## Error Handling

```basic
ON ERROR GOTO errHandler
' ... normal code ...
errHandler:
  PRINT "Error"; ERR; "at line"; ERL
  RESUME NEXT   ' continue after the failing statement
  ' or RESUME   ' retry the failing statement
  ' or RESUME 0 ' same as RESUME
  ' or END
```

## Memory Access

```basic
PEEK(addr&)           ' read byte at address (LONG address)
POKE addr&, value%    ' write byte
PEEK(addr&) + PEEK(addr&+1)*256   ' manual 16-bit read (big-endian)

' Allocate a block in chip RAM via exec AllocMem:
' ExecBase = PEEK(4) (word-pointer, needs long read)
' Use CALL with LVO -198 (AllocMem), requirements = $00010002 (chip|public)
```

## Graphics & Screen

```basic
SCREEN 1, 320, 200, 4, 1  ' id, width, height, depth(bits), mode(0=lores,1=hires)
WINDOW 1, "Title", (0,0)-(319,199), 31, 1  ' on screen 1

PALETTE 0, 0, 0, 0    ' index, r, g, b  (0.0–1.0 floats)

PSET (x%, y%), colour%
LINE (x1%,y1%)-(x2%,y2%), colour%
CIRCLE (cx%,cy%), r%, colour%
PAINT (x%,y%), fillColour%, borderColour%

GET (x1%,y1%)-(x2%,y2%), buffer%()   ' capture area to array
PUT (x%,y%), buffer%(), mode%        ' blit array  (mode: 0=PSET,1=PRESET,2=AND,3=OR,4=XOR)
```
