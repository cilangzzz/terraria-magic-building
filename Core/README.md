# Core - 核心服务层

本目录包含AI建筑生成模组的核心业务逻辑，采用**构件级生成架构**。

---

## 架构概述

### 核心流程

```
用户描述 → TrueAgentCore
              ↓
         工具调用循环 (最多10轮)
              ↓
    ┌─────────┼─────────┐
    ↓         ↓         ↓
search_   get_      generate_
buildings component design_rules
    ↓         ↓         ↓
    └─────────┴─────────┘
              ↓
         BuildingRules
              ↓
         ProceduralBuilder
              ↓
         TEditSchDesign
              ↓
         BuildingExecutor → 世界放置
```

### 构件层次架构

```
层次4: Complex    (建筑群)  → 村庄、基地
层次3: Building   (建筑)    → 住宅、塔楼、商店、神庙
层次2: Composite  (复合构件) → 房间、楼层
层次1: Atomic     (原子构件) → 屋顶、墙壁、装饰、基础
```

---

## 目录结构

```
Core/
├── README.md                    # 本说明文件
│
├── API/                         # API服务层
│   ├── AIApiService.cs          # AI API调用服务 (6.9KB)
│   ├── ApiResponseTypes.cs      # API响应类型定义 (2KB)
│   └ ApiServiceBase.cs          # API服务基类 (2.8KB)
│
├── Agents/                      # Agent服务层
│   ├── AIAgentService.cs        # Agent入口服务 (2.2KB)
│   ├── TrueAgentCore.cs         # 真Agent核心 (25KB)
│   │
│   └── Tools/                   # Agent工具集
│       ├── ToolRegistry.cs              # 工具注册中心 (9KB)
│       ├── BuildingTools.cs             # 建筑工具集 (16KB)
│       ├── SearchMaterialsTool.cs       # 材料搜索 (7KB)
│       ├── GetMaterialRecommendationTool.cs # 材料推荐 (9KB)
│       ├── GetTemplateDetailsTool.cs    # 模板详情 (3KB)
│       ├── GetBuildingSequenceTool.cs   # 建筑序列 (4KB)
│       ├── GenerateDesignRulesTool.cs   # 设计规则 (6KB)
│       └ ValidateRequirementsTool.cs   # 需求验证 (5KB)
│
├── Building/                    # 建筑执行层
│   ├── BuildingExecutor.cs      # 基础执行器 (15KB)
│   ├── EnhancedBuildingExecutor.cs # 增强执行器 (4KB)
│   ├── ProceduralBuilder.cs     # 程序化生成器 (33KB)
│   └ BuildingRequirement.cs     # 需求验证 (6KB)
│
└── KnowledgeBase/               # 知识库层
    ├── KnowledgeBaseManager.cs  # 知识库管理器 (1.8KB)
    ├── ComponentKnowledgeBase.cs # 构件知识库 (29KB)
    ├── ComponentData.cs         # 构件数据结构 (8KB)
    └ BuildingEntityBase.cs      # 建筑实体基类 (39KB)
```

---

## 模块说明

### API/ - API服务层

处理与AI API的通信，支持多种服务商。

| 文件 | 功能 |
|------|------|
| AIApiService.cs | AI API调用封装，支持OpenAI/Claude/DeepSeek/DashScope |
| ApiResponseTypes.cs | API响应数据结构定义 |
| ApiServiceBase.cs | API服务基类，提供HTTP请求公共方法 |

**支持的API格式**:
- OpenAI格式 (DeepSeek, OpenAI)
- Anthropic格式 (Claude, DashScope)

### Agents/ - Agent服务层

#### TrueAgentCore

真Agent核心实现，实现完整的工具调用循环。

**系统提示核心流程**:
1. **理解需求** - 分析用户描述，确定风格、类型、尺寸
2. **检索建筑模板** - 调用 search_buildings
3. **获取构件详情** - 调用 get_building_details
4. **获取生成规则** - 调用 get_component_rules
5. **获取材料推荐** - 调用 get_style_materials
6. **生成设计规则** - 调用 generate_design_rules

**工具调用示例**:
```json
// search_buildings
{ "style": "asian", "building_type": "house", "min_width": 15 }

// get_building_details
{ "building_id": "chinese_house_001" }

// get_style_materials
{ "style": "asian" }

// generate_design_rules
{ "style": "asian", "width": 20, "height": 15 }
```

#### ToolRegistry

工具注册中心，管理所有可用工具。

```csharp
// 注册工具
ToolRegistry.Register(new SearchBuildingsTool());
ToolRegistry.Register(new GetBuildingDetailsTool());
// ...

// 调用工具
var result = await ToolRegistry.ExecuteAsync("search_buildings", parameters, kb);
```

### Building/ - 建筑执行层

#### ProceduralBuilder

程序化建筑生成器，**核心模块**。

将AI输出的精简设计规则（BuildingRules）展开为完整的方块数据（TEditSchDesign）。

**生成流程**:
```
BuildingRules
    ↓
InitializeGrid() - 初始化网格
    ↓
ResolveMaterialPalette() - 解析材料调色板
    ↓
GenerateFrame() - 生成框架
    ↓
GenerateWalls() - 生成墙壁
    ↓
GenerateFloor() - 生成楼层 (多层)
    ↓
GenerateRoof() - 生成屋顶
    ↓
GenerateRoom() - 生成房间
    ↓
PlaceDecoration() - 放置装饰
    ↓
TEditSchDesign
```

**支持的屋顶类型**:
- `pagoda` - 宝塔顶（中式）
- `gable` - 人字顶（中世纪）
- `pyramid` - 金字塔顶
- `flat` - 平顶（现代）

### KnowledgeBase/ - 知识库层

#### ComponentKnowledgeBase

构件级建筑知识库，**核心数据存储**。

**数据结构**:
```csharp
private Dictionary<string, ComponentDefinition> _atomicComponents;  // 原子构件
private Dictionary<string, BuildingEntityV2> _buildings;           // 建筑实体
private Dictionary<string, StyleMaterialMapping> _styleMaterials;  // 风格材料
private Dictionary<string, float[]> _buildingVectors;              // 建筑向量
private Dictionary<string, float[]> _componentVectors;             // 构件向量
```

**核心方法**:
| 方法 | 功能 |
|------|------|
| `Initialize()` | 加载所有数据 |
| `SearchBuildings(criteria)` | 检索建筑模板 |
| `GetBuilding(id)` | 获取建筑详情 |
| `GetComponent(id)` | 获取构件定义 |
| `GetStyleMaterials(style)` | 获取风格材料 |

#### ComponentDefinition

构件定义数据结构。

```csharp
public class ComponentDefinition
{
    public string id;                      // 构件ID
    public string type;                    // roof/wall/floor/decoration/foundation
    public string subtype;                 // 具体类型
    public ComponentBounds bounds_relative; // 相对边界（可缩放）
    public ComponentBounds bounds_absolute; // 绝对边界
    public ComponentMaterials materials;   // 材料配置
    public GenerationRule generation_rule;  // 生成规则
    public Dictionary<string, object> parameters; // 参数
}
```

#### KnowledgeBaseManager

知识库统一管理器（单例）。

```csharp
// 初始化
KnowledgeBaseManager.Instance.Initialize();

// 访问构件库
var components = KnowledgeBaseManager.Instance.Components;

// 检索建筑
var buildings = components.SearchBuildings(new BuildingSearchCriteria {
    style = "asian",
    building_type = "house",
    min_width = 15
});
```

---

## 数据流详解

### 完整生成流程

```
1. 用户输入: "一座中式风格的宝塔"

2. TrueAgentCore 启动工具调用循环:

   第1轮: search_buildings({ style: "asian", building_type: "tower" })
   → 返回: [{ id: "chinese_pagoda_001", name: "中式宝塔", width: 15, height: 25 }]

   第2轮: get_building_details({ building_id: "chinese_pagoda_001" })
   → 返回: { components: ["roof_pagoda", "wall_outer", "floor_main", ...] }

   第3轮: get_style_materials({ style: "asian" })
   → 返回: { primary: { tile_id: 179, wall_id: 172 }, ... }

   第4轮: generate_design_rules({ style: "asian", width: 15, height: 25 })
   → 返回: BuildingRules JSON

3. ProceduralBuilder.GenerateFromRules(BuildingRules)
   → 展开为完整方块数据 TEditSchDesign

4. BuildingExecutor.BuildAtLocation(TEditSchDesign, x, y)
   → 在世界放置建筑
```

---

## Agent工具定义

| 工具名 | 功能 | 输入参数 |
|--------|------|----------|
| search_buildings | 检索建筑模板 | style, building_type, complexity, min/max_width/height, npc_valid |
| get_building_details | 获取建筑构件详情 | building_id |
| get_component_rules | 获取构件生成规则 | component_id |
| get_style_materials | 获取风格材料推荐 | style |
| generate_design_rules | 生成设计规则JSON | style, width, height, requirements |
| validate_requirements | 验证需求 | design |

---

## 相关文档

- [AI集成技术方案](../docs/ai-integration/AI集成技术方案.md)
- [AI_Agent升级方案](../docs/ai-integration/AI_Agent升级方案.md)
- [AI建筑生成升级方案_模板检索](../docs/ai-integration/AI建筑生成升级方案_模板检索.md)
- [数据检索文档](../docs/database/数据检索文档.md)
- [建筑蓝图数据格式](../docs/database/建筑蓝图数据格式.md)