class_name PlayerMotor
extends CharacterBody3D

signal health_changed(value: float)
signal defeated
enum State { GROUNDED, AIRBORNE, SWINGING, ZIPPING, CRAWLING, CEILING, STUNNED, CINEMATIC }
@export var movement: MovementTuning = preload("res://src/player/tuning/movement.tres")
@export var swing_tuning: SwingTuning = preload("res://src/player/tuning/swing.tres")
@export var crawl_tuning: CrawlTuning = preload("res://src/player/tuning/crawl.tres")
@export var synthetic_visual := false
var context := MotorContext.new()
var states: Dictionary = {}
var state := State.AIRBORNE
var controls_enabled := false
var health := 0.0
var anchor := Vector3.ZERO
var anchor_body: CollisionObject3D
var rope_length := 0.0
var wall_normal := Vector3.FORWARD
var spawn_position := Vector3.ZERO
var yaw := 0.0
var target_yaw := 0.0
var pitch := 0.0
var target_pitch := 0.0
var visual: Node3D
var animations: AnimationPlayer
var last_animation := ""
var semantic_bindings: Dictionary = {}
var rig := TraversalCameraRig.new()
var camera: Camera3D
var pivot: Node3D
var spring: SpringArm3D
var rope := MeshInstance3D.new()
var rope_mesh := ImmediateMesh.new()
var last_attack := -INF
var invulnerable_until := 0.0
var fluid: WebFluid
var difficulty: DifficultyTable

func _ready() -> void:
	context.motor = self
	context.movement = movement
	context.swing = swing_tuning
	context.crawl = crawl_tuning
	health = movement.max_health
	collision_layer = 2
	collision_mask = 1|4
	floor_snap_length = movement.floor_snap
	var collision := CollisionShape3D.new()
	var box := BoxShape3D.new()
	box.size = movement.body_size
	collision.shape = box
	collision.position = movement.body_center
	add_child(collision)
	for entry in [[State.GROUNDED,"ground"], [State.AIRBORNE,"air"], [State.SWINGING,"swing"],
		[State.ZIPPING,"zip"], [State.CRAWLING,"crawl"], [State.CEILING,"ceiling"],
		[State.STUNNED,"stunned"], [State.CINEMATIC,"cinematic"]]:
		var script: Script = load("res://src/player/states/"+entry[1]+".gd")
		var node: MotorState = script.new()
		node.name = entry[1].capitalize()
		node.context = context
		add_child(node)
		states[entry[0]] = node
	var semantics: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://docs/behavior_ledger/data/animation_semantics.json"))
	semantic_bindings = semantics.runtime_bindings
	fluid = preload("res://src/player/tuning/web_fluid.tres").duplicate(true)
	difficulty = preload("res://src/player/tuning/difficulty.tres")
	fluid.spare_swapped.connect(func(): Dialogue.play_original_sfx(movement.refill_sfx,global_position))
	fluid.empty.connect(func(): Dialogue.play_original_xa(movement.empty_bank,movement.empty_tracks[randi_range(0,movement.empty_tracks.size()-1)]))
	if not synthetic_visual:
		visual = RevampAssets.instantiate_model("res://assets/characters/spidey.glb")
		if visual:
			add_child(visual)
			RevampAssets.fit_character(visual, movement.visual_height)
			animations = RevampAssets.animation_player(visual)
	add_child(rig)
	rig.setup(self)
	camera = rig.camera
	pivot = rig.pivot
	spring = rig.spring
	rope.mesh = rope_mesh
	var material := StandardMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.albedo_color = Color.WHITE
	rope.material_override = material
	add_child(rope)

func change_state(next: int) -> void:
	if next == state:
		return
	if states.has(state):
		states[state].leave()
	state = next
	states[state].enter()
	var mode := 0
	match state:
		State.CRAWLING: mode = 1
		State.CEILING: mode = 2
		State.SWINGING: mode = 4
		State.AIRBORNE: mode = 5
	rig.director.select(mode)
	if state not in [State.CRAWLING, State.CEILING]:
		context.surface_normal = Vector3.UP
	if state != State.SWINGING:
		rope_mesh.clear_surfaces()

func _unhandled_input(event: InputEvent) -> void:
	if controls_enabled and event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		target_yaw -= event.relative.x*movement.mouse_sensitivity.x
		target_pitch = clampf(target_pitch-event.relative.y*movement.mouse_sensitivity.y, movement.pitch_limits.x, movement.pitch_limits.y)

func _physics_process(delta: float) -> void:
	if not controls_enabled:
		change_state(State.CINEMATIC)
		states[state].step(delta)
		return
	if state == State.CINEMATIC:
		change_state(State.AIRBORNE)
	target_yaw -= Input.get_axis("look_left", "look_right")*movement.look_speed.x*delta
	target_pitch = clampf(target_pitch-Input.get_axis("look_up", "look_down")*movement.look_speed.y*delta, movement.pitch_limits.x, movement.pitch_limits.y)
	yaw = lerp_angle(yaw, target_yaw, 1.0-exp(-movement.look_smoothing*delta))
	pitch = lerpf(pitch, target_pitch, 1.0-exp(-movement.look_smoothing*delta))
	context.input_vector = Input.get_vector("move_left", "move_right", "move_forward", "move_back")
	context.direction = Basis(Vector3.UP, yaw)*Vector3(context.input_vector.x, 0, context.input_vector.y)
	context.jump_pressed = Input.is_action_just_pressed("jump")
	if Input.is_action_just_pressed("swing") and state != State.SWINGING:
		begin_swing(aim_hit())
	elif state == State.SWINGING and (Input.is_action_just_released("swing") or context.jump_pressed):
		release_anchor()
	if Input.is_action_just_pressed("zip"):
		begin_zip(0, aim_hit())
	if Input.is_action_just_pressed("zip_forward"):
		begin_zip(1)
	if Input.is_action_just_pressed("crawl"):
		if state in [State.CRAWLING, State.CEILING]:
			knock_off()
		else:
			var origin := global_position+movement.body_center
			try_wall_attach(context.ray(origin, origin-camera.global_basis.z*crawl_tuning.attach_reach), true)
	states[state].step(delta)
	if state not in [State.CRAWLING, State.CEILING]:
		restore_upright(delta)
	if state not in [State.SWINGING, State.STUNNED, State.CEILING, State.CRAWLING] and context.last_event != "ground_loss":
		play_animation(movement.air_animation if state in [State.AIRBORNE,State.ZIPPING] else movement.ground_animation)
	if Input.is_action_just_pressed("attack"):
		attack(false)
	if Input.is_action_just_pressed("web"):
		attack(true)
	rig.update_view(delta, yaw, pitch)
	Dialogue.set_traversal_filter(global_position.y-spawn_position.y, velocity.length())

func aim_hit(mask: int = 1, distance: float = -1.0) -> Dictionary:
	var reach := swing_tuning.maximum_length if distance < 0.0 else distance
	return context.ray(camera.global_position, camera.global_position-camera.global_basis.z*reach, mask)

func begin_swing(hit: Dictionary) -> bool:
	if not controls_enabled:
		return false
	var valid_geometry := false
	if not hit.is_empty() and is_instance_valid(hit.get("collider")) and hit.collider is CollisionObject3D:
		var point: Vector3 = hit.position
		if hit.collider.collision_layer & 1 and point.y >= global_position.y+swing_tuning.minimum_rise and global_position.distance_to(point) <= swing_tuning.maximum_length:
			var verified := context.ray(global_position+Vector3.UP*swing_tuning.sight_origin_height,
				point+(point-global_position).normalized()*swing_tuning.sight_extension)
			valid_geometry = not verified.is_empty() and verified.collider == hit.collider and verified.position.distance_to(point) <= swing_tuning.sight_tolerance
	if swing_tuning.anchor_policy == SwingTuning.AnchorPolicy.GEOMETRY and not valid_geometry:
		return false
	if valid_geometry and swing_tuning.anchor_policy != SwingTuning.AnchorPolicy.SKY_HOOK:
		anchor = hit.position
		anchor_body = hit.collider
	else:
		anchor = global_position+Basis(Vector3.UP,yaw)*swing_tuning.sky_offset
		anchor_body = null
	rope_length = global_position.distance_to(anchor)
	if rope_length < swing_tuning.minimum_length or rope_length > swing_tuning.maximum_length:
		return false
	change_state(State.SWINGING)
	states[state].attach(Basis(Vector3.UP,yaw)*Vector3.FORWARD)
	return true

func release_anchor() -> void:
	change_state(State.AIRBORNE)
	anchor_body = null

func _step_swing(delta: float, direction: Vector3) -> void:
	context.direction = direction
	if state == State.SWINGING:
		states[state].step(delta)

func scrub_swing_animation(phase: float) -> void:
	context.animation_phase = phase
	play_animation(swing_tuning.animation_binding)
	if animations and animations.has_animation(swing_tuning.animation_binding):
		var clip := animations.get_animation(swing_tuning.animation_binding)
		animations.seek(clampf(phase,0,1)*clip.length, true)
		animations.pause()

func draw_rope() -> void:
	rope_mesh.clear_surfaces()
	rope_mesh.surface_begin(Mesh.PRIMITIVE_LINES)
	rope_mesh.surface_add_vertex(rope.to_local(global_position+Vector3.UP*movement.hand_height))
	rope_mesh.surface_add_vertex(rope.to_local(anchor))
	rope_mesh.surface_end()

func begin_zip(mode: int, hit: Dictionary = {}) -> bool:
	if not controls_enabled or mode not in [0,1] or (mode == 0 and hit.is_empty()):
		return false
	var zip = states[State.ZIPPING]
	zip.mode = mode
	if mode == 0:
		zip.destination = hit.position+hit.normal*movement.zip_clearance
		zip.landing_normal = hit.normal
	else:
		var direction := (-camera.global_basis.z+Vector3.UP*movement.zip_forward_up).normalized()
		zip.destination = global_position+direction*movement.zip_range
		zip.landing_normal = Vector3.UP
	change_state(State.ZIPPING)
	anchor_body = null
	return true

func surface_allows_crawl(hit: Dictionary) -> bool:
	var collider: Object = hit.get("collider")
	return is_instance_valid(collider) and collider.get_meta("crawl_allowed",true) and not collider.get_meta("no_stick",false)

func is_wall(normal: Vector3) -> bool:
	return normal.y >= crawl_tuning.wall_min_y and normal.y <= crawl_tuning.wall_max_y

func try_wall_attach(hit: Dictionary, explicit_input: bool) -> bool:
	if not explicit_input or hit.is_empty() or not is_wall(hit.normal) or not surface_allows_crawl(hit):
		return false
	context.surface_normal = hit.normal
	wall_normal = hit.normal
	move_and_collide(hit.position+hit.normal*crawl_tuning.body_clearance-movement.body_center-global_position)
	velocity = Vector3.ZERO
	change_state(State.CRAWLING)
	context.ground_missing_seconds = 0.0
	return true

func try_ceiling_attach(hit: Dictionary, rising: bool, contact_this_frame: bool) -> bool:
	if not rising or not contact_this_frame or hit.is_empty() or not surface_allows_crawl(hit) or -hit.normal.y <= crawl_tuning.ceiling_threshold:
		return false
	var alternate: bool = last_animation == semantic_bindings.ceiling_current
	context.surface_normal = hit.normal
	wall_normal = hit.normal
	move_and_collide(hit.position+hit.normal*crawl_tuning.body_clearance-movement.body_center-global_position)
	velocity = Vector3.ZERO
	change_state(State.CEILING)
	context.ground_missing_seconds = 0.0
	context.jump_pressed = false
	play_semantic("ceiling_alternate" if alternate else "ceiling_attach")
	Dialogue.play_original_sfx(movement.ceiling_sfx,global_position)
	return true

func try_ceiling_contacts(rising: bool) -> bool:
	for index in range(get_slide_collision_count()):
		var contact := get_slide_collision(index)
		if try_ceiling_attach({"position":contact.get_position(), "normal":contact.get_normal(), "collider":contact.get_collider()}, rising, true):
			return true
	return false

func update_wall_push(delta: float, contact: Dictionary = {}) -> void:
	if contact.is_empty():
		for index in range(get_slide_collision_count()):
			var collision := get_slide_collision(index)
			if is_wall(collision.get_normal()):
				contact = {"normal":collision.get_normal(),"collider":collision.get_collider()}
				break
	if not contact.is_empty() and surface_allows_crawl(contact) and context.direction.dot(-contact.normal) > 0.0:
		context.wall_push_seconds += delta
	else:
		context.wall_push_seconds = 0.0
	if context.wall_push_seconds+0.0000001 >= context.original_seconds(crawl_tuning.bump_frames):
		context.wall_push_seconds = 0.0
		context.event("wall_bump")
		change_state(State.STUNNED)

func knock_off() -> void:
	if state not in [State.CRAWLING,State.CEILING]:
		return
	velocity = context.surface_normal*crawl_tuning.knock_out+Vector3.UP*crawl_tuning.knock_up
	context.event("knock_off")
	change_state(State.STUNNED)

func orient_visual(normal: Vector3, up: Vector3, delta: float) -> void:
	if not visual:
		return
	var visual_scale := visual.scale
	var right := up.cross(normal).normalized()
	var surface_basis := Basis(right, normal.cross(right), normal).orthonormalized()
	visual.quaternion = visual.quaternion.slerp(surface_basis.get_rotation_quaternion(), 1.0-exp(-crawl_tuning.orientation_rate*delta))
	visual.scale = visual_scale

func restore_upright(delta: float) -> void:
	if visual:
		visual.quaternion = visual.quaternion.slerp(Quaternion(Vector3.UP,yaw+PI), 1.0-exp(-crawl_tuning.orientation_rate*delta))

func play_semantic(event: String) -> void:
	context.last_event = event
	play_animation(semantic_bindings.get(event, ""))

func play_animation(binding: StringName) -> void:
	if not animations or not animations.has_animation(binding) or String(binding) == last_animation:
		return
	animations.play(binding)
	last_animation = String(binding)

func attack(web: bool) -> void:
	var now := Time.get_ticks_msec()/1000.0
	if not controls_enabled or now-last_attack < (movement.web_cooldown if web else movement.melee_cooldown):
		return
	if web and not fluid.spend(movement.web_cost):
		return
	last_attack = now
	Dialogue.play_sfx("WebShot3" if web else "PunchBig")
	if web:
		var hit := aim_hit(1|4,movement.web_range)
		if not hit.is_empty() and hit.collider.has_method("take_damage"):
			hit.collider.take_damage(difficulty.damage(movement.web_damage,fluid.suit),true)
	else:
		for enemy in get_tree().get_nodes_in_group("enemies"):
			if global_position.distance_to(enemy.global_position) < movement.melee_range:
				enemy.take_damage(difficulty.damage(movement.melee_damage,fluid.suit),false)
				break

func take_damage(amount: float) -> void:
	var now := Time.get_ticks_msec()/1000.0
	if not controls_enabled or now < invulnerable_until:
		return
	invulnerable_until = now+movement.damage_immunity
	health = clampf(health-amount,0.0,movement.max_health)
	health_changed.emit(health)
	if health == 0.0:
		defeated.emit()

func heal(amount: float) -> void:
	if health > 0.0:
		health = minf(movement.max_health,health+maxf(0.0,amount))
		health_changed.emit(health)
