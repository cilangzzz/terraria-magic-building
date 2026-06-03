using System.Collections.Generic;
using System.Linq;

namespace trab.Core.KnowledgeBase
{
    /// <summary>
    /// 建筑构件模板库 - 屋顶、窗户、楼层结构
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
                    edge_tiles = new List<int> { 4, 143 },
                    fill_tiles = new List<int> { 4, 5 },
                    description = "经典三角形屋顶，适合小屋和城堡"
                },
                ["flat"] = new RoofTemplate {
                    name = "flat",
                    display_name = "平顶",
                    styles = new List<string> { "modern", "steampunk" },
                    min_width = 4,
                    shape_pattern = "水平一层，边缘可加装饰",
                    edge_tiles = new List<int> { 57, 41 },
                    fill_tiles = new List<int> { 57, 1 },
                    description = "现代简约平顶，适合现代建筑"
                },
                ["dome"] = new RoofTemplate {
                    name = "dome",
                    display_name = "圆顶",
                    styles = new List<string> { "fantasy", "desert" },
                    min_width = 8,
                    shape_pattern = "弧形上升，中心圆顶",
                    edge_tiles = new List<int> { 57, 41 },
                    fill_tiles = new List<int> { 57 },
                    description = "圆形穹顶，适合魔法塔和神殿"
                },
                ["pagoda"] = new RoofTemplate {
                    name = "pagoda",
                    display_name = "宝塔顶",
                    styles = new List<string> { "asian" },
                    min_width = 6,
                    shape_pattern = "多层阶梯上升，每层向外延伸",
                    edge_tiles = new List<int> { 5, 387 },
                    fill_tiles = new List<int> { 5 },
                    description = "东方风格多层屋顶，适合寺庙"
                },
                ["stepped"] = new RoofTemplate {
                    name = "stepped",
                    display_name = "阶梯顶",
                    styles = new List<string> { "steampunk", "dark" },
                    min_width = 8,
                    shape_pattern = "阶梯状上升，每层缩进",
                    edge_tiles = new List<int> { 4, 143 },
                    fill_tiles = new List<int> { 4 },
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
                    width = 1, height = 2,
                    frame_tile_id = 4, glass_tile_id = 13,
                    description = "单格窗户，通用设计"
                },
                ["double"] = new WindowTemplate {
                    name = "double",
                    display_name = "双窗",
                    styles = new List<string> { "medieval", "natural" },
                    width = 2, height = 2,
                    frame_tile_id = 4, glass_tile_id = 13,
                    description = "双格窗户，对称设计"
                },
                ["arched"] = new WindowTemplate {
                    name = "arched",
                    display_name = "拱窗",
                    styles = new List<string> { "medieval", "fantasy", "greek" },
                    width = 2, height = 3,
                    frame_tile_id = 57, glass_tile_id = 13,
                    description = "拱形窗户，优雅设计"
                },
                ["bay"] = new WindowTemplate {
                    name = "bay",
                    display_name = "凸窗",
                    styles = new List<string> { "modern", "natural" },
                    width = 3, height = 2,
                    frame_tile_id = 5, glass_tile_id = 13,
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
            return _windows.Values.Where(w => w.styles.Contains("any") || w.styles.Contains(style?.ToLower())).ToList();
        }

        public int WindowCount => _windows.Count;
    }

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
                    floor_count = 1, floor_height = 6, wall_thickness = 1,
                    floor_tiles = new List<int> { 5, 143 },
                    wall_tiles = new List<int> { 4, 5 },
                    description = "单层建筑，适合小屋"
                },
                ["two_story"] = new FloorStructure {
                    name = "two_story",
                    display_name = "双层建筑",
                    styles = new List<string> { "medieval", "natural", "fantasy" },
                    floor_count = 2, floor_height = 6, wall_thickness = 1,
                    floor_tiles = new List<int> { 5, 143 },
                    wall_tiles = new List<int> { 4, 1 },
                    description = "双层建筑，适合住宅"
                },
                ["tower"] = new FloorStructure {
                    name = "tower",
                    display_name = "塔楼",
                    styles = new List<string> { "medieval", "fantasy", "dark" },
                    floor_count = 4, floor_height = 5, wall_thickness = 1,
                    floor_tiles = new List<int> { 4, 143 },
                    wall_tiles = new List<int> { 4, 57 },
                    description = "多层塔楼，适合魔法塔或瞭望塔"
                },
                ["castle"] = new FloorStructure {
                    name = "castle",
                    display_name = "城堡",
                    styles = new List<string> { "medieval", "dark" },
                    floor_count = 3, floor_height = 8, wall_thickness = 2,
                    floor_tiles = new List<int> { 4, 143 },
                    wall_tiles = new List<int> { 4, 1 },
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
            return _floors.Values.Where(f => f.styles.Contains("any") || f.styles.Contains(style?.ToLower())).ToList();
        }

        public int FloorCount => _floors.Count;
    }

    #region 数据结构

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

    #endregion
}