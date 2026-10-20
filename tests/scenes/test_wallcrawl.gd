extends "res://tests/scenes/traversal_fixture.gd"

func run() -> Array:
	var motor := motor_at(Vector3.ZERO)
	var ceiling30 := box_at(Vector3(0,5,0),Vector3(6,0.2,6),deg_to_rad(30))
	var ceiling40 := box_at(Vector3(20,5,0),Vector3(6,0.2,6),deg_to_rad(40))
	await get_tree().physics_frame
	var hit30 := motor.context.ray(Vector3(0,2,0),Vector3(0,7,0))
	var hit40 := motor.context.ray(Vector3(20,2,0),Vector3(20,7,0))
	check(not hit30.is_empty() and motor.try_ceiling_attach(hit30,true,true), "live 30-degree ceiling attaches")
	check(motor.state == motor.State.CEILING and motor.context.last_event == "ceiling_attach", "ceiling attach state and semantic binding")
	motor.release_anchor()
	motor.position = Vector3(20,0,0)
	check(not hit40.is_empty() and not motor.try_ceiling_attach(hit40,true,true), "live 40-degree ceiling rejects")
	motor.position = Vector3.ZERO
	check(not motor.try_ceiling_attach(hit30,false,true), "downward motion prevents ceiling attach")
	check(not motor.try_ceiling_attach(hit30,true,false), "stale contact prevents ceiling attach")
	check(not motor.try_ceiling_attach({},true,true), "missing contact record prevents ceiling attach")
	ceiling30.set_meta("no_stick",true)
	check(not motor.try_ceiling_attach(hit30,true,true), "no-stick prevents ceiling attach")
	ceiling30.set_meta("no_stick",false)
	ceiling30.set_meta("crawl_allowed",false)
	check(not motor.try_ceiling_attach(hit30,true,true), "surface permission prevents ceiling attach")
	ceiling30.set_meta("crawl_allowed",true)
	motor.last_animation = motor.semantic_bindings.ceiling_current
	check(motor.try_ceiling_attach(hit30,true,true) and motor.context.last_event == "ceiling_alternate", "conditional ceiling semantic binding selected")
	var wall := box_at(Vector3(40,1,0),Vector3(0.1,4,8))
	motor.release_anchor()
	motor.position = Vector3(38,0,0)
	await get_tree().physics_frame
	var contact := motor.context.ray(Vector3(38,1,0),Vector3(42,1,0))
	check(not motor.try_wall_attach(contact,false), "wall attach requires explicit input (authored deviation)")
	check(motor.try_wall_attach(contact,true), "explicit input attaches to live wall")
	check(motor.is_wall(Vector3(0,-0.830,0)) and motor.is_wall(Vector3(0,0.635,0)) and not motor.is_wall(Vector3.UP), "Godot wall classification uses recovered range")
	motor.release_anchor()
	motor.context.direction = Vector3.RIGHT
	for i in range(36):
		motor.update_wall_push(1.0/60.0,contact)
	check(motor.state != motor.State.STUNNED, "0.60 seconds pushing does not bump")
	for i in range(6):
		motor.update_wall_push(1.0/60.0,contact)
	check(motor.state == motor.State.STUNNED and motor.context.last_event == "wall_bump", "0.70 seconds pushing bumps instead of attaching")
	motor.context.direction = Vector3.ZERO
	motor.update_wall_push(0.1,contact)
	check(motor.context.wall_push_seconds == 0.0, "release resets wall push counter")
	var ledge := box_at(Vector3(70,-0.1,0),Vector3(4,0.2,4))
	motor.position = Vector3(70,0.1,0)
	motor.velocity = Vector3.ZERO
	motor.context.direction = Vector3.ZERO
	motor.context.jump_pressed = false
	motor.change_state(motor.State.GROUNDED)
	for i in range(10):
		await get_tree().physics_frame
		motor.states[motor.State.GROUNDED].step(1.0/60.0)
	motor.position.x += 5.0
	motor.context.ground_missing_seconds = 0.0
	for i in range(7):
		await get_tree().physics_frame
		motor.states[motor.State.GROUNDED].step(1.0/60.0)
	check(motor.state == motor.State.GROUNDED, "ledge loss retains state before four original frames")
	await get_tree().physics_frame
	motor.states[motor.State.GROUNDED].step(1.0/60.0)
	check(motor.state == motor.State.AIRBORNE and motor.context.last_event == "ground_loss", "ledge loss falls after authored grace and binds recovered event")
	motor.visual = Node3D.new()
	motor.add_child(motor.visual)
	for mode in [motor.State.CRAWLING,motor.State.CEILING]:
		motor.change_state(mode)
		motor.context.surface_normal = Vector3.LEFT if mode == motor.State.CRAWLING else Vector3.DOWN
		motor.visual.rotation.z = PI/2
		motor.knock_off()
		check(motor.state == motor.State.STUNNED and motor.context.last_event == "knock_off", "knock-off state and binding %d" % mode)
		for i in range(120):
			motor.restore_upright(1.0/60.0)
		check(motor.visual.basis.y.dot(Vector3.UP) > 0.999 and motor.visual.basis.determinant() > 0, "knock-off upright without orientation flip %d" % mode)
	ledge.queue_free()
	wall.queue_free()
	ceiling30.queue_free()
	ceiling40.queue_free()
	return checks
