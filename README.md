# trab - AI建筑生成模组

泰拉瑞亚 tModLoader AI建筑生成模组，通过AI API自动生成精美建筑。

## 功能特性

- **AI智能生成**: 通过自然语言描述生成建筑设计
- **多Agent协作**: 规划Agent + 模块Agent + 合并器架构
- **知识库检索**: 方块、墙壁、家具、风格模板库
- **向量语义搜索**: 基于embedding的智能材料推荐
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
├── icon.png                 # 模组图标
│
├── Core/                    # 核心服务层
│   ├── AIAgentService.cs    # AI Agent建筑生成服务（主生成器）
│   ├── AIApiService.cs      # AI API调用服务（HTTP请求封装）
│   ├── BuildingExecutor.cs  # 基础建筑执行器（方块放置）
│   ├── EnhancedBuildingExecutor.cs  # 增强版执行器（油漆/斜坡/阴影）
│   ├── BuildingMerger.cs    # 建筑模块合并器（多Agent结果合并）
│   ├── KnowledgeBase.cs     # 知识库管理器（方块/风格/家具/模板）
│   ├── ModuleAgents.cs      # 模块生成Agent（屋顶/墙壁/窗户/家具）
│   └── VectorKnowledgeBase.cs  # 向量知识库（语义检索）
│
├── Data/                    # 数据结构层
│   ├── BuildingDesign.cs    # 建筑设计数据结构（JSON解析目标）
│   ├── BuildingPlan.cs      # 建筑规划数据（区域划分）
│   ├── tile_embeddings.json # 方块向量数据
│   ├── wall_embeddings.json # 墙壁向量数据
│   ├── style_embeddings.json # 风格向量数据
│   └── biome_embeddings.json # 生物群落向量数据
│
├── Commands/                # 聊天命令层
│   └ BuildCommands.cs       # AI建筑生成命令（/aibuild, /quickbuild）
│
├── Config/                  # 配置层
│   └ AIBuildingConfig.cs    # 模组配置（API密钥/服务选择/生成参数）
│
├── Players/                 # 玩家扩展层
│   └ AIBuildingPlayer.cs    # 玩家ModPlayer（状态管理/快捷键）
│
├── UI/                      # 用户界面层
│   └ AIBuildingUI.cs        # AI建筑UI面板（风格/尺寸/操作/日志）
│
├── Localization/            # 本地化（待实现）
│
├── Properties/              # 项目属性
│   └ launchSettings.json    # 启动配置
│
├── Tools/                   # Python数据处理工具
│   ├── crawler/             # Wiki数据爬取
│   │   ├── building_crawler.py  # 建筑Wiki爬虫
│   │   └ tile_crawler.py    # 方块Wiki爬虫
│   │   └ README.md          # 爬虫说明
│   │
│   ├── database/            # 数据库管理
│   │   ├── init_full_db.py  # 数据库初始化
│   │   ├── add_basic_tiles.py  # 基础方块导入
│   │   ├── import_json_to_db.py  # JSON导入脚本
│   │   ├── show_db_stats.py  # 数据库统计
│   │   └ README.md          # 数据库说明
│   │
│   ├── vector/              # 向量生成
│   │   ├── generate_embeddings.py  # 基础向量生成
│   │   ├── generate_embeddings_pro.py  # 专业版向量生成
│   │   ├── generate_embeddings_smart.py  # 智能版向量生成（推荐）
│   │   └ README.md          # 向量生成说明
│   │
│   └ README.md              # 工具总览
│
├── Scripts/                 # 辅助脚本
│   └ generate_embeddings.py  # 向量生成入口
│
├── docs/                    # 文档目录
│   ├── README.md            # 文档索引
│   │
│   ├── tutorial/            # 开发教程
│   │   ├── tModLoader_模组开发基础教程.md  # tModLoader入门
│   │   ├── tModLoader模组编译文档.md      # 编译方式详解
│   │   ├── tModLoader日志排查文档.md      # 日志排查方法
│   │
│   ├── terraria-systems/    # 泰拉瑞亚系统
│   │   ├── 泰拉瑞亚Tile系统开发文档.md    # Tile系统详解
│   │   ├── WorldGen系统文档.md           # WorldGen生成系统
│   │
│   ├── ai-integration/      # AI集成
│   │   ├── AI集成技术方案.md             # HTTP API调用方案
│   │   ├── AI_Agent升级方案.md           # Agent架构设计
│   │
│   ├── database/            # 数据库
│   │   ├── SQLite数据库设计方案.md       # 数据库Schema
│   │   ├── 数据检索文档.md               # 混合检索方案
│   │   ├── 数据库说明.md                 # 数据库使用说明
│   │   ├── npc_house_data.json           # NPC房屋数据
│   │
│   ├── project/             # 项目概览
│   │   ├── 项目结构文档.md               # 项目架构说明
│   │   ├── 建筑生成模组分析文档.md       # 现有模组分析
│   │
│   └── 数据检索文档.md       # 旧版检索文档
│
├── bin/                     # 编译输出（自动生成）
├── obj/                     # 编译缓存（自动生成）
│
└── walls_data.json          # 墙壁数据源
```

---

## 核心文件详解

### 模组入口

| 文件 | 功能 |
|------|------|
| [trab.cs](trab.cs) | 模组主类，初始化建筑执行器和知识库，提供配置访问接口 |
| [build.txt](build.txt) | 模组版本号定义 |
| [description.txt](description.txt) | 模组描述文本 |

### Core 核心服务

| 文件 | 功能 | 关键类/方法 |
|------|------|-------------|
| [AIAgentService.cs](Core/AIAgentService.cs) | AI Agent建筑生成服务，多Agent协作模式 | `GenerateBuildingAsync()`, `GenerateBuildingMultiAgentAsync()`, `PlanBuildingAsync()` |
| [AIApiService.cs](Core/AIApiService.cs) | AI API调用封装，支持OpenAI/Claude/DeepSeek格式 | `SendChatRequestAsync()`, `ExtractJsonFromResponse()` |
| [BuildingExecutor.cs](Core/BuildingExecutor.cs) | 基础建筑执行器，解析JSON并放置方块 | `ParseDesign()`, `BuildAtLocation()` |
| [EnhancedBuildingExecutor.cs](Core/EnhancedBuildingExecutor.cs) | 增强版执行器，支持油漆、斜坡、阴影效果 | `BuildAtLocationEnhanced()` |
| [BuildingMerger.cs](Core/BuildingMerger.cs) | 合并多个模块生成结果，验证并填充缺失元素 | `Merge()`, `ValidateAndFillMissing()` |
| [KnowledgeBase.cs](Core/KnowledgeBase.cs) | 知识库管理器，方块/风格/家具/屋顶/窗户/楼层模板 | `TileKnowledgeBase`, `StyleTemplateBase`, `RoofTemplateBase` |
| [ModuleAgents.cs](Core/ModuleAgents.cs) | 模块生成Agent，并行生成屋顶/墙壁/楼层/窗户/家具 | `GenerateRoofAsync()`, `GenerateWallsAsync()`, `GenerateFurnitureAsync()` |
| [VectorKnowledgeBase.cs](Core/VectorKnowledgeBase.cs) | 向量知识库，语义相似度检索 | `SearchTilesSemantic()`, `CosineSimilarity()` |

### Data 数据结构

| 文件 | 功能 | 关键类 |
|------|------|--------|
| [BuildingDesign.cs](Data/BuildingDesign.cs) | 建筑设计JSON结构，包含tiles/walls/furniture等 | `BuildingDesign`, `TileData`, `WallData`, `FurnitureData` |
| [BuildingPlan.cs](Data/BuildingPlan.cs) | 建筑规划结构，区域划分和参数 | `BuildingPlan`, `Region`, `ModuleResult` |
| tile_embeddings.json | 方块向量数据，用于语义检索 | - |
| style_embeddings.json | 风格向量数据 | - |

### Commands 聊天命令

| 文件 | 功能 | 命令 |
|------|------|------|
| [BuildCommands.cs](Commands/BuildCommands.cs) | AI建筑生成命令处理 | `/aibuild <描述>`, `/aibuild help`, `/aibuild list`, `/aibuild config`, `/aibuild stop` |
| - | 快速预设建筑生成 | `/quickbuild <预设>`, `/quickbuild list` |

预设建筑: `house` (木屋), `castle` (城堡), `tower` (塔楼), `cave` (地下室), `shop` (商店)

### Config 配置

| 文件 | 功能 | 配置项 |
|------|------|--------|
| [AIBuildingConfig.cs](Config/AIBuildingConfig.cs) | 客户端配置类 | `ApiKey`, `ServiceProvider`, `ModelName`, `BuildOffsetX/Y`, `MaxBuildingSize` |

支持的API服务商: `DeepSeek`, `Claude`, `OpenAI`, `DashScope`, `Custom`

### Players 玩家扩展

| 文件 | 功能 | 关键方法 |
|------|------|----------|
| [AIBuildingPlayer.cs](Players/AIBuildingPlayer.cs) | 玩家ModPlayer，管理生成状态和快捷键 | `RequestBuildingDesignAgent()`, `PlaceLastDesign()`, `StopGeneration()` |

快捷键: `P` (开关UI), `B` (放置建筑), `G` (生成), `M` (选区), `S` (停止)

### UI 用户界面

| 文件 | 功能 | 组件 |
|------|------|------|
| [AIBuildingUI.cs](UI/AIBuildingUI.cs) | AI建筑面板，风格/尺寸/操作/日志 | `AIBuildingPanel`, `AIBuildingUISystem` |

UI模块: 风格选择(6种)、尺寸选择(3种)、操作按钮(生成/放置/选区/Agent)、日志显示

---

## Tools Python工具

### crawler Wiki爬虫

| 文件 | 功能 |
|------|------|
| [building_crawler.py](Tools/crawler/building_crawler.py) | 爬取泰拉瑞亚Wiki建筑相关页面 |
| [tile_crawler.py](Tools/crawler/tile_crawler.py) | 爬取方块Wiki页面，提取方块属性 |

### database 数据库

| 文件 | 功能 |
|------|------|
| [init_full_db.py](Tools/database/init_full_db.py) | 初始化完整数据库，创建所有表 |
| [add_basic_tiles.py](Tools/database/add_basic_tiles.py) | 导入基础方块数据 |
| [import_json_to_db.py](Tools/database/import_json_to_db.py) | JSON数据导入数据库 |
| [show_db_stats.py](Tools/database/show_db_stats.py) | 显示数据库统计信息 |

### vector 向量生成

| 文件 | 功能 | 说明 |
|------|------|------|
| [generate_embeddings_smart.py](Tools/vector/generate_embeddings_smart.py) | 智能向量生成（推荐） | 分批处理，自动重试，成本优化 |
| [generate_embeddings_pro.py](Tools/vector/generate_embeddings_pro.py) | 专业版向量生成 | 更详细的分类和属性提取 |
| [generate_embeddings.py](Tools/vector/generate_embeddings.py) | 基础向量生成 | 简单版本 |

---

## docs 文档

详细文档见 [docs/README.md](docs/README.md)

| 目录 | 内容 |
|------|------|
| tutorial/ | tModLoader开发教程、编译方式、日志排查 |
| terraria-systems/ | Tile系统、WorldGen系统技术文档 |
| ai-integration/ | AI集成方案、Agent架构设计 |
| database/ | 数据库设计、检索方案 |
| project/ | 项目结构、模组分析 |

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
6. **选区** 模式可拖拽选择生成区域

### Agent模式

启用Agent模式后，AI将:
1. 规划建筑区域和结构
2. 并行生成5个模块（屋顶、墙壁、楼层、窗户、家具）
3. 合并模块结果
4. 验证NPC房屋要求并自动补充缺失元素

---

## API配置

在模组配置中设置:

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

## 架构说明

### Agent生成流程

```
用户描述 → 规划Agent(区域划分) → 模块Agents(并行生成)
         ↓                      ↓
    BuildingPlan          ModuleResult[]
                                ↓
                         BuildingMerger(合并验证)
                                ↓
                         BuildingDesign → EnhancedBuildingExecutor → 世界放置
```

### 知识库结构

```
KnowledgeBaseManager
├── Tiles (TileKnowledgeBase)     # 方块知识库
├── Styles (StyleTemplateBase)    # 风格模板库
├── Furniture (FurnitureRuleBase) # 家具规则库
├── Vectors (VectorKnowledgeBase) # 向量检索库
├── Roofs (RoofTemplateBase)      # 屋顶模板库
├── Windows (WindowTemplateBase)  # 窗户模板库
└── Floors (FloorStructureBase)   # 楼层结构库
```

### 工具调用

Agent可调用的工具:
- `search_tiles` - 搜索方块（SQL过滤 + 向量排序）
- `get_style_template` - 获取风格模板
- `search_furniture` - 搜索家具
- `get_paint_scheme` - 获取油漆方案
- `get_roof_template` - 获取屋顶设计
- `get_window_template` - 获取窗户设计
- `get_floor_structure` - 获取楼层结构

---

## 开发说明

### 编译要求

- tModLoader v2024.x+
- .NET 8.0
- Newtonsoft.Json (已包含)

### 依赖

项目使用 tModLoader 内置的 Newtonsoft.Json，无需额外安装。

---

## 许可证

MIT License