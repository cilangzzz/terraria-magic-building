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
        public RoofTemplateBase Roofs { get; private set; }       // 屋顶模板
        public WindowTemplateBase Windows { get; private set; }   // 窗户模板
        public FloorStructureBase Floors { get; private set; }    // 楼层结构

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
                Roofs = new RoofTemplateBase();       // 初始化屋顶模板
                Windows = new WindowTemplateBase();   // 初始化窗户模板
                Floors = new FloorStructureBase();    // 初始化楼层结构

                _initialized = true;

                trab.Instance?.Logger.Info($"知识库初始化完成: Tiles={Tiles.TileCount}, Styles={Styles.StyleCount}, Vectors={Vectors.TileVectorCount}, Roofs={Roofs.RoofCount}, Windows={Windows.WindowCount}, Floors={Floors.FloorCount}");
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error("知识库初始化失败: " + ex.Message);
            }
        }

        public bool IsInitialized => _initialized;

        /// <summary>
        /// 重置知识库（用于模组重新加载）
        /// </summary>
        public void Reset()
        {
            _initialized = false;
            Tiles = null;
            Styles = null;
            Furniture = null;
            Vectors = null;
        }
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
                // "any" 是通配符，匹配所有风格
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
            return _tiles.FirstOrDefault(t => t.name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public TileInfo GetTileById(int id)
        {
            return _tiles.FirstOrDefault(t => t.id == id);
        }

        public PaintInfo GetPaint(int id) => _paints.FirstOrDefault(p => p.id == id);

        public WallInfo GetWallByName(string name)
        {
            return _walls.FirstOrDefault(w => w.name.Equals(name, StringComparison.OrdinalIgnoreCase));
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
                ["WorkBench"] = new FurnitureInfo { tile_id = 17, display_name = "工作台", category = "surface", width = 2, height = 1, npc_function = "crafting" },
                ["Tables"] = new FurnitureInfo { tile_id = 87, display_name = "桌子", category = "surface", width = 3, height = 1, npc_function = "flat_surface" },
                ["Chairs"] = new FurnitureInfo { tile_id = 88, display_name = "椅子", category = "comfort", width = 1, height = 2, npc_function = "comfort" },
                ["Beds"] = new FurnitureInfo { tile_id = 89, display_name = "床", category = "comfort", width = 4, height = 2, npc_function = "comfort" },
                ["Chests"] = new FurnitureInfo { tile_id = 21, display_name = "宝箱", category = "storage", width = 2, height = 1, npc_function = "storage" },
                ["Torches"] = new FurnitureInfo { tile_id = 4, display_name = "火把", category = "light", width = 1, height = 1, npc_function = "light_source" },
                ["ClosedDoor"] = new FurnitureInfo { tile_id = 10, display_name = "门", category = "door", width = 1, height = 3, npc_function = "door" }
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
        public string category { get; set; }
        public List<string> styles { get; set; }
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
        public string category { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string npc_function { get; set; }
    }

    public class PaintSchemeRecommendation
    {
        public int PrimaryPaint { get; set; }
        public int ShadowPaint { get; set; }
        public int HighlightPaint { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// 屋顶模板知识库
    /// </summary>
    public class RoofTemplateBase
    {
        private Dictionary<string, RoofTemplate> _roofs;

        public RoofTemplateBase()
        {
            InitDefaultData();
        }

        private void InitDefaultData()
        {
            _roofs = new Dictionary<string, RoofTemplate>
            {
                ["gable"] = new RoofTemplate {
                    name = "gable",
                    display_name = "人字形屋顶",
                    styles = new List<string> { "medieval", "natural", "fantasy" },
                    min_width = 6,
                    shape_pattern = "三角上升，中心最高，两侧对称下降",
                    edge_tiles = new List<int> { 4, 143 },  // 灰砖、石板
                    fill_tiles = new List<int> { 4, 5 },    // 灰砖、木材
                    description = "经典三角形屋顶，适合小屋和城堡"
                },
                ["flat"] = new RoofTemplate {
                    name = "flat",
                    display_name = "平顶",
                    styles = new List<string> { "modern", "steampunk" },
                    min_width = 4,
                    shape_pattern = "水平一层，边缘可加装饰",
                    edge_tiles = new List<int> { 57, 41 },  // 大理石、金砖
                    fill_tiles = new List<int> { 57, 1 },   // 大理石、石头
                    description = "现代简约平顶，适合现代建筑"
                },
                ["dome"] = new RoofTemplate {
                    name = "dome",
                    display_name = "圆顶",
                    styles = new List<string> { "fantasy", "desert" },
                    min_width = 8,
                    shape_pattern = "弧形上升，中心圆顶",
                    edge_tiles = new List<int> { 57, 41 },  // 大理石、金砖
                    fill_tiles = new List<int> { 57 },      // 大理石
                    description = "圆形穹顶，适合魔法塔和神殿"
                },
                ["pagoda"] = new RoofTemplate {
                    name = "pagoda",
                    display_name = "宝塔顶",
                    styles = new List<string> { "asian" },
                    min_width = 6,
                    shape_pattern = "多层阶梯上升，每层向外延伸",
                    edge_tiles = new List<int> { 5, 387 },  // 木材、王朝木
                    fill_tiles = new List<int> { 5 },       // 木材
                    description = "东方风格多层屋顶，适合寺庙"
                },
                ["stepped"] = new RoofTemplate {
                    name = "stepped",
                    display_name = "阶梯顶",
                    styles = new List<string> { "steampunk", "dark" },
                    min_width = 8,
                    shape_pattern = "阶梯状上升，每层缩进",
                    edge_tiles = new List<int> { 4, 143 },  // 灰砖、石板
                    fill_tiles = new List<int> { 4 },       // 灰砖
                    description = "工业风格阶梯屋顶，适合工厂"
                }
            };
        }

        public RoofTemplate GetTemplate(string roofName)
        {
            if (roofName == null) return null;
            _roofs.TryGetValue(roofName.ToLower(), out var template);
            return template;
        }

        public List<string> GetAllRoofNames() => _roofs.Keys.ToList();

        public int RoofCount => _roofs.Count;
    }

    /// <summary>
    /// 窗户模板知识库
    /// </summary>
    public class WindowTemplateBase
    {
        private Dictionary<string, WindowTemplate> _windows;

        public WindowTemplateBase()
        {
            InitDefaultData();
        }

        private void InitDefaultData()
        {
            _windows = new Dictionary<string, WindowTemplate>
            {
                ["single"] = new WindowTemplate {
                    name = "single",
                    display_name = "单窗",
                    styles = new List<string> { "any" },
                    width = 1,
                    height = 2,
                    frame_tile_id = 4,   // 灰砖窗框
                    glass_tile_id = 13,  // 玻璃
                    description = "单格窗户，通用设计"
                },
                ["double"] = new WindowTemplate {
                    name = "double",
                    display_name = "双窗",
                    styles = new List<string> { "medieval", "natural" },
                    width = 2,
                    height = 2,
                    frame_tile_id = 4,   // 灰砖窗框
                    glass_tile_id = 13,  // 玻璃
                    description = "双格窗户，对称设计"
                },
                ["arched"] = new WindowTemplate {
                    name = "arched",
                    display_name = "拱窗",
                    styles = new List<string> { "medieval", "fantasy", "greek" },
                    width = 2,
                    height = 3,
                    frame_tile_id = 57,  // 大理石窗框
                    glass_tile_id = 13,  // 玻璃
                    description = "拱形窗户，优雅设计"
                },
                ["bay"] = new WindowTemplate {
                    name = "bay",
                    display_name = "凸窗",
                    styles = new List<string> { "modern", "natural" },
                    width = 3,
                    height = 2,
                    frame_tile_id = 5,   // 木材窗框
                    glass_tile_id = 13,  // 玻璃
                    description = "向外凸出的窗户，增加空间感"
                }
            };
        }

        public WindowTemplate GetTemplate(string windowName)
        {
            if (windowName == null) return null;
            _windows.TryGetValue(windowName.ToLower(), out var template);
            return template;
        }

        public List<WindowTemplate> GetTemplatesByStyle(string style)
        {
            return _windows.Values.Where(w =>
                w.styles.Contains("any") || w.styles.Contains(style?.ToLower())
            ).ToList();
        }

        public int WindowCount => _windows.Count;
    }

    /// <summary>
    /// 楼层结构模板知识库
    /// </summary>
    public class FloorStructureBase
    {
        private Dictionary<string, FloorStructure> _floors;

        public FloorStructureBase()
        {
            InitDefaultData();
        }

        private void InitDefaultData()
        {
            _floors = new Dictionary<string, FloorStructure>
            {
                ["single_story"] = new FloorStructure {
                    name = "single_story",
                    display_name = "单层建筑",
                    styles = new List<string> { "any" },
                    floor_count = 1,
                    floor_height = 6,
                    wall_thickness = 1,
                    floor_tiles = new List<int> { 5, 143 },  // 木材、石板
                    wall_tiles = new List<int> { 4, 5 },     // 灰砖、木材
                    description = "单层建筑，适合小屋"
                },
                ["two_story"] = new FloorStructure {
                    name = "two_story",
                    display_name = "双层建筑",
                    styles = new List<string> { "medieval", "natural", "fantasy" },
                    floor_count = 2,
                    floor_height = 6,
                    wall_thickness = 1,
                    floor_tiles = new List<int> { 5, 143 },  // 木材、石板
                    wall_tiles = new List<int> { 4, 1 },     // 灰砖、石头
                    description = "双层建筑，适合住宅"
                },
                ["tower"] = new FloorStructure {
                    name = "tower",
                    display_name = "塔楼",
                    styles = new List<string> { "medieval", "fantasy", "dark" },
                    floor_count = 4,
                    floor_height = 5,
                    wall_thickness = 1,
                    floor_tiles = new List<int> { 4, 143 },  // 灰砖、石板
                    wall_tiles = new List<int> { 4, 57 },    // 灰砖、大理石
                    description = "多层塔楼，适合魔法塔或瞭望塔"
                },
                ["castle"] = new FloorStructure {
                    name = "castle",
                    display_name = "城堡",
                    styles = new List<string> { "medieval", "dark" },
                    floor_count = 3,
                    floor_height = 8,
                    wall_thickness = 2,
                    floor_tiles = new List<int> { 4, 143 },  // 灰砖、石板
                    wall_tiles = new List<int> { 4, 1 },     // 灰砖、石头
                    description = "大型城堡，多层结构"
                }
            };
        }

        public FloorStructure GetTemplate(string structureName)
        {
            if (structureName == null) return null;
            _floors.TryGetValue(structureName.ToLower(), out var template);
            return template;
        }

        public List<FloorStructure> GetTemplatesByStyle(string style)
        {
            return _floors.Values.Where(f =>
                f.styles.Contains("any") || f.styles.Contains(style?.ToLower())
            ).ToList();
        }

        public int FloorCount => _floors.Count;
    }

    // 建筑构件数据结构定义
    public class RoofTemplate
    {
        public string name { get; set; }
        public string display_name { get; set; }
        public List<string> styles { get; set; }
        public int min_width { get; set; }
        public string shape_pattern { get; set; }
        public List<int> edge_tiles { get; set; }
        public List<int> fill_tiles { get; set; }
        public string description { get; set; }
    }

    public class WindowTemplate
    {
        public string name { get; set; }
        public string display_name { get; set; }
        public List<string> styles { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int frame_tile_id { get; set; }
        public int glass_tile_id { get; set; }
        public string description { get; set; }
    }

    public class FloorStructure
    {
        public string name { get; set; }
        public string display_name { get; set; }
        public List<string> styles { get; set; }
        public int floor_count { get; set; }
        public int floor_height { get; set; }
        public int wall_thickness { get; set; }
        public List<int> floor_tiles { get; set; }
        public List<int> wall_tiles { get; set; }
        public string description { get; set; }
    }
}