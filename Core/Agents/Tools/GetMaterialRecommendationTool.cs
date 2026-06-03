using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using trab.Core.KnowledgeBase;

namespace trab.Core.Agents.Tools
{
    /// <summary>
    /// 材料推荐工具
    /// 根据风格和建筑类型推荐材料组合
    /// </summary>
    public class GetMaterialRecommendationTool : BaseAgentTool
    {
        public override string Name => "get_material_recommendation";

        public override string Description => "根据风格获取推荐的材料组合。返回主方块、墙壁、地板、屋顶等推荐材料ID。用于快速确定建筑材料方案。";

        public override JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["style"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "建筑风格: asian, medieval, fantasy, snow, desert, modern, natural"
                },
                ["building_type"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "建筑类型: house, castle, tower, shop, temple"
                },
                ["biome"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "所在生物群落（可选）"
                }
            },
            ["required"] = new JArray { "style" }
        };

        public override Task<ToolResult> ExecuteAsync(JObject input, KnowledgeBaseManager kb)
        {
            string style = GetStringParam(input, "style")?.ToLower() ?? "";
            string buildingType = GetStringParam(input, "building_type")?.ToLower() ?? "house";
            string biome = GetStringParam(input, "biome")?.ToLower();

            // 根据风格获取推荐材料组合
            var recommendation = GetStyleRecommendation(style, buildingType, biome, kb);

            return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(recommendation)));
        }

        private object GetStyleRecommendation(string style, string buildingType, string biome, KnowledgeBaseManager kb)
        {
            // 风格预设推荐
            var presets = new Dictionary<string, Dictionary<string, object>>
            {
                ["asian"] = new Dictionary<string, object>
                {
                    ["primary_tile"] = new { name = "Gold Brick", id = 7, note = "金色装饰" },
                    ["secondary_tile"] = new { name = "Stone Slab", id = 142, note = "石板框架" },
                    ["primary_wall"] = new { name = "Marble Wall", id = 14, note = "大理石墙" },
                    ["secondary_wall"] = new { name = "Dynasty Wall", id = 634, note = "朝代墙" },
                    ["floor_tile"] = new { name = "Wood", id = 30, note = "木地板" },
                    ["roof_type"] = "pagoda",
                    ["decorations"] = new[]
                    {
                        new { type = "lantern", name = "Pine Lantern", placement = "every_n_tiles", spacing = 8 }
                    },
                    ["color_tone"] = new { primary = "warm", colors = new[] { "gold", "brown", "red" } }
                },
                ["medieval"] = new Dictionary<string, object>
                {
                    ["primary_tile"] = new { name = "Gray Brick", id = 6, note = "灰砖主墙" },
                    ["secondary_tile"] = new { name = "Stone Slab", id = 142, note = "石板框架" },
                    ["primary_wall"] = new { name = "Gray Brick Wall", id = 5, note = "灰砖墙" },
                    ["secondary_wall"] = new { name = "Planked Wall", id = 27, note = "木板墙" },
                    ["floor_tile"] = new { name = "Wood", id = 30, note = "木地板" },
                    ["roof_type"] = "gable",
                    ["decorations"] = new[]
                    {
                        new { type = "light", name = "Torch", placement = "corners" },
                        new { type = "light", name = "Chandelier", placement = "center" }
                    },
                    ["color_tone"] = new { primary = "neutral", colors = new[] { "gray", "brown", "stone" } }
                },
                ["fantasy"] = new Dictionary<string, object>
                {
                    ["primary_tile"] = new { name = "Pearlstone Brick", id = 22, note = "珍珠石" },
                    ["secondary_tile"] = new { name = "Crystal Block", id = 107, note = "水晶装饰" },
                    ["primary_wall"] = new { name = "Pearlstone Wall", id = 16, note = "珍珠石墙" },
                    ["secondary_wall"] = new { name = "Blue Stained Glass", id = 70, note = "蓝彩玻璃" },
                    ["floor_tile"] = new { name = "Glass", id = 13, note = "玻璃地板" },
                    ["roof_type"] = "dome",
                    ["decorations"] = new[]
                    {
                        new { type = "light", name = "Gemspark Block", placement = "edges" }
                    },
                    ["color_tone"] = new { primary = "cool", colors = new[] { "blue", "pink", "white" } }
                },
                ["snow"] = new Dictionary<string, object>
                {
                    ["primary_tile"] = new { name = "Snow Brick", id = 149, note = "雪砖" },
                    ["secondary_tile"] = new { name = "Ice Block", id = 44, note = "冰块" },
                    ["primary_wall"] = new { name = "Snow Wall", id = 13, note = "雪墙" },
                    ["secondary_wall"] = new { name = "Ice Wall", id = 71, note = "冰墙" },
                    ["floor_tile"] = new { name = "Boreal Wood", id = 48, note = "北境木" },
                    ["roof_type"] = "gable",
                    ["decorations"] = new[]
                    {
                        new { type = "heat", name = "Fireplace", placement = "center" },
                        new { type = "light", name = "Chinese Lantern", placement = "every_n_tiles", spacing = 6 }
                    },
                    ["color_tone"] = new { primary = "cold", colors = new[] { "white", "blue", "cyan" } }
                },
                ["desert"] = new Dictionary<string, object>
                {
                    ["primary_tile"] = new { name = "Sandstone Brick", id = 54, note = "砂岩砖" },
                    ["secondary_tile"] = new { name = "Palm Wood", id = 49, note = "棕榈木" },
                    ["primary_wall"] = new { name = "Sandstone Wall", id = 12, note = "砂岩墙" },
                    ["secondary_wall"] = new { name = "Cactus", id = 65, note = "仙人掌" },
                    ["floor_tile"] = new { name = "Sandstone Slab", id = 144, note = "砂岩板" },
                    ["roof_type"] = "flat",
                    ["decorations"] = new[]
                    {
                        new { type = "light", name = "Campfire", placement = "corners" }
                    },
                    ["color_tone"] = new { primary = "warm", colors = new[] { "tan", "brown", "orange" } }
                },
                ["modern"] = new Dictionary<string, object>
                {
                    ["primary_tile"] = new { name = "Granite", id = 43, note = "花岗岩" },
                    ["secondary_tile"] = new { name = "Glass", id = 13, note = "玻璃" },
                    ["primary_wall"] = new { name = "Granite Wall", id = 15, note = "花岗岩墙" },
                    ["secondary_wall"] = new { name = "Glass Wall", id = 10, note = "玻璃墙" },
                    ["floor_tile"] = new { name = "Marble", id = 42, note = "大理石" },
                    ["roof_type"] = "flat",
                    ["decorations"] = new[]
                    {
                        new { type = "light", name = "Lamp", placement = "edges" }
                    },
                    ["color_tone"] = new { primary = "neutral", colors = new[] { "white", "gray", "black" } }
                }
            };

            if (!presets.TryGetValue(style, out var preset))
            {
                // 返回默认推荐
                preset = presets["medieval"];
            }

            // 根据建筑类型调整
            if (buildingType == "castle")
            {
                preset["roof_type"] = "dome";
            }
            else if (buildingType == "tower")
            {
                preset["roof_type"] = "cone";
            }

            return new
            {
                style = style,
                building_type = buildingType,
                biome = biome,
                recommendation = preset,
                note = "以上为风格推荐，可使用 search_materials 获取更多选项"
            };
        }
    }
}
