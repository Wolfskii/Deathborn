class_name HotbarSlot
extends PanelContainer
## One hotbar slot. Shows a key bind, optional icon tint, and entry name.
## Empty slots stay dark until a spell/skill/item is assigned.

const SIZE := Vector2(52, 52)

var slot_index: int = -1

var _key_label: Label
var _icon: ColorRect
var _name_label: Label
var _entry: Dictionary = {}


func _ready() -> void:
	custom_minimum_size = SIZE
	_build_ui()
	_apply_style(false)


func setup(index: int, key_text: String) -> void:
	slot_index = index
	if _key_label:
		_key_label.text = key_text


func set_entry(data: Dictionary) -> void:
	_entry = data
	if _icon == null:
		return
	if data.is_empty():
		_icon.color = Color(0.12, 0.12, 0.14, 1.0)
		_name_label.text = ""
	else:
		_icon.color = data.get("color", Color(0.35, 0.55, 0.85, 1.0))
		_name_label.text = String(data.get("name", ""))


func get_entry() -> Dictionary:
	return _entry


func flash() -> void:
	var tween := create_tween()
	tween.tween_method(_apply_style, false, true, 0.05)
	tween.tween_method(_apply_style, true, false, 0.12)


func _build_ui() -> void:
	var margin := MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 4)
	margin.add_theme_constant_override("margin_right", 4)
	margin.add_theme_constant_override("margin_top", 4)
	margin.add_theme_constant_override("margin_bottom", 4)
	add_child(margin)

	var stack := VBoxContainer.new()
	stack.add_theme_constant_override("separation", 2)
	margin.add_child(stack)

	_icon = ColorRect.new()
	_icon.custom_minimum_size = Vector2(44, 30)
	_icon.color = Color(0.12, 0.12, 0.14, 1.0)
	stack.add_child(_icon)

	_name_label = Label.new()
	_name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_name_label.add_theme_font_size_override("font_size", 9)
	_name_label.text = ""
	stack.add_child(_name_label)

	_key_label = Label.new()
	_key_label.text = "?"
	_key_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_key_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_key_label.add_theme_font_size_override("font_size", 10)
	_key_label.add_theme_color_override("font_color", Color(0.85, 0.85, 0.75))
	_key_label.set_anchors_and_offsets_preset(Control.PRESET_TOP_LEFT)
	_key_label.position = Vector2(4, 2)
	_key_label.size = Vector2(14, 14)
	add_child(_key_label)


func _apply_style(active: bool) -> void:
	var bg := StyleBoxFlat.new()
	bg.bg_color = Color(0.08, 0.08, 0.1, 0.95) if not active else Color(0.22, 0.28, 0.38, 0.98)
	bg.border_width_left = 2
	bg.border_width_top = 2
	bg.border_width_right = 2
	bg.border_width_bottom = 2
	bg.border_color = Color(0.35, 0.38, 0.42) if not active else Color(0.75, 0.85, 1.0)
	bg.corner_radius_top_left = 4
	bg.corner_radius_top_right = 4
	bg.corner_radius_bottom_left = 4
	bg.corner_radius_bottom_right = 4
	add_theme_stylebox_override("panel", bg)
