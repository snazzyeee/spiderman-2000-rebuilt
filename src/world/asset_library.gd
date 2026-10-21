class_name RevampAssets
extends RefCounted

static func instantiate_model(path: String) -> Node3D:
	if not ResourceLoader.exists(path):
		push_error("Private asset missing. Run tools/pipeline/convert_characters.py: "+path)
		return null
	var packed = load(path)
	if not packed is PackedScene:
		return null
	var instance: Node3D = packed.instantiate()
	TextureQuality.apply_to_model(instance)
	return instance

static func bounds_of(root: Node3D) -> AABB:
	var result := AABB()
	var initialized := false
	var pending: Array[Node] = [root]
	while not pending.is_empty():
		var node: Node = pending.pop_back()
		pending.append_array(node.get_children())
		if node is MeshInstance3D and node.mesh:
			var transform := Transform3D.IDENTITY
			var parent: Node = node
			while parent != root and parent is Node3D:
				transform = parent.transform * transform
				parent = parent.get_parent()
			var box: AABB = transform * node.get_aabb()
			result = result.merge(box) if initialized else box
			initialized = true
	return result

static func fit_character(root: Node3D, height: float = 1.8) -> float:
	var box := bounds_of(root)
	var factor := height/maxf(.01, box.size.y)
	root.scale = Vector3.ONE*factor
	root.position = Vector3(-box.get_center().x, -box.position.y, -box.get_center().z)*factor
	return factor

static func animation_player(root: Node) -> AnimationPlayer:
	if root is AnimationPlayer:
		return root
	for child in root.get_children():
		var result := animation_player(child)
		if result:
			return result
	return null

static func lighting(root: Node3D) -> void:
	var environment := WorldEnvironment.new()
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color(.08, .12, .2)
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(.5, .62, .85)
	env.ambient_light_energy = .65
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.fog_enabled = true
	env.fog_light_color = Color(.24, .31, .4)
	env.fog_density = .004
	if RenderingServer.get_current_rendering_method() == "forward_plus":
		env.ssao_enabled = true
		env.volumetric_fog_enabled = true
		env.volumetric_fog_density = .007
	environment.environment = env
	root.add_child(environment)
	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-46, -28, 0)
	sun.light_color = Color(1, .86, .68)
	sun.light_energy = 1.7
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 120.0
	root.add_child(sun)
	VideoSettings.apply_scene(root)
