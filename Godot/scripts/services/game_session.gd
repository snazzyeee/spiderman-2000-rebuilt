extends Node

var training := false
var resume_checkpoint := -1
var save_path := "user://mission_checkpoint.json"

func _ready() -> void:
	for action in ["move_left", "move_right", "move_forward", "move_back", "jump", "swing", "zip", "crawl", "attack", "web", "interact", "pause", "look_left", "look_right", "look_up", "look_down"]:
		if not InputMap.has_action(action):
			InputMap.add_action(action, 0.18)
	var keys := {"move_left": KEY_A, "move_right": KEY_D, "move_forward": KEY_W, "move_back": KEY_S, "jump": KEY_SPACE, "zip": KEY_Q, "crawl": KEY_C, "attack": KEY_F, "web": KEY_R, "interact": KEY_E}
	for action in keys:
		var event := InputEventKey.new()
		event.physical_keycode = keys[action]
		InputMap.action_add_event(action, event)
	for binding in [["move_left", JOY_AXIS_LEFT_X, -1.0], ["move_right", JOY_AXIS_LEFT_X, 1.0], ["move_forward", JOY_AXIS_LEFT_Y, -1.0], ["move_back", JOY_AXIS_LEFT_Y, 1.0], ["look_left", JOY_AXIS_RIGHT_X, -1.0], ["look_right", JOY_AXIS_RIGHT_X, 1.0], ["look_up", JOY_AXIS_RIGHT_Y, -1.0], ["look_down", JOY_AXIS_RIGHT_Y, 1.0], ["swing", JOY_AXIS_TRIGGER_RIGHT, 1.0]]:
		var event := InputEventJoypadMotion.new()
		event.axis = binding[1]
		event.axis_value = binding[2]
		InputMap.action_add_event(binding[0], event)
	for binding in [["jump", JOY_BUTTON_A], ["zip", JOY_BUTTON_RIGHT_SHOULDER], ["crawl", JOY_BUTTON_LEFT_SHOULDER], ["attack", JOY_BUTTON_X], ["web", JOY_BUTTON_Y], ["interact", JOY_BUTTON_B]]:
		var event := InputEventJoypadButton.new()
		event.button_index = binding[1]
		InputMap.action_add_event(binding[0], event)
	var mouse := InputEventMouseButton.new()
	mouse.button_index = MOUSE_BUTTON_RIGHT
	InputMap.action_add_event("swing", mouse)
	var pause_button := InputEventJoypadButton.new()
	pause_button.button_index = JOY_BUTTON_START
	InputMap.action_add_event("pause", pause_button)
	var pause_key := InputEventKey.new()
	pause_key.physical_keycode = KEY_ESCAPE
	InputMap.action_add_event("pause", pause_key)
	if "--revamp-smoke" in OS.get_cmdline_user_args():
		save_path = "user://automated_checkpoint.json"
		call_deferred("_start_smoke")

func _start_smoke() -> void:
	var script = load("res://scripts/diagnostics/smoke_runner.gd")
	if not script or not script.can_instantiate():
		push_error("Smoke runner failed compilation")
		get_tree().quit(1)
		return
	var runner = script.new()
	get_tree().root.add_child(runner)

func save_checkpoint(index: int) -> void:
	var file := FileAccess.open(save_path+".tmp", FileAccess.WRITE)
	if file == null:
		push_error("Cannot write checkpoint")
		return
	file.store_string(JSON.stringify({"version": 1, "checkpoint": index}))
	file.close()
	var error := DirAccess.rename_absolute(save_path+".tmp", save_path)
	if error != OK:
		push_error("Cannot replace checkpoint: %s" % error)

func load_checkpoint() -> int:
	if not FileAccess.file_exists(save_path):
		return -1
	var data = JSON.parse_string(FileAccess.get_file_as_string(save_path))
	if not data is Dictionary or data.get("version") != 1:
		return -1
	return clampi(int(data.get("checkpoint", -1)), -1, 2)
