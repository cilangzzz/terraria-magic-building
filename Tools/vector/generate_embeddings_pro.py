#!/usr/bin/env python3
"""
高质量向量生成脚本 (使用SentenceTransformers)
语义向量，捕捉词义关系，匹配度更高

依赖安装: pip install sentence-transformers
"""

import json
import sqlite3
import math
import os
import sys
from datetime import datetime

sys.stdout.reconfigure(encoding='utf-8')

# 路径配置
DATA_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Data")
DB_PATH = os.path.join(DATA_DIR, "terraria_kb.db")
VECTOR_DIM = 384

# 尝试导入SentenceTransformers
HAS_MODEL = False
MODEL = None
try:
    from sentence_transformers import SentenceTransformer
    # 尝试加载模型，如果网络失败则回退到TF-IDF
    try:
        MODEL = SentenceTransformer('all-MiniLM-L6-v2')
        HAS_MODEL = True
        print("使用 SentenceTransformers 模型生成高质量向量")
    except Exception as e:
        print(f"模型加载失败 (网络问题): {type(e).__name__}")
        print("回退使用改进的TF-IDF方法")
except ImportError:
    print("未安装 sentence-transformers，使用改进的TF-IDF方法")
    print("安装命令: pip install sentence-transformers")


# 预定义风格关键词（增强语义）
STYLE_KEYWORDS = {
    "medieval": "medieval castle knight stone brick fortress gray brick wood torch banner flag stone slab ancient fortress",
    "fantasy": "fantasy magic elf fairy enchanted mystical pearlstone glass gold divine pearl holy rainbow magical ethereal",
    "natural": "natural wood forest tree grass organic dirt living wood rustic wild plant flower nature outdoor",
    "steampunk": "steampunk industrial copper brass gear iron brick mechanical cog metal pipe factory vintage machinery",
    "asian": "asian japanese chinese temple pagoda bamboo dynasty wood lantern paper tea eastern oriental traditional",
    "snow": "snow ice winter cold frozen arctic frost boreal wood ice block white blue crystal frozen winter",
    "desert": "desert sand egypt pyramid sandstone arid palm wood sandstone slab hot dry golden sandy ancient",
    "underground": "underground cavern cave stone dirt torch dark shadow moss gem crystal mineral dungeon",
    "ocean": "ocean sea beach tropical palm wood glass coral blue water fish shell wave aquatic marine",
    "modern": "modern contemporary sleek glass granite marble steel urban clean white gray minimalist contemporary",
}

BIOME_KEYWORDS = {
    "forest": "forest grass wood tree stone green natural leaf plant wildlife bird outdoor",
    "desert": "desert sand sandstone dry hot arid cactus palm scorpion vulture oasis sandy",
    "snow": "snow ice cold winter freeze blizzard frost polar arctic boreal tundra frozen",
    "jungle": "jungle tropical rich mahogany mud plant vine fruit tiger monkey parrot rainforest",
    "ocean": "ocean water coral fish palm beach tropical wave shell dolphin whale aquatic",
    "underground": "underground cavern cave dark stone dirt mineral gem crystal bat dungeon",
    "cavern": "cavern deep stone marble granite obsidian crystal stalactite mushroom cave",
    "hallow": "hallow pearlstone pearlwood divine rainbow fantasy light pink unicorn pixie magical",
    "corruption": "corruption ebonstone dark purple evil shadow chasm eater chaos rift cursed",
    "crimson": "crimson crimstone blood red flesh gore brain heart vein ichor bloody",
    "glowing_mushroom": "glowing mushroom blue fungi truffle spore mycelium glow illuminate bioluminescent",
    "dungeon": "dungeon brick blue green pink locked chain skeleton undead curse ancient",
    "hell": "hell obsidian hellstone ash fire lava demon imp brimstone inferno underworld",
}


class ImprovedVectorizer:
    """改进的向量生成器"""

    def __init__(self):
        self.word_weights = {}
        self.word_count = {}
        self.total_docs = 0

    def fit(self, documents):
        """计算TF-IDF权重"""
        # 统计词频
        for doc in documents:
            words = set(doc.lower().split())
            for word in words:
                self.word_count[word] = self.word_count.get(word, 0) + 1
            self.total_docs += 1

        # 计算IDF权重
        for word, count in self.word_count.items():
            self.word_weights[word] = math.log(self.total_docs / (count + 1)) + 1

    def vectorize(self, text, dim=VECTOR_DIM):
        """生成TF-IDF加权向量"""
        vector = [0.0] * dim
        words = text.lower().split()

        for word in words:
            # 主索引
            idx = abs(hash(word)) % dim
            weight = self.word_weights.get(word, 1.0)
            vector[idx] += weight

            # 添加词根哈希（处理词形变化）
            root = word[:4] if len(word) > 4 else word
            idx_root = abs(hash(root)) % dim
            vector[idx_root] += weight * 0.5

            # 添加词尾哈希
            suffix = word[-3:] if len(word) > 3 else word
            idx_suffix = abs(hash(suffix)) % dim
            vector[idx_suffix] += weight * 0.3

        # 归一化
        mag = math.sqrt(sum(v * v for v in vector))
        if mag > 0:
            vector = [v / mag for v in vector]

        return vector


def generate_embeddings_with_model(model, text):
    """使用模型生成向量"""
    embedding = model.encode(text)
    return embedding.tolist()


def generate_tile_embeddings(model=None, vectorizer=None):
    """生成方块向量"""
    print("\n=== 生成方块向量 ===")

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    cursor.execute("SELECT id, name, display_name, category, styles, biome_match, description FROM tiles")
    tiles = cursor.fetchall()

    conn.close()

    embeddings = {}
    documents = []

    # 先收集所有文本用于TF-IDF
    for tile in tiles:
        tile_id, name, display_name, category, styles, biome_match, description = tile
        text_parts = [name, display_name, category, description or ""]
        try:
            styles_list = json.loads(styles) if styles else []
            text_parts.extend(styles_list)
        except:
            pass
        try:
            biome_list = json.loads(biome_match) if biome_match else []
            text_parts.extend(biome_list)
        except:
            pass
        documents.append(" ".join([p for p in text_parts if p]))

    if vectorizer and not model:
        vectorizer.fit(documents)

    # 生成向量
    for tile, doc in zip(tiles, documents):
        tile_id = tile[0]

        if model:
            vector = generate_embeddings_with_model(model, doc)
        else:
            vector = vectorizer.vectorize(doc)

        embeddings[tile_id] = vector

    # 保存
    output_path = os.path.join(DATA_DIR, "tile_embeddings.json")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump({
            "version": "2.1",
            "method": "sentence_transformers" if model else "tfidf_improved",
            "dimension": VECTOR_DIM,
            "generated": datetime.now().isoformat(),
            "count": len(embeddings),
            "embeddings": {str(k): v for k, v in embeddings.items()}
        }, f, indent=2)

    print(f"  生成 {len(embeddings)} 个方块向量")
    print(f"  输出: {output_path}")

    return embeddings


def generate_style_embeddings(model=None, vectorizer=None):
    """生成风格向量"""
    print("\n=== 生成风格向量 ===")

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    cursor.execute("SELECT name, display_name, description, primary_tiles, primary_walls FROM style_templates")
    styles = cursor.fetchall()

    conn.close()

    embeddings = {}

    for style in styles:
        name, display_name, description, primary_tiles, primary_walls = style

        text_parts = [name, display_name, description or ""]

        # 添加预定义关键词
        if name in STYLE_KEYWORDS:
            text_parts.append(STYLE_KEYWORDS[name])

        try:
            tiles_list = json.loads(primary_tiles) if primary_tiles else []
            text_parts.extend(tiles_list)
        except:
            pass

        try:
            walls_list = json.loads(primary_walls) if primary_walls else []
            text_parts.extend(walls_list)
        except:
            pass

        text = " ".join([p for p in text_parts if p])

        if model:
            vector = generate_embeddings_with_model(model, text)
        else:
            vector = vectorizer.vectorize(text)

        embeddings[name] = vector

    output_path = os.path.join(DATA_DIR, "style_embeddings.json")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump({
            "version": "2.1",
            "method": "sentence_transformers" if model else "tfidf_improved",
            "dimension": VECTOR_DIM,
            "generated": datetime.now().isoformat(),
            "count": len(embeddings),
            "embeddings": embeddings
        }, f, indent=2)

    print(f"  生成 {len(embeddings)} 个风格向量")

    return embeddings


def generate_biome_embeddings(model=None, vectorizer=None):
    """生成生物群落向量"""
    print("\n=== 生成生物群落向量 ===")

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    cursor.execute("SELECT name, display_name, description, characteristic_tiles, characteristic_walls FROM biomes")
    biomes = cursor.fetchall()

    conn.close()

    embeddings = {}

    for biome in biomes:
        name, display_name, description, characteristic_tiles, characteristic_walls = biome

        text_parts = [name, display_name, description or ""]

        # 添加预定义关键词
        if name in BIOME_KEYWORDS:
            text_parts.append(BIOME_KEYWORDS[name])

        try:
            tiles_list = json.loads(characteristic_tiles) if characteristic_tiles else []
            text_parts.extend(tiles_list)
        except:
            pass

        try:
            walls_list = json.loads(characteristic_walls) if characteristic_walls else []
            text_parts.extend(walls_list)
        except:
            pass

        text = " ".join([p for p in text_parts if p])

        if model:
            vector = generate_embeddings_with_model(model, text)
        else:
            vector = vectorizer.vectorize(text)

        embeddings[name] = vector

    output_path = os.path.join(DATA_DIR, "biome_embeddings.json")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump({
            "version": "2.1",
            "method": "sentence_transformers" if model else "tfidf_improved",
            "dimension": VECTOR_DIM,
            "generated": datetime.now().isoformat(),
            "count": len(embeddings),
            "embeddings": embeddings
        }, f, indent=2)

    print(f"  生成 {len(embeddings)} 个生物群落向量")

    return embeddings


def test_similarity(tile_embeddings, style_embeddings):
    """测试相似度"""
    print("\n=== 测试相似度匹配 ===")

    def cosine_sim(v1, v2):
        dot = sum(a * b for a, b in zip(v1, v2))
        mag1 = math.sqrt(sum(a * a for a in v1))
        mag2 = math.sqrt(sum(b * b for b in v2))
        return dot / (mag1 * mag2) if mag1 > 0 and mag2 > 0 else 0

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    # 测试用例
    test_cases = [
        ("glass transparent window", "transparent"),
        ("luxury wood mahogany premium", "luxury"),
        ("modern glass building", "modern"),
    ]

    vectorizer = ImprovedVectorizer()

    for query, expected_category in test_cases:
        print(f"\n查询: '{query}' (期望分类: {expected_category})")

        if HAS_MODEL and MODEL:
            query_vec = generate_embeddings_with_model(MODEL, query)
        else:
            # 使用TF-IDF方法需要先fit
            all_texts = [query] + list(STYLE_KEYWORDS.values())
            vectorizer.fit(all_texts)
            query_vec = vectorizer.vectorize(query)

        results = []
        for tile_id_str, tile_vec in tile_embeddings.items():
            tile_id = int(tile_id_str)
            sim = cosine_sim(query_vec, tile_vec)

            cursor.execute('SELECT name, display_name, category FROM tiles WHERE id = ?', (tile_id,))
            row = cursor.fetchone()
            if row:
                results.append((row[0], row[2], sim))

        results.sort(key=lambda x: x[2], reverse=True)
        print("  Top-5:")
        for name, category, sim in results[:5]:
            match = "✓" if category == expected_category else ""
            print(f"    {name} ({category}): {sim:.3f} {match}")

    conn.close()


def main():
    print("=" * 60)
    print("向量库生成脚本 (高质量版)")
    print("=" * 60)

    # 使用预加载的模型或TF-IDF向量器
    if HAS_MODEL and MODEL:
        vectorizer = None
        print(f"加载模型: all-MiniLM-L6-v2 (维度: {VECTOR_DIM})")
    else:
        vectorizer = ImprovedVectorizer()
        print("使用改进的TF-IDF方法")

    # 生成向量
    tile_embeddings = generate_tile_embeddings(MODEL, vectorizer)
    style_embeddings = generate_style_embeddings(MODEL, vectorizer)
    biome_embeddings = generate_biome_embeddings(MODEL, vectorizer)

    # 测试
    test_similarity(tile_embeddings, style_embeddings)

    print("\n" + "=" * 60)
    print("完成!")
    print("=" * 60)


if __name__ == "__main__":
    main()