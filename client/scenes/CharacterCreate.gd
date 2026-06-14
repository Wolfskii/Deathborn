extends Control
## Character creation. We are already connected to the world socket; we send a
## create_character request and wait for the welcome that follows.

@onready var _name: LineEdit = $Center/VBox/NameEdit
@onready var _create_btn: Button = $Center/VBox/CreateButton
@onready var _status: Label = $Center/VBox/Status


func _ready() -> void:
	_create_btn.pressed.connect(_on_create)
	Net.welcome.connect(_go_world, CONNECT_ONE_SHOT)
	Net.server_error.connect(_on_error)


func _on_create() -> void:
	_status.text = "Creating character..."
	_create_btn.disabled = true
	Net.create_character(_name.text)


func _on_error(message: String) -> void:
	_status.text = "Error: " + message
	_create_btn.disabled = false


func _go_world(_id: int, _x: float, _y: float, _char_name: String) -> void:
	get_tree().change_scene_to_file("res://scenes/World.tscn")
