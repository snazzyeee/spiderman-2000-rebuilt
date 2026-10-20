extends SceneTree

var checks: Array = []

func _initialize() -> void:
	call_deferred("run")

func run() -> void:
	for name_of_test in ["camera"]:
		var scene: PackedScene = load("res://tests/scenes/test_"+name_of_test+".tscn")
		if not scene:
			checks.append({"label": name_of_test+" scene present", "passed": false})
			continue
		var suite = scene.instantiate()
		root.add_child(suite)
		var results: Array = await suite.run()
		checks.append_array(results)
		suite.queue_free()
		await process_frame
	var failed: Array = checks.filter(func(c): return not c.passed)
	var report := {"passed": failed.is_empty() and not checks.is_empty(), "checks": checks,
		"failures": failed, "renderer": RenderingServer.get_current_rendering_method(),
		"display_driver": DisplayServer.get_name()}
	DirAccess.make_dir_recursive_absolute("res://staging/validation")
	var file := FileAccess.open("res://staging/validation/traversal-tests.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "  "))
	file.close()
	print("TRAVERSAL_TESTS ", JSON.stringify(report))
	quit(0 if report.passed else 1)
