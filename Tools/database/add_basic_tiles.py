#!/usr/bin/env python3
"""
补充基础方块和墙壁数据
由于网络问题无法爬取，手动添加常用建筑方块数据
"""

import sqlite3
import json
import os
import sys
from datetime import datetime

sys.stdout.reconfigure(encoding='utf-8')

DATA_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Data", "kb")
DB_PATH = os.path.join(DATA_DIR, "terraria_kb.db")
SQL_PATH = os.path.join(DATA_DIR, "terraria_kb_tiles.sql")


def insert_basic_tiles(cursor):
    """插入基础方块数据"""
    tiles = [
        # 基础方块
        (1, 'StoneBlock', '石头', 'brick', '["medieval","natural","underground"]', '["forest","underground"]', 1, 1, 50, 0, '基础建筑材料'),
        (2, 'DirtBlock', '泥土', 'natural', '["natural","underground"]', '["forest","underground"]', 1, 1, 30, 0, '自然建筑材料'),
        (3, 'Grass', '草地', 'natural', '["natural"]', '["forest"]', 0, 0, 30, 0, '地表草地'),
        (5, 'Wood', '木材', 'wood', '["natural","medieval","rustic"]', '["forest"]', 1, 1, 50, 0, '基础木材'),
        (6, 'GrayBrick', '灰砖', 'brick', '["medieval","castle","urban"]', '["forest"]', 1, 0, 50, 0, '经典砖块'),
        (7, 'GoldBrick', '金砖', 'luxury', '["luxury","palace","treasure"]', '["any"]', 1, 0, 50, 0, '豪华金砖'),
        (8, 'SilverBrick', '银砖', 'luxury', '["luxury","ice"]', '["snow","ice"]', 1, 0, 50, 0, '银色砖块'),
        (9, 'CopperBrick', '铜砖', 'brick', '["steampunk","industrial"]', '["any"]', 1, 0, 50, 0, '铜色砖块'),
        (10, 'IronBrick', '铁砖', 'brick', '["steampunk","industrial"]', '["any"]', 1, 0, 50, 0, '铁色砖块'),
        (13, 'Glass', '玻璃', 'transparent', '["modern","aquarium"]', '["any"]', 1, 0, 20, 0, '透明方块'),
        (14, 'Platforms', '平台', 'platform', '["any"]', '["any"]', 1, 0, 20, 0, '可穿过的平台'),

        # 砖块系列
        (38, 'RedBrick', '红砖', 'brick', '["urban","industrial"]', '["forest","desert"]', 1, 0, 50, 0, '红色砖块'),
        (41, 'Obsidian', '黑曜石', 'luxury', '["hell","dark","volcanic"]', '["hell"]', 1, 1, 100, 0, '地狱材料'),
        (42, 'Marble', '大理石', 'luxury', '["greek","roman","temple"]', '["underground"]', 1, 1, 50, 0, '古典风格材料'),
        (43, 'Granite', '花岗岩', 'luxury', '["modern","tech"]', '["underground"]', 1, 1, 50, 0, '现代风格材料'),
        (44, 'SnowBlock', '雪块', 'natural', '["snow","winter"]', '["snow","tundra"]', 1, 1, 50, 0, '雪地方块'),
        (45, 'IceBlock', '冰块', 'transparent', '["ice","frozen"]', '["snow","ice"]', 1, 1, 50, 0, '冰冻方块'),
        (46, 'Sandstone', '砂岩', 'brick', '["desert","egyptian"]', '["desert"]', 1, 1, 50, 0, '沙漠风格材料'),
        (47, 'RichMahogany', '红木', 'wood', '["jungle","luxury"]', '["jungle"]', 1, 1, 50, 0, '丛林木材'),
        (48, 'BorealWood', '针叶木', 'wood', '["snow","rustic"]', '["snow"]', 1, 1, 50, 0, '雪地木材'),
        (49, 'PalmWood', '棕榈木', 'wood', '["tropical","beach"]', '["ocean","desert"]', 1, 1, 50, 0, '海滩木材'),

        # 石板系列
        (143, 'StoneSlab', '石板', 'brick', '["medieval","castle","temple"]', '["forest"]', 1, 0, 50, 0, '平整石板'),
        (179, 'SandstoneSlab', '砂岩板', 'brick', '["desert","egyptian"]', '["desert"]', 1, 0, 50, 0, '沙漠石板'),

        # 特殊方块
        (166, 'Pearlstone', '珍珠石', 'luxury', '["hallow","divine","fantasy"]', '["hallow"]', 1, 1, 50, 1, '神圣风格石'),
        (168, 'Ebonstone', '黑檀石', 'brick', '["corruption","dark"]', '["corruption"]', 1, 1, 50, 0, '腐化风格石材'),
        (169, 'Crimstone', '猩红石', 'brick', '["crimson","blood"]', '["crimson"]', 1, 1, 50, 0, '猩红风格石材'),
        (216, 'MushroomBlock', '蘑菇块', 'brick', '["glowing_mushroom"]', '["glowing_mushroom"]', 1, 1, 50, 5, '发光蘑菇方块'),
        (633, 'DynastyWood', '王朝木', 'wood', '["asian","eastern"]', '["any"]', 1, 1, 50, 0, '东方风格木材'),

        # 光源
        (4, 'Torch', '火把', 'light', '["any"]', '["any"]', 0, 0, 30, 10, '基础光源'),
        (33, 'Candle', '蜡烛', 'light', '["luxury"]', '["any"]', 0, 0, 30, 5, '小型光源'),
        (34, 'Chandelier', '吊灯', 'light', '["luxury","palace"]', '["any"]', 0, 0, 50, 15, '大型光源'),
        (93, 'Lamp', '立灯', 'light', '["modern"]', '["any"]', 0, 0, 50, 10, '立式灯具'),
        (215, 'Campfire', '篝火', 'light', '["outdoor"]', '["any"]', 0, 0, 50, 20, '提供生命恢复'),

        # 门
        (10, 'Door', '门', 'door', '["any"]', '["any"]', 1, 0, 50, 0, '标准门'),
        (386, 'TrapDoor', '活板门', 'door', '["any"]', '["any"]', 1, 0, 50, 0, '水平门'),
        (389, 'TallGate', '大门', 'door', '["farm"]', '["any"]', 1, 0, 50, 0, '农场大门'),
    ]

    for t in tiles:
        cursor.execute("""
            INSERT OR IGNORE INTO tiles
            (id, name, display_name, category, styles, biome_match, paint_compatible, slope_compatible, hardness, light_emission, description, source)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'manual')
        """, t)

    return len(tiles)


def insert_basic_walls(cursor):
    """插入基础墙壁数据"""
    walls = [
        (1, 'StoneWall', '石墙', 'brick', '["medieval","natural"]', 1, 0, '基础石墙'),
        (2, 'DirtWall', '泥墙', 'natural', '["natural","underground"]', 1, 1, '泥土墙'),
        (4, 'WoodWall', '木墙', 'wood', '["natural","medieval"]', 1, 0, '基础木墙'),
        (5, 'GrayBrickWall', '灰砖墙', 'brick', '["medieval","castle"]', 1, 0, '灰砖墙壁'),
        (6, 'GoldBrickWall', '金砖墙', 'luxury', '["luxury","palace"]', 1, 0, '金色墙壁'),
        (7, 'SilverBrickWall', '银砖墙', 'luxury', '["ice"]', 1, 0, '银色墙壁'),
        (8, 'CopperBrickWall', '铜砖墙', 'brick', '["steampunk"]', 1, 0, '铜色墙壁'),
        (9, 'IronBrickWall', '铁砖墙', 'brick', '["steampunk"]', 1, 0, '铁色墙壁'),
        (10, 'GlassWall', '玻璃墙', 'transparent', '["modern","aquarium"]', 1, 0, '透明墙壁'),
        (11, 'StoneSlabWall', '石板墙', 'brick', '["medieval","temple"]', 1, 0, '石板墙壁'),
        (12, 'SandstoneWall', '砂岩墙', 'brick', '["desert","egyptian"]', 1, 0, '沙漠墙壁'),
        (13, 'SnowWall', '雪墙', 'natural', '["snow","winter"]', 1, 0, '雪地墙壁'),
        (14, 'MarbleWall', '大理石墙', 'luxury', '["greek","roman"]', 1, 0, '古典墙壁'),
        (15, 'GraniteWall', '花岗岩墙', 'luxury', '["modern"]', 1, 0, '现代墙壁'),
        (16, 'PearlstoneWall', '珍珠石墙', 'luxury', '["hallow","divine"]', 1, 0, '神圣墙壁'),
        (17, 'EbonstoneWall', '黑檀石墙', 'brick', '["corruption","dark"]', 1, 0, '腐化墙壁'),
        (18, 'CrimstoneWall', '猩红石墙', 'brick', '["crimson","blood"]', 1, 0, '猩红墙壁'),
        (19, 'RedBrickWall', '红砖墙', 'brick', '["urban"]', 1, 0, '红色墙壁'),
        (20, 'DynastyWall', '王朝墙', 'wood', '["asian","eastern"]', 1, 0, '东方墙壁'),
        (21, 'MushroomWall', '蘑菇墙', 'brick', '["glowing_mushroom"]', 1, 0, '发光蘑菇墙壁'),
        (22, 'BorealWoodWall', '针叶木墙', 'wood', '["snow","rustic"]', 1, 0, '雪地木墙'),
        (23, 'PalmWoodWall', '棕榈木墙', 'wood', '["tropical","beach"]', 1, 0, '海滩木墙'),
        (24, 'RichMahoganyWall', '红木墙', 'wood', '["jungle","luxury"]', 1, 0, '丛林木墙'),
    ]

    for w in walls:
        cursor.execute("""
            INSERT OR IGNORE INTO walls
            (id, name, display_name, category, styles, paint_compatible, is_natural, description, source)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, 'manual')
        """, w)

    return len(walls)


def generate_tiles_sql(cursor, sql_path):
    """生成tiles和walls的SQL文件"""
    with open(sql_path, 'w', encoding='utf-8') as f:
        f.write("-- Terraria 基础方块和墙壁数据\n")
        f.write(f"-- 生成时间: {datetime.now().isoformat()}\n\n")

        # tiles
        f.write("-- === tiles ===\n")
        cursor.execute("SELECT * FROM tiles ORDER BY id")
        for row in cursor.fetchall():
            values = []
            for val in row:
                if val is None:
                    values.append('NULL')
                elif isinstance(val, int):
                    values.append(str(val))
                else:
                    escaped = str(val).replace("'", "''")
                    values.append(f"'{escaped}'")
            f.write(f"INSERT OR IGNORE INTO tiles VALUES ({', '.join(values)});\n")

        # walls
        f.write("\n-- === walls ===\n")
        cursor.execute("SELECT * FROM walls ORDER BY id")
        for row in cursor.fetchall():
            values = []
            for val in row:
                if val is None:
                    values.append('NULL')
                elif isinstance(val, int):
                    values.append(str(val))
                else:
                    escaped = str(val).replace("'", "''")
                    values.append(f"'{escaped}'")
            f.write(f"INSERT OR IGNORE INTO walls VALUES ({', '.join(values)});\n")

    print(f"SQL文件已生成: {sql_path}")


def main():
    print("=" * 50)
    print("补充基础方块和墙壁数据")
    print("=" * 50)

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    # 插入tiles
    count = insert_basic_tiles(cursor)
    print(f"\n插入tiles: {count}")

    # 插入walls
    count = insert_basic_walls(cursor)
    print(f"插入walls: {count}")

    conn.commit()

    # 生成SQL文件
    generate_tiles_sql(cursor, SQL_PATH)

    # 更新完整SQL文件
    full_sql_path = os.path.join(DATA_DIR, "terraria_kb_full.sql")
    with open(full_sql_path, 'r', encoding='utf-8') as f:
        existing = f.read()

    # 在开头添加tiles和walls
    with open(full_sql_path, 'w', encoding='utf-8') as f:
        f.write("-- Terraria 建筑知识库数据库 (完整版)\n")
        f.write(f"-- 更新时间: {datetime.now().isoformat()}\n\n")

        # tiles部分
        f.write("-- === tiles ===\n")
        cursor.execute("SELECT * FROM tiles ORDER BY id")
        for row in cursor.fetchall():
            values = []
            for val in row:
                if val is None:
                    values.append('NULL')
                elif isinstance(val, int):
                    values.append(str(val))
                else:
                    escaped = str(val).replace("'", "''")
                    values.append(f"'{escaped}'")
            f.write(f"INSERT OR IGNORE INTO tiles VALUES ({', '.join(values)});\n")

        # walls部分
        f.write("\n-- === walls ===\n")
        cursor.execute("SELECT * FROM walls ORDER BY id")
        for row in cursor.fetchall():
            values = []
            for val in row:
                if val is None:
                    values.append('NULL')
                elif isinstance(val, int):
                    values.append(str(val))
                else:
                    escaped = str(val).replace("'", "''")
                    values.append(f"'{escaped}'")
            f.write(f"INSERT OR IGNORE INTO walls VALUES ({', '.join(values)});\n")

        # 其他表（从原有内容提取）
        f.write("\n" + existing.split("-- === paints ===")[1])

    print(f"完整SQL已更新: {full_sql_path}")

    # 统计
    print("\n=== 最终统计 ===")
    tables = ['tiles', 'walls', 'paints', 'slopes', 'furniture', 'light_sources', 'doors',
              'style_templates', 'npc_requirements', 'house_validation', 'biomes']
    total = 0
    for t in tables:
        cursor.execute(f"SELECT COUNT(*) FROM {t}")
        count = cursor.fetchone()[0]
        total += count
        print(f"  {t}: {count}")
    print(f"  总计: {total}")

    conn.close()


if __name__ == "__main__":
    main()