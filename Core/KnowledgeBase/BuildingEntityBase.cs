using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Terraria.ModLoader;

namespace trab.Core.KnowledgeBase
{
    /// <summary>
    /// 建筑实体知识库
    /// 存储玩家建筑的完整数据：方块网格、墙壁网格、材料统计、建造顺序
    /// Agent通过向量检索找到相似建筑，然后读取构造方式进行建造
    /// </summary>
    public class BuildingEntityBase
    {
        private Dictionary<string, BuildingEntity> _buildings;
        private Dictionary<string, float[]> _buildingVectors;
        private Dictionary<string, BuildingDetailData> _buildingDetails;
        private bool _initialized = false;

        public bool IsInitialized => _initialized;
        public int BuildingCount => _buildings?.Count ?? 0;

        public void Initialize()
        {
            if (_initialized) return;

            _buildings = new Dictionary<string, BuildingEntity>();
            _buildingVectors = new Dictionary<string, float[]>();
            _buildingDetails = new Dictionary<string, BuildingDetailData>();

            try
            {
                LoadBuildingEntities();
                LoadBuildingVectors();
                LoadBuildingDetails();
                _initialized = true;

                trab.Instance?.Logger.Info($"建筑实体库初始化完成: {BuildingCount}个建筑, {_buildingDetails.Count}个详细数据");
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"建筑实体库初始化失败: {ex.Message}");
            }
        }

        private void LoadBuildingEntities()
        {
            string[] possiblePaths = GetPossibleDataPaths("building_entities.json");

            string foundPath = null;
            foreach (var p in possiblePaths)
            {
                if (File.Exists(p))
                {
                    foundPath = p;
                    break;
                }
            }

            if (foundPath == null)
            {
                trab.Instance?.Logger.Warn("建筑实体数据文件不存在，使用默认数据");
                InitDefaultEntities();
                return;
            }

            trab.Instance?.Logger.Info($"加载建筑实体: {foundPath}");
            string json = File.ReadAllText(foundPath);

            try
            {
                var jobj = JObject.Parse(json);

                if (jobj["buildings"] != null)
                {
                    var buildingsObj = jobj["buildings"] as JObject;
                    foreach (var kvp in buildingsObj)
                    {
                        var entity = JsonConvert.DeserializeObject<BuildingEntity>(kvp.Value.ToString());
                        if (entity != null)
                        {
                            _buildings[kvp.Key] = entity;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"解析建筑实体失败: {ex.Message}");
                InitDefaultEntities();
            }
        }

        private void LoadBuildingVectors()
        {
            string[] possiblePaths = GetPossibleDataPaths("building_vectors.json");

            string foundPath = possiblePaths.FirstOrDefault(p => File.Exists(p));
            if (foundPath == null) return;

            try
            {
                string json = File.ReadAllText(foundPath);
                var jobj = JObject.Parse(json);

                if (jobj["vectors"] != null)
                {
                    var vectorsObj = jobj["vectors"] as JObject;
                    foreach (var kvp in vectorsObj)
                    {
                        var embArray = kvp.Value as JArray;
                        if (embArray != null)
                        {
                            float[] vector = embArray.Select(v => (float)v).ToArray();
                            _buildingVectors[kvp.Key] = vector;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"加载建筑向量失败: {ex.Message}");
            }
        }

        private void LoadBuildingDetails()
        {
            var dataPaths = new List<string>();

            dataPaths.Add(Path.Combine(
                "C:", "Users", "admin", "Pictures", "Camera Roll",
                "20260602215014", "data.json"));

            string detailsDir = Path.Combine(
                "C:", "Users", "admin", "Documents", "My Games", "Terraria",
                "tModLoader", "ModSources", "trab", "Data", "details");

            if (Directory.Exists(detailsDir))
            {
                foreach (var file in Directory.GetFiles(detailsDir, "*_data.json"))
                {
                    dataPaths.Add(file);
                }
            }

            foreach (var path in dataPaths)
            {
                if (!File.Exists(path)) continue;

                try
                {
                    string json = File.ReadAllText(path);
                    var jobj = JObject.Parse(json);

                    string buildingId = Path.GetFileNameWithoutExtension(path).Replace("_data", "");
                    if (buildingId == "data")
                    {
                        buildingId = Path.GetFileName(Path.GetDirectoryName(path));
                    }

                    var detail = new BuildingDetailData
                    {
                        width = jobj["header"]?["width"]?.Value<int>() ?? 0,
                        height = jobj["header"]?["height"]?.Value<int>() ?? 0,
                        total_tiles = jobj["tile_stats"]?["total_tiles"]?.Value<int>() ?? 0,
                        active_tiles = jobj["tile_stats"]?["active_tiles"]?.Value<int>() ?? 0,
                        unique_tile_types = jobj["tile_stats"]?["unique_tile_types"]?.Value<int>() ?? 0,
                        unique_wall_types = jobj["wall_stats"]?["unique_wall_types"]?.Value<int>() ?? 0
                    };

                    var tileDist = jobj["tile_stats"]?["tile_distribution"] as JObject;
                    if (tileDist != null)
                    {
                        detail.tile_distribution = new Dictionary<string, int>();
                        foreach (var kvp in tileDist)
                        {
                            detail.tile_distribution[kvp.Key] = kvp.Value.Value<int>();
                        }
                    }

                    var wallDist = jobj["wall_stats"]?["wall_distribution"] as JObject;
                    if (wallDist != null)
                    {
                        detail.wall_distribution = new Dictionary<string, int>();
                        foreach (var kvp in wallDist)
                        {
                            detail.wall_distribution[kvp.Key] = kvp.Value.Value<int>();
                        }
                    }

                    var tilesSample = jobj["tiles_sample"] as JArray;
                    if (tilesSample != null && tilesSample.Count > 0)
                    {
                        detail.tiles_sample = new List<TileSample>();
                        foreach (var t in tilesSample.Take(100))
                        {
                            detail.tiles_sample.Add(new TileSample
                            {
                                x = t["x"]?.Value<int>() ?? 0,
                                y = t["y"]?.Value<int>() ?? 0,
                                type = t["type"]?.Value<int?>(),
                                type_name = t["type_name"]?.ToString(),
                                wall = t["wall"]?.Value<int?>(),
                                wall_name = t["wall_name"]?.ToString()
                            });
                        }
                    }

                    _buildingDetails[buildingId] = detail;
                    trab.Instance?.Logger.Info($"加载建筑详细数据: {buildingId} ({detail.width}x{detail.height}, {detail.active_tiles}个活跃方块)");
                }
                catch (Exception ex)
                {
                    trab.Instance?.Logger.Error($"加载建筑详细数据失败 {path}: {ex.Message}");
                }
            }
        }

        private void InitDefaultEntities()
        {
            _buildings["20260602215014"] = new BuildingEntity
            {
                id = "20260602215014",
                source = "QQ截图20260602215014.png",
                dimensions = new Dimensions { width = 53, height = 32 },
                features = new Features
                {
                    type = "residence",
                    style = "asian_fantasy",
                    progress = "mid_late_game",
                    complexity = "high",
                    structure = "multi_story"
                },
                materials = new Materials
                {
                    primary_tiles = new List<TileMaterial>
                    {
                        new TileMaterial { id = 179, name = "Gold", count = 179 },
                        new TileMaterial { id = 129, name = "Pine Lantern", count = 129 },
                        new TileMaterial { id = 104, name = "Cactus Door", count = 104 }
                    },
                    primary_walls = new List<WallMaterial>
                    {
                        new WallMaterial { id = 172, name = "Marble Wall", count = 172 },
                        new WallMaterial { id = 154, name = "Ebonwood Wall", count = 154 }
                    }
                },
                building_sequence = new List<BuildingStep>
                {
                    new BuildingStep { step = 1, action = "frame", materials = new List<string> { "Stone", "Stone Slab" }, note = "搭建主体框架" },
                    new BuildingStep { step = 2, action = "walls", materials = new List<string> { "Marble Wall", "Ebonwood Wall" }, note = "铺设背景墙" },
                    new BuildingStep { step = 3, action = "floor", materials = new List<string> { "Dirt", "Gold" }, note = "铺设地板和装饰" },
                    new BuildingStep { step = 4, action = "doors", materials = new List<string> { "Cactus Door" }, note = "安装入口" },
                    new BuildingStep { step = 5, action = "lights", materials = new List<string> { "Pine Lantern", "Blue Torch" }, note = "布置光源" },
                    new BuildingStep { step = 6, action = "furniture", materials = new List<string> { "Bookshelf", "Sink", "Teacup" }, note = "摆放家具" },
                    new BuildingStep { step = 7, action = "decor", materials = new List<string> { "Gold", "Gemspark" }, note = "添加装饰细节" }
                },
                style_tags = new List<string> { "asian", "fantasy", "residence", "gold", "lantern", "marble" },
                summary = "中式奇幻风格多层住宅，使用金块装饰、大理石墙、松木灯笼"
            };
        }

        public List<BuildingEntity> SearchByStyle(string style, int topK = 5)
        {
            if (!_initialized || string.IsNullOrEmpty(style))
                return new List<BuildingEntity>();

            var results = new List<(BuildingEntity entity, float score)>();

            foreach (var kvp in _buildings)
            {
                var entity = kvp.Value;
                float score = 0f;

                if (entity.style_tags != null && entity.style_tags.Contains(style.ToLower()))
                {
                    score += 0.5f;
                }

                if (entity.features?.type == style.ToLower())
                {
                    score += 0.3f;
                }

                if (entity.features?.style?.Contains(style.ToLower()) == true)
                {
                    score += 0.4f;
                }

                if (score > 0)
                {
                    results.Add((entity, score));
                }
            }

            return results
                .OrderByDescending(r => r.score)
                .Take(topK)
                .Select(r => r.entity)
                .ToList();
        }

        public List<BuildingEntity> SearchByVector(float[] queryVector, int topK = 5)
        {
            if (!_initialized || queryVector == null || _buildingVectors.Count == 0)
                return new List<BuildingEntity>();

            var results = new List<(string id, float similarity)>();

            foreach (var kvp in _buildingVectors)
            {
                float sim = CosineSimilarity(queryVector, kvp.Value);
                if (sim > 0.3f)
                {
                    results.Add((kvp.Key, sim));
                }
            }

            return results
                .OrderByDescending(r => r.similarity)
                .Take(topK)
                .Where(r => _buildings.ContainsKey(r.id))
                .Select(r => _buildings[r.id])
                .ToList();
        }

        public BuildingEntity GetBuilding(string id)
        {
            if (!_initialized || string.IsNullOrEmpty(id))
                return null;

            _buildings.TryGetValue(id, out var entity);
            return entity;
        }

        public List<BuildingStep> GetBuildingSequence(string id)
        {
            var entity = GetBuilding(id);
            return entity?.building_sequence;
        }

        public (List<TileMaterial> tiles, List<WallMaterial> walls) GetMaterialList(string id)
        {
            var entity = GetBuilding(id);
            return (entity?.materials?.primary_tiles, entity?.materials?.primary_walls);
        }

        public BuildingDetailData GetBuildingDetail(string id)
        {
            if (!_initialized || string.IsNullOrEmpty(id))
                return null;

            _buildingDetails.TryGetValue(id, out var detail);
            return detail;
        }

        public string GetBuildingDescriptionForAI(string id)
        {
            var entity = GetBuilding(id);
            var detail = GetBuildingDetail(id);

            if (entity == null) return null;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"建筑ID: {entity.id}");
            sb.AppendLine($"尺寸: {entity.dimensions?.width ?? 0}x{entity.dimensions?.height ?? 0}");
            sb.AppendLine($"类型: {entity.features?.type ?? "unknown"}, 风格: {entity.features?.style ?? "unknown"}");
            sb.AppendLine($"描述: {entity.summary}");

            if (detail != null)
            {
                sb.AppendLine($"\n方块统计:");
                sb.AppendLine($"- 总方块数: {detail.total_tiles}, 活跃方块: {detail.active_tiles}");
                sb.AppendLine($"- 方块种类: {detail.unique_tile_types}种, 墙壁种类: {detail.unique_wall_types}种");

                if (detail.tile_distribution != null && detail.tile_distribution.Count > 0)
                {
                    sb.AppendLine($"\n主要方块（按数量排序）:");
                    foreach (var kvp in detail.tile_distribution.OrderByDescending(t => t.Value).Take(10))
                    {
                        sb.AppendLine($"  - {kvp.Key}: {kvp.Value}个");
                    }
                }

                if (detail.wall_distribution != null && detail.wall_distribution.Count > 0)
                {
                    sb.AppendLine($"\n主要墙壁:");
                    foreach (var kvp in detail.wall_distribution.OrderByDescending(t => t.Value).Take(5))
                    {
                        sb.AppendLine($"  - {kvp.Key}: {kvp.Value}个");
                    }
                }
            }

            if (entity.building_sequence != null && entity.building_sequence.Count > 0)
            {
                sb.AppendLine($"\n建造顺序:");
                foreach (var step in entity.building_sequence)
                {
                    sb.AppendLine($"  {step.step}. {step.action}: {step.note}");
                    if (step.materials != null && step.materials.Count > 0)
                    {
                        sb.AppendLine($"     材料: {string.Join(", ", step.materials)}");
                    }
                }
            }

            return sb.ToString();
        }

        public List<string> GetAllBuildingIds()
        {
            return _buildings?.Keys.ToList() ?? new List<string>();
        }

        private float CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return 0f;

            float dot = 0f, magA = 0f, magB = 0f;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }

            if (magA == 0 || magB == 0)
                return 0f;

            return dot / (float)Math.Sqrt(magA * magB);
        }

        private string[] GetPossibleDataPaths(string filename)
        {
            var paths = new List<string>();

            string subDir = "vectors";

            paths.Add(Path.Combine(
                "C:", "Users", "admin", "Documents", "My Games", "Terraria",
                "tModLoader", "ModSources", "trab", "Data", subDir, filename));

            try
            {
                string basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrEmpty(basePath))
                {
                    paths.Add(Path.Combine(basePath, "Data", subDir, filename));
                }
            }
            catch { }

            return paths.ToArray();
        }
    }

    #region 建筑实体数据结构

    public class BuildingDetailData
    {
        public int width { get; set; }
        public int height { get; set; }
        public int total_tiles { get; set; }
        public int active_tiles { get; set; }
        public int unique_tile_types { get; set; }
        public int unique_wall_types { get; set; }
        public Dictionary<string, int> tile_distribution { get; set; }
        public Dictionary<string, int> wall_distribution { get; set; }
        public List<TileSample> tiles_sample { get; set; }
    }

    public class TileSample
    {
        public int x { get; set; }
        public int y { get; set; }
        public int? type { get; set; }
        public string type_name { get; set; }
        public int? wall { get; set; }
        public string wall_name { get; set; }
    }

    public class BuildingEntity
    {
        public string id { get; set; }
        public string source { get; set; }
        public Dimensions dimensions { get; set; }
        public Features features { get; set; }
        public Materials materials { get; set; }
        public Functions functions { get; set; }
        public List<string> style_tags { get; set; }
        public string summary { get; set; }
        public List<BuildingStep> building_sequence { get; set; }
        public int[][] tile_grid { get; set; }
        public int[][] wall_grid { get; set; }
    }

    public class Dimensions
    {
        public int width { get; set; }
        public int height { get; set; }
    }

    public class Features
    {
        public string type { get; set; }
        public string style { get; set; }
        public string progress { get; set; }
        public string complexity { get; set; }
        public string structure { get; set; }
    }

    public class Materials
    {
        public List<TileMaterial> primary_tiles { get; set; }
        public List<WallMaterial> primary_walls { get; set; }
    }

    public class TileMaterial
    {
        public int id { get; set; }
        public string name { get; set; }
        public int count { get; set; }
    }

    public class WallMaterial
    {
        public int id { get; set; }
        public string name { get; set; }
        public int count { get; set; }
    }

    public class Functions
    {
        public FunctionCount light_source { get; set; }
        public FunctionCount entry { get; set; }
        public FunctionCount storage { get; set; }
        public FunctionCount furniture { get; set; }
    }

    public class FunctionCount
    {
        public int count { get; set; }
        public List<string> items { get; set; }
    }

    public class BuildingStep
    {
        public int step { get; set; }
        public string action { get; set; }
        public List<string> materials { get; set; }
        public string note { get; set; }
    }

    #endregion
}