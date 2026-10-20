extends "res://tests/scenes/traversal_fixture.gd"

func ring_clear(motor: SpiderPlayer, normal: Vector3) -> bool:
	var zip = motor.states[motor.State.ZIPPING]
	var origin := motor.global_position+normal*motor.movement.zip_clearance
	for direction: Vector3 in zip.last_probes:
		if not motor.context.ray(origin,origin+direction*motor.movement.zip_ring_radius).is_empty():
			return false
	return true

func run() -> Array:
	var motor := motor_at(Vector3.ZERO)
	for mode in [0,1]:
		motor.position = Vector3(mode*60,0,0)
		var hit := {"position":motor.position+Vector3.FORWARD*8,"normal":Vector3.UP}
		check(motor.begin_zip(mode,hit), "zip mode %d begins" % mode)
		var zip = motor.states[motor.State.ZIPPING]
		var target: Vector3 = zip.destination
		var steps := 0
		var fixed_speed := true
		while motor.state == motor.State.ZIPPING and steps < 300:
			var before := motor.position
			zip.step(1.0/60.0)
			if motor.state == motor.State.ZIPPING:
				fixed_speed = fixed_speed and absf(before.distance_to(motor.position)*60-motor.movement.zip_speed) < 0.001
			steps += 1
		check(motor.position.distance_to(target) <= 0.25, "zip mode %d arrives within 0.25m" % mode)
		check(fixed_speed, "zip mode %d uses fixed speed" % mode)
		check(zip.last_probes.size() == 8, "zip mode %d runs all eight landing probes" % mode)
		var tangent := true
		var angles := true
		for i in range(8):
			tangent = tangent and absf(zip.last_probes[i].dot(zip.landing_normal)) < 0.00001
			angles = angles and absf(zip.last_probes[i].dot(zip.last_probes[(i+1)%8])-cos(PI/4)) < 0.00001
		check(tangent and angles, "landing ring has 45-degree spacing in surface tangent plane")
	motor.position = Vector3(120,0,0)
	var wall := box_at(Vector3(120,1,-2),Vector3(10,5,0.1))
	await get_tree().physics_frame
	motor.begin_zip(0,{"position":Vector3(120,0,-8),"normal":Vector3.UP})
	var zip = motor.states[motor.State.ZIPPING]
	zip.step(0.5)
	check(motor.state == motor.State.AIRBORNE and motor.position.z > -1.95, "maximum speed swept zip stops at first 0.1m wall")
	check(unpenetrated(motor), "thin-wall zip leaves no body penetration")
	wall.queue_free()
	await get_tree().physics_frame
	var pillar := box_at(Vector3(150,1,0),Vector3(0.5,5,0.5))
	var corner_x := box_at(Vector3(154,1,0),Vector3(0.1,5,8))
	var corner_z := box_at(Vector3(150,1,4),Vector3(8,5,0.1))
	await get_tree().physics_frame
	for point in [Vector3(150.65,0,0.65),Vector3(153.5,0,3.5)]:
		motor.position = point
		zip.tidy_landing(Vector3.UP)
		check(unpenetrated(motor) and ring_clear(motor,Vector3.UP), "pillar/corner cleanup leaves body and all eight probes clear")
	pillar.queue_free()
	corner_x.queue_free()
	corner_z.queue_free()
	return checks
