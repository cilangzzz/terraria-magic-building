using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using trab.Core.KnowledgeBase;

namespace trab.Core.Agents.Tools
{
    /// <summary>
    /// 材料检索工具
    /// 检索方块或墙壁，返回ID、名称、属性
    /// </summary>
    public class SearchMaterialsTool : BaseAgentTool
    {
        public override string Name => "search_materials";

        public override string Description => "检索材料（方块或墙壁），返回ID、名称、类别。用于确定建筑材料的选择。";

        public override JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["type"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "材料类型: tile(方块) 或 wall(墙壁)",
                    ["enum"] = new JArray { "tile", "wall" }
                },
                ["style"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "风格: asian, medieval, fantasy, snow, desert, modern, natural"
                },
                ["category"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "类别: basic(基础), wood(木材), brick(砖块), glass(玻璃), slab(板), metal(金属)"
                },
                ["biome"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "生物群落: forest, snow, desert, jungle, ocean, underground, hallow"
                },
                ["top_k"] = new JObject
                {
                    ["type"] = "integer",
                    ["description"] = "返回数量，默认10",
                    ["default"] = 10
                }
            },
            ["required"] = new JArray { "type" }
        };

        public override Task<ToolResult> ExecuteAsync(JObject input, KnowledgeBaseManager kb)
        {
            string type = GetStringParam(input, "type");
            string style = GetStringParam(input, "style");
            string category = GetStringParam(input, "category");
            string biome = GetStringParam(input, "biome");
            int topK = GetIntParam(input, "top_k") ?? 10;

            if (type == "tile")
            {
                return SearchTiles(kb, style, category, biome, topK);
            }
            else if (type == "wall")
            {
                return SearchWalls(kb, style, category, biome, topK);
            }
            else
            {
                return Task.FromResult(ToolResult.Error($"未知的材料类型: {type}，请使用 tile 或 wall"));
            }
        }

        private Task<ToolResult> SearchTiles(KnowledgeBaseManager kb, string style, string category, string biome, int topK)
        {
            var tiles = kb.Tiles.SearchTiles(style, category, biome);

            // 如果有向量库，进行语义排序
            if (kb.Vectors.IsInitialized && !string.IsNullOrEmpty(style))
            {
                tiles = kb.Vectors.SearchTilesSemantic(tiles, style, topK);
            }
            else
            {
                tiles = tiles.Take(topK).ToList();
            }

            var result = new
            {
                type = "tile",
                materials = tiles.Select(t => new
                {
                    id = t.id,
                    name = t.name,
                    display_name = t.display_name,
                    category = t.category,
                    styles = t.styles,
                    biome_match = t.biome_match,
                    paint_compatible = t.paint_compatible,
                    slope_compatible = t.slope_compatible
                }).ToList(),
                total = tiles.Count,
                note = "使用返回的 id 作为 tile_id 或 material_id"
            };

            return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(result)));
        }

        private Task<ToolResult> SearchWalls(KnowledgeBaseManager kb, string style, string category, string biome, int topK)
        {
            var walls = kb.Tiles.SearchWalls(style, category);

            walls = walls.Take(topK).ToList();

            var result = new
            {
                type = "wall",
                materials = walls.Select(w => new
                {
                    id = w.id,
                    name = w.name,
                    display_name = w.display_name,
                    category = w.category,
                    styles = w.styles
                }).ToList(),
                total = walls.Count,
                note = "使用返回的 id 作为 wall_id"
            };

            return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(result)));
        }
    }
}
