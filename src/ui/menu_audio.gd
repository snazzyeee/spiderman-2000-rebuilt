class_name MenuAudio
extends RefCounted

# Recovered event bindings and authored length/focus policy: frontend.md.
const IDS := {"move": 0x29, "confirm": 0x1F, "back": 0x23}
const MAX_SECONDS := 0.6
var focused_id := 0

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
	if stream == null: return false
	var seconds := stream.get_length()
	if not is_finite(seconds) or seconds <= 0.0 or seconds > MAX_SECONDS: return false
	if stream is AudioStreamWAV and stream.loop_mode != AudioStreamWAV.LOOP_DISABLED: return false
	if stream is AudioStreamOggVorbis and stream.loop: return false
	if stream is AudioStreamMP3 and stream.loop: return false
	return service.play_sfx_id(IDS[event])
