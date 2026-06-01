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
using trab.Data;

namespace trab.Core
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

        // 模块生成系统提示（每个模块不同）
        private const string ROOF_MODULE_PROMPT = @"屋顶模块生成。输出紧凑JSON。

输入格式：宽度={width}, 屋顶类型={roof_type}, Y范围={y_start}-{y_end}, 风格={style}

输出格式：
{""tiles"": [{""x"":0,""y"":0,""tile_id"":4,""slope"":1}]}

规则：
- 人字形(gable): 中心最高，两侧斜坡下降，slope=1左斜,slope=2右斜
- 平顶(flat): 水平一层
- 圆顶(dome): 弧形上升
- 宝塔(pagoda): 多层阶梯

常用ID: 灰砖4|木材5|石板143";

        private const string WALL_MODULE_PROMPT = @"墙壁模块生成。输出紧凑JSON。

输入格式：宽度={width}, 高度={height}, 墙厚={thickness}, 风格={style}

输出格式：
{""tiles"": [{""x"":0,""y"":0,""tile_id"":4}], ""wallRanges"": [{""x1"":2,""y1"":1,""x2"":10,""y2"":8,""wall_id"":4}]}

规则：
- 外墙用厚砖块
- 内部填充墙背景
- 预留门窗位置

常用ID: 灰砖4|石头1|木墙4|石墙1";

        private const string FLOOR_MODULE_PROMPT = @"楼层模块生成。输出紧凑JSON。

输入格式：宽度={width}, Y范围={y_start}-{y_end}, 楼层号={floor_num}, 风格={style}

输出格式：
{""tiles"": [{""x"":2,""y"":5,""tile_id"":5}], ""wallRanges"": [{""x1"":2,""y1"":5,""x2"":10,""y2"":8,""wall_id"":4}]}

规则：
- 地板用木材或石板
- 天花板用不同方块区分楼层
- 预留楼梯位置

常用ID: 木材5|石板143|灰砖4";

        private const string WINDOW_MODULE_PROMPT = @"窗户模块生成。输出紧凑JSON。

输入格式：窗户位置列表={positions}, 窗户类型={window_type}, 风格={style}

输出格式：
{""tiles"": [{""x"":3,""y"":6,""tile_id"":13},{""x"":4,""y"":6,""tile_id"":13}]}

规则：
- 玻璃(tile_id=13)
- 窗框用砖块包围
- 2x2双窗，1x2单窗

常用ID: 玻璃13|灰砖4";

        private const string FURNITURE_MODULE_PROMPT = @"家具模块生成。输出紧凑JSON。

输入格式：家具位置列表={positions}, 风格={style}

输出格式：
{""furniture"": [{""x"":5,""y"":8,""tile_id"":17}], ""doors"": [{""x"":3,""y"":1,""tile_id"":10}], ""lightSources"": [{""x"":2,""y"":3,""tile_id"":4}]}

规则：
- 工作台(17)桌子(87)椅子(88)成套摆放
- 门放地面层
- 火把放墙边

常用ID: 工作台17|桌子87|椅子88|门10|火把4|宝箱21";

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

        /// <summary>
        /// 生成屋顶模块
        /// </summary>
        public async Task<ModuleResult> GenerateRoofAsync(BuildingPlan plan, Action<string> progressCallback = null, CancellationToken ct = default)
        {
            var roofRegion = plan.RoofRegion;
            if (roofRegion == null)
                return new ModuleResult { ModuleName = "roof", IsError = true, ErrorMessage = "无屋顶区域定义" };

            progressCallback?.Invoke("生成屋顶模块...");

            string prompt = ROOF_MODULE_PROMPT
                .Replace("{width}", plan.Width.ToString())
                .Replace("{roof_type}", roofRegion.Type ?? "gable")
                .Replace("{y_start}", (roofRegion.YRange?[0] ?? 0).ToString())
                .Replace("{y_end}", (roofRegion.YRange?[1] ?? 3).ToString())
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

        /// <summary>
        /// 生成墙壁模块
        /// </summary>
        public async Task<ModuleResult> GenerateWallsAsync(BuildingPlan plan, Action<string> progressCallback = null, CancellationToken ct = default)
        {
            var wallRegion = plan.WallRegion;
            progressCallback?.Invoke("生成墙壁模块...");

            string prompt = WALL_MODULE_PROMPT
                .Replace("{width}", plan.Width.ToString())
                .Replace("{height}", plan.Height.ToString())
                .Replace("{thickness}", (wallRegion?.Thickness ?? 1).ToString())
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

        /// <summary>
        /// 生成楼层模块
        /// </summary>
        public async Task<ModuleResult> GenerateFloorsAsync(BuildingPlan plan, Action<string> progressCallback = null, CancellationToken ct = default)
        {
            progressCallback?.Invoke("生成楼层模块...");

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

        /// <summary>
        /// 生成窗户模块
        /// </summary>
        public async Task<ModuleResult> GenerateWindowsAsync(BuildingPlan plan, Action<string> progressCallback = null, CancellationToken ct = default)
        {
            progressCallback?.Invoke("生成窗户模块...");

            var positions = plan.WindowPositions;
            if (positions == null || positions.Count == 0)
                return new ModuleResult { ModuleName = "windows" };

            string positionsStr = positions.Select(p => $"({p.X},{p.Y})").Aggregate((a, b) => a + "," + b);
            string prompt = WINDOW_MODULE_PROMPT
                .Replace("{positions}", positionsStr)
                .Replace("{window_type}", positions.FirstOrDefault()?.Type ?? "double")
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

        /// <summary>
        /// 生成家具模块
        /// </summary>
        public async Task<ModuleResult> GenerateFurnitureAsync(BuildingPlan plan, Action<string> progressCallback = null, CancellationToken ct = default)
        {
            progressCallback?.Invoke("生成家具模块...");

            var positions = plan.FurniturePositions;
            string positionsStr = positions.Count > 0
                ? positions.Select(p => $"{p.Type}@({p.X},{p.Y})").Aggregate((a, b) => a + "," + b)
                : "自动布局";

            string prompt = FURNITURE_MODULE_PROMPT
                .Replace("{positions}", positionsStr)
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
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 2048  // 模块JSON小，不需要太大
            };

            string json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiEndpoint, content, ct);
            string responseJson = await response.Content.ReadAsStringAsync(ct);

            trab.Instance?.Logger.Info($"[{moduleName}] API响应: {responseJson.Substring(0, Math.Min(500, responseJson.Length))}...");

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
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 2048
            };

            string json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiEndpoint, content, ct);
            string responseJson = await response.Content.ReadAsStringAsync(ct);

            trab.Instance?.Logger.Info($"[{moduleName}] API响应: {responseJson.Substring(0, Math.Min(500, responseJson.Length))}...");

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
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 1024
            };

            string json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiEndpoint, content, ct);
            string responseJson = await response.Content.ReadAsStringAsync(ct);

            trab.Instance?.Logger.Info($"[{moduleName}] API响应: {responseJson.Substring(0, Math.Min(500, responseJson.Length))}...");

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
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"解析tiles失败: {ex.Message}");
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

                // 解析tiles
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

                // 解析wallRanges
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
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"解析模块{moduleName}失败: {ex.Message}");
                return new ModuleResult { ModuleName = moduleName, IsError = true, ErrorMessage = ex.Message };
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

                // 解析furniture
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

                // 解析doors
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

                // 解析lightSources
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
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"解析家具模块失败: {ex.Message}");
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