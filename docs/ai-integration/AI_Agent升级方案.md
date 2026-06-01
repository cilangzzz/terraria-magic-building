# AI Agent 建筑生成系统升级方案

## 一、核心问题：当前实现是伪Agent

### 1.1 真Agent vs 伪Agent

| 特性 | 伪Agent（当前实现） | 真Agent（目标实现） |
|------|---------------------|---------------------|
| **工具调用者** | 本地代码预调用 | AI模型自主决定 |
| **调用时机** | 请求前硬编码检索 | AI根据需求动态调用 |
| **交互轮数** | 1轮单次请求 | 多轮循环（最多5轮） |
| **AI自主性** | 无决策权 | 有完整决策权 |
| **知识获取** | 提示词硬编码 | 工具动态检索 |
| **适应性** | 固定流程 | 根据需求灵活调整 |

### 1.2 当前伪Agent流程（需改造）

```
用户: "建一个城堡"
  ↓
[本地代码预检索] ← 问题：AI没有参与决策
  ↓
BuildEnhancedPrompt() 硬编码:
  - kb.Tiles.SearchTiles("medieval") ← 固定风格，AI无法选择
  - 塞进提示词
  ↓
[单次API请求] ← 问题：无多轮交互
  ↓
返回JSON
```

### 1.3 真Agent流程（目标）

```
用户: "建一个城堡"
  ↓
[Agent启动 - 发送用户请求+工具定义]
  ↓
AI分析: "需要了解城堡风格..."
  ↓
[AI自主调用工具1] get_style_template(style="medieval", type="castle")
  ↓
[本地执行工具 → 返回tool_result]
  ↓
AI分析: "需要合适的方块..."
  ↓
[AI自主调用工具2] search_tiles(style="medieval", category="wall")
  ↓
[本地执行工具 → 返回tool_result]
  ↓
AI分析: "知识足够，生成设计..."
  ↓
[AI输出最终JSON] ← end_turn 或 generate_building 工具
```

---

## 二、真Agent架构设计

### 2.1 核心组件

```
┌─────────────────────────────────────────────────────────────────────┐
│                      True Agent Architecture                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐          │
│  │ 用户请求     │ → │ Agent Loop   │ → │ BuildingDesign│          │
│  │ "城堡"       │    │ (多轮循环)   │    │ (最终输出)   │          │
│  └──────────────┘    └──────┬───────┘    └──────────────┘          │
│                             │                                       │
│                    ┌────────┴────────┐                              │
│                    │  Messages Queue │                              │
│                    │  [user, assistant│                              │
│                    │   tool_use,      │                              │
│                    │   tool_result]   │                              │
│                    └────────┬────────┘                              │
│                             │                                       │
│         ┌───────────────────┼───────────────────┐                  │
│         ↓                   ↓                   ↓                  │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐           │
│  │ Tool:        │   │ Tool:        │   │ Tool:        │           │
│  │ search_tiles │   │ get_style    │   │ search_      │           │
│  │              │   │ _template    │   │ furniture    │           │
│  └──────┬───────┘   └──────┬───────┘   └──────┬───────┘           │
│         ↓                   ↓                   ↓                  │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐           │
│  │ TileKB       │   │ StyleKB      │   │ FurnitureKB  │           │
│  │ .Search()    │   │ .GetTemplate │   │ .Search()    │           │
│  └──────────────┘   └──────────────┘   └──────────────┘           │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 API协议（Anthropic Tool Use）

**请求格式：**
```json
{
  "model": "claude-sonnet-4-20250514",
  "max_tokens": 4096,
  "system": "你是泰拉瑞亚建筑设计Agent...",
  "messages": [
    {"role": "user", "content": "建一个中世纪城堡"}
  ],
  "tools": [
    {
      "name": "search_tiles",
      "description": "搜索适合指定风格的方块",
      "input_schema": {
        "type": "object",
        "properties": {
          "style": {"type": "string"},
          "category": {"type": "string"}
        },
        "required": ["style"]
      }
    }
  ]
}
```

**AI响应（tool_use）：**
```json
{
  "id": "msg_xxx",
  "content": [
    {
      "type": "tool_use",
      "id": "toolu_xxx",
      "name": "search_tiles",
      "input": {"style": "medieval", "category": "wall"}
    }
  ],
  "stop_reason": "tool_use"
}
```

**工具结果返回：**
```json
{
  "role": "user",
  "content": [
    {
      "type": "tool_result",
      "tool_use_id": "toolu_xxx",
      "content": "{\"tiles\": [...], \"count\": 15}"
    }
  ]
}
```

---

## 三、工具定义（精简版）

### 3.1 核心工具（4个）

```csharp
// 工具1：方块搜索
{
  "name": "search_tiles",
  "description": "搜索方块类型，返回ID、名称、属性。用于确定建筑材料。",
  "input_schema": {
    "type": "object",
    "properties": {
      "style": {"type": "string", "description": "风格: medieval, fantasy, natural, steampunk, asian, snow, desert, modern, dark"},
      "category": {"type": "string", "description": "类别: wall, floor, roof, decoration, furniture, light, door"},
      "biome": {"type": "string", "description": "生物群落: forest, desert, snow, jungle, ocean, underground, hallow"}
    },
    "required": ["style"]
  }
}

// 工具2：风格模板
{
  "name": "get_style_template",
  "description": "获取建筑风格模板，包含推荐方块、油漆方案、建筑规则。",
  "input_schema": {
    "type": "object",
    "properties": {
      "style": {"type": "string", "description": "风格名称"},
      "building_type": {"type": "string", "description": "建筑类型: house, castle, tower, shop, temple, workshop"}
    },
    "required": ["style"]
  }
}

// 工具3：家具搜索
{
  "name": "search_furniture",
  "description": "搜索家具及其NPC房屋功能要求。",
  "input_schema": {
    "type": "object",
    "properties": {
      "room_type": {"type": "string", "description": "房间类型: bedroom, workshop, shop, storage"},
      "npc_type": {"type": "string", "description": "目标NPC类型（可选）"}
    }
  }
}

// 工具4：油漆方案
{
  "name": "get_paint_scheme",
  "description": "获取推荐的颜色和阴影油漆方案。",
  "input_schema": {
    "type": "object",
    "properties": {
      "style": {"type": "string"},
      "theme": {"type": "string", "description": "主题: warm, cold, dark, bright"}
    }
  }
}
```

### 3.2 工具执行映射

```csharp
private ToolResult ExecuteTool(string name, JObject input)
{
    return name switch
    {
        "search_tiles" => SearchTiles(
            input["style"]?.ToString(),
            input["category"]?.ToString(),
            input["biome"]?.ToString()
        ),
        "get_style_template" => GetStyleTemplate(
            input["style"]?.ToString(),
            input["building_type"]?.ToString()
        ),
        "search_furniture" => SearchFurniture(
            input["room_type"]?.ToString(),
            input["npc_type"]?.ToString()
        ),
        "get_paint_scheme" => GetPaintScheme(
            input["style"]?.ToString(),
            input["theme"]?.ToString()
        ),
        _ => new ToolResult { IsError = true, Content = "未知工具" }
    };
}
```

---

## 四、Agent循环实现

### 4.1 核心循环逻辑

```csharp
/// <summary>
/// Agent主循环 - 真工具调用
/// </summary>
public async Task<BuildingDesign> GenerateBuildingAsync(
    string userPrompt,
    Action<string, int> progressCallback = null,  // (消息, 轮数)
    CancellationToken ct = default)
{
    KnowledgeBaseManager.Instance.Initialize();
    
    // 初始化消息队列
    var messages = new List<MessageItem>
    {
        new MessageItem { role = "user", content = userPrompt }
    };
    
    // 工具调用记录
    var toolCalls = new List<ToolCallRecord>();
    
    // 最大5轮循环
    for (int round = 1; round <= 5; round++)
    {
        progressCallback?.Invoke($"Agent轮次 {round}...", round);
        
        // 发送请求（带工具定义）
        var response = await SendAgentRequest(messages, ct);
        
        // 检查停止原因
        if (response.stop_reason == "end_turn" || 
            response.stop_reason == "stop_sequence")
        {
            // AI完成，提取最终JSON
            var textContent = ExtractTextContent(response.content);
            return ParseBuildingDesign(textContent, toolCalls);
        }
        
        if (response.stop_reason == "tool_use")
        {
            // AI调用工具
            var assistantMessage = new MessageItem
            {
                role = "assistant",
                content = response.content
            };
            messages.Add(assistantMessage);
            
            // 执行所有工具调用
            var toolResults = new List<ToolResultContent>();
            foreach (var content in response.content)
            {
                if (content.type == "tool_use")
                {
                    progressCallback?.Invoke($"调用工具: {content.name}", round);
                    
                    // 执行工具
                    var result = ExecuteTool(content.name, content.input as JObject);
                    
                    // 记录
                    toolCalls.Add(new ToolCallRecord
                    {
                        ToolName = content.name,
                        Input = content.input,
                        Output = result.Content,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });
                    
                    // 构建tool_result
                    toolResults.Add(new ToolResultContent
                    {
                        type = "tool_result",
                        tool_use_id = content.id,
                        content = result.Content,
                        is_error = result.IsError
                    });
                }
            }
            
            // 添加工具结果到消息队列
            messages.Add(new MessageItem
            {
                role = "user",
                content = toolResults
            });
            
            continue; // 继续下一轮
        }
        
        // max_tokens或其他停止
        break;
    }
    
    return null; // 超过最大轮数
}
```

### 4.2 消息结构

```csharp
// 消息项（支持多种content类型）
public class MessageItem
{
    public string role { get; set; }
    public object content { get; set; }  // string 或 content[]
}

// 工具结果内容
public class ToolResultContent
{
    public string type { get; set; } = "tool_result";
    public string tool_use_id { get; set; }
    public string content { get; set; }
    public bool is_error { get; set; } = false;
}

// Claude响应内容项
public class ClaudeContentItem
{
    public string type { get; set; }  // "text" or "tool_use"
    public string text { get; set; }  // for text
    public string id { get; set; }    // for tool_use
    public string name { get; set; }  // for tool_use
    public object input { get; set; } // for tool_use
}
```

---

## 五、系统提示词设计

### 5.1 Agent系统提示词

```csharp
private const string AGENT_SYSTEM_PROMPT = @"你是泰拉瑞亚建筑设计Agent。

## 工作流程
1. 理解用户需求，确定建筑风格和类型
2. 使用工具检索相关知识（方块、风格模板、家具、油漆）
3. 基于检索结果生成建筑设计JSON

## 工具使用规则
- 先调用 get_style_template 了解风格要求
- 再调用 search_tiles 获取合适方块
- 如需NPC房屋，调用 search_furniture
- 最后生成JSON（不使用工具，直接输出）

## 输出格式
生成JSON建筑设计，格式如下：
{
  ""name"": ""建筑名称"",
  ""width"": 10-30,
  ""height"": 8-20,
  ""style"": ""风格"",
  ""tiles"": [{""x"", ""y"", ""tile_id"", ""paint"", ""slope""}],
  ""walls"": [{""x"", ""y"", ""wall_id"", ""paint""}],
  ""furniture"": [{""x"", ""y"", ""tile_id"", ""direction""}],
  ""doors"": [{""x"", ""y"", ""tile_id""}],
  ""lightSources"": [{""x"", ""y"", ""tile_id""}]
}

## 重要规则
1. 使用工具返回的精确tile_id/wall_id
2. 应用推荐的paint方案增加层次
3. 尺寸控制在合理范围
4. 确保NPC房屋有必需家具（如需要）";
```

---

## 六、UI进度显示

### 6.1 更新UI回调

```csharp
// 当前UI显示
progressCallback?.Invoke("Agent启动...");

// 新UI显示（带轮次）
progressCallback?.Invoke($"[轮次1] 分析需求...", 1);
progressCallback?.Invoke($"[轮次1] 调用工具: get_style_template", 1);
progressCallback?.Invoke($"[轮次2] 调用工具: search_tiles", 2);
progressCallback?.Invoke($"[轮次3] 生成设计...", 3);
progressCallback?.Invoke($"完成（3轮，4次工具调用）", 0);
```

---

## 七、实现步骤

### Phase 1: 核心Agent重构（1天）

1. 重写 `AIAgentService.cs`
   - 实现消息队列管理
   - 实现工具调用循环
   - 实现tool_result返回

2. 定义数据结构
   - `MessageItem` 类
   - `ToolResultContent` 类
   - `ClaudeContentItem` 类

### Phase 2: 工具执行对接（0.5天）

3. 连接现有知识库
   - `TileKnowledgeBase.Search()`
   - `StyleTemplateBase.GetTemplate()`
   - `FurnitureRuleBase.Search()`

4. 添加油漆方案检索

### Phase 3: UI更新（0.5天）

5. 更新 `AIBuildingUI.cs`
   - 显示轮次进度
   - 显示工具调用名称
   - 显示最终统计

---

## 八、预期效果

### 对比

| 指标 | 伪Agent（当前） | 真Agent（改造后） |
|------|-----------------|-------------------|
| 工具调用 | 0次（本地预执行） | 2-4次（AI自主） |
| API轮次 | 1轮 | 3-5轮 |
| AI决策权 | 无 | 完整 |
| 风格适应 | 固定medieval | AI自主选择 |
| 油漆应用 | 无 | AI自主应用 |
| 响应时间 | ~3秒 | ~10-15秒 |
| 建筑质量 | 简陋 | 精细 |

### 优势

1. **AI自主决策** - AI根据需求动态选择风格、方块、油漆
2. **知识精确** - 使用工具返回的精确ID，而非AI猜测
3. **流程透明** - UI显示每一步工具调用
4. **可扩展** - 新增工具只需添加定义和执行函数

---

## 九、API兼容性

### 支持的API

| API | 工具调用支持 | 备注 |
|-----|-------------|------|
| Claude | ✅ 原生支持 | 最佳选择 |
| DeepSeek | ✅ Anthropic兼容端点 | 使用 `/anthropic/v1/messages` |
| DashScope | ✅ Anthropic兼容 | 需配置自定义端点 |
| OpenAI | ⚠️ 格式不同 | 需转换tools→functions格式 |

### DeepSeek配置

```csharp
// DeepSeek Anthropic兼容端点
_apiEndpoint = "https://api.deepseek.com/anthropic/v1/messages";
_httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
_httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
```

---

*文档版本: 2.0*
*更新日期: 2026-06-01*
*核心改动: 明确真伪Agent区别，重新设计架构*