#!/usr/bin/env python3
"""
建筑实体数据库管理脚本
支持从vector_data.json导入建筑实体到数据库
"""

import sqlite3
import json
import os
import sys
from datetime import datetime

sys.stdout.reconfigure(encoding='utf-8')

DATA_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Data", "kb")
DB_PATH = os.path.join(DATA_DIR, "terraria_kb.db")


def create_building_entity_tables(cursor):
    """创建建筑实体相关表"""

    # 1. 建筑实体主表
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS building_entities (
        id TEXT PRIMARY KEY,
        source TEXT,
        width INTEGER NOT NULL,
        height INTEGER NOT NULL,
        building_type TEXT,
        style TEXT,
        progress TEXT,
        complexity TEXT,
        structure_type TEXT,
        style_tags TEXT,
        color_tone_primary TEXT,
        color_tone_colors TEXT,
        biome_match TEXT,
        npc_valid INTEGER,
        npc_has_light INTEGER,
        npc_has_flat_surface INTEGER,
        npc_has_comfort INTEGER,
        npc_has_entry INTEGER,
        npc_has_walls INTEGER,
        functions_light TEXT,
        functions_entry TEXT,
        functions_storage TEXT,
        functions_furniture TEXT,
        functions_platform TEXT,
        summary TEXT,
        building_sequence TEXT,
        schematic_id INTEGER,
        created_at TEXT,
        updated_at TEXT
    )
    """)

    # 2. 建筑材料表
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS building_materials (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        building_id TEXT NOT NULL,
        material_type TEXT NOT NULL,
        material_id INTEGER NOT NULL,
        material_name TEXT,
        material_count INTEGER,
        material_ratio REAL,
        is_primary INTEGER,
        FOREIGN KEY (building_id) REFERENCES building_entities(id)
    )
    """)

    cursor.execute("CREATE INDEX IF NOT EXISTS idx_building_materials_building ON building_materials(building_id)")
    cursor.execute("CREATE INDEX IF NOT EXISTS idx_building_materials_type ON building_materials(material_type)")

    # 3. 建筑向量表
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS building_vectors (
        building_id TEXT PRIMARY KEY,
        vector TEXT,
        vector_model TEXT,
        vector_dimension INTEGER,
        keywords TEXT,
        FOREIGN KEY (building_id) REFERENCES building_entities(id)
    )
    """)

    # 4. 建筑详细数据表 - 存储方块分布等详细信息
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS building_details (
        building_id TEXT PRIMARY KEY,
        total_tiles INTEGER,
        active_tiles INTEGER,
        unique_tile_types INTEGER,
        unique_wall_types INTEGER,
        tile_distribution TEXT,
        wall_distribution TEXT,
        tiles_sample TEXT,
        raw_json TEXT,
        FOREIGN KEY (building_id) REFERENCES building_entities(id)
    )
    """)

    print("✓ 建筑实体表创建完成")
    print("  - building_entities: 建筑实体主表")
    print("  - building_materials: 材料清单表")
    print("  - building_vectors: 向量索引表")
    print("  - building_details: 详细数据表")


def import_building_from_vector_data(cursor, json_path):
    """从vector_data.json导入建筑实体，同时加载data.json的详细数据"""

    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    building_id = data.get('id')
    if not building_id:
        print("错误: 缺少建筑ID")
        return None

    # 检查是否已存在
    cursor.execute("SELECT id FROM building_entities WHERE id = ?", (building_id,))
    if cursor.fetchone():
        print(f"建筑 {building_id} 已存在，跳过")
        return building_id

    # 1. 插入建筑实体主表
    dims = data.get('dimensions', {})
    features = data.get('features', {})
    npc = data.get('npc_suitable', {})
    functions = data.get('functions', {})

    cursor.execute("""
        INSERT INTO building_entities
        (id, source, width, height, building_type, style, progress, complexity, structure_type,
         style_tags, color_tone_primary, color_tone_colors, biome_match,
         npc_valid, npc_has_light, npc_has_flat_surface, npc_has_comfort, npc_has_entry, npc_has_walls,
         functions_light, functions_entry, functions_storage, functions_furniture, functions_platform,
         summary, building_sequence, created_at)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, (
        building_id,
        data.get('source'),
        dims.get('width', 0),
        dims.get('height', 0),
        features.get('type'),
        features.get('style'),
        features.get('progress'),
        features.get('complexity'),
        features.get('structure'),
        json.dumps(data.get('style_tags', [])),
        data.get('color_tone', {}).get('primary'),
        json.dumps(data.get('color_tone', {}).get('colors', [])),
        json.dumps(data.get('biome_match', [])),
        1 if npc.get('is_valid_house') else 0,
        1 if npc.get('has_light') else 0,
        1 if npc.get('has_flat_surface') else 0,
        1 if npc.get('has_comfort') else 0,
        1 if npc.get('has_entry') else 0,
        1 if npc.get('has_walls') else 0,
        json.dumps(functions.get('light_source')),
        json.dumps(functions.get('entry')),
        json.dumps(functions.get('storage')),
        json.dumps(functions.get('furniture')),
        json.dumps(functions.get('platform')),
        data.get('summary'),
        json.dumps(data.get('building_sequence', [])),
        datetime.now().isoformat()
    ))

    # 2. 插入材料清单
    materials = data.get('materials', {})
    total_tiles = sum(t.get('count', 0) for t in materials.get('primary_tiles', []))
    total_walls = sum(w.get('count', 0) for w in materials.get('primary_walls', []))

    for i, tile in enumerate(materials.get('primary_tiles', [])[:5]):
        count = tile.get('count', 0)
        ratio = (count / total_tiles * 100) if total_tiles > 0 else 0
        cursor.execute("""
            INSERT INTO building_materials
            (building_id, material_type, material_id, material_name, material_count, material_ratio, is_primary)
            VALUES (?, ?, ?, ?, ?, ?, ?)
        """, (building_id, 'tile', tile.get('id'), tile.get('name'), count, round(ratio, 1), 1 if i < 3 else 0))

    for i, wall in enumerate(materials.get('primary_walls', [])[:5]):
        count = wall.get('count', 0)
        ratio = (count / total_walls * 100) if total_walls > 0 else 0
        cursor.execute("""
            INSERT INTO building_materials
            (building_id, material_type, material_id, material_name, material_count, material_ratio, is_primary)
            VALUES (?, ?, ?, ?, ?, ?, ?)
        """, (building_id, 'wall', wall.get('id'), wall.get('name'), count, round(ratio, 1), 1 if i < 3 else 0))

    # 3. 生成向量数据（模拟向量，后续可用API生成真实向量）
    keywords = ' '.join(data.get('style_tags', []))
    cursor.execute("""
        INSERT INTO building_vectors
        (building_id, vector, vector_model, vector_dimension, keywords)
        VALUES (?, ?, ?, ?, ?)
    """, (
        building_id,
        json.dumps(generate_mock_vector(building_id)),
        'mock-vector-v1',
        32,
        keywords
    ))

    # 4. 尝试加载data.json的详细数据
    dir_path = os.path.dirname(json_path)
    data_json_path = os.path.join(dir_path, 'data.json')

    if os.path.exists(data_json_path):
        try:
            with open(data_json_path, 'r', encoding='utf-8') as f:
                detail_data = json.load(f)

            tile_stats = detail_data.get('tile_stats', {})
            wall_stats = detail_data.get('wall_stats', {})

            cursor.execute("""
                INSERT INTO building_details
                (building_id, total_tiles, active_tiles, unique_tile_types, unique_wall_types,
                 tile_distribution, wall_distribution, tiles_sample, raw_json)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                building_id,
                tile_stats.get('total_tiles', 0),
                tile_stats.get('active_tiles', 0),
                tile_stats.get('unique_tile_types', 0),
                wall_stats.get('unique_wall_types', 0),
                json.dumps(tile_stats.get('tile_distribution', {})),
                json.dumps(wall_stats.get('wall_distribution', {})),
                json.dumps(detail_data.get('tiles_sample', [])[:100]),  # 只存前100个样本
                json.dumps(detail_data)  # 存储完整JSON
            ))

            print(f"✓ 导入建筑实体: {building_id}")
            print(f"  类型: {features.get('type')}, 风格: {features.get('style')}")
            print(f"  尺寸: {dims.get('width')}x{dims.get('height')}")
            print(f"  材料: {len(materials.get('primary_tiles', []))}种方块, {len(materials.get('primary_walls', []))}种墙壁")
            print(f"  详细数据: {tile_stats.get('total_tiles', 0)}方块, {tile_stats.get('unique_tile_types', 0)}种类型")

        except Exception as e:
            print(f"  警告: 加载详细数据失败: {e}")
            # 即使详细数据加载失败，也插入空记录
            cursor.execute("""
                INSERT INTO building_details (building_id)
                VALUES (?)
            """, (building_id,))
    else:
        print(f"  注意: 未找到data.json，跳过详细数据")
        cursor.execute("""
            INSERT INTO building_details (building_id)
            VALUES (?)
        """, (building_id,))

    return building_id


def generate_mock_vector(building_id):
    """生成模拟向量（基于ID哈希）"""
    import hashlib
    hash_val = int(hashlib.md5(building_id.encode()).hexdigest()[:8], 16)
    vector = []
    for i in range(32):
        val = ((hash_val >> (i % 8)) & 0xFF) / 255.0 * 0.5 + 0.5
        vector.append(round(val, 2))
    return vector


def import_all_buildings_from_directory(cursor, directory):
    """从目录导入所有建筑实体"""

    count = 0
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file == 'vector_data.json':
                json_path = os.path.join(root, file)
                print(f"\n处理: {json_path}")
                try:
                    if import_building_from_vector_data(cursor, json_path):
                        count += 1
                except Exception as e:
                    print(f"错误: {e}")

    return count


def search_buildings_by_style(cursor, style, top_k=5):
    """按风格搜索建筑"""

    cursor.execute("""
        SELECT be.id, be.source, be.width, be.height, be.building_type, be.style, be.summary, be.style_tags
        FROM building_entities be
        WHERE be.style LIKE ? OR be.style_tags LIKE ? OR be.building_type LIKE ?
        ORDER BY be.width DESC
        LIMIT ?
    """, (f'%{style}%', f'%{style}%', f'%{style}%', top_k))

    results = []
    for row in cursor.fetchall():
        results.append({
            'id': row[0],
            'source': row[1],
            'dimensions': {'width': row[2], 'height': row[3]},
            'building_type': row[4],
            'style': row[5],
            'summary': row[6],
            'style_tags': json.loads(row[7]) if row[7] else []
        })

    return results


def get_building_sequence(cursor, building_id):
    """获取建筑建造顺序"""

    cursor.execute("""
        SELECT building_sequence, summary
        FROM building_entities
        WHERE id = ?
    """, (building_id,))

    row = cursor.fetchone()
    if not row:
        return None

    sequence = json.loads(row[0]) if row[0] else []

    # 获取材料清单
    cursor.execute("""
        SELECT material_type, material_id, material_name, material_count, is_primary
        FROM building_materials
        WHERE building_id = ?
        ORDER BY is_primary DESC, material_count DESC
    """, (building_id,))

    materials = {'tile': [], 'wall': []}
    for m in cursor.fetchall():
        mtype = m[0]  # 'tile' or 'wall'
        if mtype in materials:
            materials[mtype].append({
                'id': m[1],
                'name': m[2],
                'count': m[3],
                'is_primary': m[4]
            })

    return {
        'building_id': building_id,
        'summary': row[1],
        'building_sequence': sequence,
        'materials': materials
    }


def get_building_detail_by_id(cursor, building_id):
    """通过ID获取建筑实体的完整JSON数据（包括详细方块分布）"""

    # 获取主表数据
    cursor.execute("""
        SELECT * FROM building_entities WHERE id = ?
    """, (building_id,))
    row = cursor.fetchone()

    if not row:
        return None

    columns = [desc[0] for desc in cursor.description]
    entity = dict(zip(columns, row))

    # 解析JSON字段
    entity['style_tags'] = json.loads(entity['style_tags']) if entity['style_tags'] else []
    entity['biome_match'] = json.loads(entity['biome_match']) if entity['biome_match'] else []
    entity['color_tone_colors'] = json.loads(entity['color_tone_colors']) if entity['color_tone_colors'] else []
    entity['building_sequence'] = json.loads(entity['building_sequence']) if entity['building_sequence'] else []
    entity['functions_light'] = json.loads(entity['functions_light']) if entity['functions_light'] else None
    entity['functions_entry'] = json.loads(entity['functions_entry']) if entity['functions_entry'] else None
    entity['functions_storage'] = json.loads(entity['functions_storage']) if entity['functions_storage'] else None
    entity['functions_furniture'] = json.loads(entity['functions_furniture']) if entity['functions_furniture'] else None
    entity['functions_platform'] = json.loads(entity['functions_platform']) if entity['functions_platform'] else None

    # 获取材料清单
    cursor.execute("""
        SELECT material_type, material_id, material_name, material_count, material_ratio, is_primary
        FROM building_materials
        WHERE building_id = ?
        ORDER BY is_primary DESC, material_count DESC
    """, (building_id,))

    tiles = []
    walls = []
    for m in cursor.fetchall():
        item = {
            'id': m[1],
            'name': m[2],
            'count': m[3],
            'ratio': m[4],
            'is_primary': bool(m[5])
        }
        if m[0] == 'tile':
            tiles.append(item)
        else:
            walls.append(item)

    entity['materials'] = {
        'primary_tiles': tiles,
        'primary_walls': walls
    }

    # 获取详细数据
    cursor.execute("""
        SELECT total_tiles, active_tiles, unique_tile_types, unique_wall_types,
               tile_distribution, wall_distribution, tiles_sample
        FROM building_details
        WHERE building_id = ?
    """, (building_id,))

    detail_row = cursor.fetchone()
    if detail_row:
        entity['detail'] = {
            'total_tiles': detail_row[0],
            'active_tiles': detail_row[1],
            'unique_tile_types': detail_row[2],
            'unique_wall_types': detail_row[3],
            'tile_distribution': json.loads(detail_row[4]) if detail_row[4] else {},
            'wall_distribution': json.loads(detail_row[5]) if detail_row[5] else {},
            'tiles_sample': json.loads(detail_row[6]) if detail_row[6] else []
        }

    return entity


def search_buildings_by_vector(cursor, query_keywords, top_k=5):
    """通过关键词（模拟向量）搜索建筑，返回ID列表"""

    # 简单的关键词匹配搜索
    # TODO: 后续替换为真正的向量相似度搜索
    keywords = query_keywords.lower().split()

    cursor.execute("""
        SELECT building_id, keywords
        FROM building_vectors
    """)

    results = []
    for row in cursor.fetchall():
        building_id = row[0]
        stored_keywords = (row[1] or '').lower()

        # 计算关键词匹配分数
        score = sum(1 for kw in keywords if kw in stored_keywords)

        if score > 0:
            results.append((building_id, score))

    # 按分数排序
    results.sort(key=lambda x: x[1], reverse=True)

    return [r[0] for r in results[:top_k]]


def show_stats(cursor):
    """显示数据库统计"""

    cursor.execute("SELECT COUNT(*) FROM building_entities")
    building_count = cursor.fetchone()[0]

    cursor.execute("SELECT COUNT(*) FROM building_materials")
    material_count = cursor.fetchone()[0]

    cursor.execute("SELECT COUNT(*) FROM building_vectors")
    vector_count = cursor.fetchone()[0]

    cursor.execute("SELECT COUNT(*) FROM building_details")
    detail_count = cursor.fetchone()[0]

    print("\n=== 建筑实体数据库统计 ===")
    print(f"建筑实体数: {building_count}")
    print(f"材料记录数: {material_count}")
    print(f"向量记录数: {vector_count}")
    print(f"详细数据数: {detail_count}")

    if building_count > 0:
        cursor.execute("""
            SELECT building_type, COUNT(*) as cnt
            FROM building_entities
            GROUP BY building_type
            ORDER BY cnt DESC
        """)
        print("\n建筑类型分布:")
        for row in cursor.fetchall():
            print(f"  {row[0]}: {row[1]}")

        cursor.execute("""
            SELECT style, COUNT(*) as cnt
            FROM building_entities
            GROUP BY style
            ORDER BY cnt DESC
            LIMIT 5
        """)
        print("\n建筑风格分布:")
        for row in cursor.fetchall():
            print(f"  {row[0]}: {row[1]}")


def main():
    print("=" * 60)
    print("建筑实体数据库管理")
    print("=" * 60)

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    # 创建表
    create_building_entity_tables(cursor)

    # 导入示例数据
    example_dir = r"C:\Users\admin\Pictures\Camera Roll\20260602215014"
    if os.path.exists(example_dir):
        print("\n=== 导入建筑实体 ===")
        count = import_all_buildings_from_directory(cursor, example_dir)
        print(f"\n导入完成: {count}个建筑")

    # 显示统计
    show_stats(cursor)

    # 测试搜索
    print("\n=== 测试搜索 ===")
    results = search_buildings_by_style(cursor, 'asian', 3)
    print(f"搜索 'asian': 找到 {len(results)}个建筑")
    for r in results:
        print(f"  - {r['id']}: {r['style']} {r['dimensions']['width']}x{r['dimensions']['height']}")

    # 测试获取建造顺序
    if results:
        print("\n=== 测试建造顺序 ===")
        seq = get_building_sequence(cursor, results[0]['id'])
        print(f"建筑: {seq['building_id']}")
        print(f"描述: {seq['summary']}")
        print(f"建造步骤: {len(seq['building_sequence'])}步")
        for step in seq['building_sequence']:
            print(f"  {step['step']}. {step['action']}: {step['note']}")

    # 测试通过ID获取完整JSON
    if results:
        print("\n=== 测试获取完整JSON ===")
        entity = get_building_detail_by_id(cursor, results[0]['id'])
        print(f"建筑ID: {entity['id']}")
        print(f"尺寸: {entity['width']}x{entity['height']}")
        print(f"风格标签: {entity['style_tags']}")
        print(f"材料: {len(entity['materials']['primary_tiles'])}种方块")
        if 'detail' in entity and entity['detail']:
            print(f"方块分布: {entity['detail']['unique_tile_types']}种类型, {entity['detail']['total_tiles']}个方块")
            print(f"方块分布示例: {list(entity['detail']['tile_distribution'].items())[:3]}")

    # 测试向量搜索
    print("\n=== 测试向量搜索 ===")
    search_results = search_buildings_by_vector(cursor, "asian fantasy residence", 3)
    print(f"搜索 'asian fantasy residence': 找到 {len(search_results)}个建筑")
    for bid in search_results:
        print(f"  - {bid}")

    conn.commit()
    conn.close()

    print("\n" + "=" * 60)
    print("完成!")
    print("=" * 60)


if __name__ == "__main__":
    main()