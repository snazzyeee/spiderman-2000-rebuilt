extends Node3D

var checks: Array = []

func check(ok: bool, label: String) -> void:
	checks.append({"passed": ok, "label": label})

func run() -> Array:
	var director := CameraDirector.new()
	add_child(director)
	var profiles: Array = []
	for key in ["floor", "wall", "ceiling", "swing", "falling"]:
		profiles.append(load("res://src/camera/profiles/"+key+".tres"))
	director.configure(profiles)
	director.SetSpideyCamValue(0, 3, 848, 32, true)
	for frame in range(32):
		director.advance(1.0/30.0)
		check(absf(director.current[3]-(528.0+10.0*(frame+1))) < 0.00001, "32-frame linear blend frame %d" % frame)
	check(director.blends.is_empty(), "32-frame blend completes exactly")
	director.SetSpideyCamValue(0, 3, 600, 0, true)
	check(director.current[3] == 600, "zero frames snaps")
	director.SetSpideyCamValue(0, 3, 900, 0, false)
	check(director.current[3] == 600 and director.profiles[0][3] == 900, "apply_now false stores only")
	director.SetSpideyCamValue(1, 3, 1234, 0, true)
	check(director.current[3] == 600, "inactive apply_now stores only")
	check(not director.SetSpideyCamValue(3, 0, 1, 0, true) and director.rejected.size() == 1, "mode 3 rejected and logged")
	check(not director.SetSpideyCamValue(0, 5, 1, 0, true), "invalid parameter rejected")
	check(not director.SetSpideyCamValue(0, 0, 32768, 0, true), "signed 16-bit value bounded")
	director.SetSpideyCamValue(0, 0, -123, 0, true)
	check(director.current[0] == -123, "signed value preserved")
	director.SetSpideyCamValue(0, 0, 100, 32, true)
	director.SetSpideyCamValue(0, 4, 200, 32, true)
	director.advance(16.0/30.0)
	check(absf(director.current[0]+11.5) < 0.00001 and director.current[4] == 100, "parameters blend independently")
	for mode in [0,1,2,4,5]:
		director.select(mode)
		check(director.active_mode == mode and director.current == director.profiles[mode], "select profile %d" % mode)
	var wall := StaticBody3D.new()
	var shape := CollisionShape3D.new()
	var box := BoxShape3D.new()
	box.size = Vector3(20,20,0.1)
	shape.shape = box
	wall.add_child(shape)
	wall.position.z = 2
	add_child(wall)
	var body := CharacterBody3D.new()
	add_child(body)
	var rig := TraversalCameraRig.new()
	body.add_child(rig)
	rig.setup(body)
	await get_tree().physics_frame
	for mode in [0,1,2,4,5]:
		rig.director.select(mode)
		for frame in range(3):
			rig.update_view(1.0/60.0, 0, 0)
			await get_tree().physics_frame
		var ray := PhysicsRayQueryParameters3D.create(rig.pivot.global_position, rig.camera.global_position, 1)
		check(get_world_3d().direct_space_state.intersect_ray(ray).is_empty(), "camera-to-pivot clear in profile %d" % mode)
		check(rig.camera.global_position.z < 1.95, "sphere spring prevents penetration %d" % mode)
	return checks
