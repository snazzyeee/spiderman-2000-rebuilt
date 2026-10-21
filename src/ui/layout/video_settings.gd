class_name VideoSettings
extends RefCounted

# Authored renderer policy: materials.md, remake:video-options-v1.
static var values: Dictionary = {}
const OPTIONS := {
	"window_mode":["Windowed","Borderless","Fullscreen"],
	"resolution":["1920x1080","1280x720","2560x1440","1920x1200"],
	"vsync":["On","Off"],"frame_cap":["60","30","120","Unlimited"],
	"render_scale":["100%","85%","75%","50%"],
	"anisotropy":["4x","Off","2x","8x","16x"],
	"shadows":["Medium","Off","Low","High"],
	"ssao":["Off","On"],"volumetric_fog":["Off","On"]}

static func forward_plus() -> bool:
	return RenderingServer.get_current_rendering_method() == "forward_plus"
static func choices(key: String) -> Array:
	if key == "aa": return ["Off","FXAA","MSAA 2x","MSAA 4x","TAA"] if forward_plus() else ["Off","FXAA","MSAA 2x","MSAA 4x"]
	if key in ["ssao","volumetric_fog"] and not forward_plus(): return ["Off"]
	return OPTIONS.get(key,[])
static func get_value(key: String) -> String:
	var options := choices(key)
	if options.is_empty(): return ""
	if not values.has(key):
		var config := ConfigFile.new()
		config.load("user://settings.cfg")
		values[key] = str(config.get_value("video",key,options[0]))
	return str(values[key]) if values[key] in options else str(options[0])
static func set_value(key: String,value: String,persist: bool = true,path: String = "user://settings.cfg") -> bool:
	if value not in choices(key): return false
	values[key] = value
	if not persist: return true
	var config := ConfigFile.new()
	var status := config.load(path)
	if status not in [OK,ERR_FILE_NOT_FOUND]: return false
	config.set_value("video",key,value)
	return config.save(path) == OK
static func apply_viewport(viewport: Viewport) -> void:
	var percent := float(get_value("render_scale").trim_suffix("%"))/100.0
	viewport.scaling_3d_mode = Viewport.SCALING_3D_MODE_FSR2 if forward_plus() and percent < 1.0 else Viewport.SCALING_3D_MODE_BILINEAR
	viewport.scaling_3d_scale = percent
	var aa := get_value("aa")
	viewport.screen_space_aa = Viewport.SCREEN_SPACE_AA_FXAA if aa == "FXAA" else Viewport.SCREEN_SPACE_AA_DISABLED
	viewport.use_taa = forward_plus() and aa == "TAA"
	viewport.msaa_3d = Viewport.MSAA_2X if aa == "MSAA 2x" else Viewport.MSAA_4X if aa == "MSAA 4x" else Viewport.MSAA_DISABLED
	viewport.anisotropic_filtering_level = {"Off":0,"2x":1,"4x":2,"8x":3,"16x":4}[get_value("anisotropy")]
static func apply_scene(node: Node) -> void:
	if node is Viewport: apply_viewport(node)
	if node is Light3D and node.has_meta("video_original_shadow"):
		node.shadow_enabled = bool(node.get_meta("video_original_shadow")) and get_value("shadows") != "Off"
	elif node is Light3D:
		node.set_meta("video_original_shadow",node.shadow_enabled)
		node.shadow_enabled = node.shadow_enabled and get_value("shadows") != "Off"
	if node is WorldEnvironment and node.environment:
		node.environment.ssao_enabled = forward_plus() and get_value("ssao") == "On"
		node.environment.volumetric_fog_enabled = forward_plus() and get_value("volumetric_fog") == "On"
	for child in node.get_children(): apply_scene(child)
static func apply(tree: SceneTree, display: bool = true) -> void:
	Engine.max_fps = 0 if get_value("frame_cap") == "Unlimited" else int(get_value("frame_cap"))
	if display and DisplayServer.get_name() != "headless":
		var config := ConfigFile.new()
		config.load("user://settings.cfg")
		var explicit_mode := values.has("window_mode") or config.has_section_key("video","window_mode")
		var explicit_size := values.has("resolution") or config.has_section_key("video","resolution")
		var mode := get_value("window_mode")
		if explicit_mode:
			var native_mode := DisplayServer.WINDOW_MODE_FULLSCREEN if mode == "Fullscreen" else DisplayServer.WINDOW_MODE_WINDOWED
			if DisplayServer.window_get_mode() != native_mode: DisplayServer.window_set_mode(native_mode)
			if DisplayServer.window_get_flag(DisplayServer.WINDOW_FLAG_BORDERLESS) != (mode == "Borderless"):
				DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS,mode == "Borderless")
		if explicit_size and mode != "Fullscreen":
			var resolution := get_value("resolution").split("x")
			var extent := Vector2i(int(resolution[0]),int(resolution[1]))
			if tree.root.size != extent: tree.root.size = extent
		DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_ENABLED if get_value("vsync") == "On" else DisplayServer.VSYNC_DISABLED)
	RenderingServer.directional_shadow_atlas_set_size({"Off":1024,"Low":1024,"Medium":2048,"High":4096}[get_value("shadows")],true)
	apply_scene(tree.root)
