using Newtonsoft.Json;
using System.Collections.Generic;

namespace trab.Data
{
    /// <summary>
    /// AI返回的建筑设计数据结构
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
    }

    /// <summary>
    /// Tile数据
    /// </summary>
    public class TileData
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string TileType { get; set; } = "Stone";

        [JsonProperty("style")]
        public int Style { get; set; } = 0;

        [JsonProperty("color")]
        public int Color { get; set; } = 0;
    }

    /// <summary>
    /// 墙壁数据
    /// </summary>
    public class WallData
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string WallType { get; set; } = "StoneWall";

        [JsonProperty("color")]
        public int Color { get; set; } = 0;
    }

    /// <summary>
    /// 墙壁范围数据 - 用于批量填充墙壁，减少token消耗
    /// </summary>
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
    }

    /// <summary>
    /// 家具数据
    /// </summary>
    public class FurnitureData
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string FurnitureType { get; set; } = "WorkBench";

        [JsonProperty("style")]
        public int Style { get; set; } = 0;
    }

    /// <summary>
    /// 门数据
    /// </summary>
    public class DoorData
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string DoorType { get; set; } = "WoodenDoor";

        [JsonProperty("direction")]
        public int Direction { get; set; } = 0;
    }

    /// <summary>
    /// 光源数据
    /// </summary>
    public class LightSourceData
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string LightType { get; set; } = "Torch";

        [JsonProperty("style")]
        public int Style { get; set; } = 0;
    }

    /// <summary>
    /// AI API响应结构
    /// </summary>
    public class AIResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("choices")]
        public Choice[] Choices { get; set; }
    }

    public class Choice
    {
        [JsonProperty("message")]
        public Message Message { get; set; }

        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class Message
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    /// <summary>
    /// 流式响应数据结构
    /// </summary>
    public class StreamChunk
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("choices")]
        public StreamChoice[] Choices { get; set; }
    }

    public class StreamChoice
    {
        [JsonProperty("delta")]
        public StreamDelta Delta { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class StreamDelta
    {
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}