class_name CameraProfile
extends Resource

@export var mode: int
@export var offset: Vector3
@export var xz_distance: float
@export var y_distance: float
@export var sphere_radius: float
@export var margin: float
@export var whisker_side: float
@export var whisker_back: float
@export var pivot_height: float
@export var fov: float
@export var near_plane: float

func parameters() -> Array[float]:
	return [offset.x, offset.y, offset.z, xz_distance, y_distance]
