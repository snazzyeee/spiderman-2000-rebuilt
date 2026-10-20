class_name MotorContext
extends RefCounted

var motor: CharacterBody3D
var movement: MovementTuning
var swing: SwingTuning
var crawl: CrawlTuning
var input_vector := Vector2.ZERO
var direction := Vector3.ZERO
var jump_pressed := false
var surface_normal := Vector3.UP
var ground_missing_seconds := 0.0
var wall_push_seconds := 0.0
var phase := 0.0
var animation_phase := 0.5
var last_event := ""

func ray(from: Vector3, to: Vector3, mask: int = 1) -> Dictionary:
	return motor.get_world_3d().direct_space_state.intersect_ray(
		PhysicsRayQueryParameters3D.create(from, to, mask, [motor.get_rid()]))

func change(next: int) -> void:
	motor.change_state(next)

func event(label: String) -> void:
	last_event = label
	motor.play_semantic(label)

func original_seconds(frames: float) -> float:
	return frames/PsxUnits.per_frame_to_per_second(1.0)
