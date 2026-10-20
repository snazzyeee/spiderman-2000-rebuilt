class_name SpiderPlayer
extends CharacterBody3D

signal health_changed(value: float)
signal defeated

enum State { GROUNDED, AIRBORNE, SWINGING, ZIPPING, CRAWLING }
var state := State.AIRBORNE
var controls_enabled := false
var health := 100.0
var anchor := Vector3.ZERO
var anchor_body: CollisionObject3D
var rope_length := 0.0
var visual: Node3D
var animations: AnimationPlayer
var animation_names := PackedStringArray()
var yaw := 0.0
var pitch := -.12
var target_yaw := 0.0
var target_pitch := -.12
var pivot := Node3D.new()
var spring := SpringArm3D.new()
var camera := Camera3D.new()
var rope := MeshInstance3D.new()
var rope_mesh := ImmediateMesh.new()
var wall_normal := Vector3.FORWARD
var last_attack := -10.0
var invulnerable_until := 0.0
var last_animation := ""
var spawn_position := Vector3.ZERO

func _ready() -> void:
	collision_layer = 2
	collision_mask = 1|4
	floor_snap_length = .2
	var collision := CollisionShape3D.new()
	var box := BoxShape3D.new()
	box.size = Vector3(.65, 1.7, .55)
	collision.shape = box
	collision.position.y = .88
	add_child(collision)
	visual = RevampAssets.instantiate_model("res://assets/characters/spidey.glb")
	if visual:
		add_child(visual)
		RevampAssets.fit_character(visual)
		animations = RevampAssets.animation_player(visual)
		if animations:
			animation_names = animations.get_animation_list()
			if animation_names.has("RESET"):
				animation_names.remove_at(animation_names.find("RESET"))
	pivot.position.y = 1.5
	add_child(pivot)
	spring.spring_length = 5.5
	spring.margin = .25
	spring.collision_mask = 1
	var sphere := SphereShape3D.new()
	sphere.radius = .22
	spring.shape = sphere
	pivot.add_child(spring)
	spring.add_excluded_object(get_rid())
	camera.fov = 72
	camera.near = .1
	camera.current = true
	spring.add_child(camera)
	var web_material := StandardMaterial3D.new()
	web_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	web_material.albedo_color = Color(.9, .94, 1)
	rope.material_override = web_material
	rope.mesh = rope_mesh
	add_child(rope)

func _unhandled_input(event: InputEvent) -> void:
	if controls_enabled and event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		target_yaw -= event.relative.x*.003
		target_pitch = clampf(target_pitch-event.relative.y*.0025, -1.1, .9)

func aim_hit(mask: int = 1, distance: float = 70.0) -> Dictionary:
	var origin := camera.global_position
	var query := PhysicsRayQueryParameters3D.create(origin, origin-camera.global_basis.z*distance, mask, [get_rid()])
	return get_world_3d().direct_space_state.intersect_ray(query)

func begin_swing(hit: Dictionary) -> bool:
	if not controls_enabled or hit.is_empty() or not hit.get("collider") is CollisionObject3D:
		return false
	if hit.collider.collision_layer & 1 == 0:
		return false
	var point: Vector3 = hit.position
	if point.y < global_position.y+1.2 or global_position.distance_to(point) > 70.0:
		return false
	var line := PhysicsRayQueryParameters3D.create(global_position+Vector3.UP*1.2, point+(point-global_position).normalized()*.05, 1, [get_rid()])
	var verified_hit := get_world_3d().direct_space_state.intersect_ray(line)
	if verified_hit.is_empty() or verified_hit.collider != hit.collider or verified_hit.position.distance_to(point) > .35:
		return false
	anchor = point
	anchor_body = hit.collider
	rope_length = global_position.distance_to(anchor)
	if rope_length < 2.0:
		return false
	state = State.SWINGING
	velocity = velocity.slide((global_position-anchor).normalized())
	return true

func release_anchor() -> void:
	state = State.AIRBORNE
	anchor_body = null
	rope_mesh.clear_surfaces()

func _physics_process(delta: float) -> void:
	if not controls_enabled:
		velocity = Vector3.ZERO
		return
	target_yaw -= Input.get_axis("look_left", "look_right")*2.6*delta
	target_pitch = clampf(target_pitch-Input.get_axis("look_up", "look_down")*1.9*delta, -1.1, .9)
	yaw = lerp_angle(yaw, target_yaw, 1.0-exp(-14.0*delta))
	pitch = lerpf(pitch, target_pitch, 1.0-exp(-14.0*delta))
	pivot.rotation = Vector3(pitch, yaw, 0)
	var input := Input.get_vector("move_left", "move_right", "move_forward", "move_back")
	var direction := Basis(Vector3.UP, yaw)*Vector3(input.x, 0, input.y)
	if Input.is_action_just_pressed("swing") and state != State.SWINGING:
		begin_swing(aim_hit())
	if state == State.SWINGING and (Input.is_action_just_released("swing") or Input.is_action_just_pressed("jump")):
		release_anchor()
	if Input.is_action_just_pressed("zip"):
		var hit := aim_hit()
		if not hit.is_empty():
			anchor = hit.position+hit.normal*1.2
			anchor_body = hit.collider
			state = State.ZIPPING
	if Input.is_action_just_pressed("crawl"):
		if state == State.CRAWLING:
			state = State.AIRBORNE
		else:
			var origin := global_position+Vector3.UP*.8
			var query := PhysicsRayQueryParameters3D.create(origin, origin-camera.global_basis.z*2.0, 1, [get_rid()])
			var wall := get_world_3d().direct_space_state.intersect_ray(query)
			if not wall.is_empty() and absf(wall.normal.y) < .5:
				wall_normal = wall.normal
				move_and_collide(wall.position+wall_normal*.5-Vector3.UP*.8-global_position)
				state = State.CRAWLING
	match state:
		State.SWINGING:
			_step_swing(delta, direction)
		State.ZIPPING:
			var offset := anchor-global_position
			velocity = offset.normalized()*minf(34.0, offset.length()/delta)
			if move_and_collide(velocity*delta) or offset.length() < 1.4:
				release_anchor()
		State.CRAWLING:
			_step_crawl(delta, input)
		_:
			velocity.x = move_toward(velocity.x, direction.x*10.0, delta*38.0)
			velocity.z = move_toward(velocity.z, direction.z*10.0, delta*38.0)
			velocity.y -= 25.0*delta
			if is_on_floor() and Input.is_action_just_pressed("jump"):
				velocity.y = 12.0
			move_and_slide()
			state = State.GROUNDED if is_on_floor() else State.AIRBORNE
	if visual and direction.length_squared() > .01 and state != State.CRAWLING:
		visual.rotation.x = 0
		visual.rotation.z = 0
		visual.rotation.y = lerp_angle(visual.rotation.y, atan2(direction.x, direction.z), delta*12.0)
	if state != State.SWINGING:
		rope_mesh.clear_surfaces()
	if Input.is_action_just_pressed("attack"):
		attack(false)
	if Input.is_action_just_pressed("web"):
		attack(true)
	_animate()
	Dialogue.set_traversal_filter(global_position.y-spawn_position.y, velocity.length())

func _step_swing(delta: float, direction: Vector3) -> void:
	if not is_instance_valid(anchor_body) or not anchor_body.is_inside_tree() or anchor_body.collision_layer & 1 == 0:
		release_anchor()
		return
	var radial := (global_position-anchor).normalized()
	var trough := pow(maxf(0.0, -radial.y), 6.0)
	velocity += (Vector3.DOWN*25.0*(1.0+trough*.25)+direction*18.0).slide(radial)*delta
	velocity = velocity.slide(radial)*exp(-.08*delta)
	velocity = velocity.limit_length(42.0)
	var next := anchor+(global_position+velocity*delta-anchor).normalized()*rope_length
	var query := PhysicsRayQueryParameters3D.create(global_position+Vector3.UP*.7, anchor, 1, [get_rid()])
	var obstruct := get_world_3d().direct_space_state.intersect_ray(query)
	if not obstruct.is_empty() and obstruct.position.distance_to(anchor) > .3:
		release_anchor()
		return
	var collision := move_and_collide(next-global_position)
	if collision:
		velocity = velocity.slide(collision.get_normal())
		release_anchor()
		return
	velocity = velocity.slide((global_position-anchor).normalized())
	_draw_web()

func _step_crawl(delta: float, input: Vector2) -> void:
	var origin := global_position+Vector3.UP*.8
	var query := PhysicsRayQueryParameters3D.create(origin, origin-wall_normal*1.5, 1, [get_rid()])
	var surface := get_world_3d().direct_space_state.intersect_ray(query)
	if surface.is_empty() or Input.is_action_just_pressed("jump"):
		velocity = wall_normal*5.0+Vector3.UP*7.0
		state = State.AIRBORNE
		return
	wall_normal = surface.normal
	var across := Vector3.UP.cross(wall_normal).normalized()
	velocity = (across*input.x+Vector3.UP*-input.y)*5.0
	move_and_collide(velocity*delta)
	if visual:
		var up := Vector3.UP.slide(wall_normal).normalized()
		visual.basis = Basis(across, up, wall_normal).orthonormalized().scaled(Vector3.ONE*visual.scale.length()/sqrt(3.0))

func _draw_web() -> void:
	rope_mesh.clear_surfaces()
	rope_mesh.surface_begin(Mesh.PRIMITIVE_LINES)
	rope_mesh.surface_add_vertex(rope.to_local(global_position+Vector3.UP*1.4))
	rope_mesh.surface_add_vertex(rope.to_local(anchor))
	rope_mesh.surface_end()

func attack(web: bool) -> void:
	var now := Time.get_ticks_msec()/1000.0
	if not controls_enabled or now-last_attack < (.25 if web else .4):
		return
	last_attack = now
	Dialogue.play_sfx("WebShot3" if web else "PunchBig")
	if web:
		var hit := aim_hit(1|4, 40.0)
		if not hit.is_empty() and hit.collider.has_method("take_damage"):
			hit.collider.take_damage(25.0, true)
	else:
		for enemy in get_tree().get_nodes_in_group("enemies"):
			if global_position.distance_to(enemy.global_position) < 2.8:
				enemy.take_damage(35.0, false)
				break

func take_damage(amount: float) -> void:
	var now := Time.get_ticks_msec()/1000.0
	if not controls_enabled or now < invulnerable_until:
		return
	invulnerable_until = now+.6
	health = maxf(0, health-amount)
	health_changed.emit(health)
	if health <= 0:
		defeated.emit()

func _animate() -> void:
	if not animations or animation_names.is_empty():
		return
	var index := 1 if state in [State.SWINGING, State.AIRBORNE, State.ZIPPING] else 0
	var selected := animation_names[mini(index, animation_names.size()-1)]
	if selected != last_animation:
		animations.get_animation(selected).loop_mode = Animation.LOOP_LINEAR
		animations.play(selected, .15)
		last_animation = selected
