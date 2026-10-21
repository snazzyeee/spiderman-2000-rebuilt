extends Node3D

const PAGE_IDS := {"Main":"main_menu","Game":"game_menu","Options":"options_menu","Training":"training","Special":"special_menu","Gallery":"gallery_menu","Controls":"controls_options","Audio":"audio"}
const PAGE_NAMES := {"main_menu":"Main","game_menu":"Game","options_menu":"Options","training":"Training","special_menu":"Special","gallery_menu":"Gallery","controls_options":"Controls","audio":"Audio"}
var panel: VBoxContainer
var page := "Main"
var current_id := "main_menu"
var gallery_index := 0
var art: TextureRect
var visual: Node3D
var settings := ConfigFile.new()
var menu_audio := MenuAudio.new()
var view: LayoutScreen
var layer := CanvasLayer.new()
var transition: Tween
var idle_seconds := 0.0
var attract := false
var previous_page := "Main"
var loading := false
var pending_scene := ""
var load_requested := false
var last_device := "keyboard"
var teaser_seconds := 0.0

func _ready() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	add_child(layer)
	settings.load("user://settings.cfg")
	for bus in ["Dialogue","Music","SFX"]:
		AudioServer.set_bus_volume_db(AudioServer.get_bus_index(bus),linear_to_db(float(settings.get_value("audio",bus,.8))))
	show_page("Main")
	_start_music()

func menu_sound(event: String) -> void:
	menu_audio.request(event,Dialogue)

func _start_music() -> void:
	for key in Dialogue.registry:
		var entry: Dictionary = Dialogue.registry[key]
		if str(entry.get("source_path","")).to_lower().ends_with("/song_full.bik"):
			Dialogue.music.stream = load(entry.file)
			Dialogue.music.play()
			if not Dialogue.music.finished.is_connected(_start_music): Dialogue.music.finished.connect(_start_music)
			break

func show_page(name_of_page: String) -> void:
	var id: String = PAGE_IDS.get(name_of_page,name_of_page)
	current_id = id
	page = PAGE_NAMES.get(id,id)
	menu_audio.reset_focus()
	if transition and transition.is_valid(): transition.kill()
	var old_view := view
	view = LayoutScreen.new()
	layer.add_child(view)
	view.build(id,FrontendSettings.get_value("style"))
	if is_instance_valid(old_view):
		if old_view.stage and view.stage:
			var container := view.stage.get_parent()
			view.stage.free()
			old_view.stage.reparent(container)
			view.stage = old_view.stage
			view.actor_root = old_view.actor_root
			view.visual = old_view.visual
			view.animation = old_view.animation
			view.reaction = old_view.reaction
			view.punch_sound_sent = old_view.punch_sound_sent
			view.rest_scale = old_view.rest_scale
			view.rest_position = old_view.rest_position
			view.clock_time = old_view.clock_time
		layer.remove_child(old_view)
		old_view.queue_free()
	panel = view.nodes.get("content") as VBoxContainer
	visual = view.visual
	if view.nodes.has("gallery_art"):
		art = view.nodes.gallery_art as TextureRect
	else:
		art = TextureRect.new()
		view.add_child(art)
		art.hide()
	if panel:
		for screen in FrontendSettings.document().screens:
			if screen.id == id:
				for action in screen.actions: _add_action(str(action[0]),str(action[1]))
		var controls := panel.find_children("*","Control",false,false)
		for control in controls:
			if control.focus_mode != Control.FOCUS_NONE and not (control is Button and control.disabled):
				control.grab_focus()
				break
	if id == "gallery_menu" or id == "attract": show_art()
	_update_hints()
	_update_teaser()
	view.modulate.a = 0.0
	transition = create_tween()
	transition.tween_property(view,"modulate:a",1.0,.1 if FrontendSettings.get_value("reduced_motion") == "On" else float(view.layout.transition_seconds))

func _style(control: Control) -> void:
	var e: Dictionary = view.layout.elements.filter(func(row): return row.id == "content")[0]
	control.custom_minimum_size.y = float(view.layout.row_height)
	control.add_theme_font_size_override("font_size",int(e.font_size))
	control.add_theme_color_override("font_color",Color(e.colours.text))
	control.add_theme_color_override("font_focus_color",Color(e.colours.focus))
	if control is Button:
		var normal := StyleBoxFlat.new()
		normal.bg_color = Color.TRANSPARENT if view.layout.get("picture_action",false) else Color(e.colours.backdrop)
		normal.border_color = Color(.36,.36,.43)
		normal.set_border_width_all(1)
		if view.style == "enhanced" and not view.layout.get("picture_action",false):
			normal.bg_color = Color("17182b")
			normal.border_color = Color("030309")
			normal.set_border_width_all(4)
			normal.skew = Vector2(.04,0)
			normal.shadow_color = Color(0,0,0,.8)
			normal.shadow_size = 4
			normal.shadow_offset = Vector2(5,5)
		control.add_theme_stylebox_override("normal",normal)
		var focus := normal.duplicate() as StyleBoxFlat
		focus.border_color = Color(e.colours.focus)
		focus.set_border_width_all(3)
		focus.border_width_left = 9
		if view.style == "classic" and current_id == "main_menu":
			normal.bg_color = Color.TRANSPARENT
			normal.set_border_width_all(0)
			focus.bg_color = Color.TRANSPARENT
			focus.set_border_width_all(0)
			focus.border_width_bottom = 2
		control.add_theme_stylebox_override("focus",focus)

func _add_action(caption: String, action: String) -> void:
	if action in ["font","style","menu_sounds"] or action.begins_with("pref:"):
		var row := HBoxContainer.new()
		var label := Label.new()
		label.text = caption
		label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		_style(label)
		row.add_child(label)
		var choice := OptionButton.new()
		choice.fit_to_longest_item = false
		var key := action.trim_prefix("pref:")
		var options: Array = OriginalFonts.VARIANTS if action == "font" else MenuAudio.MODES if action == "menu_sounds" else FrontendSettings.choices(key)
		for option in options: choice.add_item(option)
		var value: String = OriginalFonts.get_variant() if action == "font" else MenuAudio.get_mode() if action == "menu_sounds" else FrontendSettings.get_value(key)
		choice.select(options.find(value))
		_style(choice)
		choice.item_selected.connect(_choice_changed.bind(action,options))
		row.add_child(choice)
		panel.add_child(row)
		return
	if action.begins_with("volume:"):
		var bus := action.trim_prefix("volume:")
		var label := Label.new()
		label.text = caption
		_style(label)
		panel.add_child(label)
		var slider := HSlider.new()
		slider.max_value = 1.0
		slider.step = .01
		slider.value = settings.get_value("audio",bus,.8)
		_style(slider)
		slider.value_changed.connect(_volume_changed.bind(bus))
		panel.add_child(slider)
		return
	if action == "info":
		var label := Label.new()
		label.text = caption
		_style(label)
		panel.add_child(label)
		return
	var control := Button.new()
	control.text = "" if view.layout.get("picture_action",false) else caption
	control.tooltip_text = caption
	control.disabled = action == "resume" and GameSession.load_checkpoint() < 0
	_style(control)
	control.pressed.connect(_activate.bind(action))
	control.focus_entered.connect(_focus.bind(control))
	control.mouse_entered.connect(_hover.bind(control))
	panel.add_child(control)

func _focus(control: Control) -> void: menu_audio.focus(control,Dialogue)
func _hover(control: Control) -> void:
	if not (control is Button and control.disabled) and not control.has_focus(): control.grab_focus()
func _choice_changed(index: int, action: String, options: Array) -> void:
	if action == "font": OriginalFonts.set_variant(options[index])
	elif action == "menu_sounds":
		MenuAudio.set_mode(options[index])
		if options[index] == "Off": Dialogue.sfx.stop()
	else: FrontendSettings.set_value(action.trim_prefix("pref:"),options[index])
	settings.load("user://settings.cfg")
	if action in ["style","pref:side_fill"]: show_page(page)
	if action == "pref:cursor_policy" and options[index] == "Visible": Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
func _volume_changed(value: float, bus: String) -> void:
	AudioServer.set_bus_volume_db(AudioServer.get_bus_index(bus),linear_to_db(maxf(.0001,value)))
	settings.load("user://settings.cfg")
	settings.set_value("audio",bus,value)
	settings.save("user://settings.cfg")
func _activate(action: String) -> void:
	menu_sound("back" if action == "back" else "confirm")
	if action != "back": view.react()
	if action.begins_with("page:"): show_page(action.trim_prefix("page:"))
	elif action == "back": go_back()
	elif action in ["launch","practice","resume"]: begin_launch(action == "practice",action == "resume")
	elif action == "previous": gallery_index = posmod(gallery_index-1,32); show_art()
	elif action == "next": gallery_index = (gallery_index+1)%32; show_art()
	else: view.react()
func go_back() -> void:
	if attract: exit_attract(); return
	var target: String = view.layout.back
	if target not in ["none","gameplay"]: show_page(target)
func show_art() -> void:
	var path := "res://assets/ui/cb%02d.png" % gallery_index
	art.texture = load(path) if ResourceLoader.exists(path) else null
	art.visible = art.texture != null
func _update_hints() -> void:
	if not view or not view.nodes.has("input_hints"): return
	var hints: Dictionary = view.layout.input_hints
	view.nodes.input_hints.text = str(hints[last_device])
func _update_teaser() -> void:
	if not view or not view.nodes.has("gallery_teaser"): return
	var path := "res://assets/ui/cb%02d.png" % gallery_index
	view.nodes.gallery_teaser.texture = load(path) if ResourceLoader.exists(path) else null
func launch(practice: bool, resume_game: bool = false) -> void:
	GameSession.training = practice
	GameSession.resume_checkpoint = GameSession.load_checkpoint() if resume_game else -1
	Dialogue.music.stop()
	# Immediate path is retained for the existing two-frame lifecycle smoke gate.
	get_tree().change_scene_to_file("res://scenes/mission1.tscn")
func begin_launch(practice: bool, resume_game: bool = false, scene_path: String = "res://scenes/mission1.tscn") -> void:
	if loading: return
	GameSession.training = practice
	GameSession.resume_checkpoint = GameSession.load_checkpoint() if resume_game else -1
	Dialogue.music.stop()
	loading = true
	load_requested = false
	pending_scene = scene_path
	show_page("loading")
	await get_tree().process_frame
	var status := ResourceLoader.load_threaded_request(scene_path)
	load_requested = status == OK
	if not load_requested:
		loading = false
		show_page("Game")
		menu_sound("reject")

func enter_attract() -> void:
	previous_page = page
	attract = true
	show_page("attract")
func exit_attract() -> void:
	attract = false
	idle_seconds = 0.0
	show_page(previous_page)
func _input(event: InputEvent) -> void:
	if event is InputEventMouseMotion or event is InputEventMouseButton: last_device = "mouse"
	elif event is InputEventKey: last_device = "keyboard"
	elif event is InputEventJoypadButton or (event is InputEventJoypadMotion and absf(event.axis_value) > .2): last_device = "gamepad"
	_update_hints()
	if event is InputEventMouseMotion:
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	elif event is InputEventKey or event is InputEventJoypadButton or event is InputEventJoypadMotion:
		if FrontendSettings.get_value("cursor_policy") == "Hybrid": Input.mouse_mode = Input.MOUSE_MODE_HIDDEN
	if event is InputEventMouseMotion or event.is_pressed():
		idle_seconds = 0.0
		if attract:
			exit_attract()
			get_viewport().set_input_as_handled()
func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		menu_sound("back")
		go_back()
		get_viewport().set_input_as_handled()
	if event.is_action_pressed("ui_accept") and transition and transition.is_running():
		transition.kill()
		view.modulate.a = 1.0
func _process(delta: float) -> void:
	if current_id == "main_menu" and FrontendSettings.get_value("reduced_motion") != "On":
		teaser_seconds += delta
		if teaser_seconds >= 10.0:
			teaser_seconds = fmod(teaser_seconds,10.0)
			gallery_index = (gallery_index+1)%32
			_update_teaser()
	if loading:
		if not load_requested: return
		var status := ResourceLoader.load_threaded_get_status(pending_scene)
		if status == ResourceLoader.THREAD_LOAD_LOADED:
			var packed := ResourceLoader.load_threaded_get(pending_scene) as PackedScene
			loading = false
			get_tree().change_scene_to_packed(packed)
		elif status in [ResourceLoader.THREAD_LOAD_FAILED,ResourceLoader.THREAD_LOAD_INVALID_RESOURCE]:
			loading = false
			show_page("Game")
			menu_sound("reject")
		return
	idle_seconds += delta
	if not attract and not loading and idle_seconds >= 60.0: enter_attract()
	if attract:
		if view.nodes.has("actor"):
			var carousel := FrontendSettings.get_value("attract_media") == "Carousel"
			view.nodes.actor.visible = carousel and int(idle_seconds/10.0)%2 == 0
			art.visible = not view.nodes.actor.visible
		var next_index := int(idle_seconds/10.0)%32
		if next_index != gallery_index: gallery_index = next_index; show_art()
