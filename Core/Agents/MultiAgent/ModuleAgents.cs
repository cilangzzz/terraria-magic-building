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
    /// 模块生成Agent - 每个模块单独调用AI生成
    /// </summary>
    public class ModuleAgents
    {
        private HttpClient _httpClient;
        private string _apiKey;
        private string _apiEndpoint;
        private AIServiceType _serviceType;
        private string _modelName;

        private const string ROOF_MODULE_PROMPT = @"你是屋顶模块生成器。根据以下参数生成屋顶方块JSON。

参数：宽度{width}, 屋顶类型{roof_type}, Y范围{y_start}-{y_end}, 方块ID{tile_id}

任务：生成屋顶tiles数组。

规则：
- 人字形(gable): 中心x={center}最高(y={y_start}), 向两侧斜坡下降
- 左侧(x<{center})用slope=2(右斜), 右侧(x>{center})用slope=1(左斜)
- 中心点slope=0

输出示例：
{""tiles"": [{""x"":0,""y"":{y_start},""tile_id"":{tile_id},""slope"":2},{""x"":1,""y"":{y_start},""tile_id"":{tile_id},""slope"":2}]}

立即输出JSON，不要解释。";

        private const string WALL_MODULE_PROMPT = @"你是墙壁模块生成器。根据以下参数生成墙壁方块JSON。

参数：宽度{width}, 高度{height}, 墙厚{thickness}, 方块ID{tile_id}, 墙ID{wall_id}

任务：生成外墙tiles和内部wallRanges。

规则：
- 左墙: x=0, y从0到{height}-1
- 右墙: x={width}-1, y从0到{height}-1
- 内部墙背景: wallRanges填充

输出示例：
{""tiles"": [{""x"":0,""y"":0,""tile_id"":{tile_id}], ""wallRanges"": [{""x1"":1,""y1"":1,""x2"":{width}-2,""y2"":{height}-2,""wall_id"":{wall_id}]}

立即输出JSON，不要解释。";

        private const string FLOOR_MODULE_PROMPT = @"你是楼层模块生成器。根据以下参数生成楼层方块JSON。

参数：宽度{width}, Y范围{y_start}-{y_end}, 楼层号{floor_num}, 方块ID{tile_id}, 墙ID{wall_id}

任务：生成地板tiles和墙背景。

规则：
- 地板在y={y_end}位置，x从1到{width}-2
- 天花板在y={y_start}位置

输出示例：
{""tiles"": [{""x"":1,""y"":{y_end},""tile_id"":{tile_id}], ""wallRanges"": [{""x1"":1,""y1"":{y_start},""x2"":{width}-2,""y2"":{y_end},""wall_id"":{wall_id}]}

立即输出JSON，不要解释。";

        private const string WINDOW_MODULE_PROMPT = @"你是窗户模块生成器。根据以下参数生成窗户方块JSON。

参数：窗户位置{positions}, 玻璃ID{glass_id}, 窗框ID{frame_id}

任务：在每个窗户位置生成玻璃方块。

规则：
- 玻璃(tile_id={glass_id})
- 窗户宽2高2，生成4个玻璃方块

输出示例：
{""tiles"": [{""x"":3,""y"":6,""tile_id"":{glass_id},{""x"":4,""y"":6,""tile_id"":{glass_id},{""x"":3,""y"":7,""tile_id"":{glass_id},{""x"":4,""y"":7,""tile_id"":{glass_id}]}

立即输出JSON，不要解释。";

        private const string FURNITURE_MODULE_PROMPT = @"你是家具模块生成器。根据以下参数生成家具JSON。

参数：位置{positions}, 工作台ID{workbench_id}, 桌子ID{table_id}, 椅子ID{chair_id}, 门ID{door_id}

任务：生成furniture、doors、lightSources数组。

规则：
- 工作台放角落，桌子椅子成套摆放
- 门放底部中心
- 火把(tile_id=4)放墙边两侧

输出示例：
{""furniture"": [{""x"":3,""y"":6,""tile_id"":{workbench_id},{""x"":5,""y"":6,""tile_id"":{table_id},{""x"":6,""y"":6,""tile_id"":{chair_id}], ""doors"": [{""x"":5,""y"":7,""tile_id"":{door_id}], ""lightSources"": [{""x"":1,""y"":3,""tile_id"":4},{""x"":9,""y"":3,""tile_id"":4}]}

立即输出JSON，不要解释。";

        public ModuleAgents(string apiKey, AIServiceType serviceType, string modelName)
        {
            _apiKey = apiKey;
            _serviceType = serviceType;
            _modelName = modelName;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            ConfigureApiClient();
        }

        private void ConfigureApiClient()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            if (_serviceType == AIServiceType.DeepSeek)
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

        public async Task<ModuleResult> GenerateRoofAsync(BuildingPlan plan, Action<string> progressCallback = null, CancellationToken ct = default)
        {
            var roofRegion = plan.RoofRegion;
            if (roofRegion == null)
                return new ModuleResult { ModuleName = "roof", IsError = true, ErrorMessage = "无屋顶区域定义" };

            progressCallback?.Invoke("生成屋顶模块...");

            var kb = KnowledgeBaseManager.Instance;
            kb.Initialize();
            var candidates = kb.Tiles.SearchTiles(null, "brick");
            var semanticResults = kb.Vectors.SearchTilesSemantic(candidates, plan.Style, 10);
            int roofTileId = semanticResults.FirstOrDefault()?.id ?? candidates.FirstOrDefault()?.id ?? 4;

            int yStart = roofRegion.YRange?[0] ?? 0;
            int center = plan.Width / 2;

            string prompt = ROOF_MODULE_PROMPT
                .Replace("{width}", plan.Width.ToString())
                .Replace("{roof_type}", roofRegion.Type ?? "gable")
                .Replace("{y_start}", yStart.ToString())
                .Replace("{center}", center.ToString())
                .Replace("{tile_id}", roofTileId.ToString())
                .Replace("{style}", plan.Style);

            try
            {
                var tiles = await CallModuleAgentAsync(prompt, "roof", ct);
                return new ModuleResult { ModuleName = "roof", Tiles = tiles };
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"屋顶模块生成失败: {ex.Message}");
                return new ModuleResult { ModuleName = "roof", IsError = true, ErrorMessage = ex.Message };
            }
        }

        public async Task<ModuleResult> GenerateWallsAsync(BuildingPlan plan, Action<string> progressCallback = null, CancellationToken ct = default)
        {
            var wallRegion = plan.WallRegion;
            progressCallback?.Invoke("生成墙壁模块...");

            var kb = KnowledgeBaseManager.Instance;
            kb.Initialize();

            var tileCandidates = kb.Tiles.SearchTiles(null, "basic");
            var tileResults = kb.Vectors.SearchTilesSemantic(tileCandidates, plan.Style, 10);
            int wallTileId = tileResults.FirstOrDefault()?.id ?? tileCandidates.FirstOrDefault()?.id ?? 4;

            var wallCandidates = kb.Tiles.GetAllWalls();
            var wallResults = kb.Vectors.SearchWallsSemantic(wallCandidates, plan.Style, 10);
            int wallId = wallResults.FirstOrDefault()?.id ?? wallCandidates.FirstOrDefault()?.id ?? 4;

            string prompt = WALL_MODULE_PROMPT
                .Replace("{width}", plan.Width.ToString())
                .Replace("{height}", plan.Height.ToString())
                .Replace("{thickness}", (wallRegion?.Thickness ?? 1).ToString())
                .Replace("{tile_id}", wallTileId.ToString())
                .Replace("{wall_id}", wallId.ToString())
                .Replace("{style}", plan.Style);

            try
            {
                var response = await CallModuleAgentFullAsync(prompt, "walls", ct);
                return new ModuleResult
                {
                    ModuleName = "walls",
                    Tiles = response.Tiles,
                    WallRanges = response.WallRanges
                };
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"墙壁模块生成失败: {ex.Message}");
                return new ModuleResult { ModuleName = "walls", IsError = true, ErrorMessage = ex.Message };
            }
        }

        public async Task<ModuleResult> GenerateFloorsAsync(BuildingPlan plan, Action<string> progressCallback = null, CancellationToken ct = default)
        {
            progressCallback?.Invoke("生成楼层模块...");

            var kb = KnowledgeBaseManager.Instance;
            kb.Initialize();
            var candidates = kb.Tiles.SearchTiles(null, "wood");
            var semanticResults = kb.Vectors.SearchTilesSemantic(candidates, plan.Style, 10);
            int floorTileId = semanticResults.FirstOrDefault()?.id ?? candidates.FirstOrDefault()?.id ?? 5;
            var walls = kb.Tiles.GetAllWalls();
            int floorWallId = walls.FirstOrDefault(w => w.name.Contains("Wood"))?.id ?? walls.FirstOrDefault()?.id ?? 4;

            var floorRegions = plan.FloorRegions;
            if (floorRegions == null || floorRegions.Count == 0)
                return new ModuleResult { ModuleName = "floors", IsError = true, ErrorMessage = "无楼层区域定义" };

            var allTiles = new List<TileData>();
            var allWallRanges = new List<WallRangeData>();

            foreach (var floor in floorRegions)
            {
                string prompt = FLOOR_MODULE_PROMPT
                    .Replace("{width}", plan.Width.ToString())
                    .Replace("{y_start}", (floor.YRange?[0] ?? 0).ToString())
                    .Replace("{y_end}", (floor.YRange?[1] ?? 0).ToString())
                    .Replace("{floor_num}", floor.Name.Replace("floor", ""))
                    .Replace("{tile_id}", floorTileId.ToString())
                    .Replace("{wall_id}", floorWallId.ToString())
                    .Replace("{style}", plan.Style);

                try
                {
                    var response = await CallModuleAgentFullAsync(prompt, floor.Name, ct);
                    allTiles.AddRange(response.Tiles);
                    allWallRanges.AddRange(response.WallRanges);
                }
                catch (Exception ex)
                {
                    trab.Instance?.Logger.Warn($"楼层{floor.Name}生成失败: {ex.Message}");
                }
            }

            return new ModuleResult { ModuleName = "floors", Tiles = allTiles, WallRanges = allWallRanges };
        }

        public async Task<ModuleResult> GenerateWindowsAsync(BuildingPlan plan, Action<string> progressCallback = null, CancellationToken ct = default)
        {
            progressCallback?.Invoke("生成窗户模块...");

            var kb = KnowledgeBaseManager.Instance;
            kb.Initialize();
            var glassCandidates = kb.Tiles.SearchTiles(null, "transparent");
            var glassResults = kb.Vectors.SearchTilesSemantic(glassCandidates, plan.Style, 10);
            int glassTileId = glassResults.FirstOrDefault()?.id ?? glassCandidates.FirstOrDefault()?.id ?? 13;

            var positions = plan.WindowPositions;
            if (positions == null || positions.Count == 0)
                return new ModuleResult { ModuleName = "windows" };

            string positionsStr = positions.Select(p => $"({p.X},{p.Y})").Aggregate((a, b) => a + "," + b);
            string prompt = WINDOW_MODULE_PROMPT
                .Replace("{positions}", positionsStr)
                .Replace("{window_type}", positions.FirstOrDefault()?.Type ?? "double")
                .Replace("{glass_id}", glassTileId.ToString())
                .Replace("{style}", plan.Style);

            try
            {
                var tiles = await CallModuleAgentAsync(prompt, "windows", ct);
                return new ModuleResult { ModuleName = "windows", Tiles = tiles };
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"窗户模块生成失败: {ex.Message}");
                return new ModuleResult { ModuleName = "windows", IsError = true, ErrorMessage = ex.Message };
            }
        }

        public async Task<ModuleResult> GenerateFurnitureAsync(BuildingPlan plan, Action<string> progressCallback = null, CancellationToken ct = default)
        {
            progressCallback?.Invoke("生成家具模块...");

            var kb = KnowledgeBaseManager.Instance;
            kb.Initialize();

            var furnitureCandidates = kb.Furniture.SearchFurniture(null, null);
            var furnitureResults = kb.Vectors.SearchFurnitureSemantic(furnitureCandidates, plan.Style, 10);
            int workbenchId = furnitureResults.FirstOrDefault(f => f.Key.Contains("WorkBench")).Value?.tile_id ?? 17;
            int tableId = furnitureResults.FirstOrDefault(f => f.Key.Contains("Table")).Value?.tile_id ?? 87;
            int chairId = furnitureResults.FirstOrDefault(f => f.Key.Contains("Chair")).Value?.tile_id ?? 88;
            int doorId = furnitureResults.FirstOrDefault(f => f.Key.Contains("Door")).Value?.tile_id ?? 10;

            var positions = plan.FurniturePositions;
            string positionsStr = positions.Count > 0
                ? positions.Select(p => $"{p.Type}@({p.X},{p.Y})").Aggregate((a, b) => a + "," + b)
                : "自动布局";

            string prompt = FURNITURE_MODULE_PROMPT
                .Replace("{positions}", positionsStr)
                .Replace("{workbench_id}", workbenchId.ToString())
                .Replace("{table_id}", tableId.ToString())
                .Replace("{chair_id}", chairId.ToString())
                .Replace("{door_id}", doorId.ToString())
                .Replace("{style}", plan.Style);

            try
            {
                var response = await CallModuleAgentFurnitureAsync(prompt, "furniture", ct);
                return new ModuleResult
                {
                    ModuleName = "furniture",
                    Furniture = response.Furniture,
                    Doors = response.Doors,
                    LightSources = response.LightSources
                };
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"家具模块生成失败: {ex.Message}");
                return new ModuleResult { ModuleName = "furniture", IsError = true, ErrorMessage = ex.Message };
            }
        }

        #region API调用

        private async Task<List<TileData>> CallModuleAgentAsync(string prompt, string moduleName, CancellationToken ct)
        {
            var requestBody = new
            {
                model = _modelName,
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 2048
            };

            string json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiEndpoint, content, ct);
            string responseJson = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"API错误: {responseJson}");

            var apiResponse = JsonConvert.DeserializeObject<OpenAIResponse>(responseJson);
            var messageContent = apiResponse.choices?[0]?.message?.content;

            if (string.IsNullOrEmpty(messageContent))
                throw new Exception("API返回空内容");

            return ParseTilesFromJson(messageContent);
        }

        private async Task<ModuleResult> CallModuleAgentFullAsync(string prompt, string moduleName, CancellationToken ct)
        {
            var requestBody = new
            {
                model = _modelName,
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 2048
            };

            string json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiEndpoint, content, ct);
            string responseJson = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"API错误: {responseJson}");

            var apiResponse = JsonConvert.DeserializeObject<OpenAIResponse>(responseJson);
            var messageContent = apiResponse.choices?[0]?.message?.content;

            if (string.IsNullOrEmpty(messageContent))
                throw new Exception("API返回空内容");

            return ParseFullModuleFromJson(messageContent, moduleName);
        }

        private async Task<ModuleResult> CallModuleAgentFurnitureAsync(string prompt, string moduleName, CancellationToken ct)
        {
            var requestBody = new
            {
                model = _modelName,
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 1024
            };

            string json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiEndpoint, content, ct);
            string responseJson = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"API错误: {responseJson}");

            var apiResponse = JsonConvert.DeserializeObject<OpenAIResponse>(responseJson);
            var messageContent = apiResponse.choices?[0]?.message?.content;

            if (string.IsNullOrEmpty(messageContent))
                throw new Exception("API返回空内容");

            return ParseFurnitureFromJson(messageContent, moduleName);
        }

        #endregion

        #region JSON解析

        private List<TileData> ParseTilesFromJson(string content)
        {
            string json = ExtractJson(content);
            if (json == null) return new List<TileData>();

            try
            {
                var obj = JObject.Parse(json);
                var tilesArray = obj["tiles"] as JArray;
                if (tilesArray == null) return new List<TileData>();

                return tilesArray.Select(t => new TileData
                {
                    X = t["x"]?.Value<int>() ?? 0,
                    Y = t["y"]?.Value<int>() ?? 0,
                    TileId = t["tile_id"]?.Value<int>() ?? 4,
                    Slope = t["slope"]?.Value<int>() ?? 0,
                    Paint = t["paint"]?.Value<int>() ?? 0
                }).ToList();
            }
            catch
            {
                return new List<TileData>();
            }
        }

        private ModuleResult ParseFullModuleFromJson(string content, string moduleName)
        {
            string json = ExtractJson(content);
            if (json == null) return new ModuleResult { ModuleName = moduleName };

            try
            {
                var obj = JObject.Parse(json);
                var result = new ModuleResult { ModuleName = moduleName };

                var tilesArray = obj["tiles"] as JArray;
                if (tilesArray != null)
                {
                    result.Tiles = tilesArray.Select(t => new TileData
                    {
                        X = t["x"]?.Value<int>() ?? 0,
                        Y = t["y"]?.Value<int>() ?? 0,
                        TileId = t["tile_id"]?.Value<int>() ?? 4,
                        Slope = t["slope"]?.Value<int>() ?? 0
                    }).ToList();
                }

                var wallsArray = obj["wallRanges"] as JArray;
                if (wallsArray != null)
                {
                    result.WallRanges = wallsArray.Select(w => new WallRangeData
                    {
                        X1 = w["x1"]?.Value<int>() ?? 0,
                        Y1 = w["y1"]?.Value<int>() ?? 0,
                        X2 = w["x2"]?.Value<int>() ?? 0,
                        Y2 = w["y2"]?.Value<int>() ?? 0,
                        WallId = w["wall_id"]?.Value<int>() ?? 4
                    }).ToList();
                }

                return result;
            }
            catch
            {
                return new ModuleResult { ModuleName = moduleName, IsError = true };
            }
        }

        private ModuleResult ParseFurnitureFromJson(string content, string moduleName)
        {
            string json = ExtractJson(content);
            if (json == null) return new ModuleResult { ModuleName = moduleName };

            try
            {
                var obj = JObject.Parse(json);
                var result = new ModuleResult { ModuleName = moduleName };

                var furnitureArray = obj["furniture"] as JArray;
                if (furnitureArray != null)
                {
                    result.Furniture = furnitureArray.Select(f => new FurnitureData
                    {
                        X = f["x"]?.Value<int>() ?? 0,
                        Y = f["y"]?.Value<int>() ?? 0,
                        TileId = f["tile_id"]?.Value<int>() ?? 17
                    }).ToList();
                }

                var doorsArray = obj["doors"] as JArray;
                if (doorsArray != null)
                {
                    result.Doors = doorsArray.Select(d => new DoorData
                    {
                        X = d["x"]?.Value<int>() ?? 0,
                        Y = d["y"]?.Value<int>() ?? 0,
                        TileId = d["tile_id"]?.Value<int>() ?? 10
                    }).ToList();
                }

                var lightsArray = obj["lightSources"] as JArray;
                if (lightsArray != null)
                {
                    result.LightSources = lightsArray.Select(l => new LightSourceData
                    {
                        X = l["x"]?.Value<int>() ?? 0,
                        Y = l["y"]?.Value<int>() ?? 0,
                        TileId = l["tile_id"]?.Value<int>() ?? 4
                    }).ToList();
                }

                return result;
            }
            catch
            {
                return new ModuleResult { ModuleName = moduleName };
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
}