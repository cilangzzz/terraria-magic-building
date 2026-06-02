# Tools 工具目录

本目录包含知识库数据处理和向量生成的Python工具脚本。

---

## 目录结构

```
Tools/
├── database/          # 数据库管理脚本
│   ├── init_full_db.py      # 初始化数据库
│   ├── add_basic_tiles.py   # 补充方块数据
│   ├── import_json_to_db.py # JSON导入
│   ├── show_db_stats.py     # 数据库统计
│   └── README.md
│
├── crawler/           # Wiki数据爬取
│   ├── tile_crawler.py      # 爬取方块
│   ├── building_crawler.py  # 爬取建筑数据
│   └── README.md
│
├── vector/            # 向量生成
│   ├── generate_embeddings_smart.py  # 智能向量 (推荐)
│   ├── generate_embeddings_pro.py    # TF-IDF向量
│   ├── generate_embeddings.py        # 简化向量
│   └── README.md
│
├── requirements.txt   # Python依赖
└── run.bat           # 执行脚本
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
```

### 3. 生成向量

```bash
cd vector
python generate_embeddings_smart.py
```

---

## 脚本功能对照

| 功能 | 脚本路径 | 说明 |
|------|----------|------|
| 创建数据库 | `database/init_full_db.py` | 初始化表结构+基础数据 |
| 查看统计 | `database/show_db_stats.py` | 显示各表数据量 |
| 爬取Wiki | `crawler/tile_crawler.py` | 从Wiki获取方块数据 |
| 生成向量 | `vector/generate_embeddings_smart.py` | 创建语义向量 |

---

## 数据流程

```
Wiki API ──→ crawler/tile_crawler.py ──→ Data/terraria_kb.db
                                           ↓
            database/init_full_db.py ──→ 补充建筑数据
                                           ↓
            vector/generate_embeddings_smart.py ──→ Data/tile_embeddings.json
                                                    Data/style_embeddings.json
                                                    Data/biome_embeddings.json
```

---

## 输出文件

所有数据输出到 `Data/` 目录:

| 文件 | 类型 | 用途 |
|------|------|------|
| terraria_kb.db | SQLite | 知识库主数据库 |
| terraria_kb_full.sql | SQL | 数据库备份 |
| tile_embeddings.json | JSON | 方块向量 |
| style_embeddings.json | JSON | 风格向量 |
| biome_embeddings.json | JSON | 生物群落向量 |

---

## 注意事项

1. **网络依赖**: crawler脚本需要访问Wiki，网络不稳定时使用database脚本手动补充
2. **向量版本**: 推荐使用 `generate_embeddings_smart.py`，相似度匹配效果最佳
3. **数据筛选**: 只保留建筑相关方块，非建筑方块已在筛选时排除

---

## 相关文档

- [数据库说明](../docs/数据库说明.md)
- [数据检索文档](../docs/database/数据检索文档.md)