extends SceneTree
var checks: Array = []
var samples: Array = []
var screenshots: Array = []
var video_memory_bytes := 0
var menu: Node
func _initialize() -> void: call_deferred("run")
func check(value: bool, label: String) -> void:
	checks.append({"passed":value,"label":label})
	if not value: print("DESIGN_QA FAIL ",label)
func luminance(c: Color) -> float:
	var v := c.srgb_to_linear()
	return .2126*v.r+.7152*v.g+.0722*v.b
func ratio(a: Color,b: Color) -> float:
	var x := luminance(a); var y := luminance(b)
	return (maxf(x,y)+.05)/(minf(x,y)+.05)
func descendants(node: Node) -> Array[Control]:
	var result: Array[Control] = []
	for child in node.get_children():
		if child is Control: result.append(child)
		result.append_array(descendants(child))
	return result
func run() -> void:
	DirAccess.make_dir_recursive_absolute("res://staging/validation/menu")
	var saved := FrontendSettings.values.duplicate()
	var audio_mode := MenuAudio.get_mode()
	MenuAudio.set_mode("Off",false)
	menu = load("res://scenes/main_menu.tscn").instantiate()
	root.add_child(menu)
	current_scene = menu
	await process_frame
	await process_frame
	menu.show_page("Main")
	var mouse := InputEventMouseMotion.new()
	menu._input(mouse)
	check(menu.view.nodes.input_hints.text == menu.view.layout.input_hints.mouse,"mouse input selects mouse hints")
	var event := InputEventJoypadButton.new()
	event.button_index = JOY_BUTTON_DPAD_DOWN
	event.pressed = true
	Input.parse_input_event(event)
	await process_frame
	check(menu.view.nodes.input_hints.text == menu.view.layout.input_hints.gamepad,"gamepad input selects gamepad hints")
	event.pressed = false
	Input.parse_input_event(event)
	await process_frame
	var focused := root.gui_get_focus_owner()
	check(focused is Button and focused.text == "Options","gamepad moves menu focus")
	var metrics_path := "res://assets/ui/fonts/font_manifest.json"
	if FileAccess.file_exists(metrics_path):
		var manifest: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(metrics_path))
		var record: Dictionary = manifest.fonts.filter(func(row): return row.name == "sp_fnt00")[0]
		for button in menu.panel.get_children():
			if not button is Button: continue
			var expected_width := 0.0
			for character in button.text:
				expected_width += float(record.original_advances[str(character.unicode_at(0))])
			expected_width *= float(button.get_theme_font_size("font_size"))/float(record.line_height)
			var rendered_width: float = button.get_theme_font("font").get_string_size(button.text,HORIZONTAL_ALIGNMENT_LEFT,-1,button.get_theme_font_size("font_size")).x
			check(absf(rendered_width-expected_width) <= 2.0,"main caption width matches recovered advances: "+button.text)
	menu.show_page("Options")
	menu.go_back()
	check(menu.page == "Main","back follows layout target")
	menu.idle_seconds = 60.0
	menu._process(0.0)
	check(menu.attract,"attract enters at 60 seconds")
	var key := InputEventKey.new();key.keycode=KEY_SPACE;key.pressed=true
	menu._input(key)
	check(not menu.attract and menu.page == "Main","attract exits on input and restores prior page")
	check(menu.view.nodes.input_hints.text == menu.view.layout.input_hints.keyboard,"keyboard input selects keyboard hints")
	FrontendSettings.set_value("reduced_motion","On",false)
	menu.show_page("Main")
	check(menu.transition != null,"reduced motion retains skippable transition")
	FrontendSettings.set_value("reduced_motion","Off",false)
	if menu.view.visual:
		var enhanced_surfaces := 0
		for mesh in menu.view.visual.find_children("*","MeshInstance3D",true,false):
			for surface in range(mesh.mesh.get_surface_count()):
				var material := mesh.get_surface_override_material(surface) as StandardMaterial3D
				var original := mesh.mesh.surface_get_material(surface) as StandardMaterial3D
				if material and original and material.albedo_texture and material.albedo_texture != original.albedo_texture: enhanced_surfaces += 1
		check(enhanced_surfaces > 0,"menu rig textures bind through decoded pixel identity")
		menu.view._process(.5)
		check(menu.view.visual.scale.y != menu.view.rest_scale.y and menu.view.actor_root.position == Vector3.ZERO,"breathing changes rig relative to fixed camera and ground")
		FrontendSettings.set_value("reduced_motion","On",false)
		menu.view._process(0.0)
		check(menu.view.visual.scale == menu.view.rest_scale,"reduced motion restores still rig scale")
		FrontendSettings.set_value("reduced_motion","Off",false)
		menu.view.react()
		check(menu.view.reaction,"confirmed frontend punch animation is available")
		menu.view.animation.seek(.3,true)
		menu.view._process(0.0)
		check(menu.view.punch_sound_sent,"reaction reaches one-shot frame cue")
		menu.view.animation.advance(menu.view.animation.current_animation_length+.1)
		menu.view._process(0.0)
		check(not menu.view.reaction,"reaction returns to idle after original punch clip")

	for resolution in [Vector2i(1280,720),Vector2i(1920,1080),Vector2i(2560,1440),Vector2i(1920,1200)]:
		root.size = resolution
		await process_frame
		for style in ["classic","enhanced"]:
			FrontendSettings.set_value("style",style,false)
			for screen in FrontendSettings.document().screens:
				menu.show_page(screen.id)
				if menu.transition and menu.transition.is_valid(): menu.transition.kill()
				menu.view.modulate.a = 1.0
				await process_frame
				await process_frame
				var frame: Rect2 = menu.view.canvas.get_global_rect()
				var safe: Rect2 = frame.grow_individual(-frame.size.x*.05,-frame.size.y*.05,-frame.size.x*.05,-frame.size.y*.05)
				var content: Control = menu.panel
				var authored: Array = menu.view.layout.elements.filter(func(row): return row.id == "content")[0].rect
				var expected := Vector2(authored[2],authored[3])
				if screen.id == "main_menu" and style == "classic":
					check(absf(content.get_global_rect().get_center().x-frame.get_center().x) <= 2.0,"classic main list centre within 2 pixels "+str(resolution))
				check(content.size.x <= expected.x+.5 and content.size.y <= expected.y+.5,screen.id+" "+style+" "+str(resolution)+" controls fit authored region")
				var safe_text := true
				var no_overlap := true
				var actual := descendants(content)
				for c in actual:
					if c is Label or c is Button:
						safe_text = safe_text and safe.encloses(c.get_global_rect())
				for i in range(content.get_child_count()):
					for j in range(i+1,content.get_child_count()):
						no_overlap = no_overlap and not content.get_child(i).get_rect().intersects(content.get_child(j).get_rect())
				check(safe_text,screen.id+" "+style+" "+str(resolution)+" text controls inside title safe area")
				check(no_overlap,screen.id+" "+style+" "+str(resolution)+" sibling controls do not overlap")
				if DisplayServer.get_name() != "headless":
					await RenderingServer.frame_post_draw
					var image := root.get_texture().get_image()
					check(Vector2i(image.get_width(),image.get_height()) == resolution,screen.id+" "+style+" capture matches requested physical resolution "+str(resolution))
					var filename: String = screen.id+"_"+style+"_"+RenderingServer.get_current_rendering_method()+"_"+str(resolution.x)+"x"+str(resolution.y)+".png"
					image.save_png("res://staging/validation/menu/"+filename)
					screenshots.append(filename)
					var contrast_ok := true
					for c in actual:
						if not (c is Button or c is Label): continue
						if c.text.is_empty(): continue
						var rect := c.get_global_rect()
						var pos := Vector2(rect.end.x-12,rect.position.y+8)
						pos *= Vector2(image.get_width(),image.get_height())/root.get_visible_rect().size
						var pixel := image.get_pixel(clampi(int(pos.x),0,image.get_width()-1),clampi(int(pos.y),0,image.get_height()-1))
						contrast_ok = contrast_ok and ratio(Color("FFF4CF"),pixel) >= 4.5
					check(contrast_ok,screen.id+" "+style+" "+str(resolution)+" foreground contrast against captured backdrop >=4.5")
				for c in actual:
					if c is Button:
						var focus := c.get_theme_stylebox("focus") as StyleBoxFlat
						var normal := c.get_theme_stylebox("normal") as StyleBoxFlat
						check(focus != null and normal != null and (focus.border_width_left > normal.border_width_left or focus.border_width_bottom > normal.border_width_bottom),screen.id+" "+style+" focus has shape distinction")
	if DisplayServer.get_name() != "headless":
		root.size = Vector2i(1920,1080)
		FrontendSettings.set_value("style","enhanced",false)
		menu.show_page("Main")
		Engine.max_fps = 0
		DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
		for frame in range(60): await process_frame
		var previous_tick := Time.get_ticks_usec()
		for frame in range(180):
			await RenderingServer.frame_post_draw
			var tick := Time.get_ticks_usec()
			samples.append(float(tick-previous_tick)/1000000.0)
			previous_tick = tick
		var ordered := samples.duplicate();ordered.sort()
		check(float(ordered[int(ordered.size()*.95)]) <= 1.0/60.0,"1080p 95th percentile frame interval <=16.667ms on real renderer")
		video_memory_bytes = RenderingServer.get_rendering_info(RenderingServer.RENDERING_INFO_VIDEO_MEM_USED)
		check(video_memory_bytes > 0 and video_memory_bytes < 4*1024*1024*1024,"Godot reported video memory below 4 GiB")
	FrontendSettings.values = saved
	MenuAudio.set_mode(audio_mode,false)
	root.get_node("Dialogue").music.stop()
	root.get_node("Dialogue").music.stream = null
	root.get_node("Dialogue").sfx.stop()
	root.get_node("Dialogue").sfx.stream = null
	await create_timer(.1).timeout
	menu.queue_free()
	await process_frame
	var failed := checks.filter(func(row): return not row.passed)
	var report := {"passed":failed.is_empty(),"checks":checks,"failures":failed,"screenshots":screenshots,"frame_intervals_seconds":samples,"video_memory_bytes":video_memory_bytes,"renderer":RenderingServer.get_current_rendering_method(),"display_driver":DisplayServer.get_name(),"contrast_scope":"Declared foreground against actual captured backdrop; original palette outlines/antialiasing not certified", "proposal_screens":"Unverified content is shown only as authored placeholder regions; no historical fidelity claim"}
	var file := FileAccess.open("res://staging/validation/design-qa-tests.json",FileAccess.WRITE)
	file.store_string(JSON.stringify(report,"  "));file.close()
	print("DESIGN_QA checks=",checks.size()," failures=",failed.size())
	quit(0 if failed.is_empty() else 1)
