# trab - AI建筑生成模组

泰拉瑞亚 tModLoader AI建筑生成模组，通过AI API自动生成精美建筑。

## 功能特性

- **AI智能生成**: 通过自然语言描述生成建筑设计
- **多Agent协作**: 规划Agent + 模块Agent + 合并器架构
- **知识库检索**: 方块、墙壁、家具、风格模板库
- **向量语义搜索**: 基于embedding的智能材料推荐
- **建筑蓝图系统**: TEdit蓝图解析与向量检索
- **NPC房屋验证**: 自动检测建筑是否满足NPC入住条件
- **多种API支持**: DeepSeek、Claude、OpenAI、DashScope

## 快速开始

1. 安装 tModLoader
2. 将本模组放入 `ModSources/trab` 目录
3. 在游戏中编译并启用模组
4. 按 **P** 键打开AI建筑UI，或使用 `/aibuild <描述>` 命令
5. 在模组配置中设置API密钥 (ESC -> 模组配置 -> trab Config)

---

## 目录结构

```
trab/
├── trab.cs                  # 模组主入口类
├── trab.csproj              # 项目配置文件
├── build.txt                # 模组版本信息
├── description.txt          # 模组描述
├── description_workshop.txt # Workshop描述
├── icon.png                 # 模组图标 (64x64)
├── icon_small.png           # 小图标 (32x32)
├── LICENSE                  # MIT许可证
│
├── Core/                    # 核心服务层
│   ├── README.md            # 核心层说明
│   │
│   ├── API/                 # API服务
│   │   ├── AIApiService.cs      # AI API调用服务
│   │   ├── ApiResponseTypes.cs  # API响应类型
│   │   └ ApiServiceBase.cs      # API服务基类
│   │
│   ├── Agents/              # Agent服务
│   │   ├── AIAgentService.cs    # Agent入口服务
│   │   ├── MultiAgent/          # 多Agent协作
│   │   │   ├── BuildingMerger.cs    # 建筑合并器
│   │   │   ├── BuildingMultiAgent.cs # 多Agent协调器
│   │   │   ├── BuildingPlan.cs       # 建筑规划
│   │   │   └ ModuleAgents.cs         # 模块Agent
│   │   └ SingleAgent/           # 单Agent模式
│   │       └ BuildingSingleAgent.cs  # 单Agent生成器
│   │
│   ├── Building/            # 建筑执行
│   │   ├── BuildingExecutor.cs      # 基础执行器
│   │   ├── BuildingRequirement.cs   # 建筑需求验证
│   │   └ EnhancedBuildingExecutor.cs # 增强执行器
│   │
│   └── KnowledgeBase/       # 知识库
│       ├── KnowledgeBaseManager.cs  # 知识库管理器
│       ├── TileKnowledgeBase.cs     # 方块知识库
│       ├── VectorKnowledgeBase.cs   # 向量知识库
│       ├── StyleTemplateBase.cs     # 风格模板库
│       ├── FurnitureRuleBase.cs     # 家具规则库
│       ├── ComponentTemplates.cs    # 构件模板库
│       └ BuildingEntityBase.cs      # 建筑实体基类
│
├── Data/                    # 数据存储层
│   ├── README.md            # 数据层说明
│   ├── BuildingDesign.cs    # 建筑设计数据结构
│   ├── TEditSchDesign.cs    # TEdit蓝图解析
│   │
│   ├── kb/                  # 知识库数据库
│   │   ├── terraria_kb.db       # SQLite数据库 (376KB)
│   │   ├── terraria_kb_full.sql # SQL备份
│   │   └ building_entities.sql  # 建筑实体表
│   │
│   └── vectors/             # 向量数据
│       ├── tile_embeddings.json      # 方块向量 (179KB)
│       ├── wall_embeddings.json      # 墙壁向量 (119KB)
│       ├── furniture_embeddings.json # 家具向量 (55KB)
│       ├── style_embeddings.json     # 风格向量 (54KB)
│       ├── biome_embeddings.json     # 生物群落向量 (67KB)
│       ├── schematic_embeddings.json # 蓝图向量 (5KB)
│       ├── building_entities.json    # 建筑实体数据
│       └ building_vectors.json       # 建筑向量索引
│
├── Commands/                # 聊天命令层
│   ├── README.md            # 命令说明
│   └── BuildCommands.cs     # /aibuild, /quickbuild 命令
│
├── Config/                  # 配置层
│   ├── README.md            # 配置说明
│   └── AIBuildingConfig.cs  # 模组配置类
│
├── Players/                 # 玩家扩展层
│   ├── README.md            # 玩家扩展说明
│   └── AIBuildingPlayer.cs  # ModPlayer实现
│
├── UI/                      # 用户界面层
│   ├── README.md            # UI说明
│   └── AIBuildingUI.cs      # AI建筑UI面板
│
├── Localization/            # 本地化（待实现）
│
├── Properties/              # 项目属性
│   └── launchSettings.json  # 启动配置
│
├── Tools/                   # Python数据处理工具
│   ├── README.md            # 工具总览
│   ├── requirements.txt     # Python依赖
│   ├── run.bat              # 执行脚本
│   ├── building_parser.py   # 建筑解析器
│   ├── building_system.py   # 建筑系统工具
│   │
│   ├── crawler/             # Wiki数据爬取
│   │   ├── README.md
│   │   ├── building_crawler.py  # 建筑Wiki爬虫
│   │   └ tile_crawler.py        # 方块Wiki爬虫
│   │
│   ├── database/            # 数据库管理
│   │   ├── README.md
│   │   ├── init_full_db.py          # 初始化数据库
│   │   ├── init_schematic_tables.py # 初始化蓝图表
│   │   ├── add_basic_tiles.py       # 导入基础方块
│   │   ├── import_json_to_db.py     # JSON导入
│   │   ├── manage_building_entities.py # 建筑实体管理
│   │   └ show_db_stats.py           # 数据库统计
│   │
│   ├── python/              # Python工具
│   │   └ schematic_parser.py  # 蓝图解析器
│   │
│   └── vector/              # 向量生成
│       ├── README.md
│       ├── generate_embeddings_smart.py  # 智能向量生成
│       ├── generate_embeddings_full.py   # 完整版向量生成
│       └ generate_schematic_embeddings.py # 蓝图向量生成
│
├── docs/                    # 文档目录
│   ├── README.md            # 文档索引
│   │
│   ├── tutorial/            # 开发教程 (3个文档)
│   │   ├── tModLoader_模组开发基础教程.md
│   │   ├── tModLoader模组编译文档.md
│   │   └ tModLoader日志排查文档.md
│   │
│   ├── terraria-systems/    # 泰拉瑞亚系统 (2个文档)
│   │   ├── 泰拉瑞亚Tile系统开发文档.md
│   │   └ WorldGen系统文档.md
│   │
│   ├── ai-integration/      # AI集成 (2个文档)
│   │   ├── AI集成技术方案.md
│   │   └ AI_Agent升级方案.md
│   │
│   ├── database/            # 数据库 (5个文档)
│   │   ├── SQLite数据库设计方案.md
│   │   ├── 数据检索文档.md
│   │   ├── 数据库说明.md
│   │   ├── 建筑蓝图数据格式.md
│   │   └ npc_house_data.json
│   │
│   └── project/             # 项目概览 (2个文档)
│       ├── 项目结构文档.md
│       └ 建筑生成模组分析文档.md
│
├── bin/                     # 编译输出（自动生成）
├── obj/                     # 编译缓存（自动生成）
│
└── walls_data.json          # 墙壁数据源 (85KB)
```

---

## 核心模块详解

### Core/API - API服务层

| 文件 | 功能 |
|------|------|
| AIApiService.cs | AI API调用服务，支持OpenAI/Claude/DeepSeek/DashScope |
| ApiResponseTypes.cs | API响应类型定义 |
| ApiServiceBase.cs | API服务基类，提供公共方法 |

### Core/Agents - Agent服务层

| 目录/文件 | 功能 |
|-----------|------|
| AIAgentService.cs | Agent入口服务，统一调度单/多Agent |
| MultiAgent/BuildingMultiAgent.cs | 多Agent协调器，规划+模块并行生成 |
| MultiAgent/BuildingMerger.cs | 合并模块结果，验证NPC房屋要求 |
| MultiAgent/BuildingPlan.cs | 建筑规划数据结构 |
| MultiAgent/ModuleAgents.cs | 5个模块Agent（屋顶/墙壁/楼层/窗户/家具） |
| SingleAgent/BuildingSingleAgent.cs | 单Agent模式，直接生成完整建筑 |

### Core/Building - 建筑执行层

| 文件 | 功能 |
|------|------|
| BuildingExecutor.cs | 基础执行器，解析JSON并放置方块 |
| EnhancedBuildingExecutor.cs | 增强执行器，支持油漆/斜坡/阴影 |
| BuildingRequirement.cs | 建筑需求验证，检查方块ID有效性 |

### Core/KnowledgeBase - 知识库层

| 文件 | 功能 |
|------|------|
| KnowledgeBaseManager.cs | 知识库管理器（单例） |
| TileKnowledgeBase.cs | 方块知识库，提供搜索和过滤 |
| VectorKnowledgeBase.cs | 向量知识库，384维语义检索 |
| StyleTemplateBase.cs | 风格模板库（9种风格） |
| FurnitureRuleBase.cs | 家具规则库，NPC房屋功能映射 |
| ComponentTemplates.cs | 构件模板库（屋顶/窗户/楼层） |
| BuildingEntityBase.cs | 建筑实体基类，蓝图数据访问 |

### Data/vectors - 向量数据

| 文件 | 大小 | 维度 | 说明 |
|------|------|------|------|
| tile_embeddings.json | 179KB | 384 | 方块语义向量 |
| wall_embeddings.json | 119KB | 384 | 墙壁语义向量 |
| furniture_embeddings.json | 55KB | 384 | 家具语义向量 |
| style_embeddings.json | 54KB | 384 | 风格语义向量 |
| biome_embeddings.json | 67KB | 384 | 生物群落向量 |
| schematic_embeddings.json | 5KB | 384 | 建筑蓝图向量 |

### Data/kb - 数据库

| 文件 | 大小 | 说明 |
|------|------|------|
| terraria_kb.db | 376KB | SQLite主数据库，15张表 |
| building_entities.sql | 6KB | 建筑实体表结构 |

---

## 使用方法

### 聊天命令

```
/aibuild 一座中世纪风格的木屋
/aibuild help            # 显示帮助
/aibuild list            # 显示材料列表
/aibuild config          # 显示当前配置
/aibuild stop            # 停止生成

/quickbuild house        # 快速生成木屋预设
/quickbuild list         # 显示预设列表
```

### UI界面

1. 按 **P** 打开UI面板
2. 选择风格: 中世纪/现代/日式/奇幻/地下/自定义
3. 选择尺寸: 小(10x8)/中(20x15)/大(35x25)
4. 点击 **生成** 或按 **G**
5. 点击 **放置** 或按 **B** 在鼠标位置放置

### Agent模式

启用Agent模式后，AI将:
1. 规划建筑区域和结构
2. 并行生成5个模块（屋顶、墙壁、楼层、窗户、家具）
3. 合并模块结果
4. 验证NPC房屋要求并自动补充缺失元素

---

## 架构说明

### Agent生成流程

```
用户描述 → AIAgentService
              ↓
    ┌─────────┴─────────┐
    ↓                   ↓
SingleAgent         MultiAgent
    ↓                   ↓
BuildingDesign    BuildingPlan → ModuleAgents(并行)
                        ↓
                  BuildingMerger
                        ↓
                  BuildingDesign
                        ↓
            EnhancedBuildingExecutor → 世界放置
```

### 知识库结构

```
KnowledgeBaseManager
├── Tiles (TileKnowledgeBase)     # 方块知识库
├── Walls (WallKnowledgeBase)     # 墙壁知识库
├── Styles (StyleTemplateBase)    # 风格模板库 (9种)
├── Furniture (FurnitureRuleBase) # 家具规则库
├── Vectors (VectorKnowledgeBase) # 向量检索库 (384维)
├── Components (ComponentTemplates) # 构件模板
│   ├── Roofs (RoofTemplateBase)   # 屋顶模板 (5种)
│   ├── Windows (WindowTemplateBase) # 窗户模板 (4种)
│   └── Floors (FloorStructureBase) # 楼层结构 (4种)
└── BuildingEntities (BuildingEntityBase) # 建筑蓝图实体
```

### Agent工具调用

Agent可调用的工具:
- `search_tiles` - 搜索方块（SQL过滤 + 向量排序）
- `get_style_template` - 获取风格模板
- `search_furniture` - 搜索家具
- `get_paint_scheme` - 获取油漆方案
- `get_roof_template` - 获取屋顶设计
- `get_window_template` - 获取窗户设计
- `get_floor_structure` - 获取楼层结构
- `search_building_entities` - 搜索建筑蓝图实体

---

## API配置

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| ApiKey | API密钥 | 空 |
| ServiceProvider | API服务商 | DeepSeek |
| ModelName | 模型名称 | deepseek-v4-flash |
| CustomEndpoint | 自定义端点 | 空 |
| BuildOffsetX | 生成X偏移 | 5 |
| BuildOffsetY | 生成Y偏移 | 0 |
| MaxBuildingSize | 最大尺寸 | 50 |

---

## 开发说明

### 编译要求

- tModLoader v2024.x+
- .NET 8.0
- Newtonsoft.Json (已包含)

### 项目统计

| 类型 | 数量 |
|------|------|
| C# 源文件 | 22 |
| Python 脚本 | 11 |
| Markdown 文档 | 18 |
| JSON 数据文件 | 9 |
| SQL 文件 | 2 |

---

## 许可证

MIT License - Copyright (c) 2026 Cilang