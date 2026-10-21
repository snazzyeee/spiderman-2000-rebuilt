extends SceneTree
var checks: Array = []
func _initialize() -> void: call_deferred("run")
func check(value: bool, label: String) -> void:
	checks.append({"passed":value,"label":label})
	print("LAYOUT_TEST ", "PASS " if value else "FAIL ", label)
func run() -> void:
	var data := FrontendSettings.document()
	check(data.schema_version == 1 and data.screens.size() >= 17, "all proposal screens represented")
	for screen in data.screens:
		for style in ["classic", "enhanced"]:
			var layout: Dictionary = screen.styles[style]
			var valid := true
			for e in layout.elements:
				var r: Array = e.rect
				valid = valid and r[0] >= 0 and r[1] >= 0 and r[0]+r[2] <= layout.canvas[0]+.01 and r[1]+r[3] <= layout.canvas[1]+.01 and not e.evidence.is_empty()
			check(valid, screen.id+" "+style+" bounds and evidence")
	var saved := FrontendSettings.values.duplicate()
	for key in ["style", "reduced_motion", "side_fill", "cursor_policy", "remember_cheats", "attract_media", "training_scope"]:
		var options := FrontendSettings.choices(key)
		check(FrontendSettings.set_value(key, options.back(), true, "user://frontend-test.cfg"), key+" switch persists")
		var config := ConfigFile.new()
		config.load("user://frontend-test.cfg")
		check(config.get_value("frontend",key,"") == options.back(), key+" reloads exactly")
	check(not FrontendSettings.set_value("style", "invalid", false), "invalid style rejected")
	FrontendSettings.values = saved
	DirAccess.remove_absolute("user://frontend-test.cfg")
	var failed := checks.filter(func(row): return not row.passed)
	var report := {"passed":failed.is_empty(),"checks":checks,"renderer":RenderingServer.get_current_rendering_method(),"display_driver":DisplayServer.get_name()}
	var file := FileAccess.open("res://staging/validation/layout-tests.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report,"  ")); file.close()
	quit(0 if failed.is_empty() else 1)
