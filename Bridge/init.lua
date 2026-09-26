-- Codex Noita Trainer Bridge
-- The companion EXE communicates only through text files in this mod folder.

dofile_once("data/scripts/perks/perk.lua")

local COMMAND_PATH = "mods/codex_noita_trainer_bridge/bridge/command.txt"
local STATUS_PATH = "mods/codex_noita_trainer_bridge/bridge/status.txt"
local WAND_SNAPSHOT_PATH = "mods/codex_noita_trainer_bridge/bridge/wands.txt"
local CHARACTER_SNAPSHOT_PATH = "mods/codex_noita_trainer_bridge/bridge/character.txt"
local NEXT_SEED_PATH = "mods/codex_noita_trainer_bridge/bridge/next_seed.txt"
local HP_DISPLAY_SCALE = 25.0
local OVERLAY_FONT = "data/fonts/font_pixel_white.xml"
local BRIDGE_PROTOCOL = 19
local HEALED_TOTAL_KEY = "CODEX_TRAINER_HEALED_TOTAL_DISPLAY"

local initialized = false
local last_command_id = ""
local last_ok = true
local last_message = "桥接已加载"
local tick_counter = 0
local show_enemy_hp = false
local show_player_coordinates = false
local collect_gold = false
local ghost_vision = false
local peace_mode = false
local invincible_mode = false
local invincible_player = 0
local protection_all_entity = 0
local protection_polymorph_entity = 0
local pending_sheep = nil
local overlay_gui = nil
local observed_healing_player = 0
local observed_healing_hp = nil
local healed_total_display = tonumber(GlobalsGetValue(HEALED_TOTAL_KEY, "0")) or 0
local pending_world_seed = nil
local tracked_wand_ids = {}
local tracked_item_ids = {}
local wand_baseline_done = false
local item_baseline_done = false
local wand_pickup_total = 0
local item_pickup_total = 0

local wand_presets = {
  STARTER = "data/entities/items/starting_wand.xml",
  STARTER_BOMB = "data/entities/items/starting_bomb_wand.xml",
  SHUFFLE_1 = "data/entities/items/wand_level_01.xml",
  SHUFFLE_1_BETTER = "data/entities/items/wand_level_01_better.xml",
  SHUFFLE_2 = "data/entities/items/wand_level_02.xml",
  SHUFFLE_2_BETTER = "data/entities/items/wand_level_02_better.xml",
  SHUFFLE_3 = "data/entities/items/wand_level_03.xml",
  SHUFFLE_3_BETTER = "data/entities/items/wand_level_03_better.xml",
  SHUFFLE_4 = "data/entities/items/wand_level_04.xml",
  SHUFFLE_4_BETTER = "data/entities/items/wand_level_04_better.xml",
  SHUFFLE_5 = "data/entities/items/wand_level_05.xml",
  SHUFFLE_5_BETTER = "data/entities/items/wand_level_05_better.xml",
  SHUFFLE_6 = "data/entities/items/wand_level_06.xml",
  SHUFFLE_6_BETTER = "data/entities/items/wand_level_06_better.xml",
  SHUFFLE_10 = "data/entities/items/wand_level_10.xml",
  NOSHUFFLE_1 = "data/entities/items/wand_unshuffle_01.xml",
  NOSHUFFLE_2 = "data/entities/items/wand_unshuffle_02.xml",
  NOSHUFFLE_3 = "data/entities/items/wand_unshuffle_03.xml",
  NOSHUFFLE_4 = "data/entities/items/wand_unshuffle_04.xml",
  NOSHUFFLE_5 = "data/entities/items/wand_unshuffle_05.xml",
  NOSHUFFLE_6 = "data/entities/items/wand_unshuffle_06.xml",
  NOSHUFFLE_10 = "data/entities/items/wand_unshuffle_10.xml",
  KIEKURAKEPPI = "data/entities/items/wand_kiekurakeppi.xml",
  LEUKALUU = "data/entities/items/wand_leukaluu.xml",
  RUUSU = "data/entities/items/wand_ruusu.xml",
  VALTIKKA = "data/entities/items/wand_valtikka.xml",
  VARPULUUTA = "data/entities/items/wand_varpuluuta.xml",
  VASTA = "data/entities/items/wand_vasta.xml",
  VIHTA = "data/entities/items/wand_vihta.xml"
}

local item_presets = {
  SHINY_ORB = "pickup/physics_gold_orb.xml",
  CRUEL_ORB = "pickup/physics_gold_orb_greed.xml",
  BROKEN_WAND = "pickup/broken_wand.xml",
  EVIL_EYE = "pickup/evil_eye.xml",
  MOON = "pickup/moon.xml",
  SPELL_REFRESH = "pickup/spell_refresh.xml",
  UTILITY_BOX = "pickup/utility_box.xml",
  JAR = "pickup/jar.xml",
  JAR_URINE = "pickup/jar_of_urine.xml",
  GOURD = "pickup/gourd.xml",
  CHAOS_DIE = "pickup/physics_die.xml",
  GREED_DIE = "pickup/physics_greed_die.xml",
  BEAMSTONE = "pickup/beamstone.xml",
  BRIMSTONE = "pickup/brimstone.xml",
  MUSICSTONE = "pickup/musicstone.xml",
  POOPSTONE = "pickup/poopstone.xml",
  STONESTONE = "pickup/stonestone.xml",
  THUNDERSTONE = "pickup/thunderstone.xml",
  WATERSTONE = "pickup/waterstone.xml",
  WANDSTONE = "pickup/wandstone.xml",
  SAFE_HAVEN = "pickup/safe_haven.xml",
  EGG_FIRE = "pickup/egg_fire.xml",
  EGG_HOLLOW = "pickup/egg_hollow.xml",
  EGG_MONSTER = "pickup/egg_monster.xml",
  EGG_PURPLE = "pickup/egg_purple.xml",
  EGG_RED = "pickup/egg_red.xml",
  EGG_SLIME = "pickup/egg_slime.xml",
  EGG_SPIDERS = "pickup/egg_spiders.xml",
  EGG_WORM = "pickup/egg_worm.xml",
  ESSENCE_AIR = "pickup/essence_air.xml",
  ESSENCE_ALCOHOL = "pickup/essence_alcohol.xml",
  ESSENCE_FIRE = "pickup/essence_fire.xml",
  ESSENCE_EARTH = "pickup/essence_laser.xml",
  ESSENCE_WATER = "pickup/essence_water.xml",
  BOOK_00 = "books/book_00.xml",
  BOOK_01 = "books/book_01.xml",
  BOOK_02 = "books/book_02.xml",
  BOOK_03 = "books/book_03.xml",
  BOOK_04 = "books/book_04.xml",
  BOOK_05 = "books/book_05.xml",
  BOOK_06 = "books/book_06.xml",
  BOOK_07 = "books/book_07.xml",
  BOOK_08 = "books/book_08.xml",
  BOOK_09 = "books/book_09.xml",
  BOOK_10 = "books/book_10.xml",
  BOOK_ALL_SPELLS = "books/book_all_spells.xml",
  BOOK_BARREN = "books/book_barren.xml",
  BOOK_BUNKER = "books/book_bunker.xml",
  BOOK_CORPSE = "books/book_corpse.xml",
  BOOK_DIAMOND = "books/book_diamond.xml",
  BOOK_ESSENCES = "books/book_essences.xml",
  BOOK_HINT = "books/book_hint.xml",
  BOOK_MESTARI = "books/book_mestari.xml",
  BOOK_MOON = "books/book_moon.xml",
  BOOK_MUSIC_A = "books/book_music_a.xml",
  BOOK_MUSIC_B = "books/book_music_b.xml",
  BOOK_MUSIC_C = "books/book_music_c.xml",
  BOOK_ROBOT = "books/book_robot.xml",
  BOOK_S_A = "books/book_s_a.xml",
  BOOK_S_B = "books/book_s_b.xml",
  BOOK_S_C = "books/book_s_c.xml",
  BOOK_S_D = "books/book_s_d.xml",
  BOOK_S_E = "books/book_s_e.xml",
  BOOK_TREE = "books/book_tree.xml",
}

local function read_lines(path)
  local handle = io.open(path, "rb")
  if handle == nil then return nil end
  local content = handle:read("*a")
  handle:close()
  if content == nil then return nil end

  local lines = {}
  content = string.gsub(content, "\r", "")
  for line in string.gmatch(content .. "\n", "([^\n]*)\n") do
    table.insert(lines, line)
  end
  return lines
end

local function safe_text(value)
  local text = tostring(value or "")
  text = string.gsub(text, "[\r\n=]", " ")
  return text
end

local function game_message(value)
  local text = tostring(value or "")
  for _, pair in ipairs({ { "；", "; " }, { "：", ": " }, { "（", "(" }, { "）", ")" }, { "“", '"' }, { "”", '"' } }) do
    text = string.gsub(text, pair[1], pair[2])
  end
  return text
end

local function stat_number(key)
  local value = StatsGetValue(key)
  return tonumber(value) or 0
end

local function read_pending_world_seed()
  local lines = read_lines(NEXT_SEED_PATH)
  local value = lines ~= nil and tonumber(lines[1]) or nil
  if value == nil or value < 1 or value > 4294967295 or value ~= math.floor(value) then
    return nil
  end
  return value
end

function OnModPostInit()
  pending_world_seed = read_pending_world_seed()
  if pending_world_seed ~= nil then
    SetWorldSeed(pending_world_seed)
  end
end

local function consume_applied_world_seed()
  if pending_world_seed == nil then return end
  local actual_seed = math.floor(stat_number("world_seed"))
  local playtime = stat_number("playtime")
  if actual_seed == pending_world_seed and playtime >= 0 and playtime < 10 then
    os.remove(NEXT_SEED_PATH)
    pending_world_seed = nil
  end
end

local function write_status()
  local handle = io.open(STATUS_PATH, "wb")
  if handle == nil then return end

  local players = EntityGetWithTag("polymorphed_player")
  if players == nil or #players == 0 then
    players = EntityGetWithTag("player_unit")
  end

  local player = players ~= nil and players[1] or nil
  local camera_x, camera_y = GameGetCameraPos()
  local fields = {
    "connected=1",
    "protocol=" .. tostring(BRIDGE_PROTOCOL),
    "frame=" .. tostring(GameGetFrameNum()),
    "camera_x=" .. string.format("%.3f", camera_x or 0),
    "camera_y=" .. string.format("%.3f", camera_y or 0),
    "playtime=" .. string.format("%.3f", stat_number("playtime")),
    "enemies_killed=" .. tostring(math.floor(stat_number("enemies_killed"))),
    "damage_taken=" .. string.format("%.6f", stat_number("damage_taken") * HP_DISPLAY_SCALE),
    "projectiles_shot=" .. tostring(math.floor(stat_number("projectiles_shot"))),
    "gold_all=" .. tostring(math.floor(stat_number("gold_all"))),
    "world_seed=" .. tostring(math.floor(stat_number("world_seed"))),
    "healed_custom=" .. string.format("%.6f", healed_total_display),
    "places_visited=" .. tostring(math.floor(stat_number("places_visited"))),
    "items=" .. tostring(item_pickup_total),
    "wands_picked=" .. tostring(wand_pickup_total),
    "heart_containers=" .. tostring(math.floor(stat_number("heart_containers"))),
    "kicks=" .. tostring(math.floor(stat_number("kicks"))),
    "show_enemy_hp=" .. (show_enemy_hp and "1" or "0"),
    "show_player_coords=" .. (show_player_coordinates and "1" or "0"),
    "collect_gold=" .. (collect_gold and "1" or "0"),
    "ghost_vision=" .. (ghost_vision and "1" or "0"),
    "last_id=" .. safe_text(last_command_id),
    "last_ok=" .. (last_ok and "1" or "0"),
    "last_message=" .. safe_text(last_message)
  }

  if player ~= nil and EntityGetIsAlive(player) then
    local x, y = EntityGetTransform(player)
    local damage = EntityGetFirstComponentIncludingDisabled(player, "DamageModelComponent")
    table.insert(fields, "player=1")
    table.insert(fields, "x=" .. string.format("%.3f", x or 0))
    table.insert(fields, "y=" .. string.format("%.3f", y or 0))
    if damage ~= nil then
      local hp = ComponentGetValue2(damage, "hp") or 0
      local max_hp = ComponentGetValue2(damage, "max_hp") or 0
      table.insert(fields, "hp=" .. string.format("%.6f", hp * HP_DISPLAY_SCALE))
      table.insert(fields, "max_hp=" .. string.format("%.6f", max_hp * HP_DISPLAY_SCALE))
    end
  else
    table.insert(fields, "player=0")
  end

  handle:write(table.concat(fields, "\n") .. "\n")
  handle:close()
end

local function active_player()
  local players = EntityGetWithTag("polymorphed_player")
  if players ~= nil and #players > 0 and EntityGetIsAlive(players[1]) then
    return players[1]
  end
  players = EntityGetWithTag("player_unit")
  if players ~= nil and #players > 0 and EntityGetIsAlive(players[1]) then
    return players[1]
  end
  return nil
end

local function required_number(text, field_name, minimum, maximum)
  local value = tonumber(text)
  if value == nil then error(field_name .. " 不是有效数字") end
  if value < minimum or value > maximum then
    error(field_name .. " 超出范围 " .. tostring(minimum) .. ".." .. tostring(maximum))
  end
  return value
end

local function required_id(text, field_name)
  if text == nil or string.match(text, "^[A-Z0-9_]+$") == nil then
    error(field_name .. " 无效")
  end
  return text
end

local function player_and_position()
  local player = active_player()
  if player == nil then error("没有找到当前玩家；请先进入一局游戏") end
  local x, y = EntityGetTransform(player)
  return player, x, y
end

local function collect_loaded_gold(player)
  if player == nil then return 0 end
  local x, y = EntityGetTransform(player)
  local nuggets = EntityGetWithTag("gold_nugget") or {}
  local count = 0
  for _, nugget in ipairs(nuggets) do
    if EntityGetIsAlive(nugget) and EntityGetRootEntity(nugget) == nugget then
      EntitySetTransform(nugget, x, y)
      local velocity = EntityGetFirstComponentIncludingDisabled(nugget, "VelocityComponent")
      if velocity ~= nil then ComponentSetValue2(velocity, "mVelocity", 0, 0) end
      count = count + 1
    end
  end
  return count
end

local function update_healed_total(player)
  if player == nil then
    observed_healing_player = 0
    observed_healing_hp = nil
    return
  end
  local damage = EntityGetFirstComponentIncludingDisabled(player, "DamageModelComponent")
  if damage == nil then return end
  local hp = tonumber(ComponentGetValue2(damage, "hp")) or 0
  if observed_healing_player == player and observed_healing_hp ~= nil and hp > observed_healing_hp then
    healed_total_display = healed_total_display + (hp - observed_healing_hp) * HP_DISPLAY_SCALE
    GlobalsSetValue(HEALED_TOTAL_KEY, string.format("%.9g", healed_total_display))
  end
  observed_healing_player = player
  observed_healing_hp = hp
end

local function reveal_loaded_ghosts()
  local count = 0
  for _, entity in ipairs(EntityGetWithTag("mortal") or {}) do
    if entity ~= active_player() and EntityGetIsAlive(entity) then
      EntitySetComponentsWithTagEnabled(entity, "magic_eye", true)
      count = count + 1
    end
  end
  return count
end

local function refresh_spell_uses(player)
  GameRegenItemActionsInPlayer(player)
end

local function world_state_component()
  local world = GameGetWorldStateEntity()
  if world == nil or world == 0 then error("没有找到当前世界状态") end
  local component = EntityGetFirstComponentIncludingDisabled(world, "WorldStateComponent")
  if component == nil then error("当前世界没有 WorldStateComponent") end
  return component
end

local function enable_peace()
  ComponentSetValue2(world_state_component(), "global_genome_relations_modifier", 100.0)
  peace_mode = true
end

local function load_permanent_effect(player, path)
  local effect_entity = LoadGameEffectEntityTo(player, path)
  if effect_entity == nil or effect_entity == 0 then error("无法加载无敌效果 " .. path) end
  local component = EntityGetFirstComponentIncludingDisabled(effect_entity, "GameEffectComponent")
  if component == nil then
    EntityKill(effect_entity)
    error("效果缺少 GameEffectComponent，无法设置永久持续时间")
  end
  ComponentSetValue2(component, "frames", -1)
  return effect_entity
end

local function ensure_invincibility(player)
  if not invincible_mode or player == nil then return end
  if player ~= invincible_player then
    invincible_player = player
    protection_all_entity = 0
    protection_polymorph_entity = 0
  end
  if protection_all_entity == 0 or not EntityGetIsAlive(protection_all_entity) then
    protection_all_entity = load_permanent_effect(player, "data/entities/misc/effect_protection_all.xml")
  end
  if protection_polymorph_entity == 0 or not EntityGetIsAlive(protection_polymorph_entity) then
    protection_polymorph_entity = load_permanent_effect(player, "data/entities/misc/effect_protection_polymorph.xml")
  end

  local damage = EntityGetFirstComponentIncludingDisabled(player, "DamageModelComponent")
  if damage ~= nil then
    local hp = ComponentGetValue2(damage, "hp") or 0
    local max_hp = ComponentGetValue2(damage, "max_hp") or hp
    if hp <= 0 or hp < max_hp then ComponentSetValue2(damage, "hp", max_hp) end
  end
end

local function is_non_player_damageable_entity(entity, player)
  return entity ~= nil and entity ~= player and EntityGetIsAlive(entity) and
    EntityGetRootEntity(entity) == entity and
    EntityGetFirstComponentIncludingDisabled(entity, "DamageModelComponent") ~= nil
end

local CREATURE_CONTROLLER_COMPONENTS = {
  "AnimalAIComponent",
  "PhysicsAIComponent",
  "WormAIComponent",
  "AdvancedFishAIComponent",
  "AIAttackComponent",
  "BossDragonComponent",
  "LimbBossComponent",
  "ControlsComponent"
}

local function has_creature_controller(entity)
  for _, component_type in ipairs(CREATURE_CONTROLLER_COMPONENTS) do
    if EntityGetFirstComponentIncludingDisabled(entity, component_type) ~= nil then
      return true
    end
  end
  return false
end

local function is_non_player_creature(entity, player)
  return is_non_player_damageable_entity(entity, player) and
    EntityGetFirstComponentIncludingDisabled(entity, "GenomeDataComponent") ~= nil and
    has_creature_controller(entity)
end

local function inflict_normal_lethal_damage(entity, responsible)
  local damage = EntityGetFirstComponentIncludingDisabled(entity, "DamageModelComponent")
  if damage == nil then return false end
  local hp = math.abs(ComponentGetValue2(damage, "hp") or 0)
  local max_hp = math.abs(ComponentGetValue2(damage, "max_hp") or 0)
  local amount = math.max(1000000, hp + max_hp + 1000)
  local x, y = EntityGetTransform(entity)
  local old_curse_multiplier = ComponentObjectGetValue2(damage, "damage_multipliers", "curse")
  ComponentObjectSetValue2(damage, "damage_multipliers", "curse", 1.0)
  EntityInflictDamage(entity, amount, "DAMAGE_CURSE", "$damage_curse", "NONE", 0, 0,
    responsible or 0, x or 0, y or 0, 0)
  if EntityGetIsAlive(entity) and old_curse_multiplier ~= nil then
    ComponentObjectSetValue2(damage, "damage_multipliers", "curse", old_curse_multiplier)
  end
  return true
end

local function kill_loaded_entities(tag, creatures_only)
  local player = active_player()
  if player == nil then error("没有找到当前玩家；请先进入一局游戏") end
  local entities = EntityGetWithTag(tag) or {}
  local count = 0
  for _, entity in ipairs(entities) do
    local valid = creatures_only and is_non_player_creature(entity, player) or
      (not creatures_only and is_non_player_damageable_entity(entity, player))
    if valid and inflict_normal_lethal_damage(entity, player) then
      count = count + 1
    end
  end
  return count
end

local function load_temporary_effect(entity, path, frames)
  local effect_entity = LoadGameEffectEntityTo(entity, path)
  if effect_entity == nil or effect_entity == 0 then return 0 end
  local component = EntityGetFirstComponentIncludingDisabled(effect_entity, "GameEffectComponent")
  if component ~= nil then ComponentSetValue2(component, "frames", frames) end
  return effect_entity
end

local function explode_loaded_creatures(tag)
  local player = active_player()
  if player == nil then error("没有找到当前玩家；请先进入一局游戏") end
  load_temporary_effect(player, "data/entities/misc/effect_protection_explosion.xml", 180)

  local targets = {}
  for _, entity in ipairs(EntityGetWithTag(tag) or {}) do
    if is_non_player_creature(entity, player) then
      local effect = load_temporary_effect(entity, "data/entities/misc/effect_exploding_corpse.xml", 180)
      if effect ~= 0 then
        table.insert(targets, entity)
      end
    end
  end

  local count = 0
  for _, entity in ipairs(targets) do
    if EntityGetIsAlive(entity) and inflict_normal_lethal_damage(entity, player) then
      count = count + 1
    end
  end
  return count
end

local function gold_value(entity)
  for _, component in ipairs(EntityGetComponentIncludingDisabled(entity, "VariableStorageComponent") or {}) do
    if ComponentGetValue2(component, "name") == "gold_value" then
      return tonumber(ComponentGetValue2(component, "value_int")) or 10
    end
  end
  return 10
end

local function create_gold_explosion(nugget, player, pickup_count)
  local x, y = EntityGetTransform(nugget)
  local explosion = EntityLoad("data/entities/misc/perks/gold_explosion.xml", x or 0, y or 0)
  if explosion == nil or explosion == 0 then return false end
  local projectile = EntityGetFirstComponentIncludingDisabled(explosion, "ProjectileComponent")
  if projectile == nil then
    EntityKill(explosion)
    return false
  end

  local value = gold_value(nugget)
  local radius = (value / (10 + value * 0.01)) + math.min(60, 16 + (pickup_count - 1) * 4)
  local damage = ((value / (10 + value * 0.01)) + 1.5) * math.min(2.0, 0.4 + (pickup_count - 1) * 0.2)
  ComponentObjectSetValue2(projectile, "config_explosion", "explosion_radius", radius)
  ComponentObjectSetValue2(projectile, "config_explosion", "damage", damage)
  ComponentObjectSetValue2(projectile, "config_explosion", "sparks_count_min", math.max(5, math.floor(radius * 0.75)))
  ComponentObjectSetValue2(projectile, "config_explosion", "sparks_count_max", math.max(10, math.floor(radius * 1.25)))
  ComponentSetValue2(projectile, "mWhoShot", player)
  local genome = EntityGetFirstComponentIncludingDisabled(player, "GenomeDataComponent")
  if genome ~= nil then ComponentSetValue2(projectile, "mShooterHerdId", ComponentGetValue2(genome, "herd_id")) end
  ComponentObjectSetValue2(projectile, "config_explosion", "dont_damage_this", player)
  EntityKill(explosion)
  return true
end

local function explode_loaded_gold()
  local player = active_player()
  if player == nil then error("没有找到当前玩家；请先进入一局游戏") end
  local pickup_count = math.max(1, tonumber(GlobalsGetValue("PERK_PICKED_EXPLODING_GOLD_PICKUP_COUNT", "0")) or 0)
  local count = 0
  for _, nugget in ipairs(EntityGetWithTag("gold_nugget") or {}) do
    if EntityGetIsAlive(nugget) and create_gold_explosion(nugget, player, pickup_count) then
      EntityKill(nugget)
      count = count + 1
    end
  end
  return count
end

local function charm_loaded_creatures()
  local player = active_player()
  if player == nil then error("没有找到当前玩家；请先进入一局游戏") end
  local player_genome = EntityGetFirstComponentIncludingDisabled(player, "GenomeDataComponent")
  if player_genome == nil then error("玩家没有 GenomeDataComponent") end
  local player_herd = ComponentGetValue2(player_genome, "herd_id")
  local entities = EntityGetWithTag("mortal") or {}
  local count = 0
  for _, entity in ipairs(entities) do
    if is_non_player_creature(entity, player) then
      local genome = EntityGetFirstComponentIncludingDisabled(entity, "GenomeDataComponent")
      if genome ~= nil and player_herd ~= nil then
        ComponentSetValue2(genome, "herd_id", player_herd)
      end
      local effect = GetGameEffectLoadTo(entity, "CHARM", true)
      if effect ~= nil and effect ~= 0 then
        ComponentSetValue2(effect, "frames", -1)
        count = count + 1
      end
    end
  end
  return count
end

local function freeze_loaded_creatures()
  local player = active_player()
  if player == nil then error("没有找到当前玩家；请先进入一局游戏") end
  local entities = EntityGetWithTag("mortal") or {}
  local count = 0
  for _, entity in ipairs(entities) do
    if is_non_player_creature(entity, player) then
      local effect = GetGameEffectLoadTo(entity, "FROZEN", true)
      if effect ~= nil and effect ~= 0 then
        ComponentSetValue2(effect, "frames", -1)
        count = count + 1
      end
    end
  end
  return count
end

local function inventory_wands(player)
  local result = {}
  for _, item in ipairs(GameGetAllInventoryItems(player) or {}) do
    if EntityGetIsAlive(item) and EntityHasTag(item, "wand") then
      local item_component = EntityGetFirstComponentIncludingDisabled(item, "ItemComponent")
      local slot_x, slot_y = 999, 999
      if item_component ~= nil then
        slot_x, slot_y = ComponentGetValue2(item_component, "inventory_slot")
        slot_x = tonumber(slot_x) or 999
        slot_y = tonumber(slot_y) or 999
      end
      table.insert(result, { entity = item, slot_x = slot_x, slot_y = slot_y })
    end
  end
  table.sort(result, function(left, right)
    if left.slot_y ~= right.slot_y then return left.slot_y < right.slot_y end
    if left.slot_x ~= right.slot_x then return left.slot_x < right.slot_x end
    return left.entity < right.entity
  end)
  while #result > 4 do table.remove(result) end
  return result
end

-- 统计本局进入过法杖栏/物品栏的不同实体数：
-- 每根魔杖或物品按实体 ID 区分，第一次进入对应栏位时 +1；
-- 同一实体被换出再换回、更换法术等都不重复计数。
-- 开局自带的起始魔杖、炸弹魔杖与起始药水在首次观察到时做基线，不计数。
-- 配套模组自游戏启动即运行，因此中途打开修改器也会保留本局已有的计数。
local function track_pickups(player)
  if player == nil or player == 0 then return end
  local saw_wand = false
  local saw_item = false
  for _, entity in ipairs(GameGetAllInventoryItems(player) or {}) do
    if EntityGetIsAlive(entity) then
      if EntityHasTag(entity, "wand") then
        saw_wand = true
        if not wand_baseline_done then
          tracked_wand_ids[entity] = true
        elseif tracked_wand_ids[entity] == nil then
          tracked_wand_ids[entity] = true
          wand_pickup_total = wand_pickup_total + 1
        end
      elseif not EntityHasTag(entity, "card_action") then
        saw_item = true
        if not item_baseline_done then
          tracked_item_ids[entity] = true
        elseif tracked_item_ids[entity] == nil then
          tracked_item_ids[entity] = true
          item_pickup_total = item_pickup_total + 1
        end
      end
    end
  end
  if saw_wand then wand_baseline_done = true end
  if saw_item then item_baseline_done = true end
end

local function current_inventory_wand(player, entity_id)
  for _, entry in ipairs(inventory_wands(player)) do
    if entry.entity == entity_id then return entity_id end
  end
  error("所选魔杖已不在当前魔杖栏中；请重新读取")
end

local function wand_number(ability, object_name, field_name)
  return tonumber(ComponentObjectGetValue2(ability, object_name, field_name)) or 0
end

local function write_wand_snapshot(request_id, player)
  local handle = io.open(WAND_SNAPSHOT_PATH, "wb")
  if handle == nil then error("无法写入魔杖栏读取结果") end
  handle:write("request_id\t" .. tostring(request_id) .. "\n")
  local count = 0
  for index, entry in ipairs(inventory_wands(player)) do
    local ability = EntityGetFirstComponentIncludingDisabled(entry.entity, "AbilityComponent")
    if ability ~= nil then
      local shuffle = ComponentObjectGetValue2(ability, "gun_config", "shuffle_deck_when_empty")
      local is_shuffle = shuffle == true or shuffle == 1 or shuffle == "1"
      local fields = {
        tostring(entry.entity),
        tostring(entry.slot_x >= 0 and entry.slot_x or (index - 1)),
        is_shuffle and "1" or "0",
        tostring(math.floor(wand_number(ability, "gun_config", "actions_per_round"))),
        string.format("%.9g", wand_number(ability, "gunaction_config", "fire_rate_wait") / 60.0),
        string.format("%.9g", wand_number(ability, "gun_config", "reload_time") / 60.0),
        string.format("%.9g", tonumber(ComponentGetValue2(ability, "mana_max")) or 0),
        string.format("%.9g", tonumber(ComponentGetValue2(ability, "mana_charge_speed")) or 0),
        tostring(math.floor(wand_number(ability, "gun_config", "deck_capacity"))),
        string.format("%.9g", wand_number(ability, "gunaction_config", "spread_degrees")),
        string.format("%.9g", wand_number(ability, "gunaction_config", "speed_multiplier"))
      }
      handle:write(table.concat(fields, "\t") .. "\n")
      count = count + 1
    end
  end
  handle:close()
  return count
end

local function write_character_snapshot(request_id, player)
  local wallet = EntityGetFirstComponentIncludingDisabled(player, "WalletComponent")
  if wallet == nil then error("玩家没有 WalletComponent") end
  local damage = EntityGetFirstComponentIncludingDisabled(player, "DamageModelComponent")
  if damage == nil then error("玩家没有 DamageModelComponent") end
  local character = EntityGetFirstComponentIncludingDisabled(player, "CharacterDataComponent")
  if character == nil then error("玩家没有 CharacterDataComponent") end
  local platforming = EntityGetFirstComponentIncludingDisabled(player, "CharacterPlatformingComponent")
  if platforming == nil then error("玩家没有 CharacterPlatformingComponent") end

  local money = tonumber(ComponentGetValue2(wallet, "money")) or 0
  local hp = tonumber(ComponentGetValue2(damage, "hp")) or 0
  local max_hp = tonumber(ComponentGetValue2(damage, "max_hp")) or 0
  local lung_capacity = tonumber(ComponentGetValue2(damage, "air_in_lungs_max")) or 0
  local current_air = tonumber(ComponentGetValue2(damage, "air_in_lungs")) or 0
  local handle = io.open(CHARACTER_SNAPSHOT_PATH, "wb")
  if handle == nil then error("无法写入角色信息读取结果") end
  handle:write("request_id\t" .. tostring(request_id) .. "\n")
  handle:write(table.concat({
    string.format("%.0f", money),
    string.format("%.9g", hp * HP_DISPLAY_SCALE),
    string.format("%.9g", max_hp * HP_DISPLAY_SCALE),
    string.format("%.9g", lung_capacity),
    string.format("%.9g", tonumber(ComponentGetValue2(character, "fly_time_max")) or 0),
    string.format("%.9g", tonumber(ComponentGetValue2(character, "mFlyingTimeLeft")) or 0),
    string.format("%.9g", tonumber(ComponentGetValue2(character, "fly_recharge_spd")) or 0),
    string.format("%.9g", tonumber(ComponentGetValue2(character, "fly_recharge_spd_ground")) or 0),
    string.format("%.9g", current_air),
    string.format("%.9g", tonumber(ComponentGetValue2(platforming, "run_velocity")) or 0),
    string.format("%.9g", tonumber(ComponentGetValue2(platforming, "fly_velocity_x")) or 0),
    string.format("%.9g", tonumber(ComponentGetValue2(platforming, "jump_velocity_y")) or 0),
    string.format("%.9g", tonumber(ComponentGetValue2(platforming, "pixel_gravity")) or 0)
  }, "\t") .. "\n")
  handle:close()
end

local function seconds_to_frames(seconds)
  local frames = seconds * 60.0
  if frames >= 0 then return math.floor(frames + 0.5) end
  return math.ceil(frames - 0.5)
end

local function add_permanent_spell(wand, spell_id)
  local action_entity = CreateItemActionEntity(spell_id)
  if action_entity == nil or action_entity == 0 then error("无法创建法术 " .. spell_id) end
  EntityAddChild(wand, action_entity)
  local item_component = EntityGetFirstComponentIncludingDisabled(action_entity, "ItemComponent")
  if item_component == nil then
    EntityKill(action_entity)
    error("法术缺少 ItemComponent " .. spell_id)
  end
  ComponentSetValue(item_component, "permanently_attached", "1")
  EntitySetComponentsWithTagEnabled(action_entity, "enabled_in_world", false)
  local ability = EntityGetFirstComponentIncludingDisabled(wand, "AbilityComponent")
  local capacity = wand_number(ability, "gun_config", "deck_capacity")
  ComponentObjectSetValue2(ability, "gun_config", "deck_capacity", math.floor(capacity) + 1)
end

-- 判断拾取后的法术卡片是否进入了玩家法术栏（inventory_full 组）。
-- 法术栏已满时原版会把卡片塞进普通物品栏，此时返回 false。
local function spell_landed_in_spell_inventory(player, spell)
  if spell == nil or spell == 0 then return false end
  local parent = EntityGetParent(spell)
  if parent == nil or parent == 0 then return false end
  local parent_name = EntityGetName(parent)
  return parent_name == "inventory_full" or parent == player
end

local function remove_invincibility_effects()
  invincible_mode = false
  if protection_all_entity ~= 0 and EntityGetIsAlive(protection_all_entity) then
    EntityKill(protection_all_entity)
  end
  if protection_polymorph_entity ~= 0 and EntityGetIsAlive(protection_polymorph_entity) then
    EntityKill(protection_polymorph_entity)
  end
  protection_all_entity = 0
  protection_polymorph_entity = 0
end

local function execute_command(lines)
  local command = lines[2]
  if command == "NOOP" then return "已连接" end

  if command == "SET_SHOW_ENEMY_HP" then
    show_enemy_hp = lines[3] == "1"
    return show_enemy_hp and "已开启怪物血量显示" or "已关闭怪物血量显示"
  end

  if command == "SET_SHOW_PLAYER_COORDS" then
    show_player_coordinates = lines[3] == "1"
    return show_player_coordinates and "已开启玩家坐标显示" or "已关闭玩家坐标显示"
  end

  if command == "SET_COLLECT_GOLD" then
    collect_gold = lines[3] == "1"
    local count = 0
    if collect_gold then count = collect_loaded_gold(active_player()) end
    return collect_gold and ("已开启持续收集金块；本次找到 " .. tostring(count) .. " 个") or "已关闭持续收集金块"
  end

  if command == "SET_GHOST_VISION" then
    ghost_vision = lines[3] == "1"
    local count = 0
    if ghost_vision then count = reveal_loaded_ghosts() end
    return ghost_vision and ("已开启幽灵透视；本次检查 " .. tostring(count) .. " 个实体") or "已关闭幽灵透视"
  end

  if command == "TELEPORT_PLAYER" then
    local player = player_and_position()
    local target_x = required_number(lines[3], "X 坐标", -1000000000, 1000000000)
    local target_y = required_number(lines[4], "Y 坐标", -1000000000, 1000000000)
    EntitySetTransform(player, target_x, target_y)
    local character_data = EntityGetFirstComponentIncludingDisabled(player, "CharacterDataComponent")
    if character_data ~= nil then ComponentSetValue2(character_data, "mVelocity", 0, 0) end
    local velocity = EntityGetFirstComponentIncludingDisabled(player, "VelocityComponent")
    if velocity ~= nil then ComponentSetValue2(velocity, "mVelocity", 0, 0) end
    return "已传送到 (" .. tostring(target_x) .. ", " .. tostring(target_y) .. ")"
  end

  if command == "SET_HP" then
    local player = player_and_position()
    local display_hp = required_number(lines[3], "生命值", 0.04, 1000000000)
    local damage = EntityGetFirstComponentIncludingDisabled(player, "DamageModelComponent")
    if damage == nil then error("玩家没有 DamageModelComponent") end
    local max_hp = ComponentGetValue2(damage, "max_hp")
    local internal_hp = display_hp / HP_DISPLAY_SCALE
    if internal_hp > max_hp then
      ComponentSetValue2(damage, "max_hp", internal_hp)
    end
    ComponentSetValue2(damage, "hp", internal_hp)
    return "当前生命已设为 " .. tostring(display_hp)
  end

  if command == "SET_MAX_HP" then
    local player = player_and_position()
    local display_hp = required_number(lines[3], "最大生命", 1, 1000000000)
    local heal = lines[4] == "1"
    local damage = EntityGetFirstComponentIncludingDisabled(player, "DamageModelComponent")
    if damage == nil then error("玩家没有 DamageModelComponent") end
    local internal_hp = display_hp / HP_DISPLAY_SCALE
    ComponentSetValue2(damage, "max_hp", internal_hp)
    local current = ComponentGetValue2(damage, "hp")
    if heal or current > internal_hp then ComponentSetValue2(damage, "hp", internal_hp) end
    return "最大生命已设为 " .. tostring(display_hp)
  end

  if command == "FULL_HEAL" then
    local player = player_and_position()
    local damage = EntityGetFirstComponentIncludingDisabled(player, "DamageModelComponent")
    if damage == nil then error("玩家没有 DamageModelComponent") end
    ComponentSetValue2(damage, "hp", ComponentGetValue2(damage, "max_hp"))
    return "生命已回满"
  end

  if command == "GIVE_ALL_IMMUNITIES" then
    local player, x, y = player_and_position()
    if EntityHasTag(player, "polymorphed_player") then error("变形状态下无法授予免疫天赋", 0) end
    local count = 0
    for _, perk_id in ipairs({ "PROTECTION_FIRE", "PROTECTION_ELECTRICITY", "PROTECTION_EXPLOSION", "PROTECTION_MELEE", "PROTECTION_RADIOACTIVITY" }) do
      local flag = get_perk_picked_flag_name(perk_id)
      if (tonumber(GlobalsGetValue(flag .. "_PICKUP_COUNT", "0")) or 0) == 0 then
        local perk = perk_spawn(x, y, perk_id, true)
        if perk == nil or perk == 0 then error("无法生成天赋 " .. perk_id) end
        perk_pickup(perk, player, EntityGetName(perk), false, false)
        count = count + 1
      end
    end
    return "已获得五种免疫天赋（火焰、电击、爆炸、近战、毒性）；新增 " .. tostring(count) .. " 个"
  end

  if command == "REFRESH_SPELL_USES" then
    local player = player_and_position()
    refresh_spell_uses(player)
    return "玩家法术栏与四根随身法杖里的限次法术已恢复"
  end

  if command == "SIMULATE_GOLD_WORLD" then
    player_and_position()
    ConvertEverythingToGold()
    return "黄金世界模拟已启用；未触发正式结局或永久进度"
  end

  if command == "SIMULATE_TOXIC_GOLD_WORLD" then
    player_and_position()
    ConvertEverythingToGold("gold_radioactive", "gold_static_radioactive")
    return "毒金世界模拟已启用；未触发正式结局或永久进度"
  end

  if command == "SIMULATE_PEACE" then
    player_and_position()
    enable_peace()
    return "和平模拟已启用；当前与后续生物将保持非敌对"
  end

  if command == "SIMULATE_PEACE_INVINCIBLE" then
    local player = player_and_position()
    enable_peace()
    invincible_mode = true
    ensure_invincibility(player)
    return "和平+无敌模拟已启用；未改变可见最大生命"
  end

  if command == "KILL_ALL_CREATURES" then
    local count = kill_loaded_entities("mortal", true)
    return "已向 " .. tostring(count) .. " 个已加载生物施加正常致死伤害"
  end

  if command == "KILL_ENEMIES" then
    local count = kill_loaded_entities("enemy", true)
    return "已向 " .. tostring(count) .. " 个已加载敌对生物施加正常致死伤害"
  end

  if command == "KILL_ALL_ENTITIES" then
    local count = kill_loaded_entities("mortal", false)
    return "已向 " .. tostring(count) .. " 个已加载可伤害实体施加正常致死伤害"
  end

  if command == "EXPLODE_ALL_CREATURES" then
    local count = explode_loaded_creatures("mortal")
    return "已向 " .. tostring(count) .. " 个已加载生物附加爆炸尸体效果并施加致死伤害"
  end

  if command == "EXPLODE_ENEMIES" then
    local count = explode_loaded_creatures("enemy")
    return "已向 " .. tostring(count) .. " 个已加载敌对生物附加爆炸尸体效果并施加致死伤害"
  end

  if command == "EXPLODE_GOLD" then
    local count = explode_loaded_gold()
    return "已按爆炸金块效果引爆并移除 " .. tostring(count) .. " 个已加载金块"
  end

  if command == "GENOME_MORE_LOVE_X4" then
    local component = world_state_component()
    local value = tonumber(ComponentGetValue2(component, "global_genome_relations_modifier")) or 0
    ComponentSetValue2(component, "global_genome_relations_modifier", value + 100)
    peace_mode = false -- Preserve the explicitly adjusted relation value.
    return "已叠加 4 层更多友爱效果（阵营关系 +100）"
  end

  if command == "GENOME_MORE_HATRED_X4" then
    local component = world_state_component()
    local value = tonumber(ComponentGetValue2(component, "global_genome_relations_modifier")) or 0
    ComponentSetValue2(component, "global_genome_relations_modifier", value - 100)
    peace_mode = false
    return "已叠加 4 层更多仇恨效果（阵营关系 -100）"
  end

  if command == "CHARM_ALL_CREATURES" then
    local count = charm_loaded_creatures()
    return "已永久魅惑 " .. tostring(count) .. " 个已加载生物"
  end

  if command == "FREEZE_ALL_CREATURES" then
    local count = freeze_loaded_creatures()
    return "已向 " .. tostring(count) .. " 个已加载生物施加永久冻结效果，冻结免疫者可能不生效"
  end

  if command == "PERMANENT_SHEEP" then
    local player = player_and_position()
    if EntityHasTag(player, "polymorphed_player") then
      error("角色已经处于变形状态", 0)
    end
    remove_invincibility_effects()
    if GameGetGameEffectCount(player, "PROTECTION_POLYMORPH") > 0 then
      error("玩家仍有其他来源的变形免疫，无法变羊")
    end
    local effect = LoadGameEffectEntityTo(player, "data/entities/misc/effect_polymorph.xml")
    if effect == nil or effect == 0 then error("无法加载变羊效果") end
    local component = EntityGetFirstComponentIncludingDisabled(effect, "GameEffectComponent")
    if component == nil then
      EntityKill(effect)
      error("变羊效果缺少 GameEffectComponent")
    end
    ComponentSetValue2(component, "frames", -1)
    ComponentSetValue2(component, "polymorph_target", "data/entities/animals/sheep.xml")
    pending_sheep = { frame = GameGetFrameNum(), command_id = lines[1] }
    return "已请求永久变羊，正在等待游戏确认"
  end

  if command == "SUICIDE" then
    local player = player_and_position()
    remove_invincibility_effects()
    local damage = EntityGetFirstComponentIncludingDisabled(player, "DamageModelComponent")
    if damage == nil then error("玩家没有 DamageModelComponent") end
    local max_hp = math.abs(ComponentGetValue2(damage, "max_hp") or 1)
    local x, y = EntityGetTransform(player)
    EntityInflictDamage(player, math.max(1000000, max_hp + 1000), "DAMAGE_CURSE", "$damage_curse",
      "NONE", 0, 0, player, x or 0, y or 0, 0)
    return "已触发玩家死亡"
  end

  if command == "RUN_EVENT" then
    player_and_position()
    local event_id = required_id(lines[3], "事件 ID")
    dofile("data/scripts/streaming_integration/event_list.lua")
    local found = false
    for _, event in ipairs(streaming_events or {}) do
      if event.id == event_id then
        found = true
        break
      end
    end
    if not found then error("未知事件 " .. event_id) end
    _streaming_run_event(event_id)
    return "已触发事件 " .. event_id
  end

  if command == "READ_WANDS" then
    local player = player_and_position()
    local count = write_wand_snapshot(lines[1], player)
    return "已读取当前魔杖栏，共 " .. tostring(count) .. " 根魔杖"
  end

  if command == "UPDATE_WAND" then
    local player = player_and_position()
    local wand = current_inventory_wand(player, math.floor(required_number(lines[3], "魔杖实体", 1, 2147483647)))
    local ability = EntityGetFirstComponentIncludingDisabled(wand, "AbilityComponent")
    if ability == nil then error("所选魔杖没有 AbilityComponent") end
    local shuffle = lines[4] == "1"
    local actions_per_round = math.floor(required_number(lines[5], "单次施放数", 1, 1000))
    local cast_delay = required_number(lines[6], "施放延迟", -1000, 1000)
    local recharge_time = required_number(lines[7], "充能时间", -1000, 1000)
    local mana_max = required_number(lines[8], "法力上限", 0, 1000000000)
    local mana_charge = required_number(lines[9], "法力恢复速度", 0, 1000000000)
    local capacity = math.floor(required_number(lines[10], "容量", 0, 1000))
    local spread = required_number(lines[11], "散射", -3600, 3600)
    local speed = required_number(lines[12], "速度加成", 0, 1000)
    ComponentObjectSetValue2(ability, "gun_config", "shuffle_deck_when_empty", shuffle)
    ComponentObjectSetValue2(ability, "gun_config", "actions_per_round", actions_per_round)
    ComponentObjectSetValue2(ability, "gun_config", "reload_time", seconds_to_frames(recharge_time))
    ComponentObjectSetValue2(ability, "gun_config", "deck_capacity", capacity)
    ComponentObjectSetValue2(ability, "gunaction_config", "fire_rate_wait", seconds_to_frames(cast_delay))
    ComponentObjectSetValue2(ability, "gunaction_config", "spread_degrees", spread)
    ComponentObjectSetValue2(ability, "gunaction_config", "speed_multiplier", speed)
    ComponentSetValue2(ability, "mana_max", mana_max)
    ComponentSetValue2(ability, "mana_charge_speed", mana_charge)
    local current_mana = tonumber(ComponentGetValue2(ability, "mana")) or 0
    if current_mana > mana_max then ComponentSetValue2(ability, "mana", mana_max) end
    write_wand_snapshot(lines[1], player)
    return "已修改所选魔杖的属性"
  end

  if command == "ADD_WAND_ALWAYS_CAST" then
    local player = player_and_position()
    local wand = current_inventory_wand(player, math.floor(required_number(lines[3], "魔杖实体", 1, 2147483647)))
    local spell_id = required_id(lines[4], "法术 ID")
    add_permanent_spell(wand, spell_id)
    write_wand_snapshot(lines[1], player)
    return "已为所选魔杖添加始终释放法术 " .. spell_id
  end

  if command == "READ_CHARACTER" then
    local player = player_and_position()
    write_character_snapshot(lines[1], player)
    return "已读取当前角色信息"
  end

  if command == "UPDATE_CHARACTER" then
    local player = player_and_position()
    local money = math.floor(required_number(lines[3], "金钱", 0, 9007199254740991))
    local display_hp = required_number(lines[4], "当前血量", 0.04, 1000000000)
    local display_max_hp = required_number(lines[5], "最大血量", 0.04, 1000000000)
    local lung_capacity = required_number(lines[6], "肺活量", 0, 1000000000)
    local fly_time_max = required_number(lines[7], "最大浮空能量", 0, 1000000000)
    local fly_time_left = required_number(lines[8], "当前浮空能量", 0, 1000000000)
    local fly_recharge = required_number(lines[9], "空中浮空恢复速度", 0, 1000000000)
    local fly_recharge_ground = required_number(lines[10], "地面浮空恢复速度", 0, 1000000000)
    local current_air = required_number(lines[11], "当前肺活量", 0, 1000000000)
    local run_velocity = required_number(lines[12], "奔跑速度", 0, 1000000000)
    local fly_velocity_x = required_number(lines[13], "水平飞行速度", 0, 1000000000)
    local jump_velocity_y = required_number(lines[14], "跳跃速度", -1000000000, 1000000000)
    local pixel_gravity = required_number(lines[15], "重力", -1000000000, 1000000000)
    local wallet = EntityGetFirstComponentIncludingDisabled(player, "WalletComponent")
    if wallet == nil then error("玩家没有 WalletComponent") end
    local damage = EntityGetFirstComponentIncludingDisabled(player, "DamageModelComponent")
    if damage == nil then error("玩家没有 DamageModelComponent") end
    local character = EntityGetFirstComponentIncludingDisabled(player, "CharacterDataComponent")
    if character == nil then error("玩家没有 CharacterDataComponent") end
    local platforming = EntityGetFirstComponentIncludingDisabled(player, "CharacterPlatformingComponent")
    if platforming == nil then error("玩家没有 CharacterPlatformingComponent") end

    if display_hp > display_max_hp then error("当前血量不能大于最大血量") end
    if fly_time_left > fly_time_max then error("当前浮空能量不能大于最大浮空能量") end
    if current_air > lung_capacity then error("当前肺活量不能大于肺活量上限") end

    ComponentSetValue2(wallet, "money", money)
    ComponentSetValue2(damage, "max_hp", display_max_hp / HP_DISPLAY_SCALE)
    ComponentSetValue2(damage, "hp", display_hp / HP_DISPLAY_SCALE)
    ComponentSetValue2(damage, "air_in_lungs_max", lung_capacity)
    ComponentSetValue2(damage, "air_in_lungs", current_air)
    ComponentSetValue2(character, "fly_time_max", fly_time_max)
    ComponentSetValue2(character, "mFlyingTimeLeft", fly_time_left)
    ComponentSetValue2(character, "fly_recharge_spd", fly_recharge)
    ComponentSetValue2(character, "fly_recharge_spd_ground", fly_recharge_ground)
    ComponentSetValue2(platforming, "run_velocity", run_velocity)
    ComponentSetValue2(platforming, "fly_velocity_x", fly_velocity_x)
    ComponentSetValue2(platforming, "jump_velocity_y", jump_velocity_y)
    ComponentSetValue2(platforming, "pixel_gravity", pixel_gravity)
    write_character_snapshot(lines[1], player)
    return "已修改当前角色的 13 项属性"
  end

  if command == "SPAWN_SPELL" or command == "GIVE_SPELL" then
    local player, x, y = player_and_position()
    local spell_id = required_id(lines[3], "法术 ID")
    local count = math.floor(required_number(lines[4] or "1", "数量", 1, 100))
    local added = 0
    for index = 1, count do
      local spell = CreateItemActionEntity(spell_id, x + 12 + ((index - 1) % 8) * 6, y - math.floor((index - 1) / 8) * 6)
      if spell == nil or spell == 0 then error("法术创建失败，已处理 " .. tostring(index - 1) .. " 个") end
      if command == "GIVE_SPELL" and spell ~= nil then
        GamePickUpInventoryItem(player, spell, false)
        if not spell_landed_in_spell_inventory(player, spell) then
          EntityKill(spell)
          if added > 0 then
            error("法术槽已满，仅加入 " .. tostring(added) .. " 个")
          end
          error("法术槽已满")
        end
        added = added + 1
      end
    end
    if command == "GIVE_SPELL" then
      return "已直接获得法术 " .. spell_id .. " x" .. tostring(count)
    end
    return "已生成法术 " .. spell_id .. " x" .. tostring(count)
  end

  if command == "SPAWN_ITEM" or command == "GIVE_ITEM" then
    local player, x, y = player_and_position()
    local item_id = required_id(lines[3], "物品 ID")
    local path = item_presets[item_id]
    if path == nil then error("未知物品 " .. item_id) end
    local count = math.floor(required_number(lines[4] or "1", "数量", 1, 100))
    for index = 1, count do
      local item = EntityLoad("data/entities/items/" .. path,
        x + 12 + ((index - 1) % 8) * 6, y - math.floor((index - 1) / 8) * 6)
      if item == nil or item == 0 then error("物品创建失败，已处理 " .. tostring(index - 1) .. " 个") end
      if command == "GIVE_ITEM" and item ~= nil then
        GamePickUpInventoryItem(player, item, false)
        if EntityGetRootEntity(item) ~= player then
          error("物品未进入背包，已放在脚下；此前已获得 " .. tostring(index - 1) .. " 个")
        end
      end
    end
    if command == "GIVE_ITEM" then
      return "已直接获得物品 " .. item_id .. " x" .. tostring(count)
    end
    return "已生成物品 " .. item_id .. " x" .. tostring(count)
  end

  if command == "SPAWN_WAND" then
    local _, x, y = player_and_position()
    local preset = required_id(lines[3], "法杖预设")
    local path = wand_presets[preset]
    if path == nil then error("未知法杖预设 " .. preset) end
    local count = math.floor(required_number(lines[4] or "1", "数量", 1, 20))
    for index = 1, count do
      local wand = EntityLoad(path, x + 12 + (index - 1) * 8, y)
      if wand == nil or wand == 0 then error("法杖创建失败，已生成 " .. tostring(index - 1) .. " 个") end
    end
    return "已生成法杖 " .. preset .. " x" .. tostring(count)
  end

  if command == "SPAWN_CUSTOM_WAND" then
    local _, x, y = player_and_position()
    local shuffle = lines[3] == "1"
    local capacity = math.floor(required_number(lines[4], "容量", 1, 100))
    local actions_per_round = math.floor(required_number(lines[5], "每次施放", 1, 100))
    local cast_delay = required_number(lines[6], "施放延迟", -10, 1000)
    local recharge = required_number(lines[7], "充能时间", -10, 1000)
    local mana_max = required_number(lines[8], "最大法力", 0, 1000000000)
    local mana_charge = required_number(lines[9], "法力回复", 0, 1000000000)
    local spread = required_number(lines[10], "散射", -360, 360)
    local wand = EntityLoad("data/entities/items/wand_level_01.xml", x + 12, y)
    if wand == nil or wand == 0 then error("无法创建法杖实体") end
    local ability = EntityGetFirstComponentIncludingDisabled(wand, "AbilityComponent")
    if ability == nil then error("新法杖没有 AbilityComponent") end
    ComponentObjectSetValue2(ability, "gun_config", "shuffle_deck_when_empty", shuffle)
    ComponentObjectSetValue2(ability, "gun_config", "deck_capacity", capacity)
    ComponentObjectSetValue2(ability, "gun_config", "actions_per_round", actions_per_round)
    ComponentObjectSetValue2(ability, "gun_config", "reload_time", math.floor(recharge * 60 + 0.5))
    ComponentObjectSetValue2(ability, "gunaction_config", "fire_rate_wait", math.floor(cast_delay * 60 + 0.5))
    ComponentObjectSetValue2(ability, "gunaction_config", "spread_degrees", spread)
    ComponentSetValue2(ability, "mana_max", mana_max)
    ComponentSetValue2(ability, "mana", mana_max)
    ComponentSetValue2(ability, "mana_charge_speed", mana_charge)
    return "已生成自定义法杖"
  end

  if command == "SPAWN_PERK" or command == "GIVE_PERK" then
    local player, x, y = player_and_position()
    if command == "GIVE_PERK" and EntityHasTag(player, "polymorphed_player") then
      error("变形状态下无法直接授予天赋", 0)
    end
    local perk_id = required_id(lines[3], "天赋 ID")
    local count = math.floor(required_number(lines[4] or "1", "数量", 1, 100))
    for index = 1, count do
      local perk = perk_spawn(x + 12 + ((index - 1) % 8) * 10, y - math.floor((index - 1) / 8) * 10, perk_id, true)
      if perk == nil or perk == 0 then error("无法生成天赋 " .. perk_id .. "，已处理 " .. tostring(index - 1) .. " 个") end
      if command == "GIVE_PERK" then
        perk_pickup(perk, player, EntityGetName(perk), false, false)
      end
    end
    if command == "GIVE_PERK" then
      return "已直接获得天赋 " .. perk_id .. " x" .. tostring(count)
    end
    return "已在脚下生成天赋 " .. perk_id .. " x" .. tostring(count)
  end

  if command == "APPLY_STAIN" then
    local player = player_and_position()
    local material = lines[3]
    local allowed_stains = { water = true, blood = true, oil = true, slime = true,
      radioactive_liquid = true, urine = true, alcohol = true }
    if material == nil or not allowed_stains[material] then error("沾染材质无效") end
    local percent = math.floor(required_number(lines[4] or "100", "沾染程度", 1, 100) + 0.5)
    local display_name = lines[5] or material
    EntityAddRandomStains(player, CellFactory_GetType(material), percent * 20)
    return "已施加沾染 " .. display_name .. "（约 " .. tostring(percent) .. "%）"
  end

  if command == "INGEST_STATUS" then
    local player = player_and_position()
    local material = lines[3]
    if material ~= "fungi" then error("摄取材质无效") end
    local ingestion = EntityGetFirstComponentIncludingDisabled(player, "IngestionComponent")
    if ingestion == nil then error("当前形态没有摄取组件（变形状态下不可用）") end
    local cells = math.floor(required_number(lines[4], "摄取量", 1, 100000) + 0.5)
    local seconds = math.floor(required_number(lines[5] or "30", "幻觉时长", 1, 3600) + 0.5)
    local display_name = lines[6] or material
    EntityIngestMaterial(player, CellFactory_GetType(material), cells)
    return "已施加摄取状态 " .. display_name .. "（约 " .. tostring(seconds) .. " 秒；超过 180 秒会触发真菌转化）"
  end

  if command == "GIVE_EFFECT" then
    local player = active_player()
    if player == nil then error("没有找到当前玩家；请先进入一局游戏") end
    local path = lines[3]
    if path == nil or string.match(path, "^data/[A-Za-z0-9_/%.]+%.xml$") == nil then
      error("效果路径无效")
    end
    if string.match(path, "effect_") == nil and string.match(path, "curse_wither_") == nil then
      error("效果路径无效")
    end
    local permanent = lines[4] == "1"
    local seconds = required_number(lines[5] or "30", "持续时间", 1, 3600)
    local icon_path = lines[6] or ""
    local icon_name = lines[7] or ""
    local icon_description = lines[8] or ""
    local effect_entity = LoadGameEffectEntityTo(player, path)
    if effect_entity == nil or effect_entity == 0 then error("无法加载状态效果 " .. path) end
    local frames = math.floor(seconds * 60)
    local game_effect = EntityGetFirstComponentIncludingDisabled(effect_entity, "GameEffectComponent")
    if game_effect ~= nil then
      if permanent then
        ComponentSetValue2(game_effect, "frames", -1)
      else
        ComponentSetValue2(game_effect, "frames", frames)
      end
    else
      local lifetime = EntityGetFirstComponentIncludingDisabled(effect_entity, "LifetimeComponent")
      if lifetime ~= nil then
        if permanent then
          ComponentSetValue2(lifetime, "lifetime", 2147483647)
        else
          ComponentSetValue2(lifetime, "lifetime", frames)
        end
      end
    end
    if icon_path ~= "" and EntityGetFirstComponentIncludingDisabled(effect_entity, "UIIconComponent") == nil then
      EntityAddComponent2(effect_entity, "UIIconComponent", {
        name = icon_name,
        description = icon_description,
        icon_sprite_file = icon_path,
        is_perk = false,
        display_above_head = false,
        display_in_hud = true,
      })
    end
    if permanent then
      return "已施加永久状态 " .. path
    end
    return "已施加状态 " .. path .. "（" .. tostring(math.floor(seconds)) .. " 秒）"
  end

  if command == "SET_SATIATION" then
    local player = active_player()
    if player == nil then error("没有找到当前玩家；请先进入一局游戏") end
    local size = math.floor(required_number(lines[3], "饱食度", -1, 100000))
    if size < 0 then
      local damage = EntityGetFirstComponentIncludingDisabled(player, "DamageModelComponent")
      if damage == nil then error("无法读取玩家生命组件") end
      local hp = ComponentGetValue2(damage, "hp")
      local x, y = EntityGetTransform(player)
      EntityInflictDamage(player, hp * 2, "DAMAGE_OVEREATING", "$damage_overeating",
        "NONE", 0, 0, player, x or 0, y or 0, 0)
      return "已模拟“又撑又胀”：造成当前生命值两倍的伤害"
    end
    local ingestion = EntityGetFirstComponentIncludingDisabled(player, "IngestionComponent")
    if ingestion == nil then error("当前形态没有饱食度组件（变形状态下不可用）") end
    ComponentSetValue2(ingestion, "ingestion_size", size)
    return "已设置饱食度为 " .. tostring(size) .. " / 7500"
  end

  if command == "CLEAR_EFFECTS" then
    local player = active_player()
    if player == nil then error("没有找到当前玩家；请先进入一局游戏") end
    local removed = 0
    for _, child in ipairs(EntityGetAllChildren(player) or {}) do
      if EntityGetIsAlive(child) and not EntityHasTag(child, "perk_entity") then
        local filename = EntityGetFilename(child)
        local game_effect = EntityGetFirstComponentIncludingDisabled(child, "GameEffectComponent")
        local is_effect_file = string.match(filename, "entities/misc/effect_") ~= nil
          or string.match(filename, "entities/misc/curse_wither_") ~= nil
          or string.match(filename, "effect_neutralized") ~= nil
        if game_effect ~= nil or is_effect_file then
          EntityKill(child)
          removed = removed + 1
        end
      end
    end
    local ingestion_ids = { "TRIP", "INGESTION_DRUNK", "FOOD_POISONING", "FARTS", "RAINBOW_FARTS",
      "INGESTION_ON_FIRE", "INGESTION_FREEZING", "INGESTION_MOVEMENT_SLOWER", "CURSE_CLOUD", "NIGHTVISION" }
    for _, status_id in ipairs(ingestion_ids) do
      EntityRemoveIngestionStatusEffect(player, status_id)
    end
    -- 沾染类状态由引擎按身上的污渍持续重建，必须用专用接口移除状态本身
    local stain_ids = { "WET", "OILED", "BLOODY", "SLIMY", "RADIOACTIVE", "JARATE", "ALCOHOLIC" }
    for _, status_id in ipairs(stain_ids) do
      EntityRemoveStainStatusEffect(player, status_id, 0)
    end
    EntityAddRandomStains(player, CellFactory_GetType("water"), 2000)
    -- 水洗会短暂带来潮湿；抑制它在水渍晾干前反复出现
    EntityRemoveStainStatusEffect(player, "WET", 180)
    -- 幻觉/醉酒/夜视的画面扭曲缓存在 DrugEffectComponent 里，杀效果实体不会自动归零，需显式重置
    pcall(function()
      local drug = EntityGetFirstComponentIncludingDisabled(player, "DrugEffectComponent")
      if drug == nil then return end
      for _, object_name in ipairs({ "drug_fx_target", "m_drug_fx_current" }) do
        ComponentObjectSetValue2(drug, object_name, "distortion_amount", 0)
        ComponentObjectSetValue2(drug, object_name, "color_amount", 0)
        ComponentObjectSetValue2(drug, object_name, "fractals_amount", 0)
        ComponentObjectSetValue2(drug, object_name, "fractals_size", 0)
      end
    end)
    return "已清除全部状态效果（移除效果实体 " .. tostring(removed) .. " 个，清除沾染/摄取状态并重置视觉扭曲）"
  end

  if command == "SPAWN_MATERIAL_CONTAINER" then
    local player, x, y = player_and_position()
    local container = lines[3]
    local mode = lines[4]
    local count = math.floor(required_number(lines[5], "材质数量", 1, 64))
    local path = "data/entities/items/pickup/potion_empty.xml"
    if container == "pouch" then
      path = "mods/codex_noita_trainer_bridge/entities/powder_stash_empty.xml"
    end
    local entity = EntityLoad(path, x + 12, y)
    if entity == nil or entity == 0 then error("无法创建容器实体") end
    local index = 6
    for _ = 1, count do
      local material = lines[index]
      if material == nil or string.match(material, "^[A-Za-z0-9_]+$") == nil then
        error("材质 ID 无效")
      end
      local amount = math.floor(required_number(lines[index + 1], "材质数量", 1, 10000000))
      AddMaterialInventoryMaterial(entity, material, amount)
      index = index + 2
    end
    if mode == "give" then
      GamePickUpInventoryItem(player, entity, false)
      if EntityGetRootEntity(entity) ~= player then
        return "容器未进入背包，已生成在脚下（" .. tostring(count) .. " 种材质）"
      end
    end
    return "已生成药水容器（" .. tostring(count) .. " 种材质）"
  end

  error("未知命令 " .. tostring(command))
end

local function format_overlay_number(value)
  local rounded = math.floor(value + 0.5)
  if math.abs(value - rounded) < 0.01 then
    return tostring(rounded)
  end
  return string.format("%.1f", value)
end

local function centered_overlay_text(gui, center_x, y, text)
  local width = GuiGetTextDimensions(gui, text, 1, 2, OVERLAY_FONT, true)
  GuiText(gui, center_x - width * 0.5, y, text, 1, OVERLAY_FONT, true)
end

local function enemy_label_world_y(entity, entity_y)
  local hitbox = EntityGetFirstComponentIncludingDisabled(entity, "HitboxComponent")
  if hitbox == nil then return entity_y - 12 end
  local minimum_y = ComponentGetValue2(hitbox, "aabb_min_y") or -5
  local _, offset_y = ComponentGetValue2(hitbox, "offset")
  return entity_y + (offset_y or 0) + minimum_y - 6
end

local function draw_overlay()
  if overlay_gui == nil then overlay_gui = GuiCreate() end
  GuiStartFrame(overlay_gui)
  if not show_enemy_hp and not show_player_coordinates then return end

  GuiZSet(overlay_gui, -1000)
  local screen_width, screen_height = GuiGetScreenDimensions(overlay_gui)
  local player = active_player()

  if show_player_coordinates and player ~= nil then
    local player_x, player_y = EntityGetTransform(player)
    local text = string.format("(%.1f, %.1f)", player_x or 0, player_y or 0)
    centered_overlay_text(overlay_gui, screen_width * 0.5, 5, text)
  end

  if not show_enemy_hp then return end
  local camera_left, camera_top, camera_width, camera_height = GameGetCameraBounds()
  if camera_width == nil or camera_height == nil or camera_width == 0 or camera_height == 0 then return end
  local camera_center_x = camera_left + camera_width * 0.5
  local camera_center_y = camera_top + camera_height * 0.5
  local radius = math.sqrt(camera_width * camera_width + camera_height * camera_height) * 0.5 + 64
  local enemies = EntityGetInRadiusWithTag(camera_center_x, camera_center_y, radius, "enemy") or {}

  for _, enemy in ipairs(enemies) do
    if enemy ~= player and EntityGetIsAlive(enemy) then
      local damage = EntityGetFirstComponentIncludingDisabled(enemy, "DamageModelComponent")
      if damage ~= nil then
        local hp = ComponentGetValue2(damage, "hp") or 0
        if hp > 0 then
          local world_x, world_y = EntityGetTransform(enemy)
          local label_world_y = enemy_label_world_y(enemy, world_y or 0)
          local screen_x = ((world_x or 0) - camera_left) * screen_width / camera_width
          local screen_y = (label_world_y - camera_top) * screen_height / camera_height
          if screen_x >= -30 and screen_x <= screen_width + 30 and screen_y >= -12 and screen_y <= screen_height + 12 then
            centered_overlay_text(overlay_gui, screen_x, screen_y, format_overlay_number(hp * HP_DISPLAY_SCALE))
          end
        end
      end
    end
  end
end

local function process_command()
  local lines = read_lines(COMMAND_PATH)
  if lines == nil or lines[1] == nil or lines[1] == "" then return end

  if not initialized then
    last_command_id = lines[1]
    initialized = true
    last_ok = true
    last_message = "桥接已就绪"
    write_status()
    return
  end

  if lines[1] == last_command_id then return end
  last_command_id = lines[1]
  local ok, result = pcall(execute_command, lines)
  last_ok = ok
  last_message = safe_text(result)
  GamePrint((ok and "Trainer: " or "Trainer error: ") .. game_message(last_message))
  write_status()
end

local function trainer_tick()
  tick_counter = tick_counter + 1
  process_command()
  local player = active_player()
  if pending_sheep ~= nil then
    local message = nil
    -- The command only starts on an untransformed player and explicitly targets sheep.
    -- Polymorphed entities need not retain the source XML filename; use the runtime tag.
    local success = player ~= nil and EntityHasTag(player, "polymorphed_player")
    if success then
      local effect = GameGetGameEffect(player, "POLYMORPH")
      if effect ~= nil and effect ~= 0 then ComponentSetValue2(effect, "frames", -1) end
      message = "已确认角色变羊，变形效果不会自动到期"
    elseif GameGetFrameNum() - pending_sheep.frame >= 120 then
      message = "变羊未生效，请检查变形免疫或其他模组的干预"
    end
    if message ~= nil then
      if last_command_id == pending_sheep.command_id then
        last_ok = success
        last_message = message
      end
      GamePrint((success and "Trainer: " or "Trainer error: ") .. game_message(message))
      pending_sheep = nil
    end
  end
  if player ~= nil then consume_applied_world_seed() end
  track_pickups(player)
  if collect_gold and player ~= nil then collect_loaded_gold(player) end
  update_healed_total(player)
  if invincible_mode and player ~= nil then ensure_invincibility(player) end
  if peace_mode and tick_counter % 60 == 0 then
    local ok = pcall(function()
      ComponentSetValue2(world_state_component(), "global_genome_relations_modifier", 100.0)
    end)
  end
  if tick_counter % 2 == 0 then write_status() end
end

function OnWorldPreUpdate()
  trainer_tick()
end

function OnPausePreUpdate()
  trainer_tick()
end

function OnWorldPostUpdate()
  if ghost_vision and tick_counter % 5 == 0 then reveal_loaded_ghosts() end
  draw_overlay()
end

function OnPlayerSpawned(player_entity)
  pending_sheep = nil
  initialized = false
  peace_mode = false
  invincible_mode = false
  observed_healing_player = player_entity or 0
  observed_healing_hp = nil
  healed_total_display = tonumber(GlobalsGetValue(HEALED_TOTAL_KEY, "0")) or 0
  invincible_player = 0
  protection_all_entity = 0
  protection_polymorph_entity = 0
  tracked_wand_ids = {}
  tracked_item_ids = {}
  wand_baseline_done = false
  item_baseline_done = false
  wand_pickup_total = 0
  item_pickup_total = 0
  last_message = "检测到玩家，桥接正在初始化"
  write_status()
end
