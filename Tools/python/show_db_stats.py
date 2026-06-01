#!/usr/bin/env python3
"""
数据库统计查看脚本
"""

import os
import sqlite3

DB_PATH = os.path.join(os.path.dirname(__file__), "..", "..", "Data", "terraria_kb.db")


def show_stats():
    if not os.path.exists(DB_PATH):
        print(f"数据库不存在: {DB_PATH}")
        return

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    print("=" * 50)
    print("数据库统计")
    print("=" * 50)
    print(f"数据库路径: {DB_PATH}")
    print()

    tables = ["tiles", "walls", "paints", "slopes", "furniture", "light_sources", "doors",
              "style_templates", "npc_requirements", "house_validation", "biomes"]

    total = 0
    for table in tables:
        try:
            cursor.execute(f"SELECT COUNT(*) FROM {table}")
            count = cursor.fetchone()[0]
            total += count
            print(f"  {table}: {count} 条记录")
        except sqlite3.Error:
            print(f"  {table}: 表不存在")

    print()
    print(f"  总计: {total} 条记录")
    print("=" * 50)

    # 显示部分数据示例
    print("\n=== 方块数据示例 ===")
    cursor.execute("SELECT id, name, display_name, category FROM tiles LIMIT 10")
    for row in cursor.fetchall():
        print(f"  ID={row[0]}, Name={row[1]}, DisplayName={row[2]}, Category={row[3]}")

    conn.close()


if __name__ == "__main__":
    show_stats()