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
using trab.Core.Agents.Tools;
using trab.Core.API;
using trab.Core.Building;
using trab.Core.KnowledgeBase;
using trab.Data;

namespace trab.Core.Agents
{
    /// <summary>
    /// 真Agent核心 - 统一的Agent实现
    /// 替代原有的SingleAgent和MultiAgent
    /// </summary>
    public class TrueAgentCore
    {
        private const int MAX_AGENT_ROUNDS = 10;
        private const string SYSTEM_PROMPT = @"你是泰拉瑞亚建筑设计Agent。你的任务是根据用户需求生成建筑设计。

## 核心流程：检索→理解→生成

1. **理解需求** - 分析用户描述，确定：
   - 风格(asian/medieval/fantasy等)
   - 建筑类型(house/tower/castle等)
   - 尺寸范围(建议宽度10-40，高度10-30)
   - 特殊要求(灯笼装饰、NPC房屋等)

2. **检索建筑模板** - 调用 search_buildings：
   - 输入风格、类型、尺寸范围
   - 返回匹配建筑的ID、构件列表、摘要

3. **获取构件详情** - 调用 get_building_details：
   - 获取建筑的构件组成(屋顶、墙壁、楼层、装饰)
   - 获取建造顺序和材料配置
   - 理解构件的生成规则

4. **获取生成规则** - 调用 get_component_rules：
   - 获取构件的参数化生成规则
   - 可应用缩放调整尺寸

5. **获取材料推荐** - 调用 get_style_materials：
   - 获取风格的推荐材料ID
   - 注意 tile_id(方块) 和 wall_id(墙壁) 的区别

6. **生成设计规则** - 调用 generate_design_rules：
   - 输出 BuildingRules JSON
   - 包含构件列表、参数、材料

## 构件类型说明

- **roof**: 屋顶构件，形状有 pagoda(宝塔)、pyramid(金字塔)、gable(人字)、flat(平顶)
- **wall**: 墙壁构件，有 outer_wall(外墙)、inner_wall(内墙)
- **floor**: 楼层构件，参数有 level(楼层号)、thickness(厚度)
- **decoration**: 装饰构件，有 lantern(灯笼)、torch(火炬)、furniture(家具)
- **foundation**: 基础构件，通常在建筑底部

## 重要规则

- **不要猜测ID**：使用工具返回的精确 tile_id 和 wall_id
- **参数化生成**：使用构件规则而非具体坐标
- **尺寸自由**：根据用户需求和模板参考灵活调整
- **NPC房屋**：需要 light + door + table + chair + walls

## 风格关键词

- asian/中式 → 金块(179)、大理石墙(172)、灯笼(129)、宝塔屋顶
- medieval/中世纪 → 灰砖(4)、石板(143)、火炬(10)、人字屋顶
- fantasy/奇幻 → 珍珠石(182)、玻璃墙(14)、水晶灯
- snow/雪地 → 雪砖(149)、冰墙(71)、壁炉
- desert/沙漠 → 沙岩(54)、棕榈木(49)
- modern/现代 → 花岗岩(43)、玻璃墙(14)、平顶";

        private readonly string _apiKey;
        private readonly AIServiceType _serviceType;
        private readonly string _modelName;
        private readonly int _maxBuildingSize;
        private readonly HttpClient _httpClient;
        private readonly ToolRegistry _toolRegistry;
        private readonly KnowledgeBaseManager _kb;
        private readonly ProceduralBuilder _builder;

        public TrueAgentCore(
            string apiKey,
            AIServiceType serviceType,
            string modelName,
            int maxBuildingSize = 50)
        {
            _apiKey = apiKey;
            _serviceType = serviceType;
            _modelName = modelName ?? "deepseek-chat";
            _maxBuildingSize = maxBuildingSize;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(120)
            };

            // 初始化工具注册表
            _toolRegistry = ToolRegistry.CreateDefault();

            // 初始化知识库
            _kb = KnowledgeBaseManager.Instance;
            _kb.Initialize();

            // 初始化程序化生成器
            _builder = new ProceduralBuilder(_kb);
        }

        /// <summary>
        /// 运行Agent主循环
        /// </summary>
        public async Task<TEditSchDesign> RunAgentLoop(
            string userPrompt,
            Action<string, int> progressCallback = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                progressCallback?.Invoke("错误: API密钥未配置", 0);
                return null;
            }

            // 检查是否已取消
            if (ct.IsCancellationRequested)
            {
                progressCallback?.Invoke("请求已取消", 0);
                trab.Instance?.Logger.Warn("RunAgentLoop: 请求在开始前已取消");
                return null;
            }

            try
            {
                progressCallback?.Invoke("Agent启动...", 0);
                trab.Instance?.Logger.Info($"TrueAgentCore启动 - 模型: {_modelName}, 服务: {_serviceType}");

                // 初始化消息队列
                var messages = new List<AgentMessage>
                {
                    new AgentMessage { role = "user", content = userPrompt }
                };

                // Agent主循环
                for (int round = 1; round <= MAX_AGENT_ROUNDS; round++)
                {
                    // 检查取消
                    if (ct.IsCancellationRequested)
                    {
                        progressCallback?.Invoke("请求已取消", 0);
                        trab.Instance?.Logger.Warn($"RunAgentLoop: 请求在轮次{round}被取消");
                        return null;
                    }

                    progressCallback?.Invoke($"[轮次{round}]思考中...", round);

                    // 发送API请求
                    var response = await SendAgentRequest(messages, ct);

                    if (response == null)
                    {
                        progressCallback?.Invoke("API请求失败", 0);
                        return null;
                    }

                    // 检查停止原因
                    if (response.stop_reason == "end_turn" || response.stop_reason == "stop_sequence")
                    {
                        // AI完成，尝试提取最终设计
                        return await ProcessFinalResponse(response, progressCallback);
                    }

                    if (response.stop_reason == "tool_use")
                    {
                        // AI调用工具
                        var assistantMessage = new AgentMessage
                        {
                            role = "assistant",
                            content = response.content
                        };
                        messages.Add(assistantMessage);

                        // 执行工具调用
                        var toolResults = await ExecuteToolCalls(response.content, progressCallback, round);

                        // 检查是否有 generate_design_rules 的结果
                        var designResult = toolResults.FirstOrDefault(r =>
                            r.Metadata?.ContainsKey("building_rules") == true);

                        if (designResult != null)
                        {
                            // 直接从工具结果生成建筑
                            var rules = designResult.Metadata["building_rules"] as BuildingRules;
                            if (rules != null)
                            {
                                progressCallback?.Invoke("生成建筑设计...", 0);
                                var design = _builder.GenerateFromRules(rules);
                                progressCallback?.Invoke($"完成: {design.name} ({design.width}x{design.height})", 0);
                                return design;
                            }
                        }

                        // 添加工具结果到消息队列
                        messages.Add(new AgentMessage
                        {
                            role = "user",
                            content = toolResults
                        });

                        continue;
                    }

                    // 其他停止原因
                    progressCallback?.Invoke($"Agent停止: {response.stop_reason}", 0);
                    break;
                }

                progressCallback?.Invoke("超过最大轮数限制", 0);
                return null;
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"Agent错误: {ex.Message}");
                progressCallback?.Invoke($"错误: {ex.Message}", 0);
                return null;
            }
        }

        /// <summary>
        /// 发送Agent请求
        /// </summary>
        private async Task<AgentResponse> SendAgentRequest(List<AgentMessage> messages, CancellationToken ct)
        {
            bool useOpenAIFormat = _serviceType != AIServiceType.Claude && _serviceType != AIServiceType.DashScope;

            if (useOpenAIFormat)
            {
                return await SendOpenAIRequest(messages, ct);
            }
            else
            {
                return await SendAnthropicRequest(messages, ct);
            }
        }

        /// <summary>
        /// 发送OpenAI格式请求
        /// </summary>
        private async Task<AgentResponse> SendOpenAIRequest(List<AgentMessage> messages, CancellationToken ct)
        {
            try
            {
                var request = new JObject
                {
                    ["model"] = _modelName,
                    ["messages"] = BuildOpenAIMessages(messages),
                    ["tools"] = _toolRegistry.GetOpenAIToolDefinitions(),
                    ["max_tokens"] = 4096,
                    ["temperature"] = 0.7
                };

                string endpoint = GetAPIEndpoint();
                ConfigureHttpClient();

                var requestJson = request.ToString();
                trab.Instance?.Logger.Debug($"OpenAI请求: {requestJson}");

                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content, ct);

                var responseJson = await response.Content.ReadAsStringAsync(ct);
                trab.Instance?.Logger.Debug($"OpenAI响应: {responseJson}");

                if (!response.IsSuccessStatusCode)
                {
                    trab.Instance?.Logger.Error($"API错误 ({response.StatusCode}): {responseJson}");
                    return null;
                }

                return ParseOpenAIResponse(responseJson);
            }
            catch (OperationCanceledException)
            {
                trab.Instance?.Logger.Info("请求被用户取消");
                return null;
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"OpenAI请求异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 发送Anthropic格式请求
        /// </summary>
        private async Task<AgentResponse> SendAnthropicRequest(List<AgentMessage> messages, CancellationToken ct)
        {
            var request = new JObject
            {
                ["model"] = _modelName,
                ["system"] = SYSTEM_PROMPT,
                ["messages"] = BuildAnthropicMessages(messages),
                ["tools"] = _toolRegistry.GetAnthropicToolDefinitions(),
                ["max_tokens"] = 4096
            };

            string endpoint = GetAPIEndpoint();
            ConfigureHttpClient();

            var requestJson = request.ToString();
            trab.Instance?.Logger.Debug($"Anthropic请求: {requestJson}");

            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, content, ct);

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            trab.Instance?.Logger.Debug($"Anthropic响应: {responseJson}");

            if (!response.IsSuccessStatusCode)
            {
                trab.Instance?.Logger.Error($"API错误 ({response.StatusCode}): {responseJson}");
                return null;
            }

            return ParseAnthropicResponse(responseJson);
        }

        #region 消息构建

        private JArray BuildOpenAIMessages(List<AgentMessage> messages)
        {
            var result = new JArray();

            // 添加系统提示
            result.Add(new JObject
            {
                ["role"] = "system",
                ["content"] = SYSTEM_PROMPT
            });

            foreach (var msg in messages)
            {
                if (msg.role == "user" && msg.content is List<ContentItem> contentItems)
                {
                    // 工具结果 - 每个tool_result作为独立的tool消息
                    foreach (var item in contentItems)
                    {
                        if (item.type == "tool_result")
                        {
                            result.Add(new JObject
                            {
                                ["role"] = "tool",
                                ["tool_call_id"] = item.tool_call_id,
                                ["content"] = item.content
                            });
                        }
                        else if (item.type == "metadata")
                        {
                            // 跳过元数据项，不发送给API
                            continue;
                        }
                    }
                }
                else if (msg.role == "assistant" && msg.content is List<ContentItem> assistantItems)
                {
                    var msgObj = new JObject
                    {
                        ["role"] = "assistant"
                    };

                    // 提取文本内容
                    var textContent = ExtractTextContent(assistantItems);
                    if (!string.IsNullOrEmpty(textContent))
                    {
                        msgObj["content"] = textContent;
                    }
                    else
                    {
                        msgObj["content"] = ""; // OpenAI要求content字段存在
                    }

                    var toolCalls = new JArray();
                    foreach (var item in assistantItems)
                    {
                        if (item.type == "tool_use")
                        {
                            toolCalls.Add(new JObject
                            {
                                ["id"] = item.id,
                                ["type"] = "function",
                                ["function"] = new JObject
                                {
                                    ["name"] = item.name,
                                    ["arguments"] = item.input?.ToString() ?? "{}"
                                }
                            });
                        }
                    }
                    if (toolCalls.Count > 0)
                    {
                        msgObj["tool_calls"] = toolCalls;
                    }
                    result.Add(msgObj);
                }
                else
                {
                    result.Add(new JObject
                    {
                        ["role"] = msg.role,
                        ["content"] = msg.content?.ToString() ?? ""
                    });
                }
            }

            return result;
        }

        private JArray BuildAnthropicMessages(List<AgentMessage> messages)
        {
            var result = new JArray();

            foreach (var msg in messages)
            {
                if (msg.role == "assistant" && msg.content is List<ContentItem> assistantItems)
                {
                    result.Add(new JObject
                    {
                        ["role"] = "assistant",
                        ["content"] = JArray.FromObject(assistantItems)
                    });
                }
                else if (msg.role == "user" && msg.content is List<ContentItem> userItems)
                {
                    result.Add(new JObject
                    {
                        ["role"] = "user",
                        ["content"] = JArray.FromObject(userItems)
                    });
                }
                else
                {
                    result.Add(new JObject
                    {
                        ["role"] = msg.role,
                        ["content"] = new JArray
                        {
                            new JObject { ["type"] = "text", ["text"] = msg.content?.ToString() }
                        }
                    });
                }
            }

            return result;
        }

        private string ExtractTextContent(List<ContentItem> items)
        {
            var textItem = items?.FirstOrDefault(i => i.type == "text");
            return textItem?.text ?? "";
        }

        #endregion

        #region 响应解析

        private AgentResponse ParseOpenAIResponse(string json)
        {
            var jobj = JObject.Parse(json);
            var choice = jobj["choices"]?.FirstOrDefault();

            if (choice == null) return null;

            var message = choice["message"];
            var content = new List<ContentItem>();

            // 解析文本内容
            var textContent = message?["content"]?.ToString();
            if (!string.IsNullOrEmpty(textContent))
            {
                content.Add(new ContentItem { type = "text", text = textContent });
            }

            // 解析工具调用
            var toolCalls = message?["tool_calls"] as JArray;
            if (toolCalls != null)
            {
                foreach (var tc in toolCalls)
                {
                    content.Add(new ContentItem
                    {
                        type = "tool_use",
                        id = tc["id"]?.ToString(),
                        name = tc["function"]?["name"]?.ToString(),
                        input = JObject.Parse(tc["function"]?["arguments"]?.ToString() ?? "{}")
                    });
                }
            }

            return new AgentResponse
            {
                content = content,
                stop_reason = toolCalls != null && toolCalls.Count > 0 ? "tool_use" : "end_turn"
            };
        }

        private AgentResponse ParseAnthropicResponse(string json)
        {
            var jobj = JObject.Parse(json);
            var contentArray = jobj["content"] as JArray;

            var content = new List<ContentItem>();
            foreach (var item in contentArray ?? new JArray())
            {
                content.Add(new ContentItem
                {
                    type = item["type"]?.ToString(),
                    text = item["text"]?.ToString(),
                    id = item["id"]?.ToString(),
                    name = item["name"]?.ToString(),
                    input = item["input"] as JObject
                });
            }

            return new AgentResponse
            {
                content = content,
                stop_reason = jobj["stop_reason"]?.ToString() ?? "end_turn"
            };
        }

        #endregion

        #region 工具执行

        private async Task<List<ContentItem>> ExecuteToolCalls(
            List<ContentItem> assistantContent,
            Action<string, int> progressCallback,
            int round)
        {
            var results = new List<ContentItem>();

            foreach (var item in assistantContent)
            {
                if (item.type == "tool_use")
                {
                    progressCallback?.Invoke($"调用工具: {item.name}", round);

                    var result = await _toolRegistry.ExecuteToolAsync(item.name, item.input, _kb);

                    results.Add(new ContentItem
                    {
                        type = "tool_result",
                        tool_call_id = item.id,
                        content = result.Content,
                        is_error = result.IsError
                    });

                    // 保存元数据
                    if (result.Metadata != null)
                    {
                        results.Add(new ContentItem
                        {
                            type = "metadata",
                            Metadata = result.Metadata
                        });
                    }
                }
            }

            return results;
        }

        #endregion

        #region 最终响应处理

        private async Task<TEditSchDesign> ProcessFinalResponse(
            AgentResponse response,
            Action<string, int> progressCallback)
        {
            var textContent = response.content?.FirstOrDefault(c => c.type == "text")?.text;

            if (string.IsNullOrEmpty(textContent))
            {
                progressCallback?.Invoke("未获取到有效响应", 0);
                return null;
            }

            // 尝试提取JSON
            string json = ExtractJson(textContent);

            if (string.IsNullOrEmpty(json))
            {
                progressCallback?.Invoke("无法解析JSON", 0);
                return null;
            }

            // 尝试解析为BuildingRules
            try
            {
                var rules = JsonConvert.DeserializeObject<BuildingRules>(json);
                if (rules != null && rules.width > 0 && rules.height > 0)
                {
                    progressCallback?.Invoke("生成建筑设计...", 0);
                    var design = _builder.GenerateFromRules(rules);
                    progressCallback?.Invoke($"完成: {design.name} ({design.width}x{design.height})", 0);
                    return design;
                }
            }
            catch { }

            // 尝试解析为TEditSchDesign
            try
            {
                var design = JsonConvert.DeserializeObject<TEditSchDesign>(json);
                if (design != null && design.width > 0 && design.height > 0)
                {
                    progressCallback?.Invoke($"完成: {design.name} ({design.width}x{design.height})", 0);
                    return design;
                }
            }
            catch { }

            progressCallback?.Invoke("JSON格式无法识别", 0);
            return null;
        }

        private string ExtractJson(string text)
        {
            // 尝试提取 ```json ``` 格式
            int jsonStart = text.IndexOf("```json");
            if (jsonStart >= 0)
            {
                jsonStart += 7;
                int jsonEnd = text.IndexOf("```", jsonStart);
                if (jsonEnd > jsonStart)
                {
                    return text.Substring(jsonStart, jsonEnd - jsonStart).Trim();
                }
            }

            // 尝试提取大括号包围的JSON
            int braceStart = text.IndexOf('{');
            int braceEnd = text.LastIndexOf('}');
            if (braceStart >= 0 && braceEnd > braceStart)
            {
                return text.Substring(braceStart, braceEnd - braceStart + 1);
            }

            return null;
        }

        #endregion

        #region API配置

        private string GetAPIEndpoint()
        {
            return _serviceType switch
            {
                AIServiceType.Claude => "https://api.anthropic.com/v1/messages",
                AIServiceType.DeepSeek => "https://api.deepseek.com/v1/chat/completions",
                AIServiceType.DashScope => "https://dashscope.aliyuncs.com/compatible-mode/v1/messages",
                AIServiceType.OpenAI => "https://api.openai.com/v1/chat/completions",
                AIServiceType.Custom => trab.GetConfig()?.CustomEndpoint ?? "",
                _ => ""
            };
        }

        private void ConfigureHttpClient()
        {
            _httpClient.DefaultRequestHeaders.Clear();

            if (_serviceType == AIServiceType.Claude || _serviceType == AIServiceType.DashScope)
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// Agent消息
    /// </summary>
    public class AgentMessage
    {
        public string role { get; set; }
        public object content { get; set; } // string 或 List<ContentItem>
    }

    /// <summary>
    /// 内容项
    /// </summary>
    public class ContentItem
    {
        public string type { get; set; } // text, tool_use, tool_result
        public string text { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public JObject input { get; set; }
        public string content { get; set; }
        public bool is_error { get; set; }
        public string tool_call_id { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Agent响应
    /// </summary>
    public class AgentResponse
    {
        public List<ContentItem> content { get; set; }
        public string stop_reason { get; set; }
    }

    #endregion
}
