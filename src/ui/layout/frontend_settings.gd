class_name FrontendSettings
extends RefCounted

# Authored preference policy: frontend.md, remake:frontend-layout-v1.
const PATH := "res://src/ui/layout/screens.json"
static var values: Dictionary = {}

static func document() -> Dictionary:
	return JSON.parse_string(FileAccess.get_file_as_string(PATH))

static func choices(key: String) -> Array:
	if key == "style": return ["classic", "enhanced"]
	if key == "reduced_motion": return ["Off", "On"]
	return document().preferences.get(key, [])

static func get_value(key: String) -> String:
	var options := choices(key)
	if options.is_empty(): return ""
	if not values.has(key):
		var config := ConfigFile.new()
		config.load("user://settings.cfg")
		values[key] = str(config.get_value("frontend", key, "enhanced" if key == "style" else options[0]))
		if values[key] not in options: values[key] = options[0]
	return str(values[key])

static func set_value(key: String, value: String, persist: bool = true, path: String = "user://settings.cfg") -> bool:
	if value not in choices(key): return false
	values[key] = value
	if not persist: return true
	var config := ConfigFile.new()
	var status := config.load(path)
	if status not in [OK, ERR_FILE_NOT_FOUND]: return false
	config.set_value("frontend", key, value)
	return config.save(path) == OK
