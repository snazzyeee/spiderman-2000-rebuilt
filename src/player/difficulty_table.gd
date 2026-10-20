class_name DifficultyTable
extends Resource

@export var level: int
@export var dealt: PackedFloat32Array
@export var double_damage_suits: PackedInt32Array
@export var suit_multiplier: float

func damage(base: float, suit: int) -> float:
	return base*dealt[clampi(level,0,dealt.size()-1)]*(suit_multiplier if suit in double_damage_suits else 1.0)
