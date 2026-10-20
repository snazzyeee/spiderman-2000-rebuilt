extends MotorState

var u := Vector3.DOWN
var v := Vector3.RIGHT
var w := Vector3.FORWARD
var down := Vector3.DOWN
var across := Vector3.FORWARD
var theta := 0.0
var angular_velocity := 0.0

func attach(heading: Vector3) -> void:
	var motor = context.motor
	u = (motor.global_position-motor.anchor).normalized()
	v = u.cross(heading).normalized()
	if v.length_squared() < 0.5:
		v = u.cross(Vector3.RIGHT if absf(u.x) < 0.9 else Vector3.FORWARD).normalized()
	w = u.cross(v).normalized()
	down = Vector3.DOWN.slide(v).normalized()
	if down.length_squared() < 0.5:
		down = u
	across = v.cross(down).normalized()
	theta = atan2(u.dot(across), u.dot(down))
	var tangent := -down*sin(theta)+across*cos(theta)
	angular_velocity = motor.velocity.dot(tangent)/motor.rope_length
	context.phase = 0.0
	motor.velocity = tangent*angular_velocity*motor.rope_length

func step(delta: float) -> void:
	var motor = context.motor
	if motor.anchor_body != null and (not is_instance_valid(motor.anchor_body) or not motor.anchor_body.is_inside_tree() or motor.anchor_body.collision_layer & 1 == 0):
		motor.release_anchor()
		return
	var tuning := context.swing
	var line := context.ray(motor.global_position, motor.anchor)
	if not line.is_empty() and line.position.distance_to(motor.anchor) > tuning.sight_tolerance:
		motor.release_anchor()
		return
	var pump := context.direction.dot(across)*tuning.pump
	angular_velocity += (-(tuning.gravity/motor.rope_length)*sin(theta)+pump-tuning.damping*angular_velocity)*delta
	theta += angular_velocity*delta
	var radial := down*cos(theta)+across*sin(theta)
	var tangent := -down*sin(theta)+across*cos(theta)
	motor.velocity = tangent*angular_velocity*motor.rope_length
	var target: Vector3 = motor.anchor+radial*motor.rope_length
	var collision: KinematicCollision3D = motor.move_and_collide(target-motor.global_position)
	context.phase = minf(1.0, context.phase+delta*tuning.phase_per_second)
	motor.scrub_swing_animation(context.phase)
	if collision:
		# Recompute the tangent at the actual swept stopping point.
		motor.velocity = motor.velocity.slide((motor.global_position-motor.anchor).normalized())
		motor.release_anchor()
	elif context.phase >= 1.0:
		motor.release_anchor()
	else:
		motor.draw_rope()

func leave() -> void:
	if context.motor.velocity.length_squared() > 0.0:
		context.motor.velocity += context.motor.velocity.normalized()*context.swing.release_boost
