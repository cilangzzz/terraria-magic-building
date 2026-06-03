-- 建筑实体表 - 存储玩家建筑的完整数据
-- 用于Agent检索和参考建造

-- 1. 建筑实体主表
CREATE TABLE IF NOT EXISTS building_entities (
    id TEXT PRIMARY KEY,            -- 建筑ID (如 20260602215014)
    source TEXT,                    -- 来源文件名

    -- 尺寸
    width INTEGER NOT NULL,
    height INTEGER NOT NULL,

    -- 特征分类
    building_type TEXT,             -- residence, castle, tower, temple, shop
    style TEXT,                     -- asian_fantasy, medieval, modern, etc.
    progress TEXT,                  -- early_game, mid_game, mid_late_game
    complexity TEXT,                -- low, medium, high
    structure_type TEXT,            -- single_story, multi_story, underground

    -- 风格标签 (JSON数组)
    style_tags TEXT,                -- ["asian", "fantasy", "residence", "gold"]

    -- 颜色调性
    color_tone_primary TEXT,        -- warm, cold, neutral
    color_tone_colors TEXT,         -- ["gold", "brown", "green"]

    -- 生物群落匹配 (JSON数组)
    biome_match TEXT,               -- ["forest", "any"]

    -- NPC房屋验证
    npc_valid INTEGER,              -- 0/1 是否可作为NPC住所
    npc_has_light INTEGER,
    npc_has_flat_surface INTEGER,
    npc_has_comfort INTEGER,
    npc_has_entry INTEGER,
    npc_has_walls INTEGER,

    -- 功能统计 (JSON)
    functions_light TEXT,           -- {"count": 162, "items": ["Pine Lantern(129)"]}
    functions_entry TEXT,
    functions_storage TEXT,
    functions_furniture TEXT,
    functions_platform TEXT,

    -- 描述
    summary TEXT,                   -- 一句话描述

    -- 建造顺序 (JSON数组)
    building_sequence TEXT,         -- [{"step":1,"action":"frame","materials":["Stone"],...}]

    -- 关联的蓝图ID（如果有）
    schematic_id INTEGER,

    -- 时间戳
    created_at TEXT,
    updated_at TEXT,

    FOREIGN KEY (schematic_id) REFERENCES building_schematics(id)
);

-- 2. 建筑材料表 - 详细材料清单
CREATE TABLE IF NOT EXISTS building_materials (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    building_id TEXT NOT NULL,

    -- 材料类型
    material_type TEXT NOT NULL,    -- tile, wall

    -- 材料信息
    material_id INTEGER NOT NULL,   -- tile_id 或 wall_id
    material_name TEXT,
    material_count INTEGER,         -- 使用数量
    material_ratio REAL,            -- 占比百分比

    -- 是否主要材料
    is_primary INTEGER,             -- 0/1

    FOREIGN KEY (building_id) REFERENCES building_entities(id)
);

CREATE INDEX IF NOT EXISTS idx_building_materials_building ON building_materials(building_id);
CREATE INDEX IF NOT EXISTS idx_building_materials_type ON building_materials(material_type);

-- 3. 建筑向量表 - 用于语义检索
CREATE TABLE IF NOT EXISTS building_vectors (
    building_id TEXT PRIMARY KEY,

    -- 向量数据 (JSON数组)
    vector TEXT,                    -- [0.85, 0.72, 0.91, ...]

    -- 向量元数据
    vector_model TEXT,              -- text-embedding-ada-002
    vector_dimension INTEGER,

    -- 关联关键词（用于关键词检索）
    keywords TEXT,                  -- "asian fantasy residence gold lantern"

    FOREIGN KEY (building_id) REFERENCES building_entities(id)
);

-- 4. 插入示例建筑实体 (20260602215014 中式奇幻住宅)
INSERT INTO building_entities VALUES (
    '20260602215014',
    'QQ截图20260602215014.png',
    53, 32,
    'residence',
    'asian_fantasy',
    'mid_late_game',
    'high',
    'multi_story',
    '["asian", "fantasy", "residence", "gold", "lantern", "marble", "ebonwood", "crimson", "multi-story", "warm-tones"]',
    'warm',
    '["gold", "brown", "green", "purple"]',
    '["forest", "any"]',
    1, 1, 1, 1, 1, 1,
    '{"count": 162, "items": ["Pine Lantern(129)", "Blue Torch(12)", "Green Torch(6)", "Campfire(6)"]}',
    '{"count": 112, "items": ["Cactus Door(104)", "Silver Door(8)"]}',
    '{"count": 4, "items": ["Chest(4)"]}',
    '{"count": 76, "items": ["Marble Bookshelf(23)", "Dynasty Sink(12)", "Teacup(11)", "Tungsten Bathtub(4)"]}',
    '{"count": 8, "items": ["Copper Platform(8)"]}',
    '中式奇幻风格多层住宅，53x32格，使用金块装饰、大理石墙、松木灯笼等材料，包含地上居住区和地下功能区',
    '[{"step":1,"action":"frame","materials":["Stone","Stone Slab"],"note":"搭建主体框架"},{"step":2,"action":"walls","materials":["Marble Wall","Ebonwood Wall"],"note":"铺设背景墙"},{"step":3,"action":"floor","materials":["Dirt","Gold"],"note":"铺设地板和装饰"},{"step":4,"action":"doors","materials":["Cactus Door"],"note":"安装入口"},{"step":5,"action":"lights","materials":["Pine Lantern","Blue Torch"],"note":"布置光源"},{"step":6,"action":"furniture","materials":["Bookshelf","Sink","Teacup"],"note":"摆放家具"},{"step":7,"action":"decor","materials":["Gold","Gemspark"],"note":"添加装饰细节"}]',
    NULL,
    '2026-06-02T21:50:14',
    '2026-06-03T00:00:00'
);

-- 5. 插入材料清单
INSERT INTO building_materials VALUES
(1, '20260602215014', 'tile', 179, 'Gold', 179, 19.0, 1),
(2, '20260602215014', 'tile', 129, 'Pine Lantern', 129, 14.0, 1),
(3, '20260602215014', 'tile', 104, 'Cactus Door', 104, 11.0, 1),
(4, '20260602215014', 'tile', 58, 'Dirt', 58, 6.0, 0),
(5, '20260602215014', 'tile', 25, 'Slush', 25, 3.0, 0),
(6, '20260602215014', 'wall', 172, 'Marble Wall', 172, 23.0, 1),
(7, '20260602215014', 'wall', 154, 'Ebonwood Wall', 154, 20.0, 1),
(8, '20260602215014', 'wall', 145, 'Crimson Wall', 145, 19.0, 1),
(9, '20260602215014', 'wall', 128, 'Wood Wall', 128, 17.0, 0),
(10, '20260602215014', 'wall', 112, 'Mushroom Shelf Wall', 112, 15.0, 0);

-- 6. 插入向量数据
INSERT INTO building_vectors VALUES (
    '20260602215014',
    '[0.85, 0.72, 0.91, 0.68, 0.45, 0.88, 0.33, 0.76, 0.54, 0.82, 0.67, 0.49, 0.73, 0.58, 0.91, 0.44, 0.66, 0.79, 0.52, 0.87, 0.61, 0.38, 0.84, 0.55, 0.72, 0.93, 0.47, 0.69, 0.81, 0.56, 0.74, 0.42]',
    'text-embedding-ada-002',
    32,
    'asian fantasy residence gold lantern marble ebonwood crimson multi-story warm'
);