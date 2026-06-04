# trab - AI建筑生成模组

泰拉瑞亚 tModLoader AI建筑生成模组，通过AI Agent自动生成精美建筑。

## 功能特性

- **构件级生成**: 基于多层次架构（原子构件→复合构件→建筑→建筑群）
- **真Agent架构**: 工具调用循环、自主决策、多轮对话
- **程序化生成**: AI输出设计规则，程序化生成器展开为完整方块数据
- **模板检索**: 向量语义搜索建筑模板，智能匹配用户需求
- **风格材料推荐**: 自动推荐适合风格的方块和墙壁材料
- **多种API支持**: DeepSeek、Claude、OpenAI、DashScope

## 快速开始

1. 安装 tModLoader
2. 将本模组放入 `ModSources/trab` 目录
3. 在游戏中编译并启用模组
4. 按 **P** 键打开AI建筑UI，或使用 `/aibuild <描述>` 命令
5. 在模组配置中设置API密钥 (ESC -> 模组配置 -> trab Config)

---

## 架构概述

### 核心处理流程

```
用户描述 → TrueAgentCore (工具调用循环)
              ↓
         1. search_buildings (检索建筑模板)
              ↓
         2. get_building_details (获取构件详情)
              ↓
         3. get_component_rules (获取生成规则)
              ↓
         4. get_style_materials (获取材料推荐)
              ↓
         5. generate_design_rules (生成设计规则JSON)
              ↓
         BuildingRules (精简设计规则)
              ↓
         ProceduralBuilder (程序化展开)
              ↓
         TEditSchDesign (完整方块数据)
              ↓
         BuildingExecutor → 世界放置
```

### 构件层次架构

```
层次4: Complex (建筑群)    → 村庄、基地
层次3: Building (完整建筑) → 住宅、塔楼、商店、神庙
层次2: Composite (复合构件) → 房间、楼层
层次1: Atomic (原子构件)   → 屋顶、墙壁、装饰、基础
```

---

## 目录结构

```
trab/
├── trab.cs                  # 模组主入口类
├── trab.csproj              # 项目配置文件
├── build.txt                # 模组版本信息
├── description.txt          # 模组描述
├── LICENSE                  # MIT许可证
│
├── Core/                    # 核心服务层
│   ├── README.md            # 核心层说明
│   │
│   ├── API/                 # API服务层
│   │   ├── AIApiService.cs      # AI API调用服务
│   │   ├── ApiResponseTypes.cs  # API响应类型
│   │   └ ApiServiceBase.cs      # API服务基类
│   │
│   ├── Agents/              # Agent服务层
│   │   ├── AIAgentService.cs    # Agent入口服务
│   │   ├── TrueAgentCore.cs     # 真Agent核心实现
│   │   │
│   │   └── Tools/               # Agent工具集
│   │       ├── ToolRegistry.cs            # 工具注册中心
│   │       ├── BuildingTools.cs           # 建筑检索工具集
│   │       ├── SearchMaterialsTool.cs     # 材料搜索工具
│   │       ├── GetMaterialRecommendationTool.cs # 材料推荐工具
│   │       ├── GetTemplateDetailsTool.cs  # 模板详情工具
│   │       ├── GetBuildingSequenceTool.cs # 建筑序列工具
│   │       ├── GenerateDesignRulesTool.cs # 设计规则生成工具
│   │       └ ValidateRequirementsTool.cs # 需求验证工具
│   │
│   ├── Building/            # 建筑执行层
│   │   ├── BuildingExecutor.cs      # 基础执行器
│   │   ├── EnhancedBuildingExecutor.cs # 增强执行器
│   │   ├── ProceduralBuilder.cs     # 程序化生成器 (核心)
│   │   └ BuildingRequirement.cs     # 建筑需求验证
│   │
│   └── KnowledgeBase/       # 知识库层
│       ├── KnowledgeBaseManager.cs    # 知识库管理器
│       ├── ComponentKnowledgeBase.cs  # 构件知识库 (核心)
│       ├── ComponentData.cs           # 构件数据结构
│       └ BuildingEntityBase.cs        # 建筑实体基类 (兼容)
│
├── Data/                    # 数据层
│   ├── BuildingDesign.cs    # 建筑设计数据结构
│   ├── BuildingRules.cs     # 设计规则数据结构 (AI输出)
│   ├── TEditSchDesign.cs    # TEdit蓝图数据结构
│   │
│   ├── kb/                  # 知识库数据库
│   │   └ buildings_v3.sql       # 构件级数据库Schema
│   │
│   └── vectors/             # 向量数据
│       ├── tile_embeddings.json      # 方块向量
│       ├── wall_embeddings.json      # 墙壁向量
│       ├── furniture_embeddings.json # 家具向量
│       ├── style_embeddings.json     # 风格向量
│       └ schematic_embeddings.json  # 蓝图向量
│
├── Commands/                # 聊天命令层
│   └── BuildCommands.cs     # /aibuild, /quickbuild
│
├── Config/                  # 配置层
│   └ AIBuildingConfig.cs    # 模组配置
│
├── Players/                 # 玩家扩展层
│   └ AIBuildingPlayer.cs    # ModPlayer实现
│
├── UI/                      # 用户界面层
│   └ AIBuildingUI.cs        # AI建筑UI面板
│
├── Tools/                   # Python工具集
│   ├── database/            # 数据库管理
│   ├── crawler/             # Wiki爬虫
│   ├── vector/              # 向量生成
│   └ python/                # Python工具
│
└── docs/                    # 文档目录
```

---

## 核心模块详解

### Core/Agents - Agent服务层

#### TrueAgentCore

真Agent核心实现，替代原有的SingleAgent和MultiAgent架构。

**核心特性**:
- 统一的Agent实现
- 工具调用循环（最多10轮）
- 自主决策架构
- 错误恢复机制

**Agent工具集**:

| 工具名 | 功能 | 输入 |
|--------|------|------|
| search_buildings | 检索建筑模板 | style, building_type, complexity, size |
| get_building_details | 获取建筑构件详情 | building_id |
| get_component_rules | 获取构件生成规则 | component_id |
| get_style_materials | 获取风格材料推荐 | style |
| generate_design_rules | 生成设计规则JSON | requirements |
| validate_requirements | 验证需求 | design |

### Core/KnowledgeBase - 知识库层

#### ComponentKnowledgeBase

构件级建筑知识库，实现多层次架构。

**数据结构**:
- `_atomicComponents` - 原子构件（屋顶、墙壁、装饰）
- `_buildings` - 完整建筑实体
- `_styleMaterials` - 风格材料映射
- `_buildingVectors` - 建筑向量（语义检索）
- `_componentVectors` - 构件向量

**复杂性层次**:
| 层次 | 名称 | 说明 | 示例 |
|------|------|------|------|
| Atomic | 原子构件 | 最小不可分割 | 屋顶、单墙、灯笼 |
| Composite | 复合构件 | 原子组合 | 房间、楼层 |
| Building | 完整建筑 | 复合组合 | 住宅、塔楼、商店 |
| Complex | 建筑群 | 建筑组合 | 村庄、基地 |

#### ComponentDefinition

构件定义核心数据结构。

```csharp
public class ComponentDefinition
{
    public string id;                    // 构件ID
    public string type;                  // roof, wall, floor, decoration, foundation
    public string subtype;               // pagoda, outer_wall, lantern, etc.
    public ComponentBounds bounds_relative;  // 相对边界（可缩放）
    public ComponentBounds bounds_absolute;  // 绝对边界（原始坐标）
    public ComponentMaterials materials;     // 材料配置
    public GenerationRule generation_rule;   // 生成规则
    public Dictionary<string, object> parameters; // 参数
}
```

### Core/Building - 建筑执行层

#### ProceduralBuilder

程序化建筑生成器，将设计规则展开为完整方块数据。

**核心方法**:
- `GenerateFromRules(BuildingRules)` - 从规则生成建筑
- `GenerateFrame()` - 生成框架
- `GenerateWalls()` - 生成墙壁
- `GenerateFloor()` - 生成楼层
- `GenerateRoof()` - 生成屋顶
- `PlaceDecoration()` - 放置装饰

### Data - 数据层

#### BuildingRules

设计规则数据结构，AI输出的精简格式。

```json
{
  "name": "中式住宅",
  "width": 20,
  "height": 15,
  "style": "asian",
  "template_id": "chinese_house_001",
  "structure": {
    "frame": { "material": "primary" },
    "walls": { "outer_material": "primary", "inner_material": "secondary" },
    "floors": [{ "level": 1, "thickness": 1 }],
    "roof": { "shape": "pagoda", "tier_count": 2 }
  },
  "decorations": [{ "type": "lantern", "position": "entrance" }],
  "materials": {
    "primary": { "tile_id": 179, "wall_id": 172 },
    "secondary": { "tile_id": 5, "wall_id": 4 }
  }
}
```

---

## 数据库设计

### buildings_v3.sql

构件级建筑数据库Schema。

**核心表**:

| 表名 | 功能 |
|------|------|
| building_index | 建筑索引，向量检索 |
| buildings | 完整建筑实体 |
| building_components | 构件定义 |
| component_rules | 构件生成规则 |
| style_materials | 风格材料映射 |

---

## 使用方法

### 聊天命令

```
/aibuild 一座中式风格的宝塔
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

---

## API配置

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| ApiKey | API密钥 | 空 |
| ServiceProvider | API服务商 | DeepSeek |
| ModelName | 模型名称 | deepseek-chat |
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
| C# 源文件 | 26 |
| Python 脚本 | 11 |
| Markdown 文档 | 17 |
| JSON 数据文件 | 6 |
| SQL 文件 | 1 |

---

## 许可证

MIT License - Copyright (c) 2026 Cilang