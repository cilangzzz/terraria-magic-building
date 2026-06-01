# SQLite 方块数据库设计方案

## 一、数据库架构

### 1.1 数据库文件位置
```
C:\Users\admin\Documents\My Games\Terraria\tModLoader\ModSources\trab\Data\terraria_kb.db
```

### 1.2 表结构设计

## 二、核心表Schema

### 2.1 方块表 (tiles)

```sql
CREATE TABLE tiles (
    id INTEGER PRIMARY KEY,           -- Tile ID (官方ID)
    name TEXT NOT NULL UNIQUE,        -- 英文名称
    display_name TEXT,                -- 显示名称
    category TEXT,                    -- 类别: basic, brick, wood, slab, luxury, special, platform, furniture, decoration
    sub_category TEXT,                -- 子类别
    styles TEXT,                      -- 风格匹配 (JSON数组): ["medieval", "castle"]
    biome_match TEXT,                 -- 生物群落匹配 (JSON数组): ["forest", "underground"]
    paint_compatible INTEGER DEFAULT 0, -- 是否支持油漆
    slope_compatible INTEGER DEFAULT 0, -- 是否支持斜坡
    hardness INTEGER DEFAULT 50,      -- 硬度
    light_emission INTEGER DEFAULT 0, -- 光照强度
    light_color TEXT,                 -- 光照颜色
    is_solid INTEGER DEFAULT 1,       -- 是否实心
    is_multi_tile INTEGER DEFAULT 0,  -- 是否多格方块
    width INTEGER DEFAULT 1,          -- 多格宽度
    height INTEGER DEFAULT 1,         -- 多格高度
    npc_function TEXT,                -- NPC功能 (JSON数组): ["flat_surface", "crafting"]
    placement_rule TEXT,              -- 放置规则描述
    craft_station TEXT,               -- 制作站要求
    wire_compatible INTEGER DEFAULT 0, -- 是否支持电路
    description TEXT,                 -- 详细描述
    source TEXT,                      -- 数据来源
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 索引
CREATE INDEX idx_tiles_category ON tiles(category);
CREATE INDEX idx_tiles_name ON tiles(name);
CREATE INDEX idx_tiles_style ON tiles(styles);
```

### 2.2 墙壁表 (walls)

```sql
CREATE TABLE walls (
    id INTEGER PRIMARY KEY,           -- Wall ID (官方ID)
    name TEXT NOT NULL UNIQUE,        -- 英文名称
    display_name TEXT,                -- 显示名称
    category TEXT,                    -- 类别: basic, brick, luxury, special
    styles TEXT,                      -- 风格匹配 (JSON数组)
    biome_match TEXT,                 -- 生物群落匹配 (JSON数组)
    paint_compatible INTEGER DEFAULT 0, -- 是否支持油漆
    is_natural INTEGER DEFAULT 0,     -- 是否天然墙(不能用于NPC房屋)
    description TEXT,
    source TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_walls_category ON walls(category);
CREATE INDEX idx_walls_name ON walls(name);
```

### 2.3 油漆表 (paints)

```sql
CREATE TABLE paints (
    id INTEGER PRIMARY KEY,           -- Paint ID (0-31)
    name TEXT NOT NULL UNIQUE,        -- 英文名称
    display_name TEXT,                -- 显示名称
    color_hex TEXT,                   -- 颜色十六进制值
    effect_type TEXT,                 -- 效果类型: color, depth, special
    description TEXT,
    source TEXT
);
```

### 2.4 斜坡表 (slopes)

```sql
CREATE TABLE slopes (
    id INTEGER PRIMARY KEY,           -- Slope ID (0-5)
    name TEXT NOT NULL UNIQUE,        -- 英文名称
    display_name TEXT,                -- 显示名称
    direction TEXT,                   -- 方向描述
    description TEXT
);
```

### 2.5 家具表 (furniture)

```sql
CREATE TABLE furniture (
    id INTEGER PRIMARY KEY,           -- 家具Tile ID
    name TEXT NOT NULL UNIQUE,        -- 英文名称
    display_name TEXT,                -- 显示名称
    category TEXT,                    -- 类别: crafting, storage, seating, sleeping, decoration, light, door
    styles TEXT,                      -- 风格匹配 (JSON数组)
    width INTEGER DEFAULT 1,          -- 宽度
    height INTEGER DEFAULT 1,         -- 高度
    npc_function TEXT,                -- NPC功能 (JSON数组)
    paint_compatible INTEGER DEFAULT 0,
    placement_rule TEXT,              -- 放置规则
    storage_slots INTEGER DEFAULT 0,  -- 存储格数(箱子类)
    light_radius INTEGER DEFAULT 0,   -- 光照半径(光源类)
    wire_compatible INTEGER DEFAULT 0,
    craft_station TEXT,
    description TEXT,
    source TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_furniture_category ON furniture(category);
CREATE INDEX idx_furniture_npc_func ON furniture(npc_function);
```

### 2.6 光源表 (light_sources)

```sql
CREATE TABLE light_sources (
    id INTEGER PRIMARY KEY,           -- Tile ID
    name TEXT NOT NULL UNIQUE,
    display_name TEXT,
    category TEXT,                    -- torch, candle, chandelier, lamp, lantern, other
    width INTEGER DEFAULT 1,
    height INTEGER DEFAULT 1,
    light_radius INTEGER DEFAULT 10,  -- 光照半径
    light_intensity REAL DEFAULT 1.0, -- 光照强度
    light_color TEXT,                 -- 光照颜色
    styles TEXT,
    npc_function TEXT,                -- 是否可作为NPC光源
    placement_type TEXT,              -- floor, ceiling, wall, hanging
    wire_compatible INTEGER DEFAULT 0,
    description TEXT,
    source TEXT
);
```

### 2.7 门表 (doors)

```sql
CREATE TABLE doors (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    display_name TEXT,
    category TEXT,                    -- standard, trapdoor, gate
    width INTEGER DEFAULT 1,
    height INTEGER DEFAULT 3,
    styles TEXT,
    paint_compatible INTEGER DEFAULT 0,
    npc_function TEXT,
    wire_compatible INTEGER DEFAULT 0,
    description TEXT,
    source TEXT
);
```

### 2.8 风格模板表 (style_templates)

```sql
CREATE TABLE style_templates (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,        -- 风格标识: medieval, fantasy, steampunk
    display_name TEXT,                -- 显示名称: 中世纪风格
    description TEXT,                 -- 详细描述
    primary_tiles TEXT,               -- 主方块列表 (JSON)
    primary_walls TEXT,               -- 主墙壁列表 (JSON)
    accent_tiles TEXT,                -- 强调方块 (JSON)
    roof_style TEXT,                  -- 屋顶样式: triangular, dome, flat, curved
    roof_tiles TEXT,                  -- 屋顶方块 (JSON)
    furniture_style TEXT,             -- 家具风格 (JSON)
    paint_scheme TEXT,                -- 油漆方案 (JSON)
    architectural_rules TEXT,         -- 建筑规则 (JSON数组)
    biome_recommendations TEXT,       -- 推荐生物群落 (JSON数组)
    difficulty TEXT,                  -- 难度: easy, medium, hard
    wire_required INTEGER DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

### 2.9 NPC要求表 (npc_requirements)

```sql
CREATE TABLE npc_requirements (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    npc_name TEXT NOT NULL UNIQUE,    -- NPC标识: merchant, nurse
    display_name TEXT,                -- 显示名称
    spawn_condition TEXT,             -- 生成条件
    preferred_biome TEXT,             -- 偏好生物群落
    disliked_biome TEXT,              -- 不喜欢生物群落
    preferred_neighbors TEXT,         -- 偏好邻居 (JSON数组)
    disliked_neighbors TEXT,          -- 不喜欢邻居 (JSON数组)
    special_furniture TEXT,           -- 特殊家具需求
    biome_requirement TEXT,           -- 生物群落硬性要求
    description TEXT,
    source TEXT
);
```

### 2.10 房屋验证规则表 (house_validation)

```sql
CREATE TABLE house_validation (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    rule_name TEXT NOT NULL UNIQUE,   -- 规则名称
    rule_type TEXT,                   -- 类型: size, furniture, wall, frame, biome
    requirement TEXT,                 -- 要求描述
    minimum_value INTEGER,            -- 最小值
    maximum_value INTEGER,            -- 最大值
    required_elements TEXT,           -- 必需元素 (JSON数组)
    description TEXT
);
```

### 2.11 生物群落表 (biomes)

```sql
CREATE TABLE biomes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    display_name TEXT,
    category TEXT,                    -- surface, underground, special
    depth_range TEXT,                 -- 深度范围
    characteristic_tiles TEXT,        -- 特征方块 (JSON数组)
    characteristic_walls TEXT,        -- 特征墙壁 (JSON数组)
    description TEXT,
    source TEXT
);
```

---

## 三、数据来源规划

### 3.1 主要数据来源

| 来源 | URL | 数据类型 |
|------|-----|---------|
| Terraria Wiki | https://terraria.wiki.gg/wiki/Category:Tiles | 方块ID和属性 |
| Terraria Wiki | https://terraria.wiki.gg/wiki/Category:Walls | 墙壁ID和属性 |
| Terraria Wiki | https://terraria.wiki.gg/wiki/Category:Furniture | 家具信息 |
| Terraria Wiki | https://terraria.wiki.gg/wiki/House | NPC房屋要求 |
| TileID.cs | tModLoader源码 | 官方ID枚举 |
| WallID.cs | tModLoader源码 | 墙壁ID枚举 |

### 3.2 爬取策略

1. **优先级排序**：
   - 常用方块优先（Stone, Wood, Brick等）
   - 家具和光源次之
   - 装饰方块最后

2. **数据验证**：
   - ID与官方Wiki对照
   - 属性值交叉验证

---

## 四、C# SQLite集成方案

### 4.1 NuGet包

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.0" />
    <PackageReference Include="SQLitePCLRaw.bundle_e_sqlite3" Version="2.1.6" />
</ItemGroup>
```

### 4.2 数据访问层架构

```
Core/
├── Database/
│   ├── SQLiteDatabase.cs      -- 数据库连接管理
│   ├── TileRepository.cs      -- 方块数据访问
│   ├── WallRepository.cs      -- 墙壁数据访问
│   ├── FurnitureRepository.cs -- 家具数据访问
│   └── KnowledgeBaseSQLite.cs -- SQLite知识库集成
```

---

## 五、查询示例

### 5.1 搜索方块

```sql
-- 搜索中世纪风格的方块
SELECT id, name, display_name, paint_compatible, slope_compatible
FROM tiles
WHERE styles LIKE '%medieval%'
ORDER BY category, name;

-- 搜索支持油漆的方块
SELECT id, name, display_name
FROM tiles
WHERE paint_compatible = 1
AND category IN ('brick', 'wood', 'slab');

-- 搜索家具类方块
SELECT id, name, width, height, npc_function
FROM tiles
WHERE category = 'furniture';
```

### 5.2 搜索家具

```sql
-- 搜索NPC房屋必需家具
SELECT id, name, display_name, npc_function
FROM furniture
WHERE npc_function LIKE '%light_source%'
   OR npc_function LIKE '%flat_surface%'
   OR npc_function LIKE '%comfort%'
   OR npc_function LIKE '%door%';

-- 搜索光源
SELECT id, name, light_radius, light_intensity
FROM light_sources
WHERE light_radius >= 10
ORDER BY light_radius DESC;
```

---

## 六、数据量预估

| 表 | 预估记录数 | 备注 |
|-----|-----------|------|
| tiles | 800+ | 包含所有方块 |
| walls | 300+ | 所有墙壁类型 |
| paints | 32 | 固定数量 |
| slopes | 6 | 固定数量 |
| furniture | 150+ | 家具类方块 |
| light_sources | 50+ | 各类光源 |
| doors | 20+ | 各类门 |
| style_templates | 15+ | 预设风格模板 |
| npc_requirements | 30+ | 所有NPC |

---

## 七、初始化脚本

创建数据库初始化SQL文件，用于首次创建数据库结构。

---

*文档版本: 1.0*
*创建日期: 2026-06-01*