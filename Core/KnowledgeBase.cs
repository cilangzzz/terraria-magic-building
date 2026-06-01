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
        public VectorKnowledgeBase Vectors { get; private set; }  // 新增向量检索

        private bool _initialized = false;

        public void Initialize()
        {
            if (_initialized) return;

            try
            {
                Tiles = new TileKnowledgeBase();
                Styles = new StyleTemplateBase();
                Furniture = new FurnitureRuleBase();
                Vectors = new VectorKnowledgeBase();  // 初始化向量库
                Vectors.Initialize();  // 加载向量数据

                _initialized = true;

                trab.Instance?.Logger.Info($"知识库初始化完成: Tiles={Tiles.TileCount}, Styles={Styles.StyleCount}, Vectors={Vectors.TileVectorCount}");
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
                new WallInfo { id = 1, name = "StoneWall", display_name = "石墙" },
                new WallInfo { id = 4, name = "WoodWall", display_name = "木墙" },
                new WallInfo { id = 6, name = "GrayBrickWall", display_name = "灰砖墙" }
            };
        }

        public List<TileInfo> SearchTiles(string style, string category = null, string biome = null)
        {
            return _tiles.Where(t =>
                t.styles != null &&
                // "any" 是通配符，匹配所有风格
                (style == null || t.styles.Contains("any") || t.styles.Contains(style)) &&
                (category == null || t.category == category) &&
                (biome == null || t.biome_match == null || t.biome_match.Contains("any") || t.biome_match.Contains(biome))
            ).ToList();
        }

        public List<PaintInfo> GetAllPaints() => _paints;

        public List<TileInfo> GetAllTiles() => _tiles;

        public List<WallInfo> GetAllWalls() => _walls;

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
                ["medieval"] = new StyleTemplate {
                    name = "中世纪风格",
                    display_name = "Medieval",
                    description = "经典欧洲中世纪建筑风格，适合城堡、村庄和堡垒。使用灰砖作为主要材料。"
                },
                ["fantasy"] = new StyleTemplate {
                    name = "奇幻风格",
                    display_name = "Fantasy",
                    description = "魔法与幻想风格，适合精灵建筑、魔法塔。使用珍珠石和玻璃。"
                },
                ["natural"] = new StyleTemplate {
                    name = "自然风格",
                    display_name = "Natural",
                    description = "与自然融合的建筑风格，适合树屋、田园小屋。使用木材和泥土。"
                },
                ["steampunk"] = new StyleTemplate {
                    name = "蒸汽朋克风格",
                    display_name = "Steampunk",
                    description = "工业革命风格，适合工厂、机械建筑。使用铜砖和铁砖。"
                },
                ["asian"] = new StyleTemplate {
                    name = "东方风格",
                    display_name = "Asian",
                    description = "中日式建筑风格，适合茶室、寺庙。使用王朝木。"
                },
                ["snow"] = new StyleTemplate {
                    name = "冰雪风格",
                    display_name = "Snow",
                    description = "冬季风格，适合雪屋、冰堡。使用雪块和冰块。"
                },
                ["desert"] = new StyleTemplate {
                    name = "沙漠风格",
                    display_name = "Desert",
                    description = "沙漠和古埃及风格，适合金字塔。使用砂岩。"
                },
                ["modern"] = new StyleTemplate {
                    name = "现代风格",
                    display_name = "Modern",
                    description = "现代简约风格，适合现代住宅。使用花岗岩和大理石。"
                },
                ["dark"] = new StyleTemplate {
                    name = "黑暗风格",
                    display_name = "Dark",
                    description = "腐化/猩红/地狱风格，适合邪恶建筑。使用黑檀石和黑曜石。"
                }
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