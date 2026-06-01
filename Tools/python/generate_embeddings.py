#!/usr/bin/env python3
"""
向量库生成脚本
为方块、风格、生物群落生成向量嵌入
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

# 预定义风格关键词
STYLE_KEYWORDS = {
    "medieval": "medieval castle knight stone brick fortress gray brick wood torch banner flag stone slab",
    "fantasy": "fantasy magic elf fairy enchanted mystical pearlstone glass gold divine pearl holy rainbow",
    "natural": "natural wood forest tree grass organic dirt living wood rustic wild plant flower",
    "steampunk": "steampunk industrial copper brass gear iron brick mechanical cog metal pipe factory",
    "asian": "asian japanese chinese temple pagoda bamboo dynasty wood lantern paper tea eastern oriental",
    "snow": "snow ice winter cold frozen arctic frost boreal wood ice block white blue crystal",
    "desert": "desert sand egypt pyramid sandstone arid palm wood sandstone slab hot dry golden",
    "underground": "underground cavern cave stone dirt torch dark shadow moss gem crystal mineral",
    "ocean": "ocean sea beach tropical palm wood glass coral blue water fish shell wave",
    "modern": "modern contemporary sleek glass granite marble steel urban clean white gray minimalist",
}

# 生物群落关键词
BIOME_KEYWORDS = {
    "forest": "forest grass wood tree stone green natural leaf plant wildlife bird",
    "desert": "desert sand sandstone dry hot arid cactus palm scorpion vulture oasis",
    "snow": "snow ice cold winter freeze blizzard frost polar arctic boreal tundra",
    "jungle": "jungle tropical rich mahogany mud plant vine fruit tiger monkey parrot",
    "ocean": "ocean water coral fish palm beach tropical wave shell dolphin whale",
    "underground": "underground cavern cave dark stone dirt mineral gem crystal bat",
    "cavern": "cavern deep stone marble granite obsidian crystal stalactite mushroom",
    "hallow": "hallow pearlstone pearlwood divine rainbow fantasy light pink unicorn pixie",
    "corruption": "corruption ebonstone dark purple evil shadow chasm eater chaos rift",
    "crimson": "crimson crimstone blood red flesh gore brain heart vein ichor",
    "glowing_mushroom": "glowing mushroom blue fungi truffle spore mycelium glow illuminate",
    "dungeon": "dungeon brick blue green pink locked chain skeleton undead curse",
    "hell": "hell obsidian hellstone ash fire lava demon imp brimstone inferno",
}


def normalize(vector):
    """归一化向量"""
    mag = math.sqrt(sum(v * v for v in vector))
    if mag == 0:
        return vector
    return [v / mag for v in vector]


def generate_simple_vector(text, dim=VECTOR_DIM):
    """使用关键词哈希生成简化向量"""
    vector = [0.0] * dim
    words = text.lower().split()

    for word in words:
        # 使用哈希值作为索引
        idx = abs(hash(word)) % dim
        vector[idx] += 1.0

        # 添加相邻词的关联
        idx2 = abs(hash(word + word[::-1])) % dim
        vector[idx2] += 0.5

        # 词长度影响
        idx3 = abs(hash(str(len(word)))) % dim
        vector[idx3] += 0.3

    # 归一化
    return normalize(vector)


def cosine_similarity(v1, v2):
    """计算余弦相似度"""
    dot = sum(a * b for a, b in zip(v1, v2))
    mag1 = math.sqrt(sum(a * a for a in v1))
    mag2 = math.sqrt(sum(b * b for b in v2))
    if mag1 == 0 or mag2 == 0:
        return 0.0
    return dot / (mag1 * mag2)


def generate_tile_embeddings():
    """从数据库生成方块向量"""
    print("\n=== 生成方块向量 ===")

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    cursor.execute("SELECT id, name, display_name, category, styles, biome_match, description FROM tiles")
    tiles = cursor.fetchall()

    embeddings = {}

    for tile in tiles:
        tile_id, name, display_name, category, styles, biome_match, description = tile

        # 组合文本
        text_parts = [name, display_name, category, description or ""]

        # 解析JSON字段
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

        text = " ".join([p for p in text_parts if p])
        vector = generate_simple_vector(text)
        embeddings[tile_id] = vector

    conn.close()

    # 保存
    output_path = os.path.join(DATA_DIR, "tile_embeddings.json")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump({
            "version": "2.0",
            "dimension": VECTOR_DIM,
            "generated": datetime.now().isoformat(),
            "count": len(embeddings),
            "embeddings": {str(k): v for k, v in embeddings.items()}
        }, f, indent=2)

    print(f"  生成 {len(embeddings)} 个方块向量")
    print(f"  输出: {output_path}")

    return embeddings


def generate_style_embeddings():
    """生成风格向量"""
    print("\n=== 生成风格向量 ===")

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    cursor.execute("SELECT name, display_name, description, primary_tiles, primary_walls, paint_scheme FROM style_templates")
    styles = cursor.fetchall()

    conn.close()

    embeddings = {}

    for style in styles:
        name, display_name, description, primary_tiles, primary_walls, paint_scheme = style

        # 组合文本
        text_parts = [name, display_name, description or ""]

        # 添加预定义关键词（增强匹配）
        if name in STYLE_KEYWORDS:
            text_parts.append(STYLE_KEYWORDS[name])

        # 解析JSON字段
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
        vector = generate_simple_vector(text)
        embeddings[name] = vector

    # 保存
    output_path = os.path.join(DATA_DIR, "style_embeddings.json")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump({
            "version": "2.0",
            "dimension": VECTOR_DIM,
            "generated": datetime.now().isoformat(),
            "count": len(embeddings),
            "embeddings": embeddings
        }, f, indent=2)

    print(f"  生成 {len(embeddings)} 个风格向量")
    print(f"  输出: {output_path}")

    return embeddings


def generate_biome_embeddings():
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

        # 组合文本
        text_parts = [name, display_name, description or ""]

        # 添加预定义关键词
        if name in BIOME_KEYWORDS:
            text_parts.append(BIOME_KEYWORDS[name])

        # 解析JSON字段
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
        vector = generate_simple_vector(text)
        embeddings[name] = vector

    # 保存
    output_path = os.path.join(DATA_DIR, "biome_embeddings.json")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump({
            "version": "2.0",
            "dimension": VECTOR_DIM,
            "generated": datetime.now().isoformat(),
            "count": len(embeddings),
            "embeddings": embeddings
        }, f, indent=2)

    print(f"  生成 {len(embeddings)} 个生物群落向量")
    print(f"  输出: {output_path}")

    return embeddings


def test_similarity(tile_embeddings, style_embeddings):
    """测试相似度匹配"""
    print("\n=== 测试相似度匹配 ===")

    # 测试风格匹配
    test_cases = [
        ("medieval", "brick"),
        ("fantasy", "luxury"),
        ("snow", "natural"),
        ("desert", "brick"),
    ]

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    for style, category in test_cases:
        if style not in style_embeddings:
            continue

        style_vector = style_embeddings[style]

        # 获取该分类的方块
        cursor.execute("SELECT id, name FROM tiles WHERE category = ?", (category,))
        tiles = cursor.fetchall()

        # 计算相似度排序
        results = []
        for tile_id, name in tiles:
            if tile_id in tile_embeddings:
                sim = cosine_similarity(style_vector, tile_embeddings[tile_id])
                results.append((name, sim))

        results.sort(key=lambda x: x[1], reverse=True)

        print(f"\n  风格 '{style}' + 分类 '{category}' 匹配结果:")
        for name, sim in results[:5]:
            print(f"    {name}: {sim:.3f}")

    conn.close()


def update_sql_file():
    """更新SQL文件包含向量数据"""
    print("\n=== 更新SQL文件 ===")

    # 在SQL文件中添加向量数据注释
    sql_path = os.path.join(DATA_DIR, "terraria_kb_full.sql")

    with open(sql_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # 在文件开头添加向量文件说明
    header = """-- Terraria 建筑知识库数据库 (完整版)
-- 更新时间: {}
--
-- 向量数据文件:
--   tile_embeddings.json    - 方块向量 (384维)
--   style_embeddings.json   - 风格向量 (384维)
--   biome_embeddings.json   - 生物群落向量 (384维)
--
-- 向量生成: python Tools/python/generate_embeddings.py

""".format(datetime.now().isoformat())

    # 替换原有头部
    lines = content.split('\n')
    start_idx = 0
    for i, line in enumerate(lines):
        if line.startswith('-- === tiles ==='):
            start_idx = i
            break

    new_content = header + '\n'.join(lines[start_idx:])

    with open(sql_path, 'w', encoding='utf-8') as f:
        f.write(new_content)

    print(f"  更新: {sql_path}")


def main():
    print("=" * 50)
    print("向量库生成脚本")
    print("=" * 50)

    # 生成向量
    tile_embeddings = generate_tile_embeddings()
    style_embeddings = generate_style_embeddings()
    biome_embeddings = generate_biome_embeddings()

    # 测试相似度
    test_similarity(tile_embeddings, style_embeddings)

    # 更新SQL文件
    update_sql_file()

    print("\n" + "=" * 50)
    print("完成!")
    print(f"向量文件目录: {DATA_DIR}")
    print("  - tile_embeddings.json")
    print("  - style_embeddings.json")
    print("  - biome_embeddings.json")
    print("=" * 50)


if __name__ == "__main__":
    main()