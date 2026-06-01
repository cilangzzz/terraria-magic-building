-- 泰拉瑞亚方块知识库数据库初始化脚本
-- 版本: 1.0
-- 创建日期: 2026-06-01

-- ========== 方块表 ==========
CREATE TABLE IF NOT EXISTS tiles (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    display_name TEXT,
    category TEXT,
    sub_category TEXT,
    styles TEXT,
    biome_match TEXT,
    paint_compatible INTEGER DEFAULT 0,
    slope_compatible INTEGER DEFAULT 0,
    hardness INTEGER DEFAULT 50,
    light_emission INTEGER DEFAULT 0,
    light_color TEXT,
    is_solid INTEGER DEFAULT 1,
    is_multi_tile INTEGER DEFAULT 0,
    width INTEGER DEFAULT 1,
    height INTEGER DEFAULT 1,
    npc_function TEXT,
    placement_rule TEXT,
    craft_station TEXT,
    wire_compatible INTEGER DEFAULT 0,
    description TEXT,
    source TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_tiles_category ON tiles(category);
CREATE INDEX IF NOT EXISTS idx_tiles_name ON tiles(name);

-- ========== 墙壁表 ==========
CREATE TABLE IF NOT EXISTS walls (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    display_name TEXT,
    category TEXT,
    styles TEXT,
    biome_match TEXT,
    paint_compatible INTEGER DEFAULT 0,
    is_natural INTEGER DEFAULT 0,
    description TEXT,
    source TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_walls_category ON walls(category);
CREATE INDEX IF NOT EXISTS idx_walls_name ON walls(name);

-- ========== 油漆表 ==========
CREATE TABLE IF NOT EXISTS paints (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    display_name TEXT,
    color_hex TEXT,
    effect_type TEXT,
    description TEXT,
    source TEXT
);

-- ========== 斜坡表 ==========
CREATE TABLE IF NOT EXISTS slopes (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    display_name TEXT,
    direction TEXT,
    description TEXT
);

-- ========== 家具表 ==========
CREATE TABLE IF NOT EXISTS furniture (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    display_name TEXT,
    category TEXT,
    styles TEXT,
    width INTEGER DEFAULT 1,
    height INTEGER DEFAULT 1,
    npc_function TEXT,
    paint_compatible INTEGER DEFAULT 0,
    placement_rule TEXT,
    storage_slots INTEGER DEFAULT 0,
    light_radius INTEGER DEFAULT 0,
    wire_compatible INTEGER DEFAULT 0,
    craft_station TEXT,
    description TEXT,
    source TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_furniture_category ON furniture(category);
CREATE INDEX IF NOT EXISTS idx_furniture_npc_func ON furniture(npc_function);

-- ========== 光源表 ==========
CREATE TABLE IF NOT EXISTS light_sources (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    display_name TEXT,
    category TEXT,
    width INTEGER DEFAULT 1,
    height INTEGER DEFAULT 1,
    light_radius INTEGER DEFAULT 10,
    light_intensity REAL DEFAULT 1.0,
    light_color TEXT,
    styles TEXT,
    npc_function TEXT,
    placement_type TEXT,
    wire_compatible INTEGER DEFAULT 0,
    description TEXT,
    source TEXT
);

-- ========== 门表 ==========
CREATE TABLE IF NOT EXISTS doors (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    display_name TEXT,
    category TEXT,
    width INTEGER DEFAULT 1,
    height INTEGER DEFAULT 3,
    styles TEXT,
    paint_compatible INTEGER DEFAULT 0,
    npc_function TEXT,
    wire_compatible INTEGER DEFAULT 0,
    description TEXT,
    source TEXT
);

-- ========== 风格模板表 ==========
CREATE TABLE IF NOT EXISTS style_templates (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    display_name TEXT,
    description TEXT,
    primary_tiles TEXT,
    primary_walls TEXT,
    accent_tiles TEXT,
    roof_style TEXT,
    roof_tiles TEXT,
    furniture_style TEXT,
    paint_scheme TEXT,
    architectural_rules TEXT,
    biome_recommendations TEXT,
    difficulty TEXT,
    wire_required INTEGER DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- ========== NPC要求表 ==========
CREATE TABLE IF NOT EXISTS npc_requirements (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    npc_name TEXT NOT NULL UNIQUE,
    display_name TEXT,
    spawn_condition TEXT,
    preferred_biome TEXT,
    disliked_biome TEXT,
    preferred_neighbors TEXT,
    disliked_neighbors TEXT,
    special_furniture TEXT,
    biome_requirement TEXT,
    description TEXT,
    source TEXT
);

-- ========== 房屋验证规则表 ==========
CREATE TABLE IF NOT EXISTS house_validation (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    rule_name TEXT NOT NULL UNIQUE,
    rule_type TEXT,
    requirement TEXT,
    minimum_value INTEGER,
    maximum_value INTEGER,
    required_elements TEXT,
    description TEXT
);

-- ========== 生物群落表 ==========
CREATE TABLE IF NOT EXISTS biomes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    display_name TEXT,
    category TEXT,
    depth_range TEXT,
    characteristic_tiles TEXT,
    characteristic_walls TEXT,
    description TEXT,
    source TEXT
);

-- ========== 初始化基础油漆数据 ==========
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES
(0, 'None', '无油漆', NULL, 'none', '无油漆效果'),
(1, 'Red', '红色', '#FF0000', 'color', '红色油漆'),
(2, 'Orange', '橙色', '#FF8000', 'color', '橙色油漆'),
(3, 'Yellow', '黄色', '#FFFF00', 'color', '黄色油漆'),
(4, 'Lime', '青柠', '#80FF00', 'color', '青柠色油漆'),
(5, 'Green', '绿色', '#00FF00', 'color', '绿色油漆'),
(6, 'Teal', '青色', '#00FF80', 'color', '青色油漆'),
(7, 'Cyan', '蓝绿', '#00FFFF', 'color', '蓝绿色油漆'),
(8, 'SkyBlue', '天蓝', '#0080FF', 'color', '天蓝色油漆'),
(9, 'Blue', '蓝色', '#0000FF', 'color', '蓝色油漆'),
(10, 'Purple', '紫色', '#8000FF', 'color', '紫色油漆'),
(11, 'Violet', '紫罗兰', '#FF00FF', 'color', '紫罗兰色油漆'),
(12, 'Pink', '粉色', '#FF0080', 'color', '粉色油漆'),
(13, 'Crimson', '深红', '#FF4040', 'color', '深红色油漆'),
(14, 'Azure', '蔚蓝', '#4040FF', 'color', '蔚蓝色油漆'),
(28, 'Shadow', '阴影', NULL, 'depth', '阴影油漆，增加建筑层次感'),
(29, 'Negative', '反转', NULL, 'special', '反转颜色效果'),
(30, 'White', '白色', '#FFFFFF', 'color', '白色油漆'),
(31, 'Black', '黑色', '#000000', 'color', '黑色油漆');

-- ========== 初始化斜坡数据 ==========
INSERT OR IGNORE INTO slopes (id, name, display_name, direction, description) VALUES
(0, 'Solid', '完整方块', NULL, '标准完整方块'),
(1, 'HalfBlock', '半砖', NULL, '只有下半部分，可作为台阶'),
(2, 'SlopeDownRight', '右上斜坡', '左上到右下', '从左上到右下的斜坡'),
(3, 'SlopeDownLeft', '左上斜坡', '右上到左下', '从右上到左下的斜坡'),
(4, 'SlopeUpRight', '右下斜坡', '左下到右上', '从左下到右上的斜坡'),
(5, 'SlopeUpLeft', '左下斜坡', '右下到左上', '从右下到左上的斜坡');

-- ========== 初始化房屋验证规则 ==========
INSERT OR IGNORE INTO house_validation (rule_name, rule_type, requirement, minimum_value, maximum_value, required_elements, description) VALUES
('minimum_tiles', 'size', '房间内部至少60格空地', 60, NULL, NULL, '不含墙壁和家具的空地计数'),
('light_source', 'furniture', '必需一个光源', 1, NULL, '["Torches","Candles","Chandeliers"]', '火把、蜡烛、吊灯等'),
('flat_surface', 'furniture', '必需一个平坦表面', 1, NULL, '["Tables","WorkBench","Dressers"]', '桌子、工作台、梳妆台'),
('comfort', 'furniture', '必需一个舒适物品', 1, NULL, '["Chairs","Beds"]', '椅子、床'),
('door', 'furniture', '必需一个入口', 1, NULL, '["ClosedDoor","Trapdoor"]', '门、活板门'),
('wall_required', 'wall', '必须有背景墙', NULL, NULL, NULL, '天然墙无效，需要玩家放置的墙'),
('frame_required', 'frame', '必须被实心方块包围', NULL, NULL, NULL, '四周有实心方块'),
('biome_check', 'biome', '不能处于邪恶生物群落', NULL, NULL, NULL, '腐化/猩红/神圣生物群落检查');

-- ========== 初始化基础生物群落 ==========
INSERT OR IGNORE INTO biomes (name, display_name, category, depth_range, characteristic_tiles, characteristic_walls, description) VALUES
('forest', '森林', 'surface', '地表', '["Grass","Wood","Stone"]', '["WoodWall","StoneWall"]', '基础森林生物群落'),
('desert', '沙漠', 'surface', '地表', '["Sand","Sandstone","HardenedSand"]', '["SandstoneWall"]', '沙漠生物群落'),
('snow', '雪地', 'surface', '地表', '["SnowBlock","IceBlock","BorealWood"]', '["SnowWall","IceWall"]', '雪地生物群落'),
('jungle', '丛林', 'surface', '地表', '["JungleGrass","RichMahogany","Mud"]', '["JungleWall"]', '丛林生物群落'),
('ocean', '海洋', 'surface', '地表+水下', '["Sand","Coral","PalmWood"]', '["GlassWall"]', '海洋生物群落'),
('underground', '地下', 'underground', '地表以下', '["Stone","Dirt","GemsparkBlocks"]', '["StoneWall","DirtWall"]', '地下生物群落'),
('cavern', '洞穴', 'underground', '岩层以下', '["Stone","Obsidian","Marble","Granite"]', '["StoneWall"]', '深层洞穴'),
('hallow', '神圣', 'special', '地表', '["Pearlstone","HallowGrass","Pearlwood"]', '["PearlstoneWall"]', '神圣生物群落'),
('corruption', '腐化', 'special', '地表', '["Ebonstone","CorruptGrass","Ebonsand"]', '["EbonstoneWall"]', '腐化生物群落'),
('crimson', '猩红', 'special', '地表', '["Crimstone","CrimsonGrass","Crimsand"]', '["CrimstoneWall"]', '猩红生物群落'),
('hell', '地狱', 'underground', '地狱层', '["Obsidian","Hellstone","Ash"]', '["ObsidianWall"]', '地狱生物群落'),
('sky', '空岛', 'special', '天空', '["SunplateBlock","Cloud","RainCloud"]', '["CloudWall"]', '天空岛屿');