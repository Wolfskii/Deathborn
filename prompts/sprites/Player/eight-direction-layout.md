# Eight-direction sheet layout

Canonical row order for every 8-way Deathborn player sprite sheet.

When generating images, include the **Prompt block** below in the animation prompt (or reference this file alongside `master-style-prompt.md`).

==================================================
PROMPT BLOCK
==================================================

Eight directions — exactly one row each. No duplicate directions. No missing directions.

Sheet layout: 8 rows × N columns (row = facing, column = animation frame, left to right).

Row order is mandatory — do not reorder, skip, merge, or repeat any row:

Row 1: South — front view, character faces the camera
Row 2: South-East — diagonal front-right quarter view
Row 3: East — profile facing right
Row 4: North-East — diagonal back-right quarter view
Row 5: North — back view, character faces away from the camera
Row 6: North-West — diagonal back-left quarter view
Row 7: West — profile facing left
Row 8: South-West — diagonal front-left quarter view

==================================================
IMPLEMENTATION (Deathborn client)
==================================================

Sheet rows are **0-based** in code. Row-major frame index:

`frameIndex = row * framesPerDirection + column`

| Sheet row | Compass     | `Facing8`   |
|----------:|-------------|-------------|
| 0         | South       | `Down`      |
| 1         | South-East  | `DownRight` |
| 2         | East        | `Right`     |
| 3         | North-East  | `UpRight`   |
| 4         | North       | `Up`        |
| 5         | North-West  | `UpLeft`    |
| 6         | West        | `Left`      |
| 7         | South-West  | `DownLeft`  |

Clockwise from South: S → SE → E → NE → N → NW → W → SW.

`Facing8` enum order in `Facing8.cs` differs from sheet row order — map through the table above (or a `SheetRowToFacing8` lookup) when wiring animations.
