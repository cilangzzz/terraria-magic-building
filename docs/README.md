# 项目文档索引

本文档按类型整理了 trab 项目的所有技术文档。

**注意**: 项目已升级为**构件级生成架构**，采用多层次设计（原子构件→复合构件→建筑→建筑群）。

---

## 目录结构

```
docs/
├── tutorial/              # 开发教程
├── terraria-systems/      # 泰拉瑞亚系统
├── ai-integration/        # AI 集成
├── database/              # 数据库与数据
├── project/               # 项目概览与分析
└── README.md              # 本索引文件
```

---

## 1. 开发教程 (`tutorial/`)

tModLoader 开发相关的基础教程和工具文档。

| 文档 | 描述 |
|------|------|
| [tModLoader_模组开发基础教程.md](tutorial/tModLoader_模组开发基础教程.md) | tModLoader 安装配置、项目结构、Mod类使用、自定义物品、UI界面开发 |
| [tModLoader模组编译文档.md](tutorial/tModLoader模组编译文档.md) | 游戏内编译与命令行编译方式详解，CI/CD 自动化编译 |
| [tModLoader日志排查文档.md](tutorial/tModLoader日志排查文档.md) | 日志排查方法、命令行编译参数、常见问题解决 |

---

## 2. 泰拉瑞亚系统 (`terraria-systems/`)

泰拉瑞亚核心系统的技术文档。

| 文档 | 描述 |
|------|------|
| [泰拉瑞亚Tile系统开发文档.md](terraria-systems/泰拉瑞亚Tile系统开发文档.md) | Tile 基本概念、ID分类、放置/移除、属性、多Tile结构、多人同步 |
| [WorldGen系统文档.md](terraria-systems/WorldGen系统文档.md) | WorldGen 类概述、主要方法、程序化生成建筑、结构保护机制、房屋检测算法 |

---

## 3. AI 集成 (`ai-integration/`)

AI 功能集成的技术方案和实现文档。

| 文档 | 描述 |
|------|------|
| [AI集成技术方案.md](ai-integration/AI集成技术方案.md) | HTTP请求调用AI API、游戏内聊天界面、结构化数据处理、流式输出、安全性 |
| [AI_Agent升级方案.md](ai-integration/AI_Agent升级方案.md) | 真Agent vs 伪Agent对比、工具调用改造、多轮循环实现、自主决策架构 |
| [AI建筑生成升级方案_模板检索.md](ai-integration/AI建筑生成升级方案_模板检索.md) | 建筑模板检索系统、向量相似度匹配、构件级架构设计 |

---

## 4. 数据库与数据 (`database/`)

数据存储和检索相关文档。

| 文档 | 描述 |
|------|------|
| [SQLite数据库设计方案.md](database/SQLite数据库设计方案.md) | 数据库架构、核心表Schema（tiles/walls/paints/furniture等）、索引设计 |
| [数据检索文档.md](database/数据检索文档.md) | 混合检索架构、SQL精确查询+向量语义匹配、Agent工具调用接口 |
| [数据库说明.md](database/数据库说明.md) | 数据库使用说明、表结构说明 |
| [建筑蓝图数据格式.md](database/建筑蓝图数据格式.md) | TEdit蓝图数据格式、BuildingRules JSON结构定义 |
| [构件级数据库设计.md](database/构件级数据库设计.md) | 构件级架构设计、building_index/buildings/atomic_components表设计 |
| [npc_house_data.json](database/npc_house_data.json) | NPC房屋需求数据 |

---

## 5. 项目概览与分析 (`project/`)

项目整体结构和相关分析文档。

| 文档 | 描述 |
|------|------|
| [项目结构文档.md](project/项目结构文档.md) | 项目目录结构、模块划分、核心文件说明 |
| [建筑生成模组分析文档.md](project/建筑生成模组分析文档.md) | 主流建筑模组分析（CheatSheet/HEROsMod/StructureHelper）、代码模式、实现思路 |

---

## 构件级架构说明

项目采用多层次构件架构:

```
层次4: Complex (建筑群)    → 村庄、基地
层次3: Building (完整建筑) → 住宅、塔楼、商店、神庙
层次2: Composite (复合构件) → 房间、楼层
层次1: Atomic (原子构件)   → 屋顶、墙壁、装饰、基础
```

**核心处理流程**:
```
用户描述 → TrueAgentCore (工具调用循环)
              ↓
         search_buildings → 向量检索建筑模板 (building_embeddings.json)
              ↓
         get_building_details → 获取构件详情 (buildings + atomic_components)
              ↓
         get_component_rules → 获取生成规则
              ↓
         get_style_materials → 获取材料推荐 (style_materials)
              ↓
         generate_design_rules → 生成BuildingRules
              ↓
         ProceduralBuilder → 展开为完整方块数据
              ↓
         BuildingExecutor → 世界放置
```

**数据文件**:
```
Data/
├── kb/
│   ├── buildings_v3.sql      # 构件级数据库Schema
│   └── terraria_kb.db        # SQLite数据库 (1.8MB)
└── vectors/
    └── building_embeddings.json  # 建筑向量 (28个, 384维)
```

---

## 快速导航

### 按开发阶段

```
入门 → [模组开发基础教程](tutorial/tModLoader_模组开发基础教程.md) → [项目结构文档](project/项目结构文档.md)
开发 → [Tile系统文档](terraria-systems/泰拉瑞亚Tile系统开发文档.md) → [WorldGen系统文档](terraria-systems/WorldGen系统文档.md)
AI   → [AI集成技术方案](ai-integration/AI集成技术方案.md) → [AI_Agent升级方案](ai-integration/AI_Agent升级方案.md)
数据 → [SQLite数据库设计](database/SQLite数据库设计方案.md) → [数据检索文档](database/数据检索文档.md)
工具 → [Tools README](../Tools/README.md) → [向量生成](../Tools/vector/README.md)
部署 → [模组编译文档](tutorial/tModLoader模组编译文档.md) → [日志排查文档](tutorial/tModLoader日志排查文档.md)
```

### 按技术领域

| 领域 | 文档 |
|------|------|
| 前端/UI | [模组开发基础教程 § UI界面](tutorial/tModLoader_模组开发基础教程.md) |
| 世界生成 | [WorldGen系统文档](terraria-systems/WorldGen系统文档.md) |
| AI/Agent | [AI集成技术方案](ai-integration/AI集成技术方案.md) · [AI_Agent升级方案](ai-integration/AI_Agent升级方案.md) |
| 数据库 | [SQLite数据库设计方案](database/SQLite数据库设计方案.md) · [数据检索文档](database/数据检索文档.md) |
| 数据工具 | [Tools](../Tools/README.md) · [向量生成](../Tools/vector/README.md) |