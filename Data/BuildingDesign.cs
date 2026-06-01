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

        // Agent模式新增字段
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

        // Agent模式新增：NPC房屋验证
        [JsonProperty("npc_suitability")]
        public NpcSuitability NpcSuitability { get; set; } = new NpcSuitability();

        // Agent模式新增：油漆方案
        [JsonProperty("paint_scheme")]
        public PaintSchemeData PaintScheme { get; set; } = new PaintSchemeData();

        // Agent模式新增：工具调用记录（用于调试）
        [JsonProperty("tool_calls")]
        public List<ToolCallRecord> ToolCalls { get; set; } = new List<ToolCallRecord>();
    }

    /// <summary>
    /// Tile数据 - Agent模式增强版
    /// </summary>
    public class TileData
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string TileType { get; set; } = "Stone";

        // Agent模式新增：精确的Tile ID
        [JsonProperty("tile_id")]
        public int TileId { get; set; } = 1;

        [JsonProperty("style")]
        public int Style { get; set; } = 0;

        // Agent模式新增：油漆颜色ID (0-31)
        [JsonProperty("paint")]
        public int Paint { get; set; } = 0;

        // Agent模式新增：斜坡类型 (0-5)
        [JsonProperty("slope")]
        public int Slope { get; set; } = 0;

        // 兼容旧字段
        [JsonProperty("color")]
        public int Color { get; set; } = 0;

        /// <summary>
        /// 获取有效的油漆ID（优先使用paint，其次color）
        /// </summary>
        public int GetPaintId() => Paint > 0 ? Paint : Color;
    }

    /// <summary>
    /// 墙壁数据 - Agent模式增强版
    /// </summary>
    public class WallData
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string WallType { get; set; } = "StoneWall";

        // Agent模式新增：精确的Wall ID
        [JsonProperty("wall_id")]
        public int WallId { get; set; } = 1;

        // Agent模式新增：油漆颜色ID
        [JsonProperty("paint")]
        public int Paint { get; set; } = 0;

        // 兼容旧字段
        [JsonProperty("color")]
        public int Color { get; set; } = 0;

        /// <summary>
        /// 获取有效的油漆ID
        /// </summary>
        public int GetPaintId() => Paint > 0 ? Paint : Color;
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

        // Agent模式新增
        [JsonProperty("wall_id")]
        public int WallId { get; set; } = 4;

        [JsonProperty("paint")]
        public int Paint { get; set; } = 0;
    }

    /// <summary>
    /// 家具数据 - Agent模式增强版
    /// </summary>
    public class FurnitureData
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string FurnitureType { get; set; } = "WorkBench";

        // Agent模式新增：精确的Tile ID
        [JsonProperty("tile_id")]
        public int TileId { get; set; } = 17;

        [JsonProperty("style")]
        public int Style { get; set; } = 0;

        // Agent模式新增：家具方向（用于椅子等）
        [JsonProperty("direction")]
        public int Direction { get; set; } = 0;

        // Agent模式新增：油漆
        [JsonProperty("paint")]
        public int Paint { get; set; } = 0;
    }

    /// <summary>
    /// 门数据 - Agent模式增强版
    /// </summary>
    public class DoorData
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string DoorType { get; set; } = "WoodenDoor";

        // Agent模式新增：精确的Tile ID
        [JsonProperty("tile_id")]
        public int TileId { get; set; } = 11;

        [JsonProperty("direction")]
        public int Direction { get; set; } = 0;

        // Agent模式新增：油漆
        [JsonProperty("paint")]
        public int Paint { get; set; } = 0;
    }

    /// <summary>
    /// 光源数据 - Agent模式增强版
    /// </summary>
    public class LightSourceData
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("type")]
        public string LightType { get; set; } = "Torch";

        // Agent模式新增：精确的Tile ID
        [JsonProperty("tile_id")]
        public int TileId { get; set; } = 4;

        [JsonProperty("style")]
        public int Style { get; set; } = 0;

        // Agent模式新增：是否连接电路
        [JsonProperty("wire")]
        public bool Wire { get; set; } = false;
    }

    /// <summary>
    /// NPC房屋适用性 - Agent模式新增
    /// </summary>
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

    /// <summary>
    /// 油漆方案数据 - Agent模式新增
    /// </summary>
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

    /// <summary>
    /// 工具调用记录 - Agent模式新增（用于调试和展示）
    /// </summary>
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

    /// <summary>
    /// Claude API响应结构 - 支持工具调用
    /// </summary>
    public class ClaudeResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public ClaudeContent[] Content { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("stop_reason")]
        public string StopReason { get; set; }

        [JsonProperty("stop_sequence")]
        public string StopSequence { get; set; }

        [JsonProperty("usage")]
        public ClaudeUsage Usage { get; set; }
    }

    public class ClaudeContent
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        // 工具调用相关
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("input")]
        public object Input { get; set; }
    }

    public class ClaudeUsage
    {
        [JsonProperty("input_tokens")]
        public int InputTokens { get; set; }

        [JsonProperty("output_tokens")]
        public int OutputTokens { get; set; }
    }

    /// <summary>
    /// 工具定义结构
    /// </summary>
    public class ToolDefinition
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("input_schema")]
        public object InputSchema { get; set; }
    }

    /// <summary>
    /// 工具调用结果
    /// </summary>
    public class ToolResult
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "tool_result";

        [JsonProperty("tool_use_id")]
        public string ToolUseId { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("is_error")]
        public bool IsError { get; set; } = false;
    }
}