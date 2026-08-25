extends Node2D

# Crown & Conquest — Godot 4 2D Graphical RTS Viewport
# Full interactive RTS presentation layer connected to deterministic simulation logic.

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
var projectiles: Array = []

# Resources
var food: int = 500
var wood: int = 500
var gold: int = 300
var stone: int = 200
var iron: int = 150
var population: int = 13
var max_population: int = 25
var current_era: String = "Classical Era"

# Time & Simulation
var sim_tick: int = 0
var tick_accumulator: float = 0.0
var fixed_tick_dt: float = 0.05 # 20 Hz

func _ready() -> void:
	# Initialize battlefield entities
	setup_battlefield()

func setup_battlefield() -> void:
	# 1. Player Buildings (Celtic)
	buildings.append({
		"id": 1, "faction": 1, "type": "Town Center", "pos": Vector2(600, 600), "size": Vector2(120, 120),
		"hp": 1500.0, "max_hp": 1500.0, "color": Color("#1d4ed8"), "is_constructed": true, "build_progress": 1.0,
		"rally_pos": Vector2(650, 720), "queue": [], "pop_provided": 15
	})
	buildings.append({
		"id": 2, "faction": 1, "type": "Barracks", "pos": Vector2(480, 600), "size": Vector2(90, 90),
		"hp": 800.0, "max_hp": 800.0, "color": Color("#2563eb"), "is_constructed": true, "build_progress": 1.0,
		"rally_pos": Vector2(520, 700), "queue": [], "pop_provided": 0
	})
	buildings.append({
		"id": 3, "faction": 1, "type": "Blacksmith", "pos": Vector2(480, 480), "size": Vector2(80, 80),
		"hp": 600.0, "max_hp": 600.0, "color": Color("#3b82f6"), "is_constructed": true, "build_progress": 1.0,
		"rally_pos": Vector2(500, 560), "queue": [], "pop_provided": 0
	})
	buildings.append({
		"id": 4, "faction": 1, "type": "House", "pos": Vector2(620, 480), "size": Vector2(60, 60),
		"hp": 400.0, "max_hp": 400.0, "color": Color("#1e40af"), "is_constructed": true, "build_progress": 1.0,
		"rally_pos": Vector2(620, 530), "queue": [], "pop_provided": 5
	})

	# 2. Resource Nodes
	resource_nodes.append({"id": 10, "type": "Gold Mine", "res": "Gold", "pos": Vector2(750, 500), "radius": 30.0, "amount": 600, "color": Color("#eab308")})
	resource_nodes.append({"id": 11, "type": "Stone Quarry", "res": "Stone", "pos": Vector2(450, 750), "radius": 28.0, "amount": 450, "color": Color("#94a3b8")})
	resource_nodes.append({"id": 12, "type": "Iron Vein", "res": "Iron", "pos": Vector2(750, 750), "radius": 25.0, "amount": 350, "color": Color("#475569")})
	resource_nodes.append({"id": 13, "type": "Forest Trees", "res": "Wood", "pos": Vector2(350, 450), "radius": 35.0, "amount": 800, "color": Color("#15803d")})
	resource_nodes.append({"id": 14, "type": "Berry Bush", "res": "Food", "pos": Vector2(650, 400), "radius": 24.0, "amount": 400, "color": Color("#16a34a")})

	# 3. Celtic Player Units (Blue)
	# Hero Brennus
	units.append({
		"id": 100, "faction": 1, "name": "Lord Brennus", "type": "Hero Warlord",
		"pos": Vector2(650, 720), "target_pos": Vector2(650, 720), "hp": 400.0, "max_hp": 400.0, "dmg": 32.0, "armor": 5.0,
		"level": 3, "rank": "Experienced", "rank_color": Color("#d97706"), "speed": 110.0, "is_hero": true, "radius": 20.0,
		"color": Color("#2563eb"), "target_unit": null, "cooldown": 0.0, "worker_state": null
	})

	# 6 Swordsmen in Line
	for i in range(6):
		units.append({
			"id": 101 + i, "faction": 1, "name": "Celtic Swordsman", "type": "Swordsman",
			"pos": Vector2(580 + (i * 35), 780), "target_pos": Vector2(580 + (i * 35), 780), "hp": 130.0, "max_hp": 130.0, "dmg": 16.0, "armor": 3.0,
			"level": 1, "rank": "Recruit", "rank_color": Color("#ffffff"), "speed": 95.0, "is_hero": false, "radius": 14.0,
			"color": Color("#3b82f6"), "target_unit": null, "cooldown": 0.0, "worker_state": null
		})

	# 4 Villagers with Worker State Machine
	for i in range(4):
		units.append({
			"id": 110 + i, "faction": 1, "name": "Celtic Villager", "type": "Worker",
			"pos": Vector2(550 + (i * 30), 550), "target_pos": Vector2(550 + (i * 30), 550), "hp": 60.0, "max_hp": 60.0, "dmg": 5.0, "armor": 0.0,
			"level": 1, "rank": "Recruit", "rank_color": Color("#ffffff"), "speed": 85.0, "is_hero": false, "radius": 12.0,
			"color": Color("#60a5fa"), "target_unit": null, "cooldown": 0.0,
			"worker_state": {
				"task": "idle", "target_node": null, "target_building": null,
				"carried_type": "", "carried_amount": 0, "carry_cap": 10, "gather_timer": 0.0
			}
		})

	# 4. Roman Enemy Units (Red)
	for i in range(6):
		units.append({
			"id": 200 + i, "faction": 2, "name": "Roman Legionary", "type": "Legionary",
			"pos": Vector2(1100 + (i * 35), 780), "target_pos": Vector2(1100 + (i * 35), 780), "hp": 140.0, "max_hp": 140.0, "dmg": 15.0, "armor": 4.0,
			"level": 2, "rank": "Recruit", "rank_color": Color("#ffffff"), "speed": 90.0, "is_hero": false, "radius": 14.0,
			"color": Color("#dc2626"), "target_unit": null, "cooldown": 0.0, "worker_state": null
		})

	# Select Hero by default
	selected_units = [units[0]]
	selected_building = {}
	recalculate_population()

func recalculate_population() -> void:
	var total_pop: int = 0
	for u in units:
		if u.faction == 1 and u.hp > 0:
			total_pop += 1
	population = total_pop

	var cap: int = 10 # Base Town Hall
	for b in buildings:
		if b.faction == 1 and b.hp > 0 and b.is_constructed:
			cap += b.get("pop_provided", 0)
	max_population = min(cap, 200)

func _process(delta: float) -> void:
	# Camera Pan Input (WASD / Arrows)
	var move_dir: Vector2 = Vector2.ZERO
	if Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_UP): move_dir.y -= 1.0
	if Input.is_key_pressed(KEY_S) or Input.is_key_pressed(KEY_DOWN): move_dir.y += 1.0
	if Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_LEFT): move_dir.x -= 1.0
	if Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_RIGHT): move_dir.x += 1.0

	if move_dir.length_squared() > 0:
		camera_pos += move_dir.normalized() * (600.0 * delta / camera_zoom)
		camera_pos.x = clamp(camera_pos.x, 200.0, map_width - 200.0)
		camera_pos.y = clamp(camera_pos.y, 200.0, map_height - 200.0)

	# Fixed Simulation Tick Accumulator (20Hz)
	tick_accumulator += delta
	while tick_accumulator >= fixed_tick_dt:
		tick_accumulator -= fixed_tick_dt
		sim_tick += 1
		simulate_tick(fixed_tick_dt)

	# Redraw graphics
	queue_redraw()

func simulate_tick(dt: float) -> void:
	# 1. Update Buildings Production Queues & Construction
	for b in buildings:
		if b.hp <= 0: continue

		# Advance production queue
		if not b.queue.is_empty():
			var item = b.queue[0]
			item.progress += dt
			if item.progress >= item.total_time:
				# Production Complete! Spawn unit
				b.queue.remove_at(0)
				spawn_produced_unit(b, item.type)

	# 2. Update Worker State Machine & Harvesting
	for u in units:
		if u.hp <= 0: continue
		var ws = u.worker_state
		if ws != null:
			process_worker_tick(u, ws, dt)

	# 3. Update Combat & Movement
	for u in units:
		if u.hp <= 0: continue

		# Cooldown countdown
		if u.cooldown > 0:
			u.cooldown -= dt

		# Move toward target_pos if not harvesting in place
		var dist_to_target = u.pos.distance_to(u.target_pos)
		if dist_to_target > 4.0:
			var dir = (u.target_pos - u.pos).normalized()
			u.pos += dir * (u.speed * dt)

		# Auto-aggro / Combat engagement (skip non-aggressive workers)
		if u.worker_state != null and (u.worker_state.task == "harvesting" or u.worker_state.task == "returning" or u.worker_state.task == "building"):
			continue

		var closest_enemy = null
		var min_dist: float = 120.0 # Aggro range
		for other in units:
			if other.faction != u.faction and other.hp > 0:
				var d = u.pos.distance_to(other.pos)
				if d < min_dist:
					min_dist = d
					closest_enemy = other

		if closest_enemy != null:
			if min_dist > 35.0:
				var dir = (closest_enemy.pos - u.pos).normalized()
				u.pos += dir * (u.speed * dt)
			elif u.cooldown <= 0.0:
				var net_dmg = max(1.0, u.dmg - closest_enemy.armor)
				closest_enemy.hp -= net_dmg
				u.cooldown = 0.8
				add_floating_text("-" + str(int(net_dmg)), closest_enemy.pos, Color("#ef4444"))

				if closest_enemy.hp <= 0:
					u.level += 1
					u.dmg += 3.0
					u.max_hp += 20.0
					u.hp = min(u.hp + 30.0, u.max_hp)
					if u.level >= 5:
						u.rank = "Veteran"
						u.rank_color = Color("#e2e8f0")
					elif u.level >= 3:
						u.rank = "Experienced"
						u.rank_color = Color("#d97706")
					add_floating_text("LEVEL UP!", u.pos, Color("#fbbf24"))
					recalculate_population()

	# 4. Update floating texts
	for i in range(floating_texts.size() - 1, -1, -1):
		var ft = floating_texts[i]
		ft.life -= dt
		ft.pos.y -= 25.0 * dt
		if ft.life <= 0:
			floating_texts.remove_at(i)

func process_worker_tick(u: Dictionary, ws: Dictionary, dt: float) -> void:
	match ws.task:
		"moving_to_node":
			var node = ws.target_node
			if node == null or node.amount <= 0:
				ws.task = "idle"
				return
			if u.pos.distance_to(node.pos) <= node.radius + 15.0:
				ws.task = "harvesting"
				ws.gather_timer = 0.0

		"harvesting":
			var node = ws.target_node
			if node == null or node.amount <= 0:
				# Find nearest similar node or return with carried
				if ws.carried_amount > 0:
					ws.task = "returning"
					u.target_pos = get_town_center_pos()
				else:
					ws.task = "idle"
				return

			ws.gather_timer += dt
			if ws.gather_timer >= 0.5: # Harvest 1 resource every 0.5s
				ws.gather_timer = 0.0
				var harvest_amount = min(1, node.amount)
				node.amount -= harvest_amount
				ws.carried_type = node.res
				ws.carried_amount += harvest_amount

				# If node depleted, clear it
				if node.amount <= 0:
					resource_nodes.erase(node)
					add_floating_text(node.type + " Depleted", node.pos, Color("#f97316"))

				if ws.carried_amount >= ws.carry_cap or node.amount <= 0:
					ws.task = "returning"
					u.target_pos = get_town_center_pos()

		"returning":
			var tc_pos = get_town_center_pos()
			if u.pos.distance_to(tc_pos) <= 75.0:
				# Deposit into stockpile!
				deposit_resource(ws.carried_type, ws.carried_amount)
				add_floating_text("+" + str(ws.carried_amount) + " " + ws.carried_type, u.pos, Color("#22c55e"))
				ws.carried_amount = 0

				# Loop back to target node if alive
				if ws.target_node != null and ws.target_node.amount > 0:
					ws.task = "moving_to_node"
					u.target_pos = ws.target_node.pos
				else:
					ws.task = "idle"

		"moving_to_build":
			var b = ws.target_building
			if b == null or b.hp <= 0 or b.is_constructed:
				ws.task = "idle"
				return
			if u.pos.distance_to(b.pos) <= (b.size.x * 0.5) + 20.0:
				ws.task = "building"

		"building":
			var b = ws.target_building
			if b == null or b.hp <= 0 or b.is_constructed:
				ws.task = "idle"
				return
			b.hp = min(b.max_hp, b.hp + 20.0 * dt)
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

func spawn_produced_unit(b: Dictionary, unit_type: String) -> void:
	var spawn_pos = b.pos + Vector2(b.size.x * 0.5 + 20.0, 0)
	var rally = b.get("rally_pos", spawn_pos + Vector2(40, 40))

	var new_unit = {}
	if unit_type == "Celtic Villager":
		new_unit = {
			"id": 110 + units.size(), "faction": 1, "name": "Celtic Villager", "type": "Worker",
			"pos": spawn_pos, "target_pos": rally, "hp": 60.0, "max_hp": 60.0, "dmg": 5.0, "armor": 0.0,
			"level": 1, "rank": "Recruit", "rank_color": Color("#ffffff"), "speed": 85.0, "is_hero": false, "radius": 12.0,
			"color": Color("#60a5fa"), "target_unit": null, "cooldown": 0.0,
			"worker_state": {
				"task": "idle", "target_node": null, "target_building": null,
				"carried_type": "", "carried_amount": 0, "carry_cap": 10, "gather_timer": 0.0
			}
		}
	elif unit_type == "Celtic Archer":
		new_unit = {
			"id": 120 + units.size(), "faction": 1, "name": "Celtic Archer", "type": "Archer",
			"pos": spawn_pos, "target_pos": rally, "hp": 80.0, "max_hp": 80.0, "dmg": 14.0, "armor": 1.0,
			"level": 1, "rank": "Recruit", "rank_color": Color("#ffffff"), "speed": 95.0, "is_hero": false, "radius": 13.0,
			"color": Color("#38bdf8"), "target_unit": null, "cooldown": 0.0, "worker_state": null
		}
	elif unit_type == "Celtic Cavalry":
		new_unit = {
			"id": 130 + units.size(), "faction": 1, "name": "Celtic Cavalry", "type": "Cavalry",
			"pos": spawn_pos, "target_pos": rally, "hp": 160.0, "max_hp": 160.0, "dmg": 20.0, "armor": 4.0,
			"level": 1, "rank": "Recruit", "rank_color": Color("#ffffff"), "speed": 130.0, "is_hero": false, "radius": 16.0,
			"color": Color("#1d4ed8"), "target_unit": null, "cooldown": 0.0, "worker_state": null
		}
	else:
		# Celtic Swordsman default
		new_unit = {
			"id": 101 + units.size(), "faction": 1, "name": "Celtic Swordsman", "type": "Swordsman",
			"pos": spawn_pos, "target_pos": rally, "hp": 130.0, "max_hp": 130.0, "dmg": 16.0, "armor": 3.0,
			"level": 1, "rank": "Recruit", "rank_color": Color("#ffffff"), "speed": 95.0, "is_hero": false, "radius": 14.0,
			"color": Color("#3b82f6"), "target_unit": null, "cooldown": 0.0, "worker_state": null
		}

	units.append(new_unit)
	add_floating_text("⚔️ " + unit_type + " Ready!", spawn_pos, Color("#fbbf24"))
	recalculate_population()

	# If worker and rally point is near a resource node, task to harvest
	if new_unit.worker_state != null:
		for node in resource_nodes:
			if node.amount > 0 and node.pos.distance_to(rally) <= node.radius + 30.0:
				new_unit.worker_state.task = "moving_to_node"
				new_unit.worker_state.target_node = node
				break

func add_floating_text(text: String, pos: Vector2, color: Color) -> void:
	floating_texts.append({"text": text, "pos": pos, "color": color, "life": 1.4, "max_life": 1.4})

func _unhandled_input(event: InputEvent) -> void:
	# Key Hotkeys
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
		elif event.keycode == KEY_C and not selected_building.is_empty():
			try_enqueue_production("Celtic Cavalry", 70, 0, 45, 0, 0, 14.0, 2)
		elif event.keycode == KEY_ESCAPE:
			is_placing_building = false
			build_menu_open = false

	# Mouse Zoom
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			camera_zoom = clamp(camera_zoom + 0.1, 0.5, 2.5)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			camera_zoom = clamp(camera_zoom - 0.1, 0.5, 2.5)
		elif event.button_index == MOUSE_BUTTON_LEFT:
			if event.pressed:
				var world_click = screen_to_world(event.position)
				# Check if clicking in HUD build menu or production buttons
				if handle_hud_click(event.position):
					return

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
				is_placing_building = false
				return

			# Check if right clicking while building is selected (Set Rally Point)
			if not selected_building.is_empty():
				selected_building.rally_pos = world_click
				add_floating_text("🚩 Rally Point Set", world_click, Color("#fbbf24"))
				return

			# Contextual Order for Selected Units
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
		# Point click unit
		for u in units:
			if u.faction == 1 and u.hp > 0 and u.pos.distance_to(world_start) <= u.radius + 8.0:
				selected_units.append(u)
				return

		# Point click building
		for b in buildings:
			if b.faction == 1 and b.hp > 0:
				var b_rect = Rect2(b.pos - b.size * 0.5, b.size)
				if b_rect.has_point(world_start):
					selected_building = b
					return
	else:
		# Box drag selection
		for u in units:
			if u.faction == 1 and u.hp > 0:
				if u.pos.x >= min_x and u.pos.x <= max_x and u.pos.y >= min_y and u.pos.y <= max_y:
					selected_units.append(u)

func dispatch_contextual_order(world_pos: Vector2) -> void:
	if selected_units.is_empty(): return

	# 1. Check if clicking on a Resource Node (Gather order for villagers)
	for node in resource_nodes:
		if node.amount > 0 and node.pos.distance_to(world_pos) <= node.radius + 15.0:
			for u in selected_units:
				if u.worker_state != null:
					u.worker_state.task = "moving_to_node"
					u.worker_state.target_node = node
					u.target_pos = node.pos
					add_floating_text("Harvest " + node.res, u.pos, Color("#22c55e"))
			return

	# 2. Check if clicking on an unfinished Building (Construct order for villagers)
	for b in buildings:
		if b.faction == 1 and b.hp > 0 and not b.is_constructed:
			var b_rect = Rect2(b.pos - b.size * 0.5, b.size)
			if b_rect.has_point(world_pos) or b.pos.distance_to(world_pos) <= (b.size.x * 0.5) + 15.0:
				for u in selected_units:
					if u.worker_state != null:
						u.worker_state.task = "moving_to_build"
						u.worker_state.target_building = b
						u.target_pos = b.pos
						add_floating_text("Construct " + b.type, u.pos, Color("#38bdf8"))
				return

	# 3. Standard formation move / attack
	var count = selected_units.size()
	for i in range(count):
		var u = selected_units[i]
		var offset = Vector2((i - count / 2.0) * 30.0, 0.0)
		u.target_pos = world_pos + offset
		if u.worker_state != null:
			u.worker_state.task = "idle"
		add_floating_text("Move", u.pos, Color("#60a5fa"))

func handle_hud_click(screen_pos: Vector2) -> bool:
	var vp_size = get_viewport_rect().size
	var bottom_h: float = 160.0

	# Check Build Menu button clicks
	if build_menu_open and screen_pos.y >= vp_size.y - bottom_h:
		var btn_w = 120.0
		var btn_h = 36.0
		var start_x = 180.0
		var start_y = vp_size.y - 120.0

		var build_opts = [
			{"type": "House", "size": Vector2(60, 60), "w": 50, "s": 0, "g": 0, "hp": 400, "pop": 5},
			{"type": "Barracks", "size": Vector2(90, 90), "w": 150, "s": 0, "g": 0, "hp": 800, "pop": 0},
			{"type": "Blacksmith", "size": Vector2(80, 80), "w": 150, "s": 50, "g": 0, "hp": 600, "pop": 0},
			{"type": "Watchtower", "size": Vector2(60, 60), "w": 50, "s": 125, "g": 0, "hp": 500, "pop": 0},
			{"type": "Farm", "size": Vector2(70, 70), "w": 60, "s": 0, "g": 0, "hp": 300, "pop": 0}
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

	# Check Building Production action buttons
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
					add_floating_text("🏛️ Advanced to Imperial Era!", b.pos, Color("#fbbf24"))
				else:
					add_floating_text("Insufficient Resources", screen_pos, Color("#ef4444"))
				return true
		elif b.type == "Barracks":
			if Rect2(start_x, btn_y, 150, 36).has_point(screen_pos):
				try_enqueue_production("Celtic Swordsman", 60, 20, 0, 0, 0, 10.0, 1)
				return true
			if Rect2(start_x + 160, btn_y, 150, 36).has_point(screen_pos):
				try_enqueue_production("Celtic Archer", 50, 40, 0, 0, 0, 10.0, 1)
				return true
		elif b.type == "Blacksmith":
			if Rect2(start_x, btn_y, 160, 36).has_point(screen_pos):
				if wood >= 100 and gold >= 50:
					wood -= 100; gold -= 50
					for u in units:
						if u.faction == 1: u.dmg += 2.0
					add_floating_text("⚔️ Forged Blades Upgraded (+2 Dmg)", b.pos, Color("#fbbf24"))
				return true
			if Rect2(start_x + 170, btn_y, 160, 36).has_point(screen_pos):
				if wood >= 75 and iron >= 75:
					wood -= 75; iron -= 75
					for u in units:
						if u.faction == 1: u.armor += 2.0
					add_floating_text("🛡️ Scale Armor Upgraded (+2 Armor)", b.pos, Color("#fbbf24"))
				return true

	return false

func try_enqueue_production(u_type: String, f_cost: int, w_cost: int, g_cost: int, s_cost: int, i_cost: int, train_time: float, pop_cost: int) -> void:
	if selected_building.is_empty(): return
	var b = selected_building
	if b.queue.size() >= 5:
		add_floating_text("Production Queue Full", screen_to_world(get_viewport_rect().size * 0.5), Color("#ef4444"))
		return

	if population + pop_cost > max_population:
		add_floating_text("Population Cap Reached! Build Houses", b.pos, Color("#ef4444"))
		return

	if food < f_cost or wood < w_cost or gold < g_cost or stone < s_cost or iron < i_cost:
		add_floating_text("Insufficient Resources", b.pos, Color("#ef4444"))
		return

	food -= f_cost
	wood -= w_cost
	gold -= g_cost
	stone -= s_cost
	iron -= i_cost

	b.queue.append({
		"type": u_type, "total_time": train_time, "progress": 0.0,
		"f_cost": f_cost, "w_cost": w_cost, "g_cost": g_cost, "pop_cost": pop_cost
	})
	add_floating_text("Queued: " + u_type, b.pos, Color("#60a5fa"))

func place_building_blueprint(world_pos: Vector2) -> void:
	var snapped_x = round(world_pos.x / 50.0) * 50.0
	var snapped_y = round(world_pos.y / 50.0) * 50.0
	var snapped_pos = Vector2(snapped_x, snapped_y)

	var opt = placing_cost
	if wood < opt.w or stone < opt.s or gold < opt.g:
		add_floating_text("Insufficient Resources", snapped_pos, Color("#ef4444"))
		return

	wood -= opt.w
	stone -= opt.s
	gold -= opt.g

	var new_b = {
		"id": 10 + buildings.size(), "faction": 1, "type": opt.type, "pos": snapped_pos, "size": opt.size,
		"hp": opt.hp * 0.1, "max_hp": float(opt.hp), "color": Color("#2563eb"),
		"is_constructed": false, "build_progress": 0.1, "rally_pos": snapped_pos + Vector2(opt.size.x * 0.5 + 20, 0),
		"queue": [], "pop_provided": opt.pop
	}
	buildings.append(new_b)
	add_floating_text("Foundation Placed", snapped_pos, Color("#38bdf8"))
	is_placing_building = false

	# Assign any selected villagers to construct immediately
	for u in selected_units:
		if u.worker_state != null:
			u.worker_state.task = "moving_to_build"
			u.worker_state.target_building = new_b
			u.target_pos = snapped_pos

func screen_to_world(screen_pos: Vector2) -> Vector2:
	var vp_size = get_viewport_rect().size
	var center = vp_size * 0.5
	return camera_pos + (screen_pos - center) / camera_zoom

func world_to_screen(world_pos: Vector2) -> Vector2:
	var vp_size = get_viewport_rect().size
	var center = vp_size * 0.5
	return center + (world_pos - camera_pos) * camera_zoom

func _draw() -> void:
	var vp_size = get_viewport_rect().size

	# 1. Background Grass & Battlefield Grid
	draw_rect(Rect2(Vector2.ZERO, vp_size), Color("#14532d"))
	draw_battlefield_grid(vp_size)

	# 2. Resource Nodes
	for n in resource_nodes:
		var sp = world_to_screen(n.pos)
		var sr = n.radius * camera_zoom
		draw_circle(sp, sr, n.color)
		draw_arc(sp, sr, 0, TAU, 32, Color.BLACK, 2.0)
		draw_string(ThemeDB.fallback_font, sp + Vector2(-30, sr + 14), "%s (%d)" % [n.type, n.amount], HORIZONTAL_ALIGNMENT_CENTER, -1, 11, Color.WHITE)

	# 3. Buildings
	for b in buildings:
		var sp = world_to_screen(b.pos)
		var sz = b.size * camera_zoom
		var rect = Rect2(sp - sz * 0.5, sz)

		# Building Selection Ring / Border
		if b == selected_building:
			draw_rect(rect.grow(4.0), Color("#fbbf24"), false, 3.0)
			# Draw Rally Point Flag
			var rp_screen = world_to_screen(b.rally_pos)
			draw_line(sp, rp_screen, Color(0.98, 0.75, 0.14, 0.6), 1.5)
			draw_circle(rp_screen, 6.0, Color("#ef4444"))
			draw_string(ThemeDB.fallback_font, rp_screen + Vector2(10, 4), "🚩 Rally", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#fbbf24"))

		draw_rect(rect, b.color if b.is_constructed else Color("#475569"))
		draw_rect(rect, Color.BLACK, false, 2.0)

		# Building Health Bar & Construction Progress
		var hp_ratio = b.hp / b.max_hp
		draw_rect(Rect2(rect.position.x, rect.position.y - 10, sz.x, 6), Color("#1e293b"))
		draw_rect(Rect2(rect.position.x, rect.position.y - 10, sz.x * hp_ratio, 6), Color("#22c55e") if b.is_constructed else Color("#38bdf8"))
		draw_string(ThemeDB.fallback_font, sp + Vector2(-sz.x * 0.4, 4), b.type if b.is_constructed else "%s (%d%%)" % [b.type, int(hp_ratio * 100)], HORIZONTAL_ALIGNMENT_CENTER, -1, 12, Color.WHITE)

	# 4. Blueprint Ghost Preview in Placement Mode
	if is_placing_building:
		var mouse_screen = get_viewport().get_mouse_position()
		var world_m = screen_to_world(mouse_screen)
		var snap_x = round(world_m.x / 50.0) * 50.0
		var snap_y = round(world_m.y / 50.0) * 50.0
		var snap_screen = world_to_screen(Vector2(snap_x, snap_y))
		var b_size_screen = placing_grid_size * camera_zoom
		var ghost_rect = Rect2(snap_screen - b_size_screen * 0.5, b_size_screen)

		var can_afford = wood >= placing_cost.w and stone >= placing_cost.s and gold >= placing_cost.g
		var ghost_col = Color(0.13, 0.77, 0.37, 0.45) if can_afford else Color(0.94, 0.15, 0.15, 0.45)
		var ghost_border = Color("#22c55e") if can_afford else Color("#ef4444")
		draw_rect(ghost_rect, ghost_col)
		draw_rect(ghost_rect, ghost_border, false, 2.0)
		draw_string(ThemeDB.fallback_font, snap_screen + Vector2(-40, b_size_screen.y * 0.5 + 16), placing_building_type + (" (Valid)" if can_afford else " (No Resources)"), HORIZONTAL_ALIGNMENT_CENTER, -1, 12, ghost_border)

	# 5. Units
	for u in units:
		if u.hp <= 0: continue
		var sp = world_to_screen(u.pos)
		var sr = u.radius * camera_zoom

		# Selection Ring
		if u in selected_units:
			draw_arc(sp, sr + 6.0, 0, TAU, 32, Color("#22c55e"), 3.0)

		# Unit Body Token
		draw_circle(sp, sr, u.color)
		draw_arc(sp, sr, 0, TAU, 32, Color.BLACK, 2.0)

		# Hero Crown Ring
		if u.is_hero:
			draw_arc(sp, sr + 3.0, 0, TAU, 32, Color("#fbbf24"), 2.0)

		# Unit Health Bar
		var bar_w = sr * 2.2
		var hp_ratio = u.hp / u.max_hp
		draw_rect(Rect2(sp.x - bar_w * 0.5, sp.y - sr - 10, bar_w, 4), Color("#1e293b"))
		draw_rect(Rect2(sp.x - bar_w * 0.5, sp.y - sr - 10, bar_w * hp_ratio, 4), Color("#22c55e") if hp_ratio > 0.4 else Color("#ef4444"))

		# Carried Resources indicator for workers
		if u.worker_state != null and u.worker_state.carried_amount > 0:
			draw_circle(sp + Vector2(0, -sr - 16), 4.0, Color("#fbbf24"))
			draw_string(ThemeDB.fallback_font, sp + Vector2(6, -sr - 12), str(u.worker_state.carried_amount), HORIZONTAL_ALIGNMENT_LEFT, -1, 10, Color.WHITE)

		# Veterancy Rank Badge
		if u.level > 1:
			draw_circle(sp + Vector2(sr * 0.7, -sr * 0.7), 4.0 * camera_zoom, u.rank_color)

	# 6. Floating Damage / Level-up Text
	for ft in floating_texts:
		var sp = world_to_screen(ft.pos)
		var col = ft.color
		col.a = ft.life / ft.max_life
		draw_string(ThemeDB.fallback_font, sp, ft.text, HORIZONTAL_ALIGNMENT_CENTER, -1, 14, col)

	# 7. Mouse Drag Selection Rectangle
	if is_dragging:
		var min_p = Vector2(min(drag_start_screen.x, drag_current_screen.x), min(drag_start_screen.y, drag_current_screen.y))
		var max_p = Vector2(max(drag_start_screen.x, drag_current_screen.x), max(drag_start_screen.y, drag_current_screen.y))
		var rect = Rect2(min_p, max_p - min_p)
		draw_rect(rect, Color(0.13, 0.77, 0.37, 0.25))
		draw_rect(rect, Color("#22c55e"), false, 1.5)

	# 8. RTS HUD
	draw_rts_hud(vp_size)

func draw_battlefield_grid(vp_size: Vector2) -> void:
	var grid_step = 100.0 * camera_zoom
	var start_x = fmod(-camera_pos.x * camera_zoom + vp_size.x * 0.5, grid_step)
	var start_y = fmod(-camera_pos.y * camera_zoom + vp_size.y * 0.5, grid_step)

	var col = Color(0.08, 0.35, 0.18, 0.4)
	var x = start_x
	while x < vp_size.x:
		draw_line(Vector2(x, 0), Vector2(x, vp_size.y), col, 1.0)
		x += grid_step

	var y = start_y
	while y < vp_size.y:
		draw_line(Vector2(0, y), Vector2(vp_size.x, y), col, 1.0)
		y += grid_step

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

	for u in units:
		if u.hp <= 0: continue
		var bx = mm_pos.x + (u.pos.x / map_width) * mm_size
		var by = mm_pos.y + (u.pos.y / map_height) * mm_size
		draw_circle(Vector2(bx, by), 2.5, u.color)

	for b in buildings:
		if b.hp <= 0: continue
		var bx = mm_pos.x + (b.pos.x / map_width) * mm_size
		var by = mm_pos.y + (b.pos.y / map_height) * mm_size
		draw_rect(Rect2(Vector2(bx - 3, by - 3), Vector2(6, 6)), Color("#3b82f6"))

	# Selection Card (Bottom-Center)
	var card_x = 180.0

	# 1. Building Selected -> Render Production Card & Queue
	if not selected_building.is_empty():
		var b = selected_building
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 26), "%s  (HP: %d/%d)" % [b.type, int(b.hp), int(b.max_hp)], HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color("#fbbf24"))

		# Action Buttons
		var btn_y = vp_size.y - 65.0
		if b.type == "Town Center":
			draw_action_button(Rect2(card_x, btn_y, 140, 36), "[V] Train Villager", "50 Food")
			draw_action_button(Rect2(card_x + 150, btn_y, 140, 36), "[E] Advance Era", "500F, 300G")
		elif b.type == "Barracks":
			draw_action_button(Rect2(card_x, btn_y, 150, 36), "[S] Train Swordsman", "60F, 20W")
			draw_action_button(Rect2(card_x + 160, btn_y, 150, 36), "[A] Train Archer", "50F, 40W")
		elif b.type == "Blacksmith":
			draw_action_button(Rect2(card_x, btn_y, 160, 36), "[F] Forged Blades", "100W, 50G (+2 Dmg)")
			draw_action_button(Rect2(card_x + 170, btn_y, 160, 36), "[R] Scale Armor", "75W, 75I (+2 Arm)")

		# Production Queue Slots (up to 5)
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

	# 2. Build Menu Open
	elif build_menu_open:
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 26), "SETTLEMENT BUILD MENU (Press [B] to toggle)", HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color("#38bdf8"))
		var build_opts = [
			{"name": "[H] House (+5 Pop)", "cost": "50 Wood"},
			{"name": "[B] Barracks", "cost": "150 Wood"},
			{"name": "[K] Blacksmith", "cost": "150W, 50S"},
			{"name": "[T] Watchtower", "cost": "50W, 125S"},
			{"name": "[F] Farm", "cost": "60 Wood"}
		]
		for i in range(build_opts.size()):
			var opt = build_opts[i]
			draw_action_button(Rect2(card_x + (i * 130), vp_size.y - 65.0, 120, 36), opt.name, opt.cost)

	# 3. Unit Selection
	elif selected_units.size() == 1:
		var sel = selected_units[0]
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 30), sel.name + " (" + sel.rank + ")", HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color("#fbbf24"))
		var stats_text = "HP: %d/%d   Damage: %d   Armor: %d   Speed: %d   Level: %d" % [
			int(sel.hp), int(sel.max_hp), int(sel.dmg), int(sel.armor), int(sel.speed), int(sel.level)
		]
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 60), stats_text, HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color.WHITE)

		if sel.is_hero:
			draw_rect(Rect2(card_x, vp_size.y - 65, 120, 36), Color("#1e3a8a"))
			draw_rect(Rect2(card_x, vp_size.y - 65, 120, 36), Color("#60a5fa"), false, 1.5)
			draw_string(ThemeDB.fallback_font, Vector2(card_x + 10, vp_size.y - 42), "[F1] War Cry", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color.WHITE)

			draw_rect(Rect2(card_x + 130, vp_size.y - 65, 140, 36), Color("#1e3a8a"))
			draw_rect(Rect2(card_x + 130, vp_size.y - 65, 140, 36), Color("#60a5fa"), false, 1.5)
			draw_string(ThemeDB.fallback_font, Vector2(card_x + 140, vp_size.y - 42), "[F2] Heroic Strike", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color.WHITE)
		elif sel.worker_state != null:
			var ws = sel.worker_state
			var w_info = "Task: %s | Carried: %d/%d %s" % [ws.task.capitalize(), ws.carried_amount, ws.carry_cap, ws.carried_type]
			draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - 42), w_info, HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color("#60a5fa"))
	elif selected_units.size() > 1:
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 30), "Selected Squad: " + str(selected_units.size()) + " Units", HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color.WHITE)
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 60), "Formations: [1] Line  |  [2] Wedge  |  [3] Shield Wall", HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color("#60a5fa"))
	else:
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 30), "No Unit or Building Selected", HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color("#64748b"))
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 60), "Left-click buildings or drag units. Press [B] for Build Menu.", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color("#94a3b8"))

	# Controls Guide (Bottom-Right)
	var guide_x = vp_size.x - 360.0
	draw_string(ThemeDB.fallback_font, Vector2(guide_x, vp_size.y - bottom_h + 25), "CONTROLS GUIDE", HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color("#fbbf24"))
	draw_string(ThemeDB.fallback_font, Vector2(guide_x, vp_size.y - bottom_h + 50), "• [B]: Open Build Menu (House, Barracks)", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#cbd5e1"))
	draw_string(ThemeDB.fallback_font, Vector2(guide_x, vp_size.y - bottom_h + 70), "• Left Click: Select Building / Unit", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#cbd5e1"))
	draw_string(ThemeDB.fallback_font, Vector2(guide_x, vp_size.y - bottom_h + 90), "• Right Click: Harvest / Rally / Move", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#cbd5e1"))
	draw_string(ThemeDB.fallback_font, Vector2(guide_x, vp_size.y - bottom_h + 110), "• [V]/[S]/[A]: Train Units in Building", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#cbd5e1"))

func draw_action_button(rect: Rect2, title: String, cost: String) -> void:
	draw_rect(rect, Color("#1e3a8a"))
	draw_rect(rect, Color("#60a5fa"), false, 1.5)
	draw_string(ThemeDB.fallback_font, Vector2(rect.position.x + 8, rect.position.y + 16), title, HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color.WHITE)
	draw_string(ThemeDB.fallback_font, Vector2(rect.position.x + 8, rect.position.y + 30), cost, HORIZONTAL_ALIGNMENT_LEFT, -1, 10, Color("#fbbf24"))
