using Newtonsoft.Json;
using System.Collections.Generic;

namespace trab.Data
{
    /// <summary>
    /// TEditSch 格式建筑设计 - 与 TEdit Schematic JSON 格式完全兼容
    /// </summary>
    public class TEditSchDesign
    {
        [JsonProperty("name")]
        public string name { get; set; } = "未命名建筑";

        [JsonProperty("width")]
        public int width { get; set; }

        [JsonProperty("height")]
        public int height { get; set; }

        [JsonProperty("tiles")]
        public List<List<TEditTile>> tiles { get; set; } = new List<List<TEditTile>>();

        [JsonProperty("stats", NullValueHandling = NullValueHandling.Ignore)]
        public TEditSchStats stats { get; set; }

        /// <summary>
        /// 获取指定坐标的 Tile，如果越界返回 null
        /// </summary>
        public TEditTile GetTile(int x, int y)
        {
            if (y < 0 || y >= tiles.Count) return null;
            var row = tiles[y];
            if (x < 0 || x >= row.Count) return null;
            return row[x];
        }

        /// <summary>
        /// 设置指定坐标的 Tile
        /// </summary>
        public void SetTile(int x, int y, TEditTile tile)
        {
            EnsureCapacity(x + 1, y + 1);
            tiles[y][x] = tile;
        }

        /// <summary>
        /// 确保网格容量足够
        /// </summary>
        public void EnsureCapacity(int requiredWidth, int requiredHeight)
        {
            while (tiles.Count < requiredHeight)
            {
                var row = new List<TEditTile>();
                for (int x = 0; x < width; x++)
                    row.Add(new TEditTile());
                tiles.Add(row);
            }

            foreach (var row in tiles)
            {
                while (row.Count < requiredWidth)
                    row.Add(new TEditTile());
            }

            if (requiredWidth > width) width = requiredWidth;
            if (requiredHeight > height) height = requiredHeight;
        }

        /// <summary>
        /// 计算统计信息
        /// </summary>
        public void CalculateStats()
        {
            var stats = new TEditSchStats();
            var tileDist = new Dictionary<int, int>();
            var wallDist = new Dictionary<int, int>();

            foreach (var row in tiles)
            {
                foreach (var tile in row)
                {
                    if (tile.active && tile.type.HasValue)
                    {
                        stats.active_tiles++;
                        int tid = tile.type.Value;
                        if (!tileDist.ContainsKey(tid)) tileDist[tid] = 0;
                        tileDist[tid]++;
                    }
                    if (tile.wall.HasValue)
                    {
                        stats.tiles_with_wall++;
                        int wid = tile.wall.Value;
                        if (!wallDist.ContainsKey(wid)) wallDist[wid] = 0;
                        wallDist[wid]++;
                    }
                    if (tile.liquid_type.HasValue)
                        stats.tiles_with_liquid++;
                }
            }

            stats.tile_type_distribution = tileDist;
            stats.wall_distribution = wallDist;
            this.stats = stats;
        }
    }

    /// <summary>
    /// 单个 Tile 数据
    /// </summary>
    public class TEditTile
    {
        [JsonProperty("active")]
        public bool active { get; set; } = false;

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public int? type { get; set; }

        [JsonProperty("u", NullValueHandling = NullValueHandling.Ignore)]
        public int? u { get; set; }

        [JsonProperty("v", NullValueHandling = NullValueHandling.Ignore)]
        public int? v { get; set; }

        [JsonProperty("wall", NullValueHandling = NullValueHandling.Ignore)]
        public int? wall { get; set; }

        [JsonProperty("wall_color", NullValueHandling = NullValueHandling.Ignore)]
        public int? wall_color { get; set; }

        [JsonProperty("tile_color", NullValueHandling = NullValueHandling.Ignore)]
        public int? tile_color { get; set; }

        [JsonProperty("liquid_type", NullValueHandling = NullValueHandling.Ignore)]
        public int? liquid_type { get; set; }

        [JsonProperty("liquid_amount", NullValueHandling = NullValueHandling.Ignore)]
        public int? liquid_amount { get; set; }

        [JsonProperty("wires")]
        public TEditWires wires { get; set; } = new TEditWires();

        [JsonProperty("actuator")]
        public bool actuator { get; set; } = false;

        [JsonProperty("actuator_inactive")]
        public bool actuator_inactive { get; set; } = false;

        /// <summary>
        /// 创建空 Tile（无方块无墙）
        /// </summary>
        public static TEditTile Empty() => new TEditTile { active = false };

        /// <summary>
        /// 创建方块 Tile
        /// </summary>
        public static TEditTile Block(int tileId, int? wallId = null, int? paint = null)
        {
            return new TEditTile
            {
                active = true,
                type = tileId,
                wall = wallId,
                tile_color = paint
            };
        }

        /// <summary>
        /// 创建墙壁 Tile（无方块）
        /// </summary>
        public static TEditTile WallOnly(int wallId, int? paint = null)
        {
            return new TEditTile
            {
                active = false,
                wall = wallId,
                wall_color = paint
            };
        }
    }

    /// <summary>
    /// 红石线路信息
    /// </summary>
    public class TEditWires
    {
        [JsonProperty("red")]
        public bool red { get; set; } = false;

        [JsonProperty("blue")]
        public bool blue { get; set; } = false;

        [JsonProperty("green")]
        public bool green { get; set; } = false;

        [JsonProperty("yellow")]
        public bool yellow { get; set; } = false;
    }

    /// <summary>
    /// 建筑统计信息
    /// </summary>
    public class TEditSchStats
    {
        [JsonProperty("active_tiles")]
        public int active_tiles { get; set; }

        [JsonProperty("tiles_with_wall")]
        public int tiles_with_wall { get; set; }

        [JsonProperty("tiles_with_liquid")]
        public int tiles_with_liquid { get; set; }

        [JsonProperty("tile_type_distribution")]
        public Dictionary<int, int> tile_type_distribution { get; set; } = new Dictionary<int, int>();

        [JsonProperty("wall_distribution")]
        public Dictionary<int, int> wall_distribution { get; set; } = new Dictionary<int, int>();
    }

    /// <summary>
    /// 简化的建筑数据（用于 Agent 内部处理）
    /// </summary>
    public class SimplifiedBuilding
    {
        public string name { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public List<TilePlacement> tiles { get; set; } = new List<TilePlacement>();
        public List<WallRange> wallRanges { get; set; } = new List<WallRange>();

        /// <summary>
        /// 转换为 TEditSch 格式
        /// </summary>
        public TEditSchDesign ToTEditSch()
        {
            var design = new TEditSchDesign
            {
                name = name,
                width = width,
                height = height
            };

            // 初始化空网格
            for (int y = 0; y < height; y++)
            {
                var row = new List<TEditTile>();
                for (int x = 0; x < width; x++)
                    row.Add(TEditTile.Empty());
                design.tiles.Add(row);
            }

            // 放置墙壁范围
            foreach (var range in wallRanges)
            {
                for (int y = range.y1; y <= range.y2; y++)
                {
                    for (int x = range.x1; x <= range.x2; x++)
                    {
                        if (y >= 0 && y < height && x >= 0 && x < width)
                        {
                            design.tiles[y][x].wall = range.wall_id;
                            design.tiles[y][x].wall_color = range.paint;
                        }
                    }
                }
            }

            // 放置方块
            foreach (var tile in tiles)
            {
                if (tile.y >= 0 && tile.y < height && tile.x >= 0 && tile.x < width)
                {
                    design.tiles[tile.y][tile.x] = new TEditTile
                    {
                        active = true,
                        type = tile.tile_id,
                        wall = design.tiles[tile.y][tile.x].wall,
                        tile_color = tile.paint,
                        u = tile.u,
                        v = tile.v
                    };
                }
            }

            design.CalculateStats();
            return design;
        }
    }

    public class TilePlacement
    {
        public int x { get; set; }
        public int y { get; set; }
        public int tile_id { get; set; }
        public int? u { get; set; }
        public int? v { get; set; }
        public int? paint { get; set; }
    }

    public class WallRange
    {
        public int x1 { get; set; }
        public int y1 { get; set; }
        public int x2 { get; set; }
        public int y2 { get; set; }
        public int wall_id { get; set; }
        public int? paint { get; set; }
    }
}
