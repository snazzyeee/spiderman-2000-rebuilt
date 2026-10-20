extends SceneTree

var checks: Array = []
var skips: Array[String] = []

class PlayerProbe extends RefCounted:
	var active := false
	var stops := 0
	func stop() -> void:
		active = false
		stops += 1

class ServiceProbe extends RefCounted:
	var sfx := PlayerProbe.new()
	var stream: AudioStream
	var requested: Array[int] = []
	var overlaps := 0
	func sfx_stream_for_id(_id: int) -> AudioStream:
		return stream
	func play_sfx_id(id: int) -> bool:
		if sfx.active: overlaps += 1
		sfx.active = true
		requested.append(id)
		return true

func _initialize() -> void:
	call_deferred("run")

func check(condition: bool, label: String) -> void:
	checks.append({"label": label, "passed": condition})
	print("MENU_AUDIO_TEST ", "PASS " if condition else "FAIL ", label)

func fixture(frames: int) -> AudioStreamWAV:
	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_8_BITS
	stream.mix_rate = 1000
	var samples := PackedByteArray()
	samples.resize(frames)
	stream.data = samples
	return stream

func run() -> void:
	var audio := MenuAudio.new()
	var service := ServiceProbe.new()
	service.stream = fixture(600)
	check(audio.request("move", service), "exact duration boundary accepted")
	check(service.requested == [0x29], "move uses recovered global id")
	check(audio.request("confirm", service) and service.requested.back() == 0x1F, "confirm uses recovered id")
	check(audio.request("back", service) and service.requested.back() == 0x23, "back uses distinct recovered id")
	check(service.overlaps == 0 and service.sfx.stops == 3, "repeated requests stop previous playback before starting")
	service.stream = fixture(601)
	check(not audio.request("confirm", service) and not service.sfx.active, "overlong confirm rejected and previous stopped")
	check(not audio.request("back", service), "overlong back rejected")
	service.stream = fixture(0)
	check(not audio.request("move", service), "zero duration rejected")
	service.stream = null
	check(not audio.request("confirm", service), "missing sound stays silent")
	service.stream = fixture(300)
	service.stream.loop_mode = AudioStreamWAV.LOOP_FORWARD
	check(not audio.request("move", service), "looping stream rejected")
	service.stream = fixture(300)
	check(not audio.request("unknown", service), "unknown event stays silent")
	var a := Button.new()
	var b := Button.new()
	check(not audio.focus(a, service), "initial focus silent")
	check(not audio.focus(a, service), "unchanged hover focus silent")
	check(audio.focus(b, service) and service.requested.back() == 0x29, "changed hover focus plays move")
	var count := service.requested.size()
	check(not audio.focus(b, service) and service.requested.size() == count, "repeated hover never restarts move sound")
	audio.reset_focus()
	check(not audio.focus(a, service), "page entry resets silent initial focus")
	check(not audio.focus(null, service), "invalid focus ignored")
	a.free()
	b.free()
	var dialogue := root.get_node("Dialogue")
	if not dialogue.registry.is_empty():
		for event in ["move", "confirm", "back"]:
			var id: int = MenuAudio.IDS[event]
			var key: String = dialogue.resolve_sfx_id(id)
			var stream: AudioStream = dialogue.sfx_stream_for_id(id)
			check(key != "" and stream != null, "private "+event+" resolves recovered ledger id")
			var played := audio.request(event, dialogue)
			check(played == (stream != null and stream.get_length() > 0.0 and stream.get_length() <= MenuAudio.MAX_SECONDS), "private "+event+" honors duration limit")
		dialogue.sfx.stop()
	else:
		skips.append("Private audio absent; native menu identity/duration checks skipped")
	var failures := checks.filter(func(row): return not row.passed)
	var report := {"passed": failures.is_empty(), "checks": checks, "failures": failures, "skips": skips,
		"renderer": RenderingServer.get_current_rendering_method(), "display_driver": DisplayServer.get_name()}
	DirAccess.make_dir_recursive_absolute("res://staging/validation")
	var file := FileAccess.open("res://staging/validation/menu-audio-tests.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "  "))
	file.close()
	print("MENU_AUDIO_TESTS ", JSON.stringify(report))
	quit(0 if failures.is_empty() else 1)
