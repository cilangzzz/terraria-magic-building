using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Terraria;
using Terraria.ModLoader;

namespace trab.Core
{
    /// <summary>
    /// 向量知识库 - 支持语义检索
    /// 支持: 方块、墙壁、家具、风格
    /// </summary>
    public class VectorKnowledgeBase
    {
        private Dictionary<int, float[]> _tileEmbeddings;
        private Dictionary<int, float[]> _wallEmbeddings;
        private Dictionary<int, float[]> _furnitureEmbeddings;
        private Dictionary<string, float[]> _styleEmbeddings;
        private Dictionary<string, float[]> _furnitureCategoryEmbeddings;

        private Dictionary<int, TileEmbeddingEntry> _tileEntries;
        private Dictionary<int, WallEmbeddingEntry> _wallEntries;
        private Dictionary<int, FurnitureEmbeddingEntry> _furnitureEntries;

        private int _dimension = 384;
        private bool _initialized = false;

        public bool IsInitialized => _initialized;
        public int TileVectorCount => _tileEmbeddings?.Count ?? 0;
        public int WallVectorCount => _wallEmbeddings?.Count ?? 0;
        public int FurnitureVectorCount => _furnitureEmbeddings?.Count ?? 0;
        public int StyleVectorCount => _styleEmbeddings?.Count ?? 0;

        /// <summary>
        /// 初始化向量库，从JSON加载预计算向量
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;

            try
            {
                LoadTileEmbeddings();
                LoadWallEmbeddings();
                LoadFurnitureEmbeddings();
                LoadStyleEmbeddings();
                _initialized = true;

                trab.Instance?.Logger.Info($"向量库初始化完成: Tiles={TileVectorCount}, Walls={WallVectorCount}, Furniture={FurnitureVectorCount}, Styles={StyleVectorCount}");
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error($"向量库初始化失败: {ex.Message}");
                _initialized = false;
            }
        }

        private void LoadTileEmbeddings()
        {
            _tileEmbeddings = new Dictionary<int, float[]>();
            _tileEntries = new Dictionary<int, TileEmbeddingEntry>();

            // 多路径尝试策略
            string[] possiblePaths = GetPossibleDataPaths("tile_embeddings.json");

            string foundPath = null;
            foreach (var p in possiblePaths)
            {
                if (File.Exists(p))
                {
                    foundPath = p;
                    break;
                }
            }

            if (foundPath == null)
            {
                trab.Instance?.Logger.Warn($"Tile向量文件不存在，向量检索将不可用");
                return;
            }

            trab.Instance?.Logger.Info($"加载Tile向量: {foundPath}");
            string json = File.ReadAllText(foundPath);
            var entries = JsonConvert.DeserializeObject<List<TileEmbeddingEntry>>(json);

            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (entry.tile_id >= 0 && entry.embedding != null && entry.embedding.Length > 0)
                {
                    _tileEmbeddings[entry.tile_id] = entry.embedding;
                    _tileEntries[entry.tile_id] = entry;
                    _dimension = entry.embedding.Length;
                }
            }
        }

        private void LoadStyleEmbeddings()
        {
            _styleEmbeddings = new Dictionary<string, float[]>();

            string[] possiblePaths = GetPossibleDataPaths("style_embeddings.json");

            string foundPath = null;
            foreach (var p in possiblePaths)
            {
                if (File.Exists(p))
                {
                    foundPath = p;
                    break;
                }
            }

            if (foundPath == null)
            {
                trab.Instance?.Logger.Warn($"Style向量文件不存在，向量检索将不可用");
                return;
            }

            trab.Instance?.Logger.Info($"加载Style向量: {foundPath}");
            string json = File.ReadAllText(foundPath);
            var entries = JsonConvert.DeserializeObject<List<StyleEmbeddingEntry>>(json);

            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.style) && entry.embedding != null)
                {
                    _styleEmbeddings[entry.style.ToLower()] = entry.embedding;
                }
            }
        }

        private void LoadWallEmbeddings()
        {
            _wallEmbeddings = new Dictionary<int, float[]>();
            _wallEntries = new Dictionary<int, WallEmbeddingEntry>();

            string[] possiblePaths = GetPossibleDataPaths("wall_embeddings.json");
            string foundPath = possiblePaths.FirstOrDefault(p => File.Exists(p));

            if (foundPath == null)
            {
                trab.Instance?.Logger.Warn($"Wall向量文件不存在");
                return;
            }

            trab.Instance?.Logger.Info($"加载Wall向量: {foundPath}");
            string json = File.ReadAllText(foundPath);
            var entries = JsonConvert.DeserializeObject<List<WallEmbeddingEntry>>(json);

            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (entry.wall_id >= 0 && entry.embedding != null && entry.embedding.Length > 0)
                {
                    _wallEmbeddings[entry.wall_id] = entry.embedding;
                    _wallEntries[entry.wall_id] = entry;
                }
            }
        }

        private void LoadFurnitureEmbeddings()
        {
            _furnitureEmbeddings = new Dictionary<int, float[]>();
            _furnitureCategoryEmbeddings = new Dictionary<string, float[]>();
            _furnitureEntries = new Dictionary<int, FurnitureEmbeddingEntry>();

            string[] possiblePaths = GetPossibleDataPaths("furniture_embeddings.json");
            string foundPath = possiblePaths.FirstOrDefault(p => File.Exists(p));

            if (foundPath == null)
            {
                trab.Instance?.Logger.Warn($"Furniture向量文件不存在");
                return;
            }

            trab.Instance?.Logger.Info($"加载Furniture向量: {foundPath}");
            string json = File.ReadAllText(foundPath);
            var entries = JsonConvert.DeserializeObject<List<FurnitureEmbeddingEntry>>(json);

            if (entries == null) return;

            foreach (var entry in entries)
            {
                // 家具条目
                if (entry.furniture_id >= 0 && entry.embedding != null)
                {
                    _furnitureEmbeddings[entry.furniture_id] = entry.embedding;
                    _furnitureEntries[entry.furniture_id] = entry;
                }
                // 家具类别条目
                if (!string.IsNullOrEmpty(entry.furniture_category) && entry.embedding != null)
                {
                    _furnitureCategoryEmbeddings[entry.furniture_category.ToLower()] = entry.embedding;
                }
            }
        }

        private string[] GetPossibleDataPaths(string filename)
        {
            var paths = new List<string>();

            // 1. Mod源码目录 (开发模式)
            string modSourcePath = Path.Combine(
                "C:", "Users", "admin", "Documents", "My Games", "Terraria",
                "tModLoader", "ModSources", "trab", "Data", filename);
            paths.Add(modSourcePath);

            // 2. 从Assembly位置推导
            try
            {
                string basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                // tModLoader运行目录结构
                paths.Add(Path.Combine(basePath, "Data", filename));
                // ModSources相对路径
                paths.Add(Path.Combine(basePath, "..", "..", "..", "..", "ModSources", "trab", "Data", filename));
                // Terraria数据目录
                string terrariaPath = Path.Combine(Main.SavePath, "Mods", "trab", "Data", filename);
                paths.Add(terrariaPath);
            }
            catch { }

            // 3. 当前工作目录
            paths.Add(Path.Combine(Directory.GetCurrentDirectory(), "Data", filename));
            paths.Add(Path.Combine(Directory.GetCurrentDirectory(), "..", "Data", filename));

            return paths.ToArray();
        }

        /// <summary>
        /// 获取风格向量
        /// </summary>
        public float[] GetStyleVector(string style)
        {
            if (!_initialized || string.IsNullOrEmpty(style))
                return null;

            _styleEmbeddings.TryGetValue(style.ToLower(), out var vector);
            return vector;
        }

        /// <summary>
        /// 计算两个向量的余弦相似度
        /// </summary>
        public float CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return 0f;

            float dot = 0f, magA = 0f, magB = 0f;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }

            if (magA == 0 || magB == 0)
                return 0f;

            return dot / (float)Math.Sqrt(magA * magB);
        }

        /// <summary>
        /// 向量相似度搜索 - 返回与目标向量最相似的tile_id列表
        /// </summary>
        public List<(int tile_id, float similarity)> SearchSimilarTiles(float[] queryVector, int topK = 10)
        {
            if (!_initialized || queryVector == null || _tileEmbeddings == null)
                return new List<(int, float)>();

            var results = new List<(int tile_id, float similarity)>();

            foreach (var kvp in _tileEmbeddings)
            {
                float sim = CosineSimilarity(kvp.Value, queryVector);
                if (sim > 0.3f)  // 相似度阈值
                {
                    results.Add((kvp.Key, sim));
                }
            }

            // 按相似度降序排序，取topK
            return results
                .OrderByDescending(r => r.similarity)
                .Take(topK)
                .ToList();
        }

        /// <summary>
        /// 混合检索：SQL过滤 + 向量排序
        /// </summary>
        public List<TileInfo> SearchTilesSemantic(
            List<TileInfo> candidates,
            string style,
            int topK = 20)
        {
            if (!_initialized || candidates == null || candidates.Count == 0)
                return candidates;

            // 获取风格向量
            var styleVector = GetStyleVector(style);
            if (styleVector == null)
            {
                // 无向量时，使用关键词匹配
                return candidates.Where(t =>
                    t.styles != null && t.styles.Contains(style)
                ).Take(topK).ToList();
            }

            // 向量相似度排序
            var scored = new List<(TileInfo tile, float score)>();

            foreach (var tile in candidates)
            {
                if (_tileEmbeddings.TryGetValue(tile.id, out var tileVector))
                {
                    float sim = CosineSimilarity(tileVector, styleVector);
                    scored.Add((tile, sim));
                }
                else
                {
                    // 无向量的tile给默认低分
                    scored.Add((tile, 0.1f));
                }
            }

            return scored
                .OrderByDescending(s => s.score)
                .Take(topK)
                .Select(s => s.tile)
                .ToList();
        }

        /// <summary>
        /// 墙壁语义检索
        /// </summary>
        public List<WallInfo> SearchWallsSemantic(
            List<WallInfo> candidates,
            string style,
            int topK = 20)
        {
            if (!_initialized || candidates == null || candidates.Count == 0)
                return candidates;

            var styleVector = GetStyleVector(style);
            if (styleVector == null)
            {
                return candidates.Where(w =>
                    w.styles != null && w.styles.Contains(style)
                ).Take(topK).ToList();
            }

            var scored = new List<(WallInfo wall, float score)>();

            foreach (var wall in candidates)
            {
                if (_wallEmbeddings.TryGetValue(wall.id, out var wallVector))
                {
                    float sim = CosineSimilarity(wallVector, styleVector);
                    scored.Add((wall, sim));
                }
                else
                {
                    scored.Add((wall, 0.1f));
                }
            }

            return scored
                .OrderByDescending(s => s.score)
                .Take(topK)
                .Select(s => s.wall)
                .ToList();
        }

        /// <summary>
        /// 家具语义检索
        /// </summary>
        public List<KeyValuePair<string, FurnitureInfo>> SearchFurnitureSemantic(
            List<KeyValuePair<string, FurnitureInfo>> candidates,
            string category,
            int topK = 20)
        {
            if (!_initialized || candidates == null || candidates.Count == 0)
                return candidates;

            // 获取家具类别向量
            float[] categoryVector = null;
            if (!string.IsNullOrEmpty(category))
            {
                _furnitureCategoryEmbeddings.TryGetValue(category.ToLower(), out categoryVector);
            }

            if (categoryVector == null)
            {
                return candidates.Take(topK).ToList();
            }

            var scored = new List<(KeyValuePair<string, FurnitureInfo> furniture, float score)>();

            foreach (var f in candidates)
            {
                if (_furnitureEmbeddings.TryGetValue(f.Value.tile_id, out var furnitureVector))
                {
                    float sim = CosineSimilarity(furnitureVector, categoryVector);
                    scored.Add((f, sim));
                }
                else
                {
                    scored.Add((f, 0.1f));
                }
            }

            return scored
                .OrderByDescending(s => s.score)
                .Take(topK)
                .Select(s => s.furniture)
                .ToList();
        }

        /// <summary>
        /// 获取tile的embedding信息
        /// </summary>
        public TileEmbeddingEntry GetTileEmbedding(int tileId)
        {
            _tileEntries.TryGetValue(tileId, out var entry);
            return entry;
        }

        /// <summary>
        /// 获取wall的embedding信息
        /// </summary>
        public WallEmbeddingEntry GetWallEmbedding(int wallId)
        {
            _wallEntries.TryGetValue(wallId, out var entry);
            return entry;
        }

        /// <summary>
        /// 获取furniture的embedding信息
        /// </summary>
        public FurnitureEmbeddingEntry GetFurnitureEmbedding(int furnitureId)
        {
            _furnitureEntries.TryGetValue(furnitureId, out var entry);
            return entry;
        }
    }

    /// <summary>
    /// Tile向量数据条目
    /// </summary>
    public class TileEmbeddingEntry
    {
        public int tile_id { get; set; }
        public string name { get; set; }
        public string display_name { get; set; }
        public string category { get; set; }
        public string text { get; set; }
        public float[] embedding { get; set; }
    }

    /// <summary>
    /// Wall向量数据条目
    /// </summary>
    public class WallEmbeddingEntry
    {
        public int wall_id { get; set; }
        public string name { get; set; }
        public string display_name { get; set; }
        public string category { get; set; }
        public string text { get; set; }
        public float[] embedding { get; set; }
    }

    /// <summary>
    /// Furniture向量数据条目
    /// </summary>
    public class FurnitureEmbeddingEntry
    {
        public int furniture_id { get; set; }
        public string furniture_category { get; set; }
        public string name { get; set; }
        public string display_name { get; set; }
        public string category { get; set; }
        public string text { get; set; }
        public float[] embedding { get; set; }
    }

    /// <summary>
    /// Style向量数据条目
    /// </summary>
    public class StyleEmbeddingEntry
    {
        public string style { get; set; }
        public string text { get; set; }
        public float[] embedding { get; set; }
    }
}