class_name SwingTuning
extends Resource

enum AnchorPolicy { GEOMETRY, SKY_HOOK, HYBRID }
@export var anchor_policy: AnchorPolicy
@export var gravity: float
@export var pump: float
@export var damping: float
@export var phase_per_second: float
@export var release_boost: float
@export var minimum_length: float
@export var maximum_length: float
@export var minimum_rise: float
@export var sight_origin_height: float
@export var sight_tolerance: float
@export var sight_extension: float
@export var sky_offset: Vector3
@export var animation_binding: StringName
@export var animation_confidence: String
