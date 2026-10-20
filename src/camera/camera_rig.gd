class_name TraversalCameraRig
extends Node3D

var director := CameraDirector.new()
var pivot := Node3D.new()
var spring := SpringArm3D.new()
var camera := Camera3D.new()
var resources: Dictionary = {}
var owner_body: CharacterBody3D

func setup(body: CharacterBody3D) -> void:
	owner_body = body
	var profiles: Array = []
	for key in ["floor", "wall", "ceiling", "swing", "falling"]:
		var profile: CameraProfile = load("res://src/camera/profiles/"+key+".tres")
		profiles.append(profile)
		resources[profile.mode] = profile
	add_child(director)
	director.configure(profiles)
	add_child(pivot)
	pivot.add_child(spring)
	spring.add_child(camera)
	spring.collision_mask = 1
	spring.add_excluded_object(body.get_rid())
	camera.current = true
	update_view(0.0, 0.0, 0.0)

func update_view(delta: float, yaw: float, pitch: float) -> void:
	director.advance(delta)
	var profile: CameraProfile = resources[director.active_mode]
	var p: Array[float] = director.current
	pivot.position = Vector3.UP*profile.pivot_height+PsxUnits.world_to_godot(Vector3(p[0], p[1], p[2]))
	pivot.rotation = Vector3(pitch, yaw, 0)
	spring.position.y = -p[4]*PsxUnits.METERS_PER_WORLD_UNIT
	var sphere := SphereShape3D.new()
	sphere.radius = profile.sphere_radius
	spring.shape = sphere
	spring.margin = profile.margin
	camera.fov = profile.fov
	camera.near = profile.near_plane
	var length := maxf(profile.margin, absf(p[3])*PsxUnits.METERS_PER_WORLD_UNIT)
	var origin := spring.global_position
	# Authored left/right/back whiskers supplement the recovered probe categories.
	for local in [Vector3.LEFT*profile.whisker_side, Vector3.RIGHT*profile.whisker_side, Vector3.BACK*profile.whisker_back]:
		var direction: Vector3 = pivot.global_basis*local
		var hit := owner_body.get_world_3d().direct_space_state.intersect_ray(
			PhysicsRayQueryParameters3D.create(origin, origin+direction, 1, [owner_body.get_rid()]))
		if not hit.is_empty():
			length = minf(length, maxf(profile.margin, origin.distance_to(hit.position)-profile.sphere_radius))
	spring.spring_length = length
