extends SceneTree

var checks: Array = []
var skips: Array[String] = []

func _initialize() -> void:
	call_deferred("run")

func check(condition: bool, label: String) -> void:
	checks.append({"label":label, "passed":condition})
	print("FONT_TEST ","PASS " if condition else "FAIL ",label)

func run() -> void:
	var original_variant := OriginalFonts.get_variant()
	var config_path := "user://settings.cfg"
	var existed := FileAccess.file_exists(config_path)
	var saved := FileAccess.get_file_as_bytes(config_path) if existed else PackedByteArray()
	check(original_variant in OriginalFonts.VARIANTS, "valid loaded/default font variant")
	check(not OriginalFonts.set_variant("invalid", false), "unknown variant rejected")
	check(OriginalFonts.set_variant("Faithful", false), "faithful variant selected")
	var control := Label.new()
	root.add_child(control)
	OriginalFonts.bind(control,"menu")
	check(control.theme != null, "theme binds without requiring private fonts")
	var previous := control.theme
	check(OriginalFonts.set_variant("Enhanced", false) and control.theme != previous, "variant applies to existing control live")
	check(OriginalFonts.font_for("menu","res://assets/ui/fonts/definitely_missing") == null, "absent private font returns clean fallback")
	check(OriginalFonts.theme_for("menu","res://assets/ui/fonts/definitely_missing").default_font == null, "missing font leaves existing default font")
	check(OriginalFonts.set_variant("Remastered",false),"Remastered variant applies live")
	check(OriginalFonts.set_variant("Faithful"), "font selection persists")
	var config := ConfigFile.new()
	config.load(config_path)
	check(config.get_value("video","font_variant","") == "Faithful", "saved font selection reloads exactly")
	control.free()
	check(OriginalFonts.set_variant("Enhanced",false), "freed controls do not break live variant changes")
	var survivor := Label.new()
	root.add_child(survivor)
	for iteration in range(20):
		var temporary := Label.new()
		OriginalFonts.bind(temporary, "menu")
		temporary.free()
	OriginalFonts.bind(survivor, "menu")
	check(OriginalFonts.bindings.size() == 1, "binding prunes freed controls across repeated scene lifetimes")
	var old_theme := survivor.theme
	check(not OriginalFonts.set_variant("Faithful", true, "user://missing-font-settings-parent/settings.cfg"), "failed persistence is reported")
	check(OriginalFonts.get_variant() == "Faithful" and survivor.theme != old_theme, "save failure still applies live font variant")
	OriginalFonts.bind(null, "menu")
	check(OriginalFonts.bindings.size() == 1, "null binding is ignored")
	survivor.free()
	var manifest_path := "res://assets/ui/fonts/font_manifest.json"
	if FileAccess.file_exists(manifest_path):
		var manifest: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(manifest_path))
		check(manifest.get("passed",false) and manifest.fonts.size() == 6 and manifest.tracing_failures.is_empty(), "six private fonts traced without replacement")
		for record in manifest.fonts:
			var bitmap := load("res://assets/ui/fonts/"+record.faithful) as Font
			var outline := load("res://assets/ui/fonts/"+record.enhanced) as Font
			check(bitmap != null and outline != null, record.name+" both FontFiles load")
			if bitmap and outline:
				check([65,66,48,49].all(func(code): return record.mapping.has(str(code))) and record.original_advances.has("32"), record.name+" diagnostic sample has explicit exported mapping and space advance")
				var size_of_font := int(record.line_height)*4
				var expected := 0.0
				for code in [65,66,32,48,49]: expected += float(record.original_advances[str(code)])*4.0
				var a := bitmap.get_string_size("AB 01",HORIZONTAL_ALIGNMENT_LEFT,-1,size_of_font).x
				var b := outline.get_string_size("AB 01",HORIZONTAL_ALIGNMENT_LEFT,-1,size_of_font).x
				check(absf(a-expected) <= 0.01 and absf(b-expected) <= 0.01, record.name+" Godot preserves exact advances in both variants")
				check(outline.get_palette_count() > 0, record.name+" enhanced palette layers loaded")
				var remastered_path: String = "res://assets/ui/fonts/"+str(record.name)+"_remastered.fnt"
				if ResourceLoader.exists(remastered_path):
					var remastered := load(remastered_path) as Font
					check(remastered != null and absf(remastered.get_string_size("AB 01",HORIZONTAL_ALIGNMENT_LEFT,-1,size_of_font).x-expected) <= .01,record.name+" Remastered preserves original advances exactly")
	else:
		skips.append("Private font resources absent; original-font load/metrics checks skipped")
	if existed:
		var restored := FileAccess.open(config_path,FileAccess.WRITE)
		restored.store_buffer(saved)
		restored.close()
	else:
		DirAccess.remove_absolute(config_path)
	OriginalFonts.set_variant(original_variant,false)
	var failures := checks.filter(func(row): return not row.passed)
	var report := {"passed":failures.is_empty(),"checks":checks,"failures":failures,"skips":skips,
		"renderer":RenderingServer.get_current_rendering_method(),"display_driver":DisplayServer.get_name()}
	DirAccess.make_dir_recursive_absolute("res://staging/validation")
	var file := FileAccess.open("res://staging/validation/font-tests.json",FileAccess.WRITE)
	file.store_string(JSON.stringify(report,"  "))
	file.close()
	print("FONT_TESTS ",JSON.stringify(report))
	quit(0 if failures.is_empty() else 1)
