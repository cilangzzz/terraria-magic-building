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
    /// AI Agent建筑生成服务
    /// </summary>
    public class AIAgentService
    {
        private HttpClient _httpClient;
        private string _apiKey;
        private string _apiEndpoint;
        private AIServiceType _serviceType;
        private string _modelName;

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
                _apiEndpoint = "https://api.deepseek.com/v1/chat/completions";
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);
            }
            else
            {
                _apiEndpoint = "https://api.openai.com/v1/chat/completions";
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);
            }
        }

        public async Task<BuildingDesign> GenerateBuildingAsync(
            string userPrompt,
            Action<string> progressCallback = null,
            CancellationToken ct = default)
        {
            try
            {
                progressCallback?.Invoke("Agent启动...");

                KnowledgeBaseManager.Instance.Initialize();

                // 获取知识库信息
                var kb = KnowledgeBaseManager.Instance;
                progressCallback?.Invoke("知识库: " + kb.Tiles.TileCount + "方块, " + kb.Styles.StyleCount + "风格");

                // 构建带知识的提示词
                var enhancedPrompt = BuildEnhancedPrompt(userPrompt);
                progressCallback?.Invoke("发送生成请求...");

                // 发送请求
                var design = await SendAndParse(enhancedPrompt, ct);

                progressCallback?.Invoke("完成");
                return design;
            }
            catch (Exception ex)
            {
                progressCallback?.Invoke("错误: " + ex.Message);
                return null;
            }
        }

        private string BuildEnhancedPrompt(string userPrompt)
        {
            var kb = KnowledgeBaseManager.Instance;

            var sb = new StringBuilder();
            sb.AppendLine("你是泰拉瑞亚建筑设计助手。请生成JSON格式的建筑设计。");
            sb.AppendLine("");
            sb.AppendLine("可用方块:");
            var tiles = kb.Tiles.SearchTiles("medieval");
            foreach (var t in tiles.Take(10))
            {
                sb.AppendLine("- " + t.name + " (ID: " + t.id + ")");
            }
            sb.AppendLine("");
            sb.AppendLine("用户请求: " + userPrompt);
            sb.AppendLine("");
            sb.AppendLine("输出格式要求:");
            sb.AppendLine("{ name, width, height, tiles[{x,y,type}], walls[{x,y,type}], furniture[{x,y,type}], doors[{x,y}], lightSources[{x,y}] }");
            sb.AppendLine("直接输出JSON，不要额外解释。");

            return sb.ToString();
        }

        private async Task<BuildingDesign> SendAndParse(string prompt, CancellationToken ct)
        {
            var requestBody = new
            {
                model = _modelName,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 4096
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_apiEndpoint, content, ct);
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                trab.Instance?.Logger.Error("API错误: " + responseJson);
                return null;
            }

            // 解析响应
            var respObj = JsonConvert.DeserializeObject<JObject>(responseJson);
            var choices = respObj["choices"] as JArray;
            if (choices == null || choices.Count == 0) return null;

            var textContent = choices[0]["message"]?["content"]?.ToString();
            if (textContent == null) return null;

            return ParseDesign(textContent);
        }

        private BuildingDesign ParseDesign(string content)
        {
            // 提取JSON
            int start = content.IndexOf('{');
            int end = content.LastIndexOf('}');
            if (start < 0 || end <= start) return null;

            string json = content.Substring(start, end - start + 1);

            try
            {
                return JsonConvert.DeserializeObject<BuildingDesign>(json);
            }
            catch
            {
                return null;
            }
        }
    }

    public class ToolCallResult
    {
        public string ToolName { get; set; }
        public string ToolCallId { get; set; }
        public object Input { get; set; }
        public object Output { get; set; }
        public string JsonContent { get; set; }
        public string Summary { get; set; }
        public bool IsError { get; set; }
    }
}