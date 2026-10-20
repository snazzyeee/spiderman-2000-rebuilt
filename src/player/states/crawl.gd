extends MotorState

var ceiling := false

func step(delta: float) -> void:
	var motor = context.motor
	var t := context.crawl
	if context.jump_pressed:
		motor.knock_off()
		return
	var origin: Vector3 = motor.global_position+context.movement.body_center+context.surface_normal*t.body_clearance
	var surface := context.ray(origin, origin-context.surface_normal*t.probe_length)
	var forward: Vector3 = context.direction.slide(context.surface_normal).normalized()
	if surface.is_empty() and forward != Vector3.ZERO:
		surface = probe_edges(origin, forward)
	if surface.is_empty() or not motor.surface_allows_crawl(surface):
		context.ground_missing_seconds += delta
		if context.ground_missing_seconds+0.0000001 >= context.original_seconds(t.grace_frames):
			context.event("ground_loss")
			context.change(motor.State.AIRBORNE)
		return
	context.ground_missing_seconds = 0.0
	context.surface_normal = surface.normal
	motor.wall_normal = surface.normal
	var next: int = motor.State.CEILING if -surface.normal.y > t.ceiling_threshold else motor.State.CRAWLING
	if next != motor.state:
		context.change(next)
	var up := Vector3.UP.slide(surface.normal).normalized()
	if up.length_squared() < 0.5:
		up = Basis(Vector3.UP, motor.yaw)*Vector3.FORWARD
	var across := up.cross(surface.normal).normalized()
	motor.velocity = (across*context.input_vector.x-up*context.input_vector.y)*t.speed
	motor.move_and_collide(motor.velocity*delta)
	motor.orient_visual(surface.normal, up, delta)

func probe_edges(origin: Vector3, forward: Vector3) -> Dictionary:
	var t := context.crawl
	var normal := context.surface_normal
	var probes := [
		[origin, origin+forward*t.interior_reach],
		[origin+forward*t.exterior_reach-normal*t.exterior_reach, origin-forward*t.exterior_reach-normal*t.exterior_reach],
		[origin-normal*t.fence_reach+forward*t.fence_reach, origin+normal*t.fence_reach+forward*t.fence_reach]]
	for endpoints in probes:
		var hit := context.ray(endpoints[0], endpoints[1])
		if not hit.is_empty():
			return hit
	return {}
