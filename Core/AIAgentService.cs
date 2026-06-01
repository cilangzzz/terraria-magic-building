using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Terraria;
using Terraria.ModLoader;
using trab.Data;

namespace trab.Core
{
    /// <summary>
    /// AI Agent建筑生成服务 - 真工具调用实现
    /// </summary>
    public class AIAgentService
    {
        private HttpClient _httpClient;
        private string _apiKey;
        private string _apiEndpoint;
        private AIServiceType _serviceType;
        private string _modelName;

        private const int MAX_AGENT_ROUNDS = 5;

        private const string AGENT_SYSTEM_PROMPT = @"你是泰拉瑞亚建筑设计Agent。

## 工作流程
1. 理解用户需求，确定建筑风格和类型
2. 使用工具检索相关知识（方块、风格模板、家具、油漆）
3. 基于检索结果生成建筑设计JSON

## 工具使用规则
- 先调用 get_style_template 了解风格要求
- 再调用 search_tiles 获取合适方块
- 如需NPC房屋，调用 search_furniture
- 可调用 get_paint_scheme 获取油漆方案
- 最后直接输出JSON（不使用工具）

## 输出格式
生成JSON建筑设计，必须包含：
{
  ""name"": ""建筑名称"",
  ""width"": 10-30,
  ""height"": 8-20,
  ""style"": ""风格"",
  ""tiles"": [{""x"", ""y"", ""tile_id"", ""paint"", ""slope""}],
  ""walls"": [{""x"", ""y"", ""wall_id"", ""paint""}],
  ""wallRanges"": [{""x1"", ""y1"", ""x2"", ""y2"", ""wall_id"", ""paint""}],
  ""furniture"": [{""x"", ""y"", ""tile_id"", ""direction""}],
  ""doors"": [{""x"", ""y"", ""tile_id""}],
  ""lightSources"": [{""x"", ""y"", ""tile_id""}]
}

## 重要规则
1. 必须使用工具返回的精确 tile_id 和 wall_id
2. 应用推荐的 paint 方案增加层次（阴影paint=28）
3. 尺寸控制在合理范围，不要太大型建筑
4. 使用 wallRanges 批量填充内部墙壁，减少token
5. 只输出JSON，不要额外解释";

        public AIAgentService(string apiKey, AIServiceType serviceType = AIServiceType.DeepSeek, string modelName = "deepseek-chat")
        {
            _apiKey = apiKey;
            _serviceType = serviceType;
            _modelName = modelName;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(120);
            ConfigureApiClient();
        }

        private void ConfigureApiClient()
        {
            _httpClient.DefaultRequestHeaders.Clear();

            if (_serviceType == AIServiceType.Claude)
            {
                _apiEndpoint = "https://api.anthropic.com/v1/messages";
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            }
            else if (_serviceType == AIServiceType.DeepSeek)
            {
                // DeepSeek Anthropic兼容端点（支持工具调用）
                _apiEndpoint = "https://api.deepseek.com/anthropic/v1/messages";
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            }
            else if (_serviceType == AIServiceType.DashScope)
            {
                // DashScope Anthropic兼容端点
                _apiEndpoint = "https://dashscope.aliyuncs.com/compatible-mode/v1/messages";
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            }
            else
            {
                // OpenAI格式（需要转换）
                _apiEndpoint = "https://api.openai.com/v1/chat/completions";
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);
            }
        }

        /// <summary>
        /// Agent主入口 - 真工具调用循环
        /// </summary>
        public async Task<BuildingDesign> GenerateBuildingAsync(
            string userPrompt,
            Action<string, int> progressCallback = null,
            CancellationToken ct = default)
        {
            try
            {
                progressCallback?.Invoke("Agent启动...", 0);

                // 初始化知识库
                KnowledgeBaseManager.Instance.Initialize();
                var kb = KnowledgeBaseManager.Instance;

                // 消息队列
                var messages = new List<object>();

                // 用户请求
                messages.Add(new { role = "user", content = userPrompt });

                // 工具调用记录
                var toolCallRecords = new List<ToolCallRecord>();

                // Agent循环
                for (int round = 1; round <= MAX_AGENT_ROUNDS; round++)
                {
                    progressCallback?.Invoke($"[轮次{round}]思考中...", round);

                    // 构建请求
                    var requestBody = BuildAgentRequest(messages);
                    var response = await SendRequestAsync(requestBody, ct);

                    if (response == null)
                    {
                        progressCallback?.Invoke("API请求失败", 0);
                        return null;
                    }

                    // 检查停止原因
                    var stopReason = response.stop_reason;

                    if (stopReason == "end_turn" || stopReason == "stop_sequence")
                    {
                        // AI完成 - 提取最终JSON
                        progressCallback?.Invoke($"[轮次{round}]生成完成", round);

                        var textContent = ExtractTextContent(response.content);
                        if (textContent != null)
                        {
                            var design = ParseBuildingDesign(textContent, toolCallRecords);

                            // 统计信息
                            int totalToolCalls = toolCallRecords.Count;
                            progressCallback?.Invoke($"完成({round}轮,{totalToolCalls}次工具调用)", 0);

                            return design;
                        }
                        return null;
                    }

                    if (stopReason == "tool_use")
                    {
                        // AI调用工具
                        // 1. 添加assistant消息
                        messages.Add(new { role = "assistant", content = response.content });

                        // 2. 执行工具并收集结果
                        var toolResults = new List<ToolResultItem>();

                        foreach (var contentItem in response.content)
                        {
                            if (contentItem.type == "tool_use")
                            {
                                string toolName = contentItem.name?.ToString();
                                string toolId = contentItem.id?.ToString();
                                var toolInput = contentItem.input as JObject;

                                progressCallback?.Invoke($"[轮次{round}]调用工具: {toolName}", round);

                                // 执行工具
                                var result = ExecuteTool(toolName, toolInput, kb);

                                // 记录
                                toolCallRecords.Add(new ToolCallRecord
                                {
                                    ToolName = toolName,
                                    Input = toolInput,
                                    Output = result.Content,
                                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                });

                                // 添加tool_result
                                toolResults.Add(new ToolResultItem
                                {
                                    type = "tool_result",
                                    tool_use_id = toolId,
                                    content = result.Content,
                                    is_error = result.IsError
                                });
                            }
                        }

                        // 3. 添加tool_result消息
                        messages.Add(new { role = "user", content = toolResults });

                        continue;
                    }

                    // 其他停止原因（max_tokens等）
                    progressCallback?.Invoke($"停止: {stopReason}", 0);
                    break;
                }

                progressCallback?.Invoke("超过最大轮数限制", 0);
                return null;
            }
            catch (Exception ex)
            {
                progressCallback?.Invoke($"错误: {ex.Message}", 0);
                trab.Instance?.Logger.Error($"Agent错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 构建Agent请求（带工具定义）
        /// </summary>
        private object BuildAgentRequest(List<object> messages)
        {
            return new
            {
                model = _modelName,
                max_tokens = 4096,
                system = AGENT_SYSTEM_PROMPT,
                messages = messages,
                tools = GetToolDefinitions()
            };
        }

        /// <summary>
        /// 工具定义
        /// </summary>
        private List<object> GetToolDefinitions()
        {
            return new List<object>
            {
                // 工具1：方块搜索
                new
                {
                    name = "search_tiles",
                    description = "搜索方块类型，返回tile_id、名称、属性。用于确定建筑材料。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            style = new { type = "string", description = "风格: medieval, fantasy, natural, steampunk, asian, snow, desert, modern, dark, underground, ocean" },
                            category = new { type = "string", description = "类别: basic, wood, brick, slab, luxury, special, platform, furniture, light, door, decoration" },
                            biome = new { type = "string", description = "生物群落匹配: forest, desert, snow, jungle, ocean, underground, hallow, corruption, crimson" }
                        },
                        required = new[] { "style" }
                    }
                },

                // 工具2：风格模板
                new
                {
                    name = "get_style_template",
                    description = "获取建筑风格模板，包含推荐方块、油漆方案、建筑规则。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            style = new { type = "string", description = "风格名称: medieval, fantasy, steampunk, natural, asian, snow, desert, underground, ocean, dark, modern" },
                            building_type = new { type = "string", description = "建筑类型: house, castle, tower, shop, temple, workshop, farmhouse" }
                        },
                        required = new[] { "style" }
                    }
                },

                // 工具3：家具搜索
                new
                {
                    name = "search_furniture",
                    description = "搜索家具及其NPC房屋功能。返回tile_id、尺寸、放置规则。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            room_type = new { type = "string", description = "房间类型: bedroom, workshop, shop, storage, kitchen" },
                            npc_type = new { type = "string", description = "目标NPC类型（可选）" }
                        }
                    }
                },

                // 工具4：油漆方案
                new
                {
                    name = "get_paint_scheme",
                    description = "获取推荐的油漆颜色方案，包含主色、阴影、高光。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            style = new { type = "string", description = "风格名称" },
                            theme = new { type = "string", description = "主题: warm, cold, dark, bright, natural" }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 执行工具
        /// </summary>
        private ToolResult ExecuteTool(string name, JObject input, KnowledgeBaseManager kb)
        {
            try
            {
                switch (name)
                {
                    case "search_tiles":
                        return SearchTiles(
                            input["style"]?.ToString(),
                            input["category"]?.ToString(),
                            input["biome"]?.ToString(),
                            kb
                        );

                    case "get_style_template":
                        return GetStyleTemplate(
                            input["style"]?.ToString(),
                            input["building_type"]?.ToString(),
                            kb
                        );

                    case "search_furniture":
                        return SearchFurniture(
                            input["room_type"]?.ToString(),
                            input["npc_type"]?.ToString(),
                            kb
                        );

                    case "get_paint_scheme":
                        return GetPaintScheme(
                            input["style"]?.ToString(),
                            input["theme"]?.ToString(),
                            kb
                        );

                    default:
                        return new ToolResult { IsError = true, Content = "{\"error\": \"未知工具\"}" };
                }
            }
            catch (Exception ex)
            {
                return new ToolResult { IsError = true, Content = $"{{\"error\": \"{ex.Message}\"}}" };
            }
        }

        #region 工具实现

        private ToolResult SearchTiles(string style, string category, string biome, KnowledgeBaseManager kb)
        {
            var tiles = kb.Tiles.SearchTiles(style, category, biome);

            // 限制返回数量，避免token过多
            var limitedTiles = tiles.Take(20).ToList();

            var result = new
            {
                tiles = limitedTiles.Select(t => new
                {
                    id = t.id,
                    name = t.name,
                    display_name = t.display_name,
                    category = t.category,
                    paint_compatible = t.paint_compatible,
                    slope_compatible = t.slope_compatible
                }),
                total_count = tiles.Count,
                returned_count = limitedTiles.Count,
                search_criteria = new { style, category, biome }
            };

            return new ToolResult
            {
                IsError = false,
                Content = JsonConvert.SerializeObject(result)
            };
        }

        private ToolResult GetStyleTemplate(string style, string buildingType, KnowledgeBaseManager kb)
        {
            var template = kb.Styles.GetTemplate(style, buildingType);

            if (template == null)
            {
                return new ToolResult
                {
                    IsError = true,
                    Content = $"{{\"error\": \"未找到风格模板: {style}\"}}"
                };
            }

            // 获取推荐的油漆方案
            var paintScheme = kb.Tiles.GetPaintRecommendation(style);

            var result = new
            {
                style = style,
                name = template.name,
                display_name = template.display_name,
                description = template.description,
                paint_scheme = new
                {
                    primary = paintScheme.PrimaryPaint,
                    shadow = paintScheme.ShadowPaint,
                    highlight = paintScheme.HighlightPaint,
                    description = paintScheme.Description
                }
            };

            return new ToolResult
            {
                IsError = false,
                Content = JsonConvert.SerializeObject(result)
            };
        }

        private ToolResult SearchFurniture(string roomType, string npcType, KnowledgeBaseManager kb)
        {
            var furniture = kb.Furniture.SearchFurniture(roomType, npcType);

            // NPC房屋必需家具
            var npcRequirements = new
            {
                valid_house_requires = new[] { "light_source", "flat_surface", "comfort", "door" },
                light_sources = new[] { "Torches (id=4)", "Candles (id=33)", "Chandeliers (id=35)" },
                flat_surfaces = new[] { "WorkBench (id=17)", "Tables (id=87)", "Dressers (id=104)" },
                comfort_items = new[] { "Chairs (id=88)", "Beds (id=89)", "Benches" },
                doors = new[] { "ClosedDoor (id=11)", "Trapdoor (id=10)" }
            };

            var result = new
            {
                furniture = furniture.Select(f => new
                {
                    name = f.Key,
                    tile_id = f.Value.tile_id,
                    display_name = f.Value.display_name,
                    width = f.Value.width,
                    height = f.Value.height
                }),
                npc_requirements = npcRequirements
            };

            return new ToolResult
            {
                IsError = false,
                Content = JsonConvert.SerializeObject(result)
            };
        }

        private ToolResult GetPaintScheme(string style, string theme, KnowledgeBaseManager kb)
        {
            var paintScheme = kb.Tiles.GetPaintRecommendation(style);

            // 额外的油漆信息
            var paints = kb.Tiles.GetAllPaints();

            var result = new
            {
                style = style,
                theme = theme ?? "default",
                primary_paint = paintScheme.PrimaryPaint,
                shadow_paint = paintScheme.ShadowPaint,
                highlight_paint = paintScheme.HighlightPaint,
                description = paintScheme.Description,
                available_paints = paints.Take(10).Select(p => new
                {
                    id = p.id,
                    name = p.name,
                    display_name = p.display_name
                })
            };

            return new ToolResult
            {
                IsError = false,
                Content = JsonConvert.SerializeObject(result)
            };
        }

        #endregion

        #region API请求

        private async Task<ClaudeAgentResponse> SendRequestAsync(object requestBody, CancellationToken ct)
        {
            string json = JsonConvert.SerializeObject(requestBody);
            trab.Instance?.Logger.Info($"Agent请求: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_apiEndpoint, content, ct);
            string responseJson = await response.Content.ReadAsStringAsync(ct);

            trab.Instance?.Logger.Info($"Agent响应: {responseJson}");

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
                {
                    return item.text;
                }
            }
            return null;
        }

        #endregion

        #region JSON解析

        private BuildingDesign ParseBuildingDesign(string content, List<ToolCallRecord> toolCalls)
        {
            if (string.IsNullOrEmpty(content)) return null;

            // 提取JSON
            string json = ExtractJson(content);
            if (json == null) return null;

            try
            {
                var design = JsonConvert.DeserializeObject<BuildingDesign>(json);
                if (design != null)
                {
                    design.ToolCalls = toolCalls;
                }
                return design;
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"解析建筑设计失败: {ex.Message}");
                return null;
            }
        }

        private string ExtractJson(string content)
        {
            // 尝试提取 ```json 代码块
            int start = content.IndexOf("```json");
            if (start >= 0)
            {
                start += 7;
                int end = content.IndexOf("```", start);
                if (end > start)
                    return content.Substring(start, end - start).Trim();
            }

            // 尝试提取 {} 之间的内容
            int braceStart = content.IndexOf('{');
            int braceEnd = content.LastIndexOf('}');
            if (braceStart >= 0 && braceEnd > braceStart)
                return content.Substring(braceStart, braceEnd - braceStart + 1);

            return null;
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// Claude Agent响应结构
    /// </summary>
    public class ClaudeAgentResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public List<ClaudeContentItem> content { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("stop_reason")]
        public string stop_reason { get; set; }

        [JsonProperty("stop_sequence")]
        public string stop_sequence { get; set; }

        [JsonProperty("usage")]
        public ClaudeUsage usage { get; set; }
    }

    /// <summary>
    /// Claude内容项
    /// </summary>
    public class ClaudeContentItem
    {
        [JsonProperty("type")]
        public string type { get; set; }

        [JsonProperty("text")]
        public string text { get; set; }

        [JsonProperty("id")]
        public string id { get; set; }

        [JsonProperty("name")]
        public string name { get; set; }

        [JsonProperty("input")]
        public object input { get; set; }
    }

    /// <summary>
    /// Claude用量
    /// </summary>
    public class ClaudeUsage
    {
        [JsonProperty("input_tokens")]
        public int input_tokens { get; set; }

        [JsonProperty("output_tokens")]
        public int output_tokens { get; set; }
    }

    /// <summary>
    /// 工具结果项（返回给API）
    /// </summary>
    public class ToolResultItem
    {
        [JsonProperty("type")]
        public string type { get; set; } = "tool_result";

        [JsonProperty("tool_use_id")]
        public string tool_use_id { get; set; }

        [JsonProperty("content")]
        public string content { get; set; }

        [JsonProperty("is_error")]
        public bool is_error { get; set; } = false;
    }

    /// <summary>
    /// 工具执行结果
    /// </summary>
    public class ToolResult
    {
        public bool IsError { get; set; }
        public string Content { get; set; }
    }

    #endregion
}