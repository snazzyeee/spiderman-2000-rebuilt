extends Node3D

const ITEMS := ["Game", "Options", "Training", "Special", "Gallery"]
var panel := VBoxContainer.new()
var page := "Main"
var gallery_index := 0
var art := TextureRect.new()
var background := Node3D.new()
var visual: Node3D
var navigating := false
var settings := ConfigFile.new()
var backdrop: MeshInstance3D

func _ready() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	RevampAssets.lighting(self)
	add_child(background)
	if ResourceLoader.exists("res://assets/ui/shell_bg.png"):
		backdrop = MeshInstance3D.new()
		var quad := QuadMesh.new()
		quad.size = Vector2(40, 30)
		backdrop.mesh = quad
		var material := StandardMaterial3D.new()
		material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
		material.albedo_texture = load("res://assets/ui/shell_bg.png")
		material.disable_fog = true
		backdrop.material_override = material
		backdrop.position.z = -5
		background.add_child(backdrop)
	visual = RevampAssets.instantiate_model("res://assets/models/spidey.glb")
	if visual:
		background.add_child(visual)
		RevampAssets.fit_character(visual, 3.8)
		visual.rotation_degrees.y = -30
		var animation := RevampAssets.animation_player(visual)
		if animation:
			for clip in animation.get_animation_list():
				if clip != "RESET":
					animation.get_animation(clip).loop_mode = Animation.LOOP_LINEAR
					animation.play(clip)
					break
	var camera := Camera3D.new()
	add_child(camera)
	camera.position = Vector3(-1, 1.6, 5.5)
	camera.look_at(Vector3(-.7, 1.4, 0))
	camera.current = true
	if backdrop:
		backdrop.reparent(camera)
		backdrop.position = Vector3(0, 0, -10)
		backdrop.rotation = Vector3.ZERO
	var layer := CanvasLayer.new()
	add_child(layer)
	var title := Label.new()
	title.text = "SPIDER-MAN\n2000 REVAMP"
	title.position = Vector2(55, 35)
	title.add_theme_font_size_override("font_size", 44)
	title.add_theme_color_override("font_color", Color(1, .83, .1))
	title.add_theme_color_override("font_shadow_color", Color(.8, .03, .03))
	title.add_theme_constant_override("shadow_offset_x", 3)
	title.add_theme_constant_override("shadow_offset_y", 3)
	layer.add_child(title)
	panel.position = Vector2(60, 200)
	panel.custom_minimum_size = Vector2(360, 400)
	layer.add_child(panel)
	art.position = Vector2(550, 70)
	art.size = Vector2(650, 580)
	art.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	art.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	layer.add_child(art)
	settings.load("user://settings.cfg")
	for bus in ["Dialogue", "Music", "SFX"]:
		AudioServer.set_bus_volume_db(AudioServer.get_bus_index(bus), linear_to_db(float(settings.get_value("audio", bus, .8))))
	show_page("Main")
	_start_music()

func menu_sound(confirm: bool) -> void:
	var ending := "/menu.kat/001.wav" if confirm else "/menu.kat/000.wav"
	for key in Dialogue.registry:
		if str(Dialogue.registry[key].get("source_path", "")).to_lower().ends_with(ending):
			Dialogue.play_sfx(key)
			break

func _start_music() -> void:
	for key in Dialogue.registry:
		var entry: Dictionary = Dialogue.registry[key]
		if str(entry.get("source_path", "")).to_lower().ends_with("/song_full.bik"):
			Dialogue.music.stream = load(entry.file)
			Dialogue.music.play()
			if not Dialogue.music.finished.is_connected(_start_music):
				Dialogue.music.finished.connect(_start_music)
			break

func clear_panel() -> void:
	for child in panel.get_children():
		panel.remove_child(child)
		child.queue_free()
	art.visible = false
	navigating = false

func button(text: String, callback: Callable, disabled: bool = false) -> Button:
	var control := Button.new()
	control.text = text
	control.custom_minimum_size = Vector2(360, 58)
	control.disabled = disabled
	control.add_theme_font_size_override("font_size", 26)
	control.add_theme_color_override("font_focus_color", Color(1, .86, .1))
	var focus := StyleBoxFlat.new()
	focus.bg_color = Color(.35, .02, .04, .95)
	focus.border_color = Color(1, .8, .05)
	focus.set_border_width_all(3)
	control.add_theme_stylebox_override("focus", focus)
	control.pressed.connect(func():
		menu_sound(true)
		callback.call())
	control.focus_entered.connect(func():
		if navigating:
			menu_sound(false)
		navigating = true)
	panel.add_child(control)
	return control

func show_page(name_of_page: String) -> void:
	page = name_of_page
	clear_panel()
	match page:
		"Main":
			for item in ITEMS:
				button(item, func(): show_page(item))
		"Game":
			button("New Game", func(): launch(false))
			button("Load Game", func(): launch(false, true), GameSession.load_checkpoint() < 0)
			button("Back", func(): show_page("Main"))
		"Options":
			for bus in ["Dialogue", "Music", "SFX"]:
				var label := Label.new()
				label.text = bus
				panel.add_child(label)
				var slider := HSlider.new()
				slider.max_value = 1.0
				slider.step = .01
				slider.value = settings.get_value("audio", bus, .8)
				slider.custom_minimum_size.y = 35
				slider.value_changed.connect(func(value: float):
					AudioServer.set_bus_volume_db(AudioServer.get_bus_index(bus), linear_to_db(maxf(.0001, value)))
					settings.set_value("audio", bus, value)
					settings.save("user://settings.cfg"))
				panel.add_child(slider)
			button("Back", func(): show_page("Main"))
		"Training":
			button("Rooftop Practice", func(): launch(true))
			button("Controls", func(): show_page("Controls"))
			button("Back", func(): show_page("Main"))
		"Special":
			button("Replay Opening", func(): launch(false))
			button("Controls", func(): show_page("Controls"))
			button("Back", func(): show_page("Main"))
		"Controls":
			var label := Label.new()
			label.text = "WASD / Left stick: Move\nMouse / Right stick: Look\nSpace / A: Jump\nRMB / RT: Swing\nQ / RB: Zip · C / LB: Crawl\nF / X: Melee · R / Y: Web\nE / B: Interact\nEscape / Start: Pause"
			label.add_theme_font_size_override("font_size", 20)
			panel.add_child(label)
			button("Back", func(): show_page("Main"))
		"Gallery":
			show_art()
			button("Previous", func(): gallery_index = posmod(gallery_index-1, 32); show_art())
			button("Next", func(): gallery_index = (gallery_index+1)%32; show_art())
			button("Back", func(): show_page("Main"))
	var focusable := panel.find_children("*", "Control", false, false)
	for child in focusable:
		if child.focus_mode != Control.FOCUS_NONE and not (child is Button and child.disabled):
			child.grab_focus()
			break

func show_art() -> void:
	var path := "res://assets/ui/cb%02d.png" % gallery_index
	art.texture = load(path) if ResourceLoader.exists(path) else null
	art.visible = art.texture != null

func launch(practice: bool, resume_game: bool = false) -> void:
	GameSession.training = practice
	GameSession.resume_checkpoint = GameSession.load_checkpoint() if resume_game else -1
	Dialogue.music.stop()
	get_tree().change_scene_to_file("res://scenes/mission1.tscn")

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel") and page != "Main":
		menu_sound(true)
		show_page("Main")
		get_viewport().set_input_as_handled()
