extends "res://tests/scenes/traversal_fixture.gd"

func run() -> Array:
	var fluid: WebFluid = preload("res://src/player/tuning/web_fluid.tres").duplicate(true)
	for suit in [0,6,9,10]:
		fluid.suit = suit
		fluid.spares = 0
		fluid.meter = 1.0
		fluid.gain(0.25)
		check(fluid.spares == 1 and fluid.meter == 0.25, "overflow carries into meter suit %d" % suit)
		fluid.spares = fluid.spare_cap()
		fluid.meter = 1.0
		check(not fluid.gain(0.5) and fluid.spares == (10 if suit == 0 else 2), "spare cap suit %d" % suit)
	fluid.suit = 0
	fluid.cost_setting = 0
	fluid.meter = 1.0
	fluid.spend(1.0)
	check(fluid.meter == 31.0/32.0, "setting zero spends cost divided by 32")
	fluid.cost_setting = 1
	fluid.meter = 1.0
	fluid.spend(1.0)
	check(fluid.meter == 0.5, "setting one spends half cost")
	var swaps: Array = []
	fluid.spare_swapped.connect(func(): swaps.append(true))
	fluid.meter = 0.0
	fluid.spares = 1
	check(fluid.spend(1.0) and fluid.spares == 0 and fluid.meter == 0.5 and swaps.size() == 1, "empty meter consumes spare and signals refill")
	for suit in [3,4]:
		fluid.suit = suit
		fluid.meter = 0.0
		check(fluid.spend(1.0) and fluid.meter == 0.0, "no-cost suit %d" % suit)
	var table: DifficultyTable = preload("res://src/player/tuning/difficulty.tres").duplicate(true)
	for level in range(4):
		table.level = level
		for suit in [0,2,3,4]:
			check(table.damage(1.0,suit) == [2.0,1.5,1.0,0.75][level]*(1 if suit == 0 else 2), "difficulty damage level %d suit %d" % [level,suit])
	var motor := motor_at(Vector3.ZERO)
	check(Dialogue.registry.is_empty(), "public traversal tests do not load private audio registry")
	motor.fluid.meter = 0
	motor.fluid.spares = 1
	var requests: Array = []
	var receive_sfx := func(id): requests.append(id)
	Dialogue.original_sfx_requested.connect(receive_sfx)
	motor.attack(true)
	check(requests == [30], "motor requests recovered SFX30 on cartridge swap")
	Dialogue.original_sfx_requested.disconnect(receive_sfx)
	var barks: Array = []
	var receive_xa := func(bank,track): barks.append([bank,track])
	Dialogue.original_xa_requested.connect(receive_xa)
	motor.fluid.meter = 0
	motor.fluid.spares = 0
	motor.last_attack = -INF
	motor.attack(true)
	check(barks.size() == 1 and barks[0][0] == 33 and barks[0][1] in [2,3,4], "empty shot requests XA33 track 2..4")
	Dialogue.original_xa_requested.disconnect(receive_xa)
	motor.health = 90
	motor.heal(100)
	check(motor.health == 100, "health clamps to maximum")
	motor.health = 0
	motor.heal(100)
	check(motor.health == 0, "dead player cannot heal")
	return checks
