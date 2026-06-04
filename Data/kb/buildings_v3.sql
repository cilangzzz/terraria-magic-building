-- 构件级建筑数据库表结构 v3
-- 基于多层次架构设计: 原子构件 → 复合构件 → 建筑 → 建筑群
-- 更新时间: 2026-06-04

-- ============================================================
-- 0. 建筑复杂性层次枚举 (存储为TEXT)
-- ============================================================
-- atomic: 原子构件 (屋顶、墙壁、装饰)
-- composite: 复合构件 (房间、楼层)
-- building: 完整建筑 (住宅、塔楼、商店)
-- complex: 建筑群 (村庄、基地)

-- ============================================================
-- 1. 建筑索引表 (building_index) - 用于向量检索
-- ============================================================
CREATE TABLE IF NOT EXISTS building_index (
    id TEXT PRIMARY KEY,                -- 建筑ID (如: chinese_house_001)
    name TEXT,                          -- 建筑名称
    source_id TEXT,                     -- 来源原始数据ID (如: 20260602215014)

    -- 检索向量
    vector TEXT,                        -- 向量数据 (JSON数组)
    vector_model TEXT,                  -- 模型名称
    searchable_text TEXT,               -- 检索文本

    -- 快速筛选
    complexity_level TEXT,              -- 层次: atomic/composite/building/complex
    building_type TEXT,                 -- 类型: house/tower/shop/temple/village
    style TEXT,                         -- 风格: asian/medieval/fantasy/modern
    size_category TEXT,                 -- 尺寸类别: small/medium/large
    width_range TEXT,                   -- 宽度范围 (JSON: [min, max])
    height_range TEXT,                  -- 高度范围 (JSON: [min, max])

    -- 摘要
    summary TEXT,                       -- 一句话描述

    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- 2. 建筑实体表 (buildings) - 层次2: 完整建筑
-- ============================================================
CREATE TABLE IF NOT EXISTS buildings (
    id TEXT PRIMARY KEY,
    building_type TEXT,                 -- house, tower, shop, temple
    structure_type TEXT,                -- single_story, multi_story, tower

    -- 尺寸
    width INTEGER,
    height INTEGER,
    stories INTEGER,                    -- 楼层数

    -- 风格
    style_tags TEXT,                    -- JSON数组: ["asian", "fantasy"]

    -- 结构组成
    structure TEXT,                     -- JSON: {foundation, stories, roof, decorations}

    -- 构件引用
    components TEXT,                    -- JSON数组: [{ref, type}]
    build_sequence TEXT,                -- JSON数组: 建造顺序

    -- NPC验证
    npc_valid INTEGER,                  -- 是否可作为NPC住所
    npc_requirements TEXT,              -- JSON: {has_light, has_door, has_table, has_chair}

    -- 来源
    source_file TEXT,                   -- 原始TEditSch文件
    original_id TEXT,                   -- 原始建筑ID

    summary TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- 3. 原子构件表 (atomic_components) - 层次0
-- ============================================================
CREATE TABLE IF NOT EXISTS atomic_components (
    id TEXT PRIMARY KEY,                -- 构件ID (如: roof_pagoda_001)
    type TEXT NOT NULL,                 -- roof, wall, floor, decoration, foundation, opening
    subtype TEXT,                       -- pagoda, outer_wall, lantern, door, window

    -- 形状参数
    shape TEXT,                         -- pyramid_tiered, pagoda, flat, gabled, dome
    tier_count INTEGER,                 -- 层数(用于分层屋顶)
    base_width INTEGER,                 -- 基础宽度
    height_per_tier INTEGER,            -- 每层高度
    thickness INTEGER,                  -- 厚度(墙壁)
    overhang INTEGER,                   -- 悬挑

    -- 边界 (相对坐标)
    bounds_relative TEXT,               -- JSON: {base_width, base_y_offset, height_per_tier}

    -- 边界 (绝对坐标，参考)
    bounds_absolute TEXT,               -- JSON: {x1, y1, x2, y2}

    -- 材料配置
    materials TEXT,                     -- JSON: {primary, accent, frame}

    -- 生成规则
    generation_rule TEXT,               -- JSON: {pattern, params, formula}
    pattern TEXT,                       -- pyramid_step, filled_rectangle, linear_spacing
    spacing INTEGER,                    -- 间距(装饰)
    placement TEXT,                     -- ceiling_mounted, wall_mounted, floor

    -- 统计
    tile_count INTEGER,                 -- 方块数量
    wall_count INTEGER,                 -- 墙壁数量

    -- 来源建筑
    source_building TEXT,               -- 来源建筑ID

    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_atomic_type ON atomic_components(type);
CREATE INDEX IF NOT EXISTS idx_atomic_subtype ON atomic_components(subtype);
CREATE INDEX IF NOT EXISTS idx_atomic_source ON atomic_components(source_building);

-- ============================================================
-- 4. 复合构件表 (composite_components) - 层次1
-- ============================================================
CREATE TABLE IF NOT EXISTS composite_components (
    id TEXT PRIMARY KEY,                -- 构件ID (如: room_basic_001)
    type TEXT NOT NULL,                 -- room, story, roof_system

    -- 尺寸要求
    min_width INTEGER,
    min_height INTEGER,
    min_area INTEGER,

    -- 原子构件引用
    atomic_components TEXT,             -- JSON数组: [{ref, role}]

    -- 功能要求
    requirements TEXT,                  -- JSON: {has_door, has_light, min_area}

    -- 生成规则
    generation_rule TEXT,               -- JSON: {pattern, params}

    -- 来源
    source_building TEXT,

    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- 5. 建筑群表 (complexes) - 层次3
-- ============================================================
CREATE TABLE IF NOT EXISTS complexes (
    id TEXT PRIMARY KEY,                -- 建筑群ID (如: chinese_village_001)
    complex_type TEXT,                  -- village, base, campus

    -- 尺寸
    width INTEGER,
    height INTEGER,

    -- 包含的建筑
    buildings TEXT,                     -- JSON数组: [{ref, position, variant}]

    -- 共享元素
    shared_elements TEXT,               -- JSON数组: [{type, material, path}]

    -- 风格
    style_tags TEXT,

    summary TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- 6. 风格材料映射表 (style_materials)
-- ============================================================
CREATE TABLE IF NOT EXISTS style_materials (
    style TEXT PRIMARY KEY,             -- 风格名称: asian, medieval, fantasy

    -- 方块推荐
    tiles TEXT,                         -- JSON: {primary, accent}

    -- 墙壁推荐
    walls TEXT,                         -- JSON数组

    -- 装饰推荐
    decorations TEXT,                   -- JSON数组

    -- 门推荐
    doors TEXT,                         -- JSON数组

    -- 家具推荐
    furniture TEXT,                     -- JSON数组

    -- 描述
    description TEXT,

    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- 7. 向量索引表 (vectors) - 统一的向量存储
-- ============================================================
CREATE TABLE IF NOT EXISTS vectors (
    id TEXT PRIMARY KEY,                -- 实体ID
    entity_type TEXT NOT NULL,          -- building, atomic, composite, complex
    entity_level TEXT,                  -- 层次: 0/1/2/3

    vector TEXT,                        -- JSON数组
    vector_model TEXT,
    vector_dimension INTEGER,

    keywords TEXT,                      -- 关键词字符串
    searchable_text TEXT,               -- 检索文本

    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_vectors_type ON vectors(entity_type);
CREATE INDEX IF NOT EXISTS idx_vectors_level ON vectors(entity_level);

-- ============================================================
-- 8. 原始数据表 (raw_data) - 存储转换前的原始数据
-- ============================================================
CREATE TABLE IF NOT EXISTS raw_data (
    id TEXT PRIMARY KEY,
    tedit_json TEXT,                    -- TEditSch.json原始内容
    data_building_json TEXT,            -- 转换后的构件数据
    description_md TEXT,                -- 建筑描述

    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- 视图
-- ============================================================

-- 建筑概览视图
CREATE VIEW IF NOT EXISTS v_building_overview AS
SELECT
    bi.id,
    bi.name,
    bi.complexity_level,
    bi.building_type,
    bi.style,
    bi.size_category,
    bi.summary,
    b.width, b.height, b.stories,
    b.npc_valid,
    v.keywords
FROM building_index bi
LEFT JOIN buildings b ON bi.id = b.id OR bi.source_id = b.original_id
LEFT JOIN vectors v ON bi.id = v.id;

-- 构件概览视图
CREATE VIEW IF NOT EXISTS v_component_overview AS
SELECT
    id, type, subtype, shape,
    base_width, height_per_tier, thickness,
    pattern, spacing,
    source_building
FROM atomic_components;

-- ============================================================
-- 示例数据
-- ============================================================

-- 风格材料映射
INSERT INTO style_materials VALUES (
    'asian',
    '{"primary": [{"id": 179, "name": "Gold", "use": "roof, accent"}, {"id": 353, "name": "Dynasty Wood", "use": "floor, frame"}], "accent": [{"id": 215, "name": "Red Brick", "use": "detail"}]}',
    '[{"id": 172, "name": "Marble Wall", "use": "outer_wall"}, {"id": 154, "name": "Ebonwood Wall", "use": "inner_wall"}]',
    '[{"id": 312, "name": "Pine Lantern", "category": "light"}, {"id": 395, "name": "Chinese Lantern", "category": "light"}]',
    '[{"id": 104, "name": "Cactus Door", "alternative": "Dynasty Door"}]',
    '[{"id": 46, "name": "Table", "material": "Dynasty Wood"}, {"id": 47, "name": "Chair", "material": "Dynasty Wood"}]',
    '中式风格：金色装饰、大理石墙、灯笼',
    CURRENT_TIMESTAMP
);

INSERT INTO style_materials VALUES (
    'medieval',
    '{"primary": [{"id": 1, "name": "Stone", "use": "wall, foundation"}, {"id": 5, "name": "Stone Slab", "use": "floor"}], "accent": [{"id": 4, "name": "Wood", "use": "frame, roof"}]}',
    '[{"id": 1, "name": "Stone Wall"}, {"id": 6, "name": "Red Brick Wall"}]',
    '[{"id": 10, "name": "Torch", "category": "light"}, {"id": 33, "name": "Banner", "category": "decoration"}]',
    '[{"id": 14, "name": "Wooden Door"}]',
    '[{"id": 46, "name": "Table", "material": "Wood"}, {"id": 47, "name": "Chair", "material": "Wood"}]',
    '中世纪风格：石材、砖墙、火炬',
    CURRENT_TIMESTAMP
);

INSERT INTO style_materials VALUES (
    'fantasy',
    '{"primary": [{"id": 182, "name": "Pearlstone", "use": "main"}, {"id": 179, "name": "Gold", "use": "accent"}]}',
    '[{"id": 24, "name": "Glass Wall"}, {"id": 73, "name": "Obsidian Wall"}]',
    '[{"id": 1045, "name": "Crystal", "category": "light"}]',
    '[{"id": 14, "name": "Glass Door"}]',
    '[{"id": 46, "name": "Table", "material": "Pearlwood"}]',
    '奇幻风格：珍珠石、玻璃墙、水晶装饰',
    CURRENT_TIMESTAMP
);