#!/usr/bin/env python3
"""
建筑蓝图数据库表初始化脚本
基于TEditSch.json格式设计，支持向量检索
"""

import sqlite3
import json
import os
import sys
from datetime import datetime

sys.stdout.reconfigure(encoding='utf-8')

DATA_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Data", "kb")
DB_PATH = os.path.join(DATA_DIR, "terraria_kb.db")


def create_schematic_tables(cursor):
    """创建建筑蓝图相关表"""

    # 1. 建筑蓝图主表 - 元数据
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS building_schematics (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL,
        display_name TEXT,
        description TEXT,

        -- 尺寸信息
        width INTEGER NOT NULL,
        height INTEGER NOT NULL,
        total_tiles INTEGER,

        -- 版本信息
        version_raw INTEGER,
        version_actual INTEGER,
        format_version TEXT,

        -- 分类标签
        category TEXT,              -- house, castle, tower, furniture, decoration
        style TEXT,                 -- medieval, fantasy, modern, etc.
        biome_match TEXT,           -- forest, desert, snow, etc.
        complexity INTEGER,         -- 1-5 难度等级

        -- 统计信息
        unique_tiles INTEGER,       -- 不同方块种类数
        unique_walls INTEGER,       -- 不同墙壁种类数
        has_wires INTEGER,          -- 是否有电线
        has_actuators INTEGER,      --是否有制动器
        has_liquid INTEGER,         -- 是否有液体

        -- 文件信息
        source_file TEXT,           -- 原始JSON文件名
        tile_data_bytes INTEGER,    -- 原始数据大小
        created_at TEXT,
        updated_at TEXT
    )
    """)

    # 2. 蓝图方块数据表 - 压缩存储
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS schematic_tiles (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        schematic_id INTEGER NOT NULL,

        -- 坐标（相对位置）
        x INTEGER NOT NULL,
        y INTEGER NOT NULL,

        -- 方块数据
        tile_type INTEGER,          -- Tile ID (null表示空)
        tile_u INTEGER,             -- 帧坐标U
        tile_v INTEGER,             -- 帧坐标V
        tile_color INTEGER,         -- 方块油漆

        -- 墙壁数据
        wall_type INTEGER,          -- Wall ID
        wall_color INTEGER,         -- 墙壁油漆

        -- 其他数据
        liquid_type INTEGER,        -- 液体类型 (0=无, 1=水, 2=蜂蜜, 3=岩浆)
        liquid_amount INTEGER,      -- 液体量

        -- 电线/制动器
        wire_red INTEGER,
        wire_blue INTEGER,
        wire_green INTEGER,
        wire_yellow INTEGER,
        actuator INTEGER,
        actuator_inactive INTEGER,

        FOREIGN KEY (schematic_id) REFERENCES building_schematics(id)
    )
    """)

    # 创建索引加速查询
    cursor.execute("CREATE INDEX IF NOT EXISTS idx_schematic_tiles_schematic ON schematic_tiles(schematic_id)")
    cursor.execute("CREATE INDEX IF NOT EXISTS idx_schematic_tiles_type ON schematic_tiles(tile_type)")
    cursor.execute("CREATE INDEX IF NOT EXISTS idx_schematic_tiles_wall ON schematic_tiles(wall_type)")

    # 3. 蓝图分析表 - AI分析结果（用于向量检索）
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS schematic_analysis (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        schematic_id INTEGER NOT NULL,

        -- 主要方块统计 (JSON)
        primary_tiles TEXT,         -- 使用最多的方块 [{"id": 5, "count": 50, "name": "Wood"}, ...]
        primary_walls TEXT,         -- 使用最多的墙壁

        -- 风格匹配分析
        detected_style TEXT,        -- AI检测到的风格
        style_confidence REAL,      -- 置信度 0-1

        -- 生物群落匹配
        detected_biome TEXT,        -- AI检测到的适合生物群落
        biome_confidence REAL,

        -- 建筑特征
        has_roof INTEGER,           -- 是否有屋顶结构
        has_floor INTEGER,          -- 是否有地板
        has_walls_structure INTEGER,-- 是否有完整墙壁
        has_door INTEGER,           -- 是否有门
        has_light INTEGER,          -- 是否有光源
        has_furniture INTEGER,      -- 是否有家具

        -- 功能分析
        is_house INTEGER,           -- 是否可作为NPC房屋
        is_decoration INTEGER,      -- 是否纯装饰
        room_count INTEGER,         -- 房间数量

        -- 语义描述 (用于向量生成)
        semantic_text TEXT,         -- 方块+墙壁+特征的语义文本

        FOREIGN KEY (schematic_id) REFERENCES building_schematics(id)
    )
    """)

    # 4. 蓝图标签表 - 多对多标签
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS schematic_tags (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        schematic_id INTEGER NOT NULL,
        tag TEXT NOT NULL,          -- 标签：small, large, outdoor, underground, farm, etc.

        FOREIGN KEY (schematic_id) REFERENCES building_schematics(id)
    )
    """)

    cursor.execute("CREATE INDEX IF NOT EXISTS idx_schematic_tags_schematic ON schematic_tags(schematic_id)")
    cursor.execute("CREATE INDEX IF NOT EXISTS idx_schematic_tags_tag ON schematic_tags(tag)")

    print("✓ 建筑蓝图表创建完成")
    print("  - building_schematics: 蓝图元数据")
    print("  - schematic_tiles: 方块数据")
    print("  - schematic_analysis: AI分析结果")
    print("  - schematic_tags: 标签系统")


def insert_schematic_from_json(cursor, json_path):
    """从TEditSch.json导入蓝图数据"""

    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    # 1. 插入主表
    cursor.execute("""
        INSERT INTO building_schematics
        (name, width, height, total_tiles, version_raw, version_actual, format_version,
         source_file, tile_data_bytes, created_at)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, (
        data.get('name', 'unknown'),
        data.get('width', 0),
        data.get('height', 0),
        data.get('total_tiles', 0),
        data.get('version_raw', 0),
        data.get('version_actual', 0),
        data.get('format_version', 'V5'),
        os.path.basename(json_path),
        data.get('tile_data_bytes', 0),
        datetime.now().isoformat()
    ))

    schematic_id = cursor.lastrowid

    # 2. 统计方块和墙壁
    tile_counts = {}
    wall_counts = {}
    has_wires = False
    has_actuators = False
    has_liquid = False

    tiles_data = data.get('tiles', [])

    # 3. 插入方块数据
    for y, row in enumerate(tiles_data):
        for x, tile in enumerate(row):
            tile_type = tile.get('type')
            wall_type = tile.get('wall')

            # 统计
            if tile_type is not None:
                tile_counts[tile_type] = tile_counts.get(tile_type, 0) + 1
            if wall_type is not None:
                wall_counts[wall_type] = wall_counts.get(wall_type, 0) + 1

            # 检查特殊元素
            wires = tile.get('wires', {})
            if wires.get('red') or wires.get('blue') or wires.get('green') or wires.get('yellow'):
                has_wires = True
            if tile.get('actuator'):
                has_actuators = True
            if tile.get('liquid_type'):
                has_liquid = True

            # 插入方块记录
            cursor.execute("""
                INSERT INTO schematic_tiles
                (schematic_id, x, y, tile_type, tile_u, tile_v, tile_color,
                 wall_type, wall_color, liquid_type, liquid_amount,
                 wire_red, wire_blue, wire_green, wire_yellow, actuator, actuator_inactive)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                schematic_id, x, y,
                tile_type,
                tile.get('u'),
                tile.get('v'),
                tile.get('tile_color'),
                wall_type,
                tile.get('wall_color'),
                tile.get('liquid_type'),
                tile.get('liquid_amount'),
                wires.get('red', False),
                wires.get('blue', False),
                wires.get('green', False),
                wires.get('yellow', False),
                tile.get('actuator', False),
                tile.get('actuator_inactive', False)
            ))

    # 4. 更新统计信息
    cursor.execute("""
        UPDATE building_schematics SET
        unique_tiles = ?,
        unique_walls = ?,
        has_wires = ?,
        has_actuators = ?,
        has_liquid = ?
        WHERE id = ?
    """, (
        len(tile_counts),
        len(wall_counts),
        has_wires,
        has_actuators,
        has_liquid,
        schematic_id
    ))

    # 5. 生成初步分析
    # 获取主要方块（按数量排序）
    primary_tiles = sorted(tile_counts.items(), key=lambda x: x[1], reverse=True)[:10]
    primary_walls = sorted(wall_counts.items(), key=lambda x: x[1], reverse=True)[:5]

    # 生成语义文本（用于向量）
    semantic_parts = []
    semantic_parts.append(f"尺寸{data['width']}x{data['height']}")

    # 添加方块名称（需要从tiles表查询）
    for tile_id, count in primary_tiles[:5]:
        cursor.execute("SELECT name, display_name, category FROM tiles WHERE id = ?", (tile_id,))
        row = cursor.fetchone()
        if row:
            semantic_parts.append(f"{row[1]}{count}个")
            semantic_parts.append(row[2])  # category

    # 添加墙壁名称
    for wall_id, count in primary_walls[:3]:
        cursor.execute("SELECT name, display_name, category FROM walls WHERE id = ?", (wall_id,))
        row = cursor.fetchone()
        if row:
            semantic_parts.append(f"{row[1]}墙{count}个")

    semantic_text = " ".join(semantic_parts)

    cursor.execute("""
        INSERT INTO schematic_analysis
        (schematic_id, primary_tiles, primary_walls, semantic_text)
        VALUES (?, ?, ?, ?)
    """, (
        schematic_id,
        json.dumps(primary_tiles),
        json.dumps(primary_walls),
        semantic_text
    ))

    print(f"✓ 导入蓝图: {data.get('name')} ({data['width']}x{data['height']})")
    print(f"  方块种类: {len(tile_counts)}, 墙壁种类: {len(wall_counts)}")
    print(f"  语义文本: {semantic_text[:50]}...")

    return schematic_id


def main():
    print("=" * 60)
    print("建筑蓝图数据库表初始化")
    print("=" * 60)

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    # 创建表
    create_schematic_tables(cursor)

    # 测试导入示例文件
    test_json = r"C:\Users\admin\Downloads\Game\1.TEditSch.json"
    if os.path.exists(test_json):
        print("\n=== 导入测试蓝图 ===")
        insert_schematic_from_json(cursor, test_json)

    conn.commit()
    conn.close()

    print("\n" + "=" * 60)
    print("完成!")
    print("=" * 60)


if __name__ == "__main__":
    main()