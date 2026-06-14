extends Node2D
## The world scene. Renders all players from server snapshots, sends local input
## each frame, and follows the local player with the camera.

const PlayerScene := preload("res://scripts/Player.gd")
const INPUT_SEND_INTERVAL := 0.05 # seconds (~20Hz, matches server tick)

@onready var _camera: Camera2D = $Camera2D
@onready var _players_root: Node2D = $Players
@onready var _status: Label = $UI/Status
@onready var _hud: Label = $UI/Hud

var _players := {} # id (int) -> Player node
var _send_accum := 0.0
var _last_dir := Vector2.ZERO
var _local_pos := Vector2.ZERO


func _ready() -> void:
	Net.snapshot.connect(_on_snapshot)
	Net.disconnected.connect(_on_disconnected)
	_status.text = "Connected. Move with WASD / arrow keys."
	_update_hud(Vector2.ZERO, Vector2.ZERO)


func _process(delta: float) -> void:
	var dir := Vector2(
		Input.get_axis("ui_left", "ui_right"),
		Input.get_axis("ui_up", "ui_down")
	)

	_send_accum += delta
	# Send when due, or immediately when the input direction changes.
	if _send_accum >= INPUT_SEND_INTERVAL or not dir.is_equal_approx(_last_dir):
		_send_accum = 0.0
		_last_dir = dir
		Net.send_input(dir.x, dir.y)

	if Net.local_id != -1 and _players.has(Net.local_id):
		var local_player = _players[Net.local_id]
		_camera.position = local_player.position
		_local_pos = local_player.position
		_update_hud(_local_pos, _last_dir)


func _update_hud(pos: Vector2, dir: Vector2) -> void:
	var moving := "moving" if dir.length() > 0.05 else "idle"
	_hud.text = "Position: (%d, %d)   Input: (%+.1f, %+.1f)   %s" % [
		int(round(pos.x)), int(round(pos.y)), dir.x, dir.y, moving
	]


func _on_snapshot(players: Array) -> void:
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

	# Despawn players no longer present.
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


func _on_disconnected() -> void:
	_status.text = "Disconnected from server."
