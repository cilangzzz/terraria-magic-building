# 数据库管理工具

本目录包含SQLite数据库初始化和管理脚本。

---

## 脚本列表

| 脚本 | 功能 | 使用方法 |
|------|------|----------|
| init_full_db.py | 初始化完整数据库结构 | `python init_full_db.py` |
| add_basic_tiles.py | 补充基础方块和墙壁数据 | `python add_basic_tiles.py` |
| import_json_to_db.py | JSON数据导入数据库 | `python import_json_to_db.py` |
| show_db_stats.py | 显示数据库统计信息 | `python show_db_stats.py` |

---

## 数据库结构

数据库文件: `Data/terraria_kb.db`

### 核心表

| 表名 | 数据量 | 说明 |
|------|--------|------|
| tiles | 35 | 建筑方块 |
| walls | 23 | 建筑墙壁 |
| paints | 19 | 油漆 |
| slopes | 6 | 斜坡类型 |
| furniture | 11 | 家具 |
| light_sources | 5 | 光源 |
| doors | 3 | 门类型 |
| style_templates | 10 | 建筑风格模板 |
| npc_requirements | 18 | NPC偏好 |
| house_validation | 9 | 房屋验证规则 |
| biomes | 13 | 生物群落 |

---

## 使用流程

### 1. 初始化数据库

```bash
cd Tools/database
python init_full_db.py
```

创建所有表结构并填充基础数据。

### 2. 补充方块数据

```bash
python add_basic_tiles.py
```

手动添加常用建筑方块数据（网络不可用时使用）。

### 3. 查看数据库状态

```bash
python show_db_stats.py
```

输出各表数据统计。

---

## 数据来源

- Wiki API爬取（crawler脚本）
- 手动录入建筑相关方块
- 预定义建筑风格模板
- 官方Wiki NPC要求数据