extends SceneTree

# Verify PsxUnits against shared vectors, then against a Level 1 landmark:
# L1A1 TRG restart/baddy/powerup/point positions must sit on the original l1a1
# geometry under the configured scale and axis signs, and every other axis-sign
# hypothesis must fit clearly worse. Writes staging/validation/units-landmark.json.
const VECTORS := "res://tools/reference/tests/data/units_vectors.json"
const GRAPH := "res://levels/L1A1/trg_graph.json"
const LANDMARK_TYPES := ["RESTART", "BADDY", "POWERUP", "POINT"]
const SUPPORT_ABOVE := 1.5   # meters a node may float above the surface below it
const SUPPORT_BELOW := 0.3   # meters a node may sit below a surface (feet/root slack)
# Hypothesis under test: TRG positions share the converted model's units.
const CONFIGURED_RATIO := 1.0
var world := Node3D.new()
var failures: Array[String] = []

func _initialize() -> void:
	call_deferred("run")

func check(condition: bool, label: String) -> void:
	print("CHECK ", "PASS " if condition else "FAIL ", label)
	if not condition:
		failures.append(label)

func near(a: float, b: float, tolerance: float) -> bool:
	return absf(a-b) <= tolerance

func check_vectors() -> void:
	var vectors: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(VECTORS))
	var tolerance: float = vectors.tolerance
	for row in vectors.fixed_to_float:
		check(near(PsxUnits.fixed_to_float(int(row[0])), row[1], tolerance), "fixed_to_float %s" % row[0])
	for row in vectors.angle_to_radians:
		check(near(PsxUnits.angle_to_radians(int(row[0])), row[1], tolerance), "angle_to_radians %s" % row[0])
	for row in vectors.wrap_angle_delta:
		check(PsxUnits.wrap_angle_delta(int(row[0]), int(row[1])) == int(row[2]), "wrap_angle_delta %s" % [row])
	for row in vectors.world_to_godot:
		var value := PsxUnits.world_to_godot(Vector3(row[0][0], row[0][1], row[0][2]))
		check(value.distance_to(Vector3(row[1][0], row[1][1], row[1][2])) <= tolerance, "world_to_godot %s" % [row[0]])
	for row in vectors.fixed_world_to_godot:
		var value := PsxUnits.fixed_world_to_godot(Vector3i(row[0][0], row[0][1], row[0][2]))
		check(value.distance_to(Vector3(row[1][0], row[1][1], row[1][2])) <= tolerance, "fixed_world_to_godot %s" % [row[0]])

func supported(point: Vector3) -> bool:
	var query := PhysicsRayQueryParameters3D.create(point+Vector3.UP*SUPPORT_BELOW, point-Vector3.UP*SUPPORT_ABOVE, 1)
	return not world.get_world_3d().direct_space_state.intersect_ray(query).is_empty()

func run() -> void:
	check_vectors()
	root.add_child(world)
	for path in ["res://assets/environments/l1a1_g.glb", "res://assets/environments/l1a1_o.glb"]:
		var model := RevampAssets.instantiate_model(path)
		if not model:
			check(false, "landmark geometry present: "+path)
			finish({})
			return
		world.add_child(model)
		model.scale = Vector3.ONE*PsxUnits.METERS_PER_MODEL_UNIT
		for mesh in model.find_children("*", "MeshInstance3D", true, false):
			if mesh.mesh:
				mesh.create_trimesh_collision()
	if not FileAccess.file_exists(GRAPH):
		check(false, "L1A1 trg_graph.json present (run tools/reference/export_trg_graph.py)")
		finish({})
		return
	await physics_frame
	await physics_frame
	var graph: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(GRAPH))
	var landmarks: Array[Vector3] = []
	for node in graph.nodes:
		if node.type in LANDMARK_TYPES and node.has("position_world"):
			landmarks.append(Vector3(node.position_world[0], node.position_world[1], node.position_world[2]))
	var geometry := AABB()
	for model in world.get_children():
		var box: AABB = RevampAssets.bounds_of(model)
		box = AABB(box.position*model.scale, box.size*model.scale)
		geometry = box if geometry.size == Vector3.ZERO else geometry.merge(box)
	# Collision sanity: full-height vertical rays over the geometry's own footprint must hit.
	var grid_hits := 0
	for i in range(5):
		for j in range(5):
			var x := lerpf(geometry.position.x, geometry.end.x, (i+.5)/5.0)
			var z := lerpf(geometry.position.z, geometry.end.z, (j+.5)/5.0)
			var probe := PhysicsRayQueryParameters3D.create(Vector3(x, geometry.end.y+5, z), Vector3(x, geometry.position.y-5, z), 1)
			if not world.get_world_3d().direct_space_state.intersect_ray(probe).is_empty():
				grid_hits += 1
	# Rooftops cover a small part of the footprint, so any hit proves collision is live.
	check(grid_hits >= 1, "landmark geometry collision is live (%d/25 grid rays hit)" % grid_hits)
	var configured_bounds := AABB(PsxUnits.world_to_godot(landmarks[0]), Vector3.ZERO) if landmarks else AABB()
	for landmark in landmarks:
		configured_bounds = configured_bounds.expand(PsxUnits.world_to_godot(landmark))
	# Hypotheses: axis signs x power-of-two ratio between TRG units and model units.
	var scores := {}
	var best := ""
	for ratio in [0.25, 0.5, 1.0, 2.0, 4.0, 8.0]:
		for sx in [1, -1]:
			for sy in [1, -1]:
				for sz in [1, -1]:
					var signs := Vector3(sx, sy, sz)
					var hits := 0
					for landmark in landmarks:
						if supported(landmark*signs*PsxUnits.METERS_PER_WORLD_UNIT/ratio):
							hits += 1
					var key := "%d,%d,%d/%s" % [sx, sy, sz, ratio]
					scores[key] = float(hits)/maxf(1.0, landmarks.size())
					if best == "" or scores[key] > scores[best]:
						best = key
	var configured := "%d,%d,%d/%s" % [PsxUnits.AXIS_SIGNS.x, PsxUnits.AXIS_SIGNS.y, PsxUnits.AXIS_SIGNS.z, CONFIGURED_RATIO]
	var runner_up := 0.0
	for key in scores:
		if key != best:
			runner_up = maxf(runner_up, scores[key])
	check(landmarks.size() >= 50, "at least 50 L1A1 landmark nodes")
	check(best == configured, "configured axis signs fit the landmark best")
	check(scores.get(configured, 0.0) >= 0.8, "at least 80% of landmarks are supported by geometry")
	check(scores.get(configured, 0.0)-runner_up >= 0.3, "configured mapping beats every alternative by 30 points")
	finish({"landmarks": landmarks.size(), "scores": scores,
		"geometry_bounds_m": [geometry.position, geometry.end].map(func(v): return [v.x, v.y, v.z]),
		"configured_landmark_bounds_m": [configured_bounds.position, configured_bounds.end].map(func(v): return [v.x, v.y, v.z]), "best": best, "configured": configured, "runner_up": runner_up,
		"meters_per_world_unit": PsxUnits.METERS_PER_WORLD_UNIT, "support_window_m": [SUPPORT_BELOW, SUPPORT_ABOVE]})

func finish(details: Dictionary) -> void:
	var report := {"passed": failures.is_empty(), "failures": failures, "details": details}
	DirAccess.make_dir_recursive_absolute("res://staging/validation")
	var file := FileAccess.open("res://staging/validation/units-landmark.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "  "))
	file.close()
	print("UNITS_LANDMARK ", JSON.stringify(report))
	world.queue_free()
	await process_frame
	quit(0 if failures.is_empty() else 1)
