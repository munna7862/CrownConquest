extends Node2D

# Crown & Conquest — Godot 4 2D Graphical RTS Viewport
# Celtic Kings 2D Sprite Art, Directional Unit Animation, Multi-Layered Terrain & Fog of War

var map_width: float = 3200.0
var map_height: float = 3200.0
var camera_pos: Vector2 = Vector2(800.0, 800.0)
var camera_zoom: float = 1.0

# Selection & Drag Box
var is_dragging: bool = false
var drag_start_screen: Vector2 = Vector2.ZERO
var drag_current_screen: Vector2 = Vector2.ZERO
var selected_units: Array = []
var selected_building: Dictionary = {}

# Blueprint Construction Mode
var build_menu_open: bool = false
var is_placing_building: bool = false
var placing_building_type: String = ""
var placing_grid_size: Vector2 = Vector2(80, 80)
var placing_cost: Dictionary = {}

# Game Entities
var units: Array = []
var buildings: Array = []
var resource_nodes: Array = []
var floating_texts: Array = []
var dead_corpses: Array = []
var particles: Array = []

# Terrain & Roads (64x64 Grid = 3200x3200 world, tile_size = 50.0)
var tile_size: float = 50.0
var grid_w: int = 64
var grid_h: int = 64
var terrain_grid: Array = [] # 0: Grass, 1: FlowerGrass, 2: CobblestoneRoad, 3: DirtRoad, 4: ShallowWater, 5: DeepWater, 6: Cliff
var wave_phase: float = 0.0

# Dynamic Fog of War (64x64 Grid)
# 0: Unexplored (Black Shroud), 1: Explored (Fog of War), 2: Visible (In Line-of-Sight)
var fog_grid: Array = []

# Resources
var food: int = 600
var wood: int = 600
var gold: int = 400
var stone: int = 300
var iron: int = 200
var population: int = 15
var max_population: int = 30
var current_era: String = "Classical Era"

# Time & Simulation
var sim_tick: int = 0
var tick_accumulator: float = 0.0
var fixed_tick_dt: float = 0.05 # 20 Hz
var anim_time: float = 0.0

func _ready() -> void:
	init_terrain_and_fog()
	setup_battlefield()

func init_terrain_and_fog() -> void:
	terrain_grid.resize(grid_w * grid_h)
	fog_grid.resize(grid_w * grid_h)

	# 1. Base Grass & Wildflower patches
	for y in range(grid_h):
		for x in range(grid_w):
			var idx = (y * grid_w) + x
			fog_grid[idx] = 0 # Unexplored Black Shroud
			var seed_val = (x * 37 + y * 19) % 100
			if seed_val < 15:
				terrain_grid[idx] = 1 # FlowerGrass
			else:
				terrain_grid[idx] = 0 # Grass

	# 2. Cobblestone Military Roads connecting bases
	for i in range(10, 54):
		set_terrain(i, 16, 2) # East-West military road
		set_terrain(16, i, 2) # North-South military road
		if i >= 16 and i <= 48:
			set_terrain(i, i, 3) # Diagonal Dirt Road

	# 3. River Water Body (Vertical river through center with shallow fords)
	for y in range(grid_h):
		if y >= 28 and y <= 36:
			# Shallow river ford
			set_terrain(34, y, 4)
			set_terrain(35, y, 4)
			set_terrain(36, y, 4)
		else:
			set_terrain(33, y, 4) # Shallow shore
			set_terrain(34, y, 5) # Deep Water
			set_terrain(35, y, 5) # Deep Water
			set_terrain(36, y, 4) # Shallow shore

	# 4. Stone Cliff Elevation contours in North-East and South-West
	for cx in range(48, 58):
		for cy in range(6, 16):
			if cx == 48 or cx == 57 or cy == 6 or cy == 15:
				set_terrain(cx, cy, 6) # Cliff border

	for cx in range(6, 16):
		for cy in range(48, 58):
			if cx == 6 or cx == 15 or cy == 48 or cy == 57:
				set_terrain(cx, cy, 6) # Cliff border

func set_terrain(x: int, y: int, type: int) -> void:
	if x >= 0 and x < grid_w and y >= 0 and y < grid_h:
		terrain_grid[(y * grid_w) + x] = type

func get_terrain(x: int, y: int) -> int:
	if x < 0 or x >= grid_w or y < 0 or y >= grid_h:
		return 5 # Deep water boundary
	return terrain_grid[(y * grid_w) + x]

func setup_battlefield() -> void:
	# 1. Player Buildings (Celtic Architecture)
	buildings.append({
		"id": 1, "faction": 1, "type": "Town Center", "style": "celtic", "pos": Vector2(600, 600), "size": Vector2(130, 130),
		"hp": 1600.0, "max_hp": 1600.0, "color": Color("#1d4ed8"), "is_constructed": true, "build_progress": 1.0,
		"rally_pos": Vector2(660, 740), "queue": [], "pop_provided": 15, "vision_radius": 20
	})
	buildings.append({
		"id": 2, "faction": 1, "type": "Barracks", "style": "celtic", "pos": Vector2(460, 600), "size": Vector2(95, 95),
		"hp": 900.0, "max_hp": 900.0, "color": Color("#2563eb"), "is_constructed": true, "build_progress": 1.0,
		"rally_pos": Vector2(520, 720), "queue": [], "pop_provided": 0, "vision_radius": 16
	})
	buildings.append({
		"id": 3, "faction": 1, "type": "Blacksmith", "style": "celtic", "pos": Vector2(460, 460), "size": Vector2(85, 85),
		"hp": 700.0, "max_hp": 700.0, "color": Color("#3b82f6"), "is_constructed": true, "build_progress": 1.0,
		"rally_pos": Vector2(500, 560), "queue": [], "pop_provided": 0, "vision_radius": 14
	})
	buildings.append({
		"id": 4, "faction": 1, "type": "Watchtower", "style": "celtic", "pos": Vector2(750, 420), "size": Vector2(65, 65),
		"hp": 550.0, "max_hp": 550.0, "color": Color("#1e40af"), "is_constructed": true, "build_progress": 1.0,
		"rally_pos": Vector2(750, 480), "queue": [], "pop_provided": 0, "vision_radius": 24
	})
	buildings.append({
		"id": 5, "faction": 1, "type": "House", "style": "celtic", "pos": Vector2(620, 460), "size": Vector2(65, 65),
		"hp": 400.0, "max_hp": 400.0, "color": Color("#1e3a8a"), "is_constructed": true, "build_progress": 1.0,
		"rally_pos": Vector2(620, 520), "queue": [], "pop_provided": 5, "vision_radius": 12
	})

	# 2. Roman Enemy Outpost (Roman Architecture)
	buildings.append({
		"id": 6, "faction": 2, "type": "Praetorium Fortress", "style": "roman", "pos": Vector2(2300, 2300), "size": Vector2(140, 140),
		"hp": 2200.0, "max_hp": 2200.0, "color": Color("#991b1b"), "is_constructed": true, "build_progress": 1.0,
		"rally_pos": Vector2(2200, 2200), "queue": [], "pop_provided": 20, "vision_radius": 20
	})
	buildings.append({
		"id": 7, "faction": 2, "type": "Legionary Barracks", "style": "roman", "pos": Vector2(2150, 2350), "size": Vector2(100, 100),
		"hp": 1000.0, "max_hp": 1000.0, "color": Color("#b91c1c"), "is_constructed": true, "build_progress": 1.0,
		"rally_pos": Vector2(2100, 2250), "queue": [], "pop_provided": 0, "vision_radius": 16
	})

	# 3. Illustrated Natural Resources & Foliage
	# Oak & Pine Forests (Wood)
	resource_nodes.append({"id": 10, "type": "Oak Forest", "res": "Wood", "pos": Vector2(350, 420), "radius": 38.0, "amount": 800, "max_amount": 800, "foliage": "oak"})
	resource_nodes.append({"id": 11, "type": "Pine Forest", "res": "Wood", "pos": Vector2(780, 720), "radius": 36.0, "amount": 800, "max_amount": 800, "foliage": "pine"})
	# Sparkling Gold Mines (Gold)
	resource_nodes.append({"id": 12, "type": "Gold Ore Vein", "res": "Gold", "pos": Vector2(780, 520), "radius": 32.0, "amount": 700, "max_amount": 700, "foliage": "gold"})
	# Stone & Iron Boulders
	resource_nodes.append({"id": 13, "type": "Granite Quarry", "res": "Stone", "pos": Vector2(420, 780), "radius": 30.0, "amount": 500, "max_amount": 500, "foliage": "stone"})
	resource_nodes.append({"id": 14, "type": "Iron Outcropping", "res": "Iron", "pos": Vector2(420, 320), "radius": 28.0, "amount": 400, "max_amount": 400, "foliage": "iron"})
	# Berry Bush
	resource_nodes.append({"id": 15, "type": "Wild Berry Bush", "res": "Food", "pos": Vector2(680, 380), "radius": 26.0, "amount": 450, "max_amount": 450, "foliage": "berry"})

	# 4. Celtic Player Units (Blue) with 8-Directional Controllers
	# Hero Lord Brennus (Claymore)
	units.append({
		"id": 100, "faction": 1, "name": "Lord Brennus", "type": "Hero Warlord",
		"pos": Vector2(660, 740), "target_pos": Vector2(660, 740), "heading": Vector2(1, 0), "facing": "East",
		"hp": 400.0, "max_hp": 400.0, "dmg": 35.0, "armor": 5.0, "level": 3, "rank": "Experienced", "rank_color": Color("#d97706"),
		"speed": 110.0, "is_hero": true, "radius": 20.0, "color": Color("#2563eb"), "cooldown": 0.0, "worker_state": null,
		"anim_state": "idle", "anim_frame": 0, "weapon_trail": false, "vision_radius": 16
	})

	# 6 Celtic Swordsmen (Line Formation)
	for i in range(6):
		units.append({
			"id": 101 + i, "faction": 1, "name": "Celtic Swordsman", "type": "Swordsman",
			"pos": Vector2(560 + (i * 38), 800), "target_pos": Vector2(560 + (i * 38), 800), "heading": Vector2(1, 0), "facing": "East",
			"hp": 135.0, "max_hp": 135.0, "dmg": 17.0, "armor": 3.0, "level": 1, "rank": "Recruit", "rank_color": Color("#ffffff"),
			"speed": 95.0, "is_hero": false, "radius": 15.0, "color": Color("#3b82f6"), "cooldown": 0.0, "worker_state": null,
			"anim_state": "idle", "anim_frame": 0, "weapon_trail": false, "vision_radius": 12
		})

	# 2 Celtic Archers
	for i in range(2):
		units.append({
			"id": 115 + i, "faction": 1, "name": "Celtic Archer", "type": "Archer",
			"pos": Vector2(520 + (i * 40), 750), "target_pos": Vector2(520 + (i * 40), 750), "heading": Vector2(1, 0), "facing": "East",
			"hp": 85.0, "max_hp": 85.0, "dmg": 15.0, "armor": 1.0, "level": 1, "rank": "Recruit", "rank_color": Color("#ffffff"),
			"speed": 100.0, "is_hero": false, "radius": 13.0, "color": Color("#38bdf8"), "cooldown": 0.0, "worker_state": null,
			"anim_state": "idle", "anim_frame": 0, "weapon_trail": false, "vision_radius": 15
		})

	# 4 Celtic Villagers
	for i in range(4):
		units.append({
			"id": 120 + i, "faction": 1, "name": "Celtic Villager", "type": "Worker",
			"pos": Vector2(540 + (i * 32), 540), "target_pos": Vector2(540 + (i * 32), 540), "heading": Vector2(1, 0), "facing": "East",
			"hp": 65.0, "max_hp": 65.0, "dmg": 6.0, "armor": 0.0, "level": 1, "rank": "Recruit", "rank_color": Color("#ffffff"),
			"speed": 88.0, "is_hero": false, "radius": 13.0, "color": Color("#60a5fa"), "cooldown": 0.0,
			"worker_state": {
				"task": "idle", "target_node": null, "target_building": null,
				"carried_type": "", "carried_amount": 0, "carry_cap": 10, "gather_timer": 0.0
			},
			"anim_state": "idle", "anim_frame": 0, "weapon_trail": false, "vision_radius": 12
		})

	# 5. Roman Enemy Patrol Units (Red)
	for i in range(8):
		units.append({
			"id": 200 + i, "faction": 2, "name": "Roman Legionary", "type": "Legionary",
			"pos": Vector2(1200 + (i * 38), 820), "target_pos": Vector2(1200 + (i * 38), 820), "heading": Vector2(-1, 0), "facing": "West",
			"hp": 145.0, "max_hp": 145.0, "dmg": 16.0, "armor": 4.0, "level": 2, "rank": "Recruit", "rank_color": Color("#ffffff"),
			"speed": 92.0, "is_hero": false, "radius": 15.0, "color": Color("#dc2626"), "cooldown": 0.0, "worker_state": null,
			"anim_state": "idle", "anim_frame": 0, "weapon_trail": false, "vision_radius": 12
		})

	selected_units = [units[0]]
	selected_building = {}
	recalculate_population()
	update_fog_of_war()

func recalculate_population() -> void:
	var total_pop: int = 0
	for u in units:
		if u.faction == 1 and u.hp > 0:
			total_pop += 1
	population = total_pop

	var cap: int = 10
	for b in buildings:
		if b.faction == 1 and b.hp > 0 and b.is_constructed:
			cap += b.get("pop_provided", 0)
	max_population = min(cap, 200)

func update_fog_of_war() -> void:
	# Demote all Visible cells to Explored
	for i in range(fog_grid.size()):
		if fog_grid[i] == 2:
			fog_grid[i] = 1

	# Stamp Vision Circles for Allied Units
	for u in units:
		if u.faction == 1 and u.hp > 0:
			stamp_vision(u.pos, u.get("vision_radius", 12))

	# Stamp Vision Circles for Allied Buildings
	for b in buildings:
		if b.faction == 1 and b.hp > 0:
			stamp_vision(b.pos, b.get("vision_radius", 16))

func stamp_vision(world_pos: Vector2, radius_tiles: int) -> void:
	var cx: int = int(world_pos.x / tile_size)
	var cy: int = int(world_pos.y / tile_size)
	var rad_sq = radius_tiles * radius_tiles

	var min_x = max(0, cx - radius_tiles)
	var max_x = min(grid_w - 1, cx + radius_tiles)
	var min_y = max(0, cy - radius_tiles)
	var max_y = min(grid_h - 1, cy + radius_tiles)

	for y in range(min_y, max_y + 1):
		var dy = y - cy
		var dy_sq = dy * dy
		var row = y * grid_w
		for x in range(min_x, max_x + 1):
			var dx = x - cx
			if (dx * dx) + dy_sq <= rad_sq:
				fog_grid[row + x] = 2 # Visible

func get_fog_at_world(world_pos: Vector2) -> int:
	var tx = int(world_pos.x / tile_size)
	var ty = int(world_pos.y / tile_size)
	if tx < 0 or tx >= grid_w or ty < 0 or ty >= grid_h:
		return 0
	return fog_grid[(ty * grid_w) + tx]

func is_world_visible(world_pos: Vector2) -> bool:
	return get_fog_at_world(world_pos) == 2

func is_world_explored(world_pos: Vector2) -> bool:
	var f = get_fog_at_world(world_pos)
	return f == 1 or f == 2

func _process(delta: float) -> void:
	anim_time += delta
	wave_phase = fmod(wave_phase + delta * 0.8, 1.0)

	# Camera Pan Input (WASD / Arrows)
	var move_dir: Vector2 = Vector2.ZERO
	if Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_UP): move_dir.y -= 1.0
	if Input.is_key_pressed(KEY_S) or Input.is_key_pressed(KEY_DOWN): move_dir.y += 1.0
	if Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_LEFT): move_dir.x -= 1.0
	if Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_RIGHT): move_dir.x += 1.0

	if move_dir.length_squared() > 0:
		camera_pos += move_dir.normalized() * (650.0 * delta / camera_zoom)
		camera_pos.x = clamp(camera_pos.x, 200.0, map_width - 200.0)
		camera_pos.y = clamp(camera_pos.y, 200.0, map_height - 200.0)

	# Fixed Simulation Tick Accumulator (20Hz)
	tick_accumulator += delta
	while tick_accumulator >= fixed_tick_dt:
		tick_accumulator -= fixed_tick_dt
		sim_tick += 1
		simulate_tick(fixed_tick_dt)

	# Update dynamic particles
	update_particles(delta)
	queue_redraw()

func simulate_tick(dt: float) -> void:
	# 1. Update Buildings Production Queues
	for b in buildings:
		if b.hp <= 0: continue
		if not b.queue.is_empty():
			var item = b.queue[0]
			item.progress += dt
			if item.progress >= item.total_time:
				b.queue.remove_at(0)
				spawn_produced_unit(b, item.type)

		# Smoke emission for Blacksmith & damaged buildings
		if b.type == "Blacksmith" and b.is_constructed and (sim_tick % 6 == 0):
			spawn_smoke_particle(b.pos + Vector2(b.size.x * 0.25, -b.size.y * 0.35), Color(0.8, 0.8, 0.8, 0.6))
		if b.hp < b.max_hp * 0.5 and (sim_tick % 4 == 0):
			var fire_col = Color("#f97316") if b.hp < b.max_hp * 0.25 else Color(0.5, 0.5, 0.5, 0.7)
			spawn_smoke_particle(b.pos + Vector2(randf_range(-20, 20), randf_range(-20, 20)), fire_col)

	# 2. Worker State Machine
	for u in units:
		if u.hp <= 0: continue
		var ws = u.worker_state
		if ws != null:
			process_worker_tick(u, ws, dt)

	# 3. Combat, Movement & Directional Facing
	for u in units:
		if u.hp <= 0: continue
		if u.cooldown > 0: u.cooldown -= dt

		var dist_to_target = u.pos.distance_to(u.target_pos)
		if dist_to_target > 4.0:
			var dir = (u.target_pos - u.pos).normalized()
			u.heading = dir
			u.facing = heading_to_facing_str(dir)
			u.pos += dir * (u.speed * dt)
			u.anim_state = "walk"
			u.anim_frame = int(anim_time * 8.0) % 6
		else:
			u.anim_state = "idle"
			u.anim_frame = int(anim_time * 2.0) % 4

		# Attack Engagement
		if u.worker_state != null and (u.worker_state.task == "harvesting" or u.worker_state.task == "returning" or u.worker_state.task == "building"):
			continue

		var closest_enemy = null
		var min_dist: float = 130.0
		for other in units:
			if other.faction != u.faction and other.hp > 0:
				var d = u.pos.distance_to(other.pos)
				if d < min_dist:
					min_dist = d
					closest_enemy = other

		if closest_enemy != null:
			u.heading = (closest_enemy.pos - u.pos).normalized()
			u.facing = heading_to_facing_str(u.heading)
			if min_dist > 35.0:
				u.pos += u.heading * (u.speed * dt)
				u.anim_state = "walk"
			elif u.cooldown <= 0.0:
				u.anim_state = "attack"
				u.weapon_trail = true
				var net_dmg = max(1.0, u.dmg - closest_enemy.armor)
				closest_enemy.hp -= net_dmg
				u.cooldown = 0.8
				add_floating_text("-" + str(int(net_dmg)), closest_enemy.pos, Color("#ef4444"))

				if closest_enemy.hp <= 0:
					u.level += 1
					u.dmg += 3.0
					u.max_hp += 20.0
					u.hp = min(u.hp + 30.0, u.max_hp)
					if u.level >= 5: u.rank = "Veteran"; u.rank_color = Color("#e2e8f0")
					elif u.level >= 3: u.rank = "Experienced"; u.rank_color = Color("#d97706")
					add_floating_text("⭐ LEVEL UP!", u.pos, Color("#fbbf24"))
					dead_corpses.append({"pos": closest_enemy.pos, "color": closest_enemy.color, "heading": closest_enemy.heading})
					recalculate_population()

	# 4. Floating texts
	for i in range(floating_texts.size() - 1, -1, -1):
		var ft = floating_texts[i]
		ft.life -= dt
		ft.pos.y -= 22.0 * dt
		if ft.life <= 0:
			floating_texts.remove_at(i)

	update_fog_of_war()

func heading_to_facing_str(h: Vector2) -> String:
	var angle = atan2(h.y, h.x)
	var deg = rad_to_deg(angle)
	if deg < 0: deg += 360.0
	var sector = int(floor((deg + 22.5) / 45.0)) % 8
	match sector:
		0: return "East"
		1: return "SouthEast"
		2: return "South"
		3: return "SouthWest"
		4: return "West"
		5: return "NorthWest"
		6: return "North"
		7: return "NorthEast"
		_: return "South"

func process_worker_tick(u: Dictionary, ws: Dictionary, dt: float) -> void:
	match ws.task:
		"moving_to_node":
			var node = ws.target_node
			if node == null or node.amount <= 0:
				ws.task = "idle"; return
			if u.pos.distance_to(node.pos) <= node.radius + 15.0:
				ws.task = "harvesting"
				ws.gather_timer = 0.0

		"harvesting":
			var node = ws.target_node
			if node == null or node.amount <= 0:
				if ws.carried_amount > 0:
					ws.task = "returning"
					u.target_pos = get_town_center_pos()
				else:
					ws.task = "idle"
				return

			ws.gather_timer += dt
			if ws.gather_timer >= 0.5:
				ws.gather_timer = 0.0
				var harvest_amt = min(1, node.amount)
				node.amount -= harvest_amt
				ws.carried_type = node.res
				ws.carried_amount += harvest_amt

				# Dust / Chip particle
				if node.foliage == "stone" or node.foliage == "iron":
					spawn_smoke_particle(node.pos + Vector2(randf_range(-10, 10), randf_range(-10, 10)), Color("#94a3b8"))

				if node.amount <= 0:
					add_floating_text(node.type + " Depleted", node.pos, Color("#f97316"))

				if ws.carried_amount >= ws.carry_cap or node.amount <= 0:
					ws.task = "returning"
					u.target_pos = get_town_center_pos()

		"returning":
			var tc_pos = get_town_center_pos()
			if u.pos.distance_to(tc_pos) <= 80.0:
				deposit_resource(ws.carried_type, ws.carried_amount)
				add_floating_text("+" + str(ws.carried_amount) + " " + ws.carried_type, u.pos, Color("#22c55e"))
				ws.carried_amount = 0
				if ws.target_node != null and ws.target_node.amount > 0:
					ws.task = "moving_to_node"
					u.target_pos = ws.target_node.pos
				else:
					ws.task = "idle"

		"moving_to_build":
			var b = ws.target_building
			if b == null or b.hp <= 0 or b.is_constructed:
				ws.task = "idle"; return
			if u.pos.distance_to(b.pos) <= (b.size.x * 0.5) + 20.0:
				ws.task = "building"

		"building":
			var b = ws.target_building
			if b == null or b.hp <= 0 or b.is_constructed:
				ws.task = "idle"; return
			b.hp = min(b.max_hp, b.hp + 25.0 * dt)
			b.build_progress = b.hp / b.max_hp
			if b.hp >= b.max_hp:
				b.is_constructed = true
				b.build_progress = 1.0
				add_floating_text("🏛️ " + b.type + " Complete!", b.pos, Color("#fbbf24"))
				ws.task = "idle"
				recalculate_population()

func get_town_center_pos() -> Vector2:
	for b in buildings:
		if b.faction == 1 and b.type == "Town Center" and b.hp > 0:
			return b.pos
	return Vector2(600, 600)

func deposit_resource(res_type: String, amount: int) -> void:
	match res_type:
		"Food": food += amount
		"Wood": wood += amount
		"Gold": gold += amount
		"Stone": stone += amount
		"Iron": iron += amount

func spawn_smoke_particle(pos: Vector2, color: Color) -> void:
	particles.append({
		"pos": pos, "vel": Vector2(randf_range(-6, 6), randf_range(-25, -15)),
		"color": color, "life": 1.2, "max_life": 1.2, "radius": randf_range(4, 9)
	})

func update_particles(delta: float) -> void:
	for i in range(particles.size() - 1, -1, -1):
		var p = particles[i]
		p.life -= delta
		p.pos += p.vel * delta
		p.radius += 3.0 * delta
		if p.life <= 0:
			particles.remove_at(i)

func spawn_produced_unit(b: Dictionary, unit_type: String) -> void:
	var spawn_pos = b.pos + Vector2(b.size.x * 0.5 + 20.0, 0)
	var rally = b.get("rally_pos", spawn_pos + Vector2(40, 40))

	var new_unit = {
		"id": 100 + units.size(), "faction": 1, "name": unit_type, "type": unit_type,
		"pos": spawn_pos, "target_pos": rally, "heading": Vector2(1, 0), "facing": "East",
		"hp": 130.0, "max_hp": 130.0, "dmg": 16.0, "armor": 3.0, "level": 1, "rank": "Recruit", "rank_color": Color("#ffffff"),
		"speed": 95.0, "is_hero": false, "radius": 14.0, "color": Color("#3b82f6"), "cooldown": 0.0, "worker_state": null,
		"anim_state": "idle", "anim_frame": 0, "weapon_trail": false, "vision_radius": 12
	}
	if unit_type == "Celtic Villager":
		new_unit.hp = 65.0; new_unit.max_hp = 65.0; new_unit.dmg = 6.0; new_unit.armor = 0.0; new_unit.speed = 88.0
		new_unit.color = Color("#60a5fa"); new_unit.type = "Worker"
		new_unit.worker_state = {"task": "idle", "target_node": null, "target_building": null, "carried_type": "", "carried_amount": 0, "carry_cap": 10, "gather_timer": 0.0}
	elif unit_type == "Celtic Archer":
		new_unit.hp = 85.0; new_unit.max_hp = 85.0; new_unit.dmg = 15.0; new_unit.armor = 1.0; new_unit.speed = 100.0
		new_unit.color = Color("#38bdf8"); new_unit.vision_radius = 15; new_unit.type = "Archer"

	units.append(new_unit)
	add_floating_text("⚔️ " + unit_type + " Ready!", spawn_pos, Color("#fbbf24"))
	recalculate_population()

func add_floating_text(text: String, pos: Vector2, color: Color) -> void:
	floating_texts.append({"text": text, "pos": pos, "color": color, "life": 1.4, "max_life": 1.4})

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_B:
			build_menu_open = not build_menu_open
			if not build_menu_open: is_placing_building = false
		elif event.keycode == KEY_V and not selected_building.is_empty():
			try_enqueue_production("Celtic Villager", 50, 0, 0, 0, 0, 8.0, 1)
		elif event.keycode == KEY_S and not selected_building.is_empty():
			try_enqueue_production("Celtic Swordsman", 60, 20, 0, 0, 0, 10.0, 1)
		elif event.keycode == KEY_A and not selected_building.is_empty():
			try_enqueue_production("Celtic Archer", 50, 40, 0, 0, 0, 10.0, 1)
		elif event.keycode == KEY_ESCAPE:
			is_placing_building = false
			build_menu_open = false

	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			camera_zoom = clamp(camera_zoom + 0.1, 0.5, 2.5)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			camera_zoom = clamp(camera_zoom - 0.1, 0.5, 2.5)
		elif event.button_index == MOUSE_BUTTON_LEFT:
			if event.pressed:
				var world_click = screen_to_world(event.position)
				if handle_hud_click(event.position): return
				if is_placing_building:
					place_building_blueprint(world_click)
					return
				is_dragging = true
				drag_start_screen = event.position
				drag_current_screen = event.position
			else:
				if is_dragging:
					is_dragging = false
					finish_drag_selection()
		elif event.button_index == MOUSE_BUTTON_RIGHT and event.pressed:
			var world_click = screen_to_world(event.position)
			if is_placing_building:
				is_placing_building = false; return
			if not selected_building.is_empty():
				selected_building.rally_pos = world_click
				add_floating_text("🚩 Rally Point Set", world_click, Color("#fbbf24"))
				return
			dispatch_contextual_order(world_click)

	elif event is InputEventMouseMotion:
		if is_dragging:
			drag_current_screen = event.position

func finish_drag_selection() -> void:
	var drag_dist = drag_start_screen.distance_to(drag_current_screen)
	var world_start = screen_to_world(drag_start_screen)
	var world_end = screen_to_world(drag_current_screen)

	var min_x = min(world_start.x, world_end.x)
	var max_x = max(world_start.x, world_end.x)
	var min_y = min(world_start.y, world_end.y)
	var max_y = max(world_start.y, world_end.y)

	selected_units.clear()
	selected_building = {}

	if drag_dist < 8.0:
		for u in units:
			if u.faction == 1 and u.hp > 0 and u.pos.distance_to(world_start) <= u.radius + 10.0:
				selected_units.append(u)
				return
		for b in buildings:
			if b.faction == 1 and b.hp > 0:
				var b_rect = Rect2(b.pos - b.size * 0.5, b.size)
				if b_rect.has_point(world_start):
					selected_building = b
					return
	else:
		for u in units:
			if u.faction == 1 and u.hp > 0:
				if u.pos.x >= min_x and u.pos.x <= max_x and u.pos.y >= min_y and u.pos.y <= max_y:
					selected_units.append(u)

func dispatch_contextual_order(world_pos: Vector2) -> void:
	if selected_units.is_empty(): return

	for node in resource_nodes:
		if node.amount > 0 and node.pos.distance_to(world_pos) <= node.radius + 15.0:
			for u in selected_units:
				if u.worker_state != null:
					u.worker_state.task = "moving_to_node"
					u.worker_state.target_node = node
					u.target_pos = node.pos
					add_floating_text("Harvest " + node.res, u.pos, Color("#22c55e"))
			return

	for b in buildings:
		if b.faction == 1 and b.hp > 0 and not b.is_constructed:
			if b.pos.distance_to(world_pos) <= (b.size.x * 0.5) + 15.0:
				for u in selected_units:
					if u.worker_state != null:
						u.worker_state.task = "moving_to_build"
						u.worker_state.target_building = b
						u.target_pos = b.pos
						add_floating_text("Construct " + b.type, u.pos, Color("#38bdf8"))
				return

	var count = selected_units.size()
	for i in range(count):
		var u = selected_units[i]
		var offset = Vector2((i - count / 2.0) * 32.0, 0.0)
		u.target_pos = world_pos + offset
		if u.worker_state != null: u.worker_state.task = "idle"
		add_floating_text("March", u.pos, Color("#60a5fa"))

func handle_hud_click(screen_pos: Vector2) -> bool:
	var vp_size = get_viewport_rect().size
	var bottom_h: float = 160.0

	if build_menu_open and screen_pos.y >= vp_size.y - bottom_h:
		var btn_w = 120.0
		var btn_h = 36.0
		var start_x = 180.0
		var start_y = vp_size.y - 120.0

		var build_opts = [
			{"type": "House", "size": Vector2(65, 65), "w": 50, "s": 0, "g": 0, "hp": 400, "pop": 5},
			{"type": "Barracks", "size": Vector2(95, 95), "w": 150, "s": 0, "g": 0, "hp": 900, "pop": 0},
			{"type": "Blacksmith", "size": Vector2(85, 85), "w": 150, "s": 50, "g": 0, "hp": 700, "pop": 0},
			{"type": "Watchtower", "size": Vector2(65, 65), "w": 50, "s": 125, "g": 0, "hp": 550, "pop": 0}
		]

		for i in range(build_opts.size()):
			var opt = build_opts[i]
			var btn_rect = Rect2(start_x + (i * 130), start_y, btn_w, btn_h)
			if btn_rect.has_point(screen_pos):
				is_placing_building = true
				placing_building_type = opt.type
				placing_grid_size = opt.size
				placing_cost = opt
				build_menu_open = false
				return true

	if not selected_building.is_empty() and screen_pos.y >= vp_size.y - bottom_h:
		var b = selected_building
		var btn_y = vp_size.y - 65.0
		var start_x = 180.0

		if b.type == "Town Center":
			if Rect2(start_x, btn_y, 140, 36).has_point(screen_pos):
				try_enqueue_production("Celtic Villager", 50, 0, 0, 0, 0, 8.0, 1)
				return true
			if Rect2(start_x + 150, btn_y, 140, 36).has_point(screen_pos):
				if food >= 500 and gold >= 300:
					food -= 500; gold -= 300
					current_era = "Imperial Era"
					add_floating_text("🏛️ Imperial Era Advanced!", b.pos, Color("#fbbf24"))
				return true
		elif b.type == "Barracks":
			if Rect2(start_x, btn_y, 150, 36).has_point(screen_pos):
				try_enqueue_production("Celtic Swordsman", 60, 20, 0, 0, 0, 10.0, 1)
				return true
			if Rect2(start_x + 160, btn_y, 150, 36).has_point(screen_pos):
				try_enqueue_production("Celtic Archer", 50, 40, 0, 0, 0, 10.0, 1)
				return true

	return false

func try_enqueue_production(u_type: String, f_cost: int, w_cost: int, g_cost: int, s_cost: int, i_cost: int, train_time: float, pop_cost: int) -> void:
	if selected_building.is_empty(): return
	var b = selected_building
	if b.queue.size() >= 5:
		add_floating_text("Queue Full", b.pos, Color("#ef4444")); return
	if population + pop_cost > max_population:
		add_floating_text("Pop Capped!", b.pos, Color("#ef4444")); return
	if food < f_cost or wood < w_cost or gold < g_cost or stone < s_cost or iron < i_cost:
		add_floating_text("Need Resources", b.pos, Color("#ef4444")); return

	food -= f_cost; wood -= w_cost; gold -= g_cost; stone -= s_cost; iron -= i_cost
	b.queue.append({"type": u_type, "total_time": train_time, "progress": 0.0})
	add_floating_text("Queued: " + u_type, b.pos, Color("#60a5fa"))

func place_building_blueprint(world_pos: Vector2) -> void:
	var snap_pos = Vector2(round(world_pos.x / 50.0) * 50.0, round(world_pos.y / 50.0) * 50.0)
	var opt = placing_cost
	if wood < opt.w or stone < opt.s or gold < opt.g:
		add_floating_text("Need Resources", snap_pos, Color("#ef4444")); return

	wood -= opt.w; stone -= opt.s; gold -= opt.g
	var new_b = {
		"id": 10 + buildings.size(), "faction": 1, "type": opt.type, "style": "celtic", "pos": snap_pos, "size": opt.size,
		"hp": opt.hp * 0.1, "max_hp": float(opt.hp), "color": Color("#2563eb"),
		"is_constructed": false, "build_progress": 0.1, "rally_pos": snap_pos + Vector2(opt.size.x * 0.5 + 20, 0),
		"queue": [], "pop_provided": opt.pop, "vision_radius": 14
	}
	buildings.append(new_b)
	add_floating_text("Scaffolding Raised", snap_pos, Color("#38bdf8"))
	is_placing_building = false

	for u in selected_units:
		if u.worker_state != null:
			u.worker_state.task = "moving_to_build"
			u.worker_state.target_building = new_b
			u.target_pos = snap_pos

func screen_to_world(screen_pos: Vector2) -> Vector2:
	var vp_size = get_viewport_rect().size
	return camera_pos + (screen_pos - vp_size * 0.5) / camera_zoom

func world_to_screen(world_pos: Vector2) -> Vector2:
	var vp_size = get_viewport_rect().size
	return (vp_size * 0.5) + (world_pos - camera_pos) * camera_zoom

func _draw() -> void:
	var vp_size = get_viewport_rect().size

	# 1. Multi-Layered Terrain Tiles & Roads
	draw_terrain_layer(vp_size)

	# 2. Fallen Battlefield Corpses
	for c in dead_corpses:
		if is_world_explored(c.pos):
			var sp = world_to_screen(c.pos)
			draw_circle(sp, 6.0 * camera_zoom, Color(0.2, 0.2, 0.2, 0.7))

	# 3. Illustrated Natural Resources & Foliage
	draw_foliage_resources()

	# 4. Illustrated Buildings & Construction Scaffolding
	draw_illustrated_buildings()

	# 5. Animated Directional Units & Weapon Trails
	draw_directional_units()

	# 6. Dynamic Particles (Smoke, Fire, Mining dust)
	for p in particles:
		if is_world_visible(p.pos):
			var sp = world_to_screen(p.pos)
			var col = p.color
			col.a = p.life / p.max_life
			draw_circle(sp, p.radius * camera_zoom, col)

	# 7. Floating Combat / Level Text
	for ft in floating_texts:
		if is_world_visible(ft.pos):
			var sp = world_to_screen(ft.pos)
			var col = ft.color
			col.a = ft.life / ft.max_life
			draw_string(ThemeDB.fallback_font, sp, ft.text, HORIZONTAL_ALIGNMENT_CENTER, -1, 14, col)

	# 8. Dynamic Line-of-Sight Fog of War Shroud
	draw_fog_of_war_overlay(vp_size)

	# 9. Blueprint Placement Ghost
	if is_placing_building:
		var m_screen = get_viewport().get_mouse_position()
		var snap_world = Vector2(round(screen_to_world(m_screen).x / 50.0) * 50.0, round(screen_to_world(m_screen).y / 50.0) * 50.0)
		var snap_scr = world_to_screen(snap_world)
		var b_size_scr = placing_grid_size * camera_zoom
		var g_rect = Rect2(snap_scr - b_size_scr * 0.5, b_size_scr)
		var can_afford = wood >= placing_cost.w and stone >= placing_cost.s and gold >= placing_cost.g
		var g_col = Color(0.13, 0.77, 0.37, 0.4) if can_afford else Color(0.94, 0.15, 0.15, 0.4)
		draw_rect(g_rect, g_col)
		draw_rect(g_rect, Color("#22c55e") if can_afford else Color("#ef4444"), false, 2.0)

	# 10. Drag Selection Box
	if is_dragging:
		var min_p = Vector2(min(drag_start_screen.x, drag_current_screen.x), min(drag_start_screen.y, drag_current_screen.y))
		var max_p = Vector2(max(drag_start_screen.x, drag_current_screen.x), max(drag_start_screen.y, drag_current_screen.y))
		var rect = Rect2(min_p, max_p - min_p)
		draw_rect(rect, Color(0.13, 0.77, 0.37, 0.25))
		draw_rect(rect, Color("#22c55e"), false, 1.5)

	# 11. RTS HUD & Minimap
	draw_rts_hud(vp_size)

func draw_terrain_layer(vp_size: Vector2) -> void:
	# Calculate visible tile bounds on screen
	var top_left_w = screen_to_world(Vector2.ZERO)
	var bot_right_w = screen_to_world(vp_size)

	var min_tx = clamp(int(top_left_w.x / tile_size) - 1, 0, grid_w - 1)
	var max_tx = clamp(int(bot_right_w.x / tile_size) + 1, 0, grid_w - 1)
	var min_ty = clamp(int(top_left_w.y / tile_size) - 1, 0, grid_h - 1)
	var max_ty = clamp(int(bot_right_w.y / tile_size) + 1, 0, grid_h - 1)

	for ty in range(min_ty, max_ty + 1):
		for tx in range(min_tx, max_tx + 1):
			var tile_type = get_terrain(tx, ty)
			var w_pos = Vector2(tx * tile_size, ty * tile_size)
			var s_pos = world_to_screen(w_pos)
			var s_size = Vector2(tile_size, tile_size) * camera_zoom
			var t_rect = Rect2(s_pos, s_size)

			match tile_type:
				0: # Grass
					draw_rect(t_rect, Color("#166534"))
				1: # FlowerGrass
					draw_rect(t_rect, Color("#15803d"))
					draw_circle(s_pos + s_size * 0.4, 2.5 * camera_zoom, Color("#fde047"))
					draw_circle(s_pos + s_size * 0.7, 2.0 * camera_zoom, Color("#f472b6"))
				2: # Cobblestone Military Road
					draw_rect(t_rect, Color("#64748b"))
					draw_rect(t_rect, Color("#475569"), false, 1.0)
					draw_line(s_pos + Vector2(0, s_size.y * 0.5), s_pos + Vector2(s_size.x, s_size.y * 0.5), Color("#94a3b8"), 1.0)
				3: # Dirt Road
					draw_rect(t_rect, Color("#78350f"))
					draw_rect(t_rect, Color("#92400e"), false, 1.0)
				4: # Shallow Water Shoreline
					draw_rect(t_rect, Color("#0284c7"))
					# Animated Shoreline Wave Foam
					var wave_offset = sin(wave_phase * TAU + (tx + ty)) * 4.0 * camera_zoom
					draw_arc(s_pos + s_size * 0.5, (s_size.x * 0.4) + wave_offset, 0, TAU, 16, Color(1, 1, 1, 0.4), 2.0)
				5: # Deep Water
					draw_rect(t_rect, Color("#0369a1"))
				6: # Stone Cliff Elevation
					draw_rect(t_rect, Color("#334155"))
					draw_line(s_pos, s_pos + Vector2(s_size.x, 0), Color("#94a3b8"), 2.0)
					draw_line(s_pos + Vector2(0, s_size.y), s_pos + s_size, Color("#0f172a"), 2.5)

func draw_foliage_resources() -> void:
	for n in resource_nodes:
		if not is_world_explored(n.pos): continue
		var sp = world_to_screen(n.pos)
		var sr = n.radius * camera_zoom
		var dep_ratio = float(n.amount) / float(n.max_amount) if n.max_amount > 0 else 0.0

		match n.foliage:
			"oak":
				if n.amount <= 0:
					# Tree Stump
					draw_circle(sp, sr * 0.4, Color("#78350f"))
					draw_arc(sp, sr * 0.3, 0, TAU, 16, Color("#451a03"), 1.5)
				else:
					# Oak Tree Trunk & Tiered Leaf Canopy
					draw_rect(Rect2(sp.x - 4 * camera_zoom, sp.y, 8 * camera_zoom, 16 * camera_zoom), Color("#543a14"))
					draw_circle(sp + Vector2(0, -6 * camera_zoom), sr, Color("#15803d"))
					draw_circle(sp + Vector2(-6 * camera_zoom, -12 * camera_zoom), sr * 0.75, Color("#16a34a"))
					draw_circle(sp + Vector2(6 * camera_zoom, -12 * camera_zoom), sr * 0.75, Color("#22c55e"))
			"pine":
				if n.amount <= 0:
					draw_circle(sp, sr * 0.4, Color("#78350f"))
				else:
					draw_rect(Rect2(sp.x - 3 * camera_zoom, sp.y, 6 * camera_zoom, 14 * camera_zoom), Color("#451a03"))
					var top_p = sp + Vector2(0, -sr * 1.5)
					draw_colored_polygon(PackedVector2Array([
						top_p, sp + Vector2(-sr * 0.8, -sr * 0.2), sp + Vector2(sr * 0.8, -sr * 0.2)
					]), Color("#14532d"))
					draw_colored_polygon(PackedVector2Array([
						top_p + Vector2(0, 8 * camera_zoom), sp + Vector2(-sr, sr * 0.4), sp + Vector2(sr, sr * 0.4)
					]), Color("#166534"))
			"gold":
				# Sparkling Gold Vein
				var g_scale = 0.5 + (0.5 * dep_ratio)
				draw_circle(sp, sr * g_scale, Color("#eab308"))
				draw_circle(sp + Vector2(-4, -4) * camera_zoom, sr * 0.5 * g_scale, Color("#fef08a"))
				# Twinkling Sparkle Star
				var spark_alpha = (sin(anim_time * 6.0 + n.id) + 1.0) * 0.5
				draw_string(ThemeDB.fallback_font, sp + Vector2(-6, 4) * camera_zoom, "✨", HORIZONTAL_ALIGNMENT_CENTER, -1, int(14 * camera_zoom), Color(1, 1, 1, spark_alpha))
			"stone":
				var s_scale = 0.5 + (0.5 * dep_ratio)
				draw_colored_polygon(PackedVector2Array([
					sp + Vector2(-sr, sr * 0.5) * s_scale, sp + Vector2(-sr * 0.5, -sr) * s_scale,
					sp + Vector2(sr * 0.8, -sr * 0.6) * s_scale, sp + Vector2(sr, sr * 0.8) * s_scale
				]), Color("#64748b"))
			"iron":
				var i_scale = 0.5 + (0.5 * dep_ratio)
				draw_colored_polygon(PackedVector2Array([
					sp + Vector2(-sr * 0.8, sr * 0.6) * i_scale, sp + Vector2(-sr * 0.4, -sr * 0.8) * i_scale,
					sp + Vector2(sr * 0.7, -sr * 0.4) * i_scale, sp + Vector2(sr * 0.9, sr * 0.5) * i_scale
				]), Color("#334155"))
			"berry":
				draw_circle(sp, sr, Color("#16a34a"))
				var berry_dots = int(ceil(dep_ratio * 5.0))
				for b in range(berry_dots):
					var angle = (b * TAU / 5.0)
					var b_pos = sp + Vector2(cos(angle), sin(angle)) * (sr * 0.5)
					draw_circle(b_pos, 3.5 * camera_zoom, Color("#dc2626"))

		# Resource text label
		if is_world_visible(n.pos):
			draw_string(ThemeDB.fallback_font, sp + Vector2(-30, sr + 14), "%s (%d)" % [n.type, n.amount], HORIZONTAL_ALIGNMENT_CENTER, -1, 11, Color.WHITE)

func draw_illustrated_buildings() -> void:
	for b in buildings:
		if b.hp <= 0 or not is_world_explored(b.pos): continue
		var sp = world_to_screen(b.pos)
		var sz = b.size * camera_zoom
		var b_rect = Rect2(sp - sz * 0.5, sz)

		# Selection border & rally line
		if b == selected_building:
			draw_rect(b_rect.grow(4.0), Color("#fbbf24"), false, 3.0)
			var rp_screen = world_to_screen(b.rally_pos)
			draw_line(sp, rp_screen, Color(0.98, 0.75, 0.14, 0.6), 1.5)
			draw_circle(rp_screen, 6.0, Color("#ef4444"))
			draw_string(ThemeDB.fallback_font, rp_screen + Vector2(10, 4), "🚩 Rally", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#fbbf24"))

		if not b.is_constructed:
			# Scaffolding / Construction Stage
			draw_rect(b_rect, Color("#78350f"), false, 2.0)
			# Diagonal timber cross struts
			draw_line(b_rect.position, b_rect.position + b_rect.size, Color("#b45309"), 2.0)
			draw_line(Vector2(b_rect.position.x, b_rect.position.y + b_rect.size.y), Vector2(b_rect.position.x + b_rect.size.x, b_rect.position.y), Color("#b45309"), 2.0)
			var half_h = b_rect.size.y * b.build_progress
			draw_rect(Rect2(b_rect.position.x, b_rect.position.y + b_rect.size.y - half_h, b_rect.size.x, half_h), Color(0.3, 0.5, 0.8, 0.6))
		else:
			# Completed Architectural Building
			if b.style == "celtic":
				# Celtic Thatched Roof & Timber Walls
				draw_rect(b_rect, Color("#3b82f6"))
				# Thatched roof triangle
				var roof_top = sp + Vector2(0, -sz.y * 0.7)
				draw_colored_polygon(PackedVector2Array([
					roof_top, sp + Vector2(-sz.x * 0.55, -sz.y * 0.2), sp + Vector2(sz.x * 0.55, -sz.y * 0.2)
				]), Color("#ca8a04"))
				draw_rect(b_rect, Color("#1e3a8a"), false, 2.0)
			else:
				# Roman Stone Fortress / Masonry
				draw_rect(b_rect, Color("#991b1b"))
				draw_rect(Rect2(sp.x - sz.x * 0.4, sp.y - sz.y * 0.4, sz.x * 0.8, sz.y * 0.8), Color("#dc2626"))
				draw_rect(b_rect, Color("#fbbf24"), false, 2.5)

		# Health Bar
		var hp_ratio = b.hp / b.max_hp
		draw_rect(Rect2(b_rect.position.x, b_rect.position.y - 12, sz.x, 6), Color("#0f172a"))
		draw_rect(Rect2(b_rect.position.x, b_rect.position.y - 12, sz.x * hp_ratio, 6), Color("#22c55e") if b.is_constructed else Color("#38bdf8"))
		draw_string(ThemeDB.fallback_font, sp + Vector2(-sz.x * 0.4, 4), b.type if b.is_constructed else "%s (%d%%)" % [b.type, int(hp_ratio * 100)], HORIZONTAL_ALIGNMENT_CENTER, -1, 12, Color.WHITE)

func draw_directional_units() -> void:
	for u in units:
		if u.hp <= 0: continue
		# Fog of War Culling: Enemy units hidden unless in active line-of-sight
		if u.faction != 1 and not is_world_visible(u.pos):
			continue

		var sp = world_to_screen(u.pos)
		var sr = u.radius * camera_zoom

		# Selection Ring
		if u in selected_units:
			draw_arc(sp, sr + 6.0, 0, TAU, 32, Color("#22c55e"), 3.0)

		# Unit Directional Facing Vector
		var head_vec = u.heading.normalized() * (sr * 1.3)
		draw_line(sp, sp + head_vec, Color("#fbbf24"), 2.5)

		# Unit Body Sprite Token
		draw_circle(sp, sr, u.color)
		draw_arc(sp, sr, 0, TAU, 32, Color.BLACK, 2.0)

		# Faction & Unit Type Details
		if u.is_hero:
			# Golden Leadership Crown / Radiant Aura
			draw_arc(sp, sr + 4.0, 0, TAU, 32, Color("#fbbf24"), 2.5)
			draw_circle(sp + Vector2(0, -sr * 0.4), 4.0 * camera_zoom, Color("#fde047"))
		elif u.type == "Archer":
			# Bow draw indicator
			draw_arc(sp + head_vec * 0.8, sr * 0.6, 0, PI, 12, Color("#ca8a04"), 2.0)
		elif u.type == "Worker":
			# Villager Tunic Sack
			if u.worker_state != null and u.worker_state.carried_amount > 0:
				draw_circle(sp + Vector2(0, -sr - 16), 4.0, Color("#fbbf24"))
				draw_string(ThemeDB.fallback_font, sp + Vector2(6, -sr - 12), str(u.worker_state.carried_amount), HORIZONTAL_ALIGNMENT_LEFT, -1, 10, Color.WHITE)

		# Melee Weapon Slash Arc Trail
		if u.weapon_trail:
			var swing_angle = atan2(u.heading.y, u.heading.x)
			draw_arc(sp, sr * 1.8, swing_angle - 0.8, swing_angle + 0.8, 16, Color(0.98, 0.75, 0.14, 0.8), 3.0)
			u.weapon_trail = false # Reset after drawing frame

		# Health Bar
		var bar_w = sr * 2.2
		var hp_ratio = u.hp / u.max_hp
		draw_rect(Rect2(sp.x - bar_w * 0.5, sp.y - sr - 10, bar_w, 4), Color("#1e293b"))
		draw_rect(Rect2(sp.x - bar_w * 0.5, sp.y - sr - 10, bar_w * hp_ratio, 4), Color("#22c55e") if hp_ratio > 0.4 else Color("#ef4444"))

		# Veterancy Rank Badge
		if u.level > 1:
			draw_circle(sp + Vector2(sr * 0.7, -sr * 0.7), 4.0 * camera_zoom, u.rank_color)

func draw_fog_of_war_overlay(vp_size: Vector2) -> void:
	var top_left_w = screen_to_world(Vector2.ZERO)
	var bot_right_w = screen_to_world(vp_size)

	var min_tx = clamp(int(top_left_w.x / tile_size) - 1, 0, grid_w - 1)
	var max_tx = clamp(int(bot_right_w.x / tile_size) + 1, 0, grid_w - 1)
	var min_ty = clamp(int(top_left_w.y / tile_size) - 1, 0, grid_h - 1)
	var max_ty = clamp(int(bot_right_w.y / tile_size) + 1, 0, grid_h - 1)

	for ty in range(min_ty, max_ty + 1):
		for tx in range(min_tx, max_tx + 1):
			var f_state = fog_grid[(ty * grid_w) + tx]
			var w_pos = Vector2(tx * tile_size, ty * tile_size)
			var s_pos = world_to_screen(w_pos)
			var s_size = Vector2(tile_size, tile_size) * camera_zoom
			var t_rect = Rect2(s_pos, s_size)

			if f_state == 0:
				# Black Shroud
				draw_rect(t_rect, Color(0, 0, 0, 0.96))
			elif f_state == 1:
				# Explored Fog of War
				draw_rect(t_rect, Color(0.04, 0.08, 0.15, 0.60))

func draw_rts_hud(vp_size: Vector2) -> void:
	# Top Resource Bar
	draw_rect(Rect2(0, 0, vp_size.x, 40), Color("#0f172a"))
	draw_line(Vector2(0, 40), Vector2(vp_size.x, 40), Color("#334155"), 2.0)

	var res_text = "  🌾 Food: %d  |  🪵 Wood: %d  |  🪙 Gold: %d  |  🪨 Stone: %d  |  ⛏️ Iron: %d  |  👥 Pop: %d/%d (Max 200)  |  🏛️ %s" % [
		food, wood, gold, stone, iron, population, max_population, current_era
	]
	draw_string(ThemeDB.fallback_font, Vector2(20, 26), res_text, HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color.WHITE)
	draw_string(ThemeDB.fallback_font, Vector2(vp_size.x - 280, 26), "Crown & Conquest v1.2.0", HORIZONTAL_ALIGNMENT_RIGHT, -1, 13, Color("#fbbf24"))

	# Bottom Selection / Command Panel
	var bottom_h: float = 160.0
	var bottom_rect = Rect2(0, vp_size.y - bottom_h, vp_size.x, bottom_h)
	draw_rect(bottom_rect, Color("#0f172a"))
	draw_line(Vector2(0, vp_size.y - bottom_h), Vector2(vp_size.x, vp_size.y - bottom_h), Color("#334155"), 2.0)

	# Minimap (Bottom-Left)
	var mm_size = 140.0
	var mm_pos = Vector2(10, vp_size.y - bottom_h + 10)
	draw_rect(Rect2(mm_pos, Vector2(mm_size, mm_size)), Color("#022c22"))
	draw_rect(Rect2(mm_pos, Vector2(mm_size, mm_size)), Color("#334155"), false, 2.0)

	# Minimap entities (Culled if in unexplored fog)
	for u in units:
		if u.hp <= 0: continue
		if u.faction != 1 and not is_world_visible(u.pos): continue
		var bx = mm_pos.x + (u.pos.x / map_width) * mm_size
		var by = mm_pos.y + (u.pos.y / map_height) * mm_size
		draw_circle(Vector2(bx, by), 2.5, u.color)

	for b in buildings:
		if b.hp <= 0 or not is_world_explored(b.pos): continue
		var bx = mm_pos.x + (b.pos.x / map_width) * mm_size
		var by = mm_pos.y + (b.pos.y / map_height) * mm_size
		draw_rect(Rect2(Vector2(bx - 3, by - 3), Vector2(6, 6)), b.color)

	# Selection Card (Bottom-Center)
	var card_x = 180.0
	if not selected_building.is_empty():
		var b = selected_building
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 26), "%s  (HP: %d/%d)" % [b.type, int(b.hp), int(b.max_hp)], HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color("#fbbf24"))
		var btn_y = vp_size.y - 65.0
		if b.type == "Town Center":
			draw_action_button(Rect2(card_x, btn_y, 140, 36), "[V] Train Villager", "50 Food")
			draw_action_button(Rect2(card_x + 150, btn_y, 140, 36), "[E] Advance Era", "500F, 300G")
		elif b.type == "Barracks":
			draw_action_button(Rect2(card_x, btn_y, 150, 36), "[S] Train Swordsman", "60F, 20W")
			draw_action_button(Rect2(card_x + 160, btn_y, 150, 36), "[A] Train Archer", "50F, 40W")

		var q_text = "Production Queue (%d/5): " % b.queue.size()
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 52), q_text, HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color("#94a3b8"))
		for q in range(b.queue.size()):
			var item = b.queue[q]
			var qx = card_x + 160 + (q * 110)
			var q_rect = Rect2(qx, vp_size.y - bottom_h + 38, 100, 20)
			draw_rect(q_rect, Color("#1e293b"))
			var prog_ratio = item.progress / item.total_time
			draw_rect(Rect2(qx, vp_size.y - bottom_h + 38, 100 * prog_ratio, 20), Color("#22c55e"))
			draw_rect(q_rect, Color("#475569"), false, 1.0)
			draw_string(ThemeDB.fallback_font, Vector2(qx + 5, vp_size.y - bottom_h + 52), item.type.substr(0, 10), HORIZONTAL_ALIGNMENT_LEFT, -1, 10, Color.WHITE)

	elif build_menu_open:
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 26), "SETTLEMENT BUILD MENU (Press [B] to toggle)", HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color("#38bdf8"))
		var build_opts = [
			{"name": "[H] House (+5 Pop)", "cost": "50 Wood"},
			{"name": "[B] Barracks", "cost": "150 Wood"},
			{"name": "[K] Blacksmith", "cost": "150W, 50S"},
			{"name": "[T] Watchtower", "cost": "50W, 125S"}
		]
		for i in range(build_opts.size()):
			var opt = build_opts[i]
			draw_action_button(Rect2(card_x + (i * 130), vp_size.y - 65.0, 120, 36), opt.name, opt.cost)

	elif selected_units.size() == 1:
		var sel = selected_units[0]
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 30), sel.name + " (" + sel.rank + ") — Facing: " + sel.facing, HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color("#fbbf24"))
		var stats_text = "HP: %d/%d   Damage: %d   Armor: %d   Speed: %d   Level: %d   State: %s" % [
			int(sel.hp), int(sel.max_hp), int(sel.dmg), int(sel.armor), int(sel.speed), int(sel.level), sel.anim_state.capitalize()
		]
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 60), stats_text, HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color.WHITE)

		if sel.is_hero:
			draw_action_button(Rect2(card_x, vp_size.y - 65, 130, 36), "[F1] War Cry", "AoE 40 Dmg")
			draw_action_button(Rect2(card_x + 140, vp_size.y - 65, 140, 36), "[F2] Heroic Strike", "Single 75 Dmg")
		elif sel.worker_state != null:
			var ws = sel.worker_state
			var w_info = "Task: %s | Carried: %d/%d %s" % [ws.task.capitalize(), ws.carried_amount, ws.carry_cap, ws.carried_type]
			draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - 42), w_info, HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color("#60a5fa"))
	elif selected_units.size() > 1:
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 30), "Selected Squad: " + str(selected_units.size()) + " Units", HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color.WHITE)
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 60), "Formations: [1] Line  |  [2] Wedge  |  [3] Shield Wall", HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color("#60a5fa"))
	else:
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 30), "Celtic Kings RTS Engine v1.2.0", HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color("#64748b"))
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 60), "Left-click to select units/buildings. Press [B] for Build Menu. Fog of War active.", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color("#94a3b8"))

	# Controls Guide (Bottom-Right)
	var guide_x = vp_size.x - 360.0
	draw_string(ThemeDB.fallback_font, Vector2(guide_x, vp_size.y - bottom_h + 25), "CELTIC KINGS CONTROLS", HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color("#fbbf24"))
	draw_string(ThemeDB.fallback_font, Vector2(guide_x, vp_size.y - bottom_h + 50), "• [B]: Build Scaffolding (House, Barracks)", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#cbd5e1"))
	draw_string(ThemeDB.fallback_font, Vector2(guide_x, vp_size.y - bottom_h + 70), "• Left Click: Select Building / Unit", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#cbd5e1"))
	draw_string(ThemeDB.fallback_font, Vector2(guide_x, vp_size.y - bottom_h + 90), "• Right Click: Harvest / Rally / Move", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#cbd5e1"))
	draw_string(ThemeDB.fallback_font, Vector2(guide_x, vp_size.y - bottom_h + 110), "• [V]/[S]/[A]: Train Units in Building", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#cbd5e1"))

func draw_action_button(rect: Rect2, title: String, cost: String) -> void:
	draw_rect(rect, Color("#1e3a8a"))
	draw_rect(rect, Color("#60a5fa"), false, 1.5)
	draw_string(ThemeDB.fallback_font, Vector2(rect.position.x + 8, rect.position.y + 16), title, HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color.WHITE)
	draw_string(ThemeDB.fallback_font, Vector2(rect.position.x + 8, rect.position.y + 30), cost, HORIZONTAL_ALIGNMENT_LEFT, -1, 10, Color("#fbbf24"))
