#!/usr/bin/env python3
"""
建筑实体向量生成脚本 v3
为构件级建筑数据库(building_index)生成向量嵌入
"""

import json
import sqlite3
import math
import os
import sys
from datetime import datetime

sys.stdout.reconfigure(encoding='utf-8')

DATA_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Data", "vectors")
KB_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Data", "kb")
DB_PATH = os.path.join(KB_DIR, "terraria_kb.db")
VECTOR_DIM = 384

# 建筑类型关键词
BUILDING_TYPE_KEYWORDS = {
    "住宅": "house home residential living dwelling shelter npc room bedroom kitchen",
    "神庙": "temple shrine religious sacred worship altar deity spiritual meditation",
    "商店": "shop store merchant trading market commerce business retail",
    "塔楼": "tower watchtower lookout vertical tall tower spire",
    "城堡": "castle fortress stronghold medieval defense battlement",
    "农场": "farm barn agricultural crops harvest ranch farm",
}

# 风格关键词
STYLE_KEYWORDS = {
    "中式": "chinese asian oriental dynasty pagoda temple bamboo lantern gold red traditional eastern",
    "日式": "japanese asian oriental sakura cherry bamboo shrine temple zen tatami paper lantern",
    "奇幻": "fantasy magical enchanted mystical pearlstone crystal divine rainbow ethereal",
    "中世纪": "medieval castle knight stone brick fortress gray torch banner ancient",
    "现代": "modern contemporary glass steel granite sleek urban minimalist clean",
    "自然": "natural organic wood forest tree grass dirt rustic wild living",
}

# 尺寸关键词
SIZE_KEYWORDS = {
    "小型": "small tiny compact mini simple basic",
    "中型": "medium moderate average standard",
    "大型": "large big huge massive extensive complex",
}


class BuildingVectorizer:
    """建筑向量生成器"""

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
        """分词 - 支持中英文"""
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

        # TF-IDF权重
        for word in words:
            idx = abs(hash(word)) % dim
            weight = self._get_weight(word)
            vector[idx] += weight

        # 语义关键词增强
        text_lower = text.lower()
        for key, kw_text in BUILDING_TYPE_KEYWORDS.items():
            if key in text or key.lower() in text_lower:
                for kw in kw_text.split():
                    idx = abs(hash(kw)) % dim
                    vector[idx] += 2.5

        for key, kw_text in STYLE_KEYWORDS.items():
            if key in text or key.lower() in text_lower:
                for kw in kw_text.split():
                    idx = abs(hash(kw)) % dim
                    vector[idx] += 3.0

        for key, kw_text in SIZE_KEYWORDS.items():
            if key in text or key.lower() in text_lower:
                for kw in kw_text.split():
                    idx = abs(hash(kw)) % dim
                    vector[idx] += 1.5

        # 归一化
        mag = math.sqrt(sum(v * v for v in vector))
        if mag > 0:
            vector = [v / mag for v in vector]

        return vector

    def _get_weight(self, word):
        """获取词权重"""
        if word in self.doc_count:
            return math.log(self.total_docs / (self.doc_count[word] + 1)) + 1
        return 1.0


def generate_building_embeddings():
    """生成建筑实体向量"""
    print("=" * 60)
    print("建筑实体向量生成脚本 v3")
    print("=" * 60)

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    # 获取所有建筑索引数据
    cursor.execute("""
        SELECT bi.id, bi.name, bi.complexity_level, bi.building_type,
               bi.style, bi.size_category, bi.summary,
               b.width, b.height, b.stories, b.style_tags
        FROM building_index bi
        LEFT JOIN buildings b ON bi.id = b.id
    """)
    buildings = cursor.fetchall()

    if not buildings:
        print("没有建筑数据")
        conn.close()
        return

    print(f"\n加载建筑数据: {len(buildings)} 条")

    # 收集文本用于训练
    documents = []
    for b in buildings:
        (id_, name, level, btype, style, size, summary,
         width, height, stories, style_tags) = b
        text_parts = [name or "", btype or "", style or "", size or "", summary or ""]
        if style_tags:
            try:
                tags = json.loads(style_tags)
                text_parts.extend(tags)
            except:
                pass
        documents.append(" ".join([p for p in text_parts if p]))

    vectorizer = BuildingVectorizer()
    vectorizer.fit(documents)

    # 生成向量
    print("\n=== 生成建筑向量 ===")
    embeddings = {}
    vector_records = []

    for b in buildings:
        (id_, name, level, btype, style, size, summary,
         width, height, stories, style_tags) = b

        # 构建向量文本
        text_parts = [
            name or "",
            btype or "",
            style or "",
            size or "",
            summary or "",
            f"尺寸{width or 0}x{height or 0}",
        ]
        if stories:
            text_parts.append(f"{stories}层")
        if style_tags:
            try:
                tags = json.loads(style_tags)
                text_parts.extend(tags)
            except:
                pass

        # 从atomic_components获取构件信息
        cursor.execute("""
            SELECT type, subtype, tile_count
            FROM atomic_components
            WHERE source_building = ?
        """, (id_,))
        components = cursor.fetchall()
        for comp_type, comp_subtype, tile_count in components[:10]:
            text_parts.append(comp_type or "")
            if comp_subtype:
                text_parts.append(comp_subtype)
            if tile_count:
                text_parts.append(f"{comp_type}{tile_count}块")

        text = " ".join([p for p in text_parts if p])
        vector = vectorizer.vectorize(text)
        embeddings[id_] = vector

        # 准备数据库记录
        vector_records.append({
            "id": id_,
            "vector": vector,
            "searchable_text": text
        })

        print(f"  {id_}: {btype or '未知'} {style or ''} {size or ''}")

    # 更新数据库向量表
    print("\n=== 更新数据库向量表 ===")
    for record in vector_records:
        cursor.execute("""
            UPDATE vectors SET
            vector = ?,
            searchable_text = ?
            WHERE id = ?
        """, (
            json.dumps(record["vector"]),
            record["searchable_text"],
            record["id"]
        ))

        # 如果不存在则插入
        if cursor.rowcount == 0:
            cursor.execute("""
                INSERT INTO vectors (id, entity_type, entity_level, vector, vector_model, vector_dimension, searchable_text)
                VALUES (?, 'building', '2', ?, 'smart_keyword', 384, ?)
            """, (
                record["id"],
                json.dumps(record["vector"]),
                record["searchable_text"]
            ))

    # 同时更新building_index表的vector字段
    for record in vector_records:
        cursor.execute("""
            UPDATE building_index SET vector = ? WHERE id = ?
        """, (json.dumps(record["vector"]), record["id"]))

    conn.commit()
    print(f"更新 {len(vector_records)} 条向量记录")

    conn.close()

    # 保存JSON文件
    output_path = os.path.join(DATA_DIR, "building_embeddings.json")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump({
            "version": "3.0",
            "method": "building_semantic_v3",
            "dimension": VECTOR_DIM,
            "generated": datetime.now().isoformat(),
            "count": len(embeddings),
            "embeddings": {k: v for k, v in embeddings.items()}
        }, f, indent=2)

    print(f"\n向量文件: {output_path}")

    return embeddings


def test_building_search():
    """测试建筑语义检索"""
    print("\n=== 测试建筑语义检索 ===")

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    vectorizer = BuildingVectorizer()

    def cosine_sim(v1, v2):
        dot = sum(a * b for a, b in zip(v1, v2))
        mag1 = math.sqrt(sum(a * a for a in v1))
        mag2 = math.sqrt(sum(b * b for b in v2))
        return dot / (mag1 * mag2) if mag1 > 0 and mag2 > 0 else 0

    test_queries = [
        ("中式住宅 小型", "住宅", "中式"),
        ("大型神庙 塔楼", "神庙", None),
        ("奇幻风格 建筑", None, "奇幻"),
        ("日式风格 小型住宅", "住宅", "日式"),
    ]

    cursor.execute("SELECT id, vector FROM vectors WHERE entity_type = 'building'")
    vectors_data = {row[0]: json.loads(row[1]) for row in cursor.fetchall()}

    for query, expected_type, expected_style in test_queries:
        print(f"\n查询: '{query}'")
        query_vec = vectorizer.vectorize(query)

        results = []
        for building_id, building_vec in vectors_data.items():
            sim = cosine_sim(query_vec, building_vec)

            cursor.execute("""
                SELECT bi.name, bi.building_type, bi.style, bi.size_category
                FROM building_index bi WHERE bi.id = ?
            """, (building_id,))
            row = cursor.fetchone()
            if row:
                results.append((row[0], row[1], row[2], row[3], sim))

        results.sort(key=lambda x: x[4], reverse=True)
        print("  Top-5:")
        for name, btype, style, size, sim in results[:5]:
            type_match = "✓" if expected_type and btype == expected_type else ""
            style_match = "✓" if expected_style and style == expected_style else ""
            print(f"    {name[:30]} | {btype} | {style} | {size or '未知'} | {sim:.3f} {type_match}{style_match}")

    conn.close()


if __name__ == "__main__":
    embeddings = generate_building_embeddings()
    test_building_search()
    print("\n完成!")