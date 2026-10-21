class_name TextureQuality
extends RefCounted

# Authored selection and exact converted-image binding: materials.md.
const MODES := ["Original","Enhanced"]
static var selected := ""
static var textures: Dictionary = {}
static var records: Array = []
static var loaded := false

static func get_mode() -> String:
	if selected == "":
		var config := ConfigFile.new()
		config.load("user://settings.cfg")
		selected = str(config.get_value("video","texture_quality","Enhanced"))
		if selected not in MODES: selected = "Enhanced"
	return selected

static func set_mode(value: String, persist: bool = true, path: String = "user://settings.cfg") -> bool:
	if value not in MODES: return false
	selected = value
	if not persist: return true
	var config := ConfigFile.new()
	var status := config.load(path)
	if status not in [OK,ERR_FILE_NOT_FOUND]: return false
	config.set_value("video","texture_quality",value)
	return config.save(path) == OK

static func resolve_path(original: String, exists: Callable = Callable()) -> String:
	if not exists.is_valid(): exists = ResourceLoader.exists
	if not original.begins_with("res://assets/") or ".." in original: return original
	if get_mode() == "Enhanced":
		var suffix := original.trim_prefix("res://assets/")
		var overrides: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://tools/pipeline/texture_overrides.json"))
		if suffix in overrides.original_only: return original
		for directory in ["hd_overrides","hd"]:
			var candidate: String = "res://assets/"+directory+"/"+suffix
			if exists.call(candidate): return candidate
	return original

static func load_texture(original: String) -> Texture2D:
	var path := resolve_path(original)
	if not ResourceLoader.exists(path): return null
	if not textures.has(path):
		var resource := load(path) as Texture2D
		if not resource: return null
		var image := resource.get_image()
		if not image or image.is_empty(): return null
		if image.is_compressed(): image.decompress()
		image.generate_mipmaps()
		textures[path] = ImageTexture.create_from_image(image)
	return textures[path]

static func apply_to_model(node: Node) -> void:
	if not loaded:
		loaded = true
		var path := "res://assets/hd/manifest.json"
		if FileAccess.file_exists(path):
			var manifest: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(path))
			records = manifest.get("textures",[])
	if node is MeshInstance3D and node.mesh:
		for surface in range(node.mesh.get_surface_count()):
			var material := node.mesh.surface_get_material(surface) as StandardMaterial3D
			if not material or not material.albedo_texture: continue
			var image := material.albedo_texture.get_image()
			if image.is_compressed(): image.decompress()
			image.clear_mipmaps()
			image.convert(Image.FORMAT_RGBA8)
			var hash := HashingContext.new()
			hash.start(HashingContext.HASH_SHA256)
			hash.update(image.get_data())
			var digest := hash.finish().hex_encode()
			var instance := material.duplicate() as StandardMaterial3D
			instance.texture_filter = BaseMaterial3D.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS_ANISOTROPIC
			node.set_surface_override_material(surface,instance)
			for record in records:
				if record.get("pixel_sha256","") != digest: continue
				var replacement := load_texture("res://assets/"+str(record.source))
				if replacement:
					instance.albedo_texture = replacement
	for child in node.get_children(): apply_to_model(child)
