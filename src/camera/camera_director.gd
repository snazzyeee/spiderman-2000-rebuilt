class_name CameraDirector
extends Node

# Script semantics are recovered in camera.md. Follow placement is authored.
signal command_rejected(reason: String)
var profiles: Dictionary = {}
var active_mode := 0
var current: Array[float] = []
var blends: Dictionary = {}
var rejected: Array[String] = []

func configure(resources: Array) -> void:
	profiles.clear()
	for profile: CameraProfile in resources:
		profiles[profile.mode] = profile.parameters()
	select(0)

func select(mode: int) -> bool:
	if not profiles.has(mode):
		return reject("Invalid camera mode %d" % mode)
	active_mode = mode
	current.assign(profiles[mode])
	blends.clear()
	return true

func reject(reason: String) -> bool:
	rejected.append(reason)
	print("CAMERA_COMMAND_REJECTED ", reason)
	command_rejected.emit(reason)
	return false

func SetSpideyCamValue(mode: int, param: int, value: int, frames: int, apply_now: bool) -> bool:
	if not profiles.has(mode) or param < 0 or param >= 5 or value < -32768 or value > 32767 or frames < 0:
		return reject("Invalid camera command %s" % [[mode, param, value, frames]])
	profiles[mode][param] = float(value)
	if apply_now and active_mode == mode:
		if frames == 0:
			current[param] = float(value)
			blends.erase(param)
		else:
			blends[param] = {"start": current[param], "target": float(value), "elapsed": 0.0,
				"duration": frames/PsxUnits.per_frame_to_per_second(1.0)}
	return true

func advance(delta: float) -> void:
	for param in blends.keys():
		var blend: Dictionary = blends[param]
		blend.elapsed = minf(blend.duration, blend.elapsed+delta)
		var progress: float = blend.elapsed/blend.duration
		current[param] = lerpf(blend.start, blend.target, progress)
		if blend.duration-blend.elapsed < 0.0000001:
			current[param] = blend.target
			blends.erase(param)
