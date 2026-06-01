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
{""type"":""two_story"", ""w"":12, ""h"":14, ""style"":""medieval"", ""roof"":""gable"", ""floors"":2}

## 参数说明
- type: house/two_story/tower/castle
- w: 宽度(6-20)
- h: 高度(6-16)
- style: medieval/fantasy/natural/modern
- roof: gable/flat/dome/pagoda
- floors: 楼层数(1-4)

只输出一行JSON，无解释。";

        private const string AGENT_SYSTEM_PROMPT = @"泰拉瑞亚建筑设计Agent。生成有设计感的建筑JSON。

## 推荐流程（按顺序调用工具）
1. get_floor_structure - 获取楼层结构（单层/双层/塔楼）
2. get_roof_template - 获取屋顶设计（人字形/平顶/圆顶）
3. get_window_template - 获取窗户样式（单窗/双窗/拱窗）
4. search_tiles - 选择建筑材料
5. 输出完整JSON

## 设计要点
- 屋顶要有形状：人字形(gable)最常用，用斜坡方块实现三角上升
- 窗户对称布局：每层左右各1-2个窗户
- 楼层区分：不同楼层使用不同方块或油漆
- 墙壁厚度：城堡类建筑墙壁厚度=2

## JSON格式（必须完整输出）
{
""name"": ""中世纪双层小屋"",
""width"": 12,
""height"": 14,
""style"": ""medieval"",
""tiles"": [{""x"":0,""y"":0,""tile_id"":4,""slope"":0}],
""wallRanges"": [{""x1"":1,""y1"":1,""x2"":11,""y2"":13,""wall_id"":4}],
""furniture"": [{""x"":3,""y"":6,""tile_id"":17}],
""doors"": [{""x"":5,""y"":1,""tile_id"":10}],
""lightSources"": [{""x"":2,""y"":3,""tile_id"":4},{""x"":10,""y"":3,""tile_id"":4}]
}

## 常用ID
木材5|石头1|灰砖4|石板143|玻璃13|大理石57|木墙4|石墙1|门10|火把4|工作台17|桌子87|椅子88

## 规则
- 输出完整JSON，不要截断
- tiles数组包含所有方块位置
- 窗户位置放入tiles（玻璃+窗框）
- 屋顶用斜坡方块(slope=1-4)实现形状";

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
        /// Agent主入口
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

                // 使用多Agent协作模式
                return await GenerateBuildingMultiAgentAsync(userPrompt, progressCallback, ct);
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
                        description = "搜索家具及其NPC房屋功能。返回tile_id、尺寸、放置规则。",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                room_type = new { type = "string", description = "房间类型" },
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
                    description = "搜索家具及NPC房屋功能。",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            room_type = new { type = "string" },
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
            var furniture = kb.Furniture.SearchFurniture(roomType, npcType);
            var result = new
            {
                furniture = furniture.Select(f => new { name = f.Key, tile_id = f.Value.tile_id, display_name = f.Value.display_name }),
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