#!/usr/bin/env python3
"""
建筑蓝图向量生成脚本
为building_schematics生成向量嵌入，支持语义检索
"""

import json
import sqlite3
import math
import os
import sys
from datetime import datetime

sys.stdout.reconfigure(encoding='utf-8')

DATA_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Data")
DB_PATH = os.path.join(DATA_DIR, "terraria_kb.db")
VECTOR_DIM = 384

# 蓝图风格关键词权重
SCHEMATIC_KEYWORDS = {
    "house": "house home building shelter npc room door wall roof floor foundation house building",
    "castle": "castle fortress tower medieval stone brick wall battlement tower keep moat",
    "tower": "tower vertical high tall narrow climb ladder spiral staircase lookout watchtower",
    "bridge": "bridge crossing span platform wood stone connect path walkway suspension",
    "farm": "farm agricultural crops plant harvest field barn tractor farm outdoor nature",
    "furniture": "furniture table chair bed sofa lamp decoration interior indoor crafting station",
    "decoration": "decoration decorative ornamental aesthetic statue banner painting flower plant",
    "outdoor": "outdoor outside external nature tree grass flower garden park landscape",
    "underground": "underground cave cavern mine tunnel dungeon dark hidden secret below",
    "small": "small tiny compact mini little miniature simple basic",
    "large": "large big huge massive giant extensive complex grand",
    "natural": "natural organic wood living tree grass dirt forest rustic wild",
    "medieval": "medieval castle knight stone brick gray wood torch banner ancient old",
    "fantasy": "fantasy magic enchanted mystical pearlstone divine rainbow magical ethereal",
    "modern": "modern contemporary glass steel granite sleek urban clean minimalist",
    "asian": "asian dynasty oriental eastern japanese chinese temple pagoda bamboo lantern",
    "snow": "snow ice winter cold frozen boreal frost arctic tundra white blue",
    "desert": "desert sand sandstone egyptian pyramid arid hot dry golden sandy",
    "jungle": "jungle tropical mahogany rich wood plant vine rainforest wild green",
    "ocean": "ocean water coral fish palm beach tropical wave shell aquatic marine",
}


class SchematicVectorizer:
    """蓝图向量生成器"""

    def __init__(self):
        self.word_weights = {}
        self.doc_count = {}
        self.total_docs = 0

    def fit(self, documents):
        """训练TF-IDF权重"""
        for doc in documents:
            words = self._tokenize(doc)
            for word in set(words):
                self.doc_count[word] = self.doc_count.get(word, 0) + 1
            self.total_docs += 1

    def _tokenize(self, text):
        """分词"""
        words = []
        words.extend(text.lower().split())
        chinese_chars = [c for c in text if '一' <= c <= '鿿']
        words.extend(chinese_chars)
        for i in range(len(chinese_chars) - 1):
            words.append(chinese_chars[i] + chinese_chars[i+1])
        return words

    def vectorize(self, text, dim=VECTOR_DIM):
        """生成向量"""
        vector = [0.0] * dim
        words = self._tokenize(text)

        for word in words:
            idx = abs(hash(word)) % dim
            weight = self._get_weight(word)
            vector[idx] += weight

        text_lower = text.lower()
        for key, kw_text in SCHEMATIC_KEYWORDS.items():
            if key in text_lower:
                for kw in kw_text.split():
                    idx = abs(hash(kw)) % dim
                    vector[idx] += 2.0

        mag = math.sqrt(sum(v * v for v in vector))
        if mag > 0:
            vector = [v / mag for v in vector]

        return vector

    def _get_weight(self, word):
        if word in self.doc_count:
            return math.log(self.total_docs / (self.doc_count[word] + 1)) + 1
        return 1.0


def generate_schematic_embeddings():
    """生成蓝图向量"""
    print("=" * 60)
    print("建筑蓝图向量生成脚本")
    print("=" * 60)

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    cursor.execute("""
        SELECT bs.id, bs.name, bs.width, bs.height,
               bs.category, bs.style, bs.biome_match,
               sa.semantic_text, sa.primary_tiles, sa.primary_walls
        FROM building_schematics bs
        LEFT JOIN schematic_analysis sa ON bs.id = sa.schematic_id
    """)
    schematics = cursor.fetchall()

    if not schematics:
        print("没有蓝图数据，请先导入蓝图")
        conn.close()
        return

    documents = []
    for sch in schematics:
        sch_id, name, width, height, category, style, biome_match, semantic_text, primary_tiles, primary_walls = sch
        text_parts = [name, f"{width}x{height}", semantic_text or ""]
        if category: text_parts.append(category)
        if style: text_parts.append(style)
        if biome_match: text_parts.append(biome_match)
        documents.append(" ".join(text_parts))

    vectorizer = SchematicVectorizer()
    vectorizer.fit(documents)

    print("\n=== 生成蓝图向量 ===")
    embeddings = {}

    for sch in schematics:
        sch_id, name, width, height, category, style, biome_match, semantic_text, primary_tiles, primary_walls = sch

        text_parts = [name, f"尺寸{width}x{height}", semantic_text or ""]
        if category: text_parts.append(category)
        if style: text_parts.append(style)
        if biome_match: text_parts.append(biome_match)

        if primary_tiles:
            try:
                tiles_list = json.loads(primary_tiles)
                for tile_id, count in tiles_list[:5]:
                    cursor.execute("SELECT display_name, category FROM tiles WHERE id = ?", (tile_id,))
                    row = cursor.fetchone()
                    if row:
                        text_parts.append(f"{row[0]}{count}个")
                        if row[1]: text_parts.append(row[1])
            except: pass

        if primary_walls:
            try:
                walls_list = json.loads(primary_walls)
                for wall_id, count in walls_list[:3]:
                    cursor.execute("SELECT display_name FROM walls WHERE id = ?", (wall_id,))
                    row = cursor.fetchone()
                    if row: text_parts.append(f"{row[0]}墙{count}个")
            except: pass

        text = " ".join([p for p in text_parts if p])
        vector = vectorizer.vectorize(text)
        embeddings[sch_id] = vector

        print(f"  ID {sch_id}: {name} ({width}x{height})")

    conn.close()

    output_path = os.path.join(DATA_DIR, "schematic_embeddings.json")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump({
            "version": "3.0",
            "method": "schematic_semantic",
            "dimension": VECTOR_DIM,
            "generated": datetime.now().isoformat(),
            "count": len(embeddings),
            "embeddings": {str(k): v for k, v in embeddings.items()}
        }, f, indent=2)

    print(f"\n  生成 {len(embeddings)} 个蓝图向量")
    print(f"  输出: {output_path}")

    return embeddings


def test_schematic_search(embeddings):
    """测试蓝图检索"""
    print("\n=== 测试蓝图语义检索 ===")

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    vectorizer = SchematicVectorizer()

    def cosine_sim(v1, v2):
        dot = sum(a * b for a, b in zip(v1, v2))
        mag1 = math.sqrt(sum(a * a for a in v1))
        mag2 = math.sqrt(sum(b * b for b in v2))
        return dot / (mag1 * mag2) if mag1 > 0 and mag2 > 0 else 0

    test_queries = ["小型房屋 木制", "中世纪城堡 石砖", "自然风格 木头"]

    for query in test_queries:
        print(f"\n查询: '{query}'")
        query_vec = vectorizer.vectorize(query)

        results = []
        for sch_id_str, sch_vec in embeddings.items():
            sch_id = int(sch_id_str)
            sim = cosine_sim(query_vec, sch_vec)
            cursor.execute("SELECT name, width, height, style FROM building_schematics WHERE id = ?", (sch_id,))
            row = cursor.fetchone()
            if row:
                results.append((row[0], row[1], row[2], row[3], sim))

        results.sort(key=lambda x: x[4], reverse=True)
        for name, w, h, style, sim in results[:3]:
            print(f"    {name} ({w}x{h}, {style or '未知'}): {sim:.3f}")

    conn.close()


if __name__ == "__main__":
    embeddings = generate_schematic_embeddings()
    if embeddings:
        test_schematic_search(embeddings)
    print("\n完成!")