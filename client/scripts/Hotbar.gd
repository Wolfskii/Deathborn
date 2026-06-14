class_name Hotbar
extends Control
## Bottom hotbar with 10 slots bound to keys 1-9 and 0.
## Assign entries with set_slot(); listen to slot_activated for use/cast logic.

signal slot_activated(index: int, entry: Dictionary)

const SLOT_COUNT := 10
const KEY_LABELS: PackedStringArray = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"]
const HOTBAR_ACTIONS: PackedStringArray = [
	"hotbar_1", "hotbar_2", "hotbar_3", "hotbar_4", "hotbar_5",
	"hotbar_6", "hotbar_7", "hotbar_8", "hotbar_9", "hotbar_0",
]

const HotbarSlotScene := preload("res://scripts/HotbarSlot.gd")

@onready var _slots_root: HBoxContainer = $Panel/MarginContainer/HBoxContainer
@onready var _panel: PanelContainer = $Panel

var _slots: Array[HotbarSlot] = []


func _ready() -> void:
	_style_panel()
	for i in SLOT_COUNT:
		var slot := PanelContainer.new()
		slot.set_script(HotbarSlotScene)
		_slots_root.add_child(slot)
		slot.setup(i, KEY_LABELS[i])
		_slots.append(slot)


func _unhandled_input(event: InputEvent) -> void:
	if not event.is_pressed() or event.is_echo():
		return
	for i in SLOT_COUNT:
		if event.is_action_pressed(HOTBAR_ACTIONS[i]):
			_activate_slot(i)
			get_viewport().set_input_as_handled()
			return


func set_slot(index: int, entry: Dictionary) -> void:
	if index < 0 or index >= _slots.size():
		return
	_slots[index].set_entry(entry)


func get_slot(index: int) -> Dictionary:
	if index < 0 or index >= _slots.size():
		return {}
	return _slots[index].get_entry()


func clear_slot(index: int) -> void:
	set_slot(index, {})


func _activate_slot(index: int) -> void:
	var entry := get_slot(index)
	_slots[index].flash()
	slot_activated.emit(index, entry)


func _style_panel() -> void:
	var bg := StyleBoxFlat.new()
	bg.bg_color = Color(0.05, 0.06, 0.08, 0.88)
	bg.border_width_left = 1
	bg.border_width_top = 1
	bg.border_width_right = 1
	bg.border_width_bottom = 1
	bg.border_color = Color(0.25, 0.28, 0.32)
	bg.corner_radius_top_left = 6
	bg.corner_radius_top_right = 6
	bg.corner_radius_bottom_left = 6
	bg.corner_radius_bottom_right = 6
	_panel.add_theme_stylebox_override("panel", bg)
