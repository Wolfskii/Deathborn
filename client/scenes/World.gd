extends Node2D
## The world scene. Renders all players from server snapshots, sends local input
## each frame, and follows the local player with the camera.

const PlayerScene := preload("res://scripts/Player.gd")
const InteractableScene := preload("res://scripts/Interactable.gd")
const INPUT_SEND_INTERVAL := 0.05 # seconds (~20Hz, matches server tick)
const _WS_STATES: PackedStringArray = ["closed", "connecting", "open", "closing"]

@onready var _camera: Camera2D = $Camera2D
@onready var _players_root: Node2D = $Players
@onready var _interactables_root: Node2D = $Interactables
@onready var _status: Label = $UI/Status
@onready var _hud: Label = $UI/Hud
@onready var _interact_prompt: Label = $UI/InteractPrompt
@onready var _hotbar: Hotbar = $UI/Hotbar

var _players := {} # id (int) -> Player node
var _interactables: Array[Interactable] = []
var _focused: Interactable = null
var _hovered: Interactable = null
var _send_accum := 0.0
var _last_dir := Vector2.ZERO
var _local_pos := Vector2.ZERO
var _player_count := 0


func _ready() -> void:
	_camera.make_current()
	Net.snapshot.connect(_on_snapshot)
	Net.disconnected.connect(_on_disconnected)
	_hotbar.slot_activated.connect(_on_hotbar_slot)
	_status.text = "Connected. WASD to move. Click or [E] to interact."

	if Net.local_id != -1 and not _players.has(Net.local_id):
		_spawn(Net.local_id, Net.spawn_name, Vector2(Net.spawn_x, Net.spawn_y))

	_seed_demo_hotbar()
	_seed_demo_interactables()
	_update_hud(Vector2.ZERO, Vector2.ZERO)
	_update_interact_prompt()


func _process(delta: float) -> void:
	var dir := _read_move_dir()

	_send_accum += delta
	if _send_accum >= INPUT_SEND_INTERVAL or not dir.is_equal_approx(_last_dir):
		_send_accum = 0.0
		_last_dir = dir
		Net.send_input(dir.x, dir.y)

	if Net.local_id != -1 and _players.has(Net.local_id):
		var local_player = _players[Net.local_id]
		_camera.position = local_player.position
		_local_pos = local_player.position

	_update_interact_focus()
	_update_hud(_local_pos, _last_dir)


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("interact"):
		_try_interact_nearest()
		get_viewport().set_input_as_handled()
		return

	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.pressed and mb.button_index == MOUSE_BUTTON_LEFT:
			_try_interact_at(get_global_mouse_position())
			get_viewport().set_input_as_handled()


func _read_move_dir() -> Vector2:
	var dir := Input.get_vector("move_left", "move_right", "move_up", "move_down")
	if dir.length_squared() > 1.0:
		dir = dir.normalized()
	return dir


func _update_interact_focus() -> void:
	var mouse_world := get_global_mouse_position()
	var nearest := _find_nearest_in_range(_local_pos)
	var under_mouse := _find_at_point(mouse_world)

	if _focused != nearest:
		if _focused:
			_focused.set_highlighted(false)
		_focused = nearest
		if _focused and _focused != _hovered:
			_focused.set_highlighted(true)

	if _hovered != under_mouse:
		if _hovered and _hovered != _focused:
			_hovered.set_highlighted(false)
		_hovered = under_mouse
		if _hovered:
			_hovered.set_highlighted(true)
		elif _focused:
			_focused.set_highlighted(true)

	_update_interact_prompt()


func _update_interact_prompt() -> void:
	if _focused:
		_interact_prompt.text = "[E] Interact with %s  (or click)" % _focused.display_name
		_interact_prompt.visible = true
	elif _hovered:
		var in_range := _hovered.is_in_interact_range(_local_pos)
		if in_range:
			_interact_prompt.text = "Click to interact with %s" % _hovered.display_name
		else:
			_interact_prompt.text = "Too far — move closer to %s" % _hovered.display_name
		_interact_prompt.visible = true
	else:
		_interact_prompt.visible = false


func _find_at_point(world_pos: Vector2) -> Interactable:
	var best: Interactable = null
	var best_dist := INF
	for obj in _interactables:
		if not obj.is_near_point(world_pos):
			continue
		var dist := obj.global_position.distance_squared_to(world_pos)
		if dist < best_dist:
			best_dist = dist
			best = obj
	return best


func _find_nearest_in_range(from: Vector2) -> Interactable:
	var best: Interactable = null
	var best_dist := INF
	for obj in _interactables:
		if not obj.is_in_interact_range(from):
			continue
		var dist := obj.global_position.distance_squared_to(from)
		if dist < best_dist:
			best_dist = dist
			best = obj
	return best


func _try_interact_nearest() -> void:
	var target := _find_nearest_in_range(_local_pos)
	if target == null:
		_status.text = "Nothing in range to interact with."
		return
	_perform_interact(target)


func _try_interact_at(world_pos: Vector2) -> void:
	var target := _find_at_point(world_pos)
	if target == null:
		return
	if not target.is_in_interact_range(_local_pos):
		_status.text = "Too far to interact with %s." % target.display_name
		return
	_perform_interact(target)


func _perform_interact(target: Interactable) -> void:
	target.interact()
	_status.text = "Interacted with %s." % target.display_name
	Net.send_interact(target.interact_id)


func _update_hud(pos: Vector2, dir: Vector2) -> void:
	var moving := "moving" if dir.length() > 0.05 else "idle"
	var state := Net.get_ws_state()
	var ws_state: String = _WS_STATES[state] if state < _WS_STATES.size() else "unknown"
	_hud.text = "Pos: (%d, %d)  Input: (%+.1f, %+.1f)  %s  id=%d  players=%d  ws=%s" % [
		int(round(pos.x)), int(round(pos.y)), dir.x, dir.y, moving,
		Net.local_id, _player_count, ws_state
	]


func _on_snapshot(players: Array) -> void:
	_player_count = players.size()
	var seen := {}
	for p in players:
		var id := int(p.get("id", -1))
		if id == -1:
			continue
		seen[id] = true
		var pos := Vector2(float(p.get("x", 0.0)), float(p.get("y", 0.0)))
		if not _players.has(id):
			_spawn(id, String(p.get("name", "")), pos)
		else:
			_players[id].set_target(pos)

	for id in _players.keys():
		if not seen.has(id):
			_players[id].queue_free()
			_players.erase(id)


func _spawn(id: int, display_name: String, pos: Vector2) -> void:
	var node := Node2D.new()
	node.set_script(PlayerScene)
	_players_root.add_child(node)
	node.setup(display_name, pos, id == Net.local_id)
	_players[id] = node


func _spawn_interactable(
	interact_id: String,
	display_name: String,
	pos: Vector2,
	kind: Interactable.Kind,
	tint: Color
) -> void:
	var node := Node2D.new()
	node.set_script(InteractableScene)
	node.position = pos
	node.interact_id = interact_id
	node.display_name = display_name
	node.kind = kind
	node.tint = tint
	_interactables_root.add_child(node)
	_interactables.append(node)


func _seed_demo_interactables() -> void:
	_spawn_interactable("tree_oak_1", "Oak Tree", Vector2(120, -40), Interactable.Kind.TREE, Color(0.25, 0.55, 0.28))
	_spawn_interactable("rock_iron_1", "Iron Rock", Vector2(-110, 60), Interactable.Kind.ROCK, Color(0.45, 0.48, 0.52))
	_spawn_interactable("chest_starter", "Starter Chest", Vector2(40, 100), Interactable.Kind.CHEST, Color(0.62, 0.42, 0.22))
	_spawn_interactable("npc_guide", "Guide", Vector2(-50, -90), Interactable.Kind.NPC, Color(0.72, 0.58, 0.42))


func _on_disconnected() -> void:
	_status.text = "Disconnected from server."


func _seed_demo_hotbar() -> void:
	var demos: Array[Dictionary] = [
		{"id": "strike", "name": "Strike", "kind": "skill", "color": Color(0.85, 0.35, 0.3)},
		{"id": "heal", "name": "Heal", "kind": "spell", "color": Color(0.35, 0.75, 0.45)},
		{"id": "bandage", "name": "Bandage", "kind": "item", "color": Color(0.75, 0.65, 0.35)},
	]
	for i in demos.size():
		_hotbar.set_slot(i, demos[i])


func _on_hotbar_slot(index: int, entry: Dictionary) -> void:
	var key_label: String = Hotbar.KEY_LABELS[index]
	if entry.is_empty():
		_status.text = "Hotbar slot %s is empty." % key_label
	else:
		_status.text = "Used slot %s: %s (%s)" % [
			key_label,
			entry.get("name", "?"),
			entry.get("kind", "?"),
		]
