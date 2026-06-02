# Wiki数据爬取工具

本目录包含从Terraria Wiki爬取数据的脚本。

---

## 脚本列表

| 脚本 | 功能 | 数据源 |
|------|------|--------|
| tile_crawler.py | 爬取方块数据 | terraria.wiki.gg API |
| building_crawler.py | 爬取建筑相关数据 | terraria.wiki.gg API |

---

## API说明

使用MediaWiki API: `https://terraria.wiki.gg/api.php`

### 主要参数

```
action=query          # 查询操作
titles=TileName       # 页面标题
prop=revisions        # 获取页面内容
format=json           # JSON格式输出
```

---

## 使用方法

### 爬取方块数据

```bash
cd Tools/crawler
python tile_crawler.py
```

从Wiki爬取方块ID、名称、分类等数据，存入数据库。

### 注意事项

1. **网络依赖**: 需要能够访问 terraria.wiki.gg
2. **代理设置**: 如有代理需配置环境变量
3. **数据筛选**: 只保留建筑相关方块，排除矿石、植物等

---

## 数据字段

爬取的tiles表字段:

| 字段 | 类型 | 说明 |
|------|------|------|
| id | int | 游戏内Tile ID |
| name | str | 英文名称 |
| display_name | str | 中文显示名 |
| category | str | 分类(brick/wood/luxury等) |
| styles | json | 适用风格列表 |
| biome_match | json | 匹配生物群落 |
| description | str | 描述文本 |
| hardness | int | 硬度值 |
| light_emission | int | 光照值 |

---

## 备用方案

网络不可用时，使用 `database/add_basic_tiles.py` 手动补充数据。