using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace trab.Data
{
    /// <summary>
    /// 建筑规划方案 - 区域划分结果
    /// </summary>
    public class BuildingPlan
    {
        [JsonProperty("building_type")]
        public string BuildingType { get; set; } = "house";

        [JsonProperty("width")]
        public int Width { get; set; } = 10;

        [JsonProperty("height")]
        public int Height { get; set; } = 8;

        [JsonProperty("style")]
        public string Style { get; set; } = "medieval";

        [JsonProperty("regions")]
        public List<Region> Regions { get; set; } = new List<Region>();

        // 辅助属性：获取特定区域
        public Region RoofRegion => Regions.FirstOrDefault(r => r.Name == "roof");
        public List<Region> FloorRegions => Regions.Where(r => r.Name.StartsWith("floor")).ToList();
        public Region WallRegion => Regions.FirstOrDefault(r => r.Name == "walls");
        public List<WindowPosition> WindowPositions => Regions.FirstOrDefault(r => r.Name == "windows")?.Windows ?? new List<WindowPosition>();
        public List<FurniturePosition> FurniturePositions => Regions.FirstOrDefault(r => r.Name == "furniture")?.Furnitures ?? new List<FurniturePosition>();
    }

    /// <summary>
    /// 区域定义
    /// </summary>
    public class Region
    {
        [JsonProperty("name")]
        public string Name { get; set; }  // roof, floor1, walls, windows, furniture

        [JsonProperty("type")]
        public string Type { get; set; }  // gable, flat, dome (for roof)

        [JsonProperty("y_range")]
        public int[] YRange { get; set; }  // [y1, y2]

        [JsonProperty("thickness")]
        public int Thickness { get; set; } = 1;  // for walls

        [JsonProperty("positions")]
        public List<WindowPosition> Windows { get; set; }  // for windows region

        [JsonProperty("furnitures")]
        public List<FurniturePosition> Furnitures { get; set; }  // for furniture region
    }

    /// <summary>
    /// 窗户位置
    /// </summary>
    public class WindowPosition
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; } = 2;

        [JsonProperty("height")]
        public int Height { get; set; } = 2;

        [JsonProperty("type")]
        public string Type { get; set; } = "double";  // single, double, arched
    }

    /// <summary>
    /// 家具位置
    /// </summary>
    public class FurniturePosition
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "workbench";  // workbench, table, chair, bed, chest

        [JsonProperty("floor")]
        public int Floor { get; set; } = 1;  // 所在楼层
    }

    /// <summary>
    /// 模块生成结果
    /// </summary>
    public class ModuleResult
    {
        public string ModuleName { get; set; }
        public List<TileData> Tiles { get; set; } = new List<TileData>();
        public List<WallRangeData> WallRanges { get; set; } = new List<WallRangeData>();
        public List<FurnitureData> Furniture { get; set; } = new List<FurnitureData>();
        public List<DoorData> Doors { get; set; } = new List<DoorData>();
        public List<LightSourceData> LightSources { get; set; } = new List<LightSourceData>();
        public bool IsError { get; set; } = false;
        public string ErrorMessage { get; set; }
    }
}