using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Terraria.ModLoader;

namespace trab.Core
{
    /// <summary>
    /// 知识库管理器
    /// </summary>
    public class KnowledgeBaseManager
    {
        private static KnowledgeBaseManager _instance;
        public static KnowledgeBaseManager Instance => _instance ??= new KnowledgeBaseManager();

        public TileKnowledgeBase Tiles { get; private set; }
        public StyleTemplateBase Styles { get; private set; }
        public FurnitureRuleBase Furniture { get; private set; }

        private bool _initialized = false;

        public void Initialize()
        {
            if (_initialized) return;

            try
            {
                Tiles = new TileKnowledgeBase();
                Styles = new StyleTemplateBase();
                Furniture = new FurnitureRuleBase();
                _initialized = true;

                trab.Instance?.Logger.Info("知识库初始化完成");
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error("知识库初始化失败: " + ex.Message);
            }
        }

        public bool IsInitialized => _initialized;
    }

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
                new TileInfo { id = 1, name = "Stone", styles = new List<string> { "medieval", "natural" } },
                new TileInfo { id = 4, name = "GrayBrick", styles = new List<string> { "medieval", "castle" } },
                new TileInfo { id = 5, name = "Wood", styles = new List<string> { "natural", "medieval" } },
                new TileInfo { id = 13, name = "Glass", styles = new List<string> { "modern" } },
                new TileInfo { id = 41, name = "GoldBrick", styles = new List<string> { "luxury" } },
                new TileInfo { id = 57, name = "Marble", styles = new List<string> { "greek" } },
                new TileInfo { id = 143, name = "StoneSlab", styles = new List<string> { "medieval" } }
            };

            _paints = new List<PaintInfo>
            {
                new PaintInfo { id = 0, name = "None" },
                new PaintInfo { id = 28, name = "Shadow" },
                new PaintInfo { id = 30, name = "White" }
            };

            _walls = new List<WallInfo>
            {
                new WallInfo { id = 1, name = "StoneWall" },
                new WallInfo { id = 4, name = "WoodWall" },
                new WallInfo { id = 6, name = "GrayBrickWall" }
            };
        }

        public List<TileInfo> SearchTiles(string style, string category = null, string biome = null)
        {
            return _tiles.Where(t => t.styles != null && t.styles.Contains(style)).ToList();
        }

        public TileInfo GetTileByName(string name)
        {
            return _tiles.FirstOrDefault(t => t.name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public PaintInfo GetPaint(int id) => _paints.FirstOrDefault(p => p.id == id);

        public WallInfo GetWallByName(string name)
        {
            return _walls.FirstOrDefault(w => w.name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public PaintSchemeRecommendation GetPaintRecommendation(string style)
        {
            return new PaintSchemeRecommendation { PrimaryPaint = 0, ShadowPaint = 28, Description = "默认方案" };
        }

        public int TileCount => _tiles.Count;
        public int PaintCount => _paints.Count;
        public int WallCount => _walls.Count;
    }

    /// <summary>
    /// 风格模板知识库
    /// </summary>
    public class StyleTemplateBase
    {
        private Dictionary<string, StyleTemplate> _styles;

        public StyleTemplateBase()
        {
            InitDefaultData();
        }

        private void InitDefaultData()
        {
            _styles = new Dictionary<string, StyleTemplate>
            {
                ["medieval"] = new StyleTemplate { name = "中世纪风格", display_name = "Medieval" },
                ["fantasy"] = new StyleTemplate { name = "奇幻风格", display_name = "Fantasy" },
                ["natural"] = new StyleTemplate { name = "自然风格", display_name = "Natural" }
            };
        }

        public StyleTemplate GetTemplate(string styleName, string buildingType = null)
        {
            if (styleName == null) return null;
            _styles.TryGetValue(styleName.ToLower(), out var template);
            return template;
        }

        public List<string> GetAllStyleNames() => _styles.Keys.ToList();

        public int StyleCount => _styles.Count;
    }

    /// <summary>
    /// 家具规则知识库
    /// </summary>
    public class FurnitureRuleBase
    {
        private Dictionary<string, FurnitureInfo> _furniture;

        public FurnitureRuleBase()
        {
            InitDefaultData();
        }

        private void InitDefaultData()
        {
            _furniture = new Dictionary<string, FurnitureInfo>
            {
                ["WorkBench"] = new FurnitureInfo { tile_id = 17, display_name = "工作台", width = 2, height = 1 },
                ["Tables"] = new FurnitureInfo { tile_id = 87, display_name = "桌子", width = 3, height = 1 },
                ["Chairs"] = new FurnitureInfo { tile_id = 88, display_name = "椅子", width = 1, height = 2 },
                ["Beds"] = new FurnitureInfo { tile_id = 89, display_name = "床", width = 4, height = 2 },
                ["Chests"] = new FurnitureInfo { tile_id = 21, display_name = "宝箱", width = 2, height = 1 }
            };
        }

        public List<KeyValuePair<string, FurnitureInfo>> SearchFurniture(string roomType, string npcType = null)
        {
            return _furniture.ToList();
        }

        public int FurnitureCount => _furniture.Count;
    }

    // 数据结构定义
    public class TileInfo
    {
        public int id { get; set; }
        public string name { get; set; }
        public List<string> styles { get; set; }
        public bool paint_compatible { get; set; }
    }

    public class PaintInfo
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class WallInfo
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class StyleTemplate
    {
        public string name { get; set; }
        public string display_name { get; set; }
        public string description { get; set; }
    }

    public class FurnitureInfo
    {
        public int tile_id { get; set; }
        public string display_name { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class PaintSchemeRecommendation
    {
        public int PrimaryPaint { get; set; }
        public int ShadowPaint { get; set; }
        public int HighlightPaint { get; set; }
        public string Description { get; set; }
    }
}