#!/usr/bin/env python3
"""
泰拉瑞亚方块向量生成脚本
使用SentenceTransformers生成384维embedding向量
"""

import json
import sqlite3
from pathlib import Path

# 尝试导入sentence-transformers
try:
    from sentence_transformers import SentenceTransformer
    HAS_ST = True
except ImportError:
    HAS_ST = False
    print("警告: sentence-transformers未安装，将使用预定义向量")

# 配置
OUTPUT_DIR = Path(__file__).parent.parent / "Data"
DB_PATH = OUTPUT_DIR / "terraria_kb.db"
EMBEDDINGS_PATH = OUTPUT_DIR / "tile_embeddings.json"
STYLE_EMBEDDINGS_PATH = OUTPUT_DIR / "style_embeddings.json"

# 预定义风格向量 (简化版，实际应使用模型生成)
PREDEFINED_STYLE_VECTORS = {
    "medieval": "medieval castle knight stone brick fortress ancient europe",
    "fantasy": "fantasy magic elf fairy enchanted mystical sparkle crystal",
    "natural": "natural wood forest tree grass organic rustic cabin",
    "steampunk": "steampunk industrial copper brass gear mechanical victorian",
    "asian": "asian japanese chinese temple pagoda dynasty bamboo oriental",
    "snow": "snow ice winter cold frozen arctic igloo frost white",
    "desert": "desert sand egypt pyramid sandstone arid dry hot ancient",
    "modern": "modern contemporary sleek glass steel urban clean minimalist",
    "dark": "dark evil corruption crimson void shadow hell obsidian demon"
}

def generate_embedding_text(tile: dict) -> str:
    """
    为tile生成用于embedding的文本
    将name、category、styles等信息合并为语义丰富的描述
    """
    parts = []

    # 名称
    if tile.get("name"):
        parts.append(tile["name"])
    if tile.get("display_name"):
        parts.append(tile["display_name"])

    # 类别
    if tile.get("category"):
        parts.append(tile["category"])

    # 风格标签
    if tile.get("styles"):
        styles = tile["styles"]
        if isinstance(styles, str):
            parts.append(styles)
        elif isinstance(styles, list):
            parts.extend(styles)

    # 生物群落
    if tile.get("biome_match"):
        biome = tile["biome_match"]
        if isinstance(biome, str):
            parts.append(biome)
        elif isinstance(biome, list):
            parts.extend(biome)

    # 描述
    if tile.get("description"):
        parts.append(tile["description"])

    return " ".join(parts)

def generate_with_model(texts: list, model_name: str = "all-MiniLM-L6-v2") -> list:
    """使用SentenceTransformers模型生成向量"""
    if not HAS_ST:
        return None

    print(f"加载模型: {model_name}")
    model = SentenceTransformer(model_name)

    print(f"生成 {len(texts)} 个向量...")
    embeddings = model.encode(texts, show_progress_bar=True)

    return [emb.tolist() for emb in embeddings]

def generate_simple_vector(text: str, dim: int = 384) -> list:
    """
    简化向量生成：基于关键词哈希
    当没有模型时使用此方法
    """
    import hashlib

    vector = [0.0] * dim
    words = text.lower().split()

    for word in words:
        # 使用词的哈希值确定向量位置
        h = int(hashlib.md5(word.encode()).hexdigest(), 16)
        idx = h % dim
        vector[idx] += 1.0 / len(words)

    # 归一化
    mag = sum(x*x for x in vector) ** 0.5
    if mag > 0:
        vector = [x/mag for x in vector]

    return vector

def load_tiles_from_db() -> list:
    """从SQLite加载tiles数据"""
    if not DB_PATH.exists():
        print(f"数据库不存在: {DB_PATH}")
        return []

    conn = sqlite3.connect(str(DB_PATH))
    cursor = conn.cursor()

    try:
        cursor.execute("SELECT id, name, display_name, category, styles, biome_match, description FROM tiles")
        rows = cursor.fetchall()

        tiles = []
        for row in rows:
            tile = {
                "id": row[0],
                "name": row[1],
                "display_name": row[2],
                "category": row[3],
                "styles": row[4] if row[4] else [],
                "biome_match": row[5] if row[5] else [],
                "description": row[6]
            }
            tiles.append(tile)

        return tiles
    except sqlite3.OperationalError as e:
        print(f"SQL错误: {e}")
        return []
    finally:
        conn.close()

def load_tiles_from_json() -> list:
    """从JSON加载tiles数据（备用）"""
    wiki_path = OUTPUT_DIR / "Crawled" / "wiki_data.json"
    if wiki_path.exists():
        with open(wiki_path, 'r', encoding='utf-8') as f:
            data = json.load(f)
            return data.get("tiles", [])
    return []

def main():
    print("=" * 50)
    print("泰拉瑞亚向量生成脚本")
    print("=" * 50)

    # 加载tiles
    tiles = load_tiles_from_db()
    if not tiles:
        print("从数据库加载失败，尝试JSON...")
        tiles = load_tiles_from_json()

    if not tiles:
        print("错误: 无法加载tiles数据")
        return

    print(f"加载了 {len(tiles)} 个tiles")

    # 生成embedding文本
    texts = [generate_embedding_text(t) for t in tiles]

    # 生成向量
    if HAS_ST:
        embeddings = generate_with_model(texts)
    else:
        print("使用简化向量生成...")
        embeddings = [generate_simple_vector(t) for t in texts]

    # 构建输出数据
    output = []
    for tile, emb in zip(tiles, embeddings):
        output.append({
            "tile_id": tile["id"],
            "name": tile["name"],
            "text": generate_embedding_text(tile),
            "embedding": emb
        })

    # 保存
    with open(EMBEDDINGS_PATH, 'w', encoding='utf-8') as f:
        json.dump(output, f, indent=2)

    print(f"保存向量到: {EMBEDDINGS_PATH}")

    # 生成风格向量
    style_output = []
    for style, text in PREDEFINED_STYLE_VECTORS.items():
        if HAS_ST:
            emb = generate_with_model([text])[0]
        else:
            emb = generate_simple_vector(text)

        style_output.append({
            "style": style,
            "text": text,
            "embedding": emb
        })

    with open(STYLE_EMBEDDINGS_PATH, 'w', encoding='utf-8') as f:
        json.dump(style_output, f, indent=2)

    print(f"保存风格向量到: {STYLE_EMBEDDINGS_PATH}")
    print("完成!")

if __name__ == "__main__":
    main()