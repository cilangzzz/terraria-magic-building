using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Terraria.ModLoader;
using trab.Core.KnowledgeBase;
using trab.Data;

namespace trab.Core.Building
{
    /// <summary>
    /// 程序化建筑生成器
    /// 根据BuildingRules设计规则生成完整的TEditSchDesign
    /// </summary>
    public class ProceduralBuilder
    {
        private readonly KnowledgeBaseManager _kb;

        public ProceduralBuilder(KnowledgeBaseManager kb)
        {
            _kb = kb;
        }

        /// <summary>
        /// 从设计规则生成建筑
        /// </summary>
        public TEditSchDesign GenerateFromRules(BuildingRules rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            // 使用原始尺寸，不做限制
            int width = rules.width;
            int height = rules.height;

            // 创建设计对象
            var design = new TEditSchDesign
            {
                name = rules.name ?? "Generated Building",
                width = width,
                height = height
            };

            // 初始化网格
            InitializeGrid(design);

            // 解析材料调色板
            var palette = ResolveMaterialPalette(rules.materials, rules.style);

            // 生成结构
            if (rules.structure != null)
            {
                // 框架
                if (rules.structure.frame != null)
                {
                    GenerateFrame(design, rules.structure.frame, palette);
                }

                // 墙壁
                if (rules.structure.walls != null)
                {
                    GenerateWalls(design, rules.structure.walls, palette);
                }

                // 楼层
                if (rules.structure.floors != null)
                {
                    foreach (var floor in rules.structure.floors)
                    {
                        GenerateFloor(design, floor, palette);
                    }
                }

                // 屋顶
                if (rules.structure.roof != null)
                {
                    GenerateRoof(design, rules.structure.roof, palette);
                }

                // 房间
                if (rules.structure.rooms != null)
                {
                    foreach (var room in rules.structure.rooms)
                    {
                        GenerateRoom(design, room, palette);
                    }
                }
            }

            // 放置装饰
            if (rules.decorations != null)
            {
                foreach (var decor in rules.decorations)
                {
                    PlaceDecoration(design, decor, palette);
                }
            }

            // 计算统计
            design.CalculateStats();

            return design;
        }

        /// <summary>
        /// 从模板生成建筑（带修改）
        /// </summary>
        public TEditSchDesign GenerateFromTemplate(BuildingEntity template, TemplateModifications mods = null)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            // 从模板提取规则
            var rules = ExtractRulesFromTemplate(template);

            // 应用修改
            if (mods != null)
            {
                ApplyModifications(rules, mods);
            }

            // 生成建筑
            return GenerateFromRules(rules);
        }

        #region 初始化

        private void InitializeGrid(TEditSchDesign design)
        {
            design.tiles = new List<List<TEditTile>>();

            for (int y = 0; y < design.height; y++)
            {
                var row = new List<TEditTile>();
                for (int x = 0; x < design.width; x++)
                {
                    row.Add(TEditTile.Empty());
                }
                design.tiles.Add(row);
            }
        }

        #endregion

        #region 材料解析

        private MaterialPalette ResolveMaterialPalette(MaterialPalette palette, string style)
        {
            // 如果已有调色板，解析材料ID
            if (palette != null)
            {
                palette.primary_tile = ResolveMaterialRef(palette.primary_tile, "tile");
                palette.secondary_tile = ResolveMaterialRef(palette.secondary_tile, "tile");
                palette.primary_wall = ResolveMaterialRef(palette.primary_wall, "wall");
                palette.secondary_wall = ResolveMaterialRef(palette.secondary_wall, "wall");
                palette.accent_tile = ResolveMaterialRef(palette.accent_tile, "tile");
                palette.floor_tile = ResolveMaterialRef(palette.floor_tile, "tile");
                palette.roof_tile = ResolveMaterialRef(palette.roof_tile, "tile");
                return palette;
            }

            // 返回默认调色板
            return GetDefaultPalette(style);
        }

        private MaterialRef ResolveMaterialRef(MaterialRef refItem, string type)
        {
            if (refItem == null) return null;

            // 如果没有ID但有名称，使用默认材料映射
            if (!refItem.id.HasValue && !string.IsNullOrEmpty(refItem.name))
            {
                // 材料名称到ID的默认映射
                var tileDefaults = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["gray brick"] = 4, ["stone"] = 1, ["wood"] = 5,
                    ["gold brick"] = 41, ["marble"] = 57, ["glass"] = 13,
                    ["stone slab"] = 143, ["gold"] = 179
                };

                var wallDefaults = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["gray brick wall"] = 6, ["stone wall"] = 1, ["wood wall"] = 4,
                    ["glass wall"] = 14, ["marble wall"] = 172, ["gold brick wall"] = 15
                };

                if (type == "tile")
                {
                    if (tileDefaults.TryGetValue(refItem.name, out var tileId))
                    {
                        refItem.id = tileId;
                    }
                    else
                    {
                        refItem.id = 4; // 默认 Gray Brick
                    }
                }
                else if (type == "wall")
                {
                    if (wallDefaults.TryGetValue(refItem.name, out var wallId))
                    {
                        refItem.id = wallId;
                    }
                    else
                    {
                        refItem.id = 6; // 默认 Gray Brick Wall
                    }
                }
            }

            return refItem;
        }

        private MaterialPalette GetDefaultPalette(string style)
        {
            var styleLower = style?.ToLower() ?? "";

            // 默认材料组合
            var defaults = new Dictionary<string, MaterialPalette>
            {
                ["asian"] = new MaterialPalette
                {
                    primary_tile = new MaterialRef { name = "Gold Brick", id = 7 },
                    secondary_tile = new MaterialRef { name = "Stone Slab", id = 142 },
                    primary_wall = new MaterialRef { name = "Marble Wall", id = 14 },
                    floor_tile = new MaterialRef { name = "Wood", id = 30 }
                },
                ["medieval"] = new MaterialPalette
                {
                    primary_tile = new MaterialRef { name = "Gray Brick", id = 6 },
                    secondary_tile = new MaterialRef { name = "Stone Slab", id = 142 },
                    primary_wall = new MaterialRef { name = "Gray Brick Wall", id = 5 },
                    floor_tile = new MaterialRef { name = "Wood", id = 30 }
                },
                ["default"] = new MaterialPalette
                {
                    primary_tile = new MaterialRef { name = "Gray Brick", id = 6 },
                    primary_wall = new MaterialRef { name = "Gray Brick Wall", id = 5 },
                    floor_tile = new MaterialRef { name = "Wood", id = 30 }
                }
            };

            return defaults.TryGetValue(styleLower, out var palette) ? palette : defaults["default"];
        }

        #endregion

        #region 结构生成

        private void GenerateFrame(TEditSchDesign design, FrameRule frame, MaterialPalette palette)
        {
            int tileId = ResolveTileId(frame.material, frame.material_id, palette.primary_tile);
            int thickness = Math.Max(1, frame.thickness);

            switch (frame.pattern?.ToLower())
            {
                case "rectangle":
                    GenerateRectangleFrame(design, tileId, thickness);
                    break;
                case "arch":
                    GenerateArchFrame(design, tileId, thickness);
                    break;
                case "pagoda":
                    GeneratePagodaFrame(design, tileId, thickness);
                    break;
                default:
                    GenerateRectangleFrame(design, tileId, thickness);
                    break;
            }
        }

        private void GenerateRectangleFrame(TEditSchDesign design, int tileId, int thickness)
        {
            // 底部框架
            for (int x = 0; x < design.width; x++)
            {
                for (int t = 0; t < thickness; t++)
                {
                    SetTile(design, x, design.height - 1 - t, tileId);
                }
            }

            // 左右两侧框架
            for (int y = 0; y < design.height; y++)
            {
                for (int t = 0; t < thickness; t++)
                {
                    SetTile(design, t, y, tileId);
                    SetTile(design, design.width - 1 - t, y, tileId);
                }
            }

            // 顶部框架
            for (int x = 0; x < design.width; x++)
            {
                for (int t = 0; t < thickness; t++)
                {
                    SetTile(design, x, t, tileId);
                }
            }
        }

        private void GenerateArchFrame(TEditSchDesign design, int tileId, int thickness)
        {
            // 先生成矩形框架作为基础
            GenerateRectangleFrame(design, tileId, thickness);

            // 在顶部创建拱形
            int centerY = thickness;
            int archHeight = Math.Min(design.height / 4, 5);
            int startX = design.width / 4;
            int endX = design.width - design.width / 4;

            for (int x = startX; x <= endX; x++)
            {
                double angle = Math.PI * (x - startX) / (endX - startX);
                int y = (int)(centerY + archHeight * Math.Sin(angle));
                for (int t = 0; t < thickness; t++)
                {
                    ClearTile(design, x, t);
                }
                SetTile(design, x, y, tileId);
            }
        }

        private void GeneratePagodaFrame(TEditSchDesign design, int tileId, int thickness)
        {
            // 多层宝塔框架
            int layers = Math.Max(2, design.height / 8);

            for (int layer = 0; layer < layers; layer++)
            {
                int layerStartY = layer * (design.height / layers);
                int layerEndY = (layer + 1) * (design.height / layers) - 1;

                // 每层的宽度递减
                int indent = layer * 2;
                int layerWidth = design.width - indent * 2;

                if (layerWidth < 4) break;

                // 底部
                for (int x = indent; x < indent + layerWidth; x++)
                {
                    for (int t = 0; t < thickness; t++)
                    {
                        if (layerEndY - t >= 0)
                            SetTile(design, x, layerEndY - t, tileId);
                    }
                }

                // 两侧
                for (int y = layerStartY; y <= layerEndY; y++)
                {
                    for (int t = 0; t < thickness; t++)
                    {
                        SetTile(design, indent + t, y, tileId);
                        SetTile(design, indent + layerWidth - 1 - t, y, tileId);
                    }
                }

                // 屋檐（向外扩展）
                if (layer < layers - 1)
                {
                    int roofY = layerEndY;
                    for (int x = indent - 1; x <= indent + layerWidth; x++)
                    {
                        if (x >= 0 && x < design.width)
                            SetTile(design, x, roofY, tileId);
                    }
                }
            }
        }

        private void GenerateWalls(TEditSchDesign design, WallRule walls, MaterialPalette palette)
        {
            int wallId = ResolveWallId(walls.primary_material, walls.primary_wall_id, palette.primary_wall);
            int secondaryWallId = ResolveWallId(walls.secondary_material, walls.secondary_wall_id, palette.secondary_wall);

            switch (walls.fill_pattern?.ToLower())
            {
                case "solid":
                    FillSolidWalls(design, wallId, walls.paint);
                    break;
                case "checkered":
                    FillCheckeredWalls(design, wallId, secondaryWallId, walls.paint);
                    break;
                case "striped":
                    FillStripedWalls(design, wallId, secondaryWallId, walls.paint);
                    break;
                case "gradient":
                    FillGradientWalls(design, wallId, secondaryWallId, walls.paint);
                    break;
                default:
                    FillSolidWalls(design, wallId, walls.paint);
                    break;
            }
        }

        private void FillSolidWalls(TEditSchDesign design, int wallId, int? paint)
        {
            for (int y = 1; y < design.height - 1; y++)
            {
                for (int x = 1; x < design.width - 1; x++)
                {
                    if (design.tiles[y][x].wall == null || design.tiles[y][x].wall == 0)
                    {
                        design.tiles[y][x].wall = wallId;
                        if (paint.HasValue)
                        {
                            design.tiles[y][x].wall_color = paint;
                        }
                    }
                }
            }
        }

        private void FillCheckeredWalls(TEditSchDesign design, int wallId1, int wallId2, int? paint)
        {
            for (int y = 1; y < design.height - 1; y++)
            {
                for (int x = 1; x < design.width - 1; x++)
                {
                    int wallId = ((x + y) % 2 == 0) ? wallId1 : (wallId2 > 0 ? wallId2 : wallId1);
                    if (design.tiles[y][x].wall == null || design.tiles[y][x].wall == 0)
                    {
                        design.tiles[y][x].wall = wallId;
                        if (paint.HasValue)
                        {
                            design.tiles[y][x].wall_color = paint;
                        }
                    }
                }
            }
        }

        private void FillStripedWalls(TEditSchDesign design, int wallId1, int wallId2, int? paint)
        {
            for (int y = 1; y < design.height - 1; y++)
            {
                for (int x = 1; x < design.width - 1; x++)
                {
                    int wallId = (y % 2 == 0) ? wallId1 : (wallId2 > 0 ? wallId2 : wallId1);
                    if (design.tiles[y][x].wall == null || design.tiles[y][x].wall == 0)
                    {
                        design.tiles[y][x].wall = wallId;
                        if (paint.HasValue)
                        {
                            design.tiles[y][x].wall_color = paint;
                        }
                    }
                }
            }
        }

        private void FillGradientWalls(TEditSchDesign design, int wallId1, int wallId2, int? paint)
        {
            // 从上到下渐变
            for (int y = 1; y < design.height - 1; y++)
            {
                for (int x = 1; x < design.width - 1; x++)
                {
                    // 根据Y位置决定使用哪种墙
                    float ratio = (float)y / design.height;
                    int wallId = ratio < 0.5f ? wallId1 : (wallId2 > 0 ? wallId2 : wallId1);
                    if (design.tiles[y][x].wall == null || design.tiles[y][x].wall == 0)
                    {
                        design.tiles[y][x].wall = wallId;
                        if (paint.HasValue)
                        {
                            design.tiles[y][x].wall_color = paint;
                        }
                    }
                }
            }
        }

        private void GenerateFloor(TEditSchDesign design, FloorRule floor, MaterialPalette palette)
        {
            int tileId = ResolveTileId(floor.material, floor.material_id, palette.floor_tile);
            int yStart = Math.Clamp(floor.y_start, 0, design.height - 1);
            int yEnd = Math.Clamp(floor.y_end, yStart, design.height - 1);
            int thickness = Math.Max(1, floor.thickness);

            switch (floor.pattern?.ToLower())
            {
                case "solid":
                    GenerateSolidFloor(design, yStart, yEnd, tileId, thickness);
                    break;
                case "checkered":
                    GenerateCheckeredFloor(design, yStart, yEnd, tileId, thickness);
                    break;
                case "bordered":
                    GenerateBorderedFloor(design, yStart, yEnd, tileId, thickness);
                    break;
                default:
                    GenerateSolidFloor(design, yStart, yEnd, tileId, thickness);
                    break;
            }
        }

        private void GenerateSolidFloor(TEditSchDesign design, int yStart, int yEnd, int tileId, int thickness)
        {
            for (int y = yStart; y <= yEnd && y < design.height; y++)
            {
                for (int x = 1; x < design.width - 1; x++)
                {
                    SetTile(design, x, y, tileId);
                }
            }
        }

        private void GenerateCheckeredFloor(TEditSchDesign design, int yStart, int yEnd, int tileId, int thickness)
        {
            int secondaryId = tileId; // 默认使用相同
            for (int y = yStart; y <= yEnd && y < design.height; y++)
            {
                for (int x = 1; x < design.width - 1; x++)
                {
                    int id = ((x + y) % 2 == 0) ? tileId : secondaryId;
                    SetTile(design, x, y, id);
                }
            }
        }

        private void GenerateBorderedFloor(TEditSchDesign design, int yStart, int yEnd, int tileId, int thickness)
        {
            for (int y = yStart; y <= yEnd && y < design.height; y++)
            {
                // 边框
                SetTile(design, 1, y, tileId);
                SetTile(design, design.width - 2, y, tileId);

                // 中间区域保持空或有墙
                // 这里不设置tile，只保留wall
            }
        }

        private void GenerateRoof(TEditSchDesign design, RoofRule roof, MaterialPalette palette)
        {
            int tileId = ResolveTileId(roof.material, roof.material_id, palette.roof_tile ?? palette.primary_tile);
            int overhang = Math.Max(0, roof.overhang);

            switch (roof.type?.ToLower())
            {
                case "gable":
                    GenerateGableRoof(design, tileId, overhang);
                    break;
                case "flat":
                    GenerateFlatRoof(design, tileId, overhang);
                    break;
                case "dome":
                    GenerateDomeRoof(design, tileId);
                    break;
                case "pagoda":
                    GeneratePagodaRoof(design, tileId, overhang);
                    break;
                default:
                    GenerateFlatRoof(design, tileId, overhang);
                    break;
            }
        }

        private void GenerateGableRoof(TEditSchDesign design, int tileId, int overhang)
        {
            // 人字形屋顶
            int centerX = design.width / 2;
            int peakY = 0;
            int baseWidth = design.width + overhang * 2;
            int roofHeight = Math.Min(baseWidth / 4, design.height / 4);

            // 从中心向两侧扩展
            for (int level = 0; level < roofHeight; level++)
            {
                int halfWidth = (baseWidth / 2) - (level * 2);
                int y = peakY + level;

                for (int x = centerX - halfWidth; x <= centerX + halfWidth; x++)
                {
                    if (x >= -overhang && x < design.width + overhang)
                    {
                        int actualX = Math.Clamp(x, 0, design.width - 1);
                        if (y < design.height)
                        {
                            SetTile(design, actualX, y, tileId);
                        }
                    }
                }
            }
        }

        private void GenerateFlatRoof(TEditSchDesign design, int tileId, int overhang)
        {
            // 平顶
            int startY = 0;
            int rows = Math.Max(1, overhang > 0 ? 2 : 1);

            for (int y = startY; y < rows && y < design.height; y++)
            {
                for (int x = 0; x < design.width; x++)
                {
                    SetTile(design, x, y, tileId);
                }
            }
        }

        private void GenerateDomeRoof(TEditSchDesign design, int tileId)
        {
            // 圆顶
            int centerX = design.width / 2;
            int radius = Math.Min(design.width / 2, design.height / 3);

            for (int y = 0; y < radius; y++)
            {
                double angle = Math.PI * y / radius;
                int halfWidth = (int)(radius * Math.Cos(angle));

                for (int x = centerX - halfWidth; x <= centerX + halfWidth; x++)
                {
                    if (x >= 0 && x < design.width)
                    {
                        SetTile(design, x, y, tileId);
                    }
                }
            }
        }

        private void GeneratePagodaRoof(TEditSchDesign design, int tileId, int overhang)
        {
            // 多层宝塔屋顶
            int layers = Math.Max(2, design.height / 6);

            for (int layer = 0; layer < layers; layer++)
            {
                int layerY = layer * 3;
                int indent = layer * 2;
                int layerWidth = design.width - indent * 2 + overhang * 2;

                if (layerWidth < 4 || layerY >= design.height) break;

                // 屋檐
                for (int x = indent - overhang; x < indent + layerWidth + overhang; x++)
                {
                    if (x >= 0 && x < design.width)
                    {
                        SetTile(design, x, layerY, tileId);
                    }
                }
            }
        }

        private void GenerateRoom(TEditSchDesign design, RoomRule room, MaterialPalette palette)
        {
            // 验证房间位置
            int x = Math.Clamp(room.x, 1, design.width - 2);
            int y = Math.Clamp(room.y, 1, design.height - 2);
            int width = Math.Clamp(room.width, 3, design.width - x - 1);
            int height = Math.Clamp(room.height, 3, design.height - y - 1);

            // 房间边界（可选：使用不同的材料）
            // 这里主要确保房间区域有墙

            for (int ry = y; ry < y + height; ry++)
            {
                for (int rx = x; rx < x + width; rx++)
                {
                    // 确保有墙
                    if (design.tiles[ry][rx].wall == null || design.tiles[ry][rx].wall == 0)
                    {
                        int wallId = palette.primary_wall?.id ?? 5;
                        design.tiles[ry][rx].wall = wallId;
                    }
                }
            }

            // 如果需要门
            if (room.has_door)
            {
                // 在房间底部留一个门的位置
                int doorY = y + height - 1;
                int doorX = x + width / 2;
                if (doorX >= 1 && doorX < design.width - 1 && doorY < design.height - 1)
                {
                    ClearTile(design, doorX, doorY);
                }
            }
        }

        #endregion

        #region 装饰放置

        private void PlaceDecoration(TEditSchDesign design, DecorationRule decor, MaterialPalette palette)
        {
            int tileId = ResolveTileId(decor.material, decor.tile_id);

            var positions = CalculateDecorationPositions(design, decor);

            foreach (var pos in positions)
            {
                if (pos.x >= 0 && pos.x < design.width && pos.y >= 0 && pos.y < design.height)
                {
                    SetTile(design, pos.x, pos.y, tileId);
                }
            }
        }

        private List<(int x, int y)> CalculateDecorationPositions(TEditSchDesign design, DecorationRule decor)
        {
            var positions = new List<(int x, int y)>();

            switch (decor.placement?.ToLower())
            {
                case "corners":
                    // 四个角落（内部）
                    positions.Add((2, 2));
                    positions.Add((design.width - 3, 2));
                    positions.Add((2, design.height - 3));
                    positions.Add((design.width - 3, design.height - 3));
                    break;

                case "center":
                    // 中心位置
                    positions.Add((design.width / 2, design.height / 2));
                    break;

                case "edges":
                    // 边缘位置
                    int spacing = decor.spacing ?? 6;
                    // 顶部边缘
                    for (int x = spacing; x < design.width - 1; x += spacing)
                    {
                        positions.Add((x, 2));
                    }
                    break;

                case "every_n_tiles":
                    spacing = decor.spacing ?? 8;
                    for (int x = spacing; x < design.width - 1; x += spacing)
                    {
                        positions.Add((x, 2));
                    }
                    break;

                case "custom":
                    if (decor.positions != null)
                    {
                        foreach (var p in decor.positions)
                        {
                            positions.Add((p.x, p.y));
                        }
                    }
                    break;

                default:
                    // 默认放在中心
                    positions.Add((design.width / 2, design.height / 2));
                    break;
            }

            return positions;
        }

        #endregion

        #region 辅助方法

        private void SetTile(TEditSchDesign design, int x, int y, int tileId)
        {
            if (x < 0 || x >= design.width || y < 0 || y >= design.height)
                return;

            design.tiles[y][x].active = true;
            design.tiles[y][x].type = tileId;
        }

        private void ClearTile(TEditSchDesign design, int x, int y)
        {
            if (x < 0 || x >= design.width || y < 0 || y >= design.height)
                return;

            design.tiles[y][x].active = false;
            design.tiles[y][x].type = null;
        }

        private int ResolveTileId(string name, int? id, MaterialRef fallback = null)
        {
            if (id.HasValue && id.Value > 0) return id.Value;

            if (!string.IsNullOrEmpty(name))
            {
                var tileDefaults = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["gray brick"] = 4, ["stone"] = 1, ["wood"] = 5,
                    ["gold brick"] = 41, ["marble"] = 57, ["glass"] = 13,
                    ["stone slab"] = 143, ["gold"] = 179
                };
                if (tileDefaults.TryGetValue(name, out var tileId))
                    return tileId;
            }

            return fallback?.id ?? 4; // 默认 Gray Brick
        }

        private int ResolveWallId(string name, int? id, MaterialRef fallback = null)
        {
            if (id.HasValue && id.Value > 0) return id.Value;

            if (!string.IsNullOrEmpty(name))
            {
                var wallDefaults = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["gray brick wall"] = 6, ["stone wall"] = 1, ["wood wall"] = 4,
                    ["glass wall"] = 14, ["marble wall"] = 172, ["gold brick wall"] = 15
                };
                if (wallDefaults.TryGetValue(name, out var wallId))
                    return wallId;
            }

            return fallback?.id ?? 6; // 默认 Gray Brick Wall
        }

        #endregion

        #region 模板处理

        private BuildingRules ExtractRulesFromTemplate(BuildingEntity template)
        {
            var rules = new BuildingRules
            {
                name = template.id,
                width = template.dimensions?.width ?? 20,
                height = template.dimensions?.height ?? 15,
                style = template.features?.style ?? "unknown",
                template_id = template.id,
                structure = new StructureRules(),
                decorations = new List<DecorationRule>()
            };

            // 从材料列表提取调色板
            rules.materials = new MaterialPalette();

            if (template.materials?.primary_tiles != null && template.materials.primary_tiles.Count > 0)
            {
                var mainTile = template.materials.primary_tiles[0];
                rules.materials.primary_tile = new MaterialRef
                {
                    name = mainTile.name,
                    id = mainTile.id
                };
            }

            if (template.materials?.primary_walls != null && template.materials.primary_walls.Count > 0)
            {
                var mainWall = template.materials.primary_walls[0];
                rules.materials.primary_wall = new MaterialRef
                {
                    name = mainWall.name,
                    id = mainWall.id
                };
            }

            // 从建造顺序提取结构
            if (template.building_sequence != null)
            {
                foreach (var step in template.building_sequence)
                {
                    switch (step.action?.ToLower())
                    {
                        case "frame":
                            rules.structure.frame = new FrameRule
                            {
                                material = step.materials?.FirstOrDefault() ?? "Stone",
                                pattern = "rectangle"
                            };
                            break;
                        case "walls":
                            rules.structure.walls = new WallRule
                            {
                                primary_material = step.materials?.FirstOrDefault(),
                                fill_pattern = "solid"
                            };
                            break;
                        case "lights":
                            if (step.materials != null)
                            {
                                foreach (var mat in step.materials)
                                {
                                    rules.decorations.Add(new DecorationRule
                                    {
                                        type = "lantern",
                                        material = mat,
                                        placement = "every_n_tiles",
                                        spacing = 8
                                    });
                                }
                            }
                            break;
                    }
                }
            }

            return rules;
        }

        private void ApplyModifications(BuildingRules rules, TemplateModifications mods)
        {
            if (mods == null) return;

            // 应用缩放
            if (mods.scale.HasValue && mods.scale.Value > 0)
            {
                rules.width = (int)(rules.width * mods.scale.Value);
                rules.height = (int)(rules.height * mods.scale.Value);
            }

            // 应用材料替换
            if (mods.material_replacements != null)
            {
                foreach (var replacement in mods.material_replacements)
                {
                    // 替换调色板中的材料
                    if (rules.materials?.primary_tile?.name == replacement.original)
                    {
                        rules.materials.primary_tile.name = replacement.replacement;
                        rules.materials.primary_tile.id = replacement.replacement_id;
                    }
                    if (rules.materials?.primary_wall?.name == replacement.original)
                    {
                        rules.materials.primary_wall.name = replacement.replacement;
                        rules.materials.primary_wall.id = replacement.replacement_id;
                    }
                }
            }
        }

        #endregion
    }
}