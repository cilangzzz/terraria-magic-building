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
    /// 单Agent建筑生成器 - 单次API调用生成完整建筑
    /// </summary>
    public class BuildingSingleAgent : ApiServiceBase
    {
        private const int MAX_AGENT_ROUNDS = 10;

        private const string AGENT_SYSTEM_PROMPT = @"泰拉瑞亚建筑设计Agent。根据已有建筑数据生成新建筑。

## 流程：检索→参考→生成

1. analyze_requirement - 分析需求，确定风格和类型
2. search_buildings - 检索相似建筑，获取真实数据
3. get_building_sequence - 获取建造顺序步骤
4. select_materials - 选择材料ID
5. 输出JSON - 使用检索到的材料和结构

## 风格指南
- medieval: 灰砖、石板、石墙，人字形屋顶
- asian: 木材、王朝木、木墙，宝塔屋顶
- fantasy: 珍珠石、玻璃墙，圆顶
- natural: 木材、木墙，平顶
- modern: 花岗岩、玻璃墙，简洁线条

**核心原则：参考检索到的建筑数据，使用其材料和结构生成新建筑。**";

        public BuildingSingleAgent(string apiKey, AIServiceType serviceType, string modelName)
            : base(apiKey, serviceType, modelName)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(120);
        }

        /// <summary>
        /// 生成建筑
        /// </summary>
        public async Task<BuildingDesign> GenerateBuildingAsync(
            string userPrompt,
            Action<string, int> progressCallback = null,
            CancellationToken ct = default)
        {
            var kb = KnowledgeBaseManager.Instance;
            kb.Initialize();

            var toolCallRecords = new List<ToolCallRecord>();

            if (_useOpenAIFormat)
            {
                return await RunOpenAIAgentLoop(userPrompt, kb, toolCallRecords, progressCallback, ct);
            }
            else
            {
                return await RunAnthropicAgentLoop(userPrompt, kb, toolCallRecords, progressCallback, ct);
            }
        }

        #region OpenAI格式Agent

        private async Task<BuildingDesign> RunOpenAIAgentLoop(
            string userPrompt,
            KnowledgeBaseManager kb,
            List<ToolCallRecord> toolCallRecords,
            Action<string, int> progressCallback,
            CancellationToken ct)
        {
            var messages = new List<object>
            {
                new { role = "system", content = AGENT_SYSTEM_PROMPT },
                new { role = "user", content = userPrompt }
            };

            int round = 0;
            while (true)
            {
                round++;
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

                        toolCallRecords.Add(new ToolCallRecord
                        {
                            ToolName = toolName,
                            Input = argsObj,
                            Output = result.Content,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        });

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
                    var design = ParseBuildingDesign(message.content, toolCallRecords);
                    progressCallback?.Invoke($"完成({round}轮,{toolCallRecords.Count}次工具调用)", 0);
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
                max_tokens = 32768
            };
        }

        private List<object> GetOpenAIToolDefinitions()
        {
            return new List<object>
            {
                new { type = "function", function = new { name = "search_tiles", description = "搜索方块类型，返回tile_id、名称、属性。用于确定建筑材料。", parameters = new { type = "object", properties = new { style = new { type = "string", description = "风格: medieval, fantasy, natural, steampunk, asian, snow, desert, modern, dark" }, category = new { type = "string", description = "类别: basic, wood, brick, slab, luxury, furniture, light, door" }, biome = new { type = "string", description = "生物群落: forest, desert, snow, jungle, ocean, underground" } }, required = new[] { "style" } } } },
                new { type = "function", function = new { name = "search_walls", description = "搜索墙壁类型，返回wall_id、名称、属性。用于确定建筑墙壁材料。", parameters = new { type = "object", properties = new { style = new { type = "string", description = "风格: medieval, fantasy, natural, steampunk, asian, snow, desert, modern, dark" }, category = new { type = "string", description = "类别: natural, wood, brick, luxury, desert, snow" } }, required = new[] { "style" } } } },
                new { type = "function", function = new { name = "get_style_template", description = "获取建筑风格模板，包含推荐方块、油漆方案、建筑规则。", parameters = new { type = "object", properties = new { style = new { type = "string", description = "风格名称" }, building_type = new { type = "string", description = "建筑类型: house, castle, tower, shop, temple" } }, required = new[] { "style" } } } },
                new { type = "function", function = new { name = "search_furniture", description = "搜索家具及其NPC房屋功能。返回tile_id、尺寸、放置规则。支持按类别向量检索。", parameters = new { type = "object", properties = new { room_type = new { type = "string", description = "房间类型/家具类别: light(光源), surface(桌面), comfort(舒适), storage(存储), door(门), decoration(装饰)" }, npc_type = new { type = "string", description = "目标NPC类型（可选）" } } } } },
                new { type = "function", function = new { name = "get_paint_scheme", description = "获取推荐的油漆颜色方案。", parameters = new { type = "object", properties = new { style = new { type = "string", description = "风格名称" }, theme = new { type = "string", description = "主题: warm, cold, dark, bright" } } } } },
                new { type = "function", function = new { name = "get_roof_template", description = "获取屋顶设计模板，返回形状描述和推荐方块。用于设计建筑屋顶结构。", parameters = new { type = "object", properties = new { roof_type = new { type = "string", description = "屋顶类型: gable(人字形), flat(平顶), dome(圆顶), pagoda(宝塔), stepped(阶梯)" }, style = new { type = "string", description = "建筑风格: medieval, fantasy, natural..." }, width = new { type = "integer", description = "建筑宽度，用于计算屋顶高度" } }, required = new[] { "roof_type", "width" } } } },
                new { type = "function", function = new { name = "get_window_template", description = "获取窗户设计模板，返回尺寸和方块ID。用于设计窗户布局。", parameters = new { type = "object", properties = new { window_type = new { type = "string", description = "窗户类型: single(单窗), double(双窗), arched(拱窗), bay(凸窗)" }, style = new { type = "string", description = "建筑风格" } }, required = new[] { "window_type" } } } },
                new { type = "function", function = new { name = "get_floor_structure", description = "获取楼层结构模板，返回楼层数、高度、墙壁厚度。用于设计建筑骨架。", parameters = new { type = "object", properties = new { structure_type = new { type = "string", description = "结构类型: single_story(单层), two_story(双层), tower(塔楼), castle(城堡)" }, style = new { type = "string", description = "建筑风格" } }, required = new[] { "structure_type" } } } },
                new { type = "function", function = new { name = "analyze_requirement", description = "分析用户建筑需求，提取建筑类型、风格、材料类别等关键信息。这是建筑生成的第一步。", parameters = new { type = "object", properties = new { building_type = new { type = "string", description = "建筑类型: house, tower, castle, shop, temple" }, style = new { type = "string", description = "风格: medieval, fantasy, natural, steampunk, asian, snow, desert, modern, dark" }, biome = new { type = "string", description = "生物群落: forest, desert, snow, jungle, ocean, underground" }, floor_count = new { type = "integer", description = "楼层数(1-4)" }, width = new { type = "integer", description = "建筑宽度(6-20)" }, height = new { type = "integer", description = "建筑高度(6-16)" }, main_block_category = new { type = "string", description = "主要方块类别: basic, wood, brick, slab, luxury" }, roof_block_category = new { type = "string", description = "屋顶方块类别: slab, brick" }, floor_block_category = new { type = "string", description = "地板方块类别: wood, brick" }, main_wall_category = new { type = "string", description = "主要墙壁类别: natural, wood, brick" }, need_npc_house = new { type = "boolean", description = "是否需要NPC房屋功能" }, reasoning = new { type = "string", description = "分析推理说明" } }, required = new[] { "building_type", "style" } } } },
                new { type = "function", function = new { name = "select_materials", description = "从检索到的材料候选中选择最合适的材料ID。注意：wall_id是墙壁背景ID(如4=木墙、6=灰砖墙、14=玻璃墙)，tile_id是方块ID(如4=灰砖、10=门)。两者不同！", parameters = new { type = "object", properties = new { main_block_id = new { type = "integer", description = "主要方块tile_id（方块ID，如4=灰砖、5=木材）" }, secondary_block_id = new { type = "integer", description = "次要方块tile_id（可选）" }, roof_block_id = new { type = "integer", description = "屋顶方块tile_id（如143=石板）" }, floor_block_id = new { type = "integer", description = "地板方块tile_id（如5=木材、19=平台）" }, main_wall_id = new { type = "integer", description = "墙壁背景wall_id（注意：这是wall_id不是tile_id！如4=木墙、6=灰砖墙、14=玻璃墙。不要用10，10是门的tile_id不是墙！）" }, light_id = new { type = "integer", description = "光源家具tile_id（如4=火把、34=吊灯）" }, surface_id = new { type = "integer", description = "桌面家具tile_id（如17=工作台、87=桌子）" }, comfort_id = new { type = "integer", description = "舒适家具tile_id（如88=椅子、89=床）" }, door_id = new { type = "integer", description = "门家具tile_id（如10=木门、11=玻璃门）" }, primary_paint = new { type = "integer", description = "主色调油漆ID(0-31)" }, shadow_paint = new { type = "integer", description = "阴影油漆ID(默认28)" }, reasoning = new { type = "string", description = "选择理由说明" } }, required = new[] { "main_block_id", "roof_block_id", "floor_block_id", "main_wall_id" } } } },
                new { type = "function", function = new { name = "search_buildings", description = "搜索建筑实体库，返回相似建筑的ID、风格、材料、建造顺序。用于参考已有建筑的构造方式。", parameters = new { type = "object", properties = new { style = new { type = "string", description = "建筑风格: asian, medieval, fantasy, natural, steampunk, modern, dark" }, building_type = new { type = "string", description = "建筑类型: residence, castle, tower, temple, shop" }, top_k = new { type = "integer", description = "返回数量(默认3)" } } } } },
                new { type = "function", function = new { name = "get_building_sequence", description = "获取建筑的详细建造顺序，返回step-by-step建造指令。Agent按此顺序执行建造。", parameters = new { type = "object", properties = new { building_id = new { type = "string", description = "建筑实体ID（如20260602215014）" } }, required = new[] { "building_id" } } } }
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

        private async Task<BuildingDesign> RunAnthropicAgentLoop(
            string userPrompt,
            KnowledgeBaseManager kb,
            List<ToolCallRecord> toolCallRecords,
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
                        var design = ParseBuildingDesign(textContent, toolCallRecords);
                        progressCallback?.Invoke($"完成({round}轮,{toolCallRecords.Count}次工具调用)", 0);
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

                            toolCallRecords.Add(new ToolCallRecord
                            {
                                ToolName = toolName,
                                Input = toolInput,
                                Output = result.Content,
                                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                            });

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
                max_tokens = 4096,
                system = AGENT_SYSTEM_PROMPT,
                messages = messages,
                tools = GetAnthropicToolDefinitions()
            };
        }

        private List<object> GetAnthropicToolDefinitions()
        {
            return new List<object>
            {
                new { name = "search_tiles", description = "搜索方块类型，返回tile_id、名称、属性。", input_schema = new { type = "object", properties = new { style = new { type = "string" }, category = new { type = "string" }, biome = new { type = "string" } }, required = new[] { "style" } } },
                new { name = "search_walls", description = "搜索墙壁类型，返回wall_id、名称、属性。用于确定建筑墙壁材料。", input_schema = new { type = "object", properties = new { style = new { type = "string" }, category = new { type = "string" } }, required = new[] { "style" } } },
                new { name = "get_style_template", description = "获取建筑风格模板。", input_schema = new { type = "object", properties = new { style = new { type = "string" }, building_type = new { type = "string" } }, required = new[] { "style" } } },
                new { name = "search_furniture", description = "搜索家具及NPC房屋功能。支持按类别向量检索。", input_schema = new { type = "object", properties = new { room_type = new { type = "string", description = "家具类别: light, surface, comfort, storage, door, decoration" }, npc_type = new { type = "string" } } } },
                new { name = "get_paint_scheme", description = "获取油漆颜色方案。", input_schema = new { type = "object", properties = new { style = new { type = "string" }, theme = new { type = "string" } } } },
                new { name = "get_roof_template", description = "获取屋顶设计模板，返回形状描述和推荐方块。", input_schema = new { type = "object", properties = new { roof_type = new { type = "string" }, style = new { type = "string" }, width = new { type = "integer" } }, required = new[] { "roof_type", "width" } } },
                new { name = "get_window_template", description = "获取窗户设计模板，返回尺寸和方块ID。", input_schema = new { type = "object", properties = new { window_type = new { type = "string" }, style = new { type = "string" } }, required = new[] { "window_type" } } },
                new { name = "get_floor_structure", description = "获取楼层结构模板，返回楼层数、高度、墙壁厚度。", input_schema = new { type = "object", properties = new { structure_type = new { type = "string" }, style = new { type = "string" } }, required = new[] { "structure_type" } } },
                new { name = "search_buildings", description = "搜索建筑实体库，返回相似建筑的ID、风格、材料、建造顺序。用于参考已有建筑的构造方式。", input_schema = new { type = "object", properties = new { style = new { type = "string", description = "建筑风格: asian, medieval, fantasy, natural" }, building_type = new { type = "string", description = "建筑类型: residence, castle, tower, temple" }, top_k = new { type = "integer", description = "返回数量(默认3)" } } } },
                new { name = "get_building_sequence", description = "获取建筑的详细建造顺序，返回step-by-step建造指令。", input_schema = new { type = "object", properties = new { building_id = new { type = "string", description = "建筑实体ID" } }, required = new[] { "building_id" } } }
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
                        return SearchTiles(input["style"]?.ToString(), input["category"]?.ToString(), input["biome"]?.ToString(), kb);
                    case "search_walls":
                        return SearchWalls(input["style"]?.ToString(), input["category"]?.ToString(), kb);
                    case "get_style_template":
                        return GetStyleTemplate(input["style"]?.ToString(), input["building_type"]?.ToString(), kb);
                    case "search_furniture":
                        return SearchFurniture(input["room_type"]?.ToString(), input["npc_type"]?.ToString(), kb);
                    case "get_paint_scheme":
                        return GetPaintScheme(input["style"]?.ToString(), input["theme"]?.ToString(), kb);
                    case "get_roof_template":
                        return GetRoofTemplate(input["roof_type"]?.ToString(), input["style"]?.ToString(), input["width"]?.Value<int>() ?? 10, kb);
                    case "get_window_template":
                        return GetWindowTemplate(input["window_type"]?.ToString(), input["style"]?.ToString(), kb);
                    case "get_floor_structure":
                        return GetFloorStructure(input["structure_type"]?.ToString(), input["style"]?.ToString(), kb);
                    case "analyze_requirement":
                        return AnalyzeRequirement(input, kb);
                    case "select_materials":
                        return SelectMaterials(input, kb);
                    case "search_buildings":
                        return SearchBuildings(input["style"]?.ToString(), input["building_type"]?.ToString(), input["top_k"]?.Value<int>() ?? 3, kb);
                    case "get_building_sequence":
                        return GetBuildingSequence(input["building_id"]?.ToString(), kb);
                    default:
                        return new ToolResult { IsError = true, Content = "{\"error\": \"未知工具\"}" };
                }
            }
            catch (Exception ex)
            {
                return new ToolResult { IsError = true, Content = $"{{\"error\": \"{ex.Message}\"}}" };
            }
        }

        private ToolResult SearchTiles(string style, string category, string biome, KnowledgeBaseManager kb)
        {
            var candidates = kb.Tiles.SearchTiles(null, category, biome).ToList();

            if (!string.IsNullOrEmpty(style) && kb.Vectors.IsInitialized)
            {
                candidates = kb.Vectors.SearchTilesSemantic(candidates, style, 20);
            }
            else
            {
                candidates = candidates.Take(20).ToList();
            }

            var result = new
            {
                tiles = candidates.Select(t => new { id = t.id, name = t.name, display_name = t.display_name, category = t.category }),
                total_count = candidates.Count,
                search_criteria = new { style, category, biome },
                vector_search_used = kb.Vectors.IsInitialized && !string.IsNullOrEmpty(style)
            };
            return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
        }

        private ToolResult SearchWalls(string style, string category, KnowledgeBaseManager kb)
        {
            var candidates = kb.Tiles.SearchWalls(null, category).ToList();

            if (!string.IsNullOrEmpty(style) && kb.Vectors.IsInitialized)
            {
                candidates = kb.Vectors.SearchWallsSemantic(candidates, style, 20);
            }
            else
            {
                candidates = candidates.Take(20).ToList();
            }

            var result = new
            {
                walls = candidates.Select(w => new { id = w.id, name = w.name, display_name = w.display_name, category = w.category }),
                total_count = candidates.Count,
                search_criteria = new { style, category },
                vector_search_used = kb.Vectors.IsInitialized && !string.IsNullOrEmpty(style)
            };
            return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
        }

        private ToolResult GetStyleTemplate(string style, string buildingType, KnowledgeBaseManager kb)
        {
            var template = kb.Styles.GetTemplate(style, buildingType);
            if (template == null)
                return new ToolResult { IsError = true, Content = $"{{\"error\": \"未找到风格: {style}\"}}" };

            var paintScheme = kb.Tiles.GetPaintRecommendation(style);
            var result = new
            {
                style = style,
                name = template.name,
                display_name = template.display_name,
                description = template.description,
                paint_scheme = new { primary = paintScheme.PrimaryPaint, shadow = paintScheme.ShadowPaint }
            };
            return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
        }

        private ToolResult SearchFurniture(string roomType, string npcType, KnowledgeBaseManager kb)
        {
            var candidates = kb.Furniture.SearchFurniture(roomType, npcType);

            string category = roomType?.ToLower();
            if (!string.IsNullOrEmpty(category) && kb.Vectors.IsInitialized)
            {
                candidates = kb.Vectors.SearchFurnitureSemantic(candidates, category, 20);
            }
            else
            {
                candidates = candidates.Take(20).ToList();
            }

            var result = new
            {
                furniture = candidates.Select(f => new { name = f.Key, tile_id = f.Value.tile_id, display_name = f.Value.display_name, category = f.Value.category, npc_function = f.Value.npc_function }),
                total_count = candidates.Count,
                search_criteria = new { room_type = roomType, npc_type = npcType },
                vector_search_used = kb.Vectors.IsInitialized && !string.IsNullOrEmpty(category),
                npc_requirements = new { valid_house_requires = new[] { "light_source", "flat_surface", "comfort", "door" } }
            };
            return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
        }

        private ToolResult GetPaintScheme(string style, string theme, KnowledgeBaseManager kb)
        {
            var paintScheme = kb.Tiles.GetPaintRecommendation(style);
            var paints = kb.Tiles.GetAllPaints().Take(10).ToList();
            var result = new
            {
                style = style,
                primary_paint = paintScheme.PrimaryPaint,
                shadow_paint = paintScheme.ShadowPaint,
                highlight_paint = paintScheme.HighlightPaint,
                paints = paints.Select(p => new { id = p.id, name = p.name })
            };
            return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
        }

        private ToolResult GetRoofTemplate(string roofType, string style, int width, KnowledgeBaseManager kb)
        {
            var template = kb.Roofs.GetTemplate(roofType);
            if (template == null)
                return new ToolResult { IsError = true, Content = "{\"error\": \"未找到屋顶模板: " + roofType + "\"}" };

            int roofHeight = CalculateRoofHeight(roofType, width);

            var result = new
            {
                roof_type = roofType,
                display_name = template.display_name,
                shape_pattern = template.shape_pattern,
                calculated_height = roofHeight,
                edge_tiles = template.edge_tiles,
                fill_tiles = template.fill_tiles,
                description = template.description
            };
            return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
        }

        private ToolResult GetWindowTemplate(string windowType, string style, KnowledgeBaseManager kb)
        {
            var template = kb.Windows.GetTemplate(windowType);
            if (template == null)
                return new ToolResult { IsError = true, Content = "{\"error\": \"未找到窗户模板: " + windowType + "\"}" };

            var result = new
            {
                window_type = windowType,
                display_name = template.display_name,
                width = template.width,
                height = template.height,
                frame_tile_id = template.frame_tile_id,
                glass_tile_id = template.glass_tile_id,
                description = template.description
            };
            return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
        }

        private ToolResult GetFloorStructure(string structureType, string style, KnowledgeBaseManager kb)
        {
            var template = kb.Floors.GetTemplate(structureType);
            if (template == null)
                return new ToolResult { IsError = true, Content = "{\"error\": \"未找到楼层结构模板: " + structureType + "\"}" };

            int totalHeight = template.floor_count * template.floor_height;

            var result = new
            {
                structure_type = structureType,
                display_name = template.display_name,
                floor_count = template.floor_count,
                floor_height = template.floor_height,
                total_height = totalHeight,
                wall_thickness = template.wall_thickness,
                floor_tiles = template.floor_tiles,
                wall_tiles = template.wall_tiles,
                description = template.description
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
                case "pagoda": return Math.Max(3, width / 2);
                case "stepped": return Math.Max(2, width / 3);
                default: return Math.Max(2, width / 4);
            }
        }

        private ToolResult AnalyzeRequirement(JObject input, KnowledgeBaseManager kb)
        {
            try
            {
                string style = input["style"]?.ToString()?.ToLower() ?? "medieval";

                var validStyles = kb.Styles.GetAllStyleNames();
                if (!validStyles.Contains(style))
                {
                    style = "medieval";
                }

                var requirement = new BuildingRequirement
                {
                    BuildingType = input["building_type"]?.ToString() ?? "house",
                    Style = style,
                    Biome = input["biome"]?.ToString() ?? "forest",
                    FloorCount = input["floor_count"]?.Value<int>() ?? 1,
                    PreferredWidth = input["width"]?.Value<int>(),
                    PreferredHeight = input["height"]?.Value<int>(),
                    Materials = new MaterialCategoryRequirement
                    {
                        MainBlockCategory = input["main_block_category"]?.ToString() ?? "brick",
                        RoofBlockCategory = input["roof_block_category"]?.ToString() ?? "slab",
                        FloorBlockCategory = input["floor_block_category"]?.ToString() ?? "wood",
                        MainWallCategory = input["main_wall_category"]?.ToString() ?? "brick",
                        RequiredFurnitureCategories = new List<string> { "light", "surface", "comfort", "door" }
                    },
                    NeedNpcHouse = input["need_npc_house"]?.Value<bool>() ?? true,
                    AnalysisReasoning = input["reasoning"]?.ToString()
                };

                var result = new
                {
                    requirement = requirement,
                    valid_styles = validStyles,
                    style_description = kb.Styles.GetTemplate(style)?.description,
                    suggested_searches = new List<object>
                    {
                        new { tool = "search_tiles", category = requirement.Materials.MainBlockCategory, style = style },
                        new { tool = "search_tiles", category = requirement.Materials.RoofBlockCategory, style = style },
                        new { tool = "search_tiles", category = requirement.Materials.FloorBlockCategory, style = style },
                        new { tool = "search_walls", category = requirement.Materials.MainWallCategory, style = style },
                        new { tool = "search_furniture", room_type = "light" },
                        new { tool = "search_furniture", room_type = "surface" },
                        new { tool = "search_furniture", room_type = "comfort" },
                        new { tool = "search_furniture", room_type = "door" }
                    },
                    message = "需求分析完成。请按建议的搜索顺序调用工具获取材料候选，然后调用select_materials选择最终材料。"
                };

                trab.Instance?.Logger.Info($"需求分析: {requirement.BuildingType}, {requirement.Style}, {requirement.FloorCount}层");

                return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
            }
            catch (Exception ex)
            {
                return new ToolResult { IsError = true, Content = $"{{\"error\": \"{ex.Message}\"}}" };
            }
        }

        private ToolResult SelectMaterials(JObject input, KnowledgeBaseManager kb)
        {
            try
            {
                var selected = new SelectedMaterials
                {
                    MainBlockId = input["main_block_id"]?.Value<int>() ?? 4,
                    SecondaryBlockId = input["secondary_block_id"]?.Value<int>(),
                    RoofBlockId = input["roof_block_id"]?.Value<int>() ?? 143,
                    FloorBlockId = input["floor_block_id"]?.Value<int>() ?? 5,
                    MainWallId = input["main_wall_id"]?.Value<int>() ?? 6,
                    PrimaryPaint = input["primary_paint"]?.Value<int>() ?? 0,
                    ShadowPaint = input["shadow_paint"]?.Value<int>() ?? 28,
                    SelectionReasoning = input["reasoning"]?.ToString()
                };

                if (input["light_id"] != null)
                    selected.FurnitureIds["light"] = input["light_id"].Value<int>();
                if (input["surface_id"] != null)
                    selected.FurnitureIds["surface"] = input["surface_id"].Value<int>();
                if (input["comfort_id"] != null)
                    selected.FurnitureIds["comfort"] = input["comfort_id"].Value<int>();
                if (input["door_id"] != null)
                    selected.FurnitureIds["door"] = input["door_id"].Value<int>();

                var tileInfo = kb.Tiles.GetTileById(selected.MainBlockId);
                if (tileInfo == null)
                {
                    trab.Instance?.Logger.Warn($"选定的MainBlockId={selected.MainBlockId}不在知识库中，使用默认值4");
                    selected.MainBlockId = 4;
                }

                var result = new
                {
                    selected_materials = selected,
                    material_summary = new
                    {
                        main_block = kb.Tiles.GetTileById(selected.MainBlockId)?.display_name ?? "灰砖",
                        roof_block = kb.Tiles.GetTileById(selected.RoofBlockId)?.display_name ?? "石板",
                        floor_block = kb.Tiles.GetTileById(selected.FloorBlockId)?.display_name ?? "木材",
                        main_wall = kb.Tiles.GetWallById(selected.MainWallId)?.display_name ?? "灰砖墙"
                    },
                    message = "材料选择完成。请使用选定的材料ID生成建筑JSON。"
                };

                trab.Instance?.Logger.Info($"材料选择: Main={selected.MainBlockId}, Roof={selected.RoofBlockId}, Floor={selected.FloorBlockId}, Wall={selected.MainWallId}");

                return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
            }
            catch (Exception ex)
            {
                return new ToolResult { IsError = true, Content = $"{{\"error\": \"{ex.Message}\"}}" };
            }
        }

        private ToolResult SearchBuildings(string style, string buildingType, int topK, KnowledgeBaseManager kb)
        {
            try
            {
                if (!kb.Buildings.IsInitialized)
                {
                    kb.Buildings.Initialize();
                }

                var buildings = kb.Buildings.SearchByStyle(style, topK);

                if (buildings.Count == 0 && !string.IsNullOrEmpty(buildingType))
                {
                    buildings = kb.Buildings.SearchByStyle(buildingType, topK);
                }

                var result = new
                {
                    buildings = buildings.Select(b => new
                    {
                        id = b.id,
                        source = b.source,
                        dimensions = b.dimensions,
                        features = b.features,
                        style_tags = b.style_tags,
                        summary = b.summary,
                        materials_preview = b.materials?.primary_tiles?.Take(3)?.Select(t => t.name)?.ToList()
                    }),
                    total_count = buildings.Count,
                    search_criteria = new { style, building_type = buildingType },
                    message = buildings.Count > 0
                        ? $"找到{buildings.Count}个相似建筑。调用get_building_sequence获取详细建造顺序。"
                        : "未找到匹配建筑，使用默认设计流程。"
                };

                trab.Instance?.Logger.Info($"建筑实体检索: style={style}, type={buildingType}, found={buildings.Count}");

                return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
            }
            catch (Exception ex)
            {
                return new ToolResult { IsError = true, Content = $"{{\"error\": \"{ex.Message}\"}}" };
            }
        }

        private ToolResult GetBuildingSequence(string buildingId, KnowledgeBaseManager kb)
        {
            try
            {
                if (string.IsNullOrEmpty(buildingId))
                {
                    return new ToolResult { IsError = true, Content = "{\"error\": \"缺少building_id参数\"}" };
                }

                if (!kb.Buildings.IsInitialized)
                {
                    kb.Buildings.Initialize();
                }

                var building = kb.Buildings.GetBuilding(buildingId);
                if (building == null)
                {
                    return new ToolResult { IsError = true, Content = $"{{\"error\": \"未找到建筑: {buildingId}\"}}" };
                }

                var sequence = kb.Buildings.GetBuildingSequence(buildingId);
                var materials = kb.Buildings.GetMaterialList(buildingId);
                var detail = kb.Buildings.GetBuildingDetail(buildingId);

                string aiDescription = kb.Buildings.GetBuildingDescriptionForAI(buildingId);

                var result = new
                {
                    building_id = buildingId,
                    building_info = new
                    {
                        dimensions = building.dimensions,
                        features = building.features,
                        summary = building.summary,
                        style_tags = building.style_tags
                    },
                    tile_stats = detail != null ? new
                    {
                        total_tiles = detail.total_tiles,
                        active_tiles = detail.active_tiles,
                        unique_tile_types = detail.unique_tile_types,
                        unique_wall_types = detail.unique_wall_types,
                        tile_distribution = detail.tile_distribution?.OrderByDescending(t => t.Value).Take(15).ToDictionary(t => t.Key, t => t.Value),
                        wall_distribution = detail.wall_distribution?.OrderByDescending(t => t.Value).Take(10).ToDictionary(t => t.Key, t => t.Value)
                    } : null,
                    building_sequence = sequence?.Select(s => new
                    {
                        step = s.step,
                        action = s.action,
                        materials = s.materials,
                        note = s.note
                    }),
                    material_list = new
                    {
                        primary_tiles = materials.tiles?.Select(t => new { id = t.id, name = t.name, count = t.count }),
                        primary_walls = materials.walls?.Select(w => new { id = w.id, name = w.name, count = w.count })
                    },
                    ai_readable_description = aiDescription,
                    message = "建筑数据已加载。参考 tile_distribution 理解建筑组成，按 building_sequence 的顺序生成新建筑。"
                };

                trab.Instance?.Logger.Info($"获取建造顺序: building={buildingId}, steps={sequence?.Count ?? 0}, tiles={detail?.unique_tile_types ?? 0}种");

                return new ToolResult { IsError = false, Content = JsonConvert.SerializeObject(result) };
            }
            catch (Exception ex)
            {
                return new ToolResult { IsError = true, Content = $"{{\"error\": \"{ex.Message}\"}}" };
            }
        }

        #endregion

        #region JSON解析

        private BuildingDesign ParseBuildingDesign(string content, List<ToolCallRecord> toolCalls)
        {
            if (string.IsNullOrEmpty(content)) return null;
            string json = ExtractJson(content);
            if (json == null) return null;

            try
            {
                var design = JsonConvert.DeserializeObject<BuildingDesign>(json);
                if (design != null) design.ToolCalls = toolCalls;
                return design;
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"解析失败: {ex.Message}");
                return null;
            }
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

    #region 数据结构

    public class OpenAIResponse
    {
        public OpenAIChoice[] choices { get; set; }
    }

    public class OpenAIChoice
    {
        public OpenAIMessage message { get; set; }
        public string finish_reason { get; set; }
    }

    public class OpenAIMessage
    {
        public string role { get; set; }
        public string content { get; set; }
        public string reasoning_content { get; set; }
        public OpenAIToolCall[] tool_calls { get; set; }
    }

    public class OpenAIToolCall
    {
        public string id { get; set; }
        public string type { get; set; }
        public OpenAIFunction function { get; set; }
    }

    public class OpenAIFunction
    {
        public string name { get; set; }
        public string arguments { get; set; }
    }

    public class ClaudeAgentResponse
    {
        public string id { get; set; }
        public string type { get; set; }
        public string role { get; set; }
        public List<ClaudeContentItem> content { get; set; }
        public string model { get; set; }
        public string stop_reason { get; set; }
        public string stop_sequence { get; set; }
        public ClaudeUsage usage { get; set; }
    }

    public class ClaudeContentItem
    {
        public string type { get; set; }
        public string text { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public object input { get; set; }
    }

    public class ClaudeUsage
    {
        public int input_tokens { get; set; }
        public int output_tokens { get; set; }
    }

    public class ToolResult
    {
        public bool IsError { get; set; }
        public string Content { get; set; }
    }

    #endregion
}