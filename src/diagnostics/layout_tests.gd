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
	var saved_video := VideoSettings.values.duplicate()
	for key in VideoSettings.OPTIONS.keys()+["aa"]:
		var options := VideoSettings.choices(key)
		check(VideoSettings.set_value(key,options.back(),true,"user://video-test.cfg"),key+" video setting persists")
		var config := ConfigFile.new()
		config.load("user://video-test.cfg")
		check(config.get_value("video",key,"") == options.back(),key+" video setting reloads")
	check(not VideoSettings.set_value("resolution","invalid",false),"invalid display choice rejected")
	VideoSettings.apply_viewport(root)
	check(root.scaling_3d_scale == .5,"render scale applies live")
	check(root.scaling_3d_mode == (Viewport.SCALING_3D_MODE_FSR2 if VideoSettings.forward_plus() else Viewport.SCALING_3D_MODE_BILINEAR),"FSR2 limited to Forward+")
	check(root.anisotropic_filtering_level == 4,"anisotropy applies live")
	check(VideoSettings.forward_plus() or VideoSettings.choices("ssao") == ["Off"],"Mobile excludes SSAO")
	VideoSettings.values = saved_video
	VideoSettings.apply_viewport(root)
	DirAccess.remove_absolute("user://video-test.cfg")
	var saved_texture := TextureQuality.get_mode()
	TextureQuality.set_mode("Enhanced",false)
	var exists := func(path: String): return path.begins_with("res://assets/hd")
	check(TextureQuality.resolve_path("res://assets/synthetic.png",exists) == "res://assets/hd_overrides/synthetic.png","curated texture overrides have priority")
	TextureQuality.set_mode("Original",false)
	check(TextureQuality.resolve_path("res://assets/synthetic.png",exists) == "res://assets/synthetic.png","Original bypasses all enhancements")
	check(not TextureQuality.set_mode("invalid",false),"invalid texture mode rejected")
	check(TextureQuality.set_mode("Enhanced",true,"user://texture-test.cfg"),"texture mode persists")
	var texture_config := ConfigFile.new()
	texture_config.load("user://texture-test.cfg")
	check(texture_config.get_value("video","texture_quality","") == "Enhanced","texture mode reloads exactly")
	DirAccess.remove_absolute("user://texture-test.cfg")
	TextureQuality.set_mode(saved_texture,false)
	var failed := checks.filter(func(row): return not row.passed)
	var report := {"passed":failed.is_empty(),"checks":checks,"renderer":RenderingServer.get_current_rendering_method(),"display_driver":DisplayServer.get_name()}
	var file := FileAccess.open("res://staging/validation/layout-tests.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report,"  ")); file.close()
	quit(0 if failed.is_empty() else 1)
