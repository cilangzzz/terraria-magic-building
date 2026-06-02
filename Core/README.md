# Core - 核心服务层

本目录包含AI建筑生成模组的核心业务逻辑和服务实现。

---

## 文件列表 (10个文件)

| 文件 | 行数 | 功能描述 |
|------|------|----------|
| AIAgentService.cs | 1238 | AI Agent建筑生成服务，多Agent协作模式的主生成器 |
| KnowledgeBase.cs | 602 | 知识库管理器，包含方块/风格/家具/屋顶/窗户/楼层模板库 |
| ModuleAgents.cs | 619 | 模块生成Agent，并行生成屋顶/墙壁/楼层/窗户/家具5个模块 |
| VectorKnowledgeBase.cs | 504 | 向量知识库，384维embedding语义相似度检索 |
| BuildingDesign.cs | 464 | 建筑设计JSON数据结构，包含tiles/walls/furniture/doors/lightSources |
| BuildingMerger.cs | 276 | 建筑模块合并器，合并多Agent结果并验证NPC房屋要求 |
| BuildingExecutor.cs | 244 | 基础建筑执行器，解析JSON并在世界放置方块 |
| BuildingPlan.cs | 112 | 建筑规划数据结构，定义区域划分和模块参数 |
| AIApiService.cs | 241 | AI API调用封装，支持OpenAI/Claude/DeepSeek/DashScope格式 |
| EnhancedBuildingExecutor.cs | 120 | 增强版执行器，支持油漆、斜坡、阴影效果 |

---

## 核心类说明

### AIAgentService

AI Agent建筑生成的主服务类。

**关键方法**:
- `GenerateBuildingAsync()` - Agent主入口
- `GenerateBuildingMultiAgentAsync()` - 多Agent协作模式
- `PlanBuildingAsync()` - 规划Agent，划分建筑区域
- `RunOpenAIAgentLoop()` - OpenAI格式Agent循环
- `RunAnthropicAgentLoop()` - Anthropic格式Agent循环
- `ExecuteTool()` - 执行工具调用

**工具定义**: search_tiles, get_style_template, search_furniture, get_paint_scheme, get_roof_template, get_window_template, get_floor_structure

### KnowledgeBaseManager

知识库统一管理器（单例模式）。

**子库**:
- `Tiles` - TileKnowledgeBase (方块知识库)
- `Styles` - StyleTemplateBase (风格模板库)
- `Furniture` - FurnitureRuleBase (家具规则库)
- `Vectors` - VectorKnowledgeBase (向量检索库)
- `Roofs` - RoofTemplateBase (屋顶模板库)
- `Windows` - WindowTemplateBase (窗户模板库)
- `Floors` - FloorStructureBase (楼层结构库)

### ModuleAgents

模块生成Agent，负责并行生成建筑的各个模块。

**模块生成方法**:
- `GenerateRoofAsync()` - 生成屋顶模块
- `GenerateWallsAsync()` - 生成墙壁模块
- `GenerateFloorsAsync()` - 生成楼层模块
- `GenerateWindowsAsync()` - 生成窗户模块
- `GenerateFurnitureAsync()` - 生成家具模块

### VectorKnowledgeBase

向量知识库，支持语义检索。

**关键方法**:
- `Initialize()` - 加载JSON向量数据
- `CosineSimilarity()` - 计算余弦相似度
- `SearchTilesSemantic()` - 方块语义检索
- `SearchWallsSemantic()` - 墙壁语义检索
- `SearchFurnitureSemantic()` - 家具语义检索

**向量维度**: 384维 (SentenceTransformers)

### BuildingDesign

建筑设计JSON结构，Agent输出的目标格式。

**主要字段**:
- `Name`, `Description`, `Width`, `Height`, `Style`
- `Tiles` - List<TileData> 方块列表
- `Walls` - List<WallData> 墙壁列表
- `WallRanges` - List<WallRangeData> 墙壁范围
- `Furniture` - List<FurnitureData> 家具列表
- `Doors` - List<DoorData> 门列表
- `LightSources` - List<LightSourceData> 光源列表
- `NpcSuitability` - NPC房屋验证结果
- `PaintScheme` - 油漆方案
- `ToolCalls` - 工具调用记录

### BuildingMerger

合并多个模块生成结果。

**关键方法**:
- `Merge()` - 合并所有模块
- `DeduplicateTiles()` - 去重重复坐标的方块
- `ValidateAndFillMissing()` - 验证并自动补充缺失元素（门、光源、家具）
- `CreateFallbackDesign()` - 创建备用火柴盒设计

---

## 数据流

```
用户描述 → AIAgentService.GenerateBuildingAsync()
                    ↓
           GenerateBuildingMultiAgentAsync()
                    ↓
    ┌───────────────┼───────────────┐
    ↓               ↓               ↓
PlanBuildingAsync()  ModuleAgents    (并行)
    ↓               ↓
BuildingPlan    ModuleResult[]
                    ↓
            BuildingMerger.Merge()
                    ↓
              BuildingDesign
                    ↓
        EnhancedBuildingExecutor.BuildAtLocationEnhanced()
                    ↓
              世界放置完成
```

---

## API格式支持

| 服务商 | 格式 | 端点 |
|--------|------|------|
| DeepSeek | OpenAI兼容 | https://api.deepseek.com/v1/chat/completions |
| Claude | Anthropic | https://api.anthropic.com/v1/messages |
| OpenAI | OpenAI | https://api.openai.com/v1/chat/completions |
| DashScope | Anthropic兼容 | https://dashscope.aliyuncs.com/compatible-mode/v1/messages |
| Custom | 可配置 | 用户自定义端点 |

---

## 相关文档

- [AI集成技术方案](../docs/ai-integration/AI集成技术方案.md)
- [AI_Agent升级方案](../docs/ai-integration/AI_Agent升级方案.md)
- [数据检索文档](../docs/database/数据检索文档.md)