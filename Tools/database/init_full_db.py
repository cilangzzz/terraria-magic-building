#!/usr/bin/env python3
"""
完整数据库初始化脚本
创建所有建筑相关表并填充数据，生成SQL文件
"""

import sqlite3
import json
import os
import sys
from datetime import datetime

sys.stdout.reconfigure(encoding='utf-8')

# 路径配置
DATA_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Data")
DB_PATH = os.path.join(DATA_DIR, "terraria_kb.db")
SQL_PATH = os.path.join(DATA_DIR, "terraria_kb_full.sql")


def create_tables(cursor):
    """创建所有表"""

    # 1. 方块表
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS tiles (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL UNIQUE,
            display_name TEXT,
            category TEXT,
            styles TEXT,
            biome_match TEXT,
            paint_compatible INTEGER DEFAULT 1,
            slope_compatible INTEGER DEFAULT 1,
            hardness INTEGER DEFAULT 50,
            light_emission INTEGER DEFAULT 0,
            description TEXT,
            source TEXT
        )
    """)

    # 2. 墙壁表
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS walls (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL UNIQUE,
            display_name TEXT,
            category TEXT,
            styles TEXT,
            paint_compatible INTEGER DEFAULT 1,
            is_natural INTEGER DEFAULT 0,
            description TEXT,
            source TEXT
        )
    """)

    # 3. 油漆表
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS paints (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL UNIQUE,
            display_name TEXT,
            color_hex TEXT,
            effect_type TEXT,
            description TEXT
        )
    """)

    # 4. 斜坡表
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS slopes (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL UNIQUE,
            display_name TEXT,
            description TEXT
        )
    """)

    # 5. 家具表
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS furniture (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL UNIQUE,
            display_name TEXT,
            category TEXT,
            width INTEGER DEFAULT 1,
            height INTEGER DEFAULT 1,
            npc_function TEXT,
            paint_compatible INTEGER DEFAULT 1,
            description TEXT,
            source TEXT
        )
    """)

    # 6. 光源表
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS light_sources (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL UNIQUE,
            display_name TEXT,
            category TEXT,
            width INTEGER DEFAULT 1,
            height INTEGER DEFAULT 1,
            light_radius INTEGER DEFAULT 10,
            npc_function TEXT,
            placement_type TEXT,
            description TEXT
        )
    """)

    # 7. 门表
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS doors (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL UNIQUE,
            display_name TEXT,
            category TEXT,
            width INTEGER DEFAULT 1,
            height INTEGER DEFAULT 3,
            npc_function TEXT,
            paint_compatible INTEGER DEFAULT 1,
            description TEXT
        )
    """)

    # 8. 风格模板表（建筑核心数据）
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS style_templates (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL UNIQUE,
            display_name TEXT,
            description TEXT,
            primary_tiles TEXT,
            primary_walls TEXT,
            accent_tiles TEXT,
            roof_style TEXT,
            roof_tiles TEXT,
            furniture_style TEXT,
            paint_scheme TEXT,
            architectural_rules TEXT,
            biome_recommendations TEXT,
            difficulty TEXT
        )
    """)

    # 9. NPC要求表（建筑核心数据）
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS npc_requirements (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            npc_name TEXT NOT NULL UNIQUE,
            display_name TEXT,
            spawn_condition TEXT,
            preferred_biome TEXT,
            disliked_biome TEXT,
            preferred_neighbors TEXT,
            disliked_neighbors TEXT,
            special_furniture TEXT
        )
    """)

    # 10. 房屋验证规则表（建筑核心数据）
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS house_validation (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            rule_name TEXT NOT NULL UNIQUE,
            rule_type TEXT,
            requirement TEXT,
            minimum_value INTEGER,
            required_elements TEXT,
            description TEXT
        )
    """)

    # 11. 生物群落表（建筑核心数据）
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS biomes (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL UNIQUE,
            display_name TEXT,
            category TEXT,
            characteristic_tiles TEXT,
            characteristic_walls TEXT,
            description TEXT
        )
    """)


def insert_paints(cursor):
    """插入油漆数据"""
    paints = [
        (0, 'None', '无油漆', None, 'none', '无油漆效果'),
        (1, 'Red', '红色', '#FF0000', 'color', '红色油漆'),
        (2, 'Orange', '橙色', '#FF8000', 'color', '橙色油漆'),
        (3, 'Yellow', '黄色', '#FFFF00', 'color', '黄色油漆'),
        (4, 'Lime', '青柠', '#80FF00', 'color', '青柠色油漆'),
        (5, 'Green', '绿色', '#00FF00', 'color', '绿色油漆'),
        (6, 'Teal', '青色', '#00FF80', 'color', '青色油漆'),
        (7, 'Cyan', '蓝绿', '#00FFFF', 'color', '蓝绿色油漆'),
        (8, 'SkyBlue', '天蓝', '#0080FF', 'color', '天蓝色油漆'),
        (9, 'Blue', '蓝色', '#0000FF', 'color', '蓝色油漆'),
        (10, 'Purple', '紫色', '#8000FF', 'color', '紫色油漆'),
        (11, 'Violet', '紫罗兰', '#FF00FF', 'color', '紫罗兰色油漆'),
        (12, 'Pink', '粉色', '#FF0080', 'color', '粉色油漆'),
        (13, 'Crimson', '深红', '#FF4040', 'color', '深红色油漆'),
        (14, 'Azure', '蔚蓝', '#4040FF', 'color', '蔚蓝色油漆'),
        (28, 'Shadow', '阴影', None, 'depth', '阴影油漆，增加建筑层次感'),
        (29, 'Negative', '反转', None, 'special', '反转颜色效果'),
        (30, 'White', '白色', '#FFFFFF', 'color', '白色油漆'),
        (31, 'Black', '黑色', '#000000', 'color', '黑色油漆'),
    ]
    cursor.executemany("INSERT OR IGNORE INTO paints VALUES (?, ?, ?, ?, ?, ?)", paints)
    return len(paints)


def insert_slopes(cursor):
    """插入斜坡数据"""
    slopes = [
        (0, 'Solid', '完整方块', '标准完整方块'),
        (1, 'HalfBlock', '半砖', '只有下半部分，可作为台阶'),
        (2, 'SlopeDownRight', '右上斜坡', '从左上到右下的斜坡'),
        (3, 'SlopeDownLeft', '左上斜坡', '从右上到左下的斜坡'),
        (4, 'SlopeUpRight', '右下斜坡', '从左下到右上的斜坡'),
        (5, 'SlopeUpLeft', '左下斜坡', '从右下到左上的斜坡'),
    ]
    cursor.executemany("INSERT OR IGNORE INTO slopes VALUES (?, ?, ?, ?)", slopes)
    return len(slopes)


def insert_furniture(cursor):
    """插入家具数据"""
    furniture = [
        (17, 'WorkBench', '工作台', 'crafting', 2, 1, json.dumps(['flat_surface', 'crafting']), 1, '基础制作站'),
        (14, 'Tables', '桌子', 'table', 3, 1, json.dumps(['flat_surface']), 1, '平坦表面家具'),
        (15, 'Chairs', '椅子', 'chair', 1, 2, json.dumps(['comfort']), 1, '舒适物品'),
        (79, 'Beds', '床', 'bed', 4, 2, json.dumps(['comfort', 'spawn_point']), 1, '可设置重生点'),
        (21, 'Chests', '宝箱', 'storage', 2, 2, json.dumps(['storage']), 0, '存储容器'),
        (88, 'Dressers', '梳妆台', 'storage', 3, 2, json.dumps(['flat_surface', 'storage']), 1, '存储+平坦表面'),
        (87, 'Pianos', '钢琴', 'decoration', 3, 2, json.dumps(['flat_surface']), 1, '装饰家具'),
        (89, 'Sofas', '沙发', 'comfort', 3, 2, json.dumps(['comfort']), 1, '舒适物品'),
        (90, 'Bathtubs', '浴缸', 'decoration', 4, 2, None, 1, '浴室装饰'),
        (101, 'Bookcases', '书架', 'decoration', 3, 4, json.dumps(['flat_surface']), 1, '书籍存储'),
        (19, 'Platforms', '平台', 'platform', 1, 1, json.dumps(['door']), 1, '可作为入口'),
    ]
    for f in furniture:
        cursor.execute("""
            INSERT OR IGNORE INTO furniture (id, name, display_name, category, width, height, npc_function, paint_compatible, description)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, f)
    return len(furniture)


def insert_light_sources(cursor):
    """插入光源数据"""
    lights = [
        (4, 'Torches', '火把', 'torch', 1, 1, 10, json.dumps(['light_source']), 'wall/floor', '基础光源'),
        (33, 'Candles', '蜡烛', 'candle', 1, 1, 5, json.dumps(['light_source']), 'table', '小型光源'),
        (34, 'Chandeliers', '吊灯', 'chandelier', 3, 3, 15, json.dumps(['light_source']), 'ceiling', '大型光源'),
        (93, 'Lamps', '立灯', 'lamp', 1, 3, 10, json.dumps(['light_source']), 'floor', '立式灯具'),
        (215, 'Campfires', '篝火', 'campfire', 3, 2, 20, json.dumps(['light_source', 'regeneration']), 'floor', '提供生命恢复'),
    ]
    for l in lights:
        cursor.execute("""
            INSERT OR IGNORE INTO light_sources (id, name, display_name, category, width, height, light_radius, npc_function, placement_type, description)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, l)
    return len(lights)


def insert_doors(cursor):
    """插入门数据"""
    doors = [
        (10, 'Doors', '门', 'standard', 1, 3, json.dumps(['door']), 1, '标准门'),
        (387, 'TrapDoor', '活板门', 'trapdoor', 2, 1, json.dumps(['door']), 1, '水平门'),
        (388, 'TallGate', '大门', 'gate', 1, 5, json.dumps(['door']), 1, '农场大门'),
    ]
    for d in doors:
        cursor.execute("""
            INSERT OR IGNORE INTO doors (id, name, display_name, category, width, height, npc_function, paint_compatible, description)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, d)
    return len(doors)


def insert_style_templates(cursor):
    """插入风格模板数据（建筑核心）"""
    styles = [
        ('medieval', '中世纪风格', '经典欧洲中世纪建筑风格，适合城堡、村庄和堡垒',
         json.dumps(['GrayBrick', 'StoneSlab', 'Stone', 'Wood']),
         json.dumps(['GrayBrickWall', 'StoneWall', 'WoodWall']),
         json.dumps(['GoldBrick', 'IronBrick']),
         'triangular', json.dumps(['Wood', 'GrayBrick']),
         json.dumps(['WorkBench', 'Tables', 'Chairs', 'Torches', 'Banners']),
         json.dumps({'primary': 0, 'shadow': 28}),
         json.dumps(['使用灰砖作为主要墙壁', '添加木质屋顶', '使用阴影油漆增加层次', '放置火把作为光源']),
         json.dumps(['forest', 'plain']),
         'medium'),

        ('fantasy', '奇幻风格', '魔法与幻想风格，适合神圣之地建筑',
         json.dumps(['Pearlstone', 'Glass', 'GoldBrick']),
         json.dumps(['PearlstoneWall', 'GlassWall']),
         json.dumps(['CrystalBlock', 'RainbowBrick']),
         'dome', json.dumps(['Pearlstone', 'Glass']),
         json.dumps(['CrystalChandelier', 'Tables', 'Chairs']),
         json.dumps({}),
         json.dumps(['使用珍珠石作为主体', '大量使用玻璃', '圆顶屋顶设计']),
         json.dumps(['hallow']),
         'hard'),

        ('steampunk', '蒸汽朋克风格', '工业革命风格，适合机械建筑',
         json.dumps(['CopperBrick', 'IronBrick', 'GearBlock']),
         json.dumps(['CopperBrickWall', 'IronBrickWall']),
         json.dumps(['Cogs', 'MetalBars']),
         'flat', json.dumps(['CopperBrick', 'IronBrick']),
         json.dumps(['WorkBench', 'Anvil', 'Furnaces']),
         json.dumps({'primary': 13, 'metal': 14}),
         json.dumps(['使用铜砖和铁砖', '添加齿轮装饰', '工业风格平顶']),
         json.dumps(['any']),
         'medium'),

        ('natural', '自然风格', '与自然融合的建筑风格',
         json.dumps(['Wood', 'Dirt', 'Stone']),
         json.dumps(['WoodWall', 'DirtWall', 'LivingWoodWall']),
         json.dumps(['LeafBlock', 'Flowers']),
         'curved', json.dumps(['Wood', 'LivingWood']),
         json.dumps(['WorkBench', 'Tables', 'Chairs']),
         json.dumps({'primary': 0}),
         json.dumps(['使用木材和泥土', '与周围地形融合', '不规则形状']),
         json.dumps(['forest', 'jungle']),
         'easy'),

        ('asian', '东方风格', '中日式建筑风格',
         json.dumps(['DynastyWood', 'Wood', 'BambooBlock']),
         json.dumps(['DynastyWall', 'BambooWall']),
         json.dumps(['PaperLantern', 'Teapot']),
         'curved', json.dumps(['DynastyWood']),
         json.dumps(['ChineseLantern', 'Teacup', 'Tables']),
         json.dumps({'primary': 0, 'accent': 1}),
         json.dumps(['使用王朝木', '弯曲屋顶', '悬挂灯笼']),
         json.dumps(['any']),
         'medium'),

        ('snow', '冰雪风格', '冬季风格建筑',
         json.dumps(['SnowBlock', 'IceBlock', 'BorealWood']),
         json.dumps(['SnowWall', 'IceWall', 'BorealWoodWall']),
         json.dumps(['FrozenFurniture']),
         'triangular', json.dumps(['SnowBlock', 'BorealWood']),
         json.dumps(['IceChandelier', 'Tables', 'Chairs']),
         json.dumps({'primary': 0, 'ice': 7}),
         json.dumps(['使用雪块和冰块', '针叶木结构', '圣诞装饰']),
         json.dumps(['snow', 'tundra']),
         'easy'),

        ('desert', '沙漠风格', '沙漠和古埃及风格',
         json.dumps(['Sandstone', 'SandstoneSlab', 'PalmWood']),
         json.dumps(['SandstoneWall']),
         json.dumps(['GoldBrick', 'Scarab']),
         'flat', json.dumps(['Sandstone']),
         json.dumps(['Tables', 'Chairs', 'Candelabra']),
         json.dumps({'primary': 2, 'accent': 3}),
         json.dumps(['使用砂岩', '平顶或金字塔', '金色装饰']),
         json.dumps(['desert']),
         'medium'),

        ('underground', '地下风格', '地下洞穴风格',
         json.dumps(['Stone', 'Dirt', 'Wood']),
         json.dumps(['StoneWall', 'DirtWall']),
         json.dumps(['GemsparkBlocks']),
         'irregular', json.dumps(['Wood']),
         json.dumps(['Torches', 'WorkBench', 'Chairs']),
         json.dumps({'primary': 0, 'shadow': 28}),
         json.dumps(['利用自然洞穴', '木质平台楼梯', '大量火把照明']),
         json.dumps(['underground', 'cavern']),
         'easy'),

        ('ocean', '海洋风格', '海洋和海滩风格',
         json.dumps(['PalmWood', 'Glass', 'Coral']),
         json.dumps(['GlassWall']),
         json.dumps(['Seashell', 'Starfish']),
         'triangular', json.dumps(['PalmWood']),
         json.dumps(['Tables', 'Chairs', 'Lanterns']),
         json.dumps({'primary': 0, 'ocean': 8}),
         json.dumps(['使用棕榈木', '大量玻璃', '海洋装饰']),
         json.dumps(['ocean', 'beach']),
         'easy'),

        ('modern', '现代风格', '现代简约风格',
         json.dumps(['Granite', 'Glass', 'Marble']),
         json.dumps(['GraniteWall', 'GlassWall']),
         json.dumps(['MetalBars']),
         'flat', json.dumps(['Granite']),
         json.dumps(['Tables', 'Sofas', 'Lamps']),
         json.dumps({'primary': 30, 'accent': 14}),
         json.dumps(['使用花岗岩和大理石', '大量玻璃', '简约线条']),
         json.dumps(['any']),
         'medium'),
    ]

    for s in styles:
        cursor.execute("""
            INSERT OR IGNORE INTO style_templates
            (name, display_name, description, primary_tiles, primary_walls, accent_tiles,
             roof_style, roof_tiles, furniture_style, paint_scheme, architectural_rules,
             biome_recommendations, difficulty)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, s)
    return len(styles)


def insert_npc_requirements(cursor):
    """插入NPC要求数据（建筑核心）"""
    npcs = [
        ('merchant', '商人', '总资产超过50银币', 'forest', None,
         json.dumps(['nurse', 'goblin_tinkerer']), json.dumps(['tax_collector']), None),
        ('nurse', '护士', '生命值超过100', 'forest', None,
         json.dumps(['merchant']), json.dumps(['tax_collector']), None),
        ('demolitionist', '炸弹专家', '背包中有炸弹', 'underground', None,
         json.dumps(['merchant']), None, None),
        ('gunsmith', '军火商', '背包中有枪或子弹', 'forest', None,
         json.dumps(['nurse']), json.dumps(['demolitionist']), None),
        ('dryad', '树妖', '击败任何Boss', 'forest', None,
         json.dumps(['witch_doctor']), json.dumps(['truffle']), None),
        ('wizard', '巫师', '击败骷髅王', 'hallow', None,
         json.dumps(['nurse']), None, None),
        ('goblin_tinkerer', '哥布林工匠', '击败哥布林军队后地下找到', 'underground', None,
         json.dumps(['mechanic', 'cyborg']), None, None),
        ('mechanic', '电工', '地牢中解救', 'underground', None,
         json.dumps(['goblin_tinkerer', 'cyborg']), None, None),
        ('stylist', '发型师', '蜘蛛巢中解救', 'forest', None,
         json.dumps(['dye_trader']), None, None),
        ('angler', '钓鱼佬', '海边对话', 'ocean', None,
         None, json.dumps(['tax_collector', 'merchant']), None),
        ('witch_doctor', '巫医', '击败骷髅王', 'jungle', None,
         json.dumps(['dryad']), None, None),
        ('truffle', '松露人', '地表蘑菇生物群落', 'glowing_mushroom', None,
         None, json.dumps(['dryad']), None),
        ('steampunker', '蒸汽朋克人', '击败机械Boss', 'forest', None,
         json.dumps(['cyborg', 'mechanic']), None, None),
        ('cyborg', '半机械人', '击败火星入侵', 'forest', None,
         json.dumps(['mechanic', 'goblin_tinkerer']), None, None),
        ('tax_collector', '税务官', '地狱中用净化粉末解救', None, None,
         None, None, None),
        ('painter', '画家', '背包中有油漆', 'forest', None,
         json.dumps(['stylist']), None, None),
        ('party_girl', '派对女孩', '有8个NPC后随机出现', 'hallow', None,
         None, None, None),
        ('dye_trader', '染料商', '背包中有染料材料', 'desert', None,
         json.dumps(['stylist']), None, None),
    ]

    for n in npcs:
        cursor.execute("""
            INSERT OR IGNORE INTO npc_requirements
            (npc_name, display_name, spawn_condition, preferred_biome, disliked_biome,
             preferred_neighbors, disliked_neighbors, special_furniture)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        """, n)
    return len(npcs)


def insert_house_validation(cursor):
    """插入房屋验证规则数据（建筑核心）"""
    rules = [
        ('minimum_tiles', 'size', '房间内部至少60格空地', 60, None, '房间必须有足够空间'),
        ('light_source', 'furniture', '必需一个光源', 1, json.dumps(['Torches', 'Candles', 'Chandeliers']), '火把、蜡烛、吊灯等'),
        ('flat_surface', 'furniture', '必需一个平坦表面', 1, json.dumps(['Tables', 'WorkBench', 'Dressers']), '桌子、工作台、梳妆台'),
        ('comfort', 'furniture', '必需一个舒适物品', 1, json.dumps(['Chairs', 'Beds', 'Sofas']), '椅子、床、沙发'),
        ('door', 'furniture', '必需一个入口', 1, json.dumps(['Doors', 'TrapDoor', 'Platforms']), '门、活板门、平台'),
        ('wall_required', 'wall', '必须有背景墙', None, None, '天然墙无效，需要玩家放置的墙'),
        ('frame_required', 'frame', '必须被实心方块包围', None, None, '四周有实心方块作为框架'),
        ('max_size', 'size', '房间最大尺寸限制', None, None, '不宜过大以免影响判定'),
        ('biome_check', 'biome', '不能处于邪恶生物群落', None, None, '腐化/猩红/神圣生物群落会影响NPC'),
    ]

    for r in rules:
        cursor.execute("""
            INSERT OR IGNORE INTO house_validation
            (rule_name, rule_type, requirement, minimum_value, required_elements, description)
            VALUES (?, ?, ?, ?, ?, ?)
        """, r)
    return len(rules)


def insert_biomes(cursor):
    """插入生物群落数据（建筑核心）"""
    biomes = [
        ('forest', '森林', 'surface', json.dumps(['Grass', 'Wood', 'Stone']), json.dumps(['WoodWall', 'StoneWall']), '基础森林生物群落'),
        ('desert', '沙漠', 'surface', json.dumps(['Sand', 'Sandstone', 'HardenedSand']), json.dumps(['SandstoneWall']), '沙漠生物群落'),
        ('snow', '雪地', 'surface', json.dumps(['SnowBlock', 'IceBlock', 'BorealWood']), json.dumps(['SnowWall', 'IceWall']), '雪地生物群落'),
        ('jungle', '丛林', 'surface', json.dumps(['JungleGrass', 'RichMahogany', 'Mud']), json.dumps(['JungleWall']), '丛林生物群落'),
        ('ocean', '海洋', 'surface', json.dumps(['Sand', 'Coral', 'PalmWood']), json.dumps(['GlassWall']), '海洋生物群落'),
        ('underground', '地下', 'underground', json.dumps(['Stone', 'Dirt']), json.dumps(['StoneWall', 'DirtWall']), '地下生物群落'),
        ('cavern', '洞穴', 'underground', json.dumps(['Stone', 'Marble', 'Granite']), json.dumps(['StoneWall']), '深层洞穴'),
        ('hallow', '神圣', 'special', json.dumps(['Pearlstone', 'Pearlwood']), json.dumps(['PearlstoneWall']), '神圣生物群落'),
        ('corruption', '腐化', 'special', json.dumps(['Ebonstone', 'Ebonsand']), json.dumps(['EbonstoneWall']), '腐化生物群落'),
        ('crimson', '猩红', 'special', json.dumps(['Crimstone', 'Crimsand']), json.dumps(['CrimstoneWall']), '猩红生物群落'),
        ('glowing_mushroom', '发光蘑菇', 'special', json.dumps(['MushroomBlock', 'GlowingMushroom']), json.dumps(['MushroomWall']), '发光蘑菇生物群落'),
        ('dungeon', '地牢', 'special', json.dumps(['DungeonBrick']), json.dumps(['DungeonWall']), '地牢生物群落'),
        ('hell', '地狱', 'underground', json.dumps(['Obsidian', 'Hellstone', 'Ash']), json.dumps(['ObsidianWall']), '地狱生物群落'),
    ]

    for b in biomes:
        cursor.execute("""
            INSERT OR IGNORE INTO biomes
            (name, display_name, category, characteristic_tiles, characteristic_walls, description)
            VALUES (?, ?, ?, ?, ?, ?)
        """, b)
    return len(biomes)


def generate_sql_file(cursor, sql_path):
    """生成完整的SQL文件"""
    with open(sql_path, 'w', encoding='utf-8') as f:
        f.write("-- Terraria 建筑知识库数据库\n")
        f.write(f"-- 生成时间: {datetime.now().isoformat()}\n")
        f.write("-- 包含所有建筑相关数据\n\n")

        # 导出所有表的数据
        tables = ['tiles', 'walls', 'paints', 'slopes', 'furniture', 'light_sources', 'doors',
                  'style_templates', 'npc_requirements', 'house_validation', 'biomes']

        for table in tables:
            f.write(f"\n-- === {table} ===\n")

            # 获取表结构
            cursor.execute(f"PRAGMA table_info({table})")
            columns = [col[1] for col in cursor.fetchall()]

            # 获取数据
            cursor.execute(f"SELECT * FROM {table}")
            rows = cursor.fetchall()

            for row in rows:
                values = []
                for i, val in enumerate(row):
                    if val is None:
                        values.append('NULL')
                    elif isinstance(val, int):
                        values.append(str(val))
                    else:
                        # 转义字符串
                        escaped = str(val).replace("'", "''")
                        values.append(f"'{escaped}'")

                cols_str = ', '.join(columns)
                vals_str = ', '.join(values)
                f.write(f"INSERT OR IGNORE INTO {table} ({cols_str}) VALUES ({vals_str});\n")

        print(f"SQL文件已生成: {sql_path}")


def main():
    print("=" * 50)
    print("完整数据库初始化")
    print("=" * 50)

    # 删除旧数据库
    if os.path.exists(DB_PATH):
        os.remove(DB_PATH)
        print("已删除旧数据库")

    # 创建新数据库
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    # 创建表
    print("\n创建数据库表...")
    create_tables(cursor)

    # 插入基础数据
    print("\n插入基础数据...")
    count = insert_paints(cursor)
    print(f"  paints: {count}")
    count = insert_slopes(cursor)
    print(f"  slopes: {count}")
    count = insert_furniture(cursor)
    print(f"  furniture: {count}")
    count = insert_light_sources(cursor)
    print(f"  light_sources: {count}")
    count = insert_doors(cursor)
    print(f"  doors: {count}")

    # 插入建筑核心数据
    print("\n插入建筑核心数据...")
    count = insert_style_templates(cursor)
    print(f"  style_templates: {count}")
    count = insert_npc_requirements(cursor)
    print(f"  npc_requirements: {count}")
    count = insert_house_validation(cursor)
    print(f"  house_validation: {count}")
    count = insert_biomes(cursor)
    print(f"  biomes: {count}")

    conn.commit()

    # 生成SQL文件
    print("\n生成SQL备份文件...")
    generate_sql_file(cursor, SQL_PATH)

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

    print("\n" + "=" * 50)
    print("完成!")
    print(f"数据库: {DB_PATH}")
    print(f"SQL文件: {SQL_PATH}")
    print("=" * 50)


if __name__ == "__main__":
    main()