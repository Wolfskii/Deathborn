extends Node2D
## Draws a large ground plane and grid so movement is easy to read against a
## fixed reference. World-space (not camera-attached) so the player visibly
## crosses grid lines as they move.

const HALF_EXTENT := 2400.0
const CELL := 64.0

const GROUND := Color(0.18, 0.28, 0.16)
const CELL_A := Color(0.20, 0.32, 0.18)
const CELL_B := Color(0.16, 0.25, 0.14)
const GRID := Color(0.32, 0.45, 0.28, 0.55)
const ORIGIN := Color(1.0, 0.85, 0.2, 0.9)


func _ready() -> void:
	z_index = -10
	queue_redraw()


func _draw() -> void:
	var extent := HALF_EXTENT
	draw_rect(Rect2(-extent, -extent, extent * 2.0, extent * 2.0), GROUND)

	# Checkerboard cells make motion obvious even before crossing a grid line.
	var cols := int((extent * 2.0) / CELL) + 1
	var rows := cols
	var start_x := -extent
	var start_y := -extent
	for row in rows:
		for col in cols:
			var col_color := CELL_A if (row + col) % 2 == 0 else CELL_B
			draw_rect(
				Rect2(start_x + col * CELL, start_y + row * CELL, CELL, CELL),
				col_color
			)

	# Major grid lines every cell.
	for i in range(cols + 1):
		var x := start_x + i * CELL
		draw_line(Vector2(x, -extent), Vector2(x, extent), GRID, 1.0)
	for i in range(rows + 1):
		var y := start_y + i * CELL
		draw_line(Vector2(-extent, y), Vector2(extent, y), GRID, 1.0)

	# Spawn / origin marker.
	draw_line(Vector2(-24, 0), Vector2(24, 0), ORIGIN, 2.0)
	draw_line(Vector2(0, -24), Vector2(0, 24), ORIGIN, 2.0)
	draw_arc(Vector2.ZERO, 8.0, 0.0, TAU, 24, ORIGIN, 2.0)
