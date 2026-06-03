#!/usr/bin/env python3
"""
智能向量生成脚本 - 结合关键词匹配与语义特征
"""

import json
import sqlite3
import math
import os
import sys
from datetime import datetime

sys.stdout.reconfigure(encoding='utf-8')

DATA_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Data", "vectors")
DB_PATH = os.path.join(os.path.dirname(__file__), "..", "..", "Data", "kb", "terraria_kb.db")
VECTOR_DIM = 384

# 预定义语义关键词权重
SEMANTIC_KEYWORDS = {
    # 风格关键词
    "medieval": {"medieval": 3.0, "castle": 2.5, "knight": 2.0, "stone": 1.5, "brick": 1.5, "fortress": 2.0, "gray": 1.2},
    "fantasy": {"fantasy": 3.0, "magic": 2.5, "enchanted": 2.0, "pearlstone": 2.0, "divine": 1.8, "hallow": 1.5},
    "natural": {"natural": 3.0, "wood": 2.0, "forest": 1.5, "tree": 1.2, "grass": 1.2, "organic": 1.5, "rustic": 1.5},
    "steampunk": {"steampunk": 3.0, "industrial": 2.5, "copper": 2.0, "brass": 2.0, "gear": 1.5, "iron": 1.2},
    "asian": {"asian": 3.0, "dynasty": 2.5, "japanese": 2.0, "chinese": 2.0, "eastern": 1.5, "oriental": 1.5},
    "snow": {"snow": 3.0, "ice": 2.5, "winter": 2.0, "cold": 1.5, "frozen": 1.5, "boreal": 1.2},
    "desert": {"desert": 3.0, "sandstone": 2.5, "sand": 2.0, "egypt": 2.0, "arid": 1.5, "pyramid": 1.5},
    "modern": {"modern": 3.0, "glass": 2.0, "granite": 1.5, "steel": 1.5, "urban": 1.2, "sleek": 1.5},
    "luxury": {"luxury": 3.0, "gold": 2.5, "silver": 2.0, "premium": 2.0, "rich": 1.5, "palace": 2.0, "mahogany": 1.5},
    "transparent": {"transparent": 3.0, "glass": 2.5, "window": 2.0, "clear": 1.5, "ice": 1.2},

    # 中文关键词映射
    "玻璃": {"glass": 2.5, "transparent": 2.0, "透明": 2.5},
    "透明": {"transparent": 3.0, "glass": 2.0, "clear": 1.5},
    "木头": {"wood": 2.5, "natural": 1.5, "木材": 2.0},
    "砖": {"brick": 2.5, "stone": 1.5, "砖块": 2.0},
    "豪华": {"luxury": 3.0, "gold": 2.0, "premium": 2.0},
    "神圣": {"hallow": 2.5, "divine": 2.0, "pearlstone": 1.5},
    "腐化": {"corruption": 2.5, "ebonstone": 2.0, "dark": 1.5},
    "猩红": {"crimson": 2.5, "crimstone": 2.0, "blood": 1.5},
}

# 分类权重映射
CATEGORY_KEYWORDS = {
    "brick": {"brick": 3.0, "stone": 2.0, "砖": 2.5, "石": 1.5, "block": 1.2},
    "wood": {"wood": 3.0, "木材": 2.5, "木": 2.0, "natural": 1.5},
    "luxury": {"luxury": 3.0, "gold": 2.5, "silver": 2.0, "豪华": 2.5, "premium": 2.0},
    "transparent": {"transparent": 3.0, "glass": 2.5, "透明": 2.5, "玻璃": 2.0, "ice": 1.2},
    "natural": {"natural": 3.0, "dirt": 2.0, "grass": 1.5, "自然": 2.0},
    "light": {"light": 3.0, "lamp": 2.5, "torch": 2.0, "candle": 1.5, "光源": 2.0, "光": 1.5},
    "door": {"door": 3.0, "gate": 2.5, "门": 2.5},
    "platform": {"platform": 3.0, "platforms": 2.5, "平台": 2.0},
}


class SmartVectorizer:
    """智能向量生成器 - 结合语义关键词与文本特征"""

    def __init__(self):
        self.word_weights = {}
        self.doc_count = {}
        self.total_docs = 0

    def fit(self, documents):
        """计算文档词频统计"""
        for doc in documents:
            words = self._tokenize(doc)
            for word in set(words):
                self.doc_count[word] = self.doc_count.get(word, 0) + 1
            self.total_docs += 1

    def _tokenize(self, text):
        """分词 - 支持中英文"""
        words = []
        # 英文词
        eng_words = text.lower().split()
        words.extend(eng_words)
        # 中文字符（单字和双字组合）
        chinese_chars = [c for c in text if '一' <= c <= '鿿']
        words.extend(chinese_chars)
        # 双字组合
        for i in range(len(chinese_chars) - 1):
            words.append(chinese_chars[i] + chinese_chars[i+1])
        return words

    def vectorize(self, text, dim=VECTOR_DIM, category=None, styles=None):
        """生成向量 - 结合语义关键词权重"""
        vector = [0.0] * dim
        words = self._tokenize(text)

        # 基础词频向量
        for word in words:
            idx = abs(hash(word)) % dim
            weight = self._get_word_weight(word, category, styles)
            vector[idx] += weight

        # 添加语义关键词增强
        all_text = text.lower()

        # 检查预定义关键词
        for key, weights in SEMANTIC_KEYWORDS.items():
            if key.lower() in all_text or key in text:
                for kw, w in weights.items():
                    idx = abs(hash(kw)) % dim
                    vector[idx] += w

        # 分类关键词增强
        if category and category in CATEGORY_KEYWORDS:
            for kw, w in CATEGORY_KEYWORDS[category].items():
                idx = abs(hash(kw)) % dim
                vector[idx] += w

        # 归一化
        mag = math.sqrt(sum(v * v for v in vector))
        if mag > 0:
            vector = [v / mag for v in vector]

        return vector

    def _get_word_weight(self, word, category=None, styles=None):
        """获取词权重"""
        # 基础TF-IDF权重
        base_weight = 1.0
        if word in self.doc_count:
            idf = math.log(self.total_docs / (self.doc_count[word] + 1)) + 1
            base_weight = idf

        # 检查是否是预定义关键词
        for key, weights in SEMANTIC_KEYWORDS.items():
            if word.lower() == key.lower() or word == key:
                return max(base_weight, max(weights.values()))

        return base_weight


def generate_smart_embeddings():
    """生成智能向量"""
    print("=" * 60)
    print("智能向量生成脚本")
    print("=" * 60)

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    # 收集所有文本用于训练
    cursor.execute("SELECT id, name, display_name, category, styles, biome_match, description FROM tiles")
    tiles = cursor.fetchall()

    documents = []
    for tile in tiles:
        tile_id, name, display_name, category, styles, biome_match, description = tile
        text = f"{name} {display_name} {category} {description or ''}"
        try:
            styles_list = json.loads(styles) if styles else []
            text += " " + " ".join(styles_list)
        except:
            pass
        documents.append(text)

    # 初始化向量器
    vectorizer = SmartVectorizer()
    vectorizer.fit(documents)

    # 生成方块向量
    print("\n=== 生成方块向量 ===")
    tile_embeddings = {}

    for tile in tiles:
        tile_id, name, display_name, category, styles, biome_match, description = tile
        text = f"{name} {display_name} {category} {description or ''}"
        try:
            styles_list = json.loads(styles) if styles else []
            styles_str = " ".join(styles_list)
        except:
            styles_str = ""

        vector = vectorizer.vectorize(text, category=category, styles=styles_str)
        tile_embeddings[tile_id] = vector

    # 保存方块向量
    output_path = os.path.join(DATA_DIR, "tile_embeddings.json")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump({
            "version": "3.0",
            "method": "smart_keyword_tfidf",
            "dimension": VECTOR_DIM,
            "generated": datetime.now().isoformat(),
            "count": len(tile_embeddings),
            "embeddings": {str(k): v for k, v in tile_embeddings.items()}
        }, f, indent=2)
    print(f"  生成 {len(tile_embeddings)} 个方块向量")
    print(f"  输出: {output_path}")

    # 生成风格向量
    print("\n=== 生成风格向量 ===")
    cursor.execute("SELECT name, display_name, description, primary_tiles, primary_walls FROM style_templates")
    styles_data = cursor.fetchall()

    style_embeddings = {}
    for style in styles_data:
        name, display_name, description, primary_tiles, primary_walls = style
        text = f"{name} {display_name} {description or ''}"
        try:
            tiles_list = json.loads(primary_tiles) if primary_tiles else []
            text += " " + " ".join(tiles_list)
        except:
            pass
        vector = vectorizer.vectorize(text, category=None, styles=name)
        style_embeddings[name] = vector

    output_path = os.path.join(DATA_DIR, "style_embeddings.json")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump({
            "version": "3.0",
            "method": "smart_keyword_tfidf",
            "dimension": VECTOR_DIM,
            "generated": datetime.now().isoformat(),
            "count": len(style_embeddings),
            "embeddings": style_embeddings
        }, f, indent=2)
    print(f"  生成 {len(style_embeddings)} 个风格向量")

    # 生成墙壁向量
    print("\n=== 生成墙壁向量 ===")
    cursor.execute("SELECT id, name, display_name, category, styles, description FROM walls")
    walls_data = cursor.fetchall()

    wall_embeddings = {}
    for wall in walls_data:
        wall_id, name, display_name, category, styles, description = wall
        text = f"{name} {display_name} {category} {description or ''}"
        try:
            styles_list = json.loads(styles) if styles else []
            styles_str = " ".join(styles_list)
        except:
            styles_str = ""
        vector = vectorizer.vectorize(text, category=category, styles=styles_str)
        wall_embeddings[wall_id] = vector

    output_path = os.path.join(DATA_DIR, "wall_embeddings.json")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump({
            "version": "3.0",
            "method": "smart_keyword_tfidf",
            "dimension": VECTOR_DIM,
            "generated": datetime.now().isoformat(),
            "count": len(wall_embeddings),
            "embeddings": {str(k): v for k, v in wall_embeddings.items()}
        }, f, indent=2)
    print(f"  生成 {len(wall_embeddings)} 个墙壁向量")
    print(f"  输出: {output_path}")

    # 生成家具向量
    print("\n=== 生成家具向量 ===")
    cursor.execute("SELECT id, name, display_name, category, npc_function, description FROM furniture")
    furniture_data = cursor.fetchall()

    furniture_embeddings = {}
    for furniture in furniture_data:
        furniture_id, name, display_name, category, npc_function, description = furniture
        text = f"{name} {display_name} {category} {description or ''}"
        try:
            npc_list = json.loads(npc_function) if npc_function else []
            npc_str = " ".join(npc_list)
        except:
            npc_str = ""
        vector = vectorizer.vectorize(text, category=category, styles=npc_str)
        furniture_embeddings[furniture_id] = vector

    output_path = os.path.join(DATA_DIR, "furniture_embeddings.json")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump({
            "version": "3.0",
            "method": "smart_keyword_tfidf",
            "dimension": VECTOR_DIM,
            "generated": datetime.now().isoformat(),
            "count": len(furniture_embeddings),
            "embeddings": {str(k): v for k, v in furniture_embeddings.items()}
        }, f, indent=2)
    print(f"  生成 {len(furniture_embeddings)} 个家具向量")
    print(f"  输出: {output_path}")

    conn.close()

    return tile_embeddings, style_embeddings, wall_embeddings, furniture_embeddings


def test_similarity(tile_embeddings):
    """测试相似度匹配"""
    print("\n=== 测试相似度匹配 ===")

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    vectorizer = SmartVectorizer()

    def cosine_sim(v1, v2):
        dot = sum(a * b for a, b in zip(v1, v2))
        mag1 = math.sqrt(sum(a * a for a in v1))
        mag2 = math.sqrt(sum(b * b for b in v2))
        return dot / (mag1 * mag2) if mag1 > 0 and mag2 > 0 else 0

    # 测试用例
    test_cases = [
        ("玻璃 透明 glass transparent", "transparent"),
        ("豪华 金砖 luxury gold premium", "luxury"),
        ("木头 木材 wood natural", "wood"),
        ("砖块 石砖 brick stone medieval", "brick"),
        ("现代 glass modern building", "transparent"),
        ("神圣 hallow divine pearlstone", "luxury"),
        ("东方 dynasty asian wood", "wood"),
    ]

    for query, expected_category in test_cases:
        print(f"\n查询: '{query}' (期望: {expected_category})")

        query_vec = vectorizer.vectorize(query)

        results = []
        for tile_id_str, tile_vec in tile_embeddings.items():
            tile_id = int(tile_id_str)
            sim = cosine_sim(query_vec, tile_vec)

            cursor.execute('SELECT name, display_name, category FROM tiles WHERE id = ?', (tile_id,))
            row = cursor.fetchone()
            if row:
                results.append((row[0], row[1], row[2], sim))

        results.sort(key=lambda x: x[3], reverse=True)
        print("  Top-5:")
        for name, display, category, sim in results[:5]:
            match = "✓" if category == expected_category else ""
            print(f"    {name} ({display}/{category}): {sim:.3f} {match}")

    conn.close()


if __name__ == "__main__":
    tile_embeddings, style_embeddings, wall_embeddings, furniture_embeddings = generate_smart_embeddings()
    test_similarity(tile_embeddings)

    print("\n" + "=" * 60)
    print("完成!")
    print("=" * 60)