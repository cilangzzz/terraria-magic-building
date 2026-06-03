// ============================================================================
// DEPRECATED - 此文件已废弃，请使用 TrueAgentCore 替代
// 原因: SingleAgent模式已被统一到 TrueAgentCore 中
// 迁移: 使用 trab.Core.Agents.TrueAgentCore 和 AIAgentService
// 保留: 仅作为参考，不应再被调用
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Terraria.ModLoader;
using trab.Config;
using trab.Core.API;
using trab.Core.KnowledgeBase;
using trab.Data;

namespace trab.Core.Agents.SingleAgent
{
    /// <summary>
    /// 单Agent建筑生成器 - 生成TEditSch格式建筑
    /// </summary>
    public class BuildingSingleAgent : ApiServiceBase
    {
        private const int MAX_AGENT_ROUNDS = 10;
        private readonly int _maxBuildingSize;

        private const string AGENT_SYSTEM_PROMPT = @"泰拉瑞亚建筑设计Agent。输出TEditSch格式的建筑JSON。

## TEditSch输出格式（必须严格遵守）
```json
{
  ""name"": ""建筑名称"",
  ""width"": 10,
  ""height"": 8,
  ""tiles"": [
    [
      {""active"":true,""type"":4,""wall"":6},
      {""active"":true,""type"":4,""wall"":6},
      {""active"":true,""type"":4,""wall"":6},
      {""active"":true,""type"":4,""wall"":6},
      {""active"":true,""type"":4,""wall"":6},
      {""active"":true,""type"":4,""wall"":6},
      {""active"":true,""type"":4,""wall"":6},
      {""active"":true,""type"":4,""wall"":6},
      {""active"":true,""type"":4,""wall"":6},
      {""active"":true,""type"":4,""wall"":6}
    ],
    [
      {""active"":true,""type"":4,""wall"":6},
      {""active"":false,""wall"":6},
      {""active"":false,""wall"":6},
      {""active"":false,""wall"":6},
      {""active"":false,""wall"":6},
      {""active"":false,""wall"":6},
      {""active"":false,""wall"":6},
      {""active"":false,""wall"":6},
      {""active"":false,""wall"":6},
      {""active"":true,""type"":4,""wall"":6}
    ]
  ]
}
```

## tiles数组规则（重要！）
- tiles是二维数组，tiles[y][x]表示坐标(x,y)的格子
- 每行必须有width个元素，共有height行
- active=true表示有方块，active=false表示空（只有墙）
- type是TileID（方块ID），wall是WallID（墙壁ID）
- 必须为每个格子指定wall值，即使是边界方块

## TileID参考（常用）
- 1=Stone, 4=GrayBrick, 5=Wood, 30=WoodBlock
- 38=GrayBrick, 143=StoneSlab, 171=Glass
- 10=WoodenDoor, 11=GlassDoor
- 17=WorkBench, 87=Table, 88=Chair, 89=Bed
- 4=Torch, 34=Chandelier, 33=LampPost

## WallID参考（常用）
- 1=StoneWall, 4=WoodWall, 6=GrayBrickWall
- 11=BrickWall, 14=GlassWall, 16=StoneSlabWall

## 建筑设计规则
1. 尺寸控制在MaxBuildingSize以内
2. 边界放置方块(active=true,type=ID)，内部只填墙(active=false,wall=ID)
3. 底部中间留空放置门(active=true,type=10)
4. 内部放置必要家具：光源(type=4)、工作台(type=17)、椅子(type=88)

## 工具调用流程
1. search_tiles - 获取方块TileID
2. search_walls - 获取墙壁WallID
3. search_furniture - 获取家具TileID
4. select_materials - 确认材料选择
5. 输出完整TEditSch格式JSON（必须包含所有width*height个格子）";

        public BuildingSingleAgent(string apiKey, AIServiceType serviceType, string modelName, int maxBuildingSize = 50)
            : base(apiKey, serviceType, modelName)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(120);
            _maxBuildingSize = maxBuildingSize;
        }

        /// <summary>
        /// 生成建筑 - 返回TEditSch格式
        /// </summary>
        public async Task<TEditSchDesign> GenerateBuildingAsync(
            string userPrompt,
            Action<string, int> progressCallback = null,
            CancellationToken ct = default)
        {
            var kb = KnowledgeBaseManager.Instance;
            kb.Initialize();

            if (_useOpenAIFormat)
            {
                return await RunOpenAIAgentLoop(userPrompt, kb, progressCallback, ct);
            }
            else
            {
                return await RunAnthropicAgentLoop(userPrompt, kb, progressCallback, ct);
            }
        }

        #region OpenAI格式Agent

        private async Task<TEditSchDesign> RunOpenAIAgentLoop(
            string userPrompt,
            KnowledgeBaseManager kb,
            Action<string, int> progressCallback,
            CancellationToken ct)
        {
            var messages = new List<object>
            {
                new { role = "system", content = AGENT_SYSTEM_PROMPT.Replace("MaxBuildingSize", _maxBuildingSize.ToString()) },
                new { role = "user", content = userPrompt }
            };

            int round = 0;
            while (true)
            {
                round++;
                if (round > MAX_AGENT_ROUNDS)
                {
                    progressCallback?.Invoke("超过最大轮数限制", 0);
                    return null;
                }

                progressCallback?.Invoke($"[轮次{round}]思考中...", round);

                var requestBody = BuildOpenAIRequest(messages);
                var response = await SendOpenAIRequestAsync(requestBody, ct);

                if (response == null)
                {
                    progressCallback?.Invoke("API请求失败", 0);
                    return null;
                }

                var message = response.choices?[0]?.message;
                if (message == null) return null;

                var toolCalls = message.tool_calls;
                if (toolCalls != null && toolCalls.Length > 0)
                {
                    messages.Add(new
                    {
                        role = "assistant",
                        content = message.content ?? "",
                        tool_calls = toolCalls
                    });

                    foreach (var toolCall in toolCalls)
                    {
                        string toolName = toolCall.function?.name;
                        string toolCallId = toolCall.id;
                        var toolArgs = toolCall.function?.arguments;

                        progressCallback?.Invoke($"[轮次{round}]调用工具: {toolName}", round);

                        JObject argsObj = null;
                        try { argsObj = JObject.Parse(toolArgs ?? "{}"); }
                        catch { argsObj = new JObject(); }

                        var result = ExecuteTool(toolName, argsObj, kb);

                        messages.Add(new
                        {
                            role = "tool",
                            tool_call_id = toolCallId,
                            content = result.Content
                        });
                    }

                    continue;
                }

                if (!string.IsNullOrEmpty(message.content))
                {
                    progressCallback?.Invoke($"[轮次{round}]生成完成", round);
                    var design = ParseTEditSchDesign(message.content);
                    progressCallback?.Invoke($"完成({round}轮)", 0);
                    return design;
                }

                progressCallback?.Invoke($"[轮次{round}]响应异常，继续...", round);
            }
        }

        private object BuildOpenAIRequest(List<object> messages)
        {
            return new
            {
                model = _modelName,
                messages = messages,
                tools = GetOpenAIToolDefinitions(),
                tool_choice = "auto",
                max_tokens = 16384
            };
        }

        private List<object> GetOpenAIToolDefinitions()
        {
            return new List<object>
            {
                new { type = "function", function = new { name = "search_tiles", description = "搜索方块类型，返回tile_id、名称。用于确定建筑材料。", parameters = new { type = "object", properties = new { style = new { type = "string", description = "风格: medieval, fantasy, natural, asian, steampunk" }, category = new { type = "string", description = "类别: basic, wood, brick, slab, glass, door, furniture, light" } }, required = new[] { "style" } } } },
                new { type = "function", function = new { name = "search_walls", description = "搜索墙壁类型，返回wall_id、名称。用于确定建筑墙壁材料。", parameters = new { type = "object", properties = new { style = new { type = "string", description = "风格: medieval, fantasy, natural" }, category = new { type = "string", description = "类别: natural, wood, brick, glass" } }, required = new[] { "style" } } } },
                new { type = "function", function = new { name = "search_furniture", description = "搜索家具，返回tile_id、名称、NPC房屋功能。", parameters = new { type = "object", properties = new { category = new { type = "string", description = "类别: light(光源), surface(桌面), comfort(舒适), storage(存储), door(门)" } } } } },
                new { type = "function", function = new { name = "select_materials", description = "确认材料选择，返回选定的TileID和WallID。", parameters = new { type = "object", properties = new { main_tile_id = new { type = "integer", description = "主要方块TileID" }, roof_tile_id = new { type = "integer", description = "屋顶方块TileID" }, floor_tile_id = new { type = "integer", description = "地板方块TileID" }, wall_id = new { type = "integer", description = "墙壁WallID" }, door_id = new { type = "integer", description = "门TileID" }, light_id = new { type = "integer", description = "光源TileID" }, surface_id = new { type = "integer", description = "桌面TileID" }, comfort_id = new { type = "integer", description = "舒适家具TileID" } }, required = new[] { "main_tile_id", "wall_id" } } } },
                new { type = "function", function = new { name = "get_roof_template", description = "获取屋顶设计模板，返回形状描述和推荐方块。", parameters = new { type = "object", properties = new { roof_type = new { type = "string", description = "屋顶类型: gable(人字形), flat(平顶), dome(圆顶)" }, width = new { type = "integer", description = "建筑宽度" } }, required = new[] { "roof_type", "width" } } } },
                new { type = "function", function = new { name = "get_style_template", description = "获取建筑风格模板，包含推荐方块和油漆方案。", parameters = new { type = "object", properties = new { style = new { type = "string", description = "风格名称: medieval, fantasy, natural, asian" } }, required = new[] { "style" } } } }
            };
        }

        private async Task<OpenAIResponse> SendOpenAIRequestAsync(object requestBody, CancellationToken ct)
        {
            string json = JsonConvert.SerializeObject(requestBody);
            trab.Instance?.Logger.Info($"OpenAI请求: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiEndpoint, content, ct);
            string responseJson = await response.Content.ReadAsStringAsync(ct);

            trab.Instance?.Logger.Info($"OpenAI响应: {responseJson}");

            if (!response.IsSuccessStatusCode)
            {
                trab.Instance?.Logger.Error($"API错误: {responseJson}");
                return null;
            }

            return JsonConvert.DeserializeObject<OpenAIResponse>(responseJson);
        }

        #endregion

        #region Anthropic格式Agent

        private async Task<TEditSchDesign> RunAnthropicAgentLoop(
            string userPrompt,
            KnowledgeBaseManager kb,
            Action<string, int> progressCallback,
            CancellationToken ct)
        {
            var messages = new List<JObject>();
            messages.Add(JObject.FromObject(new { role = "user", content = userPrompt }));

            for (int round = 1; round <= MAX_AGENT_ROUNDS; round++)
            {
                progressCallback?.Invoke($"[轮次{round}]思考中...", round);

                var requestBody = BuildAnthropicRequest(messages);
                var response = await SendAnthropicRequestAsync(requestBody, ct);

                if (response == null)
                {
                    progressCallback?.Invoke("API请求失败", 0);
                    return null;
                }

                var stopReason = response.stop_reason;

                if (stopReason == "end_turn" || stopReason == "stop_sequence")
                {
                    progressCallback?.Invoke($"[轮次{round}]生成完成", round);
                    var textContent = ExtractTextContent(response.content);
                    if (textContent != null)
                    {
                        var design = ParseTEditSchDesign(textContent);
                        progressCallback?.Invoke($"完成({round}轮)", 0);
                        return design;
                    }
                    return null;
                }

                if (stopReason == "tool_use")
                {
                    messages.Add(JObject.FromObject(new { role = "assistant", content = response.content }));

                    var toolResults = new List<JObject>();
                    foreach (var item in response.content)
                    {
                        if (item.type == "tool_use")
                        {
                            string toolName = item.name?.ToString();
                            string toolId = item.id?.ToString();
                            var toolInput = item.input as JObject;

                            progressCallback?.Invoke($"[轮次{round}]调用工具: {toolName}", round);
                            var result = ExecuteTool(toolName, toolInput, kb);

                            toolResults.Add(JObject.FromObject(new
                            {
                                type = "tool_result",
                                tool_use_id = toolId,
                                content = result.Content,
                                is_error = result.IsError
                            }));
                        }
                    }

                    messages.Add(JObject.FromObject(new { role = "user", content = toolResults }));
                    continue;
                }

                progressCallback?.Invoke($"停止: {stopReason}", 0);
                break;
            }

            progressCallback?.Invoke("超过最大轮数限制", 0);
            return null;
        }

        private object BuildAnthropicRequest(List<JObject> messages)
        {
            return new
            {
                model = _modelName,
                max_tokens = 8192,
                system = AGENT_SYSTEM_PROMPT.Replace("MaxBuildingSize", _maxBuildingSize.ToString()),
                messages = messages,
                tools = GetAnthropicToolDefinitions()
            };
        }

        private List<object> GetAnthropicToolDefinitions()
        {
            return new List<object>
            {
                new { name = "search_tiles", description = "搜索方块类型，返回tile_id、名称。", input_schema = new { type = "object", properties = new { style = new { type = "string" }, category = new { type = "string" } }, required = new[] { "style" } } },
                new { name = "search_walls", description = "搜索墙壁类型，返回wall_id、名称。", input_schema = new { type = "object", properties = new { style = new { type = "string" }, category = new { type = "string" } }, required = new[] { "style" } } },
                new { name = "search_furniture", description = "搜索家具，返回tile_id、名称。", input_schema = new { type = "object", properties = new { category = new { type = "string" } } } },
                new { name = "select_materials", description = "确认材料选择。", input_schema = new { type = "object", properties = new { main_tile_id = new { type = "integer" }, wall_id = new { type = "integer" } }, required = new[] { "main_tile_id", "wall_id" } } },
                new { name = "get_roof_template", description = "获取屋顶设计模板。", input_schema = new { type = "object", properties = new { roof_type = new { type = "string" }, width = new { type = "integer" } }, required = new[] { "roof_type", "width" } } },
                new { name = "get_style_template", description = "获取建筑风格模板。", input_schema = new { type = "object", properties = new { style = new { type = "string" } }, required = new[] { "style" } } }
            };
        }

        private async Task<ClaudeAgentResponse> SendAnthropicRequestAsync(object requestBody, CancellationToken ct)
        {
            string json = JsonConvert.SerializeObject(requestBody);
            trab.Instance?.Logger.Info($"Anthropic请求: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiEndpoint, content, ct);
            string responseJson = await response.Content.ReadAsStringAsync(ct);

            trab.Instance?.Logger.Info($"Anthropic响应: {responseJson}");

            if (!response.IsSuccessStatusCode)
            {
                trab.Instance?.Logger.Error($"API错误: {responseJson}");
                return null;
            }

            return JsonConvert.DeserializeObject<ClaudeAgentResponse>(responseJson);
        }

        private string ExtractTextContent(List<ClaudeContentItem> contents)
        {
            if (contents == null) return null;
            foreach (var item in contents)
            {
                if (item.type == "text" && !string.IsNullOrEmpty(item.text))
                    return item.text;
            }
            return null;
        }

        #endregion

        #region 工具执行

        private ToolResult ExecuteTool(string name, JObject input, KnowledgeBaseManager kb)
        {
            try
            {
                switch (name)
                {
                    case "search_tiles":
                        return SearchTiles(input["style"]?.ToString(), input["category"]?.ToString(), kb);
                    case "search_walls":
                        return SearchWalls(input["style"]?.ToString(), input["category"]?.ToString(), kb);
                    case "search_furniture":
                        return SearchFurniture(input["category"]?.ToString(), kb);
                    case "select_materials":
                        return SelectMaterials(input, kb);
                    case "get_roof_template":
                        return GetRoofTemplate(input["roof_type"]?.ToString(), input["width"]?.Value<int>() ?? 10, kb);
                    case "get_style_template":
                        return GetStyleTemplate(input["style"]?.ToString(), kb);
                    default:
                        return new ToolResult { IsError = true, Content = "{\"error\": \"未知工具\"}" };
                }
            }
            catch (Exception ex)
            {
                return new ToolResult { IsError = true, Content = $"{{\"error\": \"{ex.Message}\"}}" };
            }
        }

        private ToolResult SearchTiles(string style, string category, KnowledgeBaseManager kb)
        {
            var candidates = kb.Tiles.SearchTiles(null, category, null).ToList();

            if (!string.IsNullOrEmpty(style) && kb.Vectors.IsInitialized)
            {
                candidates = kb.Vectors.SearchTilesSemantic(candidates, style, 15);
            }
            else
            {
                candidates = candidates.Take(15).ToList();
            }

            var result = new
            {
                tiles = candidates.Select(t => new { id = t.id, name = t.name, display_name = t.display_name }),
                note = "使用tile_id作为TEditSch格式中的type值"
            };
            return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
        }

        private ToolResult SearchWalls(string style, string category, KnowledgeBaseManager kb)
        {
            var candidates = kb.Tiles.SearchWalls(null, category).ToList();

            if (!string.IsNullOrEmpty(style) && kb.Vectors.IsInitialized)
            {
                candidates = kb.Vectors.SearchWallsSemantic(candidates, style, 15);
            }
            else
            {
                candidates = candidates.Take(15).ToList();
            }

            var result = new
            {
                walls = candidates.Select(w => new { id = w.id, name = w.name, display_name = w.display_name }),
                note = "使用wall_id作为TEditSch格式中的wall值"
            };
            return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
        }

        private ToolResult SearchFurniture(string category, KnowledgeBaseManager kb)
        {
            var candidates = kb.Furniture.SearchFurniture(category, null);

            var result = new
            {
                furniture = candidates.Select(f => new { tile_id = f.Value.tile_id, name = f.Key, display_name = f.Value.display_name, category = f.Value.category }),
                npc_requirements = new { light = "光源如Torch(id=4)", surface = "桌面如WorkBench(id=17)", comfort = "舒适如Chair(id=88)", door = "门如WoodenDoor(id=10)" }
            };
            return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
        }

        private ToolResult SelectMaterials(JObject input, KnowledgeBaseManager kb)
        {
            var result = new
            {
                selected = new
                {
                    main_tile_id = input["main_tile_id"]?.Value<int>() ?? 4,
                    roof_tile_id = input["roof_tile_id"]?.Value<int>() ?? 143,
                    floor_tile_id = input["floor_tile_id"]?.Value<int>() ?? 19,
                    wall_id = input["wall_id"]?.Value<int>() ?? 6,
                    door_id = input["door_id"]?.Value<int>() ?? 10,
                    light_id = input["light_id"]?.Value<int>() ?? 4,
                    surface_id = input["surface_id"]?.Value<int>() ?? 17,
                    comfort_id = input["comfort_id"]?.Value<int>() ?? 88
                },
                message = "材料已选择。现在生成TEditSch格式JSON。tiles[y][x]二维数组，使用选定的ID。"
            };
            return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
        }

        private ToolResult GetRoofTemplate(string roofType, int width, KnowledgeBaseManager kb)
        {
            var template = kb.Roofs.GetTemplate(roofType);
            if (template == null)
                return new ToolResult { IsError = true, Content = "{\"error\": \"未找到屋顶模板\"}" };

            int roofHeight = CalculateRoofHeight(roofType, width);

            var result = new
            {
                roof_type = roofType,
                height = roofHeight,
                shape = template.shape_pattern,
                edge_tiles = template.edge_tiles,
                fill_tiles = template.fill_tiles
            };
            return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
        }

        private ToolResult GetStyleTemplate(string style, KnowledgeBaseManager kb)
        {
            var template = kb.Styles.GetTemplate(style, null);
            if (template == null)
                return new ToolResult { IsError = true, Content = $"{{\"error\": \"未找到风格: {style}\"}}" };

            var paintScheme = kb.Tiles.GetPaintRecommendation(style);
            var result = new
            {
                style = style,
                name = template.name,
                description = template.description,
                paint = new { primary = paintScheme.PrimaryPaint, shadow = paintScheme.ShadowPaint }
            };
            return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
        }

        private int CalculateRoofHeight(string roofType, int width)
        {
            switch (roofType?.ToLower())
            {
                case "gable": return Math.Max(2, width / 3);
                case "flat": return 1;
                case "dome": return Math.Max(2, width / 4);
                default: return Math.Max(2, width / 4);
            }
        }

        #endregion

        #region JSON解析

        private TEditSchDesign ParseTEditSchDesign(string content)
        {
            if (string.IsNullOrEmpty(content)) return null;
            string json = ExtractJson(content);
            if (json == null) return null;

            try
            {
                var design = JsonConvert.DeserializeObject<TEditSchDesign>(json);

                // 验证并修复数据
                if (design != null)
                {
                    ValidateAndFixDesign(design);
                    design.CalculateStats();
                }

                return design;
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"解析TEditSch失败: {ex.Message}");

                // 尝试解析旧格式并转换
                var oldDesign = JsonConvert.DeserializeObject<BuildingDesign>(json);
                if (oldDesign != null)
                {
                    return ConvertFromOldFormat(oldDesign);
                }

                return null;
            }
        }

        private void ValidateAndFixDesign(TEditSchDesign design)
        {
            // 确保tiles数组正确
            if (design.tiles == null || design.tiles.Count == 0)
            {
                design.tiles = new List<List<TEditTile>>();
                for (int y = 0; y < design.height; y++)
                {
                    var row = new List<TEditTile>();
                    for (int x = 0; x < design.width; x++)
                        row.Add(new TEditTile());
                    design.tiles.Add(row);
                }
            }

            // 确保每行有正确的宽度
            foreach (var row in design.tiles)
            {
                while (row.Count < design.width)
                    row.Add(new TEditTile());
            }

            // 确保有足够的行
            while (design.tiles.Count < design.height)
            {
                var row = new List<TEditTile>();
                for (int x = 0; x < design.width; x++)
                    row.Add(new TEditTile());
                design.tiles.Add(row);
            }
        }

        private TEditSchDesign ConvertFromOldFormat(BuildingDesign old)
        {
            var design = new TEditSchDesign
            {
                name = old.Name,
                width = old.Width,
                height = old.Height
            };

            // 初始化空网格
            for (int y = 0; y < old.Height; y++)
            {
                var row = new List<TEditTile>();
                for (int x = 0; x < old.Width; x++)
                    row.Add(new TEditTile());
                design.tiles.Add(row);
            }

            // 放置墙壁范围
            foreach (var range in old.WallRanges)
            {
                for (int y = range.Y1; y <= range.Y2; y++)
                {
                    for (int x = range.X1; x <= range.X2; x++)
                    {
                        if (y >= 0 && y < old.Height && x >= 0 && x < old.Width)
                        {
                            design.tiles[y][x].wall = range.WallId;
                        }
                    }
                }
            }

            // 放置方块
            foreach (var tile in old.Tiles)
            {
                if (tile.Y >= 0 && tile.Y < old.Height && tile.X >= 0 && tile.X < old.Width)
                {
                    design.tiles[tile.Y][tile.X] = new TEditTile
                    {
                        active = true,
                        type = tile.TileId,
                        wall = design.tiles[tile.Y][tile.X].wall,
                        tile_color = tile.Paint > 0 ? tile.Paint : null
                    };
                }
            }

            // 放置家具
            foreach (var f in old.Furniture)
            {
                if (f.Y >= 0 && f.Y < old.Height && f.X >= 0 && f.X < old.Width)
                {
                    design.tiles[f.Y][f.X] = new TEditTile
                    {
                        active = true,
                        type = f.TileId,
                        wall = design.tiles[f.Y][f.X].wall
                    };
                }
            }

            // 放置门
            foreach (var d in old.Doors)
            {
                if (d.Y >= 0 && d.Y < old.Height && d.X >= 0 && d.X < old.Width)
                {
                    design.tiles[d.Y][d.X] = new TEditTile
                    {
                        active = true,
                        type = d.TileId,
                        wall = design.tiles[d.Y][d.X].wall
                    };
                }
            }

            // 放置光源
            foreach (var l in old.LightSources)
            {
                if (l.Y >= 0 && l.Y < old.Height && l.X >= 0 && l.X < old.Width)
                {
                    design.tiles[l.Y][l.X] = new TEditTile
                    {
                        active = true,
                        type = l.TileId,
                        wall = design.tiles[l.Y][l.X].wall
                    };
                }
            }

            design.CalculateStats();
            return design;
        }

        private string ExtractJson(string content)
        {
            int start = content.IndexOf("```json");
            if (start >= 0)
            {
                start += 7;
                int end = content.IndexOf("```", start);
                if (end > start) return content.Substring(start, end - start).Trim();
            }

            int braceStart = content.IndexOf('{');
            int braceEnd = content.LastIndexOf('}');
            if (braceStart >= 0 && braceEnd > braceStart)
                return content.Substring(braceStart, braceEnd - braceStart + 1);

            return null;
        }

        #endregion
    }
}