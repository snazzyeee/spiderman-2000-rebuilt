extends MotorState

func enter() -> void:
	context.motor.velocity = Vector3.ZERO

func step(_delta: float) -> void:
	context.motor.velocity = Vector3.ZERO
