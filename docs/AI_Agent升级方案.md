# AI Agent 建筑生成系统升级方案

## 一、当前问题分析

### 1.1 现有API模式的局限性

当前项目使用简单的API调用模式，存在以下问题：

| 问题 | 描述 | 影响 |
|------|------|------|
| **方块类型有限** | AI只知道提示词中列出的少数方块类型 | 生成结果单调，如"木屋" |
| **缺乏设计风格知识** | AI不了解泰拉瑞亚的建筑美学和风格体系 | 建筑缺乏特色和细节 |
| **油漆/阴影缺失** | AI不知道油漆系统和阴影效果 | 建筑颜色单调，缺乏层次 |
| **家具放置不精确** | 家具类型和放置规则不明确 | 房屋可能不符合NPC要求 |
| **Token消耗大** | 每次请求都传输完整提示词 | 成本高，响应慢 |
| **无上下文记忆** | 每次请求独立，无法参考之前的建筑 | 无法迭代改进 |

### 1.2 现有系统架构

```
用户输入 → API请求 → JSON解析 → 直接放置
           ↓
    硬编码提示词（有限方块类型列表）
```

---

## 二、Agent模式架构设计

### 2.1 核心概念

Agent模式与API模式的关键区别：

| 特性 | API模式 | Agent模式 |
|------|---------|-----------|
| **知识获取** | 硬编码提示词 | 动态检索知识库 |
| **工具调用** | 无 | 多工具协作 |
| **决策过程** | 单次生成 | 多步骤规划 |
| **输出控制** | 依赖AI自律 | 强制结构化Schema |
| **上下文** | 无 | RAG向量检索 |

### 2.2 系统架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                    AI Agent 建筑生成系统                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐         │
│  │  用户请求   │ → │  Agent核心   │ → │  结构化输出  │         │
│  │  "城堡"     │    │  规划引擎   │    │  JSON Schema│         │
│  └─────────────┘    └──────┬──────┘    └─────────────┘         │
│                            │                                    │
│         ┌──────────────────┼──────────────────┐                │
│         ↓                  ↓                  ↓                │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐         │
│  │ 方块检索    │    │ 风格检索    │    │ 家具检索    │         │
│  │ Tool        │    │ Tool        │    │ Tool        │         │
│  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘         │
│         ↓                  ↓                  ↓                │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐         │
│  │ 方块知识库  │    │ 风格知识库  │    │ 家具知识库  │         │
│  │ (RAG向量库) │    │ (RAG向量库) │    │ (规则库)    │         │
│  └─────────────┘    └─────────────┘    └─────────────┘         │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                    油漆/阴影/装饰知识库                       ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

---

## 三、核心组件设计

### 3.1 工具定义（Tools）

Agent需要调用的工具列表：

```csharp
/// <summary>
/// Agent工具定义
/// </summary>
public class AgentTools
{
    // 工具1：方块检索
    public static readonly Tool SearchTiles = new Tool
    {
        name = "search_tiles",
        description = "检索适合建筑风格的方块类型，返回方块ID和属性",
        input_schema = new
        {
            type = "object",
            properties = new
            {
                style = new { type = "string", description = "建筑风格: medieval, modern, natural, fantasy, steampunk" },
                material_type = new { type = "string", description = "材料类别: wall, floor, roof, decoration" },
                biome = new { type = "string", description = "生物群落: forest, desert, snow, jungle, ocean" }
            },
            required = new[] { "style" }
        }
    };

    // 工具2：风格模板检索
    public static readonly Tool GetStyleTemplate = new Tool
    {
        name = "get_style_template",
        description = "获取特定建筑风格的模板和设计规则",
        input_schema = new
        {
            type = "object",
            properties = new
            {
                style_name = new { type = "string" },
                building_type = new { type = "string", description = "house, castle, tower, shop, temple" }
            }
        }
    };

    // 工具3：家具检索
    public static readonly Tool SearchFurniture = new Tool
    {
        name = "search_furniture",
        description = "检索家具及其放置规则，包括NPC房屋要求",
        input_schema = new
        {
            type = "object",
            properties = new
            {
                room_type = new { type = "string", description = "bedroom, workshop, shop, storage" },
                npc_type = new { type = "string", description = "可选的NPC类型，如merchant, nurse" }
            }
        }
    };

    // 工具4：油漆/阴影检索
    public static readonly Tool GetPaintScheme = new Tool
    {
        name = "get_paint_scheme",
        description = "获取推荐的颜色方案和阴影效果",
        input_schema = new
        {
            type = "object",
            properties = new
            {
                theme = new { type = "string" },
                lighting_condition = new { type = "string", description = "daylight, underground, night" }
            }
        }
    };

    // 工具5：建筑生成（最终输出）
    public static readonly Tool GenerateBuilding = new Tool
    {
        name = "generate_building",
        description = "生成最终建筑设计JSON",
        input_schema = BuildingDesignSchema.Get()
    };
}
```

### 3.2 结构化输出Schema

```csharp
/// <summary>
/// 建筑设计JSON Schema - 强制AI输出符合规范
/// </summary>
public class BuildingDesignSchema
{
    public static object Get()
    {
        return new
        {
            type = "object",
            properties = new
            {
                name = new { type = "string" },
                description = new { type = "string" },
                width = new { type = "integer", minimum = 5, maximum = 50 },
                height = new { type = "integer", minimum = 5, maximum = 30 },
                style = new { type = "string" },
                biome_match = new { type = "string" },
                
                // 方块层
                tiles = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            x = new { type = "integer" },
                            y = new { type = "integer" },
                            type = new { type = "string" },
                            tile_id = new { type = "integer" },
                            style = new { type = "integer" },
                            paint = new { type = "integer", description = "油漆颜色ID 0-31" },
                            slope = new { type = "integer", description = "斜坡类型 0-4" }
                        },
                        required = new[] { "x", "y", "type", "tile_id" }
                    }
                },
                
                // 墙壁层
                walls = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            x = new { type = "integer" },
                            y = new { type = "integer" },
                            type = new { type = "string" },
                            wall_id = new { type = "integer" },
                            paint = new { type = "integer" }
                        },
                        required = new[] { "x", "y", "type", "wall_id" }
                    }
                },
                
                // 家具层
                furniture = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            x = new { type = "integer" },
                            y = new { type = "integer" },
                            type = new { type = "string" },
                            tile_id = new { type = "integer" },
                            direction = new { type = "integer" }
                        },
                        required = new[] { "x", "y", "type", "tile_id" }
                    }
                },
                
                // 光源层
                light_sources = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            x = new { type = "integer" },
                            y = new { type = "integer" },
                            type = new { type = "string" },
                            tile_id = new { type = "integer" },
                            wire = new { type = "boolean", description = "是否连接电路" }
                        }
                    }
                },
                
                // 门
                doors = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            x = new { type = "integer" },
                            y = new { type = "integer" },
                            type = new { type = "string" },
                            direction = new { type = "integer" }
                        }
                    }
                },
                
                // NPC suitability
                npc suitability = new
                {
                    type = "object",
                    properties = new
                    {
                        is_valid_house = new { type = "boolean" },
                        suitable_npcs = new { type = "array", items = new { type = "string" } },
                        missing_requirements = new { type = "array", items = new { type = "string" } }
                    }
                }
            },
            required = new[] { "name", "width", "height", "tiles", "walls" }
        };
    }
}
```

---

## 四、知识库设计（RAG系统）

### 4.1 方块知识库

创建方块数据文件 `Data/TileKnowledgeBase.json`：

```json
{
  "tiles": [
    {
      "id": 1,
      "name": "Stone",
      "display_name": "石头",
      "category": "basic",
      "styles": ["medieval", "natural", "underground"],
      "paint_compatible": true,
      "slope_compatible": true,
      "biome_match": ["forest", "underground", "mountain"],
      "hardness": 50,
      "light_emission": 0,
      "description": "基础建筑材料，适合墙壁和地基"
    },
    {
      "id": 4,
      "name": "GrayBrick",
      "display_name": "灰砖",
      "category": "brick",
      "styles": ["medieval", "castle", "urban"],
      "paint_compatible": true,
      "slope_compatible": false,
      "biome_match": ["forest", "desert"],
      "hardness": 50,
      "description": "经典砖块，适合城堡和城镇建筑"
    },
    {
      "id": 17,
      "name": "WorkBench",
      "display_name": "工作台",
      "category": "furniture",
      "styles": ["any"],
      "width": 2,
      "height": 1,
      "npc_requirement": ["crafting"],
      "description": "基础制作站，也是NPC房屋需要的平坦表面"
    }
  ],
  "paints": [
    {"id": 0, "name": "None", "color": "default"},
    {"id": 1, "name": "Red", "color": "#FF0000"},
    {"id": 2, "name": "Orange", "color": "#FF8000"},
    {"id": 3, "name": "Yellow", "color": "#FFFF00"},
    {"id": 28, "name": "Shadow", "color": "dark", "effect": "depth"},
    {"id": 29, "name": "Negative", "color": "inverse", "effect": "special"}
  ],
  "slopes": [
    {"id": 0, "name": "Solid", "description": "完整方块"},
    {"id": 1, "name": "HalfBlock", "description": "半砖"},
    {"id": 2, "name": "SlopeDownRight", "description": "右上斜坡"},
    {"id": 3, "name": "SlopeDownLeft", "description": "左上斜坡"},
    {"id": 4, "name": "SlopeUpRight", "description": "右下斜坡"},
    {"id": 5, "name": "SlopeUpLeft", "description": "左下斜坡"}
  ]
}
```

### 4.2 风格模板库

创建风格模板文件 `Data/StyleTemplates.json`：

```json
{
  "styles": {
    "medieval": {
      "name": "中世纪风格",
      "primary_tiles": ["GrayBrick", "StoneSlab", "Stone", "Wood"],
      "primary_walls": ["GrayBrickWall", "StoneWall", "WoodWall"],
      "accent_tiles": ["GoldBrick", "IronBrick"],
      "roof_style": "triangular",
      "roof_tiles": ["Wood", "GrayBrick"],
      "furniture_style": ["WoodenTable", "WoodenChair", "Torch"],
      "paint_scheme": {
        "primary": [0, 28],
        "accent": [1, 3],
        "shadow": [28]
      },
      "architectural_rules": [
        "使用灰砖作为主要墙壁材料",
        "添加木质屋顶和地板",
        "使用阴影油漆增加层次",
        "放置火把作为光源"
      ]
    },
    "fantasy": {
      "name": "奇幻风格",
      "primary_tiles": ["Pearlstone", "Glass", "GoldBrick"],
      "primary_walls": ["PearlstoneWall", "GlassWall"],
      "accent_tiles": ["DiamondGemspark", "RubyGemspark"],
      "roof_style": "dome",
      "furniture_style": ["CrystalTable", "MagicLantern"],
      "paint_scheme": {
        "primary": [23, 25],
        "accent": [12, 15],
        "glow": true
      }
    },
    "steampunk": {
      "name": "蒸汽朋克风格",
      "primary_tiles": ["CopperBrick", "IronBrick", "GearPlatform"],
      "primary_walls": ["CopperBrickWall", "IronBrickWall"],
      "accent_tiles": ["Cog", "SteamPipe"],
      "furniture_style": ["IndustrialWorkBench", "GearClock"],
      "wire_required": true
    }
  }
}
```

### 4.3 家具规则库

创建家具规则文件 `Data/FurnitureRules.json`：

```json
{
  "furniture": {
    "WorkBench": {
      "tile_id": 17,
      "width": 2,
      "height": 1,
      "placement_rule": "需要前方有至少2格空地",
      "npc_function": ["flat_surface"],
      "style_variants": [0, 1, 2]
    },
    "Table": {
      "tile_id": 87,
      "width": 3,
      "height": 1,
      "npc_function": ["flat_surface"],
      "placement_rule": "需要地板支撑"
    },
    "Chair": {
      "tile_id": 88,
      "width": 1,
      "height": 2,
      "npc_function": ["comfort"],
      "placement_rule": "需要前方有空地供NPC坐下",
      "direction": [0, 1]
    },
    "Bed": {
      "tile_id": 89,
      "width": 4,
      "height": 2,
      "npc_function": ["comfort", "spawn_point"],
      "placement_rule": "需要5x4的空间，且不能有其他床太近"
    },
    "Torch": {
      "tile_id": 4,
      "width": 1,
      "height": 1,
      "npc_function": ["light_source"],
      "placement_rule": "可贴墙或贴地",
      "light_radius": 10
    },
    "Chest": {
      "tile_id": 21,
      "width": 2,
      "height": 1,
      "placement_rule": "需要下方有实心支撑",
      "storage_slots": 40
    }
  },
  "npc_requirements": {
    "valid_house": {
      "minimum_tiles": 60,
      "required_furniture": ["light_source", "flat_surface", "comfort"],
      "wall_required": true,
      "door_required": true
    },
    "merchant": {
      "extra_requirement": "50+ silver coins in inventory"
    },
    "nurse": {
      "extra_requirement": "100+ max HP"
    },
    "gunsmith": {
      "extra_requirement": "gun or bullets in inventory"
    }
  }
}
```

---

## 五、Agent服务实现

### 5.1 核心Agent类

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace trab.Core
{
    /// <summary>
    /// AI Agent建筑生成服务 - 支持工具调用和结构化输出
    /// </summary>
    public class AIAgentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiEndpoint;
        private readonly AIServiceType _serviceType;
        private readonly string _modelName;
        
        // 知识库实例
        private readonly TileKnowledgeBase _tileKB;
        private readonly StyleTemplateBase _styleKB;
        private readonly FurnitureRuleBase _furnitureKB;

        public AIAgentService(string apiKey, AIServiceType serviceType, string modelName = "claude-sonnet-4-20250514")
        {
            _apiKey = apiKey;
            _serviceType = serviceType;
            _modelName = modelName;
            
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(120);
            
            // 加载知识库
            _tileKB = new TileKnowledgeBase();
            _styleKB = new StyleTemplateBase();
            _furnitureKB = new FurnitureRuleBase();
            
            ConfigureApiClient();
        }

        private void ConfigureApiClient()
        {
            if (_serviceType == AIServiceType.Claude || _serviceType == AIServiceType.DeepSeek)
            {
                _apiEndpoint = _serviceType == AIServiceType.Claude 
                    ? "https://api.anthropic.com/v1/messages"
                    : "https://api.deepseek.com/anthropic/v1/messages";
                    
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            }
            else
            {
                _apiEndpoint = "https://api.openai.com/v1/chat/completions";
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        /// <summary>
        /// Agent主入口 - 处理建筑请求
        /// </summary>
        public async Task<BuildingDesign> GenerateBuildingAsync(
            string userPrompt, 
            int startX, 
            int startY, 
            CancellationToken ct = default)
        {
            // 第一轮：理解需求并调用工具检索知识
            var toolResults = await RunAgentLoop(userPrompt, ct);
            
            // 第二轮：基于检索结果生成最终设计
            var design = await GenerateFinalDesign(userPrompt, toolResults, ct);
            
            return design;
        }

        /// <summary>
        /// Agent循环 - 工具调用
        /// </summary>
        private async Task<List<ToolResult>> RunAgentLoop(string prompt, CancellationToken ct)
        {
            var toolResults = new List<ToolResult>();
            int maxIterations = 5;
            
            // 定义可用工具
            var tools = DefineTools();
            
            // 初始请求
            var messages = new List<object>
            {
                new { role = "user", content = prompt }
            };

            for (int i = 0; i < maxIterations; i++)
            {
                var requestBody = BuildRequestBody(messages, tools);
                var response = await SendRequestAsync(requestBody, ct);
                
                // 检查是否需要调用工具
                if (response.stop_reason == "tool_use")
                {
                    foreach (var content in response.content)
                    {
                        if (content.type == "tool_use")
                        {
                            // 执行工具
                            var result = ExecuteTool(content.name, content.input);
                            toolResults.Add(result);
                            
                            // 将工具结果添加到消息
                            messages.Add(new { role = "assistant", content = response.content });
                            messages.Add(new
                            {
                                role = "user",
                                content = new[]
                                {
                                    new
                                    {
                                        type = "tool_result",
                                        tool_use_id = content.id,
                                        content = JsonConvert.SerializeObject(result)
                                    }
                                }
                            });
                        }
                    }
                }
                else
                {
                    // Agent完成工具调用
                    break;
                }
            }
            
            return toolResults;
        }

        /// <summary>
        /// 定义Agent可用工具
        /// </summary>
        private List<Tool> DefineTools()
        {
            return new List<Tool>
            {
                new Tool
                {
                    name = "search_tiles",
                    description = "搜索适合指定风格的方块类型。返回方块ID、名称和属性。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            style = new { type = "string", description = "建筑风格" },
                            category = new { type = "string", description = "wall, floor, decoration" },
                            biome = new { type = "string", description = "生物群落匹配" }
                        },
                        required = new[] { "style" }
                    }
                },
                new Tool
                {
                    name = "get_style_template",
                    description = "获取建筑风格模板，包含推荐方块、油漆方案和建筑规则。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            style_name = new { type = "string", enum = new[] { "medieval", "fantasy", "steampunk", "natural", "modern" } },
                            building_type = new { type = "string", enum = new[] { "house", "castle", "tower", "shop", "temple" } }
                        },
                        required = new[] { "style_name" }
                    }
                },
                new Tool
                {
                    name = "search_furniture",
                    description = "搜索家具及其放置规则，返回尺寸、NPC功能和放置要求。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            room_type = new { type = "string" },
                            npc_type = new { type = "string", description = "可选NPC类型" }
                        }
                    }
                },
                new Tool
                {
                    name = "get_paint_scheme",
                    description = "获取推荐的油漆颜色方案，包括主色、强调色和阴影。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            style = new { type = "string" },
                            biome = new { type = "string" }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 执行工具调用
        /// </summary>
        private ToolResult ExecuteTool(string toolName, JObject input)
        {
            switch (toolName)
            {
                case "search_tiles":
                    return SearchTiles(
                        input["style"]?.ToString(),
                        input["category"]?.ToString(),
                        input["biome"]?.ToString()
                    );
                    
                case "get_style_template":
                    return GetStyleTemplate(
                        input["style_name"]?.ToString(),
                        input["building_type"]?.ToString()
                    );
                    
                case "search_furniture":
                    return SearchFurniture(
                        input["room_type"]?.ToString(),
                        input["npc_type"]?.ToString()
                    );
                    
                case "get_paint_scheme":
                    return GetPaintScheme(
                        input["style"]?.ToString(),
                        input["biome"]?.ToString()
                    );
                    
                default:
                    return new ToolResult { success = false, error = "未知工具" };
            }
        }

        /// <summary>
        /// 搜索方块
        /// </summary>
        private ToolResult SearchTiles(string style, string category, string biome)
        {
            var matchingTiles = _tileKB.Search(style, category, biome);
            
            return new ToolResult
            {
                success = true,
                tool_name = "search_tiles",
                data = new
                {
                    tiles = matchingTiles,
                    total_count = matchingTiles.Count,
                    search_criteria = new { style, category, biome }
                }
            };
        }

        /// <summary>
        /// 获取风格模板
        /// </summary>
        private ToolResult GetStyleTemplate(string styleName, string buildingType)
        {
            var template = _styleKB.GetTemplate(styleName, buildingType);
            
            return new ToolResult
            {
                success = true,
                tool_name = "get_style_template",
                data = template
            };
        }

        /// <summary>
        /// 搜索家具
        /// </summary>
        private ToolResult SearchFurniture(string roomType, string npcType)
        {
            var furniture = _furnitureKB.Search(roomType, npcType);
            
            return new ToolResult
            {
                success = true,
                tool_name = "search_furniture",
                data = new
                {
                    furniture = furniture,
                    npc_requirements = _furnitureKB.GetNpcRequirements(npcType)
                }
            };
        }

        /// <summary>
        /// 获取油漆方案
        /// </summary>
        private ToolResult GetPaintScheme(string style, string biome)
        {
            var scheme = _styleKB.GetPaintScheme(style, biome);
            
            return new ToolResult
            {
                success = true,
                tool_name = "get_paint_scheme",
                data = scheme
            };
        }

        /// <summary>
        /// 生成最终建筑设计
        /// </summary>
        private async Task<BuildingDesign> GenerateFinalDesign(
            string prompt, 
            List<ToolResult> toolResults, 
            CancellationToken ct)
        {
            // 构建包含所有知识的设计请求
            var knowledgeContext = BuildKnowledgeContext(toolResults);
            
            var finalRequest = new
            {
                model = _modelName,
                max_tokens = 8192,
                system = @"你是泰拉瑞亚建筑设计Agent。基于检索的知识生成建筑设计JSON。

重要规则：
1. 使用工具返回的具体tile_id和wall_id
2. 遵循风格模板的建筑规则
3. 确保家具放置符合NPC房屋要求
4. 应用推荐的油漆方案增加层次
5. 输出必须符合JSON Schema规范",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $@"
用户请求: {prompt}

检索到的知识：
{knowledgeContext}

请生成完整的建筑设计JSON，包含：
- 精确的方块类型和ID
- 油漆颜色ID
- 家具及其放置位置
- 确保符合NPC房屋要求（如有）
"
                    }
                }
            };
            
            var response = await SendRequestAsync(finalRequest, ct);
            
            // 解析最终JSON
            var content = response.content[0].text;
            return ParseBuildingDesign(content);
        }

        private string BuildKnowledgeContext(List<ToolResult> results)
        {
            var sb = new StringBuilder();
            foreach (var result in results)
            {
                sb.AppendLine($"【{result.tool_name}】");
                sb.AppendLine(JsonConvert.SerializeObject(result.data, Formatting.Indented));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private BuildingDesign ParseBuildingDesign(string json)
        {
            try
            {
                // 提取JSON
                string extracted = AIApiService.ExtractJsonFromResponse(json) ?? json;
                return JsonConvert.DeserializeObject<BuildingDesign>(extracted);
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"解析建筑设计失败: {ex.Message}");
                return null;
            }
        }

        private async Task<ClaudeResponse> SendRequestAsync(object requestBody, CancellationToken ct)
        {
            string json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(_apiEndpoint, content, ct);
            response.EnsureSuccessStatusCode();
            
            string responseJson = await response.Content.ReadAsStringAsync(ct);
            return JsonConvert.DeserializeObject<ClaudeResponse>(responseJson);
        }
    }

    /// <summary>
    /// 工具定义
    /// </summary>
    public class Tool
    {
        public string name { get; set; }
        public string description { get; set; }
        public object input_schema { get; set; }
    }

    /// <summary>
    /// 工具执行结果
    /// </summary>
    public class ToolResult
    {
        public bool success { get; set; }
        public string tool_name { get; set; }
        public object data { get; set; }
        public string error { get; set; }
    }
}
```

### 5.2 知识库类实现

```csharp
/// <summary>
/// 方块知识库 - 支持RAG检索
/// </summary>
public class TileKnowledgeBase
{
    private List<TileInfo> _tiles;
    private List<PaintInfo> _paints;
    private List<SlopeInfo> _slopes;

    public TileKnowledgeBase()
    {
        LoadFromJson();
    }

    private void LoadFromJson()
    {
        string path = Path.Combine(ModContent.GetInstance<trab>().ModPath, "Data", "TileKnowledgeBase.json");
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<TileKnowledgeData>(json);
            _tiles = data.tiles;
            _paints = data.paints;
            _slopes = data.slopes;
        }
        else
        {
            // 使用默认数据
            InitDefaultData();
        }
    }

    /// <summary>
    /// 搜索匹配的方块
    /// </summary>
    public List<TileInfo> Search(string style, string category, string biome)
    {
        return _tiles.Where(t =>
            (style == null || t.styles.Contains(style)) &&
            (category == null || t.category == category) &&
            (biome == null || t.biome_match.Contains(biome))
        ).ToList();
    }

    /// <summary>
    /// 获取油漆信息
    /// </summary>
    public PaintInfo GetPaint(int id) => _paints.FirstOrDefault(p => p.id == id);

    private void InitDefaultData()
    {
        _tiles = new List<TileInfo>
        {
            new TileInfo { id = 1, name = "Stone", styles = new[] {"medieval", "natural"}, biome_match = new[] {"forest", "underground"} },
            new TileInfo { id = 4, name = "GrayBrick", styles = new[] {"medieval", "castle"}, biome_match = new[] {"forest"} },
            new TileInfo { id = 5, name = "Wood", styles = new[] {"natural", "medieval"}, biome_match = new[] {"forest"} },
            // ... 更多方块
        };
        
        _paints = new List<PaintInfo>
        {
            new PaintInfo { id = 0, name = "None" },
            new PaintInfo { id = 1, name = "Red", color = "#FF0000" },
            new PaintInfo { id = 28, name = "Shadow", effect = "depth" },
            // ... 更多油漆
        };
    }
}

public class TileInfo
{
    public int id { get; set; }
    public string name { get; set; }
    public string display_name { get; set; }
    public string category { get; set; }
    public string[] styles { get; set; }
    public string[] biome_match { get; set; }
    public bool paint_compatible { get; set; }
    public bool slope_compatible { get; set; }
    public string description { get; set; }
}

public class PaintInfo
{
    public int id { get; set; }
    public string name { get; set; }
    public string color { get; set; }
    public string effect { get; set; }
}
```

---

## 六、增强建筑执行器

### 6.1 支持油漆和斜坡

```csharp
/// <summary>
/// 增强版建筑执行器 - 支持油漆、斜坡、阴影
/// </summary>
public class EnhancedBuildingExecutor : BuildingExecutor
{
    public EnhancedBuildingExecutor(Mod mod) : base(mod) { }

    /// <summary>
    /// 放置带油漆的方块
    /// </summary>
    public void PlaceTileWithPaint(int x, int y, ushort type, int paintId, int slope = 0)
    {
        WorldGen.PlaceTile(x, y, type);
        
        if (paintId > 0)
        {
            Main.tile[x, y].TileColor = (byte)paintId;
        }
        
        if (slope > 0)
        {
            Main.tile[x, y].Slope = (byte)slope;
        }
    }

    /// <summary>
    /// 放置带油漆的墙壁
    /// </summary>
    public void PlaceWallWithPaint(int x, int y, ushort type, int paintId)
    {
        WorldGen.PlaceWall(x, y, type);
        
        if (paintId > 0)
        {
            Main.tile[x, y].WallColor = (byte)paintId;
        }
    }

    /// <summary>
    /// 应用阴影效果到区域
    /// </summary>
    public void ApplyShadowEffect(int startX, int startY, int width, int height)
    {
        // 边缘阴影
        for (int x = startX; x < startX + width; x++)
        {
            // 底部边缘 - 深阴影
            if (WorldGen.InWorld(x, startY + height))
            {
                var tile = Main.tile[x, startY + height];
                if (tile.HasTile)
                    tile.TileColor = 28; // Shadow paint
            }
            
            // 内部层次 - 浅阴影
            for (int y = startY + 1; y < startY + height - 1; y++)
            {
                if (WorldGen.InWorld(x, y))
                {
                    // 计算距离边缘的距离
                    int distFromEdge = Math.Min(
                        Math.Min(x - startX, startX + width - x),
                        Math.Min(y - startY, startY + height - y)
                    );
                    
                    if (distFromEdge == 1 && Main.tile[x, y].HasTile)
                    {
                        // 内边缘使用较浅阴影
                        Main.tile[x, y].TileColor = 29; // Negative/ShadowLight
                    }
                }
            }
        }
    }

    /// <summary>
    /// 验证NPC房屋
    /// </summary>
    public bool ValidateNpcHouse(int startX, int startY, int width, int height)
    {
        bool hasLight = false;
        bool hasFlatSurface = false;
        bool hasComfort = false;
        bool hasDoor = false;
        int tileCount = 0;

        // 扫描区域
        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                if (!WorldGen.InWorld(x, y)) continue;
                
                var tile = Main.tile[x, y];
                
                if (!tile.HasTile && tile.WallType > 0)
                    tileCount++;
                
                if (tile.HasTile)
                {
                    ushort type = tile.TileType;
                    
                    // 检测光源
                    if (TileID.Sets.RoomNeeds.CountsAsLightSource.Contains(type))
                        hasLight = true;
                    
                    // 检测平坦表面
                    if (TileID.Sets.RoomNeeds.CountsAsTable.Contains(type))
                        hasFlatSurface = true;
                    
                    // 检测舒适物品
                    if (TileID.Sets.RoomNeeds.CountsAsChair.Contains(type))
                        hasComfort = true;
                    
                    // 检测门
                    if (TileID.Sets.RoomNeeds.CountsAsDoor.Contains(type))
                        hasDoor = true;
                }
            }
        }

        return tileCount >= 60 && hasLight && hasFlatSurface && hasComfort && hasDoor;
    }
}
```

---

## 七、工作流程对比

### 7.1 API模式流程（现有）

```
用户: "建一个城堡"
  ↓
[单一API调用]
  ↓
AI凭记忆生成JSON（方块类型有限）
  ↓
放置方块（无油漆、无阴影）
  ↓
结果: 简陋木屋风格建筑
```

### 7.2 Agent模式流程（升级后）

```
用户: "建一个中世纪城堡"
  ↓
[Agent启动]
  ↓
┌─────────────────────────────┐
│ Tool Call 1: get_style_template │
│   → 返回: medieval模板          │
│   → 主方块: GrayBrick, StoneSlab │
│   → 油漆方案: Shadow+White       │
│   → 屋顶样式: triangular         │
└─────────────────────────────┘
  ↓
┌─────────────────────────────┐
│ Tool Call 2: search_tiles    │
│   → 返回: 20种匹配方块          │
│   → 包含ID、属性、paint兼容性    │
└─────────────────────────────┘
  ↓
┌─────────────────────────────┐
│ Tool Call 3: search_furniture │
│   → 返回: 家具类型+尺寸          │
│   → NPC房屋要求                 │
└─────────────────────────────┘
  ↓
┌─────────────────────────────┐
│ Tool Call 4: get_paint_scheme │
│   → 返回: 颜色方案               │
│   → 阴影建议                    │
└─────────────────────────────┘
  ↓
[基于知识生成最终JSON]
  ↓
放置方块 + 油漆 + 阴影 + 家具
  ↓
验证NPC房屋有效性
  ↓
结果: 精美的中世纪城堡
      - 灰砖墙壁带阴影效果
      - 木质三角屋顶
      - 符合NPC居住要求
      - 包含家具和光源
```

---

## 八、实现步骤

### Phase 1: 基础改造（预计2-3天）

1. **创建知识库JSON文件**
   - `Data/TileKnowledgeBase.json`
   - `Data/StyleTemplates.json`
   - `Data/FurnitureRules.json`

2. **实现知识库加载类**
   - `TileKnowledgeBase.cs`
   - `StyleTemplateBase.cs`
   - `FurnitureRuleBase.cs`

3. **更新数据结构**
   - 在 `BuildingDesign.cs` 中添加 `paint`、`slope`、`tile_id` 字段

### Phase 2: Agent服务实现（预计3-4天）

4. **实现 `AIAgentService.cs`**
   - 工具定义
   - 工具调用循环
   - 知识检索方法

5. **更新 `AIApiService.cs`**
   - 支持工具调用格式
   - 支持 `tool_use` 响应解析

### Phase 3: 执行器增强（预计2天）

6. **实现 `EnhancedBuildingExecutor.cs`**
   - 油漆放置
   - 斜坡支持
   - 阴影效果
   - NPC房屋验证

### Phase 4: UI更新（预计1天）

7. **更新UI显示检索过程**
   - 显示工具调用状态
   - 显示知识检索结果
   - 显示验证结果

---

## 九、预期效果对比

| 特性 | API模式 | Agent模式 |
|------|---------|-----------|
| **方块种类** | 10-15种 | 100+种（按需检索） |
| **风格模板** | 无（AI凭记忆） | 5+预设模板 |
| **油漆效果** | 无 | 完整支持31种油漆 |
| **阴影层次** | 无 | 自动应用 |
| **斜坡方块** | 无 | 支持6种斜坡 |
| **家具放置** | 简单 | 符合NPC要求 |
| **NPC房屋验证** | 无 | 自动验证 |
| **Token效率** | 高（每次重复传输提示词） | 低（知识库检索减少重复） |
| **建筑质量** | 简陋木屋 | 精美风格建筑 |

---

## 十、参考资源

### AI Agent相关
- [Anthropic Claude Tool Use文档](https://docs.anthropic.com/claude/docs/tool-use)
- [Build AI Agents with RAG: A Complete Guide](https://www.datacamp.com/tutorial/build-ai-agents-with-rag)
- [LangChain RAG Tutorial](https://langchain.com/rag-tutorial)

### 游戏AI相关
- [Game AI: Procedural Content Generation](https://aaai.org/library/game-ai-procedural-generation/)
- [AI Agents for Game Development](https://www.gamedeveloper.com/ai-agents-game-dev)

### 泰拉瑞亚技术
- [Terraria Wiki - Housing](https://terraria.wiki.gg/wiki/House)
- [tModLoader官方文档](https://docs.tmodloader.net/)

---

## 十一、后续扩展方向

1. **向量数据库集成** - 使用真正的RAG向量检索替代JSON搜索
2. **建筑预览系统** - 在放置前显示3D预览
3. **迭代改进** - 支持用户反馈并调整设计
4. **蓝图保存** - 保存生成的建筑供复用
5. **多人协作** - 支持多人模式下的Agent建筑生成

---

*文档版本: 1.0*
*创建日期: 2026-06-01*