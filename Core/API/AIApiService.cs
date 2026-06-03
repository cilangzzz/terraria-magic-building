using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using Terraria.ModLoader;
using trab.Data;

namespace trab.Core.API
{
    /// <summary>
    /// AI API服务 - 封装API请求逻辑
    /// </summary>
    public class AIApiService : ApiServiceBase
    {
        private const string SYSTEM_PROMPT = @"你是泰拉瑞亚建筑设计助手。返回简洁的JSON格式建筑设计。

重要规则：
1. 建筑尺寸控制在 10x8 以内
2. 只列出边界方块，不要列出每一个内部方块
3. 使用 ranges 表示连续方块（可选）
4. 内部墙壁用单个范围表示

示例格式：
{
  ""name"": ""木屋"",
  ""width"": 10,
  ""height"": 8,
  ""tiles"": [
    {""x"":0,""y"":0,""type"":""Wood""},
    {""x"":9,""y"":0,""type"":""Wood""}
  ],
  ""wallRanges"": [{""x1"":1,""y1"":1,""x2"":8,""y2"":6,""type"":""WoodWall""}],
  ""furniture"": [{""x"":3,""y"":6,""type"":""WorkBench""}],
  ""doors"": [{""x"":5,""y"":6}],
  ""lightSources"": [{""x"":3,""y"":2}]
}

方块类型: Stone,Dirt,Wood,GrayBrick,GoldBrick,Glass,Platform
墙壁类型: StoneWall,DirtWall,WoodWall,GrayBrickWall,GlassWall
家具: WorkBench,Table,Chair,Bed,Chest,Furnace,Torch

只返回纯JSON，不要解释，不要思考过程。";

        public AIApiService(string apiKey, AIServiceType serviceType = AIServiceType.DeepSeek, string customEndpoint = null, string modelName = "deepseek-v4-flash")
            : base(apiKey, serviceType, modelName, customEndpoint)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<string> SendChatRequestAsync(string userMessage, CancellationToken ct)
        {
            try
            {
                object requestBody;

                // Anthropic格式 (DeepSeek/Claude/DashScope)
                if (_serviceType == AIServiceType.DeepSeek || _serviceType == AIServiceType.Claude || _serviceType == AIServiceType.DashScope)
                {
                    requestBody = new
                    {
                        model = _modelName,
                        max_tokens = 4096,
                        system = SYSTEM_PROMPT,
                        messages = new[]
                        {
                            new { role = "user", content = userMessage }
                        }
                    };
                }
                else // OpenAI格式
                {
                    requestBody = new
                    {
                        model = _modelName,
                        messages = new[]
                        {
                            new { role = "system", content = SYSTEM_PROMPT },
                            new { role = "user", content = userMessage }
                        },
                        max_tokens = 2048
                    };
                }

                string json = JsonConvert.SerializeObject(requestBody);
                trab.Instance?.Logger.Info($"发送请求: {json}");
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_apiEndpoint, content, ct);

                string responseJson = await response.Content.ReadAsStringAsync(ct);
                trab.Instance?.Logger.Info($"收到响应: {responseJson}");

                if (!response.IsSuccessStatusCode)
                {
                    trab.Instance?.Logger.Error($"API错误 {response.StatusCode}: {responseJson}");
                    return null;
                }

                // 解析Anthropic格式响应
                if (_serviceType == AIServiceType.DeepSeek || _serviceType == AIServiceType.Claude || _serviceType == AIServiceType.DashScope)
                {
                    var claudeResp = JsonConvert.DeserializeObject<ClaudeResponse>(responseJson);
                    if (claudeResp?.content != null && claudeResp.content.Length > 0)
                    {
                        if (claudeResp.stop_reason == "max_tokens")
                        {
                            trab.Instance?.Logger.Warn("API响应被截断(max_tokens)，建筑设计可能不完整");
                            Main.QueueMainThreadAction(() =>
                                Main.NewText("[AI建筑] 警告: 响应被截断，建筑可能不完整，请尝试更简单的描述", Color.Yellow));
                        }

                        foreach (var item in claudeResp.content)
                        {
                            if (item.type == "text" && !string.IsNullOrEmpty(item.text))
                            {
                                return item.text;
                            }
                        }
                    }
                }
                else
                {
                    var aiResp = JsonConvert.DeserializeObject<AIResponse>(responseJson);
                    if (aiResp?.choices != null && aiResp.choices.Length > 0)
                        return aiResp.choices[0].message.content;
                }

                return null;
            }
            catch (TaskCanceledException)
            {
                trab.Instance?.Logger.Error("API请求超时");
                return null;
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"API请求失败: {ex.Message}");
                return null;
            }
        }

        public static string ExtractJsonFromResponse(string response)
        {
            if (string.IsNullOrEmpty(response)) return null;

            int start = response.IndexOf("```json");
            if (start >= 0)
            {
                start += 7;
                int end = response.IndexOf("```", start);
                if (end > start)
                    return response.Substring(start, end - start).Trim();
            }

            int braceStart = response.IndexOf('{');
            int braceEnd = response.LastIndexOf('}');
            if (braceStart >= 0 && braceEnd > braceStart)
                return response.Substring(braceStart, braceEnd - braceStart + 1);

            return null;
        }
    }

    public enum AIServiceType
    {
        OpenAI,
        Claude,
        DashScope,
        DeepSeek,
        Custom
    }

    public class ClaudeResponse
    {
        public ClaudeContent[] content { get; set; }
        public string stop_reason { get; set; }
    }

    public class ClaudeContent
    {
        public string type { get; set; }
        public string text { get; set; }
    }

    public class AIResponse
    {
        public AIChoice[] choices { get; set; }
    }

    public class AIChoice
    {
        public AIMessage message { get; set; }
    }

    public class AIMessage
    {
        public string content { get; set; }
    }
}
