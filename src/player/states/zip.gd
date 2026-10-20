extends MotorState

enum Mode { ZIP_TARGET, ZIP_FORWARD }
var mode := Mode.ZIP_TARGET
var destination := Vector3.ZERO
var landing_normal := Vector3.UP
var last_probes: Array[Vector3] = []

func step(delta: float) -> void:
	var motor = context.motor
	var offset: Vector3 = destination-motor.global_position
	var distance := minf(context.movement.zip_speed*delta, offset.length())
	motor.velocity = offset.normalized()*context.movement.zip_speed
	var collision: KinematicCollision3D = motor.move_and_collide(offset.normalized()*distance)
	if collision or distance >= offset.length():
		if collision:
			landing_normal = collision.get_normal()
		tidy_landing(landing_normal)
		motor.velocity = Vector3.ZERO
		context.change(motor.State.AIRBORNE)

func tidy_landing(normal: Vector3) -> void:
	var motor = context.motor
	var radius := context.movement.zip_ring_radius
	var tangent := normal.cross(Vector3.UP if absf(normal.y) < 0.9 else Vector3.RIGHT).normalized()
	var bitangent := normal.cross(tangent).normalized()
	last_probes.clear()
	for index in range(8):
		var angle := PsxUnits.angle_to_radians(index*PsxUnits.ANGLE_FULL_TURN/8)
		var direction := tangent*cos(angle)+bitangent*sin(angle)
		last_probes.append(direction)
		var origin: Vector3 = motor.global_position+normal*context.movement.zip_clearance
		var hit := context.ray(origin, origin+direction*radius)
		if not hit.is_empty():
			motor.move_and_collide(hit.normal*radius)
