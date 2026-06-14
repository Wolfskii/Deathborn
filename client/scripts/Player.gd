extends Node2D
## A single rendered player. Position is server-authoritative: we smoothly
## interpolate toward the last position the server sent rather than simulating
## locally. This applies to the local player too (no client-side prediction in
## the walking skeleton).

const RADIUS := 12.0
const LERP_SPEED := 12.0

var target_pos := Vector2.ZERO
var is_local := false
var move_dir := Vector2.ZERO

var _label: Label


func setup(display_name: String, pos: Vector2, local: bool) -> void:
	position = pos
	target_pos = pos
	is_local = local
	_ensure_label()
	_label.text = display_name
	queue_redraw()


func _ensure_label() -> void:
	if _label == null:
		_label = Label.new()
		_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		_label.position = Vector2(-60, -RADIUS - 22)
		_label.size = Vector2(120, 18)
		add_child(_label)


func set_target(pos: Vector2) -> void:
	if pos.distance_squared_to(target_pos) > 0.01:
		move_dir = (pos - position).normalized()
	target_pos = pos


func _process(delta: float) -> void:
	var prev := position
	position = position.lerp(target_pos, clampf(delta * LERP_SPEED, 0.0, 1.0))
	if is_local and position.distance_squared_to(prev) > 0.01:
		queue_redraw()


func _draw() -> void:
	var col := Color(0.35, 0.8, 1.0) if is_local else Color(1.0, 0.45, 0.35)
	draw_circle(Vector2.ZERO, RADIUS, col)
	draw_arc(Vector2.ZERO, RADIUS, 0.0, TAU, 32, Color(0, 0, 0, 0.6), 2.0)

	# Facing / movement hint for the local player.
	if is_local and move_dir.length_squared() > 0.01:
		var tip := move_dir * (RADIUS + 10.0)
		draw_line(Vector2.ZERO, tip, Color(1.0, 1.0, 1.0, 0.9), 3.0)
		draw_circle(tip, 3.0, Color(1.0, 1.0, 1.0, 0.9))
