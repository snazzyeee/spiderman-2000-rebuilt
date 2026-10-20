class_name WebFluid
extends Resource

signal spare_swapped
signal empty
@export var meter: float
@export var spares: int
@export var suit: int
@export var cost_setting: int
@export var cap: int
@export var reduced_cap: int
@export var reduced_cap_suits: PackedInt32Array
@export var no_cost_suits: PackedInt32Array
@export var cost_divisors: PackedFloat32Array
@export var no_cost_flag_a := false
@export var no_cost_flag_b := false
var fire := false

func spare_cap() -> int:
	return reduced_cap if suit in reduced_cap_suits else cap

func gain(amount: float) -> bool:
	if spares >= spare_cap() and meter >= 1.0:
		return false
	meter += maxf(0.0,amount)
	while meter > 1.0 and spares < spare_cap():
		spares += 1
		meter -= 1.0
	meter = minf(1.0,meter)
	return true

func spend(amount: float) -> bool:
	if suit in no_cost_suits or no_cost_flag_a or no_cost_flag_b:
		return true
	var cost := maxf(0.0,amount)/cost_divisors[clampi(cost_setting,0,cost_divisors.size()-1)]
	if meter < cost and spares > 0:
		spares -= 1
		meter += 1.0
		spare_swapped.emit()
	if meter < cost:
		empty.emit()
		return false
	meter -= cost
	return true

func set_fire() -> void:
	meter = 1.0
	fire = true
