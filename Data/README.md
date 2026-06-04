# Data 数据目录

本目录存放建筑设计数据结构和知识库数据文件，采用**构件级架构**。

---

## 目录结构

```
Data/
├── BuildingDesign.cs        # 建筑设计数据结构（旧格式兼容）
├── BuildingRules.cs         # 设计规则数据结构（AI输出格式）
├── TEditSchDesign.cs        # TEdit蓝图数据结构（生成器输出）
├── README.md                # 本说明文件
│
├── kb/                      # 知识库数据库
│   └ buildings_v3.sql       # 构件级数据库Schema
│
└── vectors/                 # 向量嵌入文件
    └── building_embeddings.json  # 建筑实体向量 (28个)
```

---

## 核心数据结构

### BuildingRules.cs

设计规则数据结构，**AI输出格式**。

AI输出的精简设计规则，程序化生成器展开为完整方块数据。

```csharp
public class BuildingRules
{
    public string name;              // 建筑名称
    public int width;                // 宽度
    public int height;               // 高度
    public string style;             // 风格
    public string template_id;       // 模板引用（可选）
    
    public StructureRules structure; // 结构规则
    public List<DecorationRule> decorations; // 装饰规则
    public MaterialPalette materials; // 材料调色板
    public TemplateModifications modifications; // 模板修改指令
}
```

#### StructureRules 结构规则

```csharp
public class StructureRules
{
    public FrameRule frame;           // 框架规则
    public WallRule walls;            // 墙壁规则
    public List<FloorRule> floors;    // 楼层规则列表
    public RoofRule roof;             // 屋顶规则
    public List<RoomRule> rooms;      // 房间规则列表
}
```

#### RoofRule 屋顶规则

```csharp
public class RoofRule
{
    public string shape;        // pagoda, gable, pyramid, flat
    public int tier_count;      // 层数（宝塔）
    public int overhang;        // 悬挑
    public string material;     // 材料引用
}
```

#### MaterialPalette 材料调色板

```csharp
public class MaterialPalette
{
    public MaterialRef primary;    // 主材料
    public MaterialRef secondary;  // 次材料
    public MaterialRef accent;     // 装饰材料
    public MaterialRef frame;      // 框架材料
}

public class MaterialRef
{
    public int tile_id;    // 方块ID
    public int wall_id;    // 墙壁ID
    public int paint;      // 油漆ID（可选）
}
```

### TEditSchDesign.cs

TEdit蓝图数据结构，**生成器输出格式**。

程序化生成器输出的完整方块数据，可直接在世界放置。

```csharp
public class TEditSchDesign
{
    public string name;
    public int width;
    public int height;
    
    public List<TileData> tiles;       // 方块列表
    public List<WallData> walls;       // 墙壁列表
    public List<WireData> wires;       // 电线列表
}
```

### BuildingDesign.cs

建筑设计数据结构，**旧格式兼容**。

用于兼容旧版生成器和预设建筑。

---

## 数据库设计

### buildings_v3.sql - 构件级架构

多层次架构: **原子构件 → 复合构件 → 建筑 → 建筑群**

#### 核心表

| 表名 | 记录数 | 功能 | 层次 |
|------|--------|------|------|
| building_index | 28 | 建筑索引，向量检索 | Building |
| buildings | 28 | 完整建筑实体 | Building |
| atomic_components | 125 | 原子构件(屋顶/墙壁/装饰等) | Atomic |
| composite_components | 0 | 复合构件(房间/楼层) | Composite |
| complexes | 0 | 建筑群(村庄/基地) | Complex |
| style_materials | 4 | 风格材料映射 | - |
| vectors | 28 | 统一向量存储 | - |
| raw_data | 28 | 原始TEdit数据 | - |

#### 基础数据表

| 表名 | 记录数 | 内容 |
|------|--------|------|
| tiles | 35 | 建筑方块 |
| walls | 23 | 建筑墙壁 |
| paints | 19 | 油漆颜色 |
| furniture | 11 | 家具类型 |
| style_templates | 10 | 建筑风格模板 |
| npc_requirements | 18 | NPC偏好要求 |
| biomes | 13 | 生物群落 |

#### building_index 表结构

```sql
CREATE TABLE building_index (
    id TEXT PRIMARY KEY,              -- 建筑ID
    name TEXT,                        -- 建筑名称
    source_id TEXT,                   -- 来源原始数据ID
    
    -- 检索向量
    vector TEXT,                      -- 向量数据 (JSON)
    searchable_text TEXT,             -- 检索文本
    
    -- 快速筛选
    complexity_level TEXT,            -- atomic/composite/building/complex
    building_type TEXT,               -- house/tower/shop/temple
    style TEXT,                       -- asian/medieval/fantasy
    size_category TEXT,               -- small/medium/large
    width_range TEXT,                 -- [min, max]
    height_range TEXT,                -- [min, max]
    
    -- 摘要
    summary TEXT                      -- 一句话描述
);
```

#### buildings 表结构

```sql
CREATE TABLE buildings (
    id TEXT PRIMARY KEY,
    building_type TEXT,               -- house/tower/shop/temple
    structure_type TEXT,              -- single_story/multi_story/tower
    
    -- 尺寸
    width INTEGER,
    height INTEGER,
    stories INTEGER,
    
    -- 风格
    style_tags TEXT,                  -- JSON数组
    
    -- 结构组成
    structure TEXT,                   -- JSON: {foundation, stories, roof}
    
    -- 构件引用
    components TEXT                   -- JSON数组: [{ref, type}]
);
```

---

## 向量文件

| 文件 | 维度 | 用途 |
|------|------|------|
| building_embeddings.json | 384 | 建筑实体语义检索 (28个) |

---

## 数据流程

```
AI Agent 工具调用
       ↓
search_buildings → building_index (向量检索)
       ↓
get_building_details → buildings + atomic_components
       ↓
get_style_materials → style_materials
       ↓
generate_design_rules → BuildingRules JSON
       ↓
ProceduralBuilder → TEditSchDesign
       ↓
BuildingExecutor → 世界放置
```

---

## 数据生成

```
Tools/database/manage_buildings_v3.py → kb/构件级表
Tools/vector/generate_building_embeddings.py → vectors/building_embeddings.json
```

---

## C# 加载示例

```csharp
// 初始化知识库
KnowledgeBaseManager.Instance.Initialize();

// 检索建筑
var criteria = new BuildingSearchCriteria {
    style = "asian",
    building_type = "house",
    min_width = 15,
    max_width = 25
};
var buildings = kb.Components.SearchBuildings(criteria);

// 获取风格材料
var materials = kb.Components.GetStyleMaterials("asian");
// 返回: { primary: { tile_id: 179, wall_id: 172 }, ... }
```

---

## 相关文档

- [SQLite数据库设计方案](../docs/database/SQLite数据库设计方案.md)
- [数据检索文档](../docs/database/数据检索文档.md)
- [建筑蓝图数据格式](../docs/database/建筑蓝图数据格式.md)