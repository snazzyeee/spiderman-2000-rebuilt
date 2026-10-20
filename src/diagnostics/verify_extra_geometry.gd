extends SceneTree

# Separate observation world from units.md; never substitutes for the original gate.
var world := Node3D.new()

func _initialize() -> void:
	call_deferred("run")

func supported(point: Vector3) -> bool:
	return not world.get_world_3d().direct_space_state.intersect_ray(
		PhysicsRayQueryParameters3D.create(point+Vector3.UP*0.3,point-Vector3.UP*1.5,1)).is_empty()

func run() -> void:
	root.add_child(world)
	var manifest: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://staging/reference/landmark_geometry/manifest.json"))
	var loaded: Array = []
	for record in manifest.records:
		if record.status != "converted": continue
		var document := GLTFDocument.new()
		var state := GLTFState.new()
		if document.append_from_file(record.file,state) != OK:
			push_error("Diagnostic GLB decode failed: "+record.file)
			quit(1)
			return
		var scene := document.generate_scene(state)
		world.add_child(scene)
		scene.scale = Vector3.ONE*PsxUnits.METERS_PER_MODEL_UNIT
		for mesh in scene.find_children("*","MeshInstance3D",true,false):
			if mesh.mesh: mesh.create_trimesh_collision()
		loaded.append({"bank":record.bank,"references":record.references,"glb_sha256":record.glb_sha256})
	await physics_frame
	await physics_frame
	var graph: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://levels/L1A1/trg_graph.json"))
	var positions: Array[Vector3] = []
	for node in graph.nodes:
		if node.type in ["RESTART","BADDY","POWERUP","POINT"] and node.has("position_world"):
			positions.append(Vector3(node.position_world[0],node.position_world[1],node.position_world[2]))
	var scores := {}
	var best := ""
	for ratio in [0.25,0.5,1.0,2.0,4.0,8.0]:
		for x in [1,-1]:
			for y in [1,-1]:
				for z in [1,-1]:
					var hits := 0
					for position in positions:
						if supported(position*Vector3(x,y,z)*PsxUnits.METERS_PER_WORLD_UNIT/ratio): hits+=1
					var key := "%d,%d,%d/%s" % [x,y,z,ratio]
					scores[key] = float(hits)/maxi(1,positions.size())
					if best == "" or scores[key] > scores[best]: best=key
	var original: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://staging/validation/units-landmark.json"))
	var report := {"original_gate_passed":original.passed,"original_failures":original.failures,
		"diagnostic_only":true,"interpretation":"Following-area references do not establish simultaneous geometry residency",
		"loaded":loaded,"landmarks":positions.size(),"scores":scores,"best":best,"configured_support":scores.get("1,-1,-1/1.0",0.0)}
	var file := FileAccess.open("res://staging/validation/units-extra-geometry.json",FileAccess.WRITE)
	file.store_string(JSON.stringify(report,"  "))
	file.close()
	print("EXTRA_GEOMETRY ",JSON.stringify({"original_gate_passed":original.passed,"banks":loaded.size(),"best":best,"configured_support":report.configured_support}))
	world.queue_free()
	await process_frame
	quit(0)
