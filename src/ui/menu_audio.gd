class_name MenuAudio
extends RefCounted

# Recovered event bindings and authored length/focus policy: frontend.md.
const IDS := {"move": 0x29, "confirm": 0x1F, "back": 0x23, "reject": 0x1B}
const MAX_SECONDS := 0.6
const MODES := ["Original", "Short", "Off"]
static var selected := ""
var focused_id := 0

static func get_mode() -> String:
	if selected == "":
		var config := ConfigFile.new()
		config.load("user://settings.cfg")
		selected = str(config.get_value("audio", "menu_sounds", "Original"))
		if selected not in MODES: selected = "Original"
	return selected

static func set_mode(value: String, persist: bool = true, path: String = "user://settings.cfg") -> bool:
	if value not in MODES: return false
	selected = value
	if not persist: return true
	var config := ConfigFile.new()
	var status := config.load(path)
	if status not in [OK, ERR_FILE_NOT_FOUND]: return false
	config.set_value("audio", "menu_sounds", value)
	return config.save(path) == OK

static func allows(stream: AudioStream, recovered: bool) -> bool:
	if stream == null or get_mode() == "Off": return false
	var seconds := stream.get_length()
	if not is_finite(seconds) or seconds <= 0.0: return false
	if (get_mode() == "Short" or not recovered) and seconds > MAX_SECONDS: return false
	if stream is AudioStreamWAV and stream.loop_mode != AudioStreamWAV.LOOP_DISABLED: return false
	if stream is AudioStreamOggVorbis and stream.loop: return false
	if stream is AudioStreamMP3 and stream.loop: return false
	return true

func reset_focus() -> void:
	focused_id = 0

func focus(control: Control, service: Object) -> bool:
	if not is_instance_valid(control): return false
	var next_id := control.get_instance_id()
	if next_id == focused_id: return false
	var initial := focused_id == 0
	focused_id = next_id
	return false if initial else request("move", service)

func request(event: String, service: Object) -> bool:
	service.sfx.stop()
	if not IDS.has(event): return false
	var stream: AudioStream = service.sfx_stream_for_id(IDS[event])
	if not allows(stream, true): return false
	return service.play_sfx_id(IDS[event])
