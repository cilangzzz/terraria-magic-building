# Data 数据目录

本目录存放知识库数据文件，按类别分子目录存储。

---

## 目录结构

```
Data/
├── kb/                        # 知识库数据库
│   ├── terraria_kb.db         # SQLite主数据库 (15张表)
│   └── terraria_kb_full.sql   # SQL备份文件
│
├── vectors/                   # 向量嵌入文件
│   ├── tile_embeddings.json   # 方块向量 (35个)
│   ├── wall_embeddings.json   # 墙壁向量 (23个)
│   ├── furniture_embeddings.json # 家具向量 (11个)
│   ├── style_embeddings.json  # 风格向量 (10个)
│   ├── biome_embeddings.json  # 生物群落向量 (13个)
│   └── schematic_embeddings.json # 建筑蓝图向量
│
└── README.md
```

---

## 数据库表 (kb/terraria_kb.db)

### 基础数据表

| 表名 | 记录数 | 内容 |
|------|--------|------|
| tiles | 35 | 建筑方块 |
| walls | 23 | 建筑墙壁 |
| paints | 19 | 油漆颜色 |
| slopes | 6 | 斜坡类型 |
| furniture | 11 | 家具类型 |
| light_sources | 5 | 光源 |
| doors | 3 | 门类型 |

### 建筑设计表

| 表名 | 记录数 | 内容 |
|------|--------|------|
| style_templates | 10 | 建筑风格模板 |
| npc_requirements | 18 | NPC偏好要求 |
| house_validation | 9 | 房屋验证规则 |
| biomes | 13 | 生物群落 |

### 建筑蓝图表

| 表名 | 内容 |
|------|------|
| building_schematics | 蓝图元数据 |
| schematic_tiles | 蓝图方块数据 |
| schematic_analysis | AI分析结果 |
| schematic_tags | 标签系统 |

---

## 向量文件 (vectors/)

| 文件 | 维度 | 用途 |
|------|------|------|
| tile_embeddings.json | 384 | 方块语义检索 |
| wall_embeddings.json | 384 | 墙壁语义检索 |
| furniture_embeddings.json | 384 | 家具语义检索 |
| style_embeddings.json | 384 | 风格匹配 |
| biome_embeddings.json | 384 | 生物群落匹配 |
| schematic_embeddings.json | 384 | 建筑蓝图检索 |

---

## 数据生成

```
Tools/database/init_full_db.py     → kb/terraria_kb.db
Tools/database/init_schematic_tables.py → 蓝图表

Tools/vector/generate_embeddings_smart.py → vectors/tile/wall/furniture/style
Tools/vector/generate_schematic_embeddings.py → vectors/schematic_embeddings.json
```

---

## C# 加载路径

更新后的路径配置：

```csharp
// 数据库路径
string dbPath = Path.Combine("Data", "kb", "terraria_kb.db");

// 向量路径
string tileVectorPath = Path.Combine("Data", "vectors", "tile_embeddings.json");
string schematicVectorPath = Path.Combine("Data", "vectors", "schematic_embeddings.json");
```