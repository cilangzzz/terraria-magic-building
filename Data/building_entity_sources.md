# Terraria建筑实体数据来源

## 一、建筑蓝图/Schematic文件来源

### 1. TEdit Schematic格式 (主要格式)
**文件格式**: `.schematic` (JSON格式)
**内容**: 包含tile、wall、liquid、wire等完整建筑数据

| 来源 | 网址 | 说明 |
|-----|------|-----|
| **TEdit官方GitHub** | github.com/TEdit/Terraria-Map-Editor | 编辑器本身，包含示例schematic |
| **Terraria论坛Schematic板块** | forums.terraria.org (搜索schematic) | 玩家分享的schematic文件 |
| **Reddit r/Terraria** | reddit.com/r/Terraria | 搜索"TEdit schematic"或"blueprint" |
| **Reddit r/TerrariaBuilds** | reddit.com/r/TerrariaBuilds | 专门分享建筑的板块 |

### 2. 世界文件来源 (.wld)
**文件格式**: `.wld` (Terraria世界文件)
**内容**: 完整世界，包含多个建筑

| 来源 | 说明 |
|-----|------|
| **Reddit帖子评论区** | 玩家常在showcase帖子分享Google Drive/Mediafire链接 |
| **Terraria官方论坛** | Community Creations板块 |
| **Steam Workshop** | 通过tModLoader分享 |
| **YouTube视频描述栏** | 建筑教程视频常附带下载链接 |

### 3. 游戏内导入工具

| 工具 | 来源 | 功能 |
|-----|------|-----|
| **TEdit Schematics Mod** | forums.terraria.org/threads/tedit-schematics-mod | 在游戏内粘贴schematic |
| **HERO's Mod** | herosmod.com | 蓝图复制粘贴功能 |
| **Cheat Sheet Mod** | tModLoader Mod Browser | 建筑工具 |

---

## 二、Schematic文件格式解析

### JSON结构示例
```json
{
  "version": 4,
  "name": "Medieval House",
  "width": 20,
  "height": 15,
  "tiles": [
    // 每个位置的方块数据
    // [x, y] = {tileId, wallId, paint, slope, wire, etc}
  ],
  "walls": [...],
  "liquids": [...],
  "tileEntities": [...],
  "wires": [...]
}
```

### 数据字段说明
| 字段 | 说明 |
|-----|------|
| `tileId` | 方块类型ID (参考tiles表) |
| `wallId` | 墙壁类型ID (参考walls表) |
| `paint` | 油漆ID |
| `slope` | 斜坡类型 (0-5) |
| `wire` | 电线状态 |
| `liquid` | 液体类型和量 |
| `tileEntities` | 特殊实体(如宝箱内容) |

---

## 三、可爬取的具体资源列表

### 1. Terraria论坛Schematic分享帖
- `forums.terraria.org/index.php?threads/tedit-schematics-a-new-way-to-build` (官方schematic mod帖)
- 搜索关键词: "schematic", "TEdit", "blueprint", "build download"

### 2. Reddit建筑分享帖
- 搜索: `site:reddit.com/r/Terraria schematic download`
- 搜索: `site:reddit.com/r/TerrariaBuilds world file`

### 3. GitHub资源
- `github.com/TEdit/Terraria-Map-Editor` - 编辑器源码+示例文件
- 搜索: `github.com search "terraria schematic"`

### 4. YouTube建筑频道
| 频道 | 特点 |
|-----|------|
| **Khao** | 详细教程，常附下载链接 |
| **HERO** | 快速建筑，有mod工具 |
| **Platypus** | 创意建筑展示 |

---

## 四、爬取/获取方案

### 方案A: 直接下载Schematic文件
1. 从论坛/Reddit获取下载链接
2. 解析JSON格式的schematic文件
3. 提取建筑数据导入知识库

### 方案B: 使用TEdit导出
1. 下载玩家分享的.wld世界文件
2. 用TEdit打开世界
3. 选择建筑区域导出为schematic
4. 批量处理多个建筑

### 方案C: 解析世界文件
1. 直接解析.wld文件格式
2. 提取建筑区域数据
3. 转换为可用格式

---

## 五、建筑分类建议

### 按类型收集
| 类别 | 示例建筑 |
|-----|---------|
| **NPC房屋** | 各风格NPC房间 |
| **城堡/堡垒** | Medieval Castle, Fortress |
| **住宅/民居** | House, Cottage, Villa |
| **神殿/寺庙** | Temple, Shrine |
| **功能建筑** | Arena, Farm, Storage |
| **装饰建筑** | Pixel Art, Statue |
| **生物群落建筑** | 沙漠金字塔、冰雪屋、蘑菇屋 |

### 按尺寸收集
| 尺寸 | 建筑类型 |
|-----|---------|
| 小型 (10x10以内) | 单NPC房间 |
| 中型 (20x20左右) | 普通房屋 |
| 大型 (50x50以上) | 城堡、基地 |
| 巨型 (100x100以上) | 完整城镇 |

---

## 六、具体下载链接示例

### 已知的Schematic/世界文件分享点
1. **论坛帖子**: forums.terraria.org american板块 - TEdit Schematics
2. **Reddit**: 每周Building Showcase帖子
3. **Google Drive合集**: 玩家自建的蓝图库
4. **Discord社区**: Terraria相关Discord服务器的资源区

---

## 七、下一步行动

1. **获取schematic文件** - 从上述来源下载建筑蓝图
2. **解析JSON数据** - 提取方块、墙壁、家具位置
3. **建立建筑实体数据库** - 存储解析后的建筑数据
4. **分类整理** - 按风格、尺寸、用途分类

是否开始尝试获取具体的schematic文件？