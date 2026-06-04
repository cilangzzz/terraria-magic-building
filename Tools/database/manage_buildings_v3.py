#!/usr/bin/env python3
"""
构件级建筑数据库管理脚本 v3
支持多层次架构: 原子构件 → 复合构件 → 建筑 → 建筑群
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
    """创建构件级建筑数据库表"""

    # 1. 建筑索引表 (向量检索)
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS building_index (
        id TEXT PRIMARY KEY,
        name TEXT,
        source_id TEXT,
        vector TEXT,
        vector_model TEXT,
        searchable_text TEXT,
        complexity_level TEXT,
        building_type TEXT,
        style TEXT,
        size_category TEXT,
        width_range TEXT,
        height_range TEXT,
        summary TEXT,
        created_at TEXT DEFAULT CURRENT_TIMESTAMP
    )
    """)

    # 2. 建筑实体表 (层次2)
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS buildings (
        id TEXT PRIMARY KEY,
        building_type TEXT,
        structure_type TEXT,
        width INTEGER,
        height INTEGER,
        stories INTEGER,
        style_tags TEXT,
        structure TEXT,
        components TEXT,
        build_sequence TEXT,
        npc_valid INTEGER,
        npc_requirements TEXT,
        source_file TEXT,
        original_id TEXT,
        summary TEXT,
        created_at TEXT DEFAULT CURRENT_TIMESTAMP
    )
    """)

    # 3. 原子构件表 (层次0)
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS atomic_components (
        id TEXT PRIMARY KEY,
        type TEXT NOT NULL,
        subtype TEXT,
        shape TEXT,
        tier_count INTEGER,
        base_width INTEGER,
        height_per_tier INTEGER,
        thickness INTEGER,
        overhang INTEGER,
        bounds_relative TEXT,
        bounds_absolute TEXT,
        materials TEXT,
        generation_rule TEXT,
        pattern TEXT,
        spacing INTEGER,
        placement TEXT,
        tile_count INTEGER,
        wall_count INTEGER,
        source_building TEXT,
        created_at TEXT DEFAULT CURRENT_TIMESTAMP
    )
    """)

    cursor.execute("CREATE INDEX IF NOT EXISTS idx_atomic_type ON atomic_components(type)")
    cursor.execute("CREATE INDEX IF NOT EXISTS idx_atomic_subtype ON atomic_components(subtype)")
    cursor.execute("CREATE INDEX IF NOT EXISTS idx_atomic_source ON atomic_components(source_building)")

    # 4. 复合构件表 (层次1)
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS composite_components (
        id TEXT PRIMARY KEY,
        type TEXT NOT NULL,
        min_width INTEGER,
        min_height INTEGER,
        min_area INTEGER,
        atomic_components TEXT,
        requirements TEXT,
        generation_rule TEXT,
        source_building TEXT,
        created_at TEXT DEFAULT CURRENT_TIMESTAMP
    )
    """)

    # 5. 建筑群表 (层次3)
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS complexes (
        id TEXT PRIMARY KEY,
        complex_type TEXT,
        width INTEGER,
        height INTEGER,
        buildings TEXT,
        shared_elements TEXT,
        style_tags TEXT,
        summary TEXT,
        created_at TEXT DEFAULT CURRENT_TIMESTAMP
    )
    """)

    # 6. 风格材料映射表
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS style_materials (
        style TEXT PRIMARY KEY,
        tiles TEXT,
        walls TEXT,
        decorations TEXT,
        doors TEXT,
        furniture TEXT,
        description TEXT,
        created_at TEXT DEFAULT CURRENT_TIMESTAMP
    )
    """)

    # 7. 向量索引表
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS vectors (
        id TEXT PRIMARY KEY,
        entity_type TEXT NOT NULL,
        entity_level TEXT,
        vector TEXT,
        vector_model TEXT,
        vector_dimension INTEGER,
        keywords TEXT,
        searchable_text TEXT,
        created_at TEXT DEFAULT CURRENT_TIMESTAMP
    )
    """)

    cursor.execute("CREATE INDEX IF NOT EXISTS idx_vectors_type ON vectors(entity_type)")
    cursor.execute("CREATE INDEX IF NOT EXISTS idx_vectors_level ON vectors(entity_level)")

    # 8. 原始数据表
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS raw_data (
        id TEXT PRIMARY KEY,
        tedit_json TEXT,
        data_building_json TEXT,
        description_md TEXT,
        created_at TEXT DEFAULT CURRENT_TIMESTAMP
    )
    """)

    print("✓ 数据库表创建完成")
    print("  - building_index: 建筑索引表(向量检索)")
    print("  - buildings: 建筑实体表(层次2)")
    print("  - atomic_components: 原子构件表(层次0)")
    print("  - composite_components: 复合构件表(层次1)")
    print("  - complexes: 建筑群表(层次3)")
    print("  - style_materials: 风格材料映射表")
    print("  - vectors: 向量索引表")
    print("  - raw_data: 原始数据表")


def import_building_data(cursor, building_dir):
    """导入建筑数据 (支持多种JSON格式)"""

    building_id = os.path.basename(building_dir)

    # 查找JSON文件
    json_files = [f for f in os.listdir(building_dir) if f.endswith('.json')]
    if not json_files:
        print(f"  跳过 {building_id}: 未找到JSON文件")
        return None

    # 优先使用特定文件
    json_file = None
    for f in json_files:
        if 'TEditSch' in f:
            json_file = f
            break
    if not json_file:
        for f in json_files:
            if 'building' in f.lower():
                json_file = f
                break
    if not json_file:
        json_file = json_files[0]

    json_path = os.path.join(building_dir, json_file)

    # 查找描述文件
    md_path = os.path.join(building_dir, '建筑整体描述.md')
    desc_data = parse_description_md(md_path)

    # 解析JSON
    with open(json_path, 'r', encoding='utf-8') as f:
        json_data = json.load(f)

    # 提取数据 (支持多种格式)
    building = {}
    components = {}
    complexity = {'level': 'building', 'subtype': 'unknown', 'confidence': 0.5}

    # 检测JSON格式
    if 'building' in json_data:
        # data_building.json 格式
        building = json_data.get('building', {})
        components = json_data.get('components', {})
        complexity = json_data.get('complexity', complexity)
    elif 'header' in json_data:
        # TEditSch.json 解析格式
        header = json_data.get('header', {})
        tile_stats = json_data.get('tile_stats', {})
        wall_stats = json_data.get('wall_stats', {})

        building = {
            'id': header.get('name', building_id),
            'building_type': 'unknown',
            'dimensions': {
                'width': header.get('width', 0),
                'height': header.get('height', 0)
            },
            'style_tags': [],
            'summary': ''
        }

        # 从描述文件补充信息
        if desc_data:
            building['building_type'] = desc_data.get('type', 'unknown')
            building['style_tags'] = [desc_data.get('style', 'unknown')]
            building['summary'] = desc_data.get('summary', '')

        # 从tile_stats生成构件
        components = generate_components_from_tile_stats(tile_stats, wall_stats, building_id)

    elif 'building_info' in json_data:
        # output/*.json 格式
        info = json_data.get('building_info', {})
        dims = json_data.get('dimensions', {})
        desc = json_data.get('description', {})
        tile_stats = json_data.get('tile_stats', {})
        wall_stats = json_data.get('wall_stats', {})

        building = {
            'id': info.get('name', building_id),
            'building_type': desc.get('type', 'unknown'),
            'dimensions': dims,
            'style_tags': desc.get('feature_tags', []),
            'summary': desc.get('summary', '')
        }

        components = generate_components_from_tile_stats(tile_stats, wall_stats, building_id)

    # 获取尺寸
    dims = building.get('dimensions', {})
    width = dims.get('width', 0)
    height = dims.get('height', 0)

    if width == 0 or height == 0:
        if 'header' in json_data:
            width = json_data['header'].get('width', 0)
            height = json_data['header'].get('height', 0)

    if desc_data and desc_data.get('width'):
        width = desc_data['width']
        height = desc_data['height']

    if width == 0 or height == 0:
        print(f"  跳过 {building_id}: 无法获取尺寸信息")
        return None

    # 生成构件级ID
    building_uuid = f"building_{building_id}"

    # 判断尺寸类别
    if width < 25 or height < 15:
        size_category = 'small'
    elif width < 60 or height < 40:
        size_category = 'medium'
    else:
        size_category = 'large'

    # 获取风格
    style_tags = building.get('style_tags', [])
    if desc_data and desc_data.get('style'):
        style = desc_data['style']
        if style not in style_tags:
            style_tags.insert(0, style)
    style = style_tags[0] if style_tags else 'unknown'

    # 获取类型
    building_type = building.get('building_type', 'unknown')
    if desc_data and desc_data.get('type'):
        building_type = desc_data['type']

    # 生成可搜索文本
    searchable_text = f"{building_type} {style} {' '.join(style_tags)} {building.get('summary', '')}"

    # 1. 插入建筑索引
    cursor.execute("""
        INSERT OR REPLACE INTO building_index
        (id, name, source_id, complexity_level, building_type, style,
         size_category, width_range, height_range, searchable_text, summary)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, (
        building_uuid,
        building.get('id', building_id),
        building_id,
        complexity.get('level', 'building'),
        building_type,
        style,
        size_category,
        json.dumps([max(0, width - 5), width + 5]),
        json.dumps([max(0, height - 5), height + 5]),
        searchable_text,
        building.get('summary', '')
    ))

    # 2. 插入建筑实体
    structure = building.get('structure', {})
    build_sequence = building.get('build_sequence', [])

    # 生成构件引用
    component_refs = []
    for comp_name in components.keys():
        comp = components[comp_name]
        component_refs.append({
            "ref": f"{building_uuid}_{comp_name}",
            "type": comp.get('type', 'unknown')
        })

    # 如果没有build_sequence，生成默认顺序
    if not build_sequence:
        build_sequence = ['foundation', 'wall_outer', 'floor_1', 'roof_main', 'deco_main']
        build_sequence = [c for c in build_sequence if c in components]

    # 检测楼层数
    stories = len([c for c in components if c.startswith('floor')])
    structure_type = 'multi_story' if stories > 1 else 'single_story'

    cursor.execute("""
        INSERT OR REPLACE INTO buildings
        (id, building_type, structure_type, width, height, stories, style_tags,
         structure, components, build_sequence, npc_valid, source_file, original_id, summary)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, (
        building_uuid,
        building_type,
        structure_type,
        width,
        height,
        max(1, stories),
        json.dumps(style_tags, ensure_ascii=False),
        json.dumps(structure, ensure_ascii=False),
        json.dumps(component_refs, ensure_ascii=False),
        json.dumps(build_sequence, ensure_ascii=False),
        0,
        json_file,
        building_id,
        building.get('summary', '')
    ))

    # 3. 插入原子构件
    for comp_name, comp_data in components.items():
        comp_id = f"{building_uuid}_{comp_name}"
        comp_type = comp_data.get('type', 'unknown')

        cursor.execute("""
            INSERT OR REPLACE INTO atomic_components
            (id, type, subtype, shape, bounds_relative, materials, generation_rule,
             tile_count, wall_count, source_building)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            comp_id,
            comp_type,
            comp_data.get('subtype', ''),
            comp_data.get('shape', ''),
            json.dumps(comp_data.get('bounds', {}), ensure_ascii=False),
            json.dumps(comp_data.get('materials', {}), ensure_ascii=False),
            json.dumps(comp_data.get('generation_rule', {}), ensure_ascii=False),
            comp_data.get('tile_count', 0),
            comp_data.get('wall_count', 0),
            building_uuid
        ))

    # 4. 插入向量索引
    keywords = ' '.join(style_tags) + f" {building_type} {complexity.get('subtype', '')}"
    cursor.execute("""
        INSERT OR REPLACE INTO vectors
        (id, entity_type, entity_level, keywords, searchable_text)
        VALUES (?, ?, ?, ?, ?)
    """, (
        building_uuid,
        'building',
        '2',
        keywords.strip(),
        searchable_text
    ))

    # 5. 存储原始数据
    with open(json_path, 'r', encoding='utf-8') as f:
        raw_json = f.read()

    description_md = None
    if os.path.exists(md_path):
        with open(md_path, 'r', encoding='utf-8') as f:
            description_md = f.read()

    cursor.execute("""
        INSERT OR REPLACE INTO raw_data
        (id, tedit_json, description_md)
        VALUES (?, ?, ?)
    """, (building_uuid, raw_json, description_md))

    print(f"✓ 导入建筑: {building_id}")
    print(f"  类型: {building_type}, 风格: {style}")
    print(f"  尺寸: {width}x{height}, 构件: {len(components)}个")

    return building_uuid


def parse_description_md(md_path):
    """解析建筑整体描述.md文件"""
    if not os.path.exists(md_path):
        return None

    with open(md_path, 'r', encoding='utf-8') as f:
        content = f.read()

    result = {}

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


def generate_components_from_tile_stats(tile_stats, wall_stats, building_id):
    """从tile_stats和wall_stats生成构件数据"""
    components = {}

    # 墙壁构件
    if wall_stats and wall_stats.get('types'):
        primary_wall = wall_stats['types'][0] if wall_stats['types'] else None
        if primary_wall:
            components['wall_outer'] = {
                'type': 'wall',
                'materials': {
                    'primary': {
                        'wall_id': primary_wall.get('id'),
                        'name': primary_wall.get('name', 'Unknown')
                    }
                },
                'wall_count': sum(w.get('count', 0) for w in wall_stats['types'])
            }

    # 地板构件
    if tile_stats and tile_stats.get('types'):
        platform_tiles = [t for t in tile_stats['types'] if 'Platform' in t.get('name', '') or t.get('id') == 19]
        if platform_tiles:
            components['floor_1'] = {
                'type': 'floor',
                'level': 1,
                'materials': {
                    'primary': {
                        'tile_id': platform_tiles[0].get('id'),
                        'name': platform_tiles[0].get('name', 'Platform')
                    }
                }
            }

        # 装饰构件
        deco_tiles = [t for t in tile_stats['types'] if any(kw in t.get('name', '') for kw in ['Lantern', 'Torch', 'Banner', 'Candle'])]
        if deco_tiles:
            components['deco_main'] = {
                'type': 'decoration',
                'types': [{
                    'tile_id': t.get('id'),
                    'name': t.get('name'),
                    'count': t.get('count', 0)
                } for t in deco_tiles[:5]]
            }

        # 屋顶构件 (顶部方块)
        top_tiles = [t for t in tile_stats['types'] if any(kw in t.get('name', '') for kw in ['Ice', 'Wood', 'Stone', 'Gold', 'Brick'])]
        if top_tiles:
            components['roof_main'] = {
                'type': 'roof',
                'shape': 'complex',
                'materials': {
                    'primary': {
                        'tile_id': top_tiles[0].get('id'),
                        'name': top_tiles[0].get('name')
                    }
                },
                'tile_count': top_tiles[0].get('count', 0)
            }

    # 基础构件
    components['foundation'] = {
        'type': 'foundation',
        'materials': {
            'primary': {
                'tile_id': 0,
                'name': 'Dirt'
            }
        }
    }

    return components

    cursor.execute("""
        INSERT OR REPLACE INTO building_index
        (id, name, source_id, complexity_level, building_type, style,
         size_category, width_range, height_range, searchable_text, summary)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, (
        building_uuid,
        building.get('id', building_id),
        building_id,
        complexity_level,
        building_type,
        style,
        size_category,
        json.dumps([width - 5, width + 5]),
        json.dumps([height - 5, height + 5]),
        searchable_text,
        building.get('summary', '')
    ))

    # 2. 插入建筑实体
    structure = building.get('structure', {})
    build_sequence = building.get('build_sequence', [])

    # 转换结构为构件引用格式
    component_refs = []
    for comp_name in build_sequence:
        if comp_name in components:
            comp = components[comp_name]
            component_refs.append({
                "ref": f"{building_uuid}_{comp_name}",
                "type": comp.get('type', 'unknown')
            })

    # NPC验证
    npc_valid = 1 if building.get('npc_valid', False) else 0

    # 检测楼层数
    stories = len(structure.get('floors', [])) if isinstance(structure.get('floors'), list) else 1
    structure_type = 'multi_story' if stories > 1 else 'single_story'

    cursor.execute("""
        INSERT OR REPLACE INTO buildings
        (id, building_type, structure_type, width, height, stories, style_tags,
         structure, components, build_sequence, npc_valid, npc_requirements,
         source_file, original_id, summary)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, (
        building_uuid,
        building_type,
        structure_type,
        width,
        height,
        stories,
        json.dumps(style_tags, ensure_ascii=False),
        json.dumps(structure, ensure_ascii=False),
        json.dumps(component_refs, ensure_ascii=False),
        json.dumps(build_sequence, ensure_ascii=False),
        npc_valid,
        json.dumps({}, ensure_ascii=False),
        data.get('source', ''),
        building_id,
        building.get('summary', '')
    ))

    # 3. 插入原子构件
    for comp_name, comp_data in components.items():
        comp_id = f"{building_uuid}_{comp_name}"
        comp_type = comp_data.get('type', 'unknown')

        bounds = comp_data.get('bounds', {})
        materials = comp_data.get('materials', {})

        # 提取生成规则
        gen_rule = {}
        if comp_type == 'roof':
            gen_rule = {
                "pattern": "pyramid_tiered" if comp_data.get('shape') == 'complex' else "simple",
                "params": ["base_width", "height_per_tier"]
            }
        elif comp_type == 'wall':
            gen_rule = {
                "pattern": "filled_rectangle",
                "params": ["width", "height", "thickness"]
            }
        elif comp_type == 'decoration':
            gen_rule = {
                "pattern": "linear_spacing",
                "params": ["start_x", "end_x", "y", "spacing"]
            }

        cursor.execute("""
            INSERT OR REPLACE INTO atomic_components
            (id, type, subtype, shape, bounds_relative, bounds_absolute,
             materials, generation_rule, tile_count, wall_count, source_building)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            comp_id,
            comp_type,
            comp_data.get('subtype', ''),
            comp_data.get('shape', ''),
            json.dumps(bounds, ensure_ascii=False),
            json.dumps(bounds, ensure_ascii=False),
            json.dumps(materials, ensure_ascii=False),
            json.dumps(gen_rule, ensure_ascii=False),
            comp_data.get('tile_count', 0),
            comp_data.get('wall_count', 0),
            building_uuid
        ))

    # 4. 插入向量索引
    keywords = ' '.join(style_tags) + f" {building_type} {complexity.get('subtype', '')}"
    cursor.execute("""
        INSERT OR REPLACE INTO vectors
        (id, entity_type, entity_level, keywords, searchable_text)
        VALUES (?, ?, ?, ?, ?)
    """, (
        building_uuid,
        'building',
        '2',
        keywords.strip(),
        searchable_text
    ))

    # 5. 存储原始数据
    with open(data_building_path, 'r', encoding='utf-8') as f:
        data_building_json = f.read()

    description_path = os.path.join(building_dir, "建筑整体描述.md")
    description_md = None
    if os.path.exists(description_path):
        with open(description_path, 'r', encoding='utf-8') as f:
            description_md = f.read()

    cursor.execute("""
        INSERT OR REPLACE INTO raw_data
        (id, data_building_json, description_md)
        VALUES (?, ?, ?)
    """, (building_uuid, data_building_json, description_md))

    print(f"✓ 导入建筑: {building_id}")
    print(f"  层次: {complexity_level}, 类型: {building_type}")
    print(f"  尺寸: {width}x{height}, 构件: {len(components)}个")
    print(f"  风格: {style}, 标签: {style_tags}")

    return building_uuid


def import_all_buildings(cursor, base_dir):
    """导入所有建筑数据"""
    count = 0
    failed = []

    for item in sorted(os.listdir(base_dir)):
        item_path = os.path.join(base_dir, item)

        if not os.path.isdir(item_path):
            continue
        if item in ['doc', 'output', '.claude']:
            continue
        if not item[0].isdigit():
            continue

        print(f"\n处理: {item}")
        try:
            result = import_building_data(cursor, item_path)
            if result:
                count += 1
            else:
                failed.append(item)
        except Exception as e:
            print(f"  错误: {e}")
            failed.append(item)

    return count, failed


def insert_default_style_materials(cursor):
    """插入默认风格材料映射"""

    styles = [
        ('asian',
         '{"primary": [{"id": 179, "name": "Gold", "use": "roof, accent"}, {"id": 353, "name": "Dynasty Wood", "use": "floor, frame"}], "accent": [{"id": 215, "name": "Red Brick", "use": "detail"}]}',
         '[{"id": 172, "name": "Marble Wall"}, {"id": 154, "name": "Ebonwood Wall"}]',
         '[{"id": 312, "name": "Pine Lantern"}, {"id": 395, "name": "Chinese Lantern"}]',
         '[{"id": 104, "name": "Cactus Door"}]',
         '[{"id": 46, "name": "Table", "material": "Dynasty Wood"}]',
         '中式风格：金色装饰、大理石墙、灯笼'),
        ('medieval',
         '{"primary": [{"id": 1, "name": "Stone"}, {"id": 5, "name": "Stone Slab"}], "accent": [{"id": 4, "name": "Wood"}]}',
         '[{"id": 1, "name": "Stone Wall"}, {"id": 6, "name": "Red Brick Wall"}]',
         '[{"id": 10, "name": "Torch"}, {"id": 33, "name": "Banner"}]',
         '[{"id": 14, "name": "Wooden Door"}]',
         '[{"id": 46, "name": "Table", "material": "Wood"}]',
         '中世纪风格：石材、砖墙、火炬'),
        ('fantasy',
         '{"primary": [{"id": 182, "name": "Pearlstone"}, {"id": 179, "name": "Gold"}]}',
         '[{"id": 24, "name": "Glass Wall"}, {"id": 73, "name": "Obsidian Wall"}]',
         '[{"id": 1045, "name": "Crystal"}]',
         '[{"id": 14, "name": "Glass Door"}]',
         '[{"id": 46, "name": "Table", "material": "Pearlwood"}]',
         '奇幻风格：珍珠石、玻璃墙、水晶'),
        ('日式',
         '{"primary": [{"id": 353, "name": "Dynasty Wood"}], "accent": [{"id": 179, "name": "Gold"}]}',
         '[{"id": 154, "name": "Ebonwood Wall"}]',
         '[{"id": 312, "name": "Pine Lantern"}, {"id": 501, "name": "Paper Lantern"}]',
         '[{"id": 104, "name": "Cactus Door"}]',
         '[{"id": 46, "name": "Table", "material": "Dynasty Wood"}]',
         '日式风格：王朝木、纸灯笼'),
    ]

    for style_data in styles:
        cursor.execute("""
            INSERT OR REPLACE INTO style_materials
            (style, tiles, walls, decorations, doors, furniture, description)
            VALUES (?, ?, ?, ?, ?, ?, ?)
        """, style_data)

    print(f"✓ 插入 {len(styles)} 种风格材料映射")


def search_buildings(cursor, style=None, building_type=None, keywords=None, complexity=None, limit=10):
    """搜索建筑"""

    sql = "SELECT * FROM building_index WHERE 1=1"
    params = []

    if style:
        sql += " AND style LIKE ?"
        params.append(f"%{style}%")

    if building_type:
        sql += " AND building_type = ?"
        params.append(building_type)

    if keywords:
        sql += " AND searchable_text LIKE ?"
        params.append(f"%{keywords}%")

    if complexity:
        sql += " AND complexity_level = ?"
        params.append(complexity)

    sql += f" ORDER BY created_at DESC LIMIT {limit}"

    cursor.execute(sql, params)

    columns = [desc[0] for desc in cursor.description]
    return [dict(zip(columns, row)) for row in cursor.fetchall()]


def get_building_with_components(cursor, building_id):
    """获取建筑及其构件详情"""

    # 获取建筑索引
    cursor.execute("SELECT * FROM building_index WHERE id = ?", (building_id,))
    row = cursor.fetchone()
    if not row:
        return None

    columns = [desc[0] for desc in cursor.description]
    building = dict(zip(columns, row))

    # 获取建筑实体
    cursor.execute("SELECT * FROM buildings WHERE id = ?", (building_id,))
    row = cursor.fetchone()
    if row:
        columns = [desc[0] for desc in cursor.description]
        entity = dict(zip(columns, row))

        # 解析JSON字段
        for field in ['style_tags', 'structure', 'components', 'build_sequence', 'npc_requirements']:
            if entity.get(field):
                try:
                    entity[field] = json.loads(entity[field])
                except:
                    pass

        building['entity'] = entity

    # 获取构件
    cursor.execute("SELECT * FROM atomic_components WHERE source_building = ?", (building_id,))
    components = []
    for row in cursor.fetchall():
        columns = [desc[0] for desc in cursor.description]
        comp = dict(zip(columns, row))
        for field in ['bounds_relative', 'bounds_absolute', 'materials', 'generation_rule']:
            if comp.get(field):
                try:
                    comp[field] = json.loads(comp[field])
                except:
                    pass
        components.append(comp)

    building['components'] = components

    return building


def show_stats(cursor):
    """显示数据库统计"""

    stats = {
        'building_index': 0,
        'buildings': 0,
        'atomic_components': 0,
        'composite_components': 0,
        'complexes': 0,
        'style_materials': 0,
        'vectors': 0
    }

    for table in stats.keys():
        cursor.execute(f"SELECT COUNT(*) FROM {table}")
        stats[table] = cursor.fetchone()[0]

    print("\n" + "=" * 60)
    print("数据库统计")
    print("=" * 60)
    print(f"建筑索引: {stats['building_index']}")
    print(f"建筑实体: {stats['buildings']}")
    print(f"原子构件: {stats['atomic_components']}")
    print(f"复合构件: {stats['composite_components']}")
    print(f"建筑群: {stats['complexes']}")
    print(f"风格映射: {stats['style_materials']}")
    print(f"向量记录: {stats['vectors']}")

    if stats['building_index'] > 0:
        # 按层次统计
        cursor.execute("""
            SELECT complexity_level, COUNT(*) as cnt
            FROM building_index
            GROUP BY complexity_level
            ORDER BY cnt DESC
        """)
        print("\n复杂性层次分布:")
        for row in cursor.fetchall():
            print(f"  {row[0]}: {row[1]}")

        # 按类型统计
        cursor.execute("""
            SELECT building_type, COUNT(*) as cnt
            FROM building_index
            GROUP BY building_type
            ORDER BY cnt DESC
        """)
        print("\n建筑类型分布:")
        for row in cursor.fetchall():
            print(f"  {row[0]}: {row[1]}")

        # 按风格统计
        cursor.execute("""
            SELECT style, COUNT(*) as cnt
            FROM building_index
            GROUP BY style
            ORDER BY cnt DESC
            LIMIT 10
        """)
        print("\n建筑风格分布:")
        for row in cursor.fetchall():
            print(f"  {row[0]}: {row[1]}")

        # 构件类型统计
        cursor.execute("""
            SELECT type, COUNT(*) as cnt
            FROM atomic_components
            GROUP BY type
            ORDER BY cnt DESC
        """)
        print("\n构件类型统计:")
        for row in cursor.fetchall():
            print(f"  {row[0]}: {row[1]}")


def main():
    print("=" * 60)
    print("构件级建筑数据库管理 v3")
    print("=" * 60)

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    # 创建表
    create_tables(cursor)
    conn.commit()

    # 插入风格材料映射
    print("\n" + "=" * 60)
    print("插入风格材料映射")
    print("=" * 60)
    insert_default_style_materials(cursor)
    conn.commit()

    # 导入建筑数据
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

    results = search_buildings(cursor, style="asian", limit=5)
    print(f"\n搜索 'asian' 风格: 找到 {len(results)}个建筑")
    for r in results:
        print(f"  - {r['id']}: {r['building_type']}, {r['style']}, {r['size_category']}")

    # 测试获取详情
    if results:
        print("\n" + "=" * 60)
        print("测试获取详情")
        print("=" * 60)

        detail = get_building_with_components(cursor, results[0]['id'])
        if detail:
            print(f"\n建筑ID: {detail['id']}")
            print(f"类型: {detail['building_type']}, 层次: {detail['complexity_level']}")
            print(f"风格: {detail['style']}, 尺寸: {detail.get('entity', {}).get('width')}x{detail.get('entity', {}).get('height')}")
            print(f"构件数量: {len(detail.get('components', []))}")
            for comp in detail.get('components', [])[:5]:
                print(f"  - {comp['type']}: {comp['id']}")

    conn.close()

    print("\n" + "=" * 60)
    print("完成!")
    print("=" * 60)


if __name__ == "__main__":
    main()