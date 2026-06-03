-- 建筑实体数据库表结构设计 (v2)
-- 基于 TEditSch.json 和 建筑整体描述.md 的新格式
-- 更新时间: 2026-06-04

-- ============================================================
-- 1. 建筑实体主表 (buildings)
-- ============================================================
CREATE TABLE IF NOT EXISTS buildings (
    id TEXT PRIMARY KEY,                -- 建筑ID (时间戳格式: 20260602215014)

    -- 来源信息
    source_file TEXT,                   -- TEdit文件名 (如: 1.TEditSch)
    screenshot_file TEXT,               -- 截图文件名 (如: QQ截图20260602215014.png)

    -- 文件元数据
    file_size INTEGER,                  -- 文件大小 (字节)
    version INTEGER,                    -- 游戏版本 (如: 319 = Terraria 1.4.4+)

    -- 尺寸信息
    width INTEGER NOT NULL,             -- 宽度 (tiles)
    height INTEGER NOT NULL,            -- 高度 (tiles)
    total_tiles INTEGER,                -- 总tile数 (width * height)
    active_tiles INTEGER,               -- 活跃tile数 (实际放置的方块)
    tiles_with_walls INTEGER,           -- 有墙壁的tile数

    -- 建筑描述 (来自建筑整体描述.md)
    building_type TEXT,                 -- 类型: 住宅, 神庙, 商店, 城堡, 战斗场地
    style TEXT,                         -- 风格: 中式, 日式, 欧式, 现代, 哥特, 奇幻
    feature_tags TEXT,                  -- 特征标签 (JSON数组)
    summary TEXT,                       -- 简介 (一句话描述)

    -- 材料分析
    primary_materials TEXT,             -- 主要材料 (JSON数组)
    style_indicators TEXT,              -- 风格指标 (JSON对象)

    -- 元数据
    generated_at TEXT,                  -- 生成日期
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- 2. 方块统计表 (building_tiles)
-- ============================================================
CREATE TABLE IF NOT EXISTS building_tiles (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    building_id TEXT NOT NULL,

    -- 方块信息
    tile_id INTEGER NOT NULL,           -- 方块ID (如: 19 = Ice)
    tile_name TEXT,                     -- 方块名称 (如: Ice, Pine Painting)
    tile_count INTEGER,                 -- 该方块数量
    tile_ratio REAL,                    -- 占比百分比

    -- 分类标记
    is_primary INTEGER DEFAULT 0,       -- 是否主要材料 (前3种)
    is_unknown INTEGER DEFAULT 0,       -- 是否未知类型 (Unknown标记)
    category TEXT,                      -- 方块类别 (brick, wood, furniture, light, decoration)

    FOREIGN KEY (building_id) REFERENCES buildings(id)
);

CREATE INDEX IF NOT EXISTS idx_building_tiles_building ON building_tiles(building_id);
CREATE INDEX IF NOT EXISTS idx_building_tiles_type ON building_tiles(tile_id);

-- ============================================================
-- 3. 墙壁统计表 (building_walls)
-- ============================================================
CREATE TABLE IF NOT EXISTS building_walls (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    building_id TEXT NOT NULL,

    -- 墙壁信息
    wall_id INTEGER NOT NULL,           -- 墙壁ID
    wall_name TEXT,                     -- 墙壁名称
    wall_count INTEGER,                 -- 该墙壁数量
    wall_ratio REAL,                    -- 占比百分比

    -- 分类标记
    is_primary INTEGER DEFAULT 0,
    is_unknown INTEGER DEFAULT 0,

    FOREIGN KEY (building_id) REFERENCES buildings(id)
);

CREATE INDEX IF NOT EXISTS idx_building_walls_building ON building_walls(building_id);

-- ============================================================
-- 4. 向量索引表 (building_vectors)
-- ============================================================
CREATE TABLE IF NOT EXISTS building_vectors (
    building_id TEXT PRIMARY KEY,

    -- 向量数据
    vector TEXT,                        -- 向量数据 (JSON数组: [0.85, 0.72, ...])
    vector_model TEXT,                  -- 生成模型 (如: text-embedding-ada-002)
    vector_dimension INTEGER,           -- 向量维度 (如: 1536)

    -- 检索关键词 (用于关键词匹配)
    keywords TEXT,                      -- 关键词字符串 (如: "中式 神庙 多层 塔楼")
    search_text TEXT,                   -- 检索文本 (用于生成向量)

    FOREIGN KEY (building_id) REFERENCES buildings(id)
);

-- ============================================================
-- 5. 建筑原始数据表 (building_raw_data)
-- ============================================================
CREATE TABLE IF NOT EXISTS building_raw_data (
    building_id TEXT PRIMARY KEY,

    -- 原始JSON数据
    tedit_json TEXT,                    -- TEditSch.json完整内容
    description_md TEXT,                -- 建筑整体描述.md内容

    -- 方块网格 (可选，大型建筑可能不存储)
    tile_grid TEXT,                     -- tile网格 (JSON二维数组)
    wall_grid TEXT,                     -- wall网格 (JSON二维数组)

    FOREIGN KEY (building_id) REFERENCES buildings(id)
);

-- ============================================================
-- 6. 建筑截图表 (building_images)
-- ============================================================
CREATE TABLE IF NOT EXISTS building_images (
    building_id TEXT PRIMARY KEY,

    -- 图片信息
    image_path TEXT,                    -- 截图路径
    image_width INTEGER,                -- 图片宽度 (像素)
    image_height INTEGER,               -- 图片高度 (像素)
    image_base64 TEXT,                  -- Base64编码 (可选，用于API)

    FOREIGN KEY (building_id) REFERENCES buildings(id)
);

-- ============================================================
-- 触发器：自动更新时间戳
-- ============================================================
CREATE TRIGGER IF NOT EXISTS update_building_timestamp
 AFTER UPDATE ON buildings
BEGIN
    UPDATE buildings SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
END;

-- ============================================================
-- 示例数据插入
-- ============================================================

-- 建筑 20260602215014 (中式奇幻住宅)
INSERT INTO buildings VALUES (
    '20260602215014',
    'data.TEditSch', 'QQ截图20260602215014.png',
    NULL, 319,
    53, 32, 1696, 940, 909,
    '住宅', '中式奇幻',
    '["多层结构", "精致装饰", "地下储藏室", "融合自然"]',
    '这是一座融合了中式建筑风格和奇幻元素的多层住宅，外观精美且功能齐全，包含地上豪华居所和地下储藏空间。',
    '["Ice", "Brick", "Bamboo"]',
    '{"ice_blocks": "大量冰块使用，可能用于装饰性屋顶或结构", "bamboo": "竹子元素，体现中式/东方风格", "brick_walls": "砖墙作为主体建筑结构"}',
    '2026-06-03',
    CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
);

-- 建筑 20260602215205 (中式神庙塔楼)
INSERT INTO buildings VALUES (
    '20260602215205',
    '1.TEditSch', 'QQ截图20260602215205.png',
    41487, 319,
    92, 179, 16388, 5644, 4812,
    '神庙', '中式',
    '["多层塔楼", "绿色屋顶", "金色尖顶", "精致细节", "对称设计"]',
    '这是一座高耸的多层中式塔楼，拥有绿色屋顶和金色尖顶，设计精美且对称，适合作为游戏中的标志性建筑或宗教场所。',
    '["Ice", "Pine Painting", "Pine Rug"]',
    '{"ice_blocks": "大量冰块作为主体结构", "pine_series": "松木系列家具装饰"}',
    '2026-06-03',
    CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
);

-- ============================================================
-- 查询视图
-- ============================================================

-- 建筑概览视图
CREATE VIEW IF NOT EXISTS v_building_overview AS
SELECT
    b.id,
    b.width, b.height,
    b.building_type, b.style,
    b.summary,
    b.feature_tags,
    bv.keywords
FROM buildings b
LEFT JOIN building_vectors bv ON b.id = bv.building_id;

-- 建筑材料视图
CREATE VIEW IF NOT EXISTS v_building_materials AS
SELECT
    b.id as building_id,
    b.building_type, b.style,
    bt.tile_id, bt.tile_name, bt.tile_count, bt.is_primary,
    bw.wall_id, bw.wall_name, bw.wall_count, bw.is_primary
FROM buildings b
LEFT JOIN building_tiles bt ON b.id = bt.building_id AND bt.is_primary = 1
LEFT JOIN building_walls bw ON b.id = bw.building_id AND bw.is_primary = 1;

-- ============================================================
-- 常用查询示例
-- ============================================================

-- 1. 按风格搜索建筑
-- SELECT * FROM buildings WHERE style LIKE '%中式%';

-- 2. 按类型搜索建筑
-- SELECT * FROM buildings WHERE building_type = '神庙';

-- 3. 搜索相似建筑 (关键词匹配)
-- SELECT b.*, bv.keywords FROM buildings b
-- JOIN building_vectors bv ON b.id = bv.building_id
-- WHERE bv.keywords LIKE '%塔楼%';

-- 4. 获取建筑详细材料
-- SELECT bt.tile_name, bt.tile_count FROM building_tiles bt
-- WHERE bt.building_id = '20260602215205' ORDER BY bt.tile_count DESC LIMIT 10;

-- 5. 获取建筑的完整数据
-- SELECT * FROM building_raw_data WHERE building_id = '20260602215014';