extends MotorState

func step(delta: float) -> void:
	var motor = context.motor
	var t := context.movement
	motor.velocity.x = move_toward(motor.velocity.x, context.direction.x*t.speed, t.acceleration*delta)
	motor.velocity.z = move_toward(motor.velocity.z, context.direction.z*t.speed, t.acceleration*delta)
	var rising: bool = motor.velocity.y >= 0.0
	motor.velocity.y -= t.gravity*delta
	motor.move_and_slide()
	if motor.try_ceiling_contacts(rising):
		return
	if motor.is_on_floor():
		context.ground_missing_seconds = 0.0
		context.change(motor.State.GROUNDED)

