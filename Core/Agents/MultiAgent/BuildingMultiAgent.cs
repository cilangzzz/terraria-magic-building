// ============================================================================
// DEPRECATED - 此文件已废弃，请使用 TrueAgentCore 替代
// 原因: MultiAgent模式已被统一到 TrueAgentCore 中
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
using trab.Core.API;
using trab.Core.KnowledgeBase;
using trab.Data;

namespace trab.Core.Agents.MultiAgent
{
    /// <summary>
    /// 多Agent协作模式 - 规划 + 模块生成 + 合并 -> TEditSch格式
    /// </summary>
    public class BuildingMultiAgent : ApiServiceBase
    {
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

        public BuildingMultiAgent(string apiKey, AIServiceType serviceType, string modelName)
            : base(apiKey, serviceType, modelName)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(120);
        }

        /// <summary>
        /// 多Agent协作生成建筑 - 返回TEditSch格式
        /// </summary>
        public async Task<TEditSchDesign> GenerateBuildingAsync(
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

            // 阶段3：合并为TEditSch格式
            progressCallback?.Invoke("[阶段3]合并模块...", 3);
            var merger = new BuildingMerger();
            var design = merger.MergeToTEditSch(plan, modules);

            progressCallback?.Invoke($"完成: {design?.stats?.active_tiles ?? 0}方块", 0);
            return design;
        }

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
                max_tokens = 1024
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

        private BuildingPlan ParseBuildingPlan(string content)
        {
            string json = ExtractJson(content);
            if (json == null) return null;

            try
            {
                var simplePlan = JsonConvert.DeserializeObject<SimpleBuildingPlan>(json);
                if (simplePlan == null) return null;

                var plan = new BuildingPlan
                {
                    BuildingType = simplePlan.Type ?? "house",
                    Width = Math.Clamp(simplePlan.W ?? 10, 6, 20),
                    Height = Math.Clamp(simplePlan.H ?? 8, 6, 16),
                    Style = simplePlan.Style ?? "medieval"
                };

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

        private void GenerateDefaultRegions(BuildingPlan plan, string roofType, int floors)
        {
            plan.Regions = new List<Region>();

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
                if (i == floors) yEnd = plan.Height - 1;

                plan.Regions.Add(new Region
                {
                    Name = $"floor{i}",
                    YRange = new[] { yStart, yEnd }
                });

                // 窗户
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

                // 家具
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

        private class SimpleBuildingPlan
        {
            public string Type { get; set; }
            public int? W { get; set; }
            public int? H { get; set; }
            public string Style { get; set; }
            public string Roof { get; set; }
            public int? Floors { get; set; }
        }
    }
}