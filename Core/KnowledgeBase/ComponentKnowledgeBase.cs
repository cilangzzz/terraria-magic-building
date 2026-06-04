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
    /// 构件级建筑知识库
    /// 实现多层次架构：原子构件→复合构件→建筑→建筑群
    /// </summary>
    public class ComponentKnowledgeBase
    {
        private Dictionary<string, ComponentDefinition> _atomicComponents;
        private Dictionary<string, BuildingEntityV2> _buildings;
        private Dictionary<string, StyleMaterialMapping> _styleMaterials;
        private Dictionary<string, float[]> _buildingVectors;
        private Dictionary<string, float[]> _componentVectors;

        private bool _initialized = false;

        public bool IsInitialized => _initialized;
        public int BuildingCount => _buildings?.Count ?? 0;
        public int ComponentCount => _atomicComponents?.Count ?? 0;
        public int StyleCount => _styleMaterials?.Count ?? 0;

        public void Initialize()
        {
            if (_initialized) return;

            _atomicComponents = new Dictionary<string, ComponentDefinition>();
            _buildings = new Dictionary<string, BuildingEntityV2>();
            _styleMaterials = new Dictionary<string, StyleMaterialMapping>();
            _buildingVectors = new Dictionary<string, float[]>();
            _componentVectors = new Dictionary<string, float[]>();

            try
            {
                LoadBuildings();
                LoadComponents();
                LoadStyleMaterials();
                LoadVectors();
                _initialized = true;

                trab.Instance?.Logger.Info($"构件知识库初始化完成: {BuildingCount}个建筑, {ComponentCount}个构件, {StyleCount}种风格");
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"构件知识库初始化失败: {ex.Message}");
            }
        }

        #region 数据加载

        private void LoadBuildings()
        {
            var paths = GetPossibleDataPaths("buildings_v2.json");
            var foundPath = paths.FirstOrDefault(p => File.Exists(p));

            if (foundPath == null)
            {
                // 尝试加载旧格式
                LoadLegacyBuildings();
                return;
            }

            try
            {
                var json = File.ReadAllText(foundPath);
                var jobj = JObject.Parse(json);

                if (jobj["buildings"] != null)
                {
                    var buildingsObj = jobj["buildings"] as JObject;
                    foreach (var kvp in buildingsObj)
                    {
                        var building = JsonConvert.DeserializeObject<BuildingEntityV2>(kvp.Value.ToString());
                        if (building != null)
                        {
                            building.id = kvp.Key;
                            _buildings[kvp.Key] = building;
                        }
                    }
                }

                trab.Instance?.Logger.Info($"加载建筑数据: {foundPath}, {BuildingCount}个建筑");
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"加载建筑数据失败: {ex.Message}");
                LoadLegacyBuildings();
            }
        }

        private void LoadLegacyBuildings()
        {
            // 从旧格式加载
            var paths = GetPossibleDataPaths("building_entities.json");
            var foundPath = paths.FirstOrDefault(p => File.Exists(p));

            if (foundPath == null)
            {
                InitDefaultBuildings();
                return;
            }

            try
            {
                var json = File.ReadAllText(foundPath);
                var jobj = JObject.Parse(json);

                if (jobj["buildings"] != null)
                {
                    var buildingsObj = jobj["buildings"] as JObject;
                    foreach (var kvp in buildingsObj)
                    {
                        // 转换旧格式到新格式
                        var legacyEntity = JsonConvert.DeserializeObject<BuildingEntity>(kvp.Value.ToString());
                        if (legacyEntity != null)
                        {
                            var building = ConvertToV2(legacyEntity);
                            _buildings[building.id] = building;
                        }
                    }
                }

                trab.Instance?.Logger.Info($"从旧格式加载建筑数据: {BuildingCount}个建筑");
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"加载旧格式建筑数据失败: {ex.Message}");
                InitDefaultBuildings();
            }
        }

        private BuildingEntityV2 ConvertToV2(BuildingEntity legacy)
        {
            var building = new BuildingEntityV2
            {
                id = legacy.id,
                name = legacy.summary,
                complexity = BuildingComplexity.Building,
                building_type = legacy.features?.type ?? "unknown",
                dimensions = legacy.dimensions,
                style_tags = legacy.style_tags ?? new List<string>(),
                summary = legacy.summary,
                components = new List<ComponentReference>(),
                build_sequence = new List<string>()
            };

            // 从 building_sequence 提取构件引用
            if (legacy.building_sequence != null)
            {
                foreach (var step in legacy.building_sequence)
                {
                    building.build_sequence.Add(step.action);
                    building.components.Add(new ComponentReference
                    {
                        type = step.action,
                        role = step.note,
                        ref_id = $"{legacy.id}_{step.action}"
                    });
                }
            }

            // 从 materials 创建构件定义
            if (legacy.materials != null)
            {
                building.structure = new BuildingStructure
                {
                    decorations = new List<ComponentReference>()
                };

                if (legacy.materials.primary_tiles != null && legacy.materials.primary_tiles.Count > 0)
                {
                    var mainTile = legacy.materials.primary_tiles[0];
                    building.structure.roof = new ComponentReference
                    {
                        type = "roof",
                        ref_id = $"roof_{mainTile.name}",
                        role = "主要屋顶"
                    };
                }
            }

            // NPC验证
            if (legacy.functions != null)
            {
                building.npc_requirements = new NPCRequirements
                {
                    has_light_source = legacy.functions.light_source?.count > 0,
                    has_door = legacy.functions.entry?.count > 0,
                    has_table = legacy.functions.furniture?.count > 0,
                    has_chair = legacy.functions.furniture?.count > 0,
                    has_walls = true,
                    valid_house = legacy.npc_suitable?.is_valid_house ?? false
                };
            }

            return building;
        }

        private void LoadComponents()
        {
            var paths = GetPossibleDataPaths("atomic_components.json");
            var foundPath = paths.FirstOrDefault(p => File.Exists(p));

            if (foundPath == null)
            {
                InitDefaultComponents();
                return;
            }

            try
            {
                var json = File.ReadAllText(foundPath);
                var jobj = JObject.Parse(json);

                if (jobj["components"] != null)
                {
                    var componentsObj = jobj["components"] as JObject;
                    foreach (var kvp in componentsObj)
                    {
                        var component = JsonConvert.DeserializeObject<ComponentDefinition>(kvp.Value.ToString());
                        if (component != null)
                        {
                            component.id = kvp.Key;
                            _atomicComponents[kvp.Key] = component;
                        }
                    }
                }

                trab.Instance?.Logger.Info($"加载构件数据: {foundPath}, {ComponentCount}个构件");
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"加载构件数据失败: {ex.Message}");
                InitDefaultComponents();
            }
        }

        private void LoadStyleMaterials()
        {
            var paths = GetPossibleDataPaths("style_materials.json");
            var foundPath = paths.FirstOrDefault(p => File.Exists(p));

            if (foundPath == null)
            {
                InitDefaultStyleMaterials();
                return;
            }

            try
            {
                var json = File.ReadAllText(foundPath);
                var jobj = JObject.Parse(json);

                if (jobj["style_materials"] != null)
                {
                    var stylesObj = jobj["style_materials"] as JObject;
                    foreach (var kvp in stylesObj)
                    {
                        var style = JsonConvert.DeserializeObject<StyleMaterialMapping>(kvp.Value.ToString());
                        if (style != null)
                        {
                            style.style = kvp.Key;
                            _styleMaterials[kvp.Key] = style;
                        }
                    }
                }

                trab.Instance?.Logger.Info($"加载风格材料映射: {StyleCount}种风格");
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"加载风格材料映射失败: {ex.Message}");
                InitDefaultStyleMaterials();
            }
        }

        private void LoadVectors()
        {
            // 加载建筑向量
            var buildingVectorPath = GetPossibleDataPaths("building_vectors.json")
                .FirstOrDefault(p => File.Exists(p));

            if (buildingVectorPath != null)
            {
                try
                {
                    var json = File.ReadAllText(buildingVectorPath);
                    var jobj = JObject.Parse(json);

                    if (jobj["vectors"] != null)
                    {
                        var vectorsObj = jobj["vectors"] as JObject;
                        foreach (var kvp in vectorsObj)
                        {
                            var arr = kvp.Value as JArray;
                            if (arr != null)
                            {
                                _buildingVectors[kvp.Key] = arr.Select(v => (float)v).ToArray();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    trab.Instance?.Logger.Error($"加载建筑向量失败: {ex.Message}");
                }
            }
        }

        #endregion

        #region 默认数据

        private void InitDefaultBuildings()
        {
            var building = new BuildingEntityV2
            {
                id = "chinese_house_001",
                name = "中式奇幻住宅",
                complexity = BuildingComplexity.Building,
                building_type = "house",
                dimensions = new Dimensions { width = 53, height = 32 },
                style_tags = new List<string> { "asian", "fantasy", "residence" },
                summary = "中式奇幻风格多层住宅，使用金块装饰、大理石墙、松木灯笼",
                components = new List<ComponentReference>
                {
                    new ComponentReference { type = "foundation", ref_id = "foundation_stone", role = "地基" },
                    new ComponentReference { type = "wall", ref_id = "wall_marble", role = "外墙" },
                    new ComponentReference { type = "floor", ref_id = "floor_wood", role = "木地板", level = 1 },
                    new ComponentReference { type = "floor", ref_id = "floor_wood", role = "木地板", level = 2 },
                    new ComponentReference { type = "roof", ref_id = "roof_pagoda_gold", role = "金顶" },
                    new ComponentReference { type = "decoration", ref_id = "deco_lantern", role = "灯笼装饰" }
                },
                build_sequence = new List<string> { "foundation", "wall", "floor", "roof", "decoration" },
                npc_requirements = new NPCRequirements
                {
                    has_light_source = true,
                    has_door = true,
                    has_table = true,
                    has_chair = true,
                    has_walls = true,
                    valid_house = true
                }
            };

            _buildings[building.id] = building;
        }

        private void InitDefaultComponents()
        {
            // 宝塔屋顶
            _atomicComponents["roof_pagoda_gold"] = new ComponentDefinition
            {
                id = "roof_pagoda_gold",
                type = "roof",
                subtype = "pagoda",
                parameters = new Dictionary<string, object>
                {
                    ["tier_count"] = 3,
                    ["base_width"] = 53,
                    ["height_per_tier"] = 4,
                    ["overhang"] = 1
                },
                materials = new ComponentMaterials
                {
                    primary = new MaterialRef { tile_id = 179, name = "Gold" }
                },
                generation_rule = new GenerationRule
                {
                    pattern = "pagoda_tiered",
                    formula = "每层宽度减少2，高度4tiles",
                    params_required = new List<string> { "tier_count", "base_width", "height_per_tier" }
                }
            };

            // 大理石墙
            _atomicComponents["wall_marble"] = new ComponentDefinition
            {
                id = "wall_marble",
                type = "wall",
                subtype = "outer_wall",
                parameters = new Dictionary<string, object>
                {
                    ["thickness"] = 2,
                    ["has_frame"] = true
                },
                materials = new ComponentMaterials
                {
                    primary = new MaterialRef { wall_id = 172, name = "Marble Wall" },
                    frame = new MaterialRef { tile_id = 4, name = "Wood" }
                },
                generation_rule = new GenerationRule
                {
                    pattern = "framed_rectangle",
                    params_required = new List<string> { "width", "height", "thickness" }
                }
            };

            // 松木灯笼
            _atomicComponents["deco_lantern"] = new ComponentDefinition
            {
                id = "deco_lantern",
                type = "decoration",
                subtype = "hanging_lantern",
                parameters = new Dictionary<string, object>
                {
                    ["spacing"] = 3,
                    ["y_offset_from_ceiling"] = 1
                },
                materials = new ComponentMaterials
                {
                    primary = new MaterialRef { tile_id = 129, name = "Pine Lantern" }
                },
                generation_rule = new GenerationRule
                {
                    pattern = "linear_spacing",
                    formula = "每3格放置一个",
                    params_required = new List<string> { "start_x", "end_x", "y", "spacing" }
                }
            };
        }

        private void InitDefaultStyleMaterials()
        {
            _styleMaterials["asian"] = new StyleMaterialMapping
            {
                style = "asian",
                tiles = new List<StyleMaterialItem>
                {
                    new StyleMaterialItem { id = 179, name = "Gold", use = "roof, accent" },
                    new StyleMaterialItem { id = 353, name = "Dynasty Wood", use = "floor, frame" }
                },
                walls = new List<StyleMaterialItem>
                {
                    new StyleMaterialItem { id = 172, name = "Marble Wall", use = "outer_wall" },
                    new StyleMaterialItem { id = 154, name = "Ebonwood Wall", use = "inner_wall" }
                },
                decorations = new List<StyleMaterialItem>
                {
                    new StyleMaterialItem { id = 129, name = "Pine Lantern", category = "light" },
                    new StyleMaterialItem { id = 395, name = "Chinese Lantern", category = "light" }
                },
                color_tone = "warm",
                colors = new List<string> { "gold", "brown", "red" }
            };

            _styleMaterials["medieval"] = new StyleMaterialMapping
            {
                style = "medieval",
                tiles = new List<StyleMaterialItem>
                {
                    new StyleMaterialItem { id = 4, name = "Gray Brick", use = "wall, foundation" },
                    new StyleMaterialItem { id = 143, name = "Stone Slab", use = "floor" }
                },
                walls = new List<StyleMaterialItem>
                {
                    new StyleMaterialItem { id = 6, name = "Gray Brick Wall", use = "outer_wall" }
                },
                decorations = new List<StyleMaterialItem>
                {
                    new StyleMaterialItem { id = 4, name = "Torch", category = "light" },
                    new StyleMaterialItem { id = 33, name = "Banner", category = "decoration" }
                },
                color_tone = "neutral",
                colors = new List<string> { "gray", "brown", "stone" }
            };

            _styleMaterials["fantasy"] = new StyleMaterialMapping
            {
                style = "fantasy",
                tiles = new List<StyleMaterialItem>
                {
                    new StyleMaterialItem { id = 182, name = "Pearlstone", use = "main" },
                    new StyleMaterialItem { id = 179, name = "Gold", use = "accent" }
                },
                walls = new List<StyleMaterialItem>
                {
                    new StyleMaterialItem { id = 24, name = "Glass Wall", use = "outer_wall" }
                },
                decorations = new List<StyleMaterialItem>
                {
                    new StyleMaterialItem { id = 1045, name = "Crystal", category = "light" }
                },
                color_tone = "cool",
                colors = new List<string> { "blue", "pink", "white" }
            };
        }

        #endregion

        #region 检索方法

        /// <summary>
        /// 按条件搜索建筑
        /// </summary>
        public List<BuildingSearchResult> SearchBuildings(BuildingSearchCriteria criteria)
        {
            var results = new List<BuildingSearchResult>();

            foreach (var kvp in _buildings)
            {
                var building = kvp.Value;
                float score = 0f;

                // 风格匹配
                if (!string.IsNullOrEmpty(criteria.style))
                {
                    if (building.style_tags?.Any(t => t.Contains(criteria.style, StringComparison.OrdinalIgnoreCase)) == true)
                    {
                        score += 0.4f;
                    }
                }

                // 类型匹配
                if (!string.IsNullOrEmpty(criteria.building_type))
                {
                    if (building.building_type?.Equals(criteria.building_type, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        score += 0.3f;
                    }
                }

                // 尺寸匹配
                if (criteria.min_width.HasValue && building.dimensions?.width >= criteria.min_width)
                    score += 0.1f;
                if (criteria.max_width.HasValue && building.dimensions?.width <= criteria.max_width)
                    score += 0.1f;
                if (criteria.min_height.HasValue && building.dimensions?.height >= criteria.min_height)
                    score += 0.1f;
                if (criteria.max_height.HasValue && building.dimensions?.height <= criteria.max_height)
                    score += 0.1f;

                // NPC房屋要求
                if (criteria.npc_valid == true && building.npc_requirements?.valid_house == true)
                    score += 0.2f;

                if (score > 0)
                {
                    results.Add(new BuildingSearchResult
                    {
                        id = building.id,
                        name = building.name,
                        complexity = building.complexity,
                        building_type = building.building_type,
                        dimensions = building.dimensions,
                        style_tags = building.style_tags,
                        summary = building.summary,
                        similarity = score,
                        available_components = building.components?.Select(c => c.ref_id).ToList()
                    });
                }
            }

            return results
                .OrderByDescending(r => r.similarity)
                .Take(criteria.top_k)
                .ToList();
        }

        /// <summary>
        /// 获取建筑详情
        /// </summary>
        public BuildingEntityV2 GetBuilding(string id)
        {
            if (!_initialized || string.IsNullOrEmpty(id))
                return null;

            _buildings.TryGetValue(id, out var building);
            return building;
        }

        /// <summary>
        /// 获取构件定义
        /// </summary>
        public ComponentDefinition GetComponent(string id)
        {
            if (!_initialized || string.IsNullOrEmpty(id))
                return null;

            _atomicComponents.TryGetValue(id, out var component);
            return component;
        }

        /// <summary>
        /// 获取建筑的所有构件定义
        /// </summary>
        public List<ComponentDefinition> GetBuildingComponents(string buildingId)
        {
            var building = GetBuilding(buildingId);
            if (building == null || building.components == null)
                return new List<ComponentDefinition>();

            var components = new List<ComponentDefinition>();
            foreach (var compRef in building.components)
            {
                var comp = GetComponent(compRef.ref_id);
                if (comp != null)
                {
                    components.Add(comp);
                }
                else
                {
                    // 创建默认构件
                    components.Add(CreateDefaultComponent(compRef));
                }
            }

            return components;
        }

        private ComponentDefinition CreateDefaultComponent(ComponentReference compRef)
        {
            return new ComponentDefinition
            {
                id = compRef.ref_id ?? Guid.NewGuid().ToString(),
                type = compRef.type,
                parameters = new Dictionary<string, object>
                {
                    ["level"] = compRef.level ?? 1
                }
            };
        }

        /// <summary>
        /// 获取风格材料映射
        /// </summary>
        public StyleMaterialMapping GetStyleMaterials(string style)
        {
            if (!_initialized || string.IsNullOrEmpty(style))
                return null;

            _styleMaterials.TryGetValue(style.ToLower(), out var mapping);
            return mapping;
        }

        /// <summary>
        /// 获取所有风格名称
        /// </summary>
        public List<string> GetAllStyles()
        {
            return _styleMaterials?.Keys.ToList() ?? new List<string>();
        }

        /// <summary>
        /// 获取所有建筑ID
        /// </summary>
        public List<string> GetAllBuildingIds()
        {
            return _buildings?.Keys.ToList() ?? new List<string>();
        }

        /// <summary>
        /// 向量相似度检索
        /// </summary>
        public List<BuildingSearchResult> SearchByVector(float[] queryVector, int topK = 5, float minSimilarity = 0.3f)
        {
            if (!_initialized || queryVector == null || _buildingVectors.Count == 0)
                return new List<BuildingSearchResult>();

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
                    var building = _buildings[r.id];
                    return new BuildingSearchResult
                    {
                        id = building.id,
                        name = building.name,
                        complexity = building.complexity,
                        building_type = building.building_type,
                        dimensions = building.dimensions,
                        style_tags = building.style_tags,
                        summary = building.summary,
                        similarity = r.similarity,
                        available_components = building.components?.Select(c => c.ref_id).ToList()
                    };
                })
                .ToList();
        }

        /// <summary>
        /// 获取建筑的AI可读描述
        /// </summary>
        public string GetBuildingDescriptionForAI(string buildingId)
        {
            var building = GetBuilding(buildingId);
            if (building == null) return null;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"建筑ID: {building.id}");
            sb.AppendLine($"名称: {building.name}");
            sb.AppendLine($"类型: {building.building_type}");
            sb.AppendLine($"复杂性: {building.complexity}");
            sb.AppendLine($"尺寸: {building.dimensions?.width ?? 0}x{building.dimensions?.height ?? 0}");
            sb.AppendLine($"风格: {string.Join(", ", building.style_tags ?? new List<string>())}");
            sb.AppendLine($"描述: {building.summary}");

            if (building.components != null && building.components.Count > 0)
            {
                sb.AppendLine($"\n构件列表:");
                foreach (var comp in building.components)
                {
                    sb.AppendLine($"  - {comp.type}: {comp.ref_id} ({comp.role})");
                }
            }

            if (building.build_sequence != null && building.build_sequence.Count > 0)
            {
                sb.AppendLine($"\n建造顺序: {string.Join(" → ", building.build_sequence)}");
            }

            if (building.npc_requirements != null)
            {
                sb.AppendLine($"\nNPC房屋: {(building.npc_requirements.valid_house ? "✓ 有效" : "✗ 无效")}");
            }

            return sb.ToString();
        }

        #endregion

        #region 辅助方法

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

            // 向量数据目录
            paths.Add(Path.Combine(
                "C:", "Users", "admin", "Documents", "My Games", "Terraria",
                "tModLoader", "ModSources", "trab", "Data", "vectors", filename));

            // 知识库数据目录
            paths.Add(Path.Combine(
                "C:", "Users", "admin", "Documents", "My Games", "Terraria",
                "tModLoader", "ModSources", "trab", "Data", "kb", filename));

            // 数据目录
            paths.Add(Path.Combine(
                "C:", "Users", "admin", "Documents", "My Games", "Terraria",
                "tModLoader", "ModSources", "trab", "Data", filename));

            return paths.ToArray();
        }

        #endregion
    }
}
