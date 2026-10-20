extends SceneTree

# Derive an authored rooftop route directly from imported original geometry.
# This runs without Unity and does not pretend to recover original scripting.
var world := Node3D.new()
var candidates: Array[Vector3] = []

func _initialize() -> void:
	call_deferred("run")

func ray(origin: Vector3, destination: Vector3) -> Dictionary:
	return world.get_world_3d().direct_space_state.intersect_ray(PhysicsRayQueryParameters3D.create(origin, destination, 1))

func clear_roof(point: Vector3) -> bool:
	var query := PhysicsShapeQueryParameters3D.new()
	var shape := BoxShape3D.new()
	shape.size = Vector3(.9, 1.6, .9)
	query.shape = shape
	query.transform.origin = point+Vector3.UP
	query.collision_mask = 1
	if not world.get_world_3d().direct_space_state.intersect_shape(query, 1).is_empty():
		return false
	for offset in [Vector3.LEFT, Vector3.RIGHT, Vector3.FORWARD, Vector3.BACK]:
		var hit := ray(point+offset+Vector3.UP*.3, point+offset-Vector3.UP*.6)
		if hit.is_empty() or hit.normal.y < .85:
			return false
	return true

func has_gap(a: Vector3, b: Vector3) -> bool:
	for index in range(1, 6):
		var point := a.lerp(b, index/6.0)
		if ray(point+Vector3.UP*.6, point-Vector3.UP*1.4).is_empty():
			return true
	return false

func coordinates(point: Vector3) -> Array:
	return [point.x, point.y, point.z]

func run() -> void:
	root.add_child(world)
	var level := RevampAssets.instantiate_model("res://assets/environments/l1a1_g.glb")
	var objects := RevampAssets.instantiate_model("res://assets/environments/l1a1_o.glb")
	if not level or not objects:
		quit(1)
		return
	world.add_child(level)
	world.add_child(objects)
	level.scale = Vector3.ONE*PsxUnits.METERS_PER_WORLD_UNIT
	objects.scale = level.scale
	var bank_mesh: MeshInstance3D
	for mesh in level.find_children("*", "MeshInstance3D", true, false):
		mesh.create_trimesh_collision()
	for mesh in objects.find_children("*", "MeshInstance3D", true, false):
		if "00000006" in mesh.mesh.resource_name:
			bank_mesh = mesh
			mesh.create_trimesh_collision()
	if not bank_mesh:
		push_error("Original entry mesh 6 missing")
		quit(1)
		return
	await physics_frame
	await physics_frame
	var bank_bounds: AABB = bank_mesh.global_transform*bank_mesh.get_aabb()
	var bank := bank_bounds.get_center()
	var bank_floor := ray(bank+Vector3.UP, bank-Vector3.UP*2)
	if not bank_floor.is_empty():
		bank.y = bank_floor.position.y+.04
	for mesh in level.find_children("*", "MeshInstance3D", true, false):
		var bounds: AABB = mesh.global_transform*mesh.get_aabb()
		for x in [0.25, 0.5, 0.75]:
			for z in [0.25, 0.5, 0.75]:
				var origin := Vector3(lerpf(bounds.position.x, bounds.end.x, x), bounds.end.y+1, lerpf(bounds.position.z, bounds.end.z, z))
				var hit := ray(origin, origin-Vector3.UP*(bounds.size.y+3))
				if hit.is_empty() or hit.normal.y < .9:
					continue
				var point: Vector3 = hit.position+Vector3.UP*.04
				if not clear_roof(point):
					continue
				var duplicate := false
				for prior in candidates:
					if prior.distance_to(point) < 6:
						duplicate = true
				if not duplicate:
					candidates.append(point)
	var reverse: Array[Vector3] = [bank]
	for stage in range(3):
		var target: Vector3 = reverse.back()
		var selected := Vector3.INF
		var best_score := INF
		for point in candidates:
			var used := false
			for prior in reverse:
				if prior.distance_to(point) < 10:
					used = true
			var distance := Vector2(point.x-target.x, point.z-target.z).length()
			if used or distance < 12 or distance > 58 or absf(point.y-target.y) > 14 or not has_gap(point, target):
				continue
			var score := absf(point.distance_to(target)-27)+maxf(0, point.y-target.y)*2
			if score < best_score:
				best_score = score
				selected = point
		if selected == Vector3.INF:
			push_error("No supported rooftop route could be derived")
			quit(1)
			return
		reverse.append(selected)
	reverse.reverse()
	var points: Array = []
	for point in reverse.slice(1):
		points.append(coordinates(point))
	DirAccess.make_dir_recursive_absolute("res://assets/environments")
	var file := FileAccess.open("res://assets/environments/mission1_layout.json", FileAccess.WRITE)
	file.store_string(JSON.stringify({"spawn": coordinates(reverse[0]), "checkpoints": points, "bank": coordinates(bank), "skylight": coordinates(bank+Vector3.FORWARD*.7), "source": "authored route derived with Godot collision queries from original geometry", "roof_candidates": candidates.size()}, "  "))
	file.close()
	print("GODOT_LAYOUT ", candidates.size(), " supported roof candidates; 3 checkpoints")
	world.queue_free()
	await process_frame
	quit(0)
