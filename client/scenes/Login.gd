extends Control
## Login / register screen. On successful auth we open the world socket and wait
## for the server to tell us whether we already have a character (-> World) or
## need to make one (-> CharacterCreate).

@onready var _email: LineEdit = $Center/VBox/EmailEdit
@onready var _password: LineEdit = $Center/VBox/PasswordEdit
@onready var _login_btn: Button = $Center/VBox/Buttons/LoginButton
@onready var _register_btn: Button = $Center/VBox/Buttons/RegisterButton
@onready var _dev_btn: Button = $Center/VBox/DevQuickLogin
@onready var _status: Label = $Center/VBox/Status

var _saved_dev_email: String = ""


func _ready() -> void:
	_login_btn.pressed.connect(_on_login)
	_register_btn.pressed.connect(_on_register)
	_dev_btn.pressed.connect(_on_dev_quick_login)
	Net.auth_result.connect(_on_auth_result)
	_setup_dev_quick_login()


func _setup_dev_quick_login() -> void:
	if not DevCredentials.is_available():
		_dev_btn.visible = false
		return

	var saved := DevCredentials.load_saved()
	if saved.is_empty():
		_dev_btn.visible = false
		return

	_saved_dev_email = String(saved["email"])
	_email.text = _saved_dev_email
	_password.text = String(saved["password"])
	_dev_btn.text = "Dev: Continue as %s" % _saved_dev_email
	_dev_btn.visible = true


func _on_login() -> void:
	_set_busy("Logging in...")
	Net.login(_email.text, _password.text)


func _on_register() -> void:
	_set_busy("Registering...")
	Net.register(_email.text, _password.text)


func _on_dev_quick_login() -> void:
	var saved := DevCredentials.load_saved()
	if saved.is_empty():
		_status.text = "No saved dev login."
		return
	_email.text = String(saved["email"])
	_password.text = String(saved["password"])
	_on_login()


func _on_auth_result(success: bool, info: String) -> void:
	if not success:
		_status.text = "Error: " + info
		_set_enabled(true)
		return

	if DevCredentials.is_available():
		DevCredentials.save(_email.text, _password.text)
		_setup_dev_quick_login()

	_status.text = "Entering world..."
	Net.welcome.connect(_go_world, CONNECT_ONE_SHOT)
	Net.need_character.connect(_go_character_create, CONNECT_ONE_SHOT)
	Net.disconnected.connect(_on_world_connect_failed, CONNECT_ONE_SHOT)
	Net.connect_world()


func _on_world_connect_failed() -> void:
	_clear_world_listeners()
	_status.text = "Error: could not connect to world server (check server logs)"
	_set_enabled(true)


func _clear_world_listeners() -> void:
	if Net.welcome.is_connected(_go_world):
		Net.welcome.disconnect(_go_world)
	if Net.need_character.is_connected(_go_character_create):
		Net.need_character.disconnect(_go_character_create)
	if Net.disconnected.is_connected(_on_world_connect_failed):
		Net.disconnected.disconnect(_on_world_connect_failed)


func _go_world(_id: int, _x: float, _y: float, _char_name: String) -> void:
	_clear_world_listeners()
	get_tree().change_scene_to_file("res://scenes/World.tscn")


func _go_character_create() -> void:
	_clear_world_listeners()
	get_tree().change_scene_to_file("res://scenes/CharacterCreate.tscn")


func _set_busy(msg: String) -> void:
	_status.text = msg
	_set_enabled(false)


func _set_enabled(enabled: bool) -> void:
	_login_btn.disabled = not enabled
	_register_btn.disabled = not enabled
	if _dev_btn.visible:
		_dev_btn.disabled = not enabled
