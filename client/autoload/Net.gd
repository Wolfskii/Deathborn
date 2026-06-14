extends Node
## Network singleton for DEATHBORN.
##
## Handles HTTP auth (register/login) and the authoritative WebSocket world
## connection. The client only ever sends input; the server decides everything
## else. As an autoload, this persists across scene changes so the login flow
## and the world scene share one connection.

const HTTP_BASE := "http://127.0.0.1:8080"
const WS_BASE := "ws://127.0.0.1:8080/ws"

## Emitted after a register/login HTTP call. On success `info` is the JWT, on
## failure it is a human-readable error message.
signal auth_result(success: bool, info: String)

## The server accepted us and told us which entity is ours.
signal welcome(character_id: int, x: float, y: float, char_name: String)

## We are connected but have no living character; show character creation.
signal need_character()

## A server->client error (e.g. invalid character name).
signal server_error(message: String)

## Authoritative world state for a tick. `players` is an Array of Dictionaries
## with keys: id, name, x, y.
signal snapshot(players: Array)

## The world socket closed.
signal disconnected()

var local_id: int = -1
var spawn_name: String = ""
var spawn_x: float = 0.0
var spawn_y: float = 0.0

var _token: String = ""
var _ws := WebSocketPeer.new()
var _ws_active := false
var _prev_state := WebSocketPeer.STATE_CLOSED


func register(email: String, password: String) -> void:
	_auth_request("/register", email, password)


func login(email: String, password: String) -> void:
	_auth_request("/login", email, password)


func _auth_request(path: String, email: String, password: String) -> void:
	var http := HTTPRequest.new()
	add_child(http)
	http.request_completed.connect(
		func(_result: int, code: int, _headers: PackedStringArray, body: PackedByteArray) -> void:
			http.queue_free()
			var text := body.get_string_from_utf8()
			var parsed: Variant = JSON.parse_string(text)
			if code == 200 and parsed is Dictionary and parsed.has("token"):
				_token = String(parsed["token"])
				auth_result.emit(true, _token)
			else:
				var msg := "request failed"
				if parsed is Dictionary and parsed.has("error"):
					msg = String(parsed["error"])
				elif code == 0:
					msg = "could not reach server"
				auth_result.emit(false, msg)
	)
	var payload := JSON.stringify({"email": email, "password": password})
	var headers := PackedStringArray(["Content-Type: application/json"])
	var err := http.request(HTTP_BASE + path, headers, HTTPClient.METHOD_POST, payload)
	if err != OK:
		http.queue_free()
		auth_result.emit(false, "could not start request")


func connect_world() -> void:
	if _token == "":
		auth_result.emit(false, "not authenticated")
		return
	local_id = -1
	_reset_ws()
	var url := WS_BASE + "?token=" + _token.uri_encode()
	var err := _ws.connect_to_url(url)
	if err == OK:
		_ws_active = true
		_prev_state = WebSocketPeer.STATE_CONNECTING
	else:
		disconnected.emit()


func _reset_ws() -> void:
	if _ws.get_ready_state() != WebSocketPeer.STATE_CLOSED:
		_ws.close()
	_ws = WebSocketPeer.new()
	_ws_active = false
	_prev_state = WebSocketPeer.STATE_CLOSED


func get_ws_state() -> int:
	return _ws.get_ready_state()


func send_input(dir_x: float, dir_y: float) -> void:
	if local_id == -1:
		return
	_send("input", {"dirX": dir_x, "dirY": dir_y})


func create_character(char_name: String) -> void:
	_send("create_character", {"name": char_name})


func _send(type: String, data: Dictionary) -> void:
	if _ws.get_ready_state() != WebSocketPeer.STATE_OPEN:
		return
	_ws.send_text(JSON.stringify({"type": type, "data": data}))


func _process(_delta: float) -> void:
	if not _ws_active:
		return

	_ws.poll()
	var state := _ws.get_ready_state()

	while state == WebSocketPeer.STATE_OPEN and _ws.get_available_packet_count() > 0:
		_handle_message(_ws.get_packet().get_string_from_utf8())

	if state == WebSocketPeer.STATE_CLOSED and _prev_state != WebSocketPeer.STATE_CLOSED:
		_ws_active = false
		disconnected.emit()

	_prev_state = state


func _handle_message(text: String) -> void:
	var env: Variant = JSON.parse_string(text)
	if not (env is Dictionary) or not env.has("type"):
		return
	var data := _parse_data(env.get("data"))
	match String(env["type"]):
		"welcome":
			local_id = int(data.get("characterId", -1))
			spawn_x = float(data.get("x", 0.0))
			spawn_y = float(data.get("y", 0.0))
			spawn_name = String(data.get("name", ""))
			welcome.emit(local_id, spawn_x, spawn_y, spawn_name)
		"need_character":
			need_character.emit()
		"snapshot":
			var players: Array = data.get("players", [])
			snapshot.emit(players)
		"error":
			server_error.emit(String(data.get("message", "error")))


func _parse_data(raw: Variant) -> Dictionary:
	if raw is Dictionary:
		return raw
	if raw is String and not raw.is_empty():
		var parsed: Variant = JSON.parse_string(raw)
		if parsed is Dictionary:
			return parsed
	return {}
