extends Node

signal subtitle_changed(speaker: String, text: String)
signal clip_finished(clip_id: String)
signal original_sfx_requested(id: int)
signal original_xa_requested(bank: int, track: int)

const MANIFEST := "res://assets/audio/dialogue/dialogue_manifest.json"
var registry: Dictionary = {}
var cinematic_owner: Object
var current_id := ""
var speech := AudioStreamPlayer.new()
var music := AudioStreamPlayer.new()
var sfx := AudioStreamPlayer.new()
var low_pass := AudioEffectLowPassFilter.new()
var music_volume_before_speech := 0.0
var ducked := false

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
	if "--traversal-tests" not in OS.get_cmdline_user_args() and FileAccess.file_exists(MANIFEST):
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

# Recovered numeric identities are separate from unresolved filename bindings.
# The behavior ledger forbids guessing the identity of a converted clip.
func play_original_sfx(id: int, at: Vector3) -> bool:
	original_sfx_requested.emit(id)
	for key in registry:
		if registry[key].get("native_sfx_id",-1) == id:
			return play_sfx(key,at)
	return false

func play_original_xa(bank: int, track: int) -> bool:
	original_xa_requested.emit(bank,track)
	for key in registry:
		var entry: Dictionary = registry[key]
		if entry.get("native_bank",-1) == bank and entry.get("native_track",-1) == track:
			return play(key)
	return false

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
	var music_bus := AudioServer.get_bus_index("Music")
	music_volume_before_speech = AudioServer.get_bus_volume_db(music_bus)
	ducked = true
	AudioServer.set_bus_volume_db(music_bus, music_volume_before_speech-12.0)
	# Keep unverified ASR out of character-attributed UI subtitles.
	if entry.get("text_status") == "verified":
		subtitle_changed.emit(entry.get("speaker", ""), entry.get("text", ""))
	return true

func stop() -> void:
	speech.stop()
	current_id = ""
	subtitle_changed.emit("", "")
	if ducked:
		AudioServer.set_bus_volume_db(AudioServer.get_bus_index("Music"), music_volume_before_speech)
		ducked = false

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
