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
using trab.Config;
using trab.Data;

namespace trab.Core
{
    /// <summary>
    /// AI Agent建筑生成服务 - 支持OpenAI和Anthropic两种格式
    /// </summary>
    public class AIAgentService
    {
        private HttpClient _httpClient;
        private string _apiKey;
        private string _apiEndpoint;
        private AIServiceType _serviceType;
        private string _modelName;
        private bool _useOpenAIFormat;  // 是否使用OpenAI格式

        private const int MAX_AGENT_ROUNDS = 10;  // 增加轮数限制，让AI有足够时间思考

        // 规划Agent系统提示 - 用于区域划分（简洁格式避免截断）
        private const string PLANNER_SYSTEM_PROMPT = @"建筑区域规划Agent。输出紧凑JSON，只包含必要信息。

## 输出格式（极简）
{""type"":""two_story"", ""w"":12, ""h"":14, ""style"":""medieval"", ""roof"":""gable"", ""floors"":2, ""main_block"":""brick"", ""roof_block"":""slab"", ""floor_block"":""wood""}

## 参数说明
- type: house/two_story/tower/castle
- w: 宽度(6-20)
- h: 高度(6-16)
- style: medieval/fantasy/natural/modern
- roof: gable/flat/dome/pagoda
- floors: 楼层数(1-4)
- main_block: 主要方块类别(brick/wood/slab/luxury)
- roof_block: 屋顶方块类别(slab/brick)
- floor_block: 地板方块类别(wood/brick)

只输出一行JSON，无解释。";

        private const string AGENT_SYSTEM_PROMPT = @"泰拉瑞亚建筑设计Agent。生成有设计感的建筑JSON。

## 重要概念区分
- **tile_id**: 方块ID（放置在世界中的实体方块），如：4=灰砖、5=木材、10=门、143=石板
- **wall_id**: 墙壁背景ID（背景墙），如：4=木墙、6=灰砖墙、14=玻璃墙
- 注意：tile_id和wall_id是不同的系统！门(tile_id=10)不是墙壁！

## 推荐流程（必须按顺序执行）
1. analyze_requirement - 分析用户需求，确定建筑类型、风格、材料类别
2. search_tiles(style=风格,category=main_block) - 检索主要方块候选
3. search_tiles(style=风格,category=roof_block) - 检索屋顶方块候选
4. search_tiles(style=风格,category=floor_block) - 检索地板方块候选
5. search_walls(style=风格) - 检索墙壁候选（返回wall_id，不是tile_id）
6. search_furniture(room_type=light/surface/comfort/door) - 检索家具候选
7. select_materials - 从候选中选择最合适的材料ID（必须调用！）
8. 输出完整JSON（必须使用select_materials返回的ID！）

## 重要规则
- select_materials返回的ID是最终使用的材料，JSON中的tile_id/wall_id必须使用这些ID
- 不要使用默认ID（如石头1、灰砖4），必须使用select_materials选定的ID
- main_wall_id必须是有效的wall_id（4=木墙、6=灰砖墙、14=玻璃墙），不能用tile_id

## JSON格式（必须完整输出，使用选定的材料ID）
{
""name"": ""中世纪双层小屋"",
""width"": 12,
""height"": 14,
""style"": ""medieval"",
""tiles"": [{""x"":0,""y"":0,""tile_id"":【main_block_id】,""slope"":0}],
""wallRanges"": [{""x1"":1,""y1"":1,""x2"":11,""y2"":13,""wall_id"":【main_wall_id】}],
""furniture"": [{""x"":3,""y"":6,""tile_id"":【surface_id】}],
""doors"": [{""x"":5,""y"":1,""tile_id"":【door_id】}],
""lightSources"": [{""x"":2,""y"":3,""tile_id"":【light_id】}]
}

## 设计要点
- 屋顶要有形状：人字形(gable)用斜坡方块实现三角上升
- 窗户对称布局：每层左右各1-2个窗户
- 楼层区分：不同楼层使用不同方块或油漆
- 墙壁厚度：城堡类建筑墙壁厚度=2";

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
                // Claude 原生 Anthropic 端点
                _apiEndpoint = "https://api.anthropic.com/v1/messages";
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                _useOpenAIFormat = false;
            }
            else if (_serviceType == AIServiceType.DeepSeek)
            {
                // DeepSeek OpenAI 兼容端点（支持工具调用）
                _apiEndpoint = "https://api.deepseek.com/v1/chat/completions";
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);
                _useOpenAIFormat = true;
            }
            else if (_serviceType == AIServiceType.DashScope)
            {
                // DashScope Anthropic兼容端点
                _apiEndpoint = "https://dashscope.aliyuncs.com/compatible-mode/v1/messages";
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                _useOpenAIFormat = false;
            }
            else
            {
                // OpenAI格式
                _apiEndpoint = "https://api.openai.com/v1/chat/completions";
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);
                _useOpenAIFormat = true;
            }
        }

        /// <summary>
        /// Agent主入口 - 根据配置选择SingleAgent或MultiAgent模式
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

                // 根据配置选择生成模式
                var config = ModContent.GetInstance<AIBuildingConfig>();

                // Pipeline模式：使用新的4阶段流程（需求分析→材料检索→材料选择→设计生成）
                if (config.UsePipelineMode)
                {
                    trab.Instance?.Logger.Info("使用Pipeline模式生成建筑（4阶段流程）");
                    progressCallback?.Invoke("Pipeline模式启动...", 0);
                    return await GenerateBuildingSingleAgentAsync(userPrompt, progressCallback, ct);
                }

                if (config.AgentGenerationMode == AgentMode.SingleAgent)
                {
                    trab.Instance?.Logger.Info("使用SingleAgent模式生成建筑");
                    return await GenerateBuildingSingleAgentAsync(userPrompt, progressCallback, ct);
                }
                else
                {
                    trab.Instance?.Logger.Info("使用MultiAgent协作模式生成建筑");
                    return await GenerateBuildingMultiAgentAsync(userPrompt, progressCallback, ct);
                }
            }
            catch (Exception ex)
            {
                progressCallback?.Invoke($"错误: {ex.Message}", 0);
                trab.Instance?.Logger.Error($"Agent错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 多Agent协作模式 - 规划 + 模块生成 + 合并
        /// </summary>
        public async Task<BuildingDesign> GenerateBuildingMultiAgentAsync(
            string userPrompt,
            Action<string, int> progressCallback = null,
            CancellationToken ct = default)
        {
            progressCallback?.Invoke("[阶段1]规划建筑区域...", 1);

            // 阶段1：规划Agent - 划分区域
            var plan = await PlanBuildingAsync(userPrompt, ct);
            if (plan == null)
            {
                progressCallback?.Invoke("规划失败，使用默认方案", 0);
                plan = CreateDefaultPlan(userPrompt);
            }

            progressCallback?.Invoke($"规划完成: {plan.BuildingType}, {plan.Width}x{plan.Height}", 1);

            // 阶段2：模块生成Agent（并行）
            progressCallback?.Invoke("[阶段2]生成各模块...", 2);
            var moduleAgents = new ModuleAgents(_apiKey, _serviceType, _modelName);

            var tasks = new List<Task<ModuleResult>>();

            // 并行生成5个模块
            tasks.Add(moduleAgents.GenerateRoofAsync(plan, s => progressCallback?.Invoke(s, 2), ct));
            tasks.Add(moduleAgents.GenerateWallsAsync(plan, s => progressCallback?.Invoke(s, 2), ct));
            tasks.Add(moduleAgents.GenerateFloorsAsync(plan, s => progressCallback?.Invoke(s, 2), ct));
            tasks.Add(moduleAgents.GenerateWindowsAsync(plan, s => progressCallback?.Invoke(s, 2), ct));
            tasks.Add(moduleAgents.GenerateFurnitureAsync(plan, s => progressCallback?.Invoke(s, 2), ct));

            var results = await Task.WhenAll(tasks);
            var modules = results.ToList();

            progressCallback?.Invoke($"模块生成完成: {modules.Count(m => !m.IsError)}/{modules.Count}成功", 2);

            // 阶段3：合并
            progressCallback?.Invoke("[阶段3]合并模块...", 3);
            var merger = new BuildingMerger();
            var design = merger.Merge(plan, modules);

            progressCallback?.Invoke($"完成: {design?.Tiles?.Count ?? 0}方块", 0);
            return design;
        }

        /// <summary>
        /// 规划Agent - 划分建筑区域
        /// </summary>
        private async Task<BuildingPlan> PlanBuildingAsync(string userPrompt, CancellationToken ct)
        {
            var requestBody = new
            {
                model = _modelName,
                messages = new[]
                {
                    new { role = "system", content = PLANNER_SYSTEM_PROMPT },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = 1024  // 规划JSON很小
            };

            string json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiEndpoint, content, ct);
            string responseJson = await response.Content.ReadAsStringAsync(ct);

            trab.Instance?.Logger.Info($"规划Agent响应: {responseJson}");

            if (!response.IsSuccessStatusCode)
            {
                trab.Instance?.Logger.Error($"规划API错误: {responseJson}");
                return null;
            }

            var apiResponse = JsonConvert.DeserializeObject<OpenAIResponse>(responseJson);
            var messageContent = apiResponse.choices?[0]?.message?.content;

            if (string.IsNullOrEmpty(messageContent))
                return null;

            return ParseBuildingPlan(messageContent);
        }

        /// <summary>
        /// 解析区域规划JSON（简化格式）
        /// </summary>
        private BuildingPlan ParseBuildingPlan(string content)
        {
            string json = ExtractJson(content);
            if (json == null) return null;

            try
            {
                // 解析简化格式
                var simplePlan = JsonConvert.DeserializeObject<SimpleBuildingPlan>(json);
                if (simplePlan == null) return null;

                // 转换为完整BuildingPlan
                var plan = new BuildingPlan
                {
                    BuildingType = simplePlan.Type ?? "house",
                    Width = Math.Clamp(simplePlan.W ?? 10, 6, 20),
                    Height = Math.Clamp(simplePlan.H ?? 8, 6, 16),
                    Style = simplePlan.Style ?? "medieval"
                };

                // 根据楼层数自动生成区域划分
                GenerateDefaultRegions(plan, simplePlan.Roof ?? "gable", simplePlan.Floors ?? 1);

                trab.Instance?.Logger.Info($"规划解析成功: {plan.BuildingType}, {plan.Width}x{plan.Height}, {plan.Regions?.Count ?? 0}区域");
                return plan;
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"规划解析失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 根据规划生成默认区域划分
        /// </summary>
        private void GenerateDefaultRegions(BuildingPlan plan, string roofType, int floors)
        {
            plan.Regions = new List<Region>();

            // 计算楼层高度分配
            int roofHeight = 2;
            int floorHeight = (plan.Height - roofHeight) / Math.Max(1, floors);

            // 屋顶区域
            plan.Regions.Add(new Region
            {
                Name = "roof",
                Type = roofType,
                YRange = new[] { 0, roofHeight - 1 }
            });

            // 楼层区域
            for (int i = 1; i <= floors; i++)
            {
                int yStart = roofHeight + (i - 1) * floorHeight;
                int yEnd = yStart + floorHeight - 1;
                if (i == floors) yEnd = plan.Height - 1;  // 最后一层到底部

                plan.Regions.Add(new Region
                {
                    Name = $"floor{i}",
                    YRange = new[] { yStart, yEnd }
                });

                // 每层添加窗户（对称布局）
                int windowY = yStart + floorHeight / 2;
                plan.Regions.Add(new Region
                {
                    Name = "windows",
                    Windows = new List<WindowPosition>
                    {
                        new WindowPosition { X = 2, Y = windowY, Width = 2, Height = 2, Type = "double" },
                        new WindowPosition { X = plan.Width - 4, Y = windowY, Width = 2, Height = 2, Type = "double" }
                    }
                });

                // 每层添加家具
                plan.Regions.Add(new Region
                {
                    Name = "furniture",
                    Furnitures = new List<FurniturePosition>
                    {
                        new FurniturePosition { X = 3, Y = yEnd, Type = "workbench", Floor = i },
                        new FurniturePosition { X = 5, Y = yEnd, Type = "table", Floor = i },
                        new FurniturePosition { X = 6, Y = yEnd, Type = "chair", Floor = i },
                        new FurniturePosition { X = plan.Width / 2, Y = yEnd, Type = "torch", Floor = i }
                    }
                });
            }

            // 墙壁区域
            plan.Regions.Add(new Region
            {
                Name = "walls",
                Thickness = plan.BuildingType == "castle" ? 2 : 1
            });
        }

        /// <summary>
        /// 创建默认规划方案
        /// </summary>
        private BuildingPlan CreateDefaultPlan(string userPrompt)
        {
            var plan = new BuildingPlan
            {
                BuildingType = "house",
                Width = 10,
                Height = 8,
                Style = "medieval"
            };

            GenerateDefaultRegions(plan, "gable", 1);
            return plan;
        }

        // 简化规划格式数据结构
        private class SimpleBuildingPlan
        {
            public string Type { get; set; }
            public int? W { get; set; }
            public int? H { get; set; }
            public string Style { get; set; }
            public string Roof { get; set; }
            public int? Floors { get; set; }
        }

        // 保留原有单Agent流程作为备选
        public async Task<BuildingDesign> GenerateBuildingSingleAgentAsync(
            string userPrompt,
            Action<string, int> progressCallback = null,
            CancellationToken ct = default)
        {
            var kb = KnowledgeBaseManager.Instance;
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
            // OpenAI消息格式
            var messages = new List<object>
            {
                new { role = "system", content = AGENT_SYSTEM_PROMPT },
                new { role = "user", content = userPrompt }
            };

            int round = 0;
            while (true)  // 无限循环，直到AI输出结果或出错
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

                // 检查是否有工具调用
                var toolCalls = message.tool_calls;
                if (toolCalls != null && toolCalls.Length > 0)
                {
                    // 添加assistant消息（包含tool_calls）
                    messages.Add(new
                    {
                        role = "assistant",
                        content = message.content ?? "",
                        tool_calls = toolCalls
                    });

                    // 执行每个工具调用
                    foreach (var toolCall in toolCalls)
                    {
                        string toolName = toolCall.function?.name;
                        string toolCallId = toolCall.id;
                        var toolArgs = toolCall.function?.arguments;

                        progressCallback?.Invoke($"[轮次{round}]调用工具: {toolName}", round);

                        // 解析参数
                        JObject argsObj = null;
                        try
                        {
                            argsObj = JObject.Parse(toolArgs ?? "{}");
                        }
                        catch { argsObj = new JObject(); }

                        // 执行工具
                        var result = ExecuteTool(toolName, argsObj, kb);

                        toolCallRecords.Add(new ToolCallRecord
                        {
                            ToolName = toolName,
                            Input = argsObj,
                            Output = result.Content,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        });

                        // 添加tool结果消息
                        messages.Add(new
                        {
                            role = "tool",
                            tool_call_id = toolCallId,
                            content = result.Content
                        });
                    }

                    continue;
                }

                // 无工具调用，获取最终结果
                if (!string.IsNullOrEmpty(message.content))
                {
                    progressCallback?.Invoke($"[轮次{round}]生成完成", round);
                    var design = ParseBuildingDesign(message.content, toolCallRecords);
                    progressCallback?.Invoke($"完成({round}轮,{toolCallRecords.Count}次工具调用)", 0);
                    return design;
                }

                // content为空但没有工具调用，可能是API异常
                progressCallback?.Invoke($"[轮次{round}]响应异常，继续...", round);
                // 继续下一轮尝试
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
                max_tokens = 32768  // DeepSeek支持最大384K，设置32K足够大型建筑
            };
        }

        private List<object> GetOpenAIToolDefinitions()
        {
            return new List<object>
            {
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "search_tiles",
                        description = "搜索方块类型，返回tile_id、名称、属性。用于确定建筑材料。",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                style = new { type = "string", description = "风格: medieval, fantasy, natural, steampunk, asian, snow, desert, modern, dark" },
                                category = new { type = "string", description = "类别: basic, wood, brick, slab, luxury, furniture, light, door" },
                                biome = new { type = "string", description = "生物群落: forest, desert, snow, jungle, ocean, underground" }
                            },
                            required = new[] { "style" }
                        }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "search_walls",
                        description = "搜索墙壁类型，返回wall_id、名称、属性。用于确定建筑墙壁材料。",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                style = new { type = "string", description = "风格: medieval, fantasy, natural, steampunk, asian, snow, desert, modern, dark" },
                                category = new { type = "string", description = "类别: natural, wood, brick, luxury, desert, snow" }
                            },
                            required = new[] { "style" }
                        }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "get_style_template",
                        description = "获取建筑风格模板，包含推荐方块、油漆方案、建筑规则。",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                style = new { type = "string", description = "风格名称" },
                                building_type = new { type = "string", description = "建筑类型: house, castle, tower, shop, temple" }
                            },
                            required = new[] { "style" }
                        }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "search_furniture",
                        description = "搜索家具及其NPC房屋功能。返回tile_id、尺寸、放置规则。支持按类别向量检索。",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                room_type = new { type = "string", description = "房间类型/家具类别: light(光源), surface(桌面), comfort(舒适), storage(存储), door(门), decoration(装饰)" },
                                npc_type = new { type = "string", description = "目标NPC类型（可选）" }
                            }
                        }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "get_paint_scheme",
                        description = "获取推荐的油漆颜色方案。",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                style = new { type = "string", description = "风格名称" },
                                theme = new { type = "string", description = "主题: warm, cold, dark, bright" }
                            }
                        }
                    }
                },
                // 建筑构件工具
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "get_roof_template",
                        description = "获取屋顶设计模板，返回形状描述和推荐方块。用于设计建筑屋顶结构。",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                roof_type = new { type = "string", description = "屋顶类型: gable(人字形), flat(平顶), dome(圆顶), pagoda(宝塔), stepped(阶梯)" },
                                style = new { type = "string", description = "建筑风格: medieval, fantasy, natural..." },
                                width = new { type = "integer", description = "建筑宽度，用于计算屋顶高度" }
                            },
                            required = new[] { "roof_type", "width" }
                        }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "get_window_template",
                        description = "获取窗户设计模板，返回尺寸和方块ID。用于设计窗户布局。",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                window_type = new { type = "string", description = "窗户类型: single(单窗), double(双窗), arched(拱窗), bay(凸窗)" },
                                style = new { type = "string", description = "建筑风格" }
                            },
                            required = new[] { "window_type" }
                        }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "get_floor_structure",
                        description = "获取楼层结构模板，返回楼层数、高度、墙壁厚度。用于设计建筑骨架。",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                structure_type = new { type = "string", description = "结构类型: single_story(单层), two_story(双层), tower(塔楼), castle(城堡)" },
                                style = new { type = "string", description = "建筑风格" }
                            },
                            required = new[] { "structure_type" }
                        }
                    }
                },
                // 新增：需求分析工具
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "analyze_requirement",
                        description = "分析用户建筑需求，提取建筑类型、风格、材料类别等关键信息。这是建筑生成的第一步。",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                building_type = new { type = "string", description = "建筑类型: house, tower, castle, shop, temple" },
                                style = new { type = "string", description = "风格: medieval, fantasy, natural, steampunk, asian, snow, desert, modern, dark" },
                                biome = new { type = "string", description = "生物群落: forest, desert, snow, jungle, ocean, underground" },
                                floor_count = new { type = "integer", description = "楼层数(1-4)" },
                                width = new { type = "integer", description = "建筑宽度(6-20)" },
                                height = new { type = "integer", description = "建筑高度(6-16)" },
                                main_block_category = new { type = "string", description = "主要方块类别: basic, wood, brick, slab, luxury" },
                                roof_block_category = new { type = "string", description = "屋顶方块类别: slab, brick" },
                                floor_block_category = new { type = "string", description = "地板方块类别: wood, brick" },
                                main_wall_category = new { type = "string", description = "主要墙壁类别: natural, wood, brick" },
                                need_npc_house = new { type = "boolean", description = "是否需要NPC房屋功能" },
                                reasoning = new { type = "string", description = "分析推理说明" }
                            },
                            required = new[] { "building_type", "style" }
                        }
                    }
                },
                // 新增：材料选择工具
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "select_materials",
                        description = "从检索到的材料候选中选择最合适的材料ID。注意：wall_id是墙壁背景ID(如4=木墙、6=灰砖墙、14=玻璃墙)，tile_id是方块ID(如4=灰砖、10=门)。两者不同！",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                main_block_id = new { type = "integer", description = "主要方块tile_id（方块ID，如4=灰砖、5=木材）" },
                                secondary_block_id = new { type = "integer", description = "次要方块tile_id（可选）" },
                                roof_block_id = new { type = "integer", description = "屋顶方块tile_id（如143=石板）" },
                                floor_block_id = new { type = "integer", description = "地板方块tile_id（如5=木材、19=平台）" },
                                main_wall_id = new { type = "integer", description = "墙壁背景wall_id（注意：这是wall_id不是tile_id！如4=木墙、6=灰砖墙、14=玻璃墙。不要用10，10是门的tile_id不是墙！）" },
                                light_id = new { type = "integer", description = "光源家具tile_id（如4=火把、34=吊灯）" },
                                surface_id = new { type = "integer", description = "桌面家具tile_id（如17=工作台、87=桌子）" },
                                comfort_id = new { type = "integer", description = "舒适家具tile_id（如88=椅子、89=床）" },
                                door_id = new { type = "integer", description = "门家具tile_id（如10=木门、11=玻璃门）" },
                                primary_paint = new { type = "integer", description = "主色调油漆ID(0-31)" },
                                shadow_paint = new { type = "integer", description = "阴影油漆ID(默认28)" },
                                reasoning = new { type = "string", description = "选择理由说明" }
                            },
                            required = new[] { "main_block_id", "roof_block_id", "floor_block_id", "main_wall_id" }
                        }
                    }
                }
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
                    // 添加assistant消息
                    messages.Add(JObject.FromObject(new { role = "assistant", content = response.content }));

                    // 执行工具
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
                new
                {
                    name = "search_tiles",
                    description = "搜索方块类型，返回tile_id、名称、属性。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            style = new { type = "string" },
                            category = new { type = "string" },
                            biome = new { type = "string" }
                        },
                        required = new[] { "style" }
                    }
                },
                new
                {
                    name = "search_walls",
                    description = "搜索墙壁类型，返回wall_id、名称、属性。用于确定建筑墙壁材料。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            style = new { type = "string" },
                            category = new { type = "string" }
                        },
                        required = new[] { "style" }
                    }
                },
                new
                {
                    name = "get_style_template",
                    description = "获取建筑风格模板。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            style = new { type = "string" },
                            building_type = new { type = "string" }
                        },
                        required = new[] { "style" }
                    }
                },
                new
                {
                    name = "search_furniture",
                    description = "搜索家具及NPC房屋功能。支持按类别向量检索。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            room_type = new { type = "string", description = "家具类别: light, surface, comfort, storage, door, decoration" },
                            npc_type = new { type = "string" }
                        }
                    }
                },
                new
                {
                    name = "get_paint_scheme",
                    description = "获取油漆颜色方案。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            style = new { type = "string" },
                            theme = new { type = "string" }
                        }
                    }
                },
                // 建筑构件工具
                new
                {
                    name = "get_roof_template",
                    description = "获取屋顶设计模板，返回形状描述和推荐方块。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            roof_type = new { type = "string" },
                            style = new { type = "string" },
                            width = new { type = "integer" }
                        },
                        required = new[] { "roof_type", "width" }
                    }
                },
                new
                {
                    name = "get_window_template",
                    description = "获取窗户设计模板，返回尺寸和方块ID。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            window_type = new { type = "string" },
                            style = new { type = "string" }
                        },
                        required = new[] { "window_type" }
                    }
                },
                new
                {
                    name = "get_floor_structure",
                    description = "获取楼层结构模板，返回楼层数、高度、墙壁厚度。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            structure_type = new { type = "string" },
                            style = new { type = "string" }
                        },
                        required = new[] { "structure_type" }
                    }
                }
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
                    // 建筑构件工具
                    case "get_roof_template":
                        return GetRoofTemplate(input["roof_type"]?.ToString(), input["style"]?.ToString(), input["width"]?.Value<int>() ?? 10, kb);
                    case "get_window_template":
                        return GetWindowTemplate(input["window_type"]?.ToString(), input["style"]?.ToString(), kb);
                    case "get_floor_structure":
                        return GetFloorStructure(input["structure_type"]?.ToString(), input["style"]?.ToString(), kb);
                    // 新增：需求分析和材料选择工具
                    case "analyze_requirement":
                        return AnalyzeRequirement(input, kb);
                    case "select_materials":
                        return SelectMaterials(input, kb);
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
            // Step1: SQL精确过滤 (category, biome)
            var candidates = kb.Tiles.SearchTiles(null, category, biome).ToList();

            // Step2: 向量语义排序 (style)
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
            // Step1: SQL精确过滤 (category)
            var candidates = kb.Tiles.SearchWalls(null, category).ToList();

            // Step2: 向量语义排序 (style)
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
            // Step1: 获取所有家具候选
            var candidates = kb.Furniture.SearchFurniture(roomType, npcType);

            // Step2: 向量语义排序 (按类别)
            string category = roomType?.ToLower();  // room_type可作为类别: light, surface, comfort, storage, door
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

        // 建筑构件工具执行方法
        private ToolResult GetRoofTemplate(string roofType, string style, int width, KnowledgeBaseManager kb)
        {
            var template = kb.Roofs.GetTemplate(roofType);
            if (template == null)
                return new ToolResult { IsError = true, Content = "{\"error\": \"未找到屋顶模板: " + roofType + "\"}" };

            // 计算屋顶高度（人字形屋顶高度约为宽度的一半）
            int roofHeight = CalculateRoofHeight(roofType, width);

            // 生成放置提示
            string placementHint = GenerateRoofPlacementHint(roofType, width, roofHeight);

            var result = new
            {
                roof_type = roofType,
                display_name = template.display_name,
                shape_pattern = template.shape_pattern,
                calculated_height = roofHeight,
                edge_tiles = template.edge_tiles,
                fill_tiles = template.fill_tiles,
                placement_hint = placementHint,
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

            // 计算总高度
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

        // 辅助方法：计算屋顶高度
        private int CalculateRoofHeight(string roofType, int width)
        {
            switch (roofType?.ToLower())
            {
                case "gable":    // 人字形：高度约为宽度的1/3
                    return Math.Max(2, width / 3);
                case "flat":     // 平顶：1层
                    return 1;
                case "dome":     // 圆顶：高度约为宽度的1/4
                    return Math.Max(2, width / 4);
                case "pagoda":   // 宝塔：多层，每层高度递减
                    return Math.Max(3, width / 2);
                case "stepped":  // 阶梯：每层缩进，高度约宽度的1/3
                    return Math.Max(2, width / 3);
                default:
                    return Math.Max(2, width / 4);
            }
        }

        // 辅助方法：生成屋顶放置提示
        private string GenerateRoofPlacementHint(string roofType, int width, int height)
        {
            switch (roofType?.ToLower())
            {
                case "gable":
                    return $"从y=0开始，中心x={width/2}最高，向两侧对称下降。使用slope方块实现斜坡。每层高度={height}";
                case "flat":
                    return $"在顶层平铺一层方块，边缘可加装饰方块。";
                case "dome":
                    return $"从中心向外扩展，形成弧形。最顶层为单方块，逐层扩大。";
                case "pagoda":
                    return $"多层结构，每层向外延伸1-2格，形成阶梯状。";
                case "stepped":
                    return $"阶梯状上升，每层缩进1格，形成金字塔形状。";
                default:
                    return "根据形状描述放置方块。";
            }
        }

        // 新增：需求分析工具执行
        private ToolResult AnalyzeRequirement(JObject input, KnowledgeBaseManager kb)
        {
            try
            {
                string style = input["style"]?.ToString()?.ToLower() ?? "medieval";

                // 验证风格是否有效
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

                // 返回分析结果和建议的材料类别
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

        // 新增：材料选择工具执行
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

                // 设置家具ID
                if (input["light_id"] != null)
                    selected.FurnitureIds["light"] = input["light_id"].Value<int>();
                if (input["surface_id"] != null)
                    selected.FurnitureIds["surface"] = input["surface_id"].Value<int>();
                if (input["comfort_id"] != null)
                    selected.FurnitureIds["comfort"] = input["comfort_id"].Value<int>();
                if (input["door_id"] != null)
                    selected.FurnitureIds["door"] = input["door_id"].Value<int>();

                // 验证选定的方块ID是否有效
                var tileInfo = kb.Tiles.GetTileById(selected.MainBlockId);
                if (tileInfo == null)
                {
                    trab.Instance?.Logger.Warn($"选定的MainBlockId={selected.MainBlockId}不在知识库中，使用默认值4");
                    selected.MainBlockId = 4;
                }

                // 返回选定结果和下一步提示
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

    // OpenAI响应结构
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
        public string reasoning_content { get; set; }  // DeepSeek的思考内容
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

    // Anthropic响应结构
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