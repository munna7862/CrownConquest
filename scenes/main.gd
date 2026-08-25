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
var population: int = 12
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
		"hp": 1500.0, "max_hp": 1500.0, "color": Color("#1d4ed8")
	})
	buildings.append({
		"id": 2, "faction": 1, "type": "Barracks", "pos": Vector2(480, 600), "size": Vector2(90, 90),
		"hp": 800.0, "max_hp": 800.0, "color": Color("#2563eb")
	})
	buildings.append({
		"id": 3, "faction": 1, "type": "Blacksmith", "pos": Vector2(480, 480), "size": Vector2(80, 80),
		"hp": 600.0, "max_hp": 600.0, "color": Color("#3b82f6")
	})

	# 2. Resource Nodes
	resource_nodes.append({"id": 10, "type": "Gold Mine", "pos": Vector2(750, 500), "radius": 30.0, "amount": 600, "color": Color("#eab308")})
	resource_nodes.append({"id": 11, "type": "Stone Quarry", "pos": Vector2(450, 750), "radius": 28.0, "amount": 450, "color": Color("#94a3b8")})
	resource_nodes.append({"id": 12, "type": "Iron Vein", "pos": Vector2(750, 750), "radius": 25.0, "amount": 350, "color": Color("#475569")})
	resource_nodes.append({"id": 13, "type": "Forest Trees", "pos": Vector2(350, 450), "radius": 35.0, "amount": 800, "color": Color("#15803d")})
	resource_nodes.append({"id": 14, "type": "Berry Bush", "pos": Vector2(650, 400), "radius": 24.0, "amount": 400, "color": Color("#16a34a")})

	# 3. Celtic Player Units (Blue)
	# Hero Brennus
	units.append({
		"id": 100, "faction": 1, "name": "Lord Brennus", "type": "Hero Warlord",
		"pos": Vector2(650, 720), "target_pos": Vector2(650, 720), "hp": 400.0, "max_hp": 400.0, "dmg": 32.0, "armor": 5.0,
		"level": 3, "rank": "Experienced", "rank_color": Color("#d97706"), "speed": 110.0, "is_hero": true, "radius": 20.0,
		"color": Color("#2563eb"), "target_unit": null, "cooldown": 0.0
	})

	# 6 Swordsmen in Line
	for i in range(6):
		units.append({
			"id": 101 + i, "faction": 1, "name": "Celtic Swordsman", "type": "Swordsman",
			"pos": Vector2(580 + (i * 35), 780), "target_pos": Vector2(580 + (i * 35), 780), "hp": 130.0, "max_hp": 130.0, "dmg": 16.0, "armor": 3.0,
			"level": 1, "rank": "Recruit", "rank_color": Color("#ffffff"), "speed": 95.0, "is_hero": false, "radius": 14.0,
			"color": Color("#3b82f6"), "target_unit": null, "cooldown": 0.0
		})

	# 3 Villagers
	for i in range(3):
		units.append({
			"id": 110 + i, "faction": 1, "name": "Celtic Villager", "type": "Worker",
			"pos": Vector2(550 + (i * 30), 550), "target_pos": Vector2(550 + (i * 30), 550), "hp": 60.0, "max_hp": 60.0, "dmg": 5.0, "armor": 0.0,
			"level": 1, "rank": "Recruit", "rank_color": Color("#ffffff"), "speed": 85.0, "is_hero": false, "radius": 12.0,
			"color": Color("#60a5fa"), "target_unit": null, "cooldown": 0.0
		})

	# 4. Roman Enemy Units (Red)
	for i in range(6):
		units.append({
			"id": 200 + i, "faction": 2, "name": "Roman Legionary", "type": "Legionary",
			"pos": Vector2(1100 + (i * 35), 780), "target_pos": Vector2(1100 + (i * 35), 780), "hp": 140.0, "max_hp": 140.0, "dmg": 15.0, "armor": 4.0,
			"level": 2, "rank": "Recruit", "rank_color": Color("#ffffff"), "speed": 90.0, "is_hero": false, "radius": 14.0,
			"color": Color("#dc2626"), "target_unit": null, "cooldown": 0.0
		})

	# Select Hero by default
	selected_units = [units[0]]

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
	# Update units movement & combat
	for u in units:
		if u.hp <= 0: continue

		# Cooldown countdown
		if u.cooldown > 0:
			u.cooldown -= dt

		# Move toward target_pos
		var dist_to_target = u.pos.distance_to(u.target_pos)
		if dist_to_target > 4.0:
			var dir = (u.target_pos - u.pos).normalized()
			u.pos += dir * (u.speed * dt)

		# Auto-aggro / Combat engagement
		var closest_enemy = null
		var min_dist: float = 120.0 # Aggro range
		for other in units:
			if other.faction != u.faction and other.hp > 0:
				var d = u.pos.distance_to(other.pos)
				if d < min_dist:
					min_dist = d
					closest_enemy = other

		if closest_enemy != null:
			# Move to attack range
			if min_dist > 35.0:
				var dir = (closest_enemy.pos - u.pos).normalized()
				u.pos += dir * (u.speed * dt)
			elif u.cooldown <= 0.0:
				# Strike enemy!
				var net_dmg = max(1.0, u.dmg - closest_enemy.armor)
				closest_enemy.hp -= net_dmg
				u.cooldown = 0.8 # Attack cooldown
				add_floating_text("-" + str(int(net_dmg)), closest_enemy.pos, Color("#ef4444"))

				# Check kill & XP Level Up
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

	# Update floating texts
	for i in range(floating_texts.size() - 1, -1, -1):
		var ft = floating_texts[i]
		ft.life -= dt
		ft.pos.y -= 25.0 * dt
		if ft.life <= 0:
			floating_texts.remove_at(i)

func add_floating_text(text: String, pos: Vector2, color: Color) -> void:
	floating_texts.append({"text": text, "pos": pos, "color": color, "life": 1.2, "max_life": 1.2})

func _unhandled_input(event: InputEvent) -> void:
	# Mouse Zoom
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			camera_zoom = clamp(camera_zoom + 0.1, 0.5, 2.5)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			camera_zoom = clamp(camera_zoom - 0.1, 0.5, 2.5)
		elif event.button_index == MOUSE_BUTTON_LEFT:
			if event.pressed:
				is_dragging = true
				drag_start_screen = event.position
				drag_current_screen = event.position
			else:
				if is_dragging:
					is_dragging = false
					finish_drag_selection()
		elif event.button_index == MOUSE_BUTTON_RIGHT and event.pressed:
			# Command dispatch: Move / Attack selected units to right-click world pos
			var world_click = screen_to_world(event.position)
			dispatch_move_order(world_click)

	elif event is InputEventMouseMotion and is_dragging:
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
	if drag_dist < 8.0:
		# Point click selection
		for u in units:
			if u.faction == 1 and u.hp > 0 and u.pos.distance_to(world_start) <= u.radius + 8.0:
				selected_units.append(u)
				break
	else:
		# Box drag selection
		for u in units:
			if u.faction == 1 and u.hp > 0:
				if u.pos.x >= min_x and u.pos.x <= max_x and u.pos.y >= min_y and u.pos.y <= max_y:
					selected_units.append(u)

func dispatch_move_order(world_pos: Vector2) -> void:
	if selected_units.is_empty(): return
	var count = selected_units.size()
	for i in range(count):
		var u = selected_units[i]
		# Spread squad in line formation around target
		var offset = Vector2((i - count / 2.0) * 30.0, 0.0)
		u.target_pos = world_pos + offset
		add_floating_text("Move", u.pos, Color("#60a5fa"))

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
	draw_rect(Rect2(Vector2.ZERO, vp_size), Color("#14532d")) # Lush grass field
	draw_battlefield_grid(vp_size)

	# 2. Resource Nodes
	for n in resource_nodes:
		var sp = world_to_screen(n.pos)
		var sr = n.radius * camera_zoom
		draw_circle(sp, sr, n.color)
		draw_arc(sp, sr, 0, TAU, 32, Color.BLACK, 2.0)
		draw_string(ThemeDB.fallback_font, sp + Vector2(-30, sr + 14), n.type, HORIZONTAL_ALIGNMENT_CENTER, -1, 11, Color.WHITE)

	# 3. Buildings
	for b in buildings:
		var sp = world_to_screen(b.pos)
		var sz = b.size * camera_zoom
		var rect = Rect2(sp - sz * 0.5, sz)
		draw_rect(rect, b.color)
		draw_rect(rect, Color.BLACK, false, 2.0)
		# Building Health Bar
		var hp_ratio = b.hp / b.max_hp
		draw_rect(Rect2(rect.position.x, rect.position.y - 10, sz.x, 6), Color("#1e293b"))
		draw_rect(Rect2(rect.position.x, rect.position.y - 10, sz.x * hp_ratio, 6), Color("#22c55e"))
		draw_string(ThemeDB.fallback_font, sp + Vector2(-sz.x * 0.4, 4), b.type, HORIZONTAL_ALIGNMENT_CENTER, -1, 12, Color.WHITE)

	# 4. Units
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

		# Veterancy Rank Badge
		if u.level > 1:
			draw_circle(sp + Vector2(sr * 0.7, -sr * 0.7), 4.0 * camera_zoom, u.rank_color)

	# 5. Floating Damage / Level-up Text
	for ft in floating_texts:
		var sp = world_to_screen(ft.pos)
		var col = ft.color
		col.a = ft.life / ft.max_life
		draw_string(ThemeDB.fallback_font, sp, ft.text, HORIZONTAL_ALIGNMENT_CENTER, -1, 14, col)

	# 6. Mouse Drag Selection Rectangle
	if is_dragging:
		var min_p = Vector2(min(drag_start_screen.x, drag_current_screen.x), min(drag_start_screen.y, drag_current_screen.y))
		var max_p = Vector2(max(drag_start_screen.x, drag_current_screen.x), max(drag_start_screen.y, drag_current_screen.y))
		var rect = Rect2(min_p, max_p - min_p)
		draw_rect(rect, Color(0.13, 0.77, 0.37, 0.25))
		draw_rect(rect, Color("#22c55e"), false, 1.5)

	# 7. RTS HUD (Top Resource Bar & Bottom Selection/Command Deck)
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
	draw_rect(Rect2(0, 0, vp_size.x, 40), Color("#0f172a")) # Slate dark
	draw_line(Vector2(0, 40), Vector2(vp_size.x, 40), Color("#334155"), 2.0)

	var res_text = "  🌾 Food: %d  |  🪵 Wood: %d  |  🪙 Gold: %d  |  🪨 Stone: %d  |  ⛏️ Iron: %d  |  👥 Pop: %d/%d  |  🏛️ %s" % [
		food, wood, gold, stone, iron, population, max_population, current_era
	]
	draw_string(ThemeDB.fallback_font, Vector2(20, 26), res_text, HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color.WHITE)
	draw_string(ThemeDB.fallback_font, Vector2(vp_size.x - 280, 26), "Crown & Conquest v1.1.0", HORIZONTAL_ALIGNMENT_RIGHT, -1, 13, Color("#fbbf24"))

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

	# Minimap Blips
	for u in units:
		if u.hp <= 0: continue
		var bx = mm_pos.x + (u.pos.x / map_width) * mm_size
		var by = mm_pos.y + (u.pos.y / map_height) * mm_size
		draw_circle(Vector2(bx, by), 2.5, u.color)

	# Selection Card (Bottom-Center)
	var card_x = 180.0
	if selected_units.size() == 1:
		var sel = selected_units[0]
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 30), sel.name + " (" + sel.rank + ")", HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color("#fbbf24"))
		var stats_text = "HP: %d/%d   Damage: %d   Armor: %d   Speed: %d   Level: %d" % [
			int(sel.hp), int(sel.max_hp), int(sel.dmg), int(sel.armor), int(sel.speed), int(sel.level)
		]
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 60), stats_text, HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color.WHITE)

		# If Hero, show ability buttons
		if sel.is_hero:
			draw_rect(Rect2(card_x, vp_size.y - 65, 120, 36), Color("#1e3a8a"))
			draw_rect(Rect2(card_x, vp_size.y - 65, 120, 36), Color("#60a5fa"), false, 1.5)
			draw_string(ThemeDB.fallback_font, Vector2(card_x + 10, vp_size.y - 42), "[F1] War Cry", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color.WHITE)

			draw_rect(Rect2(card_x + 130, vp_size.y - 65, 140, 36), Color("#1e3a8a"))
			draw_rect(Rect2(card_x + 130, vp_size.y - 65, 140, 36), Color("#60a5fa"), false, 1.5)
			draw_string(ThemeDB.fallback_font, Vector2(card_x + 140, vp_size.y - 42), "[F2] Heroic Strike", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color.WHITE)
	elif selected_units.size() > 1:
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 30), "Selected Squad: " + str(selected_units.size()) + " Units", HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color.WHITE)
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 60), "Formations: [1] Line  |  [2] Wedge  |  [3] Shield Wall", HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color("#60a5fa"))
	else:
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 30), "No Unit Selected", HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color("#64748b"))
		draw_string(ThemeDB.fallback_font, Vector2(card_x, vp_size.y - bottom_h + 60), "Left-click or drag box to select units. Right-click to Move / Attack.", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color("#94a3b8"))

	# Controls Guide (Bottom-Right)
	var guide_x = vp_size.x - 360.0
	draw_string(ThemeDB.fallback_font, Vector2(guide_x, vp_size.y - bottom_h + 25), "CONTROLS GUIDE", HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color("#fbbf24"))
	draw_string(ThemeDB.fallback_font, Vector2(guide_x, vp_size.y - bottom_h + 50), "• WASD / Arrows: Pan Camera", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#cbd5e1"))
	draw_string(ThemeDB.fallback_font, Vector2(guide_x, vp_size.y - bottom_h + 70), "• Mouse Wheel: Zoom In / Out", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#cbd5e1"))
	draw_string(ThemeDB.fallback_font, Vector2(guide_x, vp_size.y - bottom_h + 90), "• Left Click / Drag: Select Squad", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#cbd5e1"))
	draw_string(ThemeDB.fallback_font, Vector2(guide_x, vp_size.y - bottom_h + 110), "• Right Click: Move / Attack / Harvest", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color("#cbd5e1"))
