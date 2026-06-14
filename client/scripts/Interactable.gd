class_name Interactable
extends Node2D
## World object the player can click or press E to interact with when in range.

signal interacted

enum Kind { GENERIC, TREE, ROCK, CHEST, NPC, FISHING, BANK, SIGN, ANVIL }

@export var interact_id: String = ""
@export var display_name: String = "Object"
@export var kind: Kind = Kind.GENERIC
@export var pick_radius: float = 18.0
@export var interact_range: float = 72.0
@export var tint: Color = Color(0.55, 0.45, 0.32)

var _highlighted := false

var _label: Label


func _ready() -> void:
	_ensure_label()
	_label.text = display_name
	queue_redraw()


func is_near_point(world_pos: Vector2) -> bool:
	return global_position.distance_to(world_pos) <= pick_radius


func is_in_interact_range(from: Vector2) -> bool:
	return global_position.distance_to(from) <= interact_range


func set_highlighted(on: bool) -> void:
	if _highlighted == on:
		return
	_highlighted = on
	queue_redraw()


func interact() -> void:
	interacted.emit()
	_flash()


func _ensure_label() -> void:
	if _label != null:
		return
	_label = Label.new()
	_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_label.position = Vector2(-70, -pick_radius - 22)
	_label.size = Vector2(140, 18)
	add_child(_label)


func _flash() -> void:
	var tween := create_tween()
	tween.tween_property(self, "modulate", Color(1.4, 1.4, 1.4), 0.08)
	tween.tween_property(self, "modulate", Color.WHITE, 0.12)


func _draw() -> void:
	var body := tint.lightened(0.25) if _highlighted else tint
	var outline := Color(1.0, 0.92, 0.55, 0.95) if _highlighted else Color(0, 0, 0, 0.55)

	match kind:
		Kind.TREE:
			draw_circle(Vector2(0, 4), pick_radius * 0.55, Color(0.35, 0.22, 0.12))
			draw_circle(Vector2(0, -6), pick_radius * 0.85, body)
		Kind.ROCK:
			draw_circle(Vector2.ZERO, pick_radius * 0.9, body)
			draw_arc(Vector2(-4, -2), pick_radius * 0.35, 0, TAU, 12, body.lightened(0.15), 1.0)
		Kind.CHEST:
			draw_rect(Rect2(-pick_radius, -pick_radius * 0.6, pick_radius * 2, pick_radius * 1.2), body)
			draw_rect(Rect2(-pick_radius, -pick_radius * 0.15, pick_radius * 2, pick_radius * 0.2), body.darkened(0.2))
		Kind.NPC:
			draw_circle(Vector2.ZERO, pick_radius * 0.75, body)
			draw_circle(Vector2(0, -pick_radius * 0.9), pick_radius * 0.45, body.lightened(0.1))
		Kind.FISHING:
			draw_circle(Vector2(0, 6), pick_radius * 1.1, Color(0.18, 0.35, 0.62, 0.85))
			draw_line(Vector2(-8, -8), Vector2(10, -18), body, 2.5)
			draw_circle(Vector2(10, -18), 3.0, body)
		Kind.BANK:
			draw_rect(Rect2(-pick_radius * 0.9, -pick_radius * 0.5, pick_radius * 1.8, pick_radius), body)
			draw_rect(Rect2(-pick_radius * 0.35, -pick_radius * 0.2, pick_radius * 0.7, pick_radius * 0.65), body.darkened(0.25))
		Kind.SIGN:
			draw_rect(Rect2(-2, -pick_radius, 4, pick_radius * 1.6), Color(0.4, 0.28, 0.16))
			draw_rect(Rect2(-pick_radius * 0.8, -pick_radius * 1.1, pick_radius * 1.6, pick_radius * 0.55), body)
		Kind.ANVIL:
			draw_rect(Rect2(-pick_radius * 0.7, -4, pick_radius * 1.4, 8), body.darkened(0.15))
			draw_rect(Rect2(-pick_radius * 0.45, -pick_radius * 0.55, pick_radius * 0.9, pick_radius * 0.45), body)
		_:
			draw_circle(Vector2.ZERO, pick_radius, body)

	draw_arc(Vector2.ZERO, pick_radius + 4, 0, TAU, 32, outline, 2.0 if _highlighted else 1.5)

	if _highlighted:
		draw_arc(Vector2.ZERO, interact_range, 0, TAU, 48, Color(1, 1, 1, 0.08), 1.0)
