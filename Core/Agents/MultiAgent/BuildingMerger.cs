using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using trab.Data;

namespace trab.Core.Agents.MultiAgent
{
    /// <summary>
    /// 建筑模块合并器 - 将多个模块合并为TEditSch格式
    /// </summary>
    public class BuildingMerger
    {
        /// <summary>
        /// 合并模块为TEditSch格式
        /// </summary>
        public TEditSchDesign MergeToTEditSch(BuildingPlan plan, List<ModuleResult> modules)
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

            var design = new TEditSchDesign
            {
                name = GenerateBuildingName(plan),
                width = plan.Width,
                height = plan.Height
            };

            // 初始化空网格
            for (int y = 0; y < plan.Height; y++)
            {
                var row = new List<TEditTile>();
                for (int x = 0; x < plan.Width; x++)
                    row.Add(new TEditTile());
                design.tiles.Add(row);
            }

            // 合并各模块
            foreach (var module in modules)
            {
                if (module.IsError)
                {
                    trab.Instance?.Logger.Warn($"模块{module.ModuleName}有错误: {module.ErrorMessage}");
                    continue;
                }

                MergeModuleToGrid(design, module);
            }

            // 验证并填充缺失元素
            ValidateAndFillMissing(design, plan);

            // 计算统计信息
            design.CalculateStats();

            trab.Instance?.Logger.Info($"合并完成: {design.stats.active_tiles} tiles, {design.stats.tiles_with_wall} walls");

            return design;
        }

        private void MergeModuleToGrid(TEditSchDesign design, ModuleResult module)
        {
            // 合并方块
            if (module.Tiles != null)
            {
                foreach (var tile in module.Tiles)
                {
                    if (tile.X >= 0 && tile.X < design.width && tile.Y >= 0 && tile.Y < design.height)
                    {
                        design.tiles[tile.Y][tile.X] = new TEditTile
                        {
                            active = true,
                            type = tile.TileId,
                            tile_color = tile.Paint > 0 ? tile.Paint : null
                        };
                    }
                }
            }

            // 合并墙壁范围
            if (module.WallRanges != null)
            {
                foreach (var range in module.WallRanges)
                {
                    for (int y = range.Y1; y <= range.Y2; y++)
                    {
                        for (int x = range.X1; x <= range.X2; x++)
                        {
                            if (y >= 0 && y < design.height && x >= 0 && x < design.width)
                            {
                                design.tiles[y][x].wall = range.WallId;
                                design.tiles[y][x].wall_color = range.Paint > 0 ? range.Paint : null;
                            }
                        }
                    }
                }
            }

            // 合并家具
            if (module.Furniture != null)
            {
                foreach (var f in module.Furniture)
                {
                    if (f.X >= 0 && f.X < design.width && f.Y >= 0 && f.Y < design.height)
                    {
                        design.tiles[f.Y][f.X] = new TEditTile
                        {
                            active = true,
                            type = f.TileId,
                            wall = design.tiles[f.Y][f.X].wall
                        };
                    }
                }
            }

            // 合并门
            if (module.Doors != null)
            {
                foreach (var d in module.Doors)
                {
                    if (d.X >= 0 && d.X < design.width && d.Y >= 0 && d.Y < design.height)
                    {
                        design.tiles[d.Y][d.X] = new TEditTile
                        {
                            active = true,
                            type = d.TileId,
                            wall = design.tiles[d.Y][d.X].wall
                        };
                    }
                }
            }

            // 合并光源
            if (module.LightSources != null)
            {
                foreach (var l in module.LightSources)
                {
                    if (l.X >= 0 && l.X < design.width && l.Y >= 0 && l.Y < design.height)
                    {
                        design.tiles[l.Y][l.X] = new TEditTile
                        {
                            active = true,
                            type = l.TileId,
                            wall = design.tiles[l.Y][l.X].wall
                        };
                    }
                }
            }
        }

        private string GenerateBuildingName(BuildingPlan plan)
        {
            string styleName = GetStyleDisplayName(plan.Style);
            string typeName = GetBuildingTypeDisplayName(plan.BuildingType);
            return $"{styleName}{typeName}";
        }

        private void ValidateAndFillMissing(TEditSchDesign design, BuildingPlan plan)
        {
            // 检查是否有门
            bool hasDoor = false;
            bool hasLight = false;
            bool hasSurface = false;
            bool hasComfort = false;

            foreach (var row in design.tiles)
            {
                foreach (var tile in row)
                {
                    if (tile.type.HasValue)
                    {
                        int tid = tile.type.Value;
                        if (tid == 10 || tid == 11) hasDoor = true;
                        if (tid == 4 || tid == 33 || tid == 34) hasLight = true;
                        if (tid == 17 || tid == 87) hasSurface = true;
                        if (tid == 88 || tid == 89) hasComfort = true;
                    }
                }
            }

            int floorY = plan.Height - 2;

            // 添加缺失的门
            if (!hasDoor)
            {
                int doorX = plan.Width / 2;
                if (doorX >= 0 && doorX < design.width && floorY >= 0 && floorY < design.height)
                {
                    design.tiles[floorY][doorX] = new TEditTile { active = true, type = 10 };
                    trab.Instance?.Logger.Info($"自动添加门: ({doorX}, {floorY})");
                }
            }

            // 添加缺失的光源
            if (!hasLight)
            {
                int torchX1 = 2;
                int torchX2 = plan.Width - 3;
                int torchY = plan.Height / 2;

                if (torchX1 >= 0 && torchX1 < design.width && torchY >= 0 && torchY < design.height)
                    design.tiles[torchY][torchX1] = new TEditTile { active = true, type = 4 };
                if (torchX2 >= 0 && torchX2 < design.width && torchY >= 0 && torchY < design.height)
                    design.tiles[torchY][torchX2] = new TEditTile { active = true, type = 4 };

                trab.Instance?.Logger.Info($"自动添加火把");
            }

            // 添加缺失的家具
            if (!hasSurface)
            {
                int workbenchX = 3;
                if (workbenchX >= 0 && workbenchX < design.width && floorY >= 0 && floorY < design.height)
                    design.tiles[floorY][workbenchX] = new TEditTile { active = true, type = 17 };
                trab.Instance?.Logger.Info($"自动添加工作台");
            }

            if (!hasComfort)
            {
                int chairX = 5;
                if (chairX >= 0 && chairX < design.width && floorY >= 0 && floorY < design.height)
                    design.tiles[floorY][chairX] = new TEditTile { active = true, type = 88 };
                trab.Instance?.Logger.Info($"自动添加椅子");
            }
        }

        private TEditSchDesign CreateFallbackDesign(BuildingPlan plan)
        {
            trab.Instance?.Logger.Warn("使用备用设计方案");

            var design = new TEditSchDesign
            {
                name = GenerateBuildingName(plan),
                width = plan.Width,
                height = plan.Height
            };

            // 初始化网格
            for (int y = 0; y < plan.Height; y++)
            {
                var row = new List<TEditTile>();
                for (int x = 0; x < plan.Width; x++)
                    row.Add(new TEditTile());
                design.tiles.Add(row);
            }

            // 放置边界方块
            for (int x = 0; x < plan.Width; x++)
            {
                design.tiles[0][x] = new TEditTile { active = true, type = 4 }; // 屋顶边缘
                design.tiles[plan.Height - 1][x] = new TEditTile { active = true, type = 5 }; // 地板
            }
            for (int y = 0; y < plan.Height; y++)
            {
                design.tiles[y][0] = new TEditTile { active = true, type = 4 }; // 左墙
                design.tiles[y][plan.Width - 1] = new TEditTile { active = true, type = 4 }; // 右墙
            }

            // 填充墙壁
            for (int y = 1; y < plan.Height - 1; y++)
            {
                for (int x = 1; x < plan.Width - 1; x++)
                {
                    design.tiles[y][x].wall = 4; // 木墙
                }
            }

            // 添加门
            int doorX = plan.Width / 2;
            int doorY = plan.Height - 2;
            design.tiles[doorY][doorX] = new TEditTile { active = true, type = 10 };

            // 添加家具
            design.tiles[plan.Height - 2][3] = new TEditTile { active = true, type = 17 };
            design.tiles[plan.Height - 2][5] = new TEditTile { active = true, type = 87 };
            design.tiles[plan.Height - 2][6] = new TEditTile { active = true, type = 88 };

            // 添加光源
            design.tiles[plan.Height / 2][2] = new TEditTile { active = true, type = 4 };
            design.tiles[plan.Height / 2][plan.Width - 3] = new TEditTile { active = true, type = 4 };

            design.CalculateStats();
            return design;
        }

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