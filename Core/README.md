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
│   ├── AIAgentService.cs        # Agent入口服务 (2.2KB)
│   ├── TrueAgentCore.cs         # 真Agent核心实现 (25KB)
│   │
│   └── Tools/                   # Agent工具集
│       ├── ToolRegistry.cs              # 工具注册中心 (9KB)
│       ├── SearchMaterialsTool.cs       # 材料搜索工具 (5KB)
│       ├── SearchBuildingTemplatesTool.cs # 模板搜索工具 (5KB)
│       ├── GetMaterialRecommendationTool.cs # 材料推荐工具 (9KB)
│       ├── GetTemplateDetailsTool.cs    # 模板详情工具 (3KB)
│       ├── GetBuildingSequenceTool.cs   # 建筑序列工具 (4.6KB)
│       ├── GenerateDesignRulesTool.cs   # 设计规则工具 (6.8KB)
│       └ ValidateRequirementsTool.cs   # 需求验证工具 (5.6KB)
│
├── Building/                    # 建筑执行层
│   ├── BuildingExecutor.cs      # 基础执行器 (15KB)
│   ├── BuildingRequirement.cs   # 建筑需求验证 (6.3KB)
│   ├── EnhancedBuildingExecutor.cs # 增强执行器 (4KB)
│   └ ProceduralBuilder.cs       # 程序化生成器 (32KB)
│
└── KnowledgeBase/               # 知识库层
    ├── KnowledgeBaseManager.cs  # 知识库管理器 (2.4KB)
    ├── TileKnowledgeBase.cs     # 方块知识库 (8.4KB)
    ├── VectorKnowledgeBase.cs   # 向量知识库 (28.5KB)
    ├── BuildingEntityBase.cs    # 建筑实体基类 (39KB)
    ├── StyleTemplateBase.cs     # 风格模板库 (3.5KB)
    ├── FurnitureRuleBase.cs     # 家具规则库 (2.2KB)
    └ ComponentTemplates.cs      # 构件模板库 (10KB)
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

实现真Agent架构，支持工具调用和多轮对话。

| 文件 | 功能 |
|------|------|
| AIAgentService.cs | Agent入口服务，初始化并启动Agent |
| TrueAgentCore.cs | 真Agent核心实现，工具调用循环和自主决策 |

**TrueAgentCore 特性**:
- 多轮对话循环
- 工具调用分发
- 自主决策架构
- 错误恢复机制

### Agents/Tools/ - Agent工具集

Agent可调用的工具集合。

| 工具类 | 功能 | 输入参数 |
|--------|------|----------|
| SearchMaterialsTool | 搜索建筑材料 | style, category, biome |
| SearchBuildingTemplatesTool | 搜索建筑模板 | style, building_type |
| GetMaterialRecommendationTool | 获取材料推荐 | style, requirements |
| GetTemplateDetailsTool | 获取模板详情 | template_id |
| GetBuildingSequenceTool | 获取建筑序列 | building_type, style |
| GenerateDesignRulesTool | 生成设计规则 | style, constraints |
| ValidateRequirementsTool | 验证需求 | requirements, design |

**ToolRegistry** - 工具注册中心:
- 统一管理所有工具
- 提供工具发现和调用接口
- 处理工具执行结果

### Building/ - 建筑执行层

将建筑设计转换为游戏世界中的实际建筑。

| 文件 | 功能 |
|------|------|
| BuildingExecutor.cs | 基础执行器，解析JSON并放置方块 |
| EnhancedBuildingExecutor.cs | 增强执行器，支持油漆、斜坡、阴影效果 |
| BuildingRequirement.cs | 建筑需求验证，检查方块ID有效性 |
| ProceduralBuilder.cs | 程序化生成器，基于规则生成建筑 |

### KnowledgeBase/ - 知识库层

提供方块、墙壁、家具、风格等数据的查询和语义检索。

| 文件 | 功能 |
|------|------|
| KnowledgeBaseManager.cs | 知识库管理器（单例模式） |
| TileKnowledgeBase.cs | 方块知识库，提供搜索和过滤 |
| VectorKnowledgeBase.cs | 向量知识库，384维语义检索 |
| BuildingEntityBase.cs | 建筑实体基类，蓝图数据访问 |
| StyleTemplateBase.cs | 风格模板库（9种风格） |
| FurnitureRuleBase.cs | 家具规则库，NPC房屋功能映射 |
| ComponentTemplates.cs | 构件模板库（屋顶/窗户/楼层） |

---

## 核心类详解

### TrueAgentCore

真Agent核心实现，实现完整的工具调用循环。

```csharp
// 使用方式
var agent = new TrueAgentCore(apiKey, serviceType, modelName);
var result = await agent.GenerateAsync(prompt, progressCallback, ct);
```

**核心流程**:
1. 接收用户描述
2. 构建系统提示
3. 循环调用工具直到完成
4. 返回建筑设计

### ToolRegistry

工具注册中心，管理所有可用工具。

```csharp
// 注册工具
ToolRegistry.Register(new SearchMaterialsTool());
ToolRegistry.Register(new SearchBuildingTemplatesTool());

// 调用工具
var result = await ToolRegistry.ExecuteAsync("search_materials", parameters);
```

### VectorKnowledgeBase

向量知识库，提供语义相似度检索。

**关键方法**:
- `Initialize()` - 加载JSON向量数据
- `CosineSimilarity(a, b)` - 计算余弦相似度
- `SearchTilesSemantic(candidates, style, topK)` - 方块语义检索
- `SearchWallsSemantic(candidates, style, topK)` - 墙壁语义检索

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
         TrueAgentCore
              ↓
         构建系统提示 + 初始化工具
              ↓
    ┌─────────┴─────────┐
    ↓                   ↓
API调用             工具调用
    ↓                   ↓
响应解析         ToolRegistry分发
    ↓                   ↓
检查完成 ← ← ← 工具执行结果
    ↓
BuildingDesign
    ↓
BuildingExecutor → 世界放置
```

---

## Agent工具定义

Agent可调用的工具:

| 工具名 | 功能 | 参数 |
|--------|------|------|
| search_materials | 搜索建筑材料 | style, category, biome |
| search_building_templates | 搜索建筑模板 | style, building_type, top_k |
| get_material_recommendation | 获取材料推荐 | style, requirements |
| get_template_details | 获取模板详情 | template_id |
| get_building_sequence | 获取建筑序列 | building_type, style |
| generate_design_rules | 生成设计规则 | style, constraints |
| validate_requirements | 验证需求 | requirements, design |

---

## 相关文档

- [AI集成技术方案](../docs/ai-integration/AI集成技术方案.md)
- [AI_Agent升级方案](../docs/ai-integration/AI_Agent升级方案.md)
- [AI建筑生成升级方案_模板检索](../docs/ai-integration/AI建筑生成升级方案_模板检索.md)
- [数据检索文档](../docs/database/数据检索文档.md)
- [建筑蓝图数据格式](../docs/database/建筑蓝图数据格式.md)