extends SceneTree
var checks: Array = []
var skips: Array[String] = []
func _initialize() -> void: call_deferred("run")
func check(value: bool,label: String) -> void:
	checks.append({"passed":value,"label":label})
	print("LOADING_TEST ","PASS " if value else "FAIL ",label)
func run() -> void:
	var mode := MenuAudio.get_mode()
	MenuAudio.set_mode("Off",false)
	var training: bool = root.get_node("GameSession").training
	var checkpoint: int = root.get_node("GameSession").resume_checkpoint
	var menu: Node = load("res://scenes/main_menu.tscn").instantiate()
	root.add_child(menu)
	current_scene = menu
	await create_timer(.05).timeout
	menu.begin_launch(true,false,"res://src/diagnostics/fixtures/loading_destination.tscn")
	check(menu.loading and menu.current_id == "loading","launch exposes loading screen before resource request completes")
	check(menu.view.nodes.has("art") and menu.view.layout.elements.size() >= 2,"loading view comes from shared layout data")
	if ResourceLoader.exists("res://assets/ui/frontend/L1Cov.png"):
		check(menu.view.nodes.art.texture != null,"loading uses privately converted original level cover")
	else:
		skips.append("Private level cover absent; original loading art check skipped")
	var pause_view := LayoutScreen.new()
	root.add_child(pause_view)
	pause_view.build("pause",FrontendSettings.get_value("style"),true)
	var events: Array[String] = []
	for screen in FrontendSettings.document().screens:
		if screen.id == "pause":
			for action in screen.actions: pause_view.action_button(action[0],func(): events.append(action[1]))
	check(pause_view.nodes.content.get_child_count() == 3,"pause action count is data-driven")
	pause_view.nodes.content.get_child(0).pressed.emit()
	check(events == ["resume_game"],"pause action callback routes resume")
	pause_view.queue_free()
	for frame in range(120):
		await process_frame
		if is_instance_valid(current_scene) and current_scene.name == "SyntheticLoadingDestination": break
	check(is_instance_valid(current_scene) and current_scene.name == "SyntheticLoadingDestination","threaded loading reaches synthetic destination")
	check(root.get_node("GameSession").training,"launch applies practice state before scene change")
	root.get_node("GameSession").training = training
	root.get_node("GameSession").resume_checkpoint = checkpoint
	MenuAudio.set_mode(mode,false)
	var dialogue := root.get_node("Dialogue")
	dialogue.music.stop();dialogue.music.stream = null
	await create_timer(.1).timeout
	var failed := checks.filter(func(row): return not row.passed)
	var report := {"passed":failed.is_empty(),"checks":checks,"skips":skips,"renderer":RenderingServer.get_current_rendering_method(),"display_driver":DisplayServer.get_name()}
	var file := FileAccess.open("res://staging/validation/loading-tests.json",FileAccess.WRITE)
	file.store_string(JSON.stringify(report,"  "));file.close()
	quit(0 if failed.is_empty() else 1)
