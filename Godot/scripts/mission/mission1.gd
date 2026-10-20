extends Node3D

signal intro_finished
signal lifecycle_changed(phase: int)
enum Lifecycle { INTRO_CINEMATIC, MISSION_BRIEFING, GAMEPLAY_ACTIVE, COMPLETE }

const PlayerScript := preload("res://scripts/player/spider_player.gd")
const ThugScript := preload("res://scripts/mission/thug.gd")
var lifecycle := Lifecycle.INTRO_CINEMATIC
var player: SpiderPlayer
var layout: Dictionary
var checkpoint := 0
var spawn := Vector3.ZERO
var enemies: Array[Node3D] = []
var encounter_started := false
var hud := Label.new()
var prompt := Label.new()
var subtitles := Label.new()
var pause_panel := PanelContainer.new()
var intro_camera := Camera3D.new()
var intro_tween: Tween
var intro_clip := ""
var briefing_time := 0.0
var paused_game := false
var missing_assets := false

func vector(value: Array) -> Vector3:
	return Vector3(float(value[0]), float(value[1]), float(value[2]))

func _ready() -> void:
	RevampAssets.lighting(self)
	_build_hud()
	if not FileAccess.file_exists("res://assets/world/mission1_layout.json"):
		missing_assets = true
		prompt.text = "Private assets missing. Run the conversion pipeline. Escape returns to menu."
		return
	layout = JSON.parse_string(FileAccess.get_file_as_string("res://assets/world/mission1_layout.json"))
	var level := RevampAssets.instantiate_model("res://assets/models/l1a1_g.glb")
	if not level:
		missing_assets = true
		return
	add_child(level)
	level.scale = Vector3.ONE*.019792303442955017
	for mesh in level.find_children("*", "MeshInstance3D", true, false):
		if mesh.mesh:
			mesh.create_trimesh_collision()
	var skylight_geometry := RevampAssets.instantiate_model("res://assets/models/l1a1_o.glb")
	if skylight_geometry:
		add_child(skylight_geometry)
		skylight_geometry.scale = level.scale
		for mesh in skylight_geometry.find_children("*", "MeshInstance3D", true, false):
			if mesh.mesh and "00000006" in mesh.mesh.resource_name:
				mesh.create_trimesh_collision()
			else:
				mesh.visible = false
	player = PlayerScript.new()
	add_child(player)
	spawn = vector(layout.spawn)+Vector3.UP*.1
	player.global_position = spawn
	player.spawn_position = spawn
	player.defeated.connect(recover)
	var destination := vector(layout.checkpoints[0])
	player.target_yaw = atan2(spawn.x-destination.x, spawn.z-destination.z)
	player.yaw = player.target_yaw
	_build_enemies()
	intro_finished.connect(_begin_briefing)
	Dialogue.clip_finished.connect(_on_clip_finished)
	Dialogue.subtitle_changed.connect(_show_subtitle)
	if GameSession.training or GameSession.resume_checkpoint >= 0:
		if GameSession.resume_checkpoint >= 0:
			checkpoint = GameSession.resume_checkpoint
			spawn = vector(layout.checkpoints[checkpoint])+Vector3.UP*.1
			player.global_position = spawn
			checkpoint += 1
		activate_gameplay()
	else:
		start_intro()

func _build_hud() -> void:
	var layer := CanvasLayer.new()
	add_child(layer)
	hud.position = Vector2(24, 22)
	hud.add_theme_font_size_override("font_size", 24)
	layer.add_child(hud)
	prompt.position = Vector2(150, 565)
	prompt.size = Vector2(980, 130)
	prompt.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	prompt.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	prompt.add_theme_font_size_override("font_size", 23)
	prompt.add_theme_color_override("font_color", Color(1, .86, .2))
	layer.add_child(prompt)
	subtitles.position = Vector2(150, 470)
	subtitles.size = Vector2(980, 80)
	subtitles.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	subtitles.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	subtitles.add_theme_font_size_override("font_size", 22)
	layer.add_child(subtitles)
	pause_panel.position = Vector2(470, 190)
	pause_panel.custom_minimum_size = Vector2(340, 260)
	var column := VBoxContainer.new()
	pause_panel.add_child(column)
	for caption in ["Resume", "Recover to checkpoint", "Main Menu"]:
		var button := Button.new()
		button.text = caption
		button.custom_minimum_size.y = 65
		column.add_child(button)
		button.pressed.connect(func():
			if caption == "Main Menu":
				Dialogue.release_cinematic(self)
				get_tree().change_scene_to_file("res://scenes/main_menu.tscn")
			else:
				set_paused(false)
				if caption == "Recover to checkpoint":
					recover())
	pause_panel.visible = false
	layer.add_child(pause_panel)

func _show_subtitle(speaker: String, text: String) -> void:
	# Objective speech cannot replace the cinematic or authored briefing UI.
	subtitles.text = (speaker+": "+text) if lifecycle == Lifecycle.GAMEPLAY_ACTIVE and not text.is_empty() else ""

func _build_enemies() -> void:
	var bank := vector(layout.bank)
	for offset in [Vector3(-2, 0, -1), Vector3(2, 0, -1), Vector3(0, 0, 1.4)]:
		var thug = ThugScript.new()
		thug.player = player
		add_child(thug)
		thug.global_position = bank+offset
		thug.origin = thug.global_position
		thug.ranged = enemies.size() == 2
		enemies.append(thug)

func start_intro() -> void:
	lifecycle = Lifecycle.INTRO_CINEMATIC
	player.controls_enabled = false
	hud.visible = false
	prompt.text = ""
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	Dialogue.acquire_cinematic(self)
	if not intro_camera.get_parent():
		add_child(intro_camera)
	intro_camera.global_position = spawn+Vector3(9, 6, 11)
	intro_camera.look_at(spawn+Vector3.UP)
	intro_camera.current = true
	intro_tween = create_tween().set_loops()
	intro_tween.tween_property(intro_camera, "global_position", spawn+Vector3(5, 4, 8), 12.0)
	intro_tween.tween_property(intro_camera, "global_position", spawn+Vector3(9, 6, 11), 12.0)
	intro_clip = Dialogue.find_key("stan_00")
	if intro_clip.is_empty() or not Dialogue.play(intro_clip, self):
		prompt.text = "Intro audio unavailable. Press Enter / A to continue."
	lifecycle_changed.emit(lifecycle)

func _on_clip_finished(clip_id: String) -> void:
	if lifecycle == Lifecycle.INTRO_CINEMATIC and clip_id == intro_clip:
		finish_intro()

func finish_intro() -> void:
	if lifecycle != Lifecycle.INTRO_CINEMATIC:
		return
	if intro_tween:
		intro_tween.kill()
	Dialogue.release_cinematic(self)
	intro_camera.current = false
	player.camera.current = true
	intro_finished.emit()

func _begin_briefing() -> void:
	lifecycle = Lifecycle.MISSION_BRIEFING
	briefing_time = 0
	prompt.text = "MISSION BRIEFING\nCross the rooftops, defeat the bank thugs, then enter the skylight.\nEnter / A to begin."
	lifecycle_changed.emit(lifecycle)

func activate_gameplay() -> void:
	lifecycle = Lifecycle.GAMEPLAY_ACTIVE
	player.controls_enabled = true
	hud.visible = true
	prompt.text = ""
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	lifecycle_changed.emit(lifecycle)

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("pause"):
		if missing_assets:
			get_tree().change_scene_to_file("res://scenes/main_menu.tscn")
		else:
			set_paused(not paused_game)
		get_viewport().set_input_as_handled()
	elif not paused_game and event.is_action_pressed("ui_accept"):
		if lifecycle == Lifecycle.INTRO_CINEMATIC:
			finish_intro()
		elif lifecycle == Lifecycle.MISSION_BRIEFING and briefing_time > .2:
			activate_gameplay()
		elif lifecycle == Lifecycle.COMPLETE:
			get_tree().change_scene_to_file("res://scenes/main_menu.tscn")

func set_paused(value: bool) -> void:
	paused_game = value
	pause_panel.visible = value
	player.controls_enabled = not value and lifecycle == Lifecycle.GAMEPLAY_ACTIVE
	Dialogue.speech.stream_paused = value
	if intro_tween and intro_tween.is_valid():
		intro_tween.pause() if value else intro_tween.play()
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE if value or lifecycle != Lifecycle.GAMEPLAY_ACTIVE else Input.MOUSE_MODE_CAPTURED
	if value:
		pause_panel.get_child(0).get_child(0).grab_focus()

func _process(delta: float) -> void:
	if missing_assets or paused_game:
		return
	if lifecycle == Lifecycle.MISSION_BRIEFING:
		briefing_time += delta
		return
	if lifecycle != Lifecycle.GAMEPLAY_ACTIVE:
		return
	if player.global_position.y < spawn.y-60:
		recover()
	if GameSession.training:
		hud.text = "ROOFTOP PRACTICE   |   Health %d" % player.health
		prompt.text = "WASD move · Space jump · RMB swing · Q zip · C crawl\nEscape opens recovery and menu controls"
		return
	var points: Array = layout.checkpoints
	if checkpoint < points.size():
		var target := vector(points[checkpoint])
		hud.text = "ROOFTOPS %d / %d   |   Health %d\nWASD move · Space jump · RMB swing · Q zip · C crawl · F melee · R web" % [checkpoint, points.size(), player.health]
		prompt.text = "Checkpoint: %d m" % int(player.global_position.distance_to(target))
		if player.global_position.distance_to(target) < 4.0:
			spawn = target+Vector3.UP*.1
			if not GameSession.training:
				GameSession.save_checkpoint(checkpoint)
			checkpoint += 1
	else:
		if not encounter_started:
			encounter_started = true
			for enemy in enemies:
				enemy.active = true
		var living := 0
		for enemy in enemies:
			if enemy.health > 0 and enemy.state != enemy.State.DEFEATED:
				living += 1
		hud.text = "BANK ROOFTOP   |   Thugs %d   |   Health %d" % [living, player.health]
		prompt.text = "Defeat the bank thugs" if living else "Approach the skylight and press E / B"
		var bank := vector(layout.bank)
		if living and player.global_position.distance_to(bank) > 13.0:
			recover()
		if Input.is_action_just_pressed("interact"):
			try_enter_skylight()

func try_enter_skylight() -> bool:
	if lifecycle != Lifecycle.GAMEPLAY_ACTIVE or paused_game or checkpoint < layout.checkpoints.size():
		return false
	if player.global_position.distance_to(vector(layout.skylight)) >= 3:
		return false
	for enemy in enemies:
		if enemy.state != enemy.State.DEFEATED:
			return false
	lifecycle = Lifecycle.COMPLETE
	player.controls_enabled = false
	hud.text = "BANK ROOFTOP COMPLETE   |   Health %d" % player.health
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	prompt.text = "MISSION COMPLETE\nEnter / A returns to the main menu"
	lifecycle_changed.emit(lifecycle)
	return true

func recover() -> void:
	if lifecycle != Lifecycle.GAMEPLAY_ACTIVE:
		return
	player.release_anchor()
	player.global_position = spawn
	player.velocity = Vector3.ZERO
	player.health = 100

func _exit_tree() -> void:
	Dialogue.release_cinematic(self)
	Dialogue.speech.stream_paused = false
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
