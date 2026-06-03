using System.Collections.Generic;
using System.Linq;

namespace trab.Core.KnowledgeBase
{
    /// <summary>
    /// 方块知识库
    /// </summary>
    public class TileKnowledgeBase
    {
        private List<TileInfo> _tiles;
        private List<PaintInfo> _paints;
        private List<WallInfo> _walls;

        public TileKnowledgeBase()
        {
            InitDefaultData();
        }

        private void InitDefaultData()
        {
            _tiles = new List<TileInfo>
            {
                new TileInfo { id = 1, name = "Stone", display_name = "石头", category = "basic", styles = new List<string> { "medieval", "natural" }, biome_match = new List<string> { "forest", "underground" }, paint_compatible = true, slope_compatible = true },
                new TileInfo { id = 4, name = "GrayBrick", display_name = "灰砖", category = "brick", styles = new List<string> { "medieval", "castle" }, biome_match = new List<string> { "forest" }, paint_compatible = true, slope_compatible = false },
                new TileInfo { id = 5, name = "Wood", display_name = "木材", category = "wood", styles = new List<string> { "natural", "medieval" }, biome_match = new List<string> { "forest" }, paint_compatible = true, slope_compatible = true },
                new TileInfo { id = 13, name = "Glass", display_name = "玻璃", category = "transparent", styles = new List<string> { "modern" }, biome_match = new List<string> { "any" }, paint_compatible = true },
                new TileInfo { id = 41, name = "GoldBrick", display_name = "金砖", category = "luxury", styles = new List<string> { "luxury" }, biome_match = new List<string> { "any" }, paint_compatible = true },
                new TileInfo { id = 57, name = "Marble", display_name = "大理石", category = "luxury", styles = new List<string> { "greek" }, biome_match = new List<string> { "underground" }, paint_compatible = true, slope_compatible = true },
                new TileInfo { id = 143, name = "StoneSlab", display_name = "石板", category = "slab", styles = new List<string> { "medieval" }, biome_match = new List<string> { "forest", "underground" }, paint_compatible = true },
                new TileInfo { id = 17, name = "WorkBench", display_name = "工作台", category = "furniture", styles = new List<string> { "any" }, biome_match = new List<string> { "any" }, paint_compatible = true },
                new TileInfo { id = 87, name = "Tables", display_name = "桌子", category = "furniture", styles = new List<string> { "any" }, biome_match = new List<string> { "any" }, paint_compatible = true },
                new TileInfo { id = 88, name = "Chairs", display_name = "椅子", category = "furniture", styles = new List<string> { "any" }, biome_match = new List<string> { "any" }, paint_compatible = true },
                new TileInfo { id = 4, name = "Torches", display_name = "火把", category = "light", styles = new List<string> { "any" }, biome_match = new List<string> { "any" } },
                new TileInfo { id = 11, name = "ClosedDoor", display_name = "门", category = "door", styles = new List<string> { "any" }, biome_match = new List<string> { "any" }, paint_compatible = true }
            };

            _paints = new List<PaintInfo>
            {
                new PaintInfo { id = 0, name = "None", display_name = "无" },
                new PaintInfo { id = 1, name = "Red", display_name = "红色" },
                new PaintInfo { id = 2, name = "Orange", display_name = "橙色" },
                new PaintInfo { id = 3, name = "Yellow", display_name = "黄色" },
                new PaintInfo { id = 28, name = "Shadow", display_name = "阴影" },
                new PaintInfo { id = 29, name = "Negative", display_name = "反转" },
                new PaintInfo { id = 30, name = "White", display_name = "白色" },
                new PaintInfo { id = 31, name = "Black", display_name = "黑色" }
            };

            _walls = new List<WallInfo>
            {
                new WallInfo { id = 1, name = "StoneWall", display_name = "石墙", category = "natural", styles = new List<string> { "medieval", "natural" } },
                new WallInfo { id = 4, name = "WoodWall", display_name = "木墙", category = "wood", styles = new List<string> { "natural", "medieval" } },
                new WallInfo { id = 6, name = "GrayBrickWall", display_name = "灰砖墙", category = "brick", styles = new List<string> { "medieval", "castle" } },
                new WallInfo { id = 14, name = "GlassWall", display_name = "玻璃墙", category = "transparent", styles = new List<string> { "modern", "fantasy" } },
                new WallInfo { id = 15, name = "GoldBrickWall", display_name = "金砖墙", category = "luxury", styles = new List<string> { "luxury" } },
                new WallInfo { id = 16, name = "SandstoneWall", display_name = "砂岩墙", category = "desert", styles = new List<string> { "desert" } },
                new WallInfo { id = 17, name = "SnowWall", display_name = "雪墙", category = "snow", styles = new List<string> { "snow" } }
            };
        }

        public List<TileInfo> SearchTiles(string style, string category = null, string biome = null)
        {
            return _tiles.Where(t =>
                t.styles != null &&
                (style == null || t.styles.Contains("any") || t.styles.Contains(style)) &&
                (category == null || t.category == category) &&
                (biome == null || t.biome_match == null || t.biome_match.Contains("any") || t.biome_match.Contains(biome))
            ).ToList();
        }

        public List<PaintInfo> GetAllPaints() => _paints;
        public List<TileInfo> GetAllTiles() => _tiles;
        public List<WallInfo> GetAllWalls() => _walls;

        public List<WallInfo> SearchWalls(string style, string category = null)
        {
            return _walls.Where(w =>
                w.styles != null &&
                (style == null || w.styles.Contains("any") || w.styles.Contains(style)) &&
                (category == null || w.category == category)
            ).ToList();
        }

        public TileInfo GetTileByName(string name)
        {
            return _tiles.FirstOrDefault(t => t.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        }

        public TileInfo GetTileById(int id)
        {
            return _tiles.FirstOrDefault(t => t.id == id);
        }

        public PaintInfo GetPaint(int id) => _paints.FirstOrDefault(p => p.id == id);

        public WallInfo GetWallByName(string name)
        {
            return _walls.FirstOrDefault(w => w.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        }

        public WallInfo GetWallById(int id)
        {
            return _walls.FirstOrDefault(w => w.id == id);
        }

        public PaintSchemeRecommendation GetPaintRecommendation(string style)
        {
            return new PaintSchemeRecommendation { PrimaryPaint = 0, ShadowPaint = 28, Description = "默认方案" };
        }

        public int TileCount => _tiles.Count;
        public int PaintCount => _paints.Count;
        public int WallCount => _walls.Count;
    }

    #region 数据结构

    public class TileInfo
    {
        public int id { get; set; }
        public string name { get; set; }
        public string display_name { get; set; }
        public string category { get; set; }
        public List<string> styles { get; set; }
        public List<string> biome_match { get; set; }
        public bool paint_compatible { get; set; }
        public bool slope_compatible { get; set; }
        public string description { get; set; }
    }

    public class PaintInfo
    {
        public int id { get; set; }
        public string name { get; set; }
        public string display_name { get; set; }
    }

    public class WallInfo
    {
        public int id { get; set; }
        public string name { get; set; }
        public string display_name { get; set; }
        public string category { get; set; }
        public List<string> styles { get; set; }
    }

    public class PaintSchemeRecommendation
    {
        public int PrimaryPaint { get; set; }
        public int ShadowPaint { get; set; }
        public int HighlightPaint { get; set; }
        public string Description { get; set; }
    }

    #endregion
}
