extends CharacterBody3D

signal defeated(enemy: Node3D)
enum State { PATROL, ALERT, MELEE_ATTACK, RANGED_ATTACK, WEBBED_STUN, DEFEATED }
var state := State.PATROL
var player: SpiderPlayer
var active := false
var ranged := false
var health := 90.0
var origin := Vector3.ZERO
var cooldown := 0.0
var stun_left := 0.0
var attack_windup := 0.0
var web_hits := 0

func _ready() -> void:
	add_to_group("enemies")
	collision_layer = 4
	collision_mask = 1|2|4
	origin = global_position
	var shape := CollisionShape3D.new()
	var box := BoxShape3D.new()
	box.size = Vector3(.7, 1.7, .7)
	shape.shape = box
	shape.position.y = .85
	add_child(shape)
	var model := RevampAssets.instantiate_model("res://assets/characters/henchman.glb")
	if model:
		add_child(model)
		RevampAssets.fit_character(model)
		var animation := RevampAssets.animation_player(model)
		if animation:
			var names := animation.get_animation_list()
			for clip in names:
				if clip != "RESET":
					animation.get_animation(clip).loop_mode = Animation.LOOP_LINEAR
					animation.play(clip)
					break

func _physics_process(delta: float) -> void:
	if not active or not is_instance_valid(player) or not player.controls_enabled or state == State.DEFEATED:
		return
	cooldown = maxf(0.0, cooldown-delta)
	velocity.y -= 25.0*delta
	if state == State.WEBBED_STUN:
		stun_left -= delta
		velocity.x = 0
		velocity.z = 0
		if stun_left <= 0:
			state = State.ALERT
		move_and_slide()
		return
	var offset := player.global_position-global_position
	var distance := offset.length()
	if attack_windup > 0:
		attack_windup -= delta
		if attack_windup <= 0 and ((state == State.MELEE_ATTACK and distance < 2.7) or (state == State.RANGED_ATTACK and _visible_player())):
			player.take_damage(12.0 if ranged else 18.0)
		velocity.x = 0
		velocity.z = 0
	elif distance < (18.0 if ranged else 2.2) and cooldown <= 0 and _visible_player():
		state = State.RANGED_ATTACK if ranged else State.MELEE_ATTACK
		attack_windup = .7 if ranged else .45
		cooldown = 1.5
	else:
		state = State.ALERT if distance < 25 else State.PATROL
		var destination := player.global_position if state == State.ALERT else origin+Vector3(sin(Time.get_ticks_msec()/1500.0)*1.5, 0, 0)
		var desired := (destination-global_position).normalized()*3.0
		velocity.x = desired.x
		velocity.z = desired.z
	# Keep simple steering on supported rooftops rather than chasing over ledges.
	var next := global_position+Vector3(velocity.x, 0, velocity.z).normalized()*.65
	var support := PhysicsRayQueryParameters3D.create(next+Vector3.UP, next-Vector3.UP*1.5, 1, [get_rid()])
	if get_world_3d().direct_space_state.intersect_ray(support).is_empty():
		velocity.x = 0
		velocity.z = 0
	move_and_slide()

func _visible_player() -> bool:
	var ray := PhysicsRayQueryParameters3D.create(global_position+Vector3.UP*1.3, player.global_position+Vector3.UP, 1|2, [get_rid()])
	var hit := get_world_3d().direct_space_state.intersect_ray(ray)
	return not hit.is_empty() and hit.collider == player

func take_damage(amount: float, webbed: bool = false) -> void:
	if not active or state == State.DEFEATED or not player.controls_enabled:
		return
	health -= amount
	if webbed:
		web_hits += 1
		state = State.WEBBED_STUN
		stun_left = 2.5
		attack_windup = 0
	Dialogue.play_sfx("WebHit4" if webbed else "PunchMedium", global_position)
	if health <= 0 or web_hits >= 3:
		state = State.DEFEATED
		active = false
		collision_layer = 0
		rotation.z = PI*.5
		defeated.emit(self)
