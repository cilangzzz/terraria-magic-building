# Tools 工具目录

本目录包含知识库数据处理、向量生成和建筑解析的Python工具脚本。

---

## 目录结构

```
Tools/
├── README.md              # 本说明文件
├── requirements.txt       # Python依赖 (requests, beautifulsoup4)
├── run.bat               # 执行脚本
├── building_parser.py     # 建筑解析器 (21KB)
├── building_system.py     # 建筑系统工具 (20KB)
│
├── crawler/              # Wiki数据爬取
│   ├── README.md
│   ├── tile_crawler.py      # 方块Wiki爬虫 (28KB)
│   └ building_crawler.py    # 建筑Wiki爬虫 (50KB)
│
├── database/             # 数据库管理
│   ├── README.md
│   ├── init_full_db.py          # 初始化数据库 (26KB)
│   ├── init_schematic_tables.py # 初始化蓝图表 (11KB)
│   ├── add_basic_tiles.py       # 导入基础方块 (12KB)
│   ├── import_json_to_db.py     # JSON导入 (21KB)
│   ├── manage_building_entities.py # 建筑实体管理 (20KB)
│   └ show_db_stats.py           # 数据库统计 (1.4KB)
│
├── python/               # Python工具
│   └ schematic_parser.py  # TEdit蓝图解析器 (20KB)
│
└── vector/               # 向量生成
    ├── README.md
    ├── generate_embeddings_smart.py  # 智能向量生成 (13KB)
    ├── generate_embeddings_full.py   # 完整版向量生成 (11KB)
    └ generate_schematic_embeddings.py # 蓝图向量生成 (8.6KB)
```

---

## 快速开始

### 1. 安装依赖

```bash
pip install -r requirements.txt
```

可选依赖（高质量向量）:
```bash
pip install sentence-transformers
```

### 2. 初始化数据库

```bash
cd database
python init_full_db.py
python init_schematic_tables.py  # 初始化建筑蓝图表
```

### 3. 生成向量

```bash
cd vector
python generate_embeddings_smart.py
python generate_schematic_embeddings.py  # 生成蓝图向量
```

---

## 脚本功能对照

| 功能 | 脚本路径 | 说明 |
|------|----------|------|
| 创建数据库 | `database/init_full_db.py` | 初始化表结构+基础数据 |
| 创建蓝图表 | `database/init_schematic_tables.py` | 创建建筑蓝图相关表 |
| 管理建筑实体 | `database/manage_building_entities.py` | 导入/导出建筑实体数据 |
| 查看统计 | `database/show_db_stats.py` | 显示各表数据量 |
| 爬取Wiki | `crawler/tile_crawler.py` | 从Wiki获取方块数据 |
| 解析蓝图 | `python/schematic_parser.py` | 解析TEdit蓝图文件 |
| 生成向量 | `vector/generate_embeddings_smart.py` | 创建语义向量 |
| 生成蓝图向量 | `vector/generate_schematic_embeddings.py` | 创建蓝图向量 |

---

## 数据流程

```
Wiki API ──→ crawler/tile_crawler.py ──→ Data/kb/terraria_kb.db
                                              ↓
             database/init_full_db.py ──→ 补充建筑数据
                                              ↓
             vector/generate_embeddings_smart.py ──→ Data/vectors/tile_embeddings.json
                                                     Data/vectors/style_embeddings.json
                                                     Data/vectors/biome_embeddings.json
TEdit蓝图 ──→ python/schematic_parser.py ──→ 解析数据
                    ↓
             database/manage_building_entities.py ──→ Data/kb/terraria_kb.db
                    ↓
             vector/generate_schematic_embeddings.py ──→ Data/vectors/schematic_embeddings.json
```

---

## 输出文件

所有数据输出到 `Data/` 目录:

### 数据库文件 (Data/kb/)

| 文件 | 类型 | 大小 | 用途 |
|------|------|------|------|
| terraria_kb.db | SQLite | 376KB | 知识库主数据库 |
| terraria_kb_full.sql | SQL | 29KB | 数据库备份 |
| building_entities.sql | SQL | 6KB | 建筑实体表结构 |

### 向量文件 (Data/vectors/)

| 文件 | 大小 | 维度 | 用途 |
|------|------|------|------|
| tile_embeddings.json | 179KB | 384 | 方块语义向量 |
| wall_embeddings.json | 119KB | 384 | 墙壁语义向量 |
| furniture_embeddings.json | 55KB | 384 | 家具语义向量 |
| style_embeddings.json | 54KB | 384 | 风格语义向量 |
| biome_embeddings.json | 67KB | 384 | 生物群落向量 |
| schematic_embeddings.json | 5KB | 384 | 建筑蓝图向量 |
| building_entities.json | 3KB | - | 建筑实体数据 |
| building_vectors.json | 0.4KB | - | 建筑向量索引 |

---

## 核心脚本说明

### building_parser.py

建筑解析器，解析建筑蓝图并提取结构信息。

**功能**:
- 解析建筑JSON格式
- 提取方块、墙壁、家具位置
- 生成建筑结构分析

### schematic_parser.py

TEdit蓝图解析器，解析 `.schem` 或 `.tedit` 文件。

**功能**:
- 解析TEdit蓝图格式
- 提取方块ID和位置
- 生成JSON格式的建筑数据

### manage_building_entities.py

建筑实体管理工具。

**功能**:
- 导入建筑实体到数据库
- 导出建筑实体为JSON
- 管理建筑实体标签

---

## 注意事项

1. **网络依赖**: crawler脚本需要访问Wiki，网络不稳定时使用database脚本手动补充
2. **向量版本**: 推荐使用 `generate_embeddings_smart.py`，相似度匹配效果最佳
3. **数据筛选**: 只保留建筑相关方块，非建筑方块已在筛选时排除
4. **蓝图解析**: TEdit蓝图需要先转换为JSON格式再导入

---

## 相关文档

- [数据库说明](../docs/database/数据库说明.md)
- [数据检索文档](../docs/database/数据检索文档.md)
- [建筑蓝图数据格式](../docs/database/建筑蓝图数据格式.md)