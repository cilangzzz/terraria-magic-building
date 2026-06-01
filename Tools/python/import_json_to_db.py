#!/usr/bin/env python3
"""
JSON数据导入SQLite数据库脚本
将 Data 目录下的 JSON 文件导入到 SQLite 数据库

使用方法:
    python import_json_to_db.py
"""

import json
import os
import sqlite3
from datetime import datetime

# 路径配置
DATA_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Data")
DB_PATH = os.path.join(DATA_DIR, "terraria_kb.db")

# JSON文件路径
JSON_FILES = {
    "TileKnowledgeBase.json": "tile_knowledge",
    "TerrariaWikiData.json": "wiki_data",
    "StyleTemplates.json": "style_templates",
    "FurnitureRules.json": "furniture_rules"
}


class JSONImporter:
    def __init__(self):
        self.conn = None
        self.cursor = None

    def connect_db(self):
        """连接数据库"""
        os.makedirs(os.path.dirname(DB_PATH), exist_ok=True)
        self.conn = sqlite3.connect(DB_PATH)
        self.cursor = self.conn.cursor()
        print(f"连接数据库: {DB_PATH}")

    def create_tables(self):
        """创建所有表"""
        print("\n=== 创建数据库表 ===")

        # Tiles表
        self.cursor.execute("""
            CREATE TABLE IF NOT EXISTS tiles (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                display_name TEXT,
                category TEXT,
                sub_category TEXT,
                styles TEXT,
                biome_match TEXT,
                paint_compatible INTEGER DEFAULT 0,
                slope_compatible INTEGER DEFAULT 0,
                hardness INTEGER DEFAULT 50,
                light_emission INTEGER DEFAULT 0,
                light_color TEXT,
                is_solid INTEGER DEFAULT 1,
                is_multi_tile INTEGER DEFAULT 0,
                width INTEGER DEFAULT 1,
                height INTEGER DEFAULT 1,
                npc_function TEXT,
                placement_rule TEXT,
                craft_station TEXT,
                wire_compatible INTEGER DEFAULT 0,
                description TEXT,
                source TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        """)
        self.cursor.execute("CREATE INDEX IF NOT EXISTS idx_tiles_category ON tiles(category)")
        self.cursor.execute("CREATE INDEX IF NOT EXISTS idx_tiles_name ON tiles(name)")
        print("  创建 tiles 表")

        # Walls表
        self.cursor.execute("""
            CREATE TABLE IF NOT EXISTS walls (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                display_name TEXT,
                category TEXT,
                styles TEXT,
                biome_match TEXT,
                paint_compatible INTEGER DEFAULT 0,
                is_natural INTEGER DEFAULT 0,
                description TEXT,
                source TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        """)
        self.cursor.execute("CREATE INDEX IF NOT EXISTS idx_walls_category ON walls(category)")
        print("  创建 walls 表")

        # Paints表
        self.cursor.execute("""
            CREATE TABLE IF NOT EXISTS paints (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                display_name TEXT,
                color_hex TEXT,
                effect_type TEXT,
                description TEXT,
                source TEXT
            )
        """)
        print("  创建 paints 表")

        # Slopes表
        self.cursor.execute("""
            CREATE TABLE IF NOT EXISTS slopes (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                display_name TEXT,
                direction TEXT,
                description TEXT
            )
        """)
        print("  创建 slopes 表")

        # Furniture表
        self.cursor.execute("""
            CREATE TABLE IF NOT EXISTS furniture (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                display_name TEXT,
                category TEXT,
                styles TEXT,
                width INTEGER DEFAULT 1,
                height INTEGER DEFAULT 1,
                npc_function TEXT,
                paint_compatible INTEGER DEFAULT 0,
                placement_rule TEXT,
                storage_slots INTEGER DEFAULT 0,
                light_radius INTEGER DEFAULT 0,
                wire_compatible INTEGER DEFAULT 0,
                craft_station TEXT,
                description TEXT,
                source TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        """)
        self.cursor.execute("CREATE INDEX IF NOT EXISTS idx_furniture_category ON furniture(category)")
        print("  创建 furniture 表")

        # Light_sources表
        self.cursor.execute("""
            CREATE TABLE IF NOT EXISTS light_sources (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                display_name TEXT,
                category TEXT,
                width INTEGER DEFAULT 1,
                height INTEGER DEFAULT 1,
                light_radius INTEGER DEFAULT 10,
                light_intensity REAL DEFAULT 1.0,
                light_color TEXT,
                styles TEXT,
                npc_function TEXT,
                placement_type TEXT,
                wire_compatible INTEGER DEFAULT 0,
                description TEXT,
                source TEXT
            )
        """)
        print("  创建 light_sources 表")

        # Doors表
        self.cursor.execute("""
            CREATE TABLE IF NOT EXISTS doors (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                display_name TEXT,
                category TEXT,
                width INTEGER DEFAULT 1,
                height INTEGER DEFAULT 3,
                styles TEXT,
                paint_compatible INTEGER DEFAULT 0,
                npc_function TEXT,
                wire_compatible INTEGER DEFAULT 0,
                description TEXT,
                source TEXT
            )
        """)
        print("  创建 doors 表")

        # Style_templates表
        self.cursor.execute("""
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
                difficulty TEXT,
                wire_required INTEGER DEFAULT 0,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        """)
        print("  创建 style_templates 表")

        # NPC_requirements表
        self.cursor.execute("""
            CREATE TABLE IF NOT EXISTS npc_requirements (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                npc_name TEXT NOT NULL UNIQUE,
                display_name TEXT,
                spawn_condition TEXT,
                preferred_biome TEXT,
                disliked_biome TEXT,
                preferred_neighbors TEXT,
                disliked_neighbors TEXT,
                special_furniture TEXT,
                biome_requirement TEXT,
                description TEXT,
                source TEXT
            )
        """)
        print("  创建 npc_requirements 表")

        # House_validation表
        self.cursor.execute("""
            CREATE TABLE IF NOT EXISTS house_validation (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                rule_name TEXT NOT NULL UNIQUE,
                rule_type TEXT,
                requirement TEXT,
                minimum_value INTEGER,
                maximum_value INTEGER,
                required_elements TEXT,
                description TEXT
            )
        """)
        print("  创建 house_validation 表")

        # Biomes表
        self.cursor.execute("""
            CREATE TABLE IF NOT EXISTS biomes (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                display_name TEXT,
                category TEXT,
                depth_range TEXT,
                characteristic_tiles TEXT,
                characteristic_walls TEXT,
                description TEXT,
                source TEXT
            )
        """)
        print("  创建 biomes 表")

        self.conn.commit()

    def import_tile_knowledge_base(self):
        """导入TileKnowledgeBase.json"""
        json_path = os.path.join(DATA_DIR, "TileKnowledgeBase.json")
        if not os.path.exists(json_path):
            print(f"  文件不存在: {json_path}")
            return

        print(f"\n=== 导入 {json_path} ===")

        with open(json_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        # 导入tiles
        tiles = data.get("tiles", [])
        for tile in tiles:
            styles = json.dumps(tile.get("styles", []))
            biome_match = json.dumps(tile.get("biome_match", []))
            npc_func = json.dumps(tile.get("npc_function", [])) if tile.get("npc_function") else None

            self.cursor.execute("""
                INSERT OR REPLACE INTO tiles (
                    id, name, display_name, category, styles, biome_match,
                    paint_compatible, slope_compatible, hardness, light_emission,
                    width, height, npc_function, placement_rule, description, source
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'json_import')
            """, (
                tile.get("id"),
                tile.get("name"),
                tile.get("display_name"),
                tile.get("category"),
                styles,
                biome_match,
                1 if tile.get("paint_compatible") else 0,
                1 if tile.get("slope_compatible") else 0,
                tile.get("hardness", 50),
                tile.get("light_emission", 0),
                tile.get("width", 1),
                tile.get("height", 1),
                npc_func,
                tile.get("placement_rule"),
                tile.get("description")
            ))
        print(f"  导入 {len(tiles)} 个方块")

        # 导入paints
        paints = data.get("paints", [])
        for paint in paints:
            self.cursor.execute("""
                INSERT OR REPLACE INTO paints (id, name, display_name, color_hex, effect_type, description)
                VALUES (?, ?, ?, ?, ?, ?)
            """, (
                paint.get("id"),
                paint.get("name"),
                paint.get("display_name"),
                paint.get("color"),
                paint.get("effect"),
                paint.get("description")
            ))
        print(f"  导入 {len(paints)} 种油漆")

        # 导入slopes
        slopes = data.get("slopes", [])
        for slope in slopes:
            self.cursor.execute("""
                INSERT OR REPLACE INTO slopes (id, name, display_name, description)
                VALUES (?, ?, ?, ?)
            """, (
                slope.get("id"),
                slope.get("name"),
                slope.get("display_name"),
                slope.get("description")
            ))
        print(f"  导入 {len(slopes)} 种斜坡")

        # 导入walls
        walls = data.get("walls", [])
        for wall in walls:
            styles = json.dumps(wall.get("styles", []))
            self.cursor.execute("""
                INSERT OR REPLACE INTO walls (id, name, display_name, styles, paint_compatible, source)
                VALUES (?, ?, ?, ?, ?, 'json_import')
            """, (
                wall.get("id"),
                wall.get("name"),
                wall.get("display_name"),
                styles,
                1 if wall.get("paint_compatible") else 0
            ))
        print(f"  导入 {len(walls)} 个墙壁")

        self.conn.commit()

    def import_wiki_data(self):
        """导入TerrariaWikiData.json"""
        json_path = os.path.join(DATA_DIR, "TerrariaWikiData.json")
        if not os.path.exists(json_path):
            print(f"  文件不存在: {json_path}")
            return

        print(f"\n=== 导入 {json_path} ===")

        with open(json_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        # 导入furniture
        furniture = data.get("furniture", [])
        for furn in furniture:
            npc_func = json.dumps(furn.get("npc_function")) if furn.get("npc_function") else None
            variants = json.dumps(furn.get("variants", []))

            self.cursor.execute("""
                INSERT OR REPLACE INTO furniture (
                    id, name, display_name, category, width, height,
                    npc_function, paint_compatible, storage_slots, description, source
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'wiki_json')
            """, (
                furn.get("id"),
                furn.get("name"),
                furn.get("display_name"),
                furn.get("category"),
                furn.get("width", 1),
                furn.get("height", 1),
                npc_func,
                1 if furn.get("paint_compatible") else 0,
                furn.get("storage_slots", 0),
                f"Variants: {variants[:100]}..."
            ))
        print(f"  导入 {len(furniture)} 个家具")

        # 导入light_sources
        lights = data.get("light_sources", [])
        for light in lights:
            variants = json.dumps(light.get("variants", []))
            npc_func = json.dumps(light.get("npc_function")) if light.get("npc_function") else None

            self.cursor.execute("""
                INSERT OR REPLACE INTO light_sources (
                    id, name, display_name, category, light_radius, npc_function, description, source
                ) VALUES (?, ?, ?, ?, ?, ?, ?, 'wiki_json')
            """, (
                light.get("id"),
                light.get("name"),
                light.get("display_name"),
                light.get("category"),
                light.get("light_radius", 10),
                npc_func,
                f"Variants: {variants[:100]}..."
            ))
        print(f"  导入 {len(lights)} 个光源")

        # 导入doors
        doors = data.get("doors", [])
        for door in doors:
            variants = json.dumps(door.get("variants", []))
            npc_func = json.dumps(door.get("npc_function")) if door.get("npc_function") else None

            self.cursor.execute("""
                INSERT OR REPLACE INTO doors (
                    id, name, display_name, category, width, height, npc_function, paint_compatible, description, source
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, 'wiki_json')
            """, (
                door.get("id"),
                door.get("name"),
                door.get("display_name"),
                door.get("category", "door"),
                door.get("width", 1),
                door.get("height", 3),
                npc_func,
                1 if door.get("paint_compatible") else 0,
                f"Variants: {variants[:100]}..."
            ))
        print(f"  导入 {len(doors)} 个门类型")

        self.conn.commit()

    def import_style_templates(self):
        """导入StyleTemplates.json"""
        json_path = os.path.join(DATA_DIR, "StyleTemplates.json")
        if not os.path.exists(json_path):
            print(f"  文件不存在: {json_path}")
            return

        print(f"\n=== 导入 {json_path} ===")

        with open(json_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        templates = data if isinstance(data, list) else data.get("templates", data.get("styles", []))

        count = 0
        for template in templates:
            if isinstance(template, dict):
                primary_tiles = json.dumps(template.get("primary_tiles", []))
                primary_walls = json.dumps(template.get("primary_walls", []))

                self.cursor.execute("""
                    INSERT OR REPLACE INTO style_templates (
                        name, display_name, description, primary_tiles, primary_walls, paint_scheme, architectural_rules
                    ) VALUES (?, ?, ?, ?, ?, ?, ?)
                """, (
                    template.get("name"),
                    template.get("display_name"),
                    template.get("description"),
                    primary_tiles,
                    primary_walls,
                    json.dumps(template.get("paint_scheme", {})),
                    json.dumps(template.get("architectural_rules", []))
                ))
                count += 1

        print(f"  导入 {count} 个风格模板")
        self.conn.commit()

    def import_furniture_rules(self):
        """导入FurnitureRules.json"""
        json_path = os.path.join(DATA_DIR, "FurnitureRules.json")
        if not os.path.exists(json_path):
            print(f"  文件不存在: {json_path}")
            return

        print(f"\n=== 导入 {json_path} ===")

        with open(json_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        # 导入NPC requirements
        npcs = data.get("npc_requirements", data.get("npcs", []))
        count = 0
        for npc in npcs:
            if isinstance(npc, dict):
                preferred_neighbors = json.dumps(npc.get("preferred_neighbors", []))
                disliked_neighbors = json.dumps(npc.get("disliked_neighbors", []))

                self.cursor.execute("""
                    INSERT OR REPLACE INTO npc_requirements (
                        npc_name, display_name, preferred_biome, preferred_neighbors, disliked_neighbors
                    ) VALUES (?, ?, ?, ?, ?)
                """, (
                    npc.get("npc_name", npc.get("name")),
                    npc.get("display_name"),
                    npc.get("preferred_biome"),
                    preferred_neighbors,
                    disliked_neighbors
                ))
                count += 1

        print(f"  导入 {count} 个NPC要求")

        # 导入house validation rules
        rules = data.get("house_validation", data.get("rules", []))
        count = 0
        for rule in rules:
            if isinstance(rule, dict):
                required_elements = json.dumps(rule.get("required_elements", []))

                self.cursor.execute("""
                    INSERT OR REPLACE INTO house_validation (
                        rule_name, rule_type, requirement, minimum_value, required_elements, description
                    ) VALUES (?, ?, ?, ?, ?, ?)
                """, (
                    rule.get("rule_name", rule.get("name")),
                    rule.get("rule_type"),
                    rule.get("requirement"),
                    rule.get("minimum_value"),
                    required_elements,
                    rule.get("description")
                ))
                count += 1

        print(f"  导入 {count} 个房屋验证规则")
        self.conn.commit()

    def import_kb_init_sql(self):
        """执行kb_init.sql中的初始化数据"""
        sql_path = os.path.join(DATA_DIR, "kb_init.sql")
        if not os.path.exists(sql_path):
            print(f"  文件不存在: {sql_path}")
            return

        print(f"\n=== 执行 {sql_path} ===")

        with open(sql_path, "r", encoding="utf-8") as f:
            sql_content = f.read()

        # 分割SQL语句并执行
        statements = sql_content.split(";")
        count = 0
        for stmt in statements:
            stmt = stmt.strip()
            if stmt and not stmt.startswith("--") and not stmt.startswith("CREATE"):
                try:
                    self.cursor.execute(stmt)
                    count += 1
                except sqlite3.Error as e:
                    pass  # 忽略已存在的数据错误

        self.conn.commit()
        print(f"  执行 {count} 条SQL语句")

    def close(self):
        """关闭数据库连接"""
        if self.conn:
            self.conn.close()
            print("\n数据库连接已关闭")

    def run(self):
        """执行导入"""
        print("=" * 50)
        print("JSON数据导入SQLite数据库")
        print("=" * 50)

        self.connect_db()
        self.create_tables()

        # 导入各JSON文件
        self.import_tile_knowledge_base()
        self.import_wiki_data()
        self.import_style_templates()
        self.import_furniture_rules()

        # 执行SQL初始化
        self.import_kb_init_sql()

        # 显示统计
        self.show_stats()

        self.close()

    def show_stats(self):
        """显示数据库统计"""
        print("\n=== 数据库统计 ===")

        tables = ["tiles", "walls", "paints", "slopes", "furniture", "light_sources", "doors",
                  "style_templates", "npc_requirements", "house_validation", "biomes"]

        for table in tables:
            self.cursor.execute(f"SELECT COUNT(*) FROM {table}")
            count = self.cursor.fetchone()[0]
            print(f"  {table}: {count} 条记录")


def main():
    importer = JSONImporter()
    importer.run()


if __name__ == "__main__":
    main()