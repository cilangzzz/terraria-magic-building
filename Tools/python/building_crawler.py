#!/usr/bin/env python3
"""
Terraria建筑数据爬虫
从Terraria Wiki爬取建筑风格、布局、家具等详细数据
"""

import requests
from bs4 import BeautifulSoup
import json
import sqlite3
import os
import sys
import time
import re
from datetime import datetime

sys.stdout.reconfigure(encoding='utf-8')

# 路径配置
DATA_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Data")
DB_PATH = os.path.join(DATA_DIR, "terraria_kb.db")
SQL_PATH = os.path.join(DATA_DIR, "terraria_kb_full.sql")

# Wiki URLs
WIKI_BASE = "https://terraria.wiki.gg/wiki"
HEADERS = {
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
    'Accept-Language': 'zh-CN,zh;q=0.9,en;q=0.8'
}

# 建筑风格关键词映射
STYLE_KEYWORDS = {
    'medieval': ['medieval', 'castle', 'fortress', 'stone', 'brick', 'wood'],
    'fantasy': ['fantasy', 'magic', 'hallow', 'pearlstone', 'crystal', 'glowing'],
    'steampunk': ['steampunk', 'industrial', 'copper', 'iron', 'gear', 'cog'],
    'natural': ['natural', 'living', 'tree', 'forest', 'wood', 'organic'],
    'asian': ['asian', 'dynasty', 'chinese', 'japanese', 'eastern', 'bamboo'],
    'snow': ['snow', 'ice', 'frozen', 'winter', 'boreal', 'christmas'],
    'desert': ['desert', 'sand', 'sandstone', 'egyptian', 'pyramid', 'palm'],
    'ocean': ['ocean', 'beach', 'coral', 'sea', 'maritime', 'tropical'],
    'underground': ['underground', 'cave', 'cavern', 'mine', 'dungeon'],
    'modern': ['modern', 'glass', 'granite', 'marble', 'minimalist'],
    'gothic': ['gothic', 'dark', 'obsidian', 'spooky', 'haunted'],
    'village': ['village', 'house', 'cottage', 'rustic', 'farm']
}


def fetch_wiki_page(url, retry=3):
    """获取Wiki页面内容"""
    for i in range(retry):
        try:
            response = requests.get(url, headers=HEADERS, timeout=30)
            if response.status_code == 200:
                return BeautifulSoup(response.content, 'html.parser')
            print(f"  请求失败: {response.status_code}")
        except Exception as e:
            print(f"  请求错误: {e}")
            time.sleep(2)
    return None


def parse_furniture_table(soup, table_id=None):
    """解析家具表格"""
    furniture_list = []

    tables = soup.find_all('table', class_='wikitable')
    for table in tables:
        rows = table.find_all('tr')
        if len(rows) < 2:
            continue

        # 获取表头
        headers = []
        header_row = rows[0]
        for th in header_row.find_all(['th', 'td']):
            headers.append(th.get_text(strip=True).lower())

        # 解析数据行
        for row in rows[1:]:
            cells = row.find_all(['td', 'th'])
            if len(cells) < 2:
                continue

            item = {}
            for i, cell in enumerate(cells):
                if i < len(headers):
                    key = headers[i]
                    # 获取链接和文本
                    link = cell.find('a')
                    if link:
                        item['link'] = link.get('href', '')
                        item[key] = link.get_text(strip=True)
                    else:
                        item[key] = cell.get_text(strip=True)

            if item:
                furniture_list.append(item)

    return furniture_list


def crawl_furniture():
    """爬取家具数据"""
    print("\n=== 爬取家具数据 ===")

    furniture_data = []

    # 主要家具类别页面
    furniture_pages = [
        ('Furniture', 'https://terraria.wiki.gg/wiki/Furniture'),
        ('Tables', 'https://terraria.wiki.gg/wiki/Tables'),
        ('Chairs', 'https://terraria.wiki.gg/wiki/Chairs'),
        ('Beds', 'https://terraria.wiki.gg/wiki/Beds'),
        ('Sofas', 'https://terraria.wiki.gg/wiki/Sofas'),
        ('Bookcases', 'https://terraria.wiki.gg/wiki/Bookcases'),
        ('Dressers', 'https://terraria.wiki.gg/wiki/Dressers'),
        ('Pianos', 'https://terraria.wiki.gg/wiki/Pianos'),
        ('Bathtubs', 'https://terraria.wiki.gg/wiki/Bathtubs'),
        ('Lamps', 'https://terraria.wiki.gg/wiki/Lamps'),
        ('Chandeliers', 'https://terraria.wiki.gg/wiki/Chandeliers'),
        ('Candles', 'https://terraria.wiki.gg/wiki/Candles'),
        ('Banners', 'https://terraria.wiki.gg/wiki/Banners'),
        ('Paintings', 'https://terraria.wiki.gg/wiki/Paintings'),
        ('Trophies', 'https://terraria.wiki.gg/wiki/Trophies'),
        ('Statues', 'https://terraria.wiki.gg/wiki/Statues'),
    ]

    for category, url in furniture_pages:
        print(f"  爬取: {category}")
        soup = fetch_wiki_page(url)
        if not soup:
            continue

        items = parse_furniture_table(soup)
        for item in items:
            # 提取关键信息
            name = item.get('name', item.get('item', item.get('image', '')))
            if not name:
                continue

            furniture_entry = {
                'name': name,
                'category': category,
                'source': url,
                'raw_data': item
            }

            # 尝试获取尺寸信息
            if 'width' in item:
                furniture_entry['width'] = int(item['width']) if item['width'].isdigit() else 1
            if 'height' in item:
                furniture_entry['height'] = int(item['height']) if item['height'].isdigit() else 1

            furniture_data.append(furniture_entry)

        time.sleep(0.5)  # 避免请求过快

    print(f"  共获取 {len(furniture_data)} 条家具数据")
    return furniture_data


def crawl_building_materials():
    """爬取建筑材料数据"""
    print("\n=== 爬取建筑材料数据 ===")

    materials_data = []

    # 建筑材料页面
    material_pages = [
        ('Blocks', 'https://terraria.wiki.gg/wiki/Blocks'),
        ('Bricks', 'https://terraria.wiki.gg/wiki/Bricks'),
        ('Walls', 'https://terraria.wiki.gg/wiki/Walls'),
        ('Wood', 'https://terraria.wiki.gg/wiki/Wood'),
        ('Platforms', 'https://terraria.wiki.gg/wiki/Platforms'),
    ]

    for category, url in material_pages:
        print(f"  爬取: {category}")
        soup = fetch_wiki_page(url)
        if not soup:
            continue

        items = parse_furniture_table(soup)
        for item in items:
            name = item.get('name', item.get('item', item.get('image', '')))
            if not name:
                continue

            material_entry = {
                'name': name,
                'category': category,
                'source': url,
                'raw_data': item
            }
            materials_data.append(material_entry)

        time.sleep(0.5)

    print(f"  共获取 {len(materials_data)} 条材料数据")
    return materials_data


def crawl_light_sources():
    """爬取光源数据"""
    print("\n=== 爬取光源数据 ===")

    light_data = []

    light_pages = [
        ('Torches', 'https://terraria.wiki.gg/wiki/Torches'),
        ('Candles', 'https://terraria.wiki.gg/wiki/Candles'),
        ('Chandeliers', 'https://terraria.wiki.gg/wiki/Chandeliers'),
        ('Lamps', 'https://terraria.wiki.gg/wiki/Lamps'),
        ('Campfires', 'https://terraria.wiki.gg/wiki/Campfires'),
        ('Light sources', 'https://terraria.wiki.gg/wiki/Light_sources'),
    ]

    for category, url in light_pages:
        print(f"  爬取: {category}")
        soup = fetch_wiki_page(url)
        if not soup:
            continue

        items = parse_furniture_table(soup)
        for item in items:
            name = item.get('name', item.get('item', item.get('image', '')))
            if not name:
                continue

            light_entry = {
                'name': name,
                'category': category,
                'source': url,
                'raw_data': item
            }

            # 尝试获取光照半径
            if 'light' in item:
                light_text = item['light']
                # 解析光照值
                match = re.search(r'(\d+)', light_text)
                if match:
                    light_entry['light_radius'] = int(match.group(1))

            light_data.append(light_entry)

        time.sleep(0.5)

    print(f"  共获取 {len(light_data)} 条光源数据")
    return light_data


def crawl_npc_housing():
    """爬取NPC房屋数据"""
    print("\n=== 爬取NPC房屋数据 ===")

    npc_data = []

    url = "https://terraria.wiki.gg/wiki/NPCs"
    print(f"  爬取: NPCs")
    soup = fetch_wiki_page(url)
    if soup:
        items = parse_furniture_table(soup)
        for item in items:
            name = item.get('name', item.get('npc', item.get('image', '')))
            if not name:
                continue

            npc_entry = {
                'name': name,
                'source': url,
                'raw_data': item
            }
            npc_data.append(npc_entry)

    # 单独爬取重要NPC页面获取详细信息
    important_npcs = [
        'Merchant', 'Nurse', 'Demolitionist', 'Gunsmith', 'Dryad',
        'Wizard', 'Goblin_Tinkerer', 'Mechanic', 'Stylist', 'Angler',
        'Witch_Doctor', 'Truffle', 'Steampunker', 'Cyborg', 'Tax_Collector',
        'Painter', 'Party_Girl', 'Dye_Trader', 'Princess', 'Bestiary'
    ]

    for npc in important_npcs:
        print(f"  爬取NPC详情: {npc}")
        url = f"{WIKI_BASE}/{npc}"
        soup = fetch_wiki_page(url)
        if not soup:
            continue

        # 解析NPC偏好信息
        npc_entry = {
            'name': npc.replace('_', ''),
            'source': url
        }

        # 查找偏好信息
        content = soup.find('div', class_='mw-parser-output')
        if content:
            text = content.get_text()

            # 提取生物群落偏好
            biome_patterns = ['lives in', 'prefers', 'biome', 'environment']
            for pattern in biome_patterns:
                if pattern in text.lower():
                    # 尝试提取相关信息
                    pass

        npc_data.append(npc_entry)
        time.sleep(0.5)

    print(f"  共获取 {len(npc_data)} 条NPC数据")
    return npc_data


def crawl_biomes():
    """爬取生物群落数据"""
    print("\n=== 爬取生物群落数据 ===")

    biome_data = []

    biome_pages = [
        ('Forest', 'https://terraria.wiki.gg/wiki/Forest'),
        ('Desert', 'https://terraria.wiki.gg/wiki/Desert'),
        ('Snow biome', 'https://terraria.wiki.gg/wiki/Snow_biome'),
        ('Jungle', 'https://terraria.wiki.gg/wiki/Jungle'),
        ('Ocean', 'https://terraria.wiki.gg/wiki/Ocean'),
        ('Underground', 'https://terraria.wiki.gg/wiki/Underground'),
        ('Cavern', 'https://terraria.wiki.gg/wiki/Cavern'),
        ('The Hallow', 'https://terraria.wiki.gg/wiki/The_Hallow'),
        ('Corruption', 'https://terraria.wiki.gg/wiki/The_Corruption'),
        ('Crimson', 'https://terraria.wiki.gg/wiki/The_Crimson'),
        ('Glowing Mushroom biome', 'https://terraria.wiki.gg/wiki/Glowing_Mushroom_biome'),
        ('Dungeon', 'https://terraria.wiki.gg/wiki/Dungeon'),
        ('Underworld', 'https://terraria.wiki.gg/wiki/Underworld'),
        ('Sky', 'https://terraria.wiki.gg/wiki/Space'),
    ]

    for biome_name, url in biome_pages:
        print(f"  爬取: {biome_name}")
        soup = fetch_wiki_page(url)
        if not soup:
            continue

        biome_entry = {
            'name': biome_name,
            'source': url
        }

        # 解析生物群落特征
        content = soup.find('div', class_='mw-parser-output')
        if content:
            text = content.get_text()

            # 提取特征方块
            blocks = []
            block_patterns = ['block', 'brick', 'stone', 'wood']
            for pattern in block_patterns:
                matches = re.findall(rf'\b(\w+{pattern}\w*)\b', text, re.IGNORECASE)
                blocks.extend(matches[:5])  # 限制数量

            biome_entry['characteristic_blocks'] = list(set(blocks))

        biome_data.append(biome_entry)
        time.sleep(0.5)

    print(f"  共获取 {len(biome_data)} 条生物群落数据")
    return biome_data


def crawl_building_tutorials():
    """爬取建筑教程数据"""
    print("\n=== 爬取建筑教程数据 ===")

    tutorials = []

    # 建筑指南页面
    guide_pages = [
        ('House', 'https://terraria.wiki.gg/wiki/House'),
        ('NPC House', 'https://terraria.wiki.gg/wiki/House#NPC_House'),
        ('Building', 'https://terraria.wiki.gg/wiki/Building'),
        ('Guide:Building', 'https://terraria.wiki.gg/wiki/Guide:Building'),
        ('Guide:Base construction', 'https://terraria.wiki.gg/wiki/Guide:Base_construction'),
        ('Guide:NPC housing', 'https://terraria.wiki.gg/wiki/Guide:NPC_housing'),
    ]

    for guide_name, url in guide_pages:
        print(f"  爬取: {guide_name}")
        soup = fetch_wiki_page(url)
        if not soup:
            continue

        tutorial_entry = {
            'name': guide_name,
            'source': url,
            'type': 'guide'
        }

        # 解析建筑规则和建议
        content = soup.find('div', class_='mw-parser-output')
        if content:
            text = content.get_text()

            # 提取房屋尺寸要求
            size_match = re.search(r'(\d+)\s*(tiles|blocks|格)', text)
            if size_match:
                tutorial_entry['min_size'] = int(size_match.group(1))

            # 提取必需家具
            furniture_keywords = ['table', 'chair', 'torch', 'door', 'light', 'comfort', 'flat surface']
            required_furniture = []
            for keyword in furniture_keywords:
                if keyword in text.lower():
                    required_furniture.append(keyword)

            tutorial_entry['required_elements'] = required_furniture

        tutorials.append(tutorial_entry)
        time.sleep(0.5)

    print(f"  共获取 {len(tutorials)} 条教程数据")
    return tutorials


def extract_building_styles():
    """从爬取的数据中提取建筑风格"""
    print("\n=== 提取建筑风格 ===")

    styles = []

    # 定义详细的建筑风格模板
    style_templates = [
        {
            'name': 'medieval_castle',
            'display_name': '中世纪城堡',
            'description': '经典欧洲中世纪城堡风格，使用灰砖和石材',
            'primary_tiles': ['GrayBrick', 'StoneSlab', 'StoneBlock'],
            'primary_walls': ['GrayBrickWall', 'StoneWall'],
            'accent_tiles': ['GoldBrick', 'IronBrick'],
            'roof_style': 'triangular',
            'roof_tiles': ['GrayBrick', 'StoneSlab'],
            'furniture_style': ['WorkBench', 'Tables', 'Chairs', 'Torches', 'Banners'],
            'paint_scheme': {'primary': 0, 'shadow': 28, 'accent': 9},
            'architectural_rules': [
                '使用灰砖作为主要建筑材料',
                '添加木质或石质屋顶',
                '使用阴影油漆增加层次感',
                '放置火把和吊灯作为光源',
                '使用旗帜和绘画作为装饰',
                '添加城墙和塔楼结构'
            ],
            'layout_templates': [
                {'name': '小型城堡', 'size': '15x10', 'rooms': 2},
                {'name': '中型城堡', 'size': '25x20', 'rooms': 4},
                {'name': '大型城堡', 'size': '40x30', 'rooms': 8}
            ],
            'biome_recommendations': ['forest', 'plain'],
            'difficulty': 'medium'
        },
        {
            'name': 'medieval_house',
            'display_name': '中世纪民居',
            'description': '欧洲中世纪乡村房屋风格',
            'primary_tiles': ['Wood', 'GrayBrick', 'StoneBlock'],
            'primary_walls': ['WoodWall', 'GrayBrickWall'],
            'accent_tiles': ['Glass'],
            'roof_style': 'triangular',
            'roof_tiles': ['Wood', 'GrayBrick'],
            'furniture_style': ['WorkBench', 'Tables', 'Chairs', 'Torches'],
            'paint_scheme': {'primary': 0, 'wood': 5},
            'architectural_rules': [
                '使用木材和灰砖混合',
                '小型三角屋顶',
                '玻璃窗户',
                '木质门',
                '简单家具布局'
            ],
            'layout_templates': [
                {'name': '单层小屋', 'size': '10x8', 'rooms': 1},
                {'name': '双层民居', 'size': '12x15', 'rooms': 2}
            ],
            'biome_recommendations': ['forest', 'plain'],
            'difficulty': 'easy'
        },
        {
            'name': 'fantasy_palace',
            'display_name': '奇幻宫殿',
            'description': '魔法奇幻风格宫殿，适合神圣之地',
            'primary_tiles': ['Pearlstone', 'Glass', 'GoldBrick'],
            'primary_walls': ['PearlstoneWall', 'GlassWall'],
            'accent_tiles': ['CrystalBlock', 'RainbowBrick', 'GemsparkBlock'],
            'roof_style': 'dome',
            'roof_tiles': ['Pearlstone', 'Glass'],
            'furniture_style': ['CrystalChandelier', 'Tables', 'Chairs', 'Bookcases'],
            'paint_scheme': {'primary': 0, 'accent': 11, 'crystal': 7},
            'architectural_rules': [
                '使用珍珠石作为主体',
                '大量使用玻璃和水晶方块',
                '圆顶或穹顶屋顶设计',
                '使用彩虹砖和宝石方块装饰',
                '吊灯和立灯作为光源',
                '添加魔法氛围装饰'
            ],
            'layout_templates': [
                {'name': '小型宫殿', 'size': '20x15', 'rooms': 3},
                {'name': '大型宫殿', 'size': '50x40', 'rooms': 10}
            ],
            'biome_recommendations': ['hallow'],
            'difficulty': 'hard'
        },
        {
            'name': 'steampunk_factory',
            'display_name': '蒸汽朋克工厂',
            'description': '工业革命风格工厂建筑',
            'primary_tiles': ['CopperBrick', 'IronBrick', 'MetalBars'],
            'primary_walls': ['CopperBrickWall', 'IronBrickWall'],
            'accent_tiles': ['GearBlock', 'Cogs', 'Pipe'],
            'roof_style': 'flat',
            'roof_tiles': ['CopperBrick', 'IronBrick'],
            'furniture_style': ['WorkBench', 'Anvil', 'Furnaces', 'Tables'],
            'paint_scheme': {'primary': 13, 'metal': 14, 'rust': 2},
            'architectural_rules': [
                '使用铜砖和铁砖',
                '添加齿轮和管道装饰',
                '工业风格平顶设计',
                '使用深红和铜色油漆',
                '大量使用金属家具',
                '添加烟囱结构'
            ],
            'layout_templates': [
                {'name': '小型工坊', 'size': '15x12', 'rooms': 2},
                {'name': '大型工厂', 'size': '30x25', 'rooms': 5}
            ],
            'biome_recommendations': ['any'],
            'difficulty': 'medium'
        },
        {
            'name': 'natural_treehouse',
            'display_name': '自然树屋',
            'description': '与自然融合的树屋建筑',
            'primary_tiles': ['Wood', 'LivingWood', 'RichMahogany'],
            'primary_walls': ['WoodWall', 'LivingWoodWall', 'LeafWall'],
            'accent_tiles': ['LeafBlock', 'Vines', 'Flowers'],
            'roof_style': 'curved',
            'roof_tiles': ['LivingWood', 'LeafBlock'],
            'furniture_style': ['WorkBench', 'Tables', 'Chairs', 'Campfires'],
            'paint_scheme': {'primary': 0, 'leaf': 5},
            'architectural_rules': [
                '使用木材和活木',
                '与树木结构融合',
                '添加叶子和藤蔓装饰',
                '使用篝火作为光源',
                '不规则有机形状',
                '多层平台设计'
            ],
            'layout_templates': [
                {'name': '小型树屋', 'size': '8x8', 'rooms': 1},
                {'name': '大型树屋', 'size': '15x20', 'rooms': 3}
            ],
            'biome_recommendations': ['forest', 'jungle'],
            'difficulty': 'easy'
        },
        {
            'name': 'asian_temple',
            'display_name': '东方寺庙',
            'description': '中日式寺庙建筑风格',
            'primary_tiles': ['DynastyWood', 'BambooBlock', 'Wood'],
            'primary_walls': ['DynastyWall', 'BambooWall'],
            'accent_tiles': ['Paper', 'LanternBlock'],
            'roof_style': 'curved',
            'roof_tiles': ['DynastyWood'],
            'furniture_style': ['ChineseLantern', 'PaperLantern', 'Tables', 'Chairs'],
            'paint_scheme': {'primary': 0, 'accent': 1, 'gold': 2},
            'architectural_rules': [
                '使用王朝木和竹子',
                '弯曲屋顶设计',
                '悬挂灯笼作为光源',
                '使用纸张装饰',
                '添加门廊和庭院',
                '对称布局设计'
            ],
            'layout_templates': [
                {'name': '小型寺庙', 'size': '15x12', 'rooms': 2},
                {'name': '大型寺庙', 'size': '30x25', 'rooms': 5}
            ],
            'biome_recommendations': ['any'],
            'difficulty': 'medium'
        },
        {
            'name': 'asian_house',
            'display_name': '东方民居',
            'description': '中日式传统民居风格',
            'primary_tiles': ['DynastyWood', 'Wood', 'BambooBlock'],
            'primary_walls': ['DynastyWall', 'WoodWall'],
            'accent_tiles': ['Paper', 'Glass'],
            'roof_style': 'curved',
            'roof_tiles': ['DynastyWood'],
            'furniture_style': ['Tables', 'Chairs', 'ChineseLantern', 'Teacup'],
            'paint_scheme': {'primary': 0, 'accent': 1},
            'architectural_rules': [
                '使用王朝木',
                '弯曲屋顶',
                '纸门和屏风',
                '茶具和灯笼装饰',
                '榻榻米风格地板'
            ],
            'layout_templates': [
                {'name': '小型民居', 'size': '10x8', 'rooms': 1},
                {'name': '中型民居', 'size': '15x12', 'rooms': 2}
            ],
            'biome_recommendations': ['any'],
            'difficulty': 'easy'
        },
        {
            'name': 'snow_cottage',
            'display_name': '雪地小屋',
            'description': '冬季雪地风格小屋',
            'primary_tiles': ['SnowBlock', 'IceBlock', 'BorealWood'],
            'primary_walls': ['SnowWall', 'IceWall', 'BorealWoodWall'],
            'accent_tiles': ['FrozenFurniture', 'ChristmasDecorations'],
            'roof_style': 'triangular',
            'roof_tiles': ['SnowBlock', 'BorealWood'],
            'furniture_style': ['IceChandelier', 'Tables', 'Chairs', 'Campfires'],
            'paint_scheme': {'primary': 0, 'ice': 7, 'warm': 3},
            'architectural_rules': [
                '使用雪块和冰块',
                '针叶木结构',
                '三角屋顶',
                '冰制吊灯',
                '圣诞装饰',
                '篝火取暖'
            ],
            'layout_templates': [
                {'name': '雪地小屋', 'size': '10x8', 'rooms': 1},
                {'name': '冰屋', 'size': '12x10', 'rooms': 1}
            ],
            'biome_recommendations': ['snow', 'tundra'],
            'difficulty': 'easy'
        },
        {
            'name': 'desert_pyramid',
            'display_name': '沙漠金字塔',
            'description': '古埃及金字塔风格建筑',
            'primary_tiles': ['Sandstone', 'SandstoneSlab', 'GoldBrick'],
            'primary_walls': ['SandstoneWall'],
            'accent_tiles': ['Scarab', 'AncientDecorations'],
            'roof_style': 'pyramid',
            'roof_tiles': ['Sandstone', 'GoldBrick'],
            'furniture_style': ['Tables', 'Chairs', 'Candelabra', 'Statues'],
            'paint_scheme': {'primary': 2, 'accent': 3, 'gold': 2},
            'architectural_rules': [
                '使用砂岩',
                '金字塔形状',
                '金色装饰',
                '古代雕像',
                '烛台照明',
                '神秘氛围'
            ],
            'layout_templates': [
                {'name': '小型金字塔', 'size': '15x15', 'rooms': 2},
                {'name': '大型金字塔', 'size': '30x30', 'rooms': 5}
            ],
            'biome_recommendations': ['desert'],
            'difficulty': 'medium'
        },
        {
            'name': 'ocean_beach_house',
            'display_name': '海滩小屋',
            'description': '海洋海滩风格建筑',
            'primary_tiles': ['PalmWood', 'Glass', 'Coral'],
            'primary_walls': ['GlassWall', 'PalmWoodWall'],
            'accent_tiles': ['Seashell', 'Starfish', 'Bubbles'],
            'roof_style': 'triangular',
            'roof_tiles': ['PalmWood'],
            'furniture_style': ['Tables', 'Chairs', 'Lanterns', 'FishingDecorations'],
            'paint_scheme': {'primary': 0, 'ocean': 8, 'sand': 2},
            'architectural_rules': [
                '使用棕榈木',
                '大量玻璃窗户',
                '海洋装饰品',
                '悬挂灯笼',
                '开放式设计',
                '海滩色调'
            ],
            'layout_templates': [
                {'name': '海滩小屋', 'size': '12x10', 'rooms': 1},
                {'name': '海边别墅', 'size': '20x15', 'rooms': 3}
            ],
            'biome_recommendations': ['ocean', 'beach'],
            'difficulty': 'easy'
        },
        {
            'name': 'underground_cave_home',
            'display_name': '地下洞穴屋',
            'description': '地下洞穴风格住所',
            'primary_tiles': ['Stone', 'Dirt', 'Wood'],
            'primary_walls': ['StoneWall', 'DirtWall', 'WoodWall'],
            'accent_tiles': ['GemsparkBlocks', 'OreBlocks'],
            'roof_style': 'irregular',
            'roof_tiles': ['Wood', 'Stone'],
            'furniture_style': ['Torches', 'WorkBench', 'Chairs', 'Campfires'],
            'paint_scheme': {'primary': 0, 'shadow': 28},
            'architectural_rules': [
                '利用自然洞穴形状',
                '木质平台楼梯',
                '大量火把照明',
                '宝石方块装饰',
                '阴影油漆增加深度',
                '矿道风格设计'
            ],
            'layout_templates': [
                {'name': '小型洞穴屋', 'size': '10x10', 'rooms': 1},
                {'name': '大型地下基地', 'size': '30x20', 'rooms': 4}
            ],
            'biome_recommendations': ['underground', 'cavern'],
            'difficulty': 'easy'
        },
        {
            'name': 'modern_apartment',
            'display_name': '现代公寓',
            'description': '现代简约风格公寓',
            'primary_tiles': ['Granite', 'Glass', 'Marble'],
            'primary_walls': ['GraniteWall', 'GlassWall', 'MarbleWall'],
            'accent_tiles': ['MetalBars', 'ModernFurniture'],
            'roof_style': 'flat',
            'roof_tiles': ['Granite'],
            'furniture_style': ['Tables', 'Sofas', 'Lamps', 'Bookcases'],
            'paint_scheme': {'primary': 30, 'accent': 14, 'modern': 9},
            'architectural_rules': [
                '使用花岗岩和大理石',
                '大量玻璃幕墙',
                '简约线条设计',
                '现代灯具',
                '白色和蓝色调',
                '开放式布局'
            ],
            'layout_templates': [
                {'name': '小型公寓', 'size': '10x10', 'rooms': 1},
                {'name': '大型公寓', 'size': '20x20', 'rooms': 4}
            ],
            'biome_recommendations': ['any'],
            'difficulty': 'medium'
        },
        {
            'name': 'gothic_castle',
            'display_name': '哥特城堡',
            'description': '黑暗哥特风格城堡',
            'primary_tiles': ['Obsidian', 'Ebonstone', 'StoneBlock'],
            'primary_walls': ['ObsidianWall', 'EbonstoneWall', 'StoneWall'],
            'accent_tiles': ['SpookyDecorations', 'IronBrick'],
            'roof_style': 'spired',
            'roof_tiles': ['Obsidian', 'IronBrick'],
            'furniture_style': ['Chandeliers', 'Tables', 'Chairs', 'Candelabra'],
            'paint_scheme': {'primary': 31, 'shadow': 28, 'blood': 13},
            'architectural_rules': [
                '使用黑曜石和黑檀石',
                '尖塔屋顶设计',
                '黑色和深红色调',
                '烛台和吊灯照明',
                '哥特式拱门',
                '恐怖装饰品'
            ],
            'layout_templates': [
                {'name': '哥特小屋', 'size': '12x10', 'rooms': 1},
                {'name': '哥特城堡', 'size': '30x25', 'rooms': 5}
            ],
            'biome_recommendations': ['corruption', 'crimson'],
            'difficulty': 'hard'
        },
        {
            'name': 'village_cottage',
            'display_name': '乡村小屋',
            'description': '乡村田园风格小屋',
            'primary_tiles': ['Wood', 'GrayBrick', 'Dirt'],
            'primary_walls': ['WoodWall', 'GrayBrickWall'],
            'accent_tiles': ['Flowers', 'Hay', 'FarmDecorations'],
            'roof_style': 'triangular',
            'roof_tiles': ['Wood', 'Hay'],
            'furniture_style': ['WorkBench', 'Tables', 'Chairs', 'Campfires'],
            'paint_scheme': {'primary': 0, 'warm': 2},
            'architectural_rules': [
                '使用木材和砖块',
                '简单三角屋顶',
                '田园装饰',
                '篝火和灯笼',
                '花园设计',
                '温馨色调'
            ],
            'layout_templates': [
                {'name': '乡村小屋', 'size': '10x8', 'rooms': 1},
                {'name': '农舍', 'size': '15x12', 'rooms': 2}
            ],
            'biome_recommendations': ['forest', 'plain'],
            'difficulty': 'easy'
        },
        {
            'name': 'mushroom_house',
            'display_name': '蘑菇屋',
            'description': '发光蘑菇生物群落风格',
            'primary_tiles': ['MushroomBlock', 'GlowingMushroom', 'Wood'],
            'primary_walls': ['MushroomWall', 'WoodWall'],
            'accent_tiles': ['MushroomDecorations', 'MushroomFurniture'],
            'roof_style': 'dome',
            'roof_tiles': ['MushroomBlock'],
            'furniture_style': ['MushroomWorkBench', 'MushroomTables', 'MushroomChairs'],
            'paint_scheme': {'primary': 0, 'mushroom': 6},
            'architectural_rules': [
                '使用蘑菇方块',
                '圆顶蘑菇形状',
                '蘑菇家具',
                '自然发光',
                '蓝绿色调',
                '有机形状设计'
            ],
            'layout_templates': [
                {'name': '蘑菇小屋', 'size': '10x10', 'rooms': 1},
                {'name': '蘑菇庄园', 'size': '20x15', 'rooms': 3}
            ],
            'biome_recommendations': ['glowing_mushroom'],
            'difficulty': 'medium'
        },
        {
            'name': 'jungle_temple',
            'display_name': '丛林神庙',
            'description': '丛林风格神庙建筑',
            'primary_tiles': ['RichMahogany', 'JungleWood', 'LihzahrdBrick'],
            'primary_walls': ['RichMahoganyWall', 'JungleWall', 'LihzahrdWall'],
            'accent_tiles': ['Vines', 'JunglePlants', 'Spikes'],
            'roof_style': 'stepped',
            'roof_tiles': ['RichMahogany', 'LihzahrdBrick'],
            'furniture_style': ['Tables', 'Chairs', 'Banners', 'Statues'],
            'paint_scheme': {'primary': 0, 'jungle': 5, 'gold': 2},
            'architectural_rules': [
                '使用红木和丛林木',
                '阶梯金字塔屋顶',
                '藤蔓和植物装饰',
                '丛林色调',
                '神庙风格设计',
                '古代雕像'
            ],
            'layout_templates': [
                {'name': '丛林小屋', 'size': '10x8', 'rooms': 1},
                {'name': '丛林神庙', 'size': '25x20', 'rooms': 4}
            ],
            'biome_recommendations': ['jungle'],
            'difficulty': 'medium'
        },
        {
            'name': 'dungeon_keep',
            'display_name': '地牢堡垒',
            'description': '地牢风格堡垒建筑',
            'primary_tiles': ['DungeonBrick', 'StoneBlock', 'IronBrick'],
            'primary_walls': ['DungeonWall', 'StoneWall'],
            'accent_tiles': ['Spikes', 'Chains', 'Trophies'],
            'roof_style': 'flat',
            'roof_tiles': ['DungeonBrick'],
            'furniture_style': ['Tables', 'Chairs', 'Chandeliers', 'Trophies'],
            'paint_scheme': {'primary': 0, 'dungeon': 10, 'accent': 31},
            'architectural_rules': [
                '使用地牢砖',
                '蓝紫色调',
                '铁栏杆和链条',
                '奖杯装饰',
                '烛台照明',
                '阴森氛围'
            ],
            'layout_templates': [
                {'name': '地牢房间', 'size': '15x12', 'rooms': 1},
                {'name': '地牢堡垒', 'size': '30x25', 'rooms': 5}
            ],
            'biome_recommendations': ['dungeon'],
            'difficulty': 'medium'
        }
    ]

    return style_templates


def generate_layout_templates():
    """生成布局模板"""
    print("\n=== 生成布局模板 ===")

    layouts = []

    # 基础房间布局
    basic_room_layouts = [
        {
            'name': 'basic_npc_room',
            'display_name': '基础NPC房间',
            'description': '满足NPC居住最低要求的房间',
            'min_width': 7,
            'min_height': 7,
            'max_width': 30,
            'max_height': 30,
            'min_tiles': 60,
            'furniture_placement': {
                'light': {'position': 'center_ceiling', 'options': ['Torch', 'Candle', 'Chandelier']},
                'table': {'position': 'floor_left', 'options': ['Table', 'WorkBench', 'Dresser']},
                'chair': {'position': 'floor_right', 'options': ['Chair', 'Bed', 'Sofa']},
                'door': {'position': 'bottom_wall', 'options': ['Door', 'Platform']}
            },
            'wall_pattern': 'solid',
            'floor_pattern': 'solid',
            'ceiling_pattern': 'solid',
            'difficulty': 'easy'
        },
        {
            'name': 'comfortable_room',
            'display_name': '舒适房间',
            'description': '额外装饰的舒适房间',
            'min_width': 10,
            'min_height': 8,
            'max_width': 40,
            'max_height': 40,
            'min_tiles': 80,
            'furniture_placement': {
                'light': {'position': 'center_ceiling', 'options': ['Chandelier', 'Lamp', 'Candles']},
                'light_secondary': {'position': 'corners', 'options': ['Torch', 'Candle']},
                'table': {'position': 'floor_left', 'options': ['Table', 'WorkBench', 'Dresser']},
                'chair': {'position': 'floor_right', 'options': ['Chair', 'Sofa']},
                'bed': {'position': 'floor_corner', 'options': ['Bed']},
                'door': {'position': 'bottom_wall', 'options': ['Door', 'Platform']},
                'decoration': {'position': 'walls', 'options': ['Painting', 'Banner', 'Statue']}
            },
            'wall_pattern': 'solid',
            'floor_pattern': 'solid_with_decor',
            'ceiling_pattern': 'solid_with_light',
            'difficulty': 'easy'
        },
        {
            'name': 'multi_story_house',
            'display_name': '多层房屋',
            'description': '两层或多层房屋布局',
            'min_width': 10,
            'min_height': 15,
            'max_width': 30,
            'max_height': 50,
            'min_tiles': 150,
            'furniture_placement': {
                'floor_1': {
                    'light': {'position': 'center_ceiling', 'options': ['Chandelier']},
                    'table': {'position': 'floor_center', 'options': ['Table']},
                    'chair': {'position': 'floor_sides', 'options': ['Chair']},
                    'door': {'position': 'bottom_wall', 'options': ['Door']},
                    'stairs': {'position': 'side', 'options': ['Platform']}
                },
                'floor_2': {
                    'light': {'position': 'center_ceiling', 'options': ['Lamp', 'Candle']},
                    'bed': {'position': 'floor_corner', 'options': ['Bed']},
                    'table': {'position': 'floor_side', 'options': ['WorkBench', 'Dresser']},
                    'chair': {'position': 'floor_side', 'options': ['Chair', 'Sofa']}
                }
            },
            'wall_pattern': 'solid',
            'floor_pattern': 'solid',
            'ceiling_pattern': 'solid',
            'difficulty': 'medium'
        }
    ]

    # 特殊房间布局
    special_layouts = [
        {
            'name': 'crafting_room',
            'display_name': '制作室',
            'description': '集中放置制作站的房间',
            'min_width': 12,
            'min_height': 8,
            'max_width': 25,
            'max_height': 20,
            'min_tiles': 96,
            'furniture_placement': {
                'crafting_stations': {
                    'positions': ['floor_left', 'floor_center', 'floor_right'],
                    'options': ['WorkBench', 'Anvil', 'Furnace', 'AlchemyTable', 'Sawmill']
                },
                'storage': {'position': 'floor_corner', 'options': ['Chest', 'Safe']},
                'light': {'position': 'ceiling', 'options': ['Chandelier', 'Lamp']}
            },
            'wall_pattern': 'solid',
            'floor_pattern': 'solid',
            'ceiling_pattern': 'solid',
            'difficulty': 'medium'
        },
        {
            'name': 'storage_room',
            'display_name': '储藏室',
            'description': '大量存储容器的房间',
            'min_width': 15,
            'min_height': 8,
            'max_width': 30,
            'max_height': 20,
            'min_tiles': 120,
            'furniture_placement': {
                'storage': {
                    'positions': ['floor_rows', 'wall_sides'],
                    'options': ['Chest', 'Safe', 'PiggyBank', 'DefenderForge']
                },
                'light': {'position': 'ceiling', 'options': ['Torch', 'Lamp']}
            },
            'wall_pattern': 'solid',
            'floor_pattern': 'solid',
            'ceiling_pattern': 'solid',
            'difficulty': 'easy'
        },
        {
            'name': 'npc_village',
            'display_name': 'NPC村庄布局',
            'description': '多个NPC房屋的村庄布局',
            'layout_type': 'village',
            'house_spacing': 5,
            'road_width': 3,
            'village_size': {'small': 3, 'medium': 8, 'large': 15},
            'common_buildings': ['TownCenter', 'Market', 'CraftingArea'],
            'difficulty': 'medium'
        },
        {
            'name': 'tower_layout',
            'display_name': '塔楼布局',
            'description': '垂直塔楼结构布局',
            'min_width': 7,
            'min_height': 20,
            'max_width': 12,
            'max_height': 50,
            'floor_height': 5,
            'furniture_placement': {
                'each_floor': {
                    'light': {'position': 'ceiling', 'options': ['Torch', 'Chandelier']},
                    'table': {'position': 'floor', 'options': ['Table', 'WorkBench']},
                    'chair': {'position': 'floor', 'options': ['Chair']},
                    'door': {'position': 'wall', 'options': ['Platform']}
                }
            },
            'roof_type': 'conical',
            'difficulty': 'medium'
        }
    ]

    layouts.extend(basic_room_layouts)
    layouts.extend(special_layouts)

    return layouts


def update_database(all_data):
    """更新数据库"""
    print("\n=== 更新数据库 ===")

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    # 添加新表：layout_templates
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS layout_templates (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL UNIQUE,
            display_name TEXT,
            description TEXT,
            layout_type TEXT,
            min_width INTEGER,
            min_height INTEGER,
            max_width INTEGER,
            max_height INTEGER,
            min_tiles INTEGER,
            furniture_placement TEXT,
            wall_pattern TEXT,
            floor_pattern TEXT,
            ceiling_pattern TEXT,
            difficulty TEXT
        )
    """)

    # 添加新表：enhanced_furniture
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS enhanced_furniture (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            display_name TEXT,
            category TEXT,
            sub_category TEXT,
            width INTEGER DEFAULT 1,
            height INTEGER DEFAULT 1,
            paint_compatible INTEGER DEFAULT 1,
            light_emission INTEGER DEFAULT 0,
            npc_function TEXT,
            crafting_station TEXT,
            source_biome TEXT,
            rarity TEXT,
            description TEXT,
            source_url TEXT
        )
    """)

    # 添加新表：building_rules
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS building_rules (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            rule_name TEXT NOT NULL UNIQUE,
            category TEXT,
            priority INTEGER,
            rule_text TEXT,
            applies_to TEXT,
            exceptions TEXT,
            description TEXT
        )
    """)

    # 插入布局模板
    layouts = all_data.get('layouts', [])
    for layout in layouts:
        cursor.execute("""
            INSERT OR IGNORE INTO layout_templates
            (name, display_name, description, layout_type, min_width, min_height,
             max_width, max_height, min_tiles, furniture_placement, wall_pattern,
             floor_pattern, ceiling_pattern, difficulty)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            layout.get('name'),
            layout.get('display_name'),
            layout.get('description'),
            layout.get('layout_type', 'room'),
            layout.get('min_width'),
            layout.get('min_height'),
            layout.get('max_width'),
            layout.get('max_height'),
            layout.get('min_tiles'),
            json.dumps(layout.get('furniture_placement', {})),
            layout.get('wall_pattern'),
            layout.get('floor_pattern'),
            layout.get('ceiling_pattern'),
            layout.get('difficulty')
        ))

    # 更新风格模板（添加布局信息）
    styles = all_data.get('styles', [])
    for style in styles:
        # 检查是否存在
        cursor.execute("SELECT id FROM style_templates WHERE name = ?", (style['name'],))
        existing = cursor.fetchone()

        if existing:
            # 更新现有记录
            cursor.execute("""
                UPDATE style_templates SET
                    display_name = ?,
                    description = ?,
                    architectural_rules = ?,
                    layout_templates = ?
                WHERE name = ?
            """, (
                style.get('display_name'),
                style.get('description'),
                json.dumps(style.get('architectural_rules', [])),
                json.dumps(style.get('layout_templates', [])),
                style['name']
            ))
        else:
            # 插入新记录
            cursor.execute("""
                INSERT INTO style_templates
                (name, display_name, description, primary_tiles, primary_walls,
                 accent_tiles, roof_style, roof_tiles, furniture_style, paint_scheme,
                 architectural_rules, biome_recommendations, difficulty, layout_templates)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                style['name'],
                style.get('display_name'),
                style.get('description'),
                json.dumps(style.get('primary_tiles', [])),
                json.dumps(style.get('primary_walls', [])),
                json.dumps(style.get('accent_tiles', [])),
                style.get('roof_style'),
                json.dumps(style.get('roof_tiles', [])),
                json.dumps(style.get('furniture_style', [])),
                json.dumps(style.get('paint_scheme', {})),
                json.dumps(style.get('architectural_rules', [])),
                json.dumps(style.get('biome_recommendations', [])),
                style.get('difficulty'),
                json.dumps(style.get('layout_templates', []))
            ))

    # 插入爬取的家具数据
    furniture = all_data.get('furniture', [])
    for f in furniture:
        raw = f.get('raw_data', {})
        cursor.execute("""
            INSERT OR IGNORE INTO enhanced_furniture
            (name, category, sub_category, source_url, description)
            VALUES (?, ?, ?, ?, ?)
        """, (
            f.get('name'),
            f.get('category'),
            raw.get('type', ''),
            f.get('source'),
            json.dumps(raw)
        ))

    # 插入建筑规则
    rules = [
        ('npc_room_minimum', 'housing', 1, '房间必须有至少60格空地', 'all_npcs', None, 'NPC房屋最低尺寸要求'),
        ('npc_room_light', 'housing', 2, '房间必须有光源', 'all_npcs', None, '火把、蜡烛、吊灯等'),
        ('npc_room_table', 'housing', 3, '房间必须有平坦表面', 'all_npcs', None, '桌子、工作台、梳妆台'),
        ('npc_room_chair', 'housing', 4, '房间必须有舒适物品', 'all_npcs', None, '椅子、床、沙发'),
        ('npc_room_door', 'housing', 5, '房间必须有入口', 'all_npcs', None, '门、活板门、平台'),
        ('npc_room_wall', 'housing', 6, '房间必须有背景墙', 'all_npcs', 'natural_walls', '玩家放置的墙'),
        ('roof_overhang', 'decoration', 7, '屋顶可以延伸1-2格超出墙壁', 'decorative', None, '增加建筑美观'),
        ('paint_depth', 'decoration', 8, '使用阴影油漆增加层次感', 'decorative', None, '阴影油漆效果'),
        ('biome_theming', 'style', 9, '建筑风格应匹配周围生物群落', 'styling', None, '环境融合'),
        ('furniture_grouping', 'layout', 10, '家具应成组放置而非分散', 'layout', None, '房间布局建议'),
    ]

    for rule in rules:
        cursor.execute("""
            INSERT OR IGNORE INTO building_rules
            (rule_name, category, priority, rule_text, applies_to, exceptions, description)
            VALUES (?, ?, ?, ?, ?, ?, ?)
        """, rule)

    conn.commit()

    # 统计
    print("\n数据库更新统计:")
    tables = ['layout_templates', 'enhanced_furniture', 'building_rules', 'style_templates']
    for t in tables:
        cursor.execute(f"SELECT COUNT(*) FROM {t}")
        count = cursor.fetchone()[0]
        print(f"  {t}: {count}")

    conn.close()


def generate_json_output(all_data):
    """生成JSON输出文件"""
    print("\n=== 生成JSON数据文件 ===")

    json_path = os.path.join(DATA_DIR, "building_data.json")

    output = {
        'metadata': {
            'generated_at': datetime.now().isoformat(),
            'version': '2.0',
            'source': 'Terraria Wiki + Manual'
        },
        'styles': all_data.get('styles', []),
        'layouts': all_data.get('layouts', []),
        'furniture': all_data.get('furniture', []),
        'materials': all_data.get('materials', []),
        'light_sources': all_data.get('light_sources', []),
        'npcs': all_data.get('npcs', []),
        'biomes': all_data.get('biomes', []),
        'tutorials': all_data.get('tutorials', [])
    }

    with open(json_path, 'w', encoding='utf-8') as f:
        json.dump(output, f, ensure_ascii=False, indent=2)

    print(f"  JSON文件: {json_path}")
    print(f"  数据大小: {len(json.dumps(output))} 字符")


def main():
    print("=" * 60)
    print("Terraria建筑数据爬虫")
    print("=" * 60)

    all_data = {}

    # 爬取Wiki数据
    print("\n[1/6] 爬取Wiki家具数据...")
    all_data['furniture'] = crawl_furniture()

    print("\n[2/6] 爬取Wiki建筑材料数据...")
    all_data['materials'] = crawl_building_materials()

    print("\n[3/6] 爬取Wiki光源数据...")
    all_data['light_sources'] = crawl_light_sources()

    print("\n[4/6] 爬取Wiki NPC数据...")
    all_data['npcs'] = crawl_npc_housing()

    print("\n[5/6] 爬取Wiki生物群落数据...")
    all_data['biomes'] = crawl_biomes()

    print("\n[6/6] 爬取Wiki建筑教程数据...")
    all_data['tutorials'] = crawl_building_tutorials()

    # 生成详细风格模板
    print("\n[补充] 生成详细建筑风格模板...")
    all_data['styles'] = extract_building_styles()

    # 生成布局模板
    print("\n[补充] 生成布局模板...")
    all_data['layouts'] = generate_layout_templates()

    # 更新数据库
    update_database(all_data)

    # 生成JSON文件
    generate_json_output(all_data)

    print("\n" + "=" * 60)
    print("完成!")
    print("=" * 60)


if __name__ == "__main__":
    main()