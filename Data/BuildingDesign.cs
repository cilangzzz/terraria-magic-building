using Newtonsoft.Json;
using System.Collections.Generic;

namespace trab.Data
{
    /// <summary>
    /// AI返回的建筑设计数据结构 - Agent模式增强版
    /// </summary>
    public class BuildingDesign
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "未命名建筑";

        [JsonProperty("description")]
        public string Description { get; set; } = "";

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("style")]
        public string Style { get; set; } = "medieval";

        [JsonProperty("biome_match")]
        public string BiomeMatch { get; set; } = "forest";

        [JsonProperty("difficulty")]
        public string Difficulty { get; set; } = "easy";

        [JsonProperty("tiles")]
        public List<TileData> Tiles { get; set; } = new List<TileData>();

        [JsonProperty("walls")]
        public List<WallData> Walls { get; set; } = new List<WallData>();

        [JsonProperty("wallRanges")]
        public List<WallRangeData> WallRanges { get; set; } = new List<WallRangeData>();

        [JsonProperty("furniture")]
        public List<FurnitureData> Furniture { get; set; } = new List<FurnitureData>();

        [JsonProperty("doors")]
        public List<DoorData> Doors { get; set; } = new List<DoorData>();

        [JsonProperty("lightSources")]
        public List<LightSourceData> LightSources { get; set; } = new List<LightSourceData>();

        [JsonProperty("npc_suitability")]
        public NpcSuitability NpcSuitability { get; set; } = new NpcSuitability();

        [JsonProperty("paint_scheme")]
        public PaintSchemeData PaintScheme { get; set; } = new PaintSchemeData();

        [JsonProperty("tool_calls")]
        public List<ToolCallRecord> ToolCalls { get; set; } = new List<ToolCallRecord>();
    }

    public class TileData
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string TileType { get; set; } = "Stone";

        [JsonProperty("tile_id")]
        public int TileId { get; set; } = 1;

        [JsonProperty("style")]
        public int Style { get; set; } = 0;

        [JsonProperty("paint")]
        public int Paint { get; set; } = 0;

        [JsonProperty("slope")]
        public int Slope { get; set; } = 0;

        [JsonProperty("color")]
        public int Color { get; set; } = 0;

        public int GetPaintId() => Paint > 0 ? Paint : Color;
    }

    public class WallData
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string WallType { get; set; } = "StoneWall";

        [JsonProperty("wall_id")]
        public int WallId { get; set; } = 1;

        [JsonProperty("paint")]
        public int Paint { get; set; } = 0;

        [JsonProperty("color")]
        public int Color { get; set; } = 0;

        public int GetPaintId() => Paint > 0 ? Paint : Color;
    }

    public class WallRangeData
    {
        [JsonProperty("x1")]
        public int X1 { get; set; }

        [JsonProperty("y1")]
        public int Y1 { get; set; }

        [JsonProperty("x2")]
        public int X2 { get; set; }

        [JsonProperty("y2")]
        public int Y2 { get; set; }

        [JsonProperty("type")]
        public string WallType { get; set; } = "WoodWall";

        [JsonProperty("wall_id")]
        public int WallId { get; set; } = 4;

        [JsonProperty("paint")]
        public int Paint { get; set; } = 0;
    }

    public class FurnitureData
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string FurnitureType { get; set; } = "WorkBench";

        [JsonProperty("tile_id")]
        public int TileId { get; set; } = 17;

        [JsonProperty("style")]
        public int Style { get; set; } = 0;

        [JsonProperty("direction")]
        public int Direction { get; set; } = 0;

        [JsonProperty("paint")]
        public int Paint { get; set; } = 0;
    }

    public class DoorData
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string DoorType { get; set; } = "WoodenDoor";

        [JsonProperty("tile_id")]
        public int TileId { get; set; } = 11;

        [JsonProperty("direction")]
        public int Direction { get; set; } = 0;

        [JsonProperty("paint")]
        public int Paint { get; set; } = 0;
    }

    public class LightSourceData
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string LightType { get; set; } = "Torch";

        [JsonProperty("tile_id")]
        public int TileId { get; set; } = 4;

        [JsonProperty("style")]
        public int Style { get; set; } = 0;

        [JsonProperty("wire")]
        public bool Wire { get; set; } = false;
    }

    public class NpcSuitability
    {
        [JsonProperty("is_valid_house")]
        public bool IsValidHouse { get; set; } = false;

        [JsonProperty("suitable_npcs")]
        public List<string> SuitableNpcs { get; set; } = new List<string>();

        [JsonProperty("missing_requirements")]
        public List<string> MissingRequirements { get; set; } = new List<string>();

        [JsonProperty("tile_count")]
        public int TileCount { get; set; } = 0;

        [JsonProperty("has_light")]
        public bool HasLight { get; set; } = false;

        [JsonProperty("has_flat_surface")]
        public bool HasFlatSurface { get; set; } = false;

        [JsonProperty("has_comfort")]
        public bool HasComfort { get; set; } = false;

        [JsonProperty("has_door")]
        public bool HasDoor { get; set; } = false;
    }

    public class PaintSchemeData
    {
        [JsonProperty("primary_paint")]
        public int PrimaryPaint { get; set; } = 0;

        [JsonProperty("shadow_paint")]
        public int ShadowPaint { get; set; } = 28;

        [JsonProperty("highlight_paint")]
        public int HighlightPaint { get; set; } = 0;

        [JsonProperty("accent_paint")]
        public int AccentPaint { get; set; } = 0;

        [JsonProperty("description")]
        public string Description { get; set; } = "";
    }

    public class ToolCallRecord
    {
        [JsonProperty("tool_name")]
        public string ToolName { get; set; }

        [JsonProperty("input")]
        public object Input { get; set; }

        [JsonProperty("output")]
        public object Output { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }
}