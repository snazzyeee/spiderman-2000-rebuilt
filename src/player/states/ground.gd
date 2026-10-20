extends MotorState

func enter() -> void:
	context.last_event = ""

func step(delta: float) -> void:
	var motor = context.motor
	var t := context.movement
	motor.velocity.x = move_toward(motor.velocity.x, context.direction.x*t.speed, t.acceleration*delta)
	motor.velocity.z = move_toward(motor.velocity.z, context.direction.z*t.speed, t.acceleration*delta)
	if context.jump_pressed:
		motor.velocity.y = t.jump_speed
		context.ground_missing_seconds = 0.0
		context.change(motor.State.AIRBORNE)
	else:
		motor.velocity.y -= t.gravity*delta
	motor.move_and_slide()
	if motor.is_on_floor():
		context.ground_missing_seconds = 0.0
	else:
		context.ground_missing_seconds += delta
		if context.ground_missing_seconds+0.0000001 >= context.original_seconds(context.crawl.grace_frames):
			context.event("ground_loss")
			context.change(motor.State.AIRBORNE)
	motor.update_wall_push(delta)

