# Terraria 数据工具 (Python)

从 Terraria Wiki 爬取数据并导入SQLite数据库的Python工具集。

## 工具列表

| 文件 | 功能 |
|------|------|
| tile_crawler.py | 使用MediaWiki API爬取Wiki数据 |
| import_json_to_db.py | 将现有JSON数据导入SQLite数据库 |
| show_db_stats.py | 查看数据库统计信息 |
| run.bat | Windows运行脚本 |

## 安装依赖

```bash
pip install -r requirements.txt
```

依赖：
- requests - HTTP请求
- beautifulsoup4 - HTML解析（备用）

## 使用方法

### 方式一：交互式菜单
双击 `run.bat` 选择操作

### 方式二：命令行
```bash
# 爬取Wiki数据（API方式）
python tile_crawler.py

# 导入现有JSON数据到数据库
python import_json_to_db.py

# 查看数据库统计
python show_db_stats.py
```

## 数据流程

```
Wiki API (terraria.wiki.gg)
    ↓ tile_crawler.py
JSON文件 (Data/crawled/)
    ↓ import_json_to_db.py
SQLite数据库 (Data/terraria_kb.db)
```

## 数据库结构

数据库包含以下表：

| 表名 | 说明 | 预估数据量 |
|------|------|-----------|
| tiles | 方块数据 | 800+ |
| walls | 墙壁数据 | 300+ |
| paints | 油漆数据 | 32 |
| slopes | 斜坡数据 | 6 |
| furniture | 家具数据 | 150+ |
| light_sources | 光源数据 | 50+ |
| doors | 门数据 | 20+ |
| style_templates | 风格模板 | 15+ |
| npc_requirements | NPC要求 | 30+ |
| house_validation | 房屋验证规则 | 10+ |
| biomes | 生物群落 | 15+ |

## API数据格式

Terraria Wiki使用MediaWiki API，主要接口：

```
# 获取页面内容
https://terraria.wiki.gg/w/api.php?action=query&titles=Tile_IDs&prop=revisions&rvprop=content&format=json

# 查询分类成员
https://terraria.wiki.gg/w/api.php?action=query&list=categorymembers&cmtitle=Category:Tiles&format=json
```

## 输出文件

爬取输出 (`Data/crawled/`)：
- tiles_insert.sql - 方块SQL
- walls_insert.sql - 墙壁SQL
- full_insert.sql - 合并SQL
- wiki_data.json - JSON格式数据

数据库文件：
- `Data/terraria_kb.db` - SQLite数据库

## 注意事项

1. API请求有间隔延迟，避免服务器压力
2. Wiki页面结构可能变化，需定期更新解析逻辑
3. 部分数据可能需要手动补充和完善