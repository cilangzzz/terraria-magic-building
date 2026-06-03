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

        /// <summary>
        /// 多维度模板检索
        /// 支持按风格、类型、特征、尺寸等条件组合检索
        /// </summary>
        public List<TemplateSearchResult> SearchTemplates(TemplateSearchCriteria criteria, int topK = 5)
        {
            if (!_initialized)
                return new List<TemplateSearchResult>();

            var results = new List<(BuildingEntity entity, float score, Dictionary<string, float> scoreBreakdown)>();

            foreach (var kvp in _buildings)
            {
                var entity = kvp.Value;
                var scoreBreakdown = new Dictionary<string, float>();
                float totalScore = 0f;

                // 风格匹配
                if (!string.IsNullOrEmpty(criteria.style))
                {
                    float styleScore = CalculateStyleScore(entity, criteria.style);
                    if (styleScore > 0)
                    {
                        scoreBreakdown["style"] = styleScore;
                        totalScore += styleScore * (criteria.styleWeight > 0 ? criteria.styleWeight : 1f);
                    }
                }

                // 类型匹配
                if (!string.IsNullOrEmpty(criteria.buildingType))
                {
                    float typeScore = CalculateTypeScore(entity, criteria.buildingType);
                    if (typeScore > 0)
                    {
                        scoreBreakdown["type"] = typeScore;
                        totalScore += typeScore * (criteria.typeWeight > 0 ? criteria.typeWeight : 1f);
                    }
                }

                // 特征匹配
                if (criteria.features != null && criteria.features.Count > 0)
                {
                    float featureScore = CalculateFeatureScore(entity, criteria.features);
                    if (featureScore > 0)
                    {
                        scoreBreakdown["features"] = featureScore;
                        totalScore += featureScore * (criteria.featureWeight > 0 ? criteria.featureWeight : 1f);
                    }
                }

                // 尺寸匹配
                if (criteria.minWidth > 0 || criteria.maxWidth > 0 || criteria.minHeight > 0 || criteria.maxHeight > 0)
                {
                    float sizeScore = CalculateSizeScore(entity, criteria);
                    if (sizeScore > 0)
                    {
                        scoreBreakdown["size"] = sizeScore;
                        totalScore += sizeScore * (criteria.sizeWeight > 0 ? criteria.sizeWeight : 1f);
                    }
                }

                // 材料匹配
                if (criteria.requiredMaterials != null && criteria.requiredMaterials.Count > 0)
                {
                    float materialScore = CalculateMaterialScore(entity, criteria.requiredMaterials);
                    if (materialScore > 0)
                    {
                        scoreBreakdown["materials"] = materialScore;
                        totalScore += materialScore * (criteria.materialWeight > 0 ? criteria.materialWeight : 1f);
                    }
                }

                // NPC适合性
                if (criteria.npcType != null)
                {
                    float npcScore = CalculateNPCScore(entity, criteria.npcType);
                    if (npcScore > 0)
                    {
                        scoreBreakdown["npc"] = npcScore;
                        totalScore += npcScore * (criteria.npcWeight > 0 ? criteria.npcWeight : 1f);
                    }
                }

                if (totalScore > 0)
                {
                    results.Add((entity, totalScore, scoreBreakdown));
                }
            }

            return results
                .OrderByDescending(r => r.score)
                .Take(topK)
                .Select(r => new TemplateSearchResult
                {
                    id = r.entity.id,
                    dimensions = r.entity.dimensions,
                    style_tags = r.entity.style_tags,
                    features = r.entity.features,
                    summary = r.entity.summary,
                    score = r.score,
                    score_breakdown = r.scoreBreakdown
                })
                .ToList();
        }

        /// <summary>
        /// 向量相似度检索
        /// 使用语义向量进行相似建筑搜索
        /// </summary>
        public List<TemplateSearchResult> SearchByVectorSimilarity(float[] queryVector, int topK = 5, float minSimilarity = 0.3f)
        {
            if (!_initialized || queryVector == null || _buildingVectors.Count == 0)
                return new List<TemplateSearchResult>();

            var results = new List<(string id, float similarity)>();

            foreach (var kvp in _buildingVectors)
            {
                float sim = CosineSimilarity(queryVector, kvp.Value);
                if (sim > minSimilarity)
                {
                    results.Add((kvp.Key, sim));
                }
            }

            return results
                .OrderByDescending(r => r.similarity)
                .Take(topK)
                .Where(r => _buildings.ContainsKey(r.id))
                .Select(r =>
                {
                    var entity = _buildings[r.id];
                    return new TemplateSearchResult
                    {
                        id = entity.id,
                        dimensions = entity.dimensions,
                        style_tags = entity.style_tags,
                        features = entity.features,
                        summary = entity.summary,
                        score = r.similarity,
                        score_breakdown = new Dictionary<string, float> { ["vector_similarity"] = r.similarity }
                    };
                })
                .ToList();
        }

        /// <summary>
        /// 获取材料清单
        /// 返回指定建筑的所有材料信息
        /// </summary>
        public MaterialListResult GetMaterialListForAI(string id)
        {
            var entity = GetBuilding(id);
            if (entity == null) return null;

            var result = new MaterialListResult
            {
                building_id = id,
                tiles = new List<MaterialInfo>(),
                walls = new List<MaterialInfo>()
            };

            if (entity.materials?.primary_tiles != null)
            {
                foreach (var tile in entity.materials.primary_tiles)
                {
                    result.tiles.Add(new MaterialInfo
                    {
                        id = tile.id,
                        name = tile.name,
                        count = tile.count,
                        category = GetTileCategory(tile.name)
                    });
                }
            }

            if (entity.materials?.primary_walls != null)
            {
                foreach (var wall in entity.materials.primary_walls)
                {
                    result.walls.Add(new MaterialInfo
                    {
                        id = wall.id,
                        name = wall.name,
                        count = wall.count,
                        category = "wall"
                    });
                }
            }

            // 计算总量
            result.total_tiles = result.tiles.Sum(t => t.count);
            result.total_walls = result.walls.Sum(w => w.count);

            return result;
        }

        /// <summary>
        /// 混合检索：结合向量相似度和多维度匹配
        /// </summary>
        public List<TemplateSearchResult> HybridSearch(float[] queryVector, TemplateSearchCriteria criteria, int topK = 5, float vectorWeight = 0.4f)
        {
            var vectorResults = SearchByVectorSimilarity(queryVector, topK * 2, 0.2f);
            var criteriaResults = SearchTemplates(criteria, topK * 2);

            var combined = new Dictionary<string, TemplateSearchResult>();

            // 合并向量检索结果
            foreach (var r in vectorResults)
            {
                if (!combined.ContainsKey(r.id))
                {
                    combined[r.id] = r;
                    combined[r.id].score *= vectorWeight;
                }
            }

            // 合并条件检索结果
            foreach (var r in criteriaResults)
            {
                if (combined.ContainsKey(r.id))
                {
                    combined[r.id].score += r.score * (1f - vectorWeight);
                    combined[r.id].score_breakdown["criteria_match"] = r.score;
                }
                else
                {
                    combined[r.id] = r;
                    combined[r.id].score *= (1f - vectorWeight);
                }
            }

            return combined.Values
                .OrderByDescending(r => r.score)
                .Take(topK)
                .ToList();
        }

        #region 检索评分辅助方法

        private float CalculateStyleScore(BuildingEntity entity, string style)
        {
            float score = 0f;
            string styleLower = style.ToLower();

            if (entity.style_tags != null)
            {
                foreach (var tag in entity.style_tags)
                {
                    if (tag.Contains(styleLower))
                        score += 0.3f;
                    if (tag == styleLower)
                        score += 0.2f;
                }
            }

            if (entity.features?.style?.Contains(styleLower) == true)
                score += 0.5f;

            return Math.Min(score, 1f);
        }

        private float CalculateTypeScore(BuildingEntity entity, string buildingType)
        {
            float score = 0f;
            string typeLower = buildingType.ToLower();

            if (entity.features?.type == typeLower)
                score = 1f;
            else if (entity.features?.type?.Contains(typeLower) == true)
                score = 0.5f;

            return score;
        }

        private float CalculateFeatureScore(BuildingEntity entity, List<string> features)
        {
            float score = 0f;
            int matched = 0;

            foreach (var feature in features)
            {
                string featureLower = feature.ToLower();

                if (entity.features?.structure?.Contains(featureLower) == true)
                    matched++;
                if (entity.features?.complexity?.Contains(featureLower) == true)
                    matched++;
                if (entity.features?.progress?.Contains(featureLower) == true)
                    matched++;
                if (entity.style_tags?.Any(t => t.Contains(featureLower)) == true)
                    matched++;
            }

            score = (float)matched / features.Count;
            return score;
        }

        private float CalculateSizeScore(BuildingEntity entity, TemplateSearchCriteria criteria)
        {
            if (entity.dimensions == null) return 0f;

            float widthScore = 0f;
            float heightScore = 0f;

            int width = entity.dimensions.width;
            int height = entity.dimensions.height;

            // 宽度匹配
            if (criteria.minWidth > 0 && criteria.maxWidth > 0)
            {
                if (width >= criteria.minWidth && width <= criteria.maxWidth)
                    widthScore = 1f;
                else if (width >= criteria.minWidth - 5 && width <= criteria.maxWidth + 5)
                    widthScore = 0.5f;
            }
            else if (criteria.minWidth > 0)
            {
                if (width >= criteria.minWidth)
                    widthScore = 1f;
                else if (width >= criteria.minWidth - 5)
                    widthScore = 0.5f;
            }
            else if (criteria.maxWidth > 0)
            {
                if (width <= criteria.maxWidth)
                    widthScore = 1f;
                else if (width <= criteria.maxWidth + 5)
                    widthScore = 0.5f;
            }

            // 高度匹配
            if (criteria.minHeight > 0 && criteria.maxHeight > 0)
            {
                if (height >= criteria.minHeight && height <= criteria.maxHeight)
                    heightScore = 1f;
                else if (height >= criteria.minHeight - 3 && height <= criteria.maxHeight + 3)
                    heightScore = 0.5f;
            }
            else if (criteria.minHeight > 0)
            {
                if (height >= criteria.minHeight)
                    heightScore = 1f;
                else if (height >= criteria.minHeight - 3)
                    heightScore = 0.5f;
            }
            else if (criteria.maxHeight > 0)
            {
                if (height <= criteria.maxHeight)
                    heightScore = 1f;
                else if (height <= criteria.maxHeight + 3)
                    heightScore = 0.5f;
            }

            return (widthScore + heightScore) / 2f;
        }

        private float CalculateMaterialScore(BuildingEntity entity, List<string> requiredMaterials)
        {
            if (entity.materials == null) return 0f;

            int matched = 0;
            foreach (var required in requiredMaterials)
            {
                string requiredLower = required.ToLower();

                if (entity.materials.primary_tiles?.Any(t => t.name?.ToLower().Contains(requiredLower) == true) == true)
                    matched++;
                if (entity.materials.primary_walls?.Any(w => w.name?.ToLower().Contains(requiredLower) == true) == true)
                    matched++;
            }

            return (float)matched / requiredMaterials.Count;
        }

        private float CalculateNPCScore(BuildingEntity entity, string npcType)
        {
            // 基于建筑特征判断NPC适合性
            float score = 0f;
            string npcLower = npcType?.ToLower() ?? "";

            // 基本房屋要求检查
            if (entity.dimensions?.width >= 7 && entity.dimensions?.height >= 7)
                score += 0.3f;

            // 特定NPC偏好
            var npcPreferences = new Dictionary<string, List<string>>
            {
                ["merchant"] = new List<string> { "storage", "shop" },
                ["nurse"] = new List<string> { "medical", "clean" },
                ["wizard"] = new List<string> { "magic", "bookshelf", "crystal" },
                ["smith"] = new List<string> { "forge", "anvil", "metal" },
                ["guide"] = new List<string> { "library", "map" }
            };

            if (npcPreferences.ContainsKey(npcLower))
            {
                var preferences = npcPreferences[npcLower];
                foreach (var pref in preferences)
                {
                    if (entity.style_tags?.Any(t => t.Contains(pref)) == true)
                        score += 0.2f;
                }
            }

            return Math.Min(score, 1f);
        }

        private string GetTileCategory(string tileName)
        {
            if (string.IsNullOrEmpty(tileName)) return "misc";

            string nameLower = tileName.ToLower();

            if (nameLower.Contains("brick") || nameLower.Contains("stone") || nameLower.Contains("slab"))
                return "structural";
            if (nameLower.Contains("wood") || nameLower.Contains("plank"))
                return "wood";
            if (nameLower.Contains("glass") || nameLower.Contains("window"))
                return "glass";
            if (nameLower.Contains("door") || nameLower.Contains("platform"))
                return "entry";
            if (nameLower.Contains("torch") || nameLower.Contains("lamp") || nameLower.Contains("lantern") || nameLower.Contains("chandelier"))
                return "light";
            if (nameLower.Contains("chair") || nameLower.Contains("table") || nameLower.Contains("bed") || nameLower.Contains("sofa"))
                return "furniture";
            if (nameLower.Contains("painting") || nameLower.Contains("statue") || nameLower.Contains("plant"))
                return "decoration";

            return "misc";
        }

        #endregion

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

    /// <summary>
    /// 模板检索条件
    /// </summary>
    public class TemplateSearchCriteria
    {
        public string style { get; set; }
        public string buildingType { get; set; }
        public List<string> features { get; set; }
        public List<string> requiredMaterials { get; set; }
        public string npcType { get; set; }

        public int minWidth { get; set; }
        public int maxWidth { get; set; }
        public int minHeight { get; set; }
        public int maxHeight { get; set; }

        // 权重配置（可选）
        public float styleWeight { get; set; }
        public float typeWeight { get; set; }
        public float featureWeight { get; set; }
        public float sizeWeight { get; set; }
        public float materialWeight { get; set; }
        public float npcWeight { get; set; }
    }

    /// <summary>
    /// 模板检索结果
    /// </summary>
    public class TemplateSearchResult
    {
        public string id { get; set; }
        public Dimensions dimensions { get; set; }
        public List<string> style_tags { get; set; }
        public Features features { get; set; }
        public string summary { get; set; }
        public float score { get; set; }
        public Dictionary<string, float> score_breakdown { get; set; }
    }

    /// <summary>
    /// 材料清单结果
    /// </summary>
    public class MaterialListResult
    {
        public string building_id { get; set; }
        public List<MaterialInfo> tiles { get; set; }
        public List<MaterialInfo> walls { get; set; }
        public int total_tiles { get; set; }
        public int total_walls { get; set; }
    }

    /// <summary>
    /// 材料信息
    /// </summary>
    public class MaterialInfo
    {
        public int id { get; set; }
        public string name { get; set; }
        public int count { get; set; }
        public string category { get; set; }
    }

    #endregion
}