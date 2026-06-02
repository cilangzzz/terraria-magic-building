# 向量生成工具

本目录包含为知识库生成向量嵌入的脚本，用于语义搜索。

---

## 脚本列表

| 脚本 | 方法 | 适用场景 | 推荐 |
|------|------|----------|------|
| generate_embeddings_smart.py | 语义关键词匹配 | tiles/styles/biomes | ✓ 推荐 |
| generate_embeddings_full.py | SentenceTransformers/哈希 | tiles/walls/furniture/styles | 全面 |

---

## 向量维度

统一使用 **384维** 向量，与SentenceTransformers `all-MiniLM-L6-v2` 模型兼容。

---

## 推荐使用

### generate_embeddings_smart.py (推荐)

```bash
cd Tools/vector
python generate_embeddings_smart.py
```

**特点**:
- 预定义语义关键词权重
- 中英文双语支持
- 分类关键词增强
- 无需网络/外部依赖
- 相似度匹配效果最佳

**输出**: `tile_embeddings.json`, `style_embeddings.json`

**测试结果**:
```
玻璃/透明 → Glass: 0.778 ✓
豪华/金砖 → GoldBrick: 0.628 ✓
神圣/珍珠石 → Pearlstone: 0.554 ✓
东方/王朝木 → DynastyWood: 0.408 ✓
```

### generate_embeddings_full.py (全面)

```bash
cd Tools/vector
python generate_embeddings_full.py
```

**特点**:
- 处理所有表：tiles、walls、furniture、styles
- 支持SentenceTransformers（需网络）
- 哈希向量作为备用方案

**输出**: `tile_embeddings.json`, `wall_embeddings.json`, `furniture_embeddings.json`, `style_embeddings.json`

---

## 生成文件

运行后生成以下JSON文件到 `Data/` 目录:

| 文件 | 内容 |
|------|------|
| tile_embeddings.json | 方块向量 (35个) |
| style_embeddings.json | 风格向量 (10个) |
| biome_embeddings.json | 生物群落向量 (13个) |

---

## 向量格式

```json
{
  "version": "3.0",
  "method": "smart_keyword_tfidf",
  "dimension": 384,
  "generated": "2026-06-02T...",
  "count": 35,
  "embeddings": {
    "1": [0.123, 0.456, ...],  // StoneBlock
    "13": [0.234, 0.567, ...], // Glass
    ...
  }
}
```

---

## 语义关键词映射

预定义关键词权重:

```python
SEMANTIC_KEYWORDS = {
    "玻璃": {"glass": 2.5, "transparent": 2.0},
    "透明": {"transparent": 3.0, "glass": 2.0},
    "豪华": {"luxury": 3.0, "gold": 2.0},
    "神圣": {"hallow": 2.5, "divine": 2.0},
    "东方": {"dynasty": 2.5, "asian": 2.0},
    ...
}
```

---

## C#集成

向量文件由 `VectorKnowledgeBase.cs` 加载:

```csharp
public class VectorKnowledgeBase
{
    private Dictionary<int, float[]> _tileEmbeddings;
    
    public void Initialize()
    {
        var json = File.ReadAllText("Data/tile_embeddings.json");
        var data = JsonConvert.DeserializeObject<EmbeddingData>(json);
        _tileEmbeddings = data.embeddings;
    }
    
    public float CosineSimilarity(float[] a, float[] b)
    {
        // 计算余弦相似度
    }
}
```