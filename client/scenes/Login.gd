extends Control
## Login / register screen. On successful auth we open the world socket and wait
## for the server to tell us whether we already have a character (-> World) or
## need to make one (-> CharacterCreate).

@onready var _email: LineEdit = $Center/VBox/EmailEdit
@onready var _password: LineEdit = $Center/VBox/PasswordEdit
@onready var _login_btn: Button = $Center/VBox/Buttons/LoginButton
@onready var _register_btn: Button = $Center/VBox/Buttons/RegisterButton
@onready var _status: Label = $Center/VBox/Status


func _ready() -> void:
	_login_btn.pressed.connect(_on_login)
	_register_btn.pressed.connect(_on_register)
	Net.auth_result.connect(_on_auth_result)


func _on_login() -> void:
	_set_busy("Logging in...")
	Net.login(_email.text, _password.text)


func _on_register() -> void:
	_set_busy("Registering...")
	Net.register(_email.text, _password.text)


func _on_auth_result(success: bool, info: String) -> void:
	if not success:
		_status.text = "Error: " + info
		_set_enabled(true)
		return
	_status.text = "Entering world..."
	Net.welcome.connect(_go_world, CONNECT_ONE_SHOT)
	Net.need_character.connect(_go_character_create, CONNECT_ONE_SHOT)
	Net.connect_world()


func _go_world(_id: int, _x: float, _y: float, _char_name: String) -> void:
	if Net.need_character.is_connected(_go_character_create):
		Net.need_character.disconnect(_go_character_create)
	get_tree().change_scene_to_file("res://scenes/World.tscn")


func _go_character_create() -> void:
	if Net.welcome.is_connected(_go_world):
		Net.welcome.disconnect(_go_world)
	get_tree().change_scene_to_file("res://scenes/CharacterCreate.tscn")


func _set_busy(msg: String) -> void:
	_status.text = msg
	_set_enabled(false)


func _set_enabled(enabled: bool) -> void:
	_login_btn.disabled = not enabled
	_register_btn.disabled = not enabled
