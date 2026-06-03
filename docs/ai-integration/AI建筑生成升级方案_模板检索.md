# AI建筑生成升级方案：模板检索 + 分层生成

## 一、核心问题

### 1.1 当前困境

| 问题 | 描述 |
|------|------|
| **生成质量差** | AI缺乏建筑设计的领域知识，生成的建筑不协调、不美观 |
| **数据量大** | 一个53x32的建筑需要1700+个方块数据，超出AI单次输出能力 |
| **缺乏参考** | AI没有看过真实的泰拉瑞亚建筑，无法学习好的设计模式 |

### 1.2 新增资源

你已经有了真实建筑数据 `building_entities.json`，包含：

```json
{
  "id": "20260602215014",
  "dimensions": { "width": 53, "height": 32 },
  "features": {
    "type": "residence",
    "style": "asian_fantasy",
    "structure": "multi_story"
  },
  "materials": {
    "primary_tiles": [...],
    "primary_walls": [...]
  },
  "building_sequence": [
    { "step": 1, "action": "frame", "materials": ["Stone"], "note": "搭建框架" },
    { "step": 2, "action": "walls", ... },
    ...
  ],
  "style_tags": ["asian", "fantasy", "gold", "lantern"],
  "summary": "中式奇幻风格多层住宅..."
}
```

---

## 二、核心思路：不要让AI生成，让AI检索+修改

### 2.1 传统方案（失败）

```
用户: "建一个中式城堡"
    ↓
AI: 直接生成JSON
    ↓
问题: AI不知道"中式"该用什么方块、什么结构
```

### 2.2 新方案（模板检索）

```
用户: "建一个中式城堡"
    ↓
[步骤1] 检索相似建筑模板
    → 找到: "20260602215014" (中式奇幻住宅, similarity=0.85)
    ↓
[步骤2] AI分析用户需求与模板差异
    → 用户要"城堡"，模板是"住宅"
    → 需要调整: 增加防御结构、扩大尺寸
    ↓
[步骤3] 基于模板生成新设计
    → 保留: 风格、材料选择逻辑
    → 修改: 结构布局、尺寸
```

---

## 三、技术架构

### 3.1 三层架构

```
┌─────────────────────────────────────────────────────────────────┐
│                      用户请求层                                  │
│  "建一个中式风格的三层小楼，要有灯笼装饰"                          │
└─────────────────────────┬───────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────────┐
│                    AI理解层 (LLM)                                │
│  - 分析用户意图                                                  │
│  - 提取关键特征: style=asian, floors=3, decor=lantern           │
│  - 构建检索查询                                                  │
└─────────────────────────┬───────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────────┐
│                   模板检索层 (RAG)                               │
│  向量检索: similarity(query, building_vectors) > 0.7            │
│  返回: 相似度最高的3个建筑模板                                    │
└─────────────────────────┬───────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────────┐
│                    生成执行层                                    │
│  方案A: 直接使用模板（相似度>0.9）                                │
│  方案B: 模板+AI修改（相似度0.7-0.9）                              │
│  方案C: 多模板融合（相似度<0.7）                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 数据流

```csharp
// 步骤1: 用户输入
string userPrompt = "建一个中式风格的三层小楼";

// 步骤2: AI理解意图（使用LLM）
var intent = await AnalyzeIntent(userPrompt);
// intent = { style: "asian", type: "residence", floors: 3 }

// 步骤3: 检索相似模板
var templates = buildingEntityBase.SearchByVector(intent.embedding, topK: 3);

// 步骤4: 根据相似度选择策略
if (templates[0].similarity > 0.9f)
{
    // 直接使用模板
    return BuildFromTemplate(templates[0]);
}
else if (templates[0].similarity > 0.7f)
{
    // 模板+AI修改
    return await ModifyTemplate(templates[0], intent);
}
else
{
    // 多模板融合
    return await MergeTemplates(templates, intent);
}
```

---

## 四、解决"数据太大"问题

### 4.1 分层生成策略

**核心思想**: 不要让AI一次性生成所有方块，而是生成"设计规则"

```
传统方案（失败）:
AI输出: { tiles: [{x:0,y:0,type:1}, {x:0,y:1,type:1}, ...1700个] }
问题: 输出太长，AI会截断、出错

新方案（成功）:
AI输出: {
  "structure": {
    "frame": { "material": "Stone", "pattern": "rectangle" },
    "walls": { "material": "Marble Wall", "fill": "solid" },
    "floors": [
      { "y": 0, "material": "Gold", "pattern": "solid" },
      { "y": 10, "material": "Wood", "pattern": "checkered" }
    ]
  },
  "decorations": [
    { "type": "lantern", "pattern": "every_5_tiles", "material": "Pine Lantern" }
  ]
}
```

### 4.2 程序化生成

```csharp
/// <summary>
/// 根据设计规则生成实际方块数据
/// </summary>
public class ProceduralBuilder
{
    public List<TileData> GenerateFromRules(BuildingRules rules, int width, int height)
    {
        var tiles = new List<TileData>();
        
        // 1. 生成框架
        GenerateFrame(tiles, rules.structure.frame, width, height);
        
        // 2. 填充墙壁区域
        GenerateWalls(tiles, rules.structure.walls, width, height);
        
        // 3. 生成地板
        foreach (var floor in rules.structure.floors)
        {
            GenerateFloor(tiles, floor, width);
        }
        
        // 4. 放置装饰
        foreach (var decor in rules.decorations)
        {
            PlaceDecoration(tiles, decor, width, height);
        }
        
        return tiles;
    }
    
    private void GenerateFrame(List<TileData> tiles, FrameRule frame, int w, int h)
    {
        int tileId = GetTileId(frame.material);
        
        // 四边框架
        for (int x = 0; x < w; x++)
        {
            tiles.Add(new TileData { X = x, Y = 0, TypeId = tileId });
            tiles.Add(new TileData { X = x, Y = h - 1, TypeId = tileId });
        }
        for (int y = 0; y < h; y++)
        {
            tiles.Add(new TileData { X = 0, Y = y, TypeId = tileId });
            tiles.Add(new TileData { X = w - 1, Y = y, TypeId = tileId });
        }
    }
}
```

---

## 五、Agent工具设计

### 5.1 核心工具（5个）

```json
[
  {
    "name": "search_similar_buildings",
    "description": "检索与用户需求相似的建筑模板，返回模板摘要",
    "input_schema": {
      "type": "object",
      "properties": {
        "style": { "type": "string", "description": "风格: asian, medieval, fantasy, snow, desert" },
        "building_type": { "type": "string", "description": "类型: house, castle, tower, shop, temple" },
        "features": { "type": "array", "items": { "type": "string" }, "description": "特征标签" }
      }
    }
  },
  {
    "name": "get_building_template",
    "description": "获取指定建筑模板的完整信息，包括建造顺序和材料列表",
    "input_schema": {
      "type": "object",
      "properties": {
        "building_id": { "type": "string" }
      },
      "required": ["building_id"]
    }
  },
  {
    "name": "get_material_recommendation",
    "description": "根据风格获取推荐的材料组合",
    "input_schema": {
      "type": "object",
      "properties": {
        "style": { "type": "string" },
        "biome": { "type": "string", "description": "生物群落: forest, snow, desert, jungle, ocean" }
      }
    }
  },
  {
    "name": "validate_building_requirements",
    "description": "验证建筑是否满足NPC房屋要求",
    "input_schema": {
      "type": "object",
      "properties": {
        "width": { "type": "integer" },
        "height": { "type": "integer" },
        "has_light": { "type": "boolean" },
        "has_door": { "type": "boolean" },
        "has_table": { "type": "boolean" },
        "has_chair": { "type": "boolean" }
      }
    }
  },
  {
    "name": "generate_building_design",
    "description": "生成最终建筑设计JSON，结束Agent循环",
    "input_schema": {
      "type": "object",
      "properties": {
        "name": { "type": "string" },
        "width": { "type": "integer" },
        "height": { "type": "integer" },
        "template_id": { "type": "string", "description": "基于的模板ID" },
        "modifications": { "type": "object", "description": "对模板的修改" }
      }
    }
  }
]
```

### 5.2 Agent执行流程

```
用户: "建一个中式风格的三层小楼，要有灯笼装饰"

[轮次1]
AI: 调用 search_similar_buildings(style="asian", building_type="house")
工具返回: [
  { id: "20260602215014", similarity: 0.85, summary: "中式奇幻风格多层住宅，使用金块、大理石墙、松木灯笼" },
  { id: "20260602215015", similarity: 0.72, summary: "普通木屋" }
]

[轮次2]
AI: 调用 get_building_template(building_id="20260602215014")
工具返回: {
  dimensions: { width: 53, height: 32 },
  materials: { primary_tiles: [...], primary_walls: [...] },
  building_sequence: [
    { step: 1, action: "frame", materials: ["Stone"] },
    { step: 5, action: "lights", materials: ["Pine Lantern"] }
  ]
}

[轮次3]
AI: 分析用户需求与模板差异
- 用户要"三层"，模板是多层结构 ✓
- 用户要"灯笼"，模板有松木灯笼 ✓
- 用户要"小楼"，模板53x32可能太大
决定: 使用模板，缩小尺寸到30x20

AI: 调用 generate_building_design(
  name: "中式三层小楼",
  width: 30,
  height: 20,
  template_id: "20260602215014",
  modifications: { scale: 0.6, keep_style: true }
)

[结束]
返回: TEditSchDesign JSON
```

---

## 六、实现步骤

### Phase 1: 模板检索增强（1天）

1. **完善BuildingEntityBase**
   - 增加更多检索维度（类型、尺寸范围、复杂度）
   - 支持模糊匹配

2. **添加向量嵌入**
   - 为每个建筑生成text-embedding
   - 支持语义检索

### Phase 2: 程序化生成器（2天）

3. **实现BuildingRules数据结构**
   - 定义设计规则格式
   - 支持模式化生成（solid, checkered, bordered等）

4. **实现ProceduralBuilder**
   - 根据规则生成实际方块
   - 支持模板缩放、镜像

### Phase 3: Agent工具对接（1天）

5. **实现5个工具**
   - 封装现有知识库为工具
   - 添加参数验证

6. **更新System Prompt**
   - 引导AI使用工具而非直接生成
   - 强调"检索-修改"模式

### Phase 4: 测试优化（1天）

7. **测试用例**
   - 完全匹配的请求（直接使用模板）
   - 部分匹配的请求（模板修改）
   - 无匹配的请求（多模板融合或默认生成）

---

## 七、预期效果

### 对比

| 指标 | 当前方案 | 新方案 |
|------|----------|--------|
| 建筑质量 | 随机，通常不协调 | 基于真实建筑，协调美观 |
| 生成速度 | 10-30秒 | 3-10秒（检索快于生成） |
| 输出大小 | 可能截断 | 基于模板，不会截断 |
| 风格一致性 | 差 | 高（使用真实风格标签） |
| 可扩展性 | 难 | 易（添加新模板即可） |

---

## 八、关键代码示例

### 8.1 检索+修改 Agent

```csharp
public class TemplateBasedAgent
{
    private BuildingEntityBase _buildingKB;
    
    public async Task<TEditSchDesign> GenerateAsync(string userPrompt)
    {
        // 步骤1: AI分析用户意图
        var intent = await AnalyzeUserIntent(userPrompt);
        
        // 步骤2: 检索相似模板
        var templates = _buildingKB.SearchByStyle(intent.Style, topK: 3);
        
        if (templates.Count == 0)
        {
            // 没有匹配模板，使用默认生成
            return await GenerateFromScratch(intent);
        }
        
        var bestMatch = templates[0];
        
        // 步骤3: 判断是否需要修改
        if (IsPerfectMatch(bestMatch, intent))
        {
            // 直接使用模板
            return BuildFromTemplate(bestMatch);
        }
        else
        {
            // 基于模板修改
            return await ModifyTemplate(bestMatch, intent);
        }
    }
    
    private async Task<TEditSchDesign> ModifyTemplate(BuildingEntity template, BuildingIntent intent)
    {
        // 获取模板的详细数据
        var detail = _buildingKB.GetBuildingDetail(template.id);
        
        // 计算尺寸调整
        int newWidth = intent.Width ?? template.dimensions.width;
        int newHeight = intent.Height ?? template.dimensions.height;
        
        // 使用程序化生成器
        var builder = new ProceduralBuilder();
        
        // 从模板提取规则
        var rules = ExtractRulesFromTemplate(template, detail);
        
        // 应用用户修改
        ApplyModifications(rules, intent);
        
        // 生成实际方块
        var tiles = builder.GenerateFromRules(rules, newWidth, newHeight);
        
        return ConvertToTEditSch(tiles, newWidth, newHeight);
    }
}
```

### 8.2 设计规则数据结构

```csharp
public class BuildingRules
{
    public StructureRules structure { get; set; }
    public List<DecorationRule> decorations { get; set; }
    public MaterialPalette materials { get; set; }
}

public class StructureRules
{
    public FrameRule frame { get; set; }
    public WallRule walls { get; set; }
    public List<FloorRule> floors { get; set; }
    public List<RoomRule> rooms { get; set; }
}

public class FrameRule
{
    public string material { get; set; }
    public int thickness { get; set; } = 1;
    public string pattern { get; set; } = "rectangle"; // rectangle, arch, dome
}

public class DecorationRule
{
    public string type { get; set; } // lantern, torch, painting, statue
    public string material { get; set; }
    public string placement { get; set; } // corners, center, edges, every_n_tiles
    public int spacing { get; set; } = 5;
}

public class MaterialPalette
{
    public string primary_wall { get; set; }
    public string secondary_wall { get; set; }
    public string primary_tile { get; set; }
    public string accent_tile { get; set; }
    public string floor_tile { get; set; }
}
```

---

## 九、总结

**核心思想转变**:

- ❌ 让AI从零生成建筑 → ✅ 让AI检索并修改现有建筑模板
- ❌ AI直接输出方块坐标 → ✅ AI输出设计规则，程序生成方块

**优势**:

1. **质量保证**: 基于真实玩家设计的建筑模板
2. **输出可控**: 设计规则远小于方块列表
3. **风格一致**: 使用真实风格标签和材料组合
4. **易于扩展**: 添加新建筑模板即可支持更多风格

---

*文档版本: 3.0*
*更新日期: 2026-06-03*
*核心改动: 模板检索+分层生成架构*
