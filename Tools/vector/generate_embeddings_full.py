#!/usr/bin/env python3
"""
泰拉瑞亚向量生成脚本
使用SentenceTransformers生成384维embedding向量
支持: 方块(tiles)、墙壁(walls)、家具(furniture)、风格(styles)
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
    print("警告: sentence-transformers未安装，将使用简化向量")

# 配置
OUTPUT_DIR = Path(__file__).parent.parent / "Data" / "vectors"
DB_PATH = Path(__file__).parent.parent / "Data" / "kb" / "terraria_kb.db"

# 输出路径
TILE_EMBEDDINGS_PATH = OUTPUT_DIR / "tile_embeddings.json"
WALL_EMBEDDINGS_PATH = OUTPUT_DIR / "wall_embeddings.json"
FURNITURE_EMBEDDINGS_PATH = OUTPUT_DIR / "furniture_embeddings.json"
STYLE_EMBEDDINGS_PATH = OUTPUT_DIR / "style_embeddings.json"

# 预定义风格向量
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

# 预定义家具类别向量
PREDEFINED_FURNITURE_VECTORS = {
    "light": "light lamp torch candle lantern illumination brightness glow",
    "surface": "table desk bench counter flat work surface crafting station",
    "comfort": "chair bed sofa couch seat rest relax comfort furniture",
    "storage": "chest barrel crate box container safe locker inventory",
    "door": "door gate entrance exit portal passage entry",
    "decoration": "painting statue banner flag ornament decorative aesthetic"
}

def generate_embedding_text(item: dict, item_type: str = "tile") -> str:
    """
    生成用于embedding的文本
    """
    parts = []

    # 名称
    if item.get("name"):
        parts.append(item["name"])
    if item.get("display_name"):
        parts.append(item["display_name"])

    # 类别
    if item.get("category"):
        parts.append(item["category"])

    # 类型标识
    parts.append(item_type)

    # 风格标签
    if item.get("styles"):
        styles = item["styles"]
        if isinstance(styles, str):
            parts.append(styles)
        elif isinstance(styles, list):
            parts.extend(styles)

    # 生物群落
    if item.get("biome_match"):
        biome = item["biome_match"]
        if isinstance(biome, str):
            parts.append(biome)
        elif isinstance(biome, list):
            parts.extend(biome)

    # NPC功能 (家具)
    if item.get("npc_function"):
        parts.append(item["npc_function"])

    # 描述
    if item.get("description"):
        parts.append(item["description"])

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
    """简化向量生成：基于关键词哈希"""
    import hashlib

    vector = [0.0] * dim
    words = text.lower().split()

    for word in words:
        h = int(hashlib.md5(word.encode()).hexdigest(), 16)
        idx = h % dim
        vector[idx] += 1.0 / len(words)

    # 归一化
    mag = sum(x*x for x in vector) ** 0.5
    if mag > 0:
        vector = [x/mag for x in vector]

    return vector

def load_from_db(table: str) -> list:
    """从SQLite加载数据"""
    if not DB_PATH.exists():
        print(f"数据库不存在: {DB_PATH}")
        return []

    conn = sqlite3.connect(str(DB_PATH))
    cursor = conn.cursor()

    try:
        if table == "tiles":
            cursor.execute("SELECT id, name, display_name, category, styles, biome_match, description FROM tiles")
        elif table == "walls":
            cursor.execute("SELECT id, name, display_name, category, styles, biome_match, description FROM walls")
        elif table == "furniture":
            cursor.execute("SELECT id, name, display_name, category, styles, npc_function, description FROM furniture")
        else:
            return []

        rows = cursor.fetchall()
        items = []
        for row in rows:
            item = {
                "id": row[0],
                "name": row[1],
                "display_name": row[2],
                "category": row[3],
                "styles": row[4] if row[4] else [],
                "description": row[-1]
            }
            if table == "tiles" or table == "walls":
                item["biome_match"] = row[5] if row[5] else []
            if table == "furniture":
                item["npc_function"] = row[5] if row[5] else ""
            items.append(item)

        return items
    except sqlite3.OperationalError as e:
        print(f"SQL错误({table}): {e}")
        return []
    finally:
        conn.close()

def load_default_walls() -> list:
    """加载默认墙壁数据"""
    return [
        {"id": 1, "name": "StoneWall", "display_name": "石墙", "category": "natural", "styles": ["medieval", "natural"]},
        {"id": 4, "name": "WoodWall", "display_name": "木墙", "category": "wood", "styles": ["natural", "medieval"]},
        {"id": 6, "name": "GrayBrickWall", "display_name": "灰砖墙", "category": "brick", "styles": ["medieval", "castle"]},
        {"id": 15, "name": "GoldBrickWall", "display_name": "金砖墙", "category": "luxury", "styles": ["luxury"]},
        {"id": 16, "name": "SandstoneWall", "display_name": "砂岩墙", "category": "desert", "styles": ["desert"]},
        {"id": 17, "name": "SnowWall", "display_name": "雪墙", "category": "snow", "styles": ["snow"]},
    ]

def load_default_furniture() -> list:
    """加载默认家具数据"""
    return [
        {"id": 17, "name": "WorkBench", "display_name": "工作台", "category": "surface", "styles": ["any"], "npc_function": "crafting"},
        {"id": 87, "name": "Tables", "display_name": "桌子", "category": "surface", "styles": ["any"], "npc_function": "flat_surface"},
        {"id": 88, "name": "Chairs", "display_name": "椅子", "category": "comfort", "styles": ["any"], "npc_function": "comfort"},
        {"id": 89, "name": "Beds", "display_name": "床", "category": "comfort", "styles": ["any"], "npc_function": "comfort"},
        {"id": 21, "name": "Chests", "display_name": "宝箱", "category": "storage", "styles": ["any"], "npc_function": "storage"},
        {"id": 4, "name": "Torches", "display_name": "火把", "category": "light", "styles": ["any"], "npc_function": "light_source"},
        {"id": 10, "name": "ClosedDoor", "display_name": "门", "category": "door", "styles": ["any"], "npc_function": "door"},
    ]

def generate_embeddings(items: list, item_type: str) -> list:
    """为物品列表生成向量"""
    texts = [generate_embedding_text(item, item_type) for item in items]

    if HAS_ST:
        embeddings = generate_with_model(texts)
    else:
        embeddings = [generate_simple_vector(t) for t in texts]

    output = []
    for item, emb in zip(items, embeddings):
        output.append({
            f"{item_type}_id": item["id"],
            "name": item["name"],
            "display_name": item.get("display_name", ""),
            "category": item.get("category", ""),
            "text": generate_embedding_text(item, item_type),
            "embedding": emb
        })

    return output

def main():
    print("=" * 50)
    print("泰拉瑞亚向量生成脚本 (完整版)")
    print("=" * 50)

    # 1. 方块向量
    print("\n[1] 生成方块向量...")
    tiles = load_from_db("tiles")
    if not tiles:
        print("数据库无tiles，使用wiki_data.json...")
        wiki_path = OUTPUT_DIR / "Crawled" / "wiki_data.json"
        if wiki_path.exists():
            with open(wiki_path, 'r') as f:
                data = json.load(f)
                tiles = data.get("tiles", [])[:100]  # 取前100个

    if tiles:
        tile_output = generate_embeddings(tiles, "tile")
        with open(TILE_EMBEDDINGS_PATH, 'w', encoding='utf-8') as f:
            json.dump(tile_output, f, indent=2)
        print(f"保存方块向量: {len(tile_output)} 个 → {TILE_EMBEDDINGS_PATH}")

    # 2. 墙壁向量
    print("\n[2] 生成墙壁向量...")
    walls = load_from_db("walls")
    if not walls:
        print("数据库无walls，使用默认数据...")
        walls = load_default_walls()

    if walls:
        wall_output = generate_embeddings(walls, "wall")
        with open(WALL_EMBEDDINGS_PATH, 'w', encoding='utf-8') as f:
            json.dump(wall_output, f, indent=2)
        print(f"保存墙壁向量: {len(wall_output)} 个 → {WALL_EMBEDDINGS_PATH}")

    # 3. 家具向量
    print("\n[3] 生成家具向量...")
    furniture = load_from_db("furniture")
    if not furniture:
        print("数据库无furniture，使用默认数据...")
        furniture = load_default_furniture()

    if furniture:
        furniture_output = generate_embeddings(furniture, "furniture")
        with open(FURNITURE_EMBEDDINGS_PATH, 'w', encoding='utf-8') as f:
            json.dump(furniture_output, f, indent=2)
        print(f"保存家具向量: {len(furniture_output)} 个 → {FURNITURE_EMBEDDINGS_PATH}")

    # 4. 风格向量
    print("\n[4] 生成风格向量...")
    style_output = []
    for style, text in PREDEFINED_STYLE_VECTORS.items():
        if HAS_ST:
            emb = generate_with_model([text])[0]
        else:
            emb = generate_simple_vector(text)
        style_output.append({"style": style, "text": text, "embedding": emb})

    with open(STYLE_EMBEDDINGS_PATH, 'w', encoding='utf-8') as f:
        json.dump(style_output, f, indent=2)
    print(f"保存风格向量: {len(style_output)} 个 → {STYLE_EMBEDDINGS_PATH}")

    # 5. 家具类别向量
    print("\n[5] 生成家具类别向量...")
    furniture_category_output = []
    for category, text in PREDEFINED_FURNITURE_VECTORS.items():
        if HAS_ST:
            emb = generate_with_model([text])[0]
        else:
            emb = generate_simple_vector(text)
        furniture_category_output.append({"category": category, "text": text, "embedding": emb})

    # 合并到家具向量文件
    furniture_data = []
    if FURNITURE_EMBEDDINGS_PATH.exists():
        with open(FURNITURE_EMBEDDINGS_PATH, 'r') as f:
            furniture_data = json.load(f)
    furniture_data.extend([{"furniture_category": item["category"], **item} for item in furniture_category_output])
    with open(FURNITURE_EMBEDDINGS_PATH, 'w', encoding='utf-8') as f:
        json.dump(furniture_data, f, indent=2)

    print("=" * 50)
    print("完成!")
    print(f"方块向量: {TILE_EMBEDDINGS_PATH}")
    print(f"墙壁向量: {WALL_EMBEDDINGS_PATH}")
    print(f"家具向量: {FURNITURE_EMBEDDINGS_PATH}")
    print(f"风格向量: {STYLE_EMBEDDINGS_PATH}")

if __name__ == "__main__":
    main()