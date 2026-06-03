#!/usr/bin/env python3
"""
建筑实体数据库管理脚本 v2
支持新的数据格式：TEditSch.json + 建筑整体描述.md
"""

import sqlite3
import json
import os
import sys
import re
from datetime import datetime

sys.stdout.reconfigure(encoding='utf-8')

DATA_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Data", "kb")
DB_PATH = os.path.join(DATA_DIR, "terraria_kb.db")
CAMERA_ROLL_DIR = r"C:\Users\admin\Pictures\Camera Roll"


def create_tables(cursor):
    """创建建筑实体相关表"""

    # 1. 建筑实体主表
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS buildings (
        id TEXT PRIMARY KEY,
        source_file TEXT,
        screenshot_file TEXT,
        file_size INTEGER,
        version INTEGER,
        width INTEGER NOT NULL,
        height INTEGER NOT NULL,
        total_tiles INTEGER,
        active_tiles INTEGER,
        tiles_with_walls INTEGER,
        building_type TEXT,
        style TEXT,
        feature_tags TEXT,
        summary TEXT,
        primary_materials TEXT,
        style_indicators TEXT,
        generated_at TEXT,
        created_at TEXT DEFAULT CURRENT_TIMESTAMP,
        updated_at TEXT DEFAULT CURRENT_TIMESTAMP
    )
    """)

    # 2. 方块统计表
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS building_tiles (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        building_id TEXT NOT NULL,
        tile_id INTEGER NOT NULL,
        tile_name TEXT,
        tile_count INTEGER,
        tile_ratio REAL,
        is_primary INTEGER DEFAULT 0,
        is_unknown INTEGER DEFAULT 0,
        category TEXT,
        FOREIGN KEY (building_id) REFERENCES buildings(id)
    )
    """)

    cursor.execute("CREATE INDEX IF NOT EXISTS idx_building_tiles_building ON building_tiles(building_id)")
    cursor.execute("CREATE INDEX IF NOT EXISTS idx_building_tiles_type ON building_tiles(tile_id)")

    # 3. 墙壁统计表
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS building_walls (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        building_id TEXT NOT NULL,
        wall_id INTEGER NOT NULL,
        wall_name TEXT,
        wall_count INTEGER,
        wall_ratio REAL,
        is_primary INTEGER DEFAULT 0,
        is_unknown INTEGER DEFAULT 0,
        FOREIGN KEY (building_id) REFERENCES buildings(id)
    )
    """)

    cursor.execute("CREATE INDEX IF NOT EXISTS idx_building_walls_building ON building_walls(building_id)")

    # 4. 向量索引表
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS building_vectors (
        building_id TEXT PRIMARY KEY,
        vector TEXT,
        vector_model TEXT,
        vector_dimension INTEGER,
        keywords TEXT,
        search_text TEXT,
        FOREIGN KEY (building_id) REFERENCES buildings(id)
    )
    """)

    # 5. 原始数据表
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS building_raw_data (
        building_id TEXT PRIMARY KEY,
        tedit_json TEXT,
        description_md TEXT,
        tile_grid TEXT,
        wall_grid TEXT,
        FOREIGN KEY (building_id) REFERENCES buildings(id)
    )
    """)

    # 6. 截图表
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS building_images (
        building_id TEXT PRIMARY KEY,
        image_path TEXT,
        image_width INTEGER,
        image_height INTEGER,
        image_base64 TEXT,
        FOREIGN KEY (building_id) REFERENCES buildings(id)
    )
    """)

    print("✓ 数据库表创建完成")
    print("  - buildings: 建筑实体主表")
    print("  - building_tiles: 方块统计表")
    print("  - building_walls: 墙壁统计表")
    print("  - building_vectors: 向量索引表")
    print("  - building_raw_data: 原始数据表")
    print("  - building_images: 截图表")


def parse_description_md(md_path):
    """解析建筑整体描述.md文件"""
    if not os.path.exists(md_path):
        return None

    with open(md_path, 'r', encoding='utf-8') as f:
        content = f.read()

    result = {
        'size': None,
        'type': None,
        'style': None,
        'feature_tags': [],
        'summary': None
    }

    # 解析尺寸
    match = re.search(r'\*\*尺寸\*\*:\s*(\d+)\s*x\s*(\d+)', content)
    if match:
        result['width'] = int(match.group(1))
        result['height'] = int(match.group(2))

    # 解析类型
    match = re.search(r'\*\*类型\*\*:\s*(.+?)(?:\n|$)', content)
    if match:
        result['type'] = match.group(1).strip()

    # 解析风格
    match = re.search(r'\*\*风格\*\*:\s*(.+?)(?:\n|$)', content)
    if match:
        result['style'] = match.group(1).strip()

    # 解析特征标签
    match = re.search(r'\*\*特征标签\*\*:\s*(.+?)(?:\n|$)', content)
    if match:
        tags_str = match.group(1).strip()
        result['feature_tags'] = [t.strip() for t in tags_str.split(',')]

    # 解析简介
    match = re.search(r'\*\*简介\*\*:\s*(.+?)(?:\n|$)', content)
    if match:
        result['summary'] = match.group(1).strip()

    return result


def parse_tedit_json(json_path):
    """解析TEditSch.json文件"""
    if not os.path.exists(json_path):
        return None

    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    return data


def import_building(cursor, building_dir):
    """导入单个建筑目录"""
    building_id = os.path.basename(building_dir)

    # 查找JSON文件
    json_files = [f for f in os.listdir(building_dir) if f.endswith('.TEditSch.json') or f.endswith('.json')]
    if not json_files:
        print(f"  跳过 {building_id}: 未找到JSON文件")
        return None

    # 优先使用TEditSch.json
    json_file = None
    for f in json_files:
        if 'TEditSch' in f:
            json_file = f
            break
    if not json_file:
        json_file = json_files[0]

    json_path = os.path.join(building_dir, json_file)

    # 查找描述文件
    md_path = os.path.join(building_dir, '建筑整体描述.md')

    # 查找截图文件
    screenshots = [f for f in os.listdir(building_dir) if f.endswith('.png') or f.endswith('.jpg')]
    screenshot_file = screenshots[0] if screenshots else None

    # 解析数据
    json_data = parse_tedit_json(json_path)
    desc_data = parse_description_md(md_path)

    if not json_data:
        print(f"  跳过 {building_id}: JSON解析失败")
        return None

    # 提取数据
    header = json_data.get('header', {})
    tile_stats = json_data.get('tile_stats', {})
    wall_stats = json_data.get('wall_stats', {})
    dimensions = json_data.get('dimensions', {})
    description = json_data.get('description', {})
    material_analysis = json_data.get('material_analysis', {})

    # 优先使用描述文件的数据
    width = desc_data.get('width') if desc_data else None
    height = desc_data.get('height') if desc_data else None
    building_type = desc_data.get('type') if desc_data else None
    style = desc_data.get('style') if desc_data else None
    feature_tags = desc_data.get('feature_tags', []) if desc_data else []
    summary = desc_data.get('summary') if desc_data else None

    # 如果描述文件没有尺寸，使用JSON数据
    if not width:
        width = header.get('width') or dimensions.get('width', 0)
    if not height:
        height = header.get('height') or dimensions.get('height', 0)

    # 如果描述文件没有类型/风格，使用JSON数据
    if not building_type:
        building_type = description.get('type')
    if not style:
        style = description.get('style')
    if not summary:
        summary = description.get('summary')
    if not feature_tags:
        feature_tags = description.get('feature_tags', [])

    # 检查是否已存在
    cursor.execute("SELECT id FROM buildings WHERE id = ?", (building_id,))
    if cursor.fetchone():
        print(f"  建筑 {building_id} 已存在，跳过")
        return building_id

    # 插入主表
    cursor.execute("""
        INSERT INTO buildings
        (id, source_file, screenshot_file, file_size, version, width, height,
         total_tiles, active_tiles, tiles_with_walls,
         building_type, style, feature_tags, summary,
         primary_materials, style_indicators, generated_at)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, (
        building_id,
        json_file,
        screenshot_file,
        json_data.get('file_size'),
        header.get('version'),
        width,
        height,
        dimensions.get('total_tiles') or (width * height),
        dimensions.get('active_tiles') or tile_stats.get('total_active'),
        dimensions.get('tiles_with_walls') or tile_stats.get('with_wall'),
        building_type,
        style,
        json.dumps(feature_tags, ensure_ascii=False),
        summary,
        json.dumps(material_analysis.get('primary_materials', []), ensure_ascii=False),
        json.dumps(material_analysis.get('style_indicators', {}), ensure_ascii=False),
        json_data.get('metadata', {}).get('generated_at')
    ))

    # 插入方块统计
    tile_types = tile_stats.get('types', [])
    total_tiles = sum(t.get('count', 0) for t in tile_types)

    for i, tile in enumerate(tile_types):
        tile_id = tile.get('id')
        tile_name = tile.get('name', f'Unknown({tile_id})')
        tile_count = tile.get('count', 0)
        tile_ratio = round(tile_count / total_tiles * 100, 2) if total_tiles > 0 else 0
        is_primary = 1 if i < 3 else 0
        is_unknown = 1 if 'Unknown' in tile_name else 0

        cursor.execute("""
            INSERT INTO building_tiles
            (building_id, tile_id, tile_name, tile_count, tile_ratio, is_primary, is_unknown)
            VALUES (?, ?, ?, ?, ?, ?, ?)
        """, (building_id, tile_id, tile_name, tile_count, tile_ratio, is_primary, is_unknown))

    # 插入墙壁统计
    wall_types = wall_stats.get('types', [])
    total_walls = sum(w.get('count', 0) for w in wall_types)

    for i, wall in enumerate(wall_types):
        wall_id = wall.get('id')
        wall_name = wall.get('name', f'Unknown Wall({wall_id})')
        wall_count = wall.get('count', 0)
        wall_ratio = round(wall_count / total_walls * 100, 2) if total_walls > 0 else 0
        is_primary = 1 if i < 3 else 0
        is_unknown = 1 if 'Unknown' in wall_name else 0

        cursor.execute("""
            INSERT INTO building_walls
            (building_id, wall_id, wall_name, wall_count, wall_ratio, is_primary, is_unknown)
            VALUES (?, ?, ?, ?, ?, ?, ?)
        """, (building_id, wall_id, wall_name, wall_count, wall_ratio, is_primary, is_unknown))

    # 插入向量索引
    keywords = f"{style} {building_type} " + " ".join(feature_tags)
    search_text = f"{summary} {keywords}"

    cursor.execute("""
        INSERT INTO building_vectors
        (building_id, keywords, search_text)
        VALUES (?, ?, ?)
    """, (building_id, keywords.strip(), search_text.strip()))

    # 插入原始数据
    with open(json_path, 'r', encoding='utf-8') as f:
        tedit_json = f.read()

    description_md = None
    if os.path.exists(md_path):
        with open(md_path, 'r', encoding='utf-8') as f:
            description_md = f.read()

    cursor.execute("""
        INSERT INTO building_raw_data
        (building_id, tedit_json, description_md)
        VALUES (?, ?, ?)
    """, (building_id, tedit_json, description_md))

    # 插入截图信息
    if screenshot_file:
        screenshot_path = os.path.join(building_dir, screenshot_file)
        cursor.execute("""
            INSERT INTO building_images
            (building_id, image_path)
            VALUES (?, ?)
        """, (building_id, screenshot_path))

    print(f"✓ 导入建筑: {building_id}")
    print(f"  类型: {building_type}, 风格: {style}")
    print(f"  尺寸: {width}x{height}, 方块: {tile_stats.get('total_active', 0)}")
    print(f"  方块种类: {len(tile_types)}种, 墙壁种类: {len(wall_types)}种")

    return building_id


def import_all_buildings(cursor, base_dir):
    """导入所有建筑"""
    count = 0
    failed = []

    for item in sorted(os.listdir(base_dir)):
        item_path = os.path.join(base_dir, item)

        # 跳过非目录和特殊目录
        if not os.path.isdir(item_path):
            continue
        if item in ['doc', 'output', '.claude']:
            continue

        # 检查是否是建筑目录（以数字开头）
        if not item[0].isdigit():
            continue

        print(f"\n处理: {item}")
        try:
            result = import_building(cursor, item_path)
            if result:
                count += 1
            else:
                failed.append(item)
        except Exception as e:
            print(f"  错误: {e}")
            failed.append(item)

    return count, failed


def search_buildings(cursor, style=None, building_type=None, keywords=None, limit=10):
    """搜索建筑"""
    sql = "SELECT * FROM buildings WHERE 1=1"
    params = []

    if style:
        sql += " AND style LIKE ?"
        params.append(f"%{style}%")

    if building_type:
        sql += " AND building_type = ?"
        params.append(building_type)

    if keywords:
        sql += """
            AND id IN (
                SELECT building_id FROM building_vectors
                WHERE keywords LIKE ? OR search_text LIKE ?
            )
        """
        params.extend([f"%{keywords}%", f"%{keywords}%"])

    sql += f" ORDER BY created_at DESC LIMIT {limit}"

    cursor.execute(sql, params)

    results = []
    columns = [desc[0] for desc in cursor.description]

    for row in cursor.fetchall():
        results.append(dict(zip(columns, row)))

    return results


def get_building_detail(cursor, building_id):
    """获取建筑详细信息"""
    # 获取主表数据
    cursor.execute("SELECT * FROM buildings WHERE id = ?", (building_id,))
    row = cursor.fetchone()

    if not row:
        return None

    columns = [desc[0] for desc in cursor.description]
    building = dict(zip(columns, row))

    # 解析JSON字段
    if building.get('feature_tags'):
        building['feature_tags'] = json.loads(building['feature_tags'])
    if building.get('primary_materials'):
        building['primary_materials'] = json.loads(building['primary_materials'])
    if building.get('style_indicators'):
        building['style_indicators'] = json.loads(building['style_indicators'])

    # 获取方块统计
    cursor.execute("""
        SELECT tile_id, tile_name, tile_count, tile_ratio, is_primary
        FROM building_tiles
        WHERE building_id = ?
        ORDER BY tile_count DESC
    """, (building_id,))

    building['tiles'] = [dict(zip(['tile_id', 'tile_name', 'tile_count', 'tile_ratio', 'is_primary'], row))
                         for row in cursor.fetchall()]

    # 获取墙壁统计
    cursor.execute("""
        SELECT wall_id, wall_name, wall_count, wall_ratio, is_primary
        FROM building_walls
        WHERE building_id = ?
        ORDER BY wall_count DESC
    """, (building_id,))

    building['walls'] = [dict(zip(['wall_id', 'wall_name', 'wall_count', 'wall_ratio', 'is_primary'], row))
                         for row in cursor.fetchall()]

    # 获取向量数据
    cursor.execute("SELECT keywords, search_text FROM building_vectors WHERE building_id = ?", (building_id,))
    row = cursor.fetchone()
    if row:
        building['keywords'] = row[0]
        building['search_text'] = row[1]

    return building


def get_building_json(cursor, building_id):
    """获取建筑的原始JSON数据"""
    cursor.execute("SELECT tedit_json, description_md FROM building_raw_data WHERE building_id = ?", (building_id,))
    row = cursor.fetchone()

    if not row:
        return None

    return {
        'building_id': building_id,
        'tedit_json': json.loads(row[0]) if row[0] else None,
        'description_md': row[1]
    }


def show_stats(cursor):
    """显示数据库统计"""
    cursor.execute("SELECT COUNT(*) FROM buildings")
    building_count = cursor.fetchone()[0]

    cursor.execute("SELECT COUNT(*) FROM building_tiles")
    tile_count = cursor.fetchone()[0]

    cursor.execute("SELECT COUNT(*) FROM building_walls")
    wall_count = cursor.fetchone()[0]

    cursor.execute("SELECT COUNT(*) FROM building_vectors")
    vector_count = cursor.fetchone()[0]

    print("\n" + "=" * 60)
    print("数据库统计")
    print("=" * 60)
    print(f"建筑实体数: {building_count}")
    print(f"方块记录数: {tile_count}")
    print(f"墙壁记录数: {wall_count}")
    print(f"向量记录数: {vector_count}")

    if building_count > 0:
        # 按类型统计
        cursor.execute("""
            SELECT building_type, COUNT(*) as cnt
            FROM buildings
            GROUP BY building_type
            ORDER BY cnt DESC
        """)
        print("\n建筑类型分布:")
        for row in cursor.fetchall():
            print(f"  {row[0]}: {row[1]}")

        # 按风格统计
        cursor.execute("""
            SELECT style, COUNT(*) as cnt
            FROM buildings
            GROUP BY style
            ORDER BY cnt DESC
            LIMIT 10
        """)
        print("\n建筑风格分布:")
        for row in cursor.fetchall():
            print(f"  {row[0]}: {row[1]}")


def main():
    print("=" * 60)
    print("建筑实体数据库管理 v2")
    print("=" * 60)

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    # 创建表
    create_tables(cursor)
    conn.commit()

    # 导入建筑
    print("\n" + "=" * 60)
    print("导入建筑数据")
    print("=" * 60)

    count, failed = import_all_buildings(cursor, CAMERA_ROLL_DIR)
    conn.commit()

    print(f"\n导入完成: {count}个建筑")
    if failed:
        print(f"失败: {failed}")

    # 显示统计
    show_stats(cursor)

    # 测试搜索
    print("\n" + "=" * 60)
    print("测试搜索功能")
    print("=" * 60)

    results = search_buildings(cursor, style="中式", limit=5)
    print(f"\n搜索 '中式' 风格: 找到 {len(results)}个建筑")
    for r in results:
        print(f"  - {r['id']}: {r['building_type']}, {r['style']}, {r['width']}x{r['height']}")

    # 测试获取详情
    if results:
        print("\n" + "=" * 60)
        print("测试获取详情")
        print("=" * 60)

        detail = get_building_detail(cursor, results[0]['id'])
        print(f"\n建筑ID: {detail['id']}")
        print(f"类型: {detail['building_type']}, 风格: {detail['style']}")
        print(f"尺寸: {detail['width']}x{detail['height']}")
        print(f"特征标签: {detail['feature_tags']}")
        print(f"简介: {detail['summary'][:50]}..." if detail.get('summary') else "简介: 无")
        print(f"主要方块: {[t['tile_name'] for t in detail['tiles'][:5]]}")

    conn.close()

    print("\n" + "=" * 60)
    print("完成!")
    print("=" * 60)


if __name__ == "__main__":
    main()