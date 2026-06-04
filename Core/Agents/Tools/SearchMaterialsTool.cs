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
    /// 从建筑实体库中检索材料信息
    /// </summary>
    public class SearchMaterialsTool : BaseAgentTool
    {
        public override string Name => "search_materials";

        public override string Description => "从建筑实体库中检索材料信息。返回材料ID、名称、用途。用于确定建筑材料的选择。";

        public override JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["style"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "风格: asian, medieval, fantasy, snow, desert, modern, natural"
                },
                ["building_id"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "指定建筑ID获取其材料列表"
                },
                ["top_k"] = new JObject
                {
                    ["type"] = "integer",
                    ["description"] = "返回数量，默认10",
                    ["default"] = 10
                }
            }
        };

        public override Task<ToolResult> ExecuteAsync(JObject input, KnowledgeBaseManager kb)
        {
            string style = GetStringParam(input, "style");
            string buildingId = GetStringParam(input, "building_id");
            int topK = GetIntParam(input, "top_k") ?? 10;

            // 如果指定了建筑ID，返回该建筑的材料列表
            if (!string.IsNullOrEmpty(buildingId))
            {
                return GetBuildingMaterials(kb, buildingId);
            }

            // 否则按风格搜索建筑，返回材料汇总
            return SearchMaterialsByStyle(kb, style, topK);
        }

        private Task<ToolResult> GetBuildingMaterials(KnowledgeBaseManager kb, string buildingId)
        {
            var entity = kb.Buildings.GetBuilding(buildingId);
            if (entity == null)
            {
                return Task.FromResult(ToolResult.Error($"未找到建筑: {buildingId}"));
            }

            var detail = kb.Buildings.GetBuildingDetail(buildingId);

            var result = new
            {
                building_id = buildingId,
                name = entity.summary,
                dimensions = entity.dimensions,
                materials = new
                {
                    tiles = entity.materials?.primary_tiles?.Select(t => new
                    {
                        id = t.id,
                        name = t.name,
                        count = t.count
                    }),
                    walls = entity.materials?.primary_walls?.Select(w => new
                    {
                        id = w.id,
                        name = w.name,
                        count = w.count
                    })
                },
                tile_distribution = detail?.tile_distribution?.OrderByDescending(t => t.Value).Take(15),
                wall_distribution = detail?.wall_distribution?.OrderByDescending(w => w.Value).Take(10),
                note = "使用 tile_distribution 中的材料ID作为 tile_id，wall_distribution 中的ID作为 wall_id"
            };

            return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(result)));
        }

        private Task<ToolResult> SearchMaterialsByStyle(KnowledgeBaseManager kb, string style, int topK)
        {
            // 按风格搜索建筑
            var buildings = kb.Buildings.SearchByStyle(style, topK);

            if (buildings.Count == 0)
            {
                // 返回默认材料推荐
                return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(GetDefaultMaterials(style))));
            }

            // 汇总所有建筑的材料
            var allTiles = new Dictionary<int, (string name, int count)>();
            var allWalls = new Dictionary<int, (string name, int count)>();

            foreach (var building in buildings)
            {
                if (building.materials?.primary_tiles != null)
                {
                    foreach (var tile in building.materials.primary_tiles)
                    {
                        if (allTiles.ContainsKey(tile.id))
                        {
                            var existing = allTiles[tile.id];
                            allTiles[tile.id] = (existing.name, existing.count + tile.count);
                        }
                        else
                        {
                            allTiles[tile.id] = (tile.name, tile.count);
                        }
                    }
                }

                if (building.materials?.primary_walls != null)
                {
                    foreach (var wall in building.materials.primary_walls)
                    {
                        if (allWalls.ContainsKey(wall.id))
                        {
                            var existing = allWalls[wall.id];
                            allWalls[wall.id] = (existing.name, existing.count + wall.count);
                        }
                        else
                        {
                            allWalls[wall.id] = (wall.name, wall.count);
                        }
                    }
                }
            }

            var result = new
            {
                style = style,
                matched_buildings = buildings.Count,
                tiles = allTiles.OrderByDescending(t => t.Value.count).Select(t => new
                {
                    id = t.Key,
                    name = t.Value.name,
                    count = t.Value.count
                }),
                walls = allWalls.OrderByDescending(w => w.Value.count).Select(w => new
                {
                    id = w.Key,
                    name = w.Value.name,
                    count = w.Value.count
                }),
                note = "以上材料从匹配风格的建筑中汇总得出"
            };

            return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(result)));
        }

        private object GetDefaultMaterials(string style)
        {
            // 默认材料推荐
            var defaults = new Dictionary<string, object>
            {
                ["medieval"] = new
                {
                    tiles = new[] { new { id = 4, name = "Gray Brick", count = 100 } },
                    walls = new[] { new { id = 6, name = "Gray Brick Wall", count = 80 } }
                },
                ["asian"] = new
                {
                    tiles = new[] { new { id = 179, name = "Gold", count = 100 } },
                    walls = new[] { new { id = 172, name = "Marble Wall", count = 80 } }
                },
                ["fantasy"] = new
                {
                    tiles = new[] { new { id = 57, name = "Marble", count = 100 } },
                    walls = new[] { new { id = 14, name = "Glass Wall", count = 80 } }
                }
            };

            if (defaults.TryGetValue(style ?? "medieval", out var materials))
            {
                return new { style = style ?? "medieval", materials, note = "默认材料推荐" };
            }

            return new { style = style ?? "medieval", materials = defaults["medieval"], note = "默认材料推荐" };
        }
    }
}
