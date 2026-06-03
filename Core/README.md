# Core - 核心服务层

本目录包含AI建筑生成模组的核心业务逻辑和服务实现。

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
│   ├── AIAgentService.cs        # Agent入口服务 (2.5KB)
│   │
│   ├── MultiAgent/              # 多Agent协作模式
│   │   ├── BuildingMerger.cs    # 建筑合并器 (11.7KB)
│   │   ├── BuildingMultiAgent.cs # 多Agent协调器 (9.8KB)
│   │   ├── BuildingPlan.cs      # 建筑规划 (3.2KB)
│   │   └ ModuleAgents.cs        # 模块Agent (23KB)
│   │
│   └── SingleAgent/             # 单Agent模式
│       └ BuildingSingleAgent.cs # 单Agent生成器 (30KB)
│
├── Building/                    # 建筑执行层
│   ├── BuildingExecutor.cs      # 基础执行器 (15KB)
│   ├── BuildingRequirement.cs   # 建筑需求验证 (6.3KB)
│   └ EnhancedBuildingExecutor.cs # 增强执行器 (4KB)
│
└── KnowledgeBase/               # 知识库层
    ├── KnowledgeBaseManager.cs  # 知识库管理器 (2.4KB)
    ├── TileKnowledgeBase.cs     # 方块知识库 (8.4KB)
    ├── VectorKnowledgeBase.cs   # 向量知识库 (28.5KB)
    ├── StyleTemplateBase.cs     # 风格模板库 (3.5KB)
    ├── FurnitureRuleBase.cs     # 家具规则库 (2.2KB)
    ├── ComponentTemplates.cs    # 构件模板库 (10KB)
    └ BuildingEntityBase.cs      # 建筑实体基类 (20.8KB)
```

---

## 模块说明

### API/ - API服务层

处理与AI API的通信，支持多种服务商。

| 文件 | 功能 |
|------|------|
| AIApiService.cs | AI API调用封装，支持OpenAI/Claude/DeepSeek/DashScope格式 |
| ApiResponseTypes.cs | API响应数据结构定义 |
| ApiServiceBase.cs | API服务基类，提供HTTP请求公共方法 |

**支持的API格式**:
- OpenAI格式 (DeepSeek, OpenAI)
- Anthropic格式 (Claude, DashScope)

### Agents/ - Agent服务层

AI Agent架构实现，支持单Agent和多Agent两种模式。

| 文件/目录 | 功能 |
|-----------|------|
| AIAgentService.cs | Agent入口服务，根据配置选择单/多Agent模式 |
| MultiAgent/ | 多Agent协作模式，规划+模块并行生成 |
| SingleAgent/ | 单Agent模式，直接生成完整建筑 |

**MultiAgent 子模块**:
| 文件 | 功能 |
|------|------|
| BuildingMultiAgent.cs | 多Agent协调器，调度规划和模块生成 |
| BuildingMerger.cs | 合并模块结果，验证NPC房屋要求 |
| BuildingPlan.cs | 建筑规划数据结构 |
| ModuleAgents.cs | 5个模块Agent（屋顶/墙壁/楼层/窗户/家具） |

### Building/ - 建筑执行层

将建筑设计转换为游戏世界中的实际建筑。

| 文件 | 功能 |
|------|------|
| BuildingExecutor.cs | 基础执行器，解析JSON并放置方块 |
| EnhancedBuildingExecutor.cs | 增强执行器，支持油漆、斜坡、阴影效果 |
| BuildingRequirement.cs | 建筑需求验证，检查方块ID有效性 |

### KnowledgeBase/ - 知识库层

提供方块、墙壁、家具、风格等数据的查询和语义检索。

| 文件 | 功能 |
|------|------|
| KnowledgeBaseManager.cs | 知识库管理器（单例模式） |
| TileKnowledgeBase.cs | 方块知识库，提供搜索和过滤 |
| VectorKnowledgeBase.cs | 向量知识库，384维语义检索 |
| StyleTemplateBase.cs | 风格模板库（9种风格） |
| FurnitureRuleBase.cs | 家具规则库，NPC房屋功能映射 |
| ComponentTemplates.cs | 构件模板库（屋顶/窗户/楼层） |
| BuildingEntityBase.cs | 建筑实体基类，蓝图数据访问 |

---

## 核心类详解

### AIAgentService

Agent入口服务，统一调度单/多Agent模式。

```csharp
// 使用方式
var agent = new AIAgentService(apiKey, serviceType, modelName);
var design = await agent.GenerateBuildingAsync(prompt, progressCallback, ct);
```

### BuildingMultiAgent

多Agent协调器，实现规划+模块并行生成。

**生成流程**:
1. 规划Agent - 划分建筑区域
2. 模块Agents - 并行生成5个模块
3. 合并器 - 合并验证生成最终设计

### BuildingSingleAgent

单Agent模式，直接生成完整建筑。

**特点**:
- 简单直接，适合小型建筑
- 支持工具调用（search_tiles等）
- 单次API请求完成生成

### VectorKnowledgeBase

向量知识库，提供语义相似度检索。

**关键方法**:
- `Initialize()` - 加载JSON向量数据
- `CosineSimilarity(a, b)` - 计算余弦相似度
- `SearchTilesSemantic(candidates, style, topK)` - 方块语义检索
- `SearchWallsSemantic(candidates, style, topK)` - 墙壁语义检索
- `SearchFurnitureSemantic(candidates, category, topK)` - 家具语义检索

**向量维度**: 384维 (SentenceTransformers)

### BuildingEntityBase

建筑实体基类，提供蓝图数据访问。

**功能**:
- 从数据库加载建筑蓝图实体
- 向量相似度搜索蓝图
- 获取蓝图详细信息

---

## 数据流

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

---

## Agent工具定义

Agent可调用的工具:

| 工具名 | 功能 | 参数 |
|--------|------|------|
| search_tiles | 搜索方块 | style, category, biome |
| get_style_template | 获取风格模板 | style, building_type |
| search_furniture | 搜索家具 | room_type, npc_type |
| get_paint_scheme | 获取油漆方案 | style, theme |
| get_roof_template | 获取屋顶设计 | roof_type, style, width |
| get_window_template | 获取窗户设计 | window_type, style |
| get_floor_structure | 获取楼层结构 | structure_type, style |
| search_building_entities | 搜索建筑蓝图 | query, style, top_k |

---

## 相关文档

- [AI集成技术方案](../docs/ai-integration/AI集成技术方案.md)
- [AI_Agent升级方案](../docs/ai-integration/AI_Agent升级方案.md)
- [数据检索文档](../docs/database/数据检索文档.md)
- [建筑蓝图数据格式](../docs/database/建筑蓝图数据格式.md)