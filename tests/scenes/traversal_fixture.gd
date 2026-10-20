extends Node3D

var checks: Array = []

func check(ok: bool, label: String) -> void:
	checks.append({"passed":ok,"label":label})
	print("TRAVERSAL ", "PASS " if ok else "FAIL ", label)

func motor_at(point: Vector3) -> SpiderPlayer:
	var motor := SpiderPlayer.new()
	motor.synthetic_visual = true
	motor.movement = motor.movement.duplicate(true)
	motor.swing_tuning = motor.swing_tuning.duplicate(true)
	motor.crawl_tuning = motor.crawl_tuning.duplicate(true)
	add_child(motor)
	motor.position = point
	motor.controls_enabled = true
	motor.set_physics_process(false)
	return motor

func box_at(point: Vector3, size: Vector3, rotation_x: float = 0.0) -> StaticBody3D:
	var body := StaticBody3D.new()
	var collision := CollisionShape3D.new()
	var shape := BoxShape3D.new()
	shape.size = size
	collision.shape = shape
	body.add_child(collision)
	add_child(body)
	body.position = point
	body.rotation.x = rotation_x
	return body

func unpenetrated(motor: SpiderPlayer) -> bool:
	var query := PhysicsShapeQueryParameters3D.new()
	var shape := BoxShape3D.new()
	shape.size = motor.movement.body_size
	query.shape = shape
	query.transform.origin = motor.global_position+motor.movement.body_center
	query.collision_mask = 1
	return get_world_3d().direct_space_state.intersect_shape(query).is_empty()
