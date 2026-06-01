using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using trab.Data;

namespace trab.Core
{
    /// <summary>
    /// 建筑模块合并器 - 将多个模块JSON合并为完整BuildingDesign
    /// </summary>
    public class BuildingMerger
    {
        /// <summary>
        /// 合并所有模块生成最终建筑设计
        /// </summary>
        public BuildingDesign Merge(BuildingPlan plan, List<ModuleResult> modules)
        {
            if (plan == null)
            {
                trab.Instance?.Logger.Error("合并失败: plan为null");
                return null;
            }

            if (modules == null || modules.Count == 0)
            {
                trab.Instance?.Logger.Error("合并失败: modules为空");
                return CreateFallbackDesign(plan);
            }

            var design = new BuildingDesign
            {
                Name = GenerateBuildingName(plan),
                Width = plan.Width,
                Height = plan.Height,
                Style = plan.Style,
                Description = GenerateDescription(plan, modules)
            };

            // 合并所有模块的tiles
            foreach (var module in modules)
            {
                if (module.IsError)
                {
                    trab.Instance?.Logger.Warn($"模块{module.ModuleName}有错误: {module.ErrorMessage}");
                    continue;
                }

                // 合并tiles
                if (module.Tiles != null && module.Tiles.Count > 0)
                {
                    design.Tiles.AddRange(module.Tiles);
                }

                // 合并wallRanges
                if (module.WallRanges != null && module.WallRanges.Count > 0)
                {
                    design.WallRanges.AddRange(module.WallRanges);
                }

                // 合并furniture
                if (module.Furniture != null && module.Furniture.Count > 0)
                {
                    design.Furniture.AddRange(module.Furniture);
                }

                // 合并doors
                if (module.Doors != null && module.Doors.Count > 0)
                {
                    design.Doors.AddRange(module.Doors);
                }

                // 合并lightSources
                if (module.LightSources != null && module.LightSources.Count > 0)
                {
                    design.LightSources.AddRange(module.LightSources);
                }
            }

            // 去重tiles（防止重复坐标）
            design.Tiles = DeduplicateTiles(design.Tiles);

            // 验证并填充缺失
            ValidateAndFillMissing(design, plan);

            trab.Instance?.Logger.Info($"合并完成: {design.Tiles.Count} tiles, {design.WallRanges.Count} walls, {design.Furniture.Count} furniture");

            return design;
        }

        /// <summary>
        /// 生成建筑名称
        /// </summary>
        private string GenerateBuildingName(BuildingPlan plan)
        {
            string styleName = GetStyleDisplayName(plan.Style);
            string typeName = GetBuildingTypeDisplayName(plan.BuildingType);
            return $"{styleName}{typeName}";
        }

        /// <summary>
        /// 生成描述
        /// </summary>
        private string GenerateDescription(BuildingPlan plan, List<ModuleResult> modules)
        {
            int successModules = modules.Count(m => !m.IsError);
            int totalTiles = modules.Sum(m => m.Tiles?.Count ?? 0);
            return $"分模块生成建筑，{successModules}/{modules.Count}模块成功，共{totalTiles}个方块";
        }

        /// <summary>
        /// 去重tiles
        /// </summary>
        private List<TileData> DeduplicateTiles(List<TileData> tiles)
        {
            if (tiles == null) return new List<TileData>();

            var uniqueTiles = new Dictionary<string, TileData>();
            foreach (var tile in tiles)
            {
                string key = $"{tile.X},{tile.Y}";
                if (!uniqueTiles.ContainsKey(key))
                {
                    uniqueTiles[key] = tile;
                }
                else
                {
                    // 如果已存在，保留有slope或paint的版本
                    var existing = uniqueTiles[key];
                    if (tile.Slope > 0 || tile.Paint > 0)
                    {
                        uniqueTiles[key] = tile;
                    }
                }
            }

            return uniqueTiles.Values.ToList();
        }

        /// <summary>
        /// 验证并填充缺失元素
        /// </summary>
        private void ValidateAndFillMissing(BuildingDesign design, BuildingPlan plan)
        {
            // 确保有门
            if (design.Doors.Count == 0)
            {
                // 添加默认门（底部中心）
                int doorX = plan.Width / 2;
                int doorY = plan.Height - 2;
                design.Doors.Add(new DoorData
                {
                    X = doorX,
                    Y = doorY,
                    TileId = 10  // 木门
                });
                trab.Instance?.Logger.Info($"自动添加门: ({doorX}, {doorY})");
            }

            // 确保有光源
            if (design.LightSources.Count == 0)
            {
                // 添加默认火把（两侧）
                design.LightSources.Add(new LightSourceData { X = 2, Y = plan.Height / 2, TileId = 4 });
                design.LightSources.Add(new LightSourceData { X = plan.Width - 3, Y = plan.Height / 2, TileId = 4 });
                trab.Instance?.Logger.Info($"自动添加火把");
            }

            // 确保有基础家具（NPC房屋要求）
            if (design.Furniture.Count == 0)
            {
                int floorY = plan.Height - 2;
                design.Furniture.Add(new FurnitureData { X = 3, Y = floorY, TileId = 17 });  // 工作台
                design.Furniture.Add(new FurnitureData { X = 5, Y = floorY, TileId = 87 });  // 桌子
                design.Furniture.Add(new FurnitureData { X = 6, Y = floorY, TileId = 88 });  // 椅子
                trab.Instance?.Logger.Info($"自动添加基础家具");
            }

            // 设置NPC房屋适用性
            design.NpcSuitability = new NpcSuitability
            {
                IsValidHouse = design.Doors.Count > 0 && design.LightSources.Count > 0 && design.Furniture.Count > 0,
                HasLight = design.LightSources.Count > 0,
                HasFlatSurface = design.Furniture.Any(f => f.TileId == 17 || f.TileId == 87),
                HasComfort = design.Furniture.Any(f => f.TileId == 88),
                HasDoor = design.Doors.Count > 0,
                TileCount = design.Tiles.Count
            };
        }

        /// <summary>
        /// 创建备用设计（当模块生成全部失败时）
        /// </summary>
        private BuildingDesign CreateFallbackDesign(BuildingPlan plan)
        {
            trab.Instance?.Logger.Warn("使用备用设计方案");

            var design = new BuildingDesign
            {
                Name = GenerateBuildingName(plan),
                Width = plan.Width,
                Height = plan.Height,
                Style = plan.Style
            };

            // 简单火柴盒结构
            // 外墙
            for (int x = 0; x < plan.Width; x++)
            {
                design.Tiles.Add(new TileData { X = x, Y = 0, TileId = 4 });  // 顶
                design.Tiles.Add(new TileData { X = x, Y = plan.Height - 1, TileId = 5 });  // 底
            }
            for (int y = 0; y < plan.Height; y++)
            {
                design.Tiles.Add(new TileData { X = 0, Y = y, TileId = 4 });  // 左墙
                design.Tiles.Add(new TileData { X = plan.Width - 1, Y = y, TileId = 4 });  // 右墙
            }

            // 墙背景
            design.WallRanges.Add(new WallRangeData
            {
                X1 = 1, Y1 = 1, X2 = plan.Width - 2, Y2 = plan.Height - 2, WallId = 4
            });

            // 门
            design.Doors.Add(new DoorData { X = plan.Width / 2, Y = plan.Height - 2, TileId = 10 });

            // 家具
            design.Furniture.Add(new FurnitureData { X = 3, Y = plan.Height - 2, TileId = 17 });
            design.Furniture.Add(new FurnitureData { X = 5, Y = plan.Height - 2, TileId = 87 });
            design.Furniture.Add(new FurnitureData { X = 6, Y = plan.Height - 2, TileId = 88 });

            // 光源
            design.LightSources.Add(new LightSourceData { X = 2, Y = plan.Height / 2, TileId = 4 });
            design.LightSources.Add(new LightSourceData { X = plan.Width - 3, Y = plan.Height / 2, TileId = 4 });

            return design;
        }

        /// <summary>
        /// 获取风格显示名称
        /// </summary>
        private string GetStyleDisplayName(string style)
        {
            var styleNames = new Dictionary<string, string>
            {
                ["medieval"] = "中世纪",
                ["fantasy"] = "奇幻",
                ["natural"] = "自然",
                ["steampunk"] = "蒸汽朋克",
                ["asian"] = "东方",
                ["snow"] = "冰雪",
                ["desert"] = "沙漠",
                ["modern"] = "现代",
                ["dark"] = "黑暗"
            };
            return styleNames.TryGetValue(style?.ToLower(), out var name) ? name : "";
        }

        /// <summary>
        /// 获取建筑类型显示名称
        /// </summary>
        private string GetBuildingTypeDisplayName(string type)
        {
            var typeNames = new Dictionary<string, string>
            {
                ["house"] = "小屋",
                ["two_story"] = "双层小屋",
                ["tower"] = "塔楼",
                ["castle"] = "城堡",
                ["shop"] = "商店",
                ["temple"] = "神殿"
            };
            return typeNames.TryGetValue(type?.ToLower(), out var name) ? name : "建筑";
        }
    }
}