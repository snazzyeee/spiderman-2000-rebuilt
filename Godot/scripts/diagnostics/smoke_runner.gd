extends Node

var checks: Array[String] = []
var failures: Array[String] = []

func check(condition: bool, label: String) -> void:
	checks.append(label)
	if not condition:
		failures.append(label)
	print("CHECK ", "PASS " if condition else "FAIL ", label)

func _ready() -> void:
	call_deferred("run")

func capture(name_of_view: String) -> void:
	if DisplayServer.get_name() == "headless":
		return
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png("res://../TestResults/godot-"+name_of_view+".png")

func run() -> void:
	await get_tree().process_frame
	await get_tree().process_frame
	var menu = get_tree().current_scene
	var labels: Array[String] = []
	for child in menu.panel.get_children():
		if child is Button:
			labels.append(child.text)
	check(labels == ["Game", "Options", "Training", "Special", "Gallery"], "five root menu items")
	check(Dialogue.registry.size() == 3661, "all extracted clips loaded")
	var manifest = JSON.parse_string(FileAccess.get_file_as_string("res://assets/models/conversion_manifest.json"))
	check(manifest.errors.is_empty(), "batch character conversion without failures")
	check(manifest.converted.size() >= 100, "complete surveyed model batch exported")
	var skeletons = menu.visual.find_children("*", "Skeleton3D", true, false)
	check(not skeletons.is_empty() and skeletons[0].get_bone_count() == 18, "original 18 bone Spider-Man rig imported")
	var animator := RevampAssets.animation_player(menu.visual)
	check(animator.get_animation_list().size() >= 300, "original animation slots imported")
	await capture("menu")
	menu.show_page("Options")
	check(menu.panel.find_children("*", "HSlider", false, false).size() == 3, "three audio bus sliders")
	menu.show_page("Gallery")
	check(menu.art.texture != null, "original comic texture loaded")
	menu.menu_sound(true)
	check(Dialogue.sfx.stream != null, "original menu confirm audio bound")
	menu.menu_sound(false)
	check(Dialogue.sfx.stream != null, "original menu shift audio bound")
	var configured_music_volume := AudioServer.get_bus_volume_db(AudioServer.get_bus_index("Music"))
	menu.launch(false)
	await get_tree().process_frame
	await get_tree().process_frame
	await get_tree().physics_frame
	var mission = get_tree().current_scene
	check(not mission.missing_assets, "original rooftop meshes and layout loaded")
	var floor_query := PhysicsRayQueryParameters3D.create(mission.spawn+Vector3.UP, mission.spawn-Vector3.UP, 1, [mission.player.get_rid()])
	var floor_hit: Dictionary = mission.get_world_3d().direct_space_state.intersect_ray(floor_query)
	check(not floor_hit.is_empty() and floor_hit.normal.y > .8, "spawn supported by original rooftop collision")
	check(mission.lifecycle == mission.Lifecycle.INTRO_CINEMATIC, "intro lifecycle starts first")
	check(not mission.player.controls_enabled and not mission.hud.visible and mission.prompt.text.is_empty(), "intro blocks controls HUD and briefing")
	check(Dialogue.cinematic_owner == mission and Dialogue.speech.playing, "original narration has exclusive ownership")
	check(not Dialogue.play(Dialogue.find_key("L1A1_01")), "competing objective speech blocked")
	var prior_health: float = mission.player.health
	mission.player.take_damage(50)
	check(mission.player.health == prior_health, "cinematic blocks gameplay damage")
	await capture("intro")
	mission.finish_intro()
	check(mission.lifecycle == mission.Lifecycle.MISSION_BRIEFING and not mission.player.controls_enabled, "intro finished starts separate locked briefing")
	check(mission.prompt.text.begins_with("MISSION BRIEFING"), "authored briefing labeled separately")
	await capture("briefing")
	check(absf(AudioServer.get_bus_volume_db(AudioServer.get_bus_index("Music"))-configured_music_volume) < .01, "dialogue restores configured music volume")
	mission.start_intro()
	mission.set_paused(true)
	check(Dialogue.speech.stream_paused and not mission.player.controls_enabled, "pause retains cinematic audio and control lock")
	mission.set_paused(false)
	Dialogue.speech.seek(maxf(0, Dialogue.speech.stream.get_length()-.04))
	await get_tree().create_timer(.4).timeout
	check(mission.lifecycle == mission.Lifecycle.MISSION_BRIEFING, "natural audio completion starts briefing")
	mission.activate_gameplay()
	check(mission.player.controls_enabled and mission.hud.visible, "briefing completion enables gameplay")
	var start: Vector3 = mission.player.global_position
	Input.action_press("move_forward")
	for step in range(12):
		await get_tree().physics_frame
	Input.action_release("move_forward")
	check(start.distance_to(mission.player.global_position) > .1, "keyboard movement on original rooftop")
	mission.recover()
	check(mission.player.global_position.distance_to(mission.spawn) < .01, "checkpoint recovery")
	check(not mission.player.begin_swing({}), "swing rejects missing anchors")
	mission.player.global_position += Vector3.UP*5.0
	var anchor_hit := find_anchor(mission.player)
	check(not anchor_hit.is_empty(), "raycast finds an original building anchor")
	if not anchor_hit.is_empty():
		check(mission.player.begin_swing(anchor_hit), "swing accepts valid geometry anchor")
		var radius: float = mission.player.rope_length
		var radial_error := 0.0
		var swing_steps := 0
		for step in range(30):
			if mission.player.state != SpiderPlayer.State.SWINGING:
				break
			mission.player._step_swing(1.0/60.0, Vector3.FORWARD)
			swing_steps += 1
			radial_error = maxf(radial_error, absf(mission.player.global_position.distance_to(mission.player.anchor)-radius))
		check(radial_error < .003, "pendulum maintains fixed rope length until collision release")
		check(swing_steps >= 6, "pendulum test advances through actual airborne steps")
		check(absf(mission.player.velocity.dot((mission.player.global_position-mission.player.anchor).normalized())) < .005, "swing velocity remains tangent to rope")
		mission.player.release_anchor()
		check(mission.player.state == SpiderPlayer.State.AIRBORNE, "release retains airborne traversal")
		mission.recover()
	await capture("rooftop")
	for index in range(mission.layout.checkpoints.size()):
		mission.player.global_position = mission.vector(mission.layout.checkpoints[index])
		mission._process(.016)
	check(mission.checkpoint == 3, "ordered rooftop checkpoint progression")
	mission._process(.016)
	check(mission.encounter_started, "bank encounter activates")
	mission.player.global_position = mission.vector(mission.layout.skylight)
	check(not mission.try_enter_skylight() and mission.lifecycle == mission.Lifecycle.GAMEPLAY_ACTIVE, "living enemies gate skylight")
	for enemy in mission.enemies:
		enemy.take_damage(25, true)
	check(mission.enemies[0].state == mission.enemies[0].State.WEBBED_STUN, "web hits enter stun")
	for enemy in mission.enemies:
		enemy.take_damage(100, false)
	check(mission.try_enter_skylight() and mission.lifecycle == mission.Lifecycle.COMPLETE, "defeated encounter unlocks mission completion")
	check(GameSession.load_checkpoint() == 2, "separate automation checkpoint persists")
	await capture("mission")
	var report := {"passed": failures.is_empty(), "checks": checks, "failures": failures, "renderer": RenderingServer.get_current_rendering_method(), "display_driver": DisplayServer.get_name(), "godot": Engine.get_version_info().string}
	var file := FileAccess.open("res://../TestResults/godot-smoke.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "  "))
	file.close()
	print("GODOT_SMOKE ", JSON.stringify(report))
	# Let scene-owned resources and awaited callbacks finish before engine exit.
	get_tree().current_scene.queue_free()
	await get_tree().process_frame
	get_tree().quit(0 if failures.is_empty() else 1)

func find_anchor(player: SpiderPlayer) -> Dictionary:
	var origin := player.global_position+Vector3.UP*2.0
	for elevation in [.2, .4, .65]:
		for direction_index in range(16):
			var angle := float(direction_index)*TAU/16.0
			var direction := Vector3(cos(angle), elevation, sin(angle)).normalized()
			var query := PhysicsRayQueryParameters3D.create(origin, origin+direction*65.0, 1, [player.get_rid()])
			var hit := player.get_world_3d().direct_space_state.intersect_ray(query)
			if not hit.is_empty() and hit.position.y > player.global_position.y+1.2 and hit.position.distance_to(player.global_position) > 3:
				return hit
	return {}
