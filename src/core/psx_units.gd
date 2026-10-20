class_name PsxUnits
extends RefCounted

# Original PC/PSX numeric conventions and their Godot equivalents.
# Mirrors tools/reference/psx_units.py; both must reproduce
# tools/reference/tests/data/units_vectors.json. Evidence: docs/behavior_ledger/units.md.

const FIXED_SHIFT := 12
const FIXED_ONE := 1 << FIXED_SHIFT        # 4.12 / 20.12 fixed point: 4096 == 1.0
const ANGLE_FULL_TURN := 4096              # angle units per revolution
# Authored calibration: Spider-Man's bind pose (~90.9 world units) stands 1.8 m.
const METERS_PER_MODEL_UNIT := 0.019792303442955017
const CONVERTER_WORLD_DIVISOR := 2.25
const METERS_PER_WORLD_UNIT := METERS_PER_MODEL_UNIT/CONVERTER_WORLD_DIVISOR
# World (PSX, Y down) to Godot axis signs, applied before scaling. Decomp convention;
# the L1A1 TRG-vs-geometry landmark check does not confirm it yet (units.md).
const AXIS_SIGNS := Vector3(1, -1, -1)
const ORIGINAL_TICK_HZ := 30               # frame-based constants assume this until measured

static func fixed_to_float(value: int, shift: int = FIXED_SHIFT) -> float:
	return value/float(1 << shift)

static func float_to_fixed(value: float, shift: int = FIXED_SHIFT) -> int:
	return roundi(value*float(1 << shift))

static func angle_to_radians(units: int) -> float:
	return units*TAU/ANGLE_FULL_TURN

# Shortest signed delta in angle units, matching the decomp's CalcTheta.
static func wrap_angle_delta(from_units: int, to_units: int) -> int:
	var delta := (to_units & 0xFFF)-(from_units & 0xFFF)
	if delta > 2048:
		return delta-4096
	if delta < -2048:
		return delta+4096
	return delta

# Integer world units (TRG/model space) to Godot meters.
static func world_to_godot(position: Vector3) -> Vector3:
	return position*AXIS_SIGNS*METERS_PER_WORLD_UNIT

# 20.12 fixed world coordinates (CBody::mPos) to Godot meters.
static func fixed_world_to_godot(position: Vector3i) -> Vector3:
	return world_to_godot(Vector3(fixed_to_float(position.x), fixed_to_float(position.y), fixed_to_float(position.z)))

static func per_frame_to_per_second(value: float, tick_hz: int = ORIGINAL_TICK_HZ) -> float:
	return value*tick_hz
