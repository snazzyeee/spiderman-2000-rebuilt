class_name OriginalFonts
extends RefCounted

# Recovered metrics/mapping and authored role/variant policy: fonts.md.
const BASE := "res://assets/ui/fonts"
const ROLES := {"title":"sp_fnt00", "menu":"sp_fnt00", "hud":"sp_fnt00", "caption":"sp_fnt00", "subtitle":"sp_fnt02"}
const VARIANTS := ["Faithful", "Enhanced"]
static var selected := ""
static var bindings: Array[Dictionary] = []
static var fonts := {}

static func get_variant() -> String:
	if selected == "":
		var config := ConfigFile.new()
		config.load("user://settings.cfg")
		selected = str(config.get_value("video", "font_variant", "Enhanced"))
		if selected not in VARIANTS: selected = "Enhanced"
	return selected

static func font_for(role: String, directory: String = BASE) -> Font:
	var stem: String = ROLES.get(role, "sp_fnt00")
	var suffix := "_faithful.fnt" if get_variant() == "Faithful" else "_enhanced.ttf"
	var path := directory+"/"+stem+suffix
	if not ResourceLoader.exists(path): return null
	if not fonts.has(path):
		var font := load(path) as Font
		if font:
			font.fallbacks = [ThemeDB.fallback_font]
		fonts[path] = font
	return fonts[path]

static func theme_for(role: String, directory: String = BASE) -> Theme:
	var result := (load("res://src/ui/theme/default_theme.tres") as Theme).duplicate() as Theme
	var font := font_for(role, directory)
	if font: result.default_font = font
	return result

static func bind(control: Control, role: String) -> void:
	bindings = bindings.filter(func(binding): return is_instance_valid(binding.control.get_ref()))
	if not is_instance_valid(control): return
	for binding in bindings:
		if binding.control.get_ref() == control:
			binding.role = role
			_apply(control, role)
			return
	bindings.append({"control":weakref(control), "role":role})
	_apply(control, role)

static func _apply(control: Control, role: String) -> void:
	control.theme = theme_for(role)
	control.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST if get_variant() == "Faithful" else CanvasItem.TEXTURE_FILTER_LINEAR

static func set_variant(value: String, persist: bool = true, settings_path: String = "user://settings.cfg") -> bool:
	if value not in VARIANTS: return false
	var persisted := true
	if persist:
		var config := ConfigFile.new()
		var status := config.load(settings_path)
		if status in [OK, ERR_FILE_NOT_FOUND]:
			config.set_value("video", "font_variant", value)
			status = config.save(settings_path)
		persisted = status == OK
		if not persisted: push_warning("Font setting could not be saved; applying for this session")
	selected = value
	var live: Array[Dictionary] = []
	for binding in bindings:
		var control = binding.control.get_ref()
		if is_instance_valid(control):
			_apply(control, binding.role)
			live.append(binding)
	bindings = live
	return persisted

static func uses_original(control: Control) -> bool:
	return control.theme != null and control.theme.default_font != null and control.theme.default_font.resource_path.begins_with(BASE+"/")
