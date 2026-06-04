using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using trab.Core.KnowledgeBase;

namespace trab.Core.Agents.Tools
{
    /// <summary>
    /// 构件级建筑检索工具
    /// 根据风格、类型、复杂性层次检索建筑模板
    /// </summary>
    public class SearchBuildingsTool : BaseAgentTool
    {
        public override string Name => "search_buildings";

        public override string Description => "检索建筑模板。返回建筑ID、名称、尺寸、风格、构件列表。用于找到合适的参考建筑。";

        public override JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["style"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "建筑风格: asian(中式), medieval(中世纪), fantasy(奇幻), snow(雪地), desert(沙漠), modern(现代)"
                },
                ["building_type"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "建筑类型: house(住宅), tower(塔楼), castle(城堡), shop(商店), temple(神庙)"
                },
                ["complexity"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "复杂性层次: atomic(原子构件), composite(复合构件), building(完整建筑), complex(建筑群)",
                    ["enum"] = new JArray { "atomic", "composite", "building", "complex" }
                },
                ["min_width"] = new JObject { ["type"] = "integer", ["description"] = "最小宽度" },
                ["max_width"] = new JObject { ["type"] = "integer", ["description"] = "最大宽度" },
                ["min_height"] = new JObject { ["type"] = "integer", ["description"] = "最小高度" },
                ["max_height"] = new JObject { ["type"] = "integer", ["description"] = "最大高度" },
                ["npc_valid"] = new JObject { ["type"] = "boolean", ["description"] = "是否需要NPC房屋功能" },
                ["top_k"] = new JObject { ["type"] = "integer", ["description"] = "返回数量(默认5)", ["default"] = 5 }
            }
        };

        public override Task<ToolResult> ExecuteAsync(JObject input, KnowledgeBaseManager kb)
        {
            var criteria = new BuildingSearchCriteria
            {
                style = GetStringParam(input, "style"),
                building_type = GetStringParam(input, "building_type"),
                min_width = GetIntParam(input, "min_width"),
                max_width = GetIntParam(input, "max_width"),
                min_height = GetIntParam(input, "min_height"),
                max_height = GetIntParam(input, "max_height"),
                npc_valid = GetBoolParam(input, "npc_valid"),
                top_k = GetIntParam(input, "top_k") ?? 5
            };

            // 解析复杂性层次
            string complexityStr = GetStringParam(input, "complexity");
            if (!string.IsNullOrEmpty(complexityStr))
            {
                criteria.complexity = Enum.Parse<BuildingComplexity>(complexityStr, true);
            }

            // 检索建筑
            var results = kb.Components.SearchBuildings(criteria);

            var summaries = results.Select(r => new
            {
                id = r.id,
                name = r.name,
                complexity = r.complexity.ToString(),
                building_type = r.building_type,
                dimensions = new { width = r.dimensions?.width ?? 0, height = r.dimensions?.height ?? 0 },
                style_tags = r.style_tags,
                summary = r.summary,
                similarity = r.similarity,
                available_components = r.available_components,
                note = "使用 get_building_details 获取完整信息，使用 get_component_rules 获取构件生成规则"
            }).ToList();

            var result = new
            {
                buildings = summaries,
                total = summaries.Count,
                search_criteria = criteria,
                message = summaries.Count > 0
                    ? $"找到{summaries.Count}个匹配建筑。调用 get_building_details 获取详情。"
                    : "未找到匹配建筑，建议使用 get_style_materials 获取风格推荐后直接生成。"
            };

            return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(result)));
        }
    }

    /// <summary>
    /// 建筑详情获取工具
    /// </summary>
    public class GetBuildingDetailsTool : BaseAgentTool
    {
        public override string Name => "get_building_details";

        public override string Description => "获取建筑的完整信息，包括构件列表、建造顺序、NPC房屋验证。用于深入了解建筑结构。";

        public override JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["building_id"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "建筑ID，从 search_buildings 返回"
                }
            },
            ["required"] = new JArray { "building_id" }
        };

        public override Task<ToolResult> ExecuteAsync(JObject input, KnowledgeBaseManager kb)
        {
            string buildingId = GetStringParam(input, "building_id");

            if (string.IsNullOrEmpty(buildingId))
            {
                return Task.FromResult(ToolResult.Error("building_id 参数为空"));
            }

            // 获取建筑详情
            var building = kb.Components.GetBuilding(buildingId);
            if (building == null)
            {
                // 尝试从旧格式获取
                var legacyBuilding = kb.Buildings.GetBuilding(buildingId);
                if (legacyBuilding != null)
                {
                    return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(new
                    {
                        id = legacyBuilding.id,
                        source = legacyBuilding.source,
                        dimensions = legacyBuilding.dimensions,
                        features = legacyBuilding.features,
                        materials = legacyBuilding.materials,
                        functions = legacyBuilding.functions,
                        style_tags = legacyBuilding.style_tags,
                        building_sequence = legacyBuilding.building_sequence,
                        summary = legacyBuilding.summary,
                        npc_suitable = legacyBuilding.npc_suitable,
                        note = "旧格式数据，建议迁移到新格式"
                    })));
                }

                return Task.FromResult(ToolResult.Error($"未找到建筑: {buildingId}"));
            }

            // 获取构件定义
            var components = kb.Components.GetBuildingComponents(buildingId);

            // 获取AI可读描述
            string aiDescription = kb.Components.GetBuildingDescriptionForAI(buildingId);

            var result = new
            {
                id = building.id,
                name = building.name,
                complexity = building.complexity.ToString(),
                building_type = building.building_type,
                dimensions = building.dimensions,
                style_tags = building.style_tags,
                components = components.Select(c => new
                {
                    id = c.id,
                    type = c.type,
                    subtype = c.subtype,
                    materials = c.materials,
                    generation_rule = c.generation_rule?.pattern,
                    parameters = c.parameters
                }),
                build_sequence = building.build_sequence,
                npc_requirements = building.npc_requirements,
                summary = building.summary,
                ai_readable_description = aiDescription,
                note = "参考 components 中的 generation_rule 理解生成方式，使用 get_component_rules 获取详细规则。"
            };

            return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(result)));
        }
    }

    /// <summary>
    /// 构件生成规则获取工具
    /// </summary>
    public class GetComponentRulesTool : BaseAgentTool
    {
        public override string Name => "get_component_rules";

        public override string Description => "获取构件的详细生成规则和参数说明。用于理解如何程序化生成该构件。";

        public override JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["component_id"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "构件ID，从 get_building_details 返回"
                },
                ["scale"] = new JObject
                {
                    ["type"] = "number",
                    ["description"] = "缩放比例(默认1.0)，用于调整构件尺寸"
                }
            },
            ["required"] = new JArray { "component_id" }
        };

        public override Task<ToolResult> ExecuteAsync(JObject input, KnowledgeBaseManager kb)
        {
            string componentId = GetStringParam(input, "component_id");
            double? scale = GetDoubleParam(input, "scale");

            if (string.IsNullOrEmpty(componentId))
            {
                return Task.FromResult(ToolResult.Error("component_id 参数为空"));
            }

            var component = kb.Components.GetComponent(componentId);
            if (component == null)
            {
                return Task.FromResult(ToolResult.Error($"未找到构件: {componentId}"));
            }

            // 应用缩放
            var scaledParams = ApplyScale(component.parameters, scale ?? 1.0);

            var result = new
            {
                id = component.id,
                type = component.type,
                subtype = component.subtype,
                generation_rule = new
                {
                    pattern = component.generation_rule?.pattern,
                    formula = component.generation_rule?.formula,
                    params_required = component.generation_rule?.params_required,
                    description = GetRuleDescription(component.generation_rule?.pattern)
                },
                original_parameters = component.parameters,
                scaled_parameters = scaledParams,
                materials = component.materials,
                scale_applied = scale ?? 1.0,
                note = "使用 scaled_parameters 和 materials 在 generate_design_rules 中定义构件。"
            };

            return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(result)));
        }

        private Dictionary<string, object> ApplyScale(Dictionary<string, object> parameters, double scale)
        {
            if (parameters == null || scale == 1.0)
                return parameters;

            var result = new Dictionary<string, object>();
            foreach (var kvp in parameters)
            {
                if (kvp.Value is int intValue)
                {
                    result[kvp.Key] = (int)(intValue * scale);
                }
                else if (kvp.Value is double doubleValue)
                {
                    result[kvp.Key] = doubleValue * scale;
                }
                else
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            return result;
        }

        private string GetRuleDescription(string pattern)
        {
            return pattern switch
            {
                "pagoda_tiered" => "分层宝塔屋顶：每层宽度递减2，高度固定。参数：tier_count(层数), base_width(底层宽), height_per_tier(每层高)",
                "pyramid_step" => "阶梯金字塔：逐层向上缩进。参数：step_width(每步缩进), step_height(每步高度)",
                "framed_rectangle" => "框架矩形墙：外围框架+内部填充。参数：width, height, thickness, has_frame",
                "linear_spacing" => "线性排列装饰：沿直线均匀分布。参数：start_x, end_x, y, spacing(间距)",
                "horizontal_line" => "水平线条地板：水平铺设。参数：y, thickness, x_start, x_end",
                "solid_rectangle" => "实心矩形地基：填充矩形区域。参数：y_start, height, width",
                _ => $"生成模式: {pattern}"
            };
        }
    }

    /// <summary>
    /// 风格材料推荐工具
    /// </summary>
    public class GetStyleMaterialsTool : BaseAgentTool
    {
        public override string Name => "get_style_materials";

        public override string Description => "获取风格的推荐材料列表。返回方块、墙壁、装饰、家具推荐。用于确定建筑材料方案。";

        public override JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["style"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "建筑风格: asian, medieval, fantasy, snow, desert, modern, natural"
                }
            },
            ["required"] = new JArray { "style" }
        };

        public override Task<ToolResult> ExecuteAsync(JObject input, KnowledgeBaseManager kb)
        {
            string style = GetStringParam(input, "style");

            if (string.IsNullOrEmpty(style))
            {
                return Task.FromResult(ToolResult.Error("style 参数为空"));
            }

            var mapping = kb.Components.GetStyleMaterials(style);
            if (mapping == null)
            {
                // 返回可用风格列表
                var availableStyles = kb.Components.GetAllStyles();
                return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(new
                {
                    requested_style = style,
                    available_styles = availableStyles,
                    message = $"未找到风格 '{style}'，请使用可用风格之一。"
                })));
            }

            var result = new
            {
                style = mapping.style,
                tiles = mapping.tiles?.Select(t => new
                {
                    id = t.id,
                    name = t.name,
                    use = t.use,
                    note = $"使用 tile_id={t.id} 作为{t.use}"
                }),
                walls = mapping.walls?.Select(w => new
                {
                    id = w.id,
                    name = w.name,
                    use = w.use,
                    note = $"使用 wall_id={w.id} 作为{w.use}"
                }),
                decorations = mapping.decorations?.Select(d => new
                {
                    id = d.id,
                    name = d.name,
                    category = d.category,
                    note = $"装饰类: {d.category}"
                }),
                doors = mapping.doors,
                furniture = mapping.furniture,
                color_tone = mapping.color_tone,
                colors = mapping.colors,
                note = "tile_id用于方块，wall_id用于墙壁背景，两者不同。"
            };

            return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(result)));
        }
    }
}