using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Terraria.ModLoader;

namespace trab.Core
{
    /// <summary>
    /// 向量知识库 - 支持语义检索
    /// </summary>
    public class VectorKnowledgeBase
    {
        private Dictionary<int, float[]> _tileEmbeddings;
        private Dictionary<string, float[]> _styleEmbeddings;
        private Dictionary<int, TileEmbeddingEntry> _tileEntries;
        private int _dimension = 384;
        private bool _initialized = false;

        public bool IsInitialized => _initialized;
        public int TileVectorCount => _tileEmbeddings?.Count ?? 0;
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
                LoadStyleEmbeddings();
                _initialized = true;

                trab.Instance?.Logger.Info($"向量库初始化完成: Tiles={TileVectorCount}, Styles={StyleVectorCount}");
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

            // 使用相对路径，从Mod源码目录加载
            string basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string modSourcePath = Path.Combine(basePath, "..", "..", "..", "..", "ModSources", "trab");
            string path = Path.Combine(modSourcePath, "Data", "tile_embeddings.json");

            // 备用路径：直接从运行目录
            if (!File.Exists(path))
            {
                path = Path.Combine(basePath, "Data", "tile_embeddings.json");
            }

            if (!File.Exists(path))
            {
                trab.Instance?.Logger.Warn($"Tile向量文件不存在，使用空向量库");
                return;
            }

            string json = File.ReadAllText(path);
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

            string basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string modSourcePath = Path.Combine(basePath, "..", "..", "..", "..", "ModSources", "trab");
            string path = Path.Combine(modSourcePath, "Data", "style_embeddings.json");

            if (!File.Exists(path))
            {
                path = Path.Combine(basePath, "Data", "style_embeddings.json");
            }

            if (!File.Exists(path))
            {
                trab.Instance?.Logger.Warn($"Style向量文件不存在，使用空向量库");
                return;
            }

            string json = File.ReadAllText(path);
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
        /// 获取tile的embedding信息
        /// </summary>
        public TileEmbeddingEntry GetTileEmbedding(int tileId)
        {
            _tileEntries.TryGetValue(tileId, out var entry);
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