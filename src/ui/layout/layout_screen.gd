class_name LayoutScreen
extends Control

var canvas := Control.new()
var nodes: Dictionary = {}
var layout: Dictionary
var screen_id := ""
var style := "enhanced"
var visual: Node3D
var stage: SubViewport
var actor_root: Node3D
var animation: AnimationPlayer
var reaction := false
var punch_sound_sent := false
var clock_time := 0.0
var rest_scale := Vector3.ONE
var rest_position := Vector3.ZERO

func build(id: String, selected_style: String, dimmed: bool = false) -> void:
	screen_id = id
	style = selected_style
	var document := FrontendSettings.document()
	for screen in document.screens:
		if screen.id == id: layout = screen.styles[style]
	if layout.is_empty(): return
	add_child(canvas)
	var black := ColorRect.new()
	black.color = Color.TRANSPARENT if dimmed else Color.BLACK
	black.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	black.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(black)
	move_child(black, 0)
	for e in layout.elements:
		var control: Control
		match e.type:
			"menu_list": control = VBoxContainer.new()
			"button": control = VBoxContainer.new()
			"3d_actor": control = SubViewportContainer.new()
			"image": control = TextureRect.new() if e.asset != "" else ColorRect.new()
			_: control = Label.new()
		control.name = e.id
		control.position = Vector2(e.rect[0],e.rect[1])
		control.size = Vector2(e.rect[2],e.rect[3])
		control.z_index = int(e.z)
		control.mouse_filter = Control.MOUSE_FILTER_IGNORE if e.id != "content" else Control.MOUSE_FILTER_PASS
		canvas.add_child(control)
		nodes[e.id] = control
		if control is ColorRect:
			control.color = Color("100E1B")
			if dimmed:
				control.color = Color(0,0,0,.7)
			elif style == "enhanced" and FrontendSettings.get_value("side_fill") == "Black":
				var frame: Array = layout.pillarbox_frame
				control.position = Vector2(frame[0],frame[1])
				control.size = Vector2(frame[2],frame[3])
			elif style == "enhanced" and FrontendSettings.get_value("side_fill") == "Wings":
				var material := ShaderMaterial.new()
				material.shader = load("res://src/ui/layout/ink_backdrop.gdshader")
				control.material = material
		if control is TextureRect:
			control.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
			control.stretch_mode = TextureRect.STRETCH_SCALE if e.get("virtual_raster",false) else TextureRect.STRETCH_KEEP_ASPECT_CENTERED
			if ResourceLoader.exists(e.asset): control.texture = load(e.asset)
		if control is Label:
			control.text = e.get("text", "")
			control.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			control.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
			OriginalFonts.bind(control,"menu")
			control.add_theme_font_size_override("font_size",int(e.font_size))
			control.add_theme_color_override("font_color",Color(e.colours.text))
		if e.id == "content":
			OriginalFonts.bind(control,"menu")
			control.add_theme_constant_override("separation",int(layout.gap))
		if control is SubViewportContainer: _build_actor(control,e.asset)
	resize_canvas()

func _ready() -> void:
	get_viewport().size_changed.connect(resize_canvas)
	resize_canvas()

func resize_canvas() -> void:
	if layout.is_empty() or not is_inside_tree(): return
	var available := get_viewport_rect().size
	size = available
	var reference := Vector2(layout.canvas[0],layout.canvas[1])
	var factor := minf(available.x/reference.x,available.y/reference.y)
	canvas.size = reference
	canvas.scale = Vector2.ONE*factor
	canvas.position = (available-reference*factor)*.5

func _build_actor(container: SubViewportContainer, path: String) -> void:
	stage = SubViewport.new()
	stage.transparent_bg = true
	stage.own_world_3d = true
	stage.size = Vector2i(container.size)
	stage.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	container.stretch = true
	container.add_child(stage)
	actor_root = Node3D.new()
	stage.add_child(actor_root)
	var config: Dictionary = layout.actor_stage
	if ResourceLoader.exists(path):
		visual = (load(path) as PackedScene).instantiate()
		actor_root.add_child(visual)
		RevampAssets.fit_character(visual,float(config.height))
		rest_scale = visual.scale
		rest_position = visual.position
		visual.rotation_degrees.y = float(config.yaw)
		animation = RevampAssets.animation_player(visual)
		if animation and animation.has_animation(config.idle_slot):
			animation.get_animation(config.idle_slot).loop_mode = Animation.LOOP_LINEAR
			animation.play(config.idle_slot)
	var camera := Camera3D.new()
	actor_root.add_child(camera)
	camera.position = Vector3(config.camera[0],config.camera[1],config.camera[2])
	camera.look_at(Vector3(config.target[0],config.target[1],config.target[2]))
	camera.fov = 55.0
	camera.current = true
	for pair in [["key_rotation",1.6,Color(1,.85,.65)],["fill_rotation",.6,Color(.45,.6,1)],["rim_rotation",1.2,Color(.75,.65,1)]]:
		var light := DirectionalLight3D.new()
		var angles: Array = config[pair[0]]
		light.rotation_degrees = Vector3(angles[0],angles[1],angles[2])
		light.light_energy = pair[1]
		light.light_color = pair[2]
		light.shadow_enabled = pair[0] == "key_rotation"
		actor_root.add_child(light)
	var floor_mesh := MeshInstance3D.new()
	var plane := PlaneMesh.new()
	plane.size = Vector2.ONE*float(config.ground_size)
	floor_mesh.mesh = plane
	var material := StandardMaterial3D.new()
	material.albedo_color = Color("05050a")
	material.roughness = .9
	floor_mesh.material_override = material
	actor_root.add_child(floor_mesh)

func react() -> void:
	if FrontendSettings.get_value("reduced_motion") == "On": return
	if not animation or not animation.has_animation(layout.actor_stage.punch_slot): return
	reaction = true
	punch_sound_sent = false
	animation.get_animation(layout.actor_stage.punch_slot).loop_mode = Animation.LOOP_NONE
	animation.play(layout.actor_stage.punch_slot)

func _process(delta: float) -> void:
	clock_time += delta
	if nodes.has("canvas_backdrop") and nodes.canvas_backdrop.material is ShaderMaterial:
		var offset := Vector2.ZERO
		if FrontendSettings.get_value("reduced_motion") != "On":
			offset = (get_global_mouse_position()/get_viewport_rect().size-Vector2(.5,.5))*float(layout.actor_stage.parallax_pixels)
		nodes.canvas_backdrop.material.set_shader_parameter("drift",offset)
	if not visual: return
	var reduced := FrontendSettings.get_value("reduced_motion") == "On"
	if animation: animation.speed_scale = 0.0 if reduced else 1.0
	if reduced:
		visual.scale = rest_scale
		visual.position = rest_position
		return
	var breath := 1.0+sin(clock_time*TAU/float(layout.actor_stage.breathing_period))*float(layout.actor_stage.breathing_amplitude)
	visual.scale = rest_scale*Vector3(1,breath,1)
	visual.position.y = rest_position.y*breath
	if reaction and animation:
		if not animation.is_playing():
			reaction = false
			animation.play(layout.actor_stage.idle_slot)
			return
		if animation.current_animation_position >= 7.0/30.0 and not punch_sound_sent:
			punch_sound_sent = true
			# Actor event IDs are ledgered separately from menu UI events.
			var dialogue := get_tree().root.get_node("Dialogue")
			dialogue.sfx.stop()
			var sound_id := randi_range(0xE,0x11)
			if MenuAudio.allows(dialogue.sfx_stream_for_id(sound_id),true): dialogue.play_sfx_id(sound_id)

func action_button(caption: String, callback: Callable) -> Button:
	var column: VBoxContainer = nodes.content
	var e: Dictionary = layout.elements.filter(func(row): return row.id == "content")[0]
	var control := Button.new()
	control.text = caption
	control.custom_minimum_size.y = float(layout.row_height)
	control.add_theme_font_size_override("font_size",int(e.font_size))
	control.add_theme_color_override("font_color",Color(e.colours.text))
	var normal := StyleBoxFlat.new()
	normal.bg_color = Color(e.colours.backdrop)
	normal.border_color = Color(.36,.36,.43)
	normal.set_border_width_all(1)
	control.add_theme_stylebox_override("normal",normal)
	var focus := normal.duplicate() as StyleBoxFlat
	focus.border_color = Color(e.colours.focus)
	focus.set_border_width_all(3)
	focus.border_width_left = 9
	control.add_theme_stylebox_override("focus",focus)
	control.pressed.connect(callback)
	control.mouse_entered.connect(func(): control.grab_focus())
	column.add_child(control)
	return control
