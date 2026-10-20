extends Node

signal subtitle_changed(speaker: String, text: String)
signal clip_finished(clip_id: String)

const MANIFEST := "res://assets/audio/dialogue_manifest.json"
var registry: Dictionary = {}
var cinematic_owner: Object
var current_id := ""
var speech := AudioStreamPlayer.new()
var music := AudioStreamPlayer.new()
var sfx := AudioStreamPlayer.new()
var low_pass := AudioEffectLowPassFilter.new()

func _ready() -> void:
	for bus in ["Dialogue", "Music", "SFX"]:
		if AudioServer.get_bus_index(bus) == -1:
			AudioServer.add_bus()
			AudioServer.set_bus_name(AudioServer.bus_count-1, bus)
	AudioServer.add_bus_effect(AudioServer.get_bus_index("SFX"), low_pass)
	for pair in [[speech, "Dialogue"], [music, "Music"], [sfx, "SFX"]]:
		pair[0].bus = pair[1]
		add_child(pair[0])
	speech.finished.connect(_finished)
	if FileAccess.file_exists(MANIFEST):
		var parsed = JSON.parse_string(FileAccess.get_file_as_string(MANIFEST))
		if parsed is Dictionary:
			registry = parsed

func acquire_cinematic(owner: Object) -> bool:
	if is_instance_valid(cinematic_owner) and cinematic_owner != owner:
		return false
	stop()
	cinematic_owner = owner
	return true

func release_cinematic(owner: Object) -> void:
	if cinematic_owner == owner:
		stop()
		cinematic_owner = null

func find_key(stem: String) -> String:
	for key in registry:
		if str(key).to_lower() == stem.to_lower():
			return key
	return ""

func play(clip_id: String, owner: Object = null) -> bool:
	if is_instance_valid(cinematic_owner) and cinematic_owner != owner:
		return false
	var entry: Dictionary = registry.get(clip_id, {})
	if entry.is_empty() or entry.get("category") != "Dialogue":
		return false
	var stream = load(entry.file)
	if not stream is AudioStream:
		return false
	stop()
	current_id = clip_id
	speech.stream = stream
	speech.play()
	AudioServer.set_bus_volume_db(AudioServer.get_bus_index("Music"), -12.0)
	# Keep unverified ASR out of character-attributed UI subtitles.
	if entry.get("text_status") == "verified":
		subtitle_changed.emit(entry.get("speaker", ""), entry.get("text", ""))
	return true

func stop() -> void:
	speech.stop()
	current_id = ""
	subtitle_changed.emit("", "")
	AudioServer.set_bus_volume_db(AudioServer.get_bus_index("Music"), 0.0)

func _finished() -> void:
	var completed := current_id
	stop()
	clip_finished.emit(completed)

func play_sfx(stem: String, position_3d: Variant = null) -> bool:
	var entry: Dictionary = registry.get(find_key(stem), {})
	if entry.is_empty() or entry.get("category") != "SFX":
		return false
	var stream = load(entry.file)
	if not stream is AudioStream:
		return false
	if position_3d is Vector3 and get_tree().current_scene is Node3D:
		var spatial := AudioStreamPlayer3D.new()
		get_tree().current_scene.add_child(spatial)
		spatial.global_position = position_3d
		spatial.bus = "SFX"
		spatial.unit_size = 3.0
		spatial.max_distance = 60.0
		spatial.stream = stream
		spatial.finished.connect(spatial.queue_free)
		spatial.play()
	else:
		sfx.stream = stream
		sfx.play()
	return true

func set_traversal_filter(height: float, speed: float) -> void:
	low_pass.cutoff_hz = lerpf(20000.0, 5000.0, clampf(absf(height)/100.0+speed/120.0, 0.0, 1.0))
