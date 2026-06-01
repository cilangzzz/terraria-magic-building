#!/usr/bin/env python3
"""
Terraria Wiki API 爬虫
使用 MediaWiki API 从 terraria.wiki.gg 爬取方块和墙壁数据

使用方法:
    python tile_crawler.py           # 爬取所有数据
    python tile_crawler.py --tiles   # 只爬取方块
    python tile_crawler.py --walls   # 只爬取墙壁
    python tile_crawler.py --furniture # 爬取家具信息
"""

import argparse
import json
import os
import re
import sqlite3
import time
from datetime import datetime
from urllib.parse import quote

import requests

# 配置
BASE_URL = "https://terraria.wiki.gg"
API_URL = f"{BASE_URL}/api.php"  # 正确的API路径
OUTPUT_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Data", "crawled")
DB_PATH = os.path.join(os.path.dirname(__file__), "..", "..", "Data", "terraria_kb.db")
HEADERS = {
    "User-Agent": "TerrariaTileCrawler/1.0 (Python script for data collection)",
    "Accept": "application/json",
}
DELAY = 0.3  # API请求间隔


class TerrariaWikiCrawler:
    def __init__(self):
        self.session = requests.Session()
        self.session.headers.update(HEADERS)
        self.tiles = []
        self.walls = []
        self.furniture = []
        self.light_sources = []
        self.doors = []

    def api_query(self, params):
        """执行MediaWiki API查询"""
        try:
            print(f"  API请求: {params.get('action', 'unknown')}")
            response = self.session.get(API_URL, params=params, timeout=30)
            response.raise_for_status()
            time.sleep(DELAY)
            return response.json()
        except Exception as e:
            print(f"  API错误: {e}")
            return None

    def get_page_content(self, title):
        """获取页面原始内容"""
        params = {
            "action": "query",
            "titles": title,
            "prop": "revisions",
            "rvprop": "content",
            "format": "json",
            "formatversion": "2",
        }
        result = self.api_query(params)
        if result and "query" in result and "pages" in result["query"]:
            for page in result["query"]["pages"]:
                if "revisions" in page:
                    return page["revisions"][0]["content"]
        return None

    def get_parsed_page(self, title):
        """获取解析后的页面HTML"""
        params = {
            "action": "parse",
            "page": title,
            "prop": "text",
            "format": "json",
            "formatversion": "2",
        }
        result = self.api_query(params)
        if result and "parse" in result:
            return result["parse"]["text"]
        return None

    def query_category(self, category, limit=500):
        """查询分类下的所有页面"""
        params = {
            "action": "query",
            "list": "categorymembers",
            "cmtitle": f"Category:{category}",
            "cmlimit": limit,
            "format": "json",
            "formatversion": "2",
        }
        result = self.api_query(params)
        if result and "query" in result:
            return result["query"]["categorymembers"]
        return []

    def parse_tile_ids_table(self, content):
        """解析Tile IDs页面的表格数据"""
        if not content:
            return []

        tiles = []
        seen_ids = set()  # 防止重复ID

        # 表格格式: | ID || Sub ID || Picture || Item/Entity || Internal Name
        lines = content.split("\n")
        for line in lines:
            # 查找表格行，跳过表头和特殊行
            if line.startswith("| ") and not line.startswith("|-") and not line.startswith("|}"):
                # 解析单元格，使用 || 分隔
                cells = [c.strip() for c in line.split("||")]
                if len(cells) >= 5:
                    # 第1列: ID
                    id_text = cells[0].replace("|", "").strip()
                    try:
                        tile_id = int(id_text)
                    except ValueError:
                        continue

                    # 跳过已处理的ID（由于有Sub ID变体，同一个ID可能出现多次）
                    if tile_id in seen_ids:
                        continue
                    seen_ids.add(tile_id)

                    # 第4列: Item/Entity，提取名称
                    item_cell = cells[3]
                    # 解析 {{item|Name}} 或 {{eil|Name}} 或 [[Name]]
                    name = None

                    # {{item|Name}} 格式
                    item_match = re.search(r"\{\{item\|([^}|]+)", item_cell)
                    if item_match:
                        name = item_match.group(1)

                    # {{eil|Name}} 格式 (entity item link)
                    if not name:
                        eil_match = re.search(r"\{\{eil\|([^}|]+)", item_cell)
                        if eil_match:
                            name = eil_match.group(1)

                    # [[Name]] 格式
                    if not name:
                        link_match = re.search(r"\[\[([^\]|]+)", item_cell)
                        if link_match:
                            name = link_match.group(1)

                    # 第5列: Internal Name，格式 <code>Name</code>
                    internal_cell = cells[4]
                    internal_match = re.search(r"<code>([^<]+)</code>", internal_cell)
                    if internal_match:
                        internal_name = internal_match.group(1)
                    else:
                        internal_name = name or f"Tile_{tile_id}"

                    if not name:
                        name = internal_name

                    # 清理名称
                    name = self.clean_name(name)

                    tiles.append({
                        "id": tile_id,
                        "name": name,
                        "display_name": name,
                        "internal_name": internal_name,
                        "category": self.guess_category_from_name(name),
                        "source": "wiki_api"
                    })

        return tiles

    def guess_category_from_name(self, name):
        """根据名称猜测分类"""
        name_lower = name.lower()
        if any(x in name_lower for x in ["brick", "wall", "stone", "slab", "plate"]):
            return "brick"
        elif any(x in name_lower for x in ["wood", "plank", "log", "tree"]):
            return "wood"
        elif any(x in name_lower for x in ["platform", "bridge"]):
            return "platform"
        elif any(x in name_lower for x in ["torch", "lamp", "candle", "chandelier", "lantern", "light"]):
            return "light"
        elif any(x in name_lower for x in ["door", "gate", "trapdoor"]):
            return "door"
        elif any(x in name_lower for x in ["table", "chair", "bed", "bench", "sofa", "dresser", "piano", "bathtub"]):
            return "furniture"
        elif any(x in name_lower for x in ["chest", "safe", "vault"]):
            return "storage"
        elif any(x in name_lower for x in ["grass", "dirt", "sand", "clay", "mud", "snow", "ice"]):
            return "natural"
        elif any(x in name_lower for x in ["corrupt", "crimson", "hallow", "ebon", "crim", "pearl"]):
            return "special"
        elif any(x in name_lower for x in ["glass", "gem", "crystal"]):
            return "transparent"
        elif any(x in name_lower for x in ["gold", "silver", "copper", "iron", "platinum", "obsidian"]):
            return "luxury"
        return "basic"

    def crawl_tiles_from_page(self):
        """从Tile IDs页面爬取方块数据"""
        print("\n=== 爬取方块ID数据 ===")

        # Tile IDs页面使用分页结构，需要获取子页面
        # Part1: 0-30, Part2: 31-90, ..., Part9: 693-752
        parts = ["Tile IDs/Part1", "Tile IDs/Part2", "Tile IDs/Part3", "Tile IDs/Part4",
                 "Tile IDs/Part5", "Tile IDs/Part6", "Tile IDs/Part7", "Tile IDs/Part8", "Tile IDs/Part9"]

        for part in parts:
            content = self.get_page_content(part)
            if content:
                tiles = self.parse_tile_ids_table(content)
                print(f"  {part}: {len(tiles)} 个方块")
                self.tiles.extend(tiles)
            time.sleep(DELAY)

        print(f"  总计: {len(self.tiles)} 个方块")

        # 如果分页获取失败，尝试获取主页面并解析表格
        if len(self.tiles) == 0:
            print("  分页获取失败，尝试解析主页面...")
            content = self.get_page_content("Tile IDs")
            if content:
                tiles = self.parse_tile_ids_table(content)
                self.tiles.extend(tiles)
                print(f"  从主页获取到 {len(tiles)} 个方块")

        # 如果仍然失败，尝试获取解析后的HTML页面
        if len(self.tiles) == 0:
            print("  尝试获取解析后的HTML...")
            for part in parts[:3]:  # 只尝试前3页
                html = self.get_parsed_page(part)
                if html:
                    tiles = self.parse_html_table(html)
                    print(f"  {part} (HTML): {len(tiles)} 个方块")
                    self.tiles.extend(tiles)
                time.sleep(DELAY)

    def parse_html_table(self, html):
        """解析HTML格式的表格"""
        tiles = []
        if not html:
            return tiles

        # 使用正则表达式解析HTML表格
        # 查找表格行 <tr>...</tr>
        tr_pattern = r'<tr[^>]*>(.*?)</tr>'
        td_pattern = r'<td[^>]*>(.*?)</td>'

        for tr_match in re.finditer(tr_pattern, html, re.DOTALL):
            tr_content = tr_match.group(1)
            tds = re.findall(td_pattern, tr_content, re.DOTALL)

            if len(tds) >= 2:
                # 第一个td是ID
                id_text = re.sub(r'<[^>]+>', '', tds[0]).strip()
                try:
                    tile_id = int(id_text)
                except ValueError:
                    continue

                # 第二个td是名称
                name_cell = re.sub(r'<[^>]+>', '', tds[1]).strip()
                name = self.clean_name(name_cell)

                tiles.append({
                    "id": tile_id,
                    "name": name,
                    "display_name": name,
                    "source": "wiki_html"
                })

        return tiles

    def crawl_walls_from_page(self):
        """从Wall IDs页面爬取墙壁数据"""
        print("\n=== 爬取墙壁ID数据 ===")

        # Wall IDs页面使用动态模板，需要获取解析后的HTML
        html = self.get_parsed_page("Wall_IDs")
        if html:
            walls = self.parse_html_table(html)
            print(f"  从HTML解析到 {len(walls)} 个墙壁")
            self.walls.extend(walls)

        # 如果HTML解析失败，尝试获取原始内容
        if len(self.walls) == 0:
            content = self.get_page_content("Wall_IDs")
            if content:
                walls = self.parse_wall_ids_table(content)
                print(f"  从原始内容解析到 {len(walls)} 个墙壁")
                self.walls.extend(walls)

        print(f"  总计: {len(self.walls)} 个墙壁")

    def parse_wall_ids_table(self, content):
        """解析Wall IDs表格"""
        walls = []
        if not content:
            return walls

        lines = content.split("\n")
        for line in lines:
            if line.startswith("|") and not line.startswith("|-") and not line.startswith("|}"):
                cells = [c.strip() for c in line.split("||")]
                if len(cells) >= 2:
                    id_text = cells[0].replace("|", "").strip()
                    try:
                        wall_id = int(id_text)
                    except ValueError:
                        continue

                    name_cell = cells[1]
                    name_match = re.search(r"\[\[([^\]|]+)", name_cell)
                    if name_match:
                        name = name_match.group(1)
                    else:
                        name = name_cell

                    name = self.clean_name(name)
                    walls.append({
                        "id": wall_id,
                        "name": name,
                        "display_name": name,
                        "source": "wiki_api"
                    })

        return walls

    def crawl_furniture_data(self):
        """爬取家具数据"""
        print("\n=== 爬取家具数据 ===")

        # 定义家具类型及其页面
        furniture_pages = {
            "Tables": {"id": 14, "category": "table", "width": 3, "height": 1, "npc_function": "flat_surface"},
            "Chairs": {"id": 15, "category": "chair", "width": 1, "height": 2, "npc_function": "comfort"},
            "Beds": {"id": 79, "category": "bed", "width": 4, "height": 2, "npc_function": ["comfort", "spawn_point"]},
            "Chests": {"id": 21, "category": "chest", "width": 2, "height": 2, "npc_function": "storage"},
            "Work Benches": {"id": 18, "category": "crafting", "width": 2, "height": 1, "npc_function": ["flat_surface", "crafting"]},
            "Dressers": {"id": 88, "category": "dresser", "width": 3, "height": 2, "npc_function": ["flat_surface", "storage"]},
            "Bookcases": {"id": 101, "category": "bookcase", "width": 3, "height": 4, "npc_function": "flat_surface"},
            "Pianos": {"id": 87, "category": "piano", "width": 3, "height": 2, "npc_function": "flat_surface"},
            "Bathtubs": {"id": 90, "category": "bathtub", "width": 4, "height": 2},
            "Sofas": {"id": 89, "category": "sofa", "width": 3, "height": 2, "npc_function": "comfort"},
            "Platforms": {"id": 19, "category": "platform", "width": 1, "height": 1, "npc_function": "door"},
        }

        for page_name, base_info in furniture_pages.items():
            content = self.get_page_content(page_name)
            variants = self.parse_variants_from_content(content, page_name)

            furniture_item = {
                "id": base_info["id"],
                "name": page_name.replace(" ", ""),
                "display_name": page_name,
                "category": base_info["category"],
                "width": base_info.get("width", 1),
                "height": base_info.get("height", 1),
                "npc_function": base_info.get("npc_function"),
                "paint_compatible": True,
                "variants": variants,
                "source": "wiki_api"
            }
            self.furniture.append(furniture_item)

        print(f"  获取到 {len(self.furniture)} 类家具")

    def parse_variants_from_content(self, content, item_type):
        """从页面内容解析变体列表"""
        variants = []
        if not content:
            return variants

        # 查找变体列表
        # 通常格式为: {{infobox|...}} 或列表
        variant_patterns = [
            r"\*\s*\[\[([^\]]+)\]\]",  # * [[Variant Name]]
            r"\|\s*variants\s*=\s*([^\n]+)",  # | variants = ...
        ]

        for pattern in variant_patterns:
            matches = re.findall(pattern, content)
            for match in matches:
                if isinstance(match, str):
                    name = match.split("|")[0].strip()
                    if name and not name.startswith("Category"):
                        variants.append(name)

        return variants[:20]  # 限制数量

    def crawl_light_sources(self):
        """爬取光源数据"""
        print("\n=== 爬取光源数据 ===")

        light_types = {
            "Torches": {"id": 4, "category": "torch", "light_radius": 10},
            "Candles": {"id": 33, "category": "candle", "light_radius": 5},
            "Chandeliers": {"id": 34, "category": "chandelier", "light_radius": 15},
            "Lanterns": {"id": 42, "category": "lantern", "light_radius": 10},
            "Campfires": {"id": 215, "category": "campfire", "light_radius": 20},
        }

        for name, base_info in light_types.items():
            self.light_sources.append({
                "id": base_info["id"],
                "name": name,
                "display_name": name,
                "category": base_info["category"],
                "light_radius": base_info["light_radius"],
                "npc_function": "light_source",
                "source": "wiki_api"
            })

        print(f"  获取到 {len(self.light_sources)} 类光源")

    def crawl_doors(self):
        """爬取门数据"""
        print("\n=== 爬取门数据 ===")

        door_types = [
            {"id": 10, "name": "Doors", "display_name": "门", "width": 1, "height": 3},
            {"id": 387, "name": "TrapDoor", "display_name": "活板门", "width": 2, "height": 1},
            {"id": 388, "name": "TallGate", "display_name": "大门", "width": 1, "height": 5},
        ]

        for door in door_types:
            self.doors.append({
                "id": door["id"],
                "name": door["name"],
                "display_name": door["display_name"],
                "category": "door",
                "width": door["width"],
                "height": door["height"],
                "npc_function": "door",
                "paint_compatible": True,
                "source": "wiki_api"
            })

        print(f"  获取到 {len(self.doors)} 类门")

    def clean_name(self, name):
        """清理名称"""
        # 移除括号和特殊字符
        name = re.sub(r"\[.*?\]", "", name)
        name = re.sub(r"\(.*?\)", "", name)
        name = name.replace("'", "").replace('"', "").replace("&nbsp;", " ")
        name = name.strip()

        # 转换为标识符格式
        if " " in name:
            parts = name.split()
            name = "".join(p.capitalize() for p in parts)

        return name

    def generate_sql(self):
        """生成SQL插入文件"""
        print("\n=== 生成SQL文件 ===")

        os.makedirs(OUTPUT_DIR, exist_ok=True)

        # 生成tiles SQL
        tiles_sql_path = os.path.join(OUTPUT_DIR, "tiles_insert.sql")
        with open(tiles_sql_path, "w", encoding="utf-8") as f:
            f.write("-- Terraria Tiles Data (API Crawl)\n")
            f.write(f"-- Generated: {datetime.now().isoformat()}\n")
            f.write(f"-- Total: {len(self.tiles)} tiles\n\n")

            for tile in self.tiles:
                sql = f"""INSERT OR IGNORE INTO tiles (id, name, display_name, category, source) VALUES (
    {tile['id']}, '{self.escape_sql(tile['name'])}', '{self.escape_sql(tile['display_name'])}',
    '{tile.get('category', 'basic')}', 'wiki_api'
);\n"""
                f.write(sql)

        print(f"  生成: {tiles_sql_path}")

        # 生成walls SQL
        walls_sql_path = os.path.join(OUTPUT_DIR, "walls_insert.sql")
        with open(walls_sql_path, "w", encoding="utf-8") as f:
            f.write("-- Terraria Walls Data (API Crawl)\n")
            f.write(f"-- Generated: {datetime.now().isoformat()}\n")
            f.write(f"-- Total: {len(self.walls)} walls\n\n")

            for wall in self.walls:
                sql = f"""INSERT OR IGNORE INTO walls (id, name, display_name, category, source) VALUES (
    {wall['id']}, '{self.escape_sql(wall['name'])}', '{self.escape_sql(wall['display_name'])}',
    '{wall.get('category', 'basic')}', 'wiki_api'
);\n"""
                f.write(sql)

        print(f"  生成: {walls_sql_path}")

        # 生成完整SQL（包含所有数据）
        full_sql_path = os.path.join(OUTPUT_DIR, "full_insert.sql")
        with open(full_sql_path, "w", encoding="utf-8") as f:
            f.write("-- Terraria Complete Data (API Crawl)\n")
            f.write(f"-- Generated: {datetime.now().isoformat()}\n\n")

            # 写入tiles
            with open(tiles_sql_path, "r", encoding="utf-8") as tf:
                f.write(tf.read())

            f.write("\n")

            # 写入walls
            with open(walls_sql_path, "r", encoding="utf-8") as wf:
                f.write(wf.read())

        print(f"  生成: {full_sql_path}")

    def generate_json(self):
        """生成JSON文件"""
        print("\n=== 生成JSON文件 ===")

        os.makedirs(OUTPUT_DIR, exist_ok=True)

        data = {
            "tiles": self.tiles,
            "walls": self.walls,
            "furniture": self.furniture,
            "light_sources": self.light_sources,
            "doors": self.doors,
            "metadata": {
                "source": "terraria.wiki.gg API",
                "crawl_date": datetime.now().isoformat(),
                "version": "1.4.5"
            }
        }

        json_path = os.path.join(OUTPUT_DIR, "wiki_data.json")
        with open(json_path, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, ensure_ascii=False)

        print(f"  生成: {json_path}")

    def import_to_database(self):
        """直接导入到SQLite数据库"""
        print("\n=== 导入SQLite数据库 ===")

        db_path = DB_PATH
        os.makedirs(os.path.dirname(db_path), exist_ok=True)

        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()

        # 创建表结构
        self.create_tables(cursor)

        # 导入tiles
        for tile in self.tiles:
            cursor.execute("""
                INSERT OR IGNORE INTO tiles (id, name, display_name, category, source)
                VALUES (?, ?, ?, ?, ?)
            """, (tile['id'], tile['name'], tile['display_name'], tile.get('category', 'basic'), 'wiki_api'))

        # 导入walls
        for wall in self.walls:
            cursor.execute("""
                INSERT OR IGNORE INTO walls (id, name, display_name, category, source)
                VALUES (?, ?, ?, ?, ?)
            """, (wall['id'], wall['name'], wall['display_name'], wall.get('category', 'basic'), 'wiki_api'))

        # 导入furniture
        for furn in self.furniture:
            npc_func = json.dumps(furn.get('npc_function')) if furn.get('npc_function') else None
            variants = json.dumps(furn.get('variants', []))
            cursor.execute("""
                INSERT OR IGNORE INTO furniture (id, name, display_name, category, width, height, npc_function, paint_compatible, source)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (furn['id'], furn['name'], furn['display_name'], furn['category'],
                  furn.get('width', 1), furn.get('height', 1), npc_func, 1, 'wiki_api'))

        # 导入light_sources
        for light in self.light_sources:
            cursor.execute("""
                INSERT OR IGNORE INTO light_sources (id, name, display_name, category, light_radius, npc_function, source)
                VALUES (?, ?, ?, ?, ?, ?, ?)
            """, (light['id'], light['name'], light['display_name'], light['category'],
                  light.get('light_radius', 10), 'light_source', 'wiki_api'))

        # 导入doors
        for door in self.doors:
            cursor.execute("""
                INSERT OR IGNORE INTO doors (id, name, display_name, category, width, height, npc_function, paint_compatible, source)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (door['id'], door['name'], door['display_name'], 'door',
                  door.get('width', 1), door.get('height', 3), 'door', 1, 'wiki_api'))

        conn.commit()
        conn.close()

        print(f"  数据库: {db_path}")
        print(f"  导入: {len(self.tiles)} tiles, {len(self.walls)} walls, {len(self.furniture)} furniture")

    def create_tables(self, cursor):
        """创建数据库表"""
        # Tiles表
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS tiles (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                display_name TEXT,
                category TEXT,
                styles TEXT,
                biome_match TEXT,
                paint_compatible INTEGER DEFAULT 0,
                slope_compatible INTEGER DEFAULT 0,
                hardness INTEGER DEFAULT 50,
                light_emission INTEGER DEFAULT 0,
                description TEXT,
                source TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        """)

        # Walls表
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS walls (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                display_name TEXT,
                category TEXT,
                styles TEXT,
                paint_compatible INTEGER DEFAULT 0,
                is_natural INTEGER DEFAULT 0,
                description TEXT,
                source TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        """)

        # Furniture表
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS furniture (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                display_name TEXT,
                category TEXT,
                width INTEGER DEFAULT 1,
                height INTEGER DEFAULT 1,
                npc_function TEXT,
                paint_compatible INTEGER DEFAULT 0,
                description TEXT,
                source TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        """)

        # Light_sources表
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS light_sources (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                display_name TEXT,
                category TEXT,
                light_radius INTEGER DEFAULT 10,
                npc_function TEXT,
                description TEXT,
                source TEXT
            )
        """)

        # Doors表
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS doors (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                display_name TEXT,
                category TEXT,
                width INTEGER DEFAULT 1,
                height INTEGER DEFAULT 3,
                npc_function TEXT,
                paint_compatible INTEGER DEFAULT 0,
                description TEXT,
                source TEXT
            )
        """)

        # Paints表（基础数据）
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

        # Slopes表（基础数据）
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS slopes (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                display_name TEXT,
                description TEXT
            )
        """)

        # 插入基础paints数据
        paints_data = [
            (0, 'None', '无油漆', None, 'none'),
            (1, 'Red', '红色', '#FF0000', 'color'),
            (2, 'Orange', '橙色', '#FF8000', 'color'),
            (3, 'Yellow', '黄色', '#FFFF00', 'color'),
            (4, 'Lime', '青柠', '#80FF00', 'color'),
            (5, 'Green', '绿色', '#00FF00', 'color'),
            (6, 'Teal', '青色', '#00FF80', 'color'),
            (7, 'Cyan', '蓝绿', '#00FFFF', 'color'),
            (8, 'SkyBlue', '天蓝', '#0080FF', 'color'),
            (9, 'Blue', '蓝色', '#0000FF', 'color'),
            (10, 'Purple', '紫色', '#8000FF', 'color'),
            (11, 'Violet', '紫罗兰', '#FF00FF', 'color'),
            (12, 'Pink', '粉色', '#FF0080', 'color'),
            (28, 'Shadow', '阴影', None, 'depth'),
            (29, 'Negative', '反转', None, 'special'),
            (30, 'White', '白色', '#FFFFFF', 'color'),
            (31, 'Black', '黑色', '#000000', 'color'),
        ]
        cursor.executemany("INSERT OR IGNORE INTO paints VALUES (?, ?, ?, ?, ?, NULL)", paints_data)

        # 插入基础slopes数据
        slopes_data = [
            (0, 'Solid', '完整方块', '标准完整方块'),
            (1, 'HalfBlock', '半砖', '只有下半部分'),
            (2, 'SlopeDownRight', '右上斜坡', '从左上到右下'),
            (3, 'SlopeDownLeft', '左上斜坡', '从右上到左下'),
            (4, 'SlopeUpRight', '右下斜坡', '从左下到右上'),
            (5, 'SlopeUpLeft', '左下斜坡', '从右下到左上'),
        ]
        cursor.executemany("INSERT OR IGNORE INTO slopes VALUES (?, ?, ?, ?)", slopes_data)

    def escape_sql(self, value):
        """转义SQL字符串"""
        if not value:
            return ""
        return value.replace("'", "''").replace("\\", "\\\\").replace("\n", " ")

    def run(self, mode="all"):
        """执行爬取"""
        print("=" * 50)
        print("Terraria Wiki API 爬虫")
        print("=" * 50)

        if mode in ["all", "tiles"]:
            self.crawl_tiles_from_page()

        if mode in ["all", "walls"]:
            self.crawl_walls_from_page()

        if mode in ["all", "furniture"]:
            self.crawl_furniture_data()
            self.crawl_light_sources()
            self.crawl_doors()

        # 生成输出文件
        self.generate_sql()
        self.generate_json()

        # 导入数据库
        self.import_to_database()

        print("\n" + "=" * 50)
        print("爬取完成!")
        print(f"方块: {len(self.tiles)}")
        print(f"墙壁: {len(self.walls)}")
        print(f"家具: {len(self.furniture)}")
        print(f"光源: {len(self.light_sources)}")
        print(f"门: {len(self.doors)}")
        print(f"数据库: {DB_PATH}")
        print("=" * 50)


def main():
    parser = argparse.ArgumentParser(description="Terraria Wiki API 爬虫")
    parser.add_argument("--tiles", action="store_true", help="只爬取方块")
    parser.add_argument("--walls", action="store_true", help="只爬取墙壁")
    parser.add_argument("--furniture", action="store_true", help="爬取家具信息")

    args = parser.parse_args()

    crawler = TerrariaWikiCrawler()

    if args.tiles:
        mode = "tiles"
    elif args.walls:
        mode = "walls"
    elif args.furniture:
        mode = "furniture"
    else:
        mode = "all"

    crawler.run(mode)


if __name__ == "__main__":
    main()