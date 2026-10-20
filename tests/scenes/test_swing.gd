extends "res://tests/scenes/traversal_fixture.gd"

func run() -> Array:
	var motor := motor_at(Vector3.ZERO)
	check(not motor.begin_swing({}), "GEOMETRY rejects missing live anchor")
	motor.swing_tuning.anchor_policy = SwingTuning.AnchorPolicy.SKY_HOOK
	motor.velocity = Vector3(0,0,-5)
	var player := AnimationPlayer.new()
	motor.add_child(player)
	motor.animations = player
	var library := AnimationLibrary.new()
	var clip := Animation.new()
	clip.length = 2.0
	library.add_animation(motor.swing_tuning.animation_binding,clip)
	player.add_animation_library("",library)
	check(motor.begin_swing({}), "SKY_HOOK authored anchor attaches")
	var frame = motor.states[motor.State.SWINGING]
	check(absf(frame.u.dot(frame.v)) < 0.00001 and absf(frame.w.length()-1) < 0.00001, "recovered rope frame is orthonormal")
	var radius := motor.rope_length
	var radial_error := 0.0
	var phase_error := 0.0
	var monotonic := true
	var animation_error := 0.0
	var steps := 0
	while motor.state == motor.State.SWINGING and steps < 300:
		var prior := motor.context.phase
		motor._step_swing(1.0/60.0,Vector3.ZERO)
		radial_error = maxf(radial_error,absf(motor.position.distance_to(motor.anchor)-radius))
		monotonic = monotonic and motor.context.phase >= prior
		phase_error = maxf(phase_error,absf(motor.context.animation_phase-motor.context.phase))
		animation_error = maxf(animation_error,absf(player.current_animation_position-motor.context.phase*clip.length))
		steps += 1
	check(radial_error/radius <= 0.01, "rope radius within one percent throughout full arc")
	check(monotonic and phase_error < 0.00001, "normalized phase monotonic and animation follows phase")
	check(animation_error < 0.0001, "actual animation player scrub follows phase")
	check(motor.state == motor.State.AIRBORNE and motor.context.phase == 1.0, "arc ends at phase one without input")
	motor.begin_swing({})
	for i in range(20):
		motor._step_swing(1.0/60.0,Vector3.FORWARD)
	var before := motor.velocity
	motor.release_anchor()
	check(motor.velocity.distance_to(before) <= maxf(0.00001,before.length()*0.02), "release preserves tangent velocity within two percent")
	check(absf(motor.velocity.dot((motor.position-motor.anchor).normalized())) < 0.00001, "release is tangent to rope")
	motor.position = Vector3(30,0,0)
	var roof := box_at(Vector3(30,12,-6),Vector3(4,0.2,4))
	await get_tree().physics_frame
	motor.swing_tuning.anchor_policy = SwingTuning.AnchorPolicy.GEOMETRY
	var hit := motor.context.ray(motor.position+Vector3.UP,roof.position)
	check(not hit.is_empty() and motor.begin_swing(hit), "GEOMETRY accepts live unobstructed synthetic roof")
	motor.release_anchor()
	var blocker := box_at(Vector3(30,6,-3),Vector3(10,0.1,10))
	await get_tree().physics_frame
	check(not motor.begin_swing(hit), "GEOMETRY rejects occluded roof despite valid supplied hit")
	blocker.queue_free()
	roof.queue_free()
	await get_tree().physics_frame
	motor.position = Vector3(60,0,0)
	motor.swing_tuning.anchor_policy = SwingTuning.AnchorPolicy.SKY_HOOK
	motor.velocity = Vector3(0,0,-20)
	motor.begin_swing({})
	var wall := box_at(Vector3(60,0.8,-0.6),Vector3(6,4,0.1))
	await get_tree().physics_frame
	for i in range(60):
		if motor.state != motor.State.SWINGING:
			break
		motor._step_swing(1.0/60.0,Vector3.ZERO)
	check(motor.state == motor.State.AIRBORNE and motor.context.phase < 1.0, "swept collision ends swing before arc end")
	check(unpenetrated(motor), "swept swing stops without penetrating thin wall")
	wall.queue_free()
	return checks
