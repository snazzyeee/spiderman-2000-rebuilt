extends MotorState

var remaining := 0.0

func enter() -> void:
	remaining = context.movement.stunned_seconds

func step(delta: float) -> void:
	context.motor.velocity.y -= context.movement.gravity*delta
	context.motor.move_and_slide()
	context.motor.restore_upright(delta)
	remaining -= delta
	if remaining <= 0.0:
		context.change(context.motor.State.AIRBORNE)
