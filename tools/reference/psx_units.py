"""Original PC/PSX numeric conventions and their Godot equivalents.

Mirror of src/core/psx_units.gd; tools/reference/tests/data/units_vectors.json
holds the shared vectors both implementations must reproduce. See
docs/behavior_ledger/units.md for evidence.
"""
from __future__ import annotations
import math

FIXED_SHIFT = 12
FIXED_ONE = 1 << FIXED_SHIFT          # 4.12 / 20.12 fixed point: 4096 == 1.0
ANGLE_FULL_TURN = 4096                # 4096 angle units per revolution
# Authored calibration: Spider-Man's bind pose (~90.9 world units) stands 1.8 m.
METERS_PER_WORLD_UNIT = 0.019792303442955017
# World (PSX, Y down) to Godot axis signs, applied before scaling. Decomp convention;
# the L1A1 TRG-vs-geometry landmark check does not confirm it yet (units.md).
AXIS_SIGNS = (1, -1, -1)
LANDMARK_STATUS = 'unverified: L1A1 TRG positions do not align with converted l1a1 geometry (docs/behavior_ledger/units.md)'
ORIGINAL_TICK_HZ = 30                 # frame-based constants assume this until measured


def fixed_to_float(value: int, shift: int = FIXED_SHIFT) -> float:
    return value / float(1 << shift)


def float_to_fixed(value: float, shift: int = FIXED_SHIFT) -> int:
    return int(round(value * (1 << shift)))


def angle_to_radians(units: int) -> float:
    return units * math.tau / ANGLE_FULL_TURN


def wrap_angle_delta(from_units: int, to_units: int) -> int:
    """Shortest signed delta in angle units, matching the decomp's CalcTheta."""
    delta = (to_units & 0xFFF) - (from_units & 0xFFF)
    if delta > 2048:
        return delta - 4096
    if delta < -2048:
        return delta + 4096
    return delta


def world_to_godot(position: list[int] | tuple[int, int, int]) -> tuple[float, float, float]:
    """Integer world units (TRG/model space) to Godot meters."""
    return tuple(value * sign * METERS_PER_WORLD_UNIT for value, sign in zip(position, AXIS_SIGNS))


def fixed_world_to_godot(position: list[int] | tuple[int, int, int]) -> tuple[float, float, float]:
    """20.12 fixed world coordinates (CBody::mPos) to Godot meters."""
    return world_to_godot([fixed_to_float(v) for v in position])


def per_frame_to_per_second(value: float, tick_hz: int = ORIGINAL_TICK_HZ) -> float:
    return value * tick_hz


def describe() -> dict:
    return dict(position_world='integer world units; CBody mPos is the same value << 12',
                meters_per_world_unit=METERS_PER_WORLD_UNIT, axis_signs=list(AXIS_SIGNS),
                fixed_one=FIXED_ONE, angle_full_turn=ANGLE_FULL_TURN, original_tick_hz=ORIGINAL_TICK_HZ,
                position_godot_status=LANDMARK_STATUS)
