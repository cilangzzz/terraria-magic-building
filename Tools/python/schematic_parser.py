#!/usr/bin/env python3
"""
TEdit Schematic 解析器
将schematic JSON文件转换为语义化的建筑描述
"""

import json
import sys
import os
from typing import List, Dict, Tuple, Optional, Set
from dataclasses import dataclass, field, asdict

sys.stdout.reconfigure(encoding='utf-8')

# ==================== 建筑体系定义 ====================

# 家具方块ID映射
FURNITURE_IDS = {
    4: {"name": "Torch", "category": "light", "function": ["light_source"], "size": (1, 1)},
    10: {"name": "Door", "category": "door", "function": ["entry"], "size": (1, 3)},
    14: {"name": "Table", "category": "furniture", "function": ["flat_surface"], "size": (3, 1)},
    15: {"name": "Chair", "category": "furniture", "function": ["comfort"], "size": (1, 2)},
    16: {"name": "Anvil", "category": "crafting", "function": ["crafting"], "size": (2, 1)},
    17: {"name": "WorkBench", "category": "furniture", "function": ["flat_surface", "crafting"], "size": (2, 1)},
    19: {"name": "Platform", "category": "platform", "function": ["entry"], "size": (1, 1)},
    21: {"name": "Chest", "category": "storage", "function": ["storage"], "size": (2, 2)},
    33: {"name": "Candle", "category": "light", "function": ["light_source"], "size": (1, 1)},
    34: {"name": "Chandelier", "category": "light", "function": ["light_source"], "size": (3, 3)},
    77: {"name": "Furnace", "category": "crafting", "function": ["crafting"], "size": (3, 2)},
    79: {"name": "Bed", "category": "furniture", "function": ["comfort", "spawn_point"], "size": (4, 2)},
    87: {"name": "Piano", "category": "furniture", "function": ["flat_surface"], "size": (3, 2)},
    88: {"name": "Dresser", "category": "furniture", "function": ["flat_surface", "storage"], "size": (3, 2)},
    89: {"name": "Sofa", "category": "furniture", "function": ["comfort"], "size": (3, 2)},
    90: {"name": "Bathtub", "category": "furniture", "function": ["decoration"], "size": (4, 2)},
    93: {"name": "Lamp", "category": "light", "function": ["light_source"], "size": (1, 3)},
    101: {"name": "Bookcase", "category": "furniture", "function": ["flat_surface"], "size": (3, 4)},
    215: {"name": "Campfire", "category": "light", "function": ["light_source", "regeneration"], "size": (3, 2)},
    387: {"name": "TrapDoor", "category": "door", "function": ["entry"], "size": (2, 1)},
    388: {"name": "TallGate", "category": "door", "function": ["entry"], "size": (1, 5)},
}

# 建筑方块ID映射
BUILDING_TILE_IDS = {
    0: {"name": "Dirt", "style": ["natural", "underground"], "is_solid": True},
    1: {"name": "Stone", "style": ["medieval", "natural", "underground"], "is_solid": True},
    2: {"name": "Grass", "style": ["natural", "forest"], "is_solid": True},
    3: {"name": "Weeds", "style": ["natural"], "is_solid": False, "is_decoration": True},
    5: {"name": "Wood", "style": ["natural", "medieval", "village"], "is_solid": True},
    6: {"name": "GrayBrick", "style": ["medieval", "castle"], "is_solid": True},
    7: {"name": "GoldBrick", "style": ["luxury", "palace", "temple"], "is_solid": True},
    13: {"name": "Glass", "style": ["modern", "fantasy", "ocean"], "is_solid": True},
    38: {"name": "RedBrick", "style": ["urban", "industrial"], "is_solid": True},
    41: {"name": "Obsidian", "style": ["gothic", "hell"], "is_solid": True},
    42: {"name": "Marble", "style": ["greek", "roman", "modern"], "is_solid": True},
    43: {"name": "Granite", "style": ["modern", "tech"], "is_solid": True},
    44: {"name": "SnowBlock", "style": ["snow", "winter"], "is_solid": True},
    45: {"name": "IceBlock", "style": ["snow", "ice"], "is_solid": True},
    46: {"name": "Sandstone", "style": ["desert", "egyptian"], "is_solid": True},
    47: {"name": "RichMahogany", "style": ["jungle", "luxury"], "is_solid": True},
    48: {"name": "BorealWood", "style": ["snow", "rustic"], "is_solid": True},
    49: {"name": "PalmWood", "style": ["ocean", "tropical"], "is_solid": True},
    143: {"name": "StoneSlab", "style": ["medieval", "temple"], "is_solid": True},
    166: {"name": "Pearlstone", "style": ["hallow", "fantasy"], "is_solid": True},
    168: {"name": "Ebonstone", "style": ["corruption", "gothic"], "is_solid": True},
    169: {"name": "Crimstone", "style": ["crimson", "gothic"], "is_solid": True},
    187: {"name": "LivingWood", "style": ["natural", "treehouse"], "is_solid": True},
    216: {"name": "MushroomBlock", "style": ["mushroom"], "is_solid": True},
    633: {"name": "DynastyWood", "style": ["asian", "eastern"], "is_solid": True},
}

# 墙壁ID映射
WALL_IDS = {
    1: {"name": "DirtWall_Natural", "is_natural": True, "style": ["natural"]},
    2: {"name": "DirtWall", "is_natural": False, "style": ["natural", "underground"]},
    4: {"name": "WoodWall", "is_natural": False, "style": ["natural", "medieval"]},
    5: {"name": "GrayBrickWall", "is_natural": False, "style": ["medieval", "castle"]},
    6: {"name": "GoldBrickWall", "is_natural": False, "style": ["luxury", "palace"]},
    10: {"name": "GlassWall", "is_natural": False, "style": ["modern", "aquarium"]},
    13: {"name": "SnowWall", "is_natural": False, "style": ["snow", "winter"]},
    14: {"name": "MarbleWall", "is_natural": False, "style": ["greek", "roman"]},
    15: {"name": "GraniteWall", "is_natural": False, "style": ["modern"]},
    16: {"name": "PearlstoneWall", "is_natural": False, "style": ["hallow"]},
    17: {"name": "EbonstoneWall", "is_natural": False, "style": ["corruption"]},
    18: {"name": "CrimstoneWall", "is_natural": False, "style": ["crimson"]},
    59: {"name": "LivingWoodWall", "is_natural": False, "style": ["natural", "treehouse"]},
}

# ==================== 数据类 ====================

@dataclass
class ParsedTile:
    """解析后的方块"""
    x: int
    y: int
    type_id: Optional[int] = None
    type_name: Optional[str] = None
    wall_id: Optional[int] = None
    wall_name: Optional[str] = None
    category: str = "empty"
    functions: List[str] = field(default_factory=list)
    style: List[str] = field(default_factory=list)
    is_solid: bool = False
    is_furniture: bool = False
    is_light: bool = False
    is_door: bool = False
    paint: Optional[int] = None

@dataclass
class ParsedRoom:
    """解析后的房间"""
    id: int
    name: str
    bounds: Tuple[int, int, int, int]  # (x1, y1, x2, y2)
    width: int
    height: int
    interior_area: int
    wall_coverage: float
    furniture: List[dict] = field(default_factory=list)
    light_sources: List[dict] = field(default_factory=list)
    flat_surfaces: List[dict] = field(default_factory=list)
    comfort_items: List[dict] = field(default_factory=list)
    doors: List[dict] = field(default_factory=list)
    is_valid_house: bool = False
    issues: List[str] = field(default_factory=list)

@dataclass
class ParsedBuilding:
    """解析后的建筑"""
    name: str
    source_file: str
    width: int
    height: int
    total_tiles: int
    tile_stats: Dict[str, int] = field(default_factory=dict)
    wall_stats: Dict[str, int] = field(default_factory=dict)
    furniture_stats: Dict[str, int] = field(default_factory=dict)
    detected_style: str = "unknown"
    rooms: List[ParsedRoom] = field(default_factory=list)
    semantic_description: str = ""
    is_house: bool = False
    has_valid_npc_room: bool = False

# ==================== 解析器 ====================

class SchematicParser:
    """TEdit Schematic 解析器"""

    def __init__(self):
        self.furniture_ids = FURNITURE_IDS
        self.tile_ids = BUILDING_TILE_IDS
        self.wall_ids = WALL_IDS

    def parse_file(self, file_path: str) -> ParsedBuilding:
        """解析schematic文件"""
        with open(file_path, 'r', encoding='utf-8') as f:
            data = json.load(f)

        building = ParsedBuilding(
            name=data.get('name', 'unknown'),
            source_file=file_path,
            width=data.get('width', 0),
            height=data.get('height', 0),
            total_tiles=data.get('total_tiles', 0)
        )

        # 解析所有方块
        tiles_data = data.get('tiles', [])
        parsed_tiles = self._parse_tiles(tiles_data)

        # 统计方块类型
        building.tile_stats = self._count_tiles(parsed_tiles)
        building.wall_stats = self._count_walls(parsed_tiles)
        building.furniture_stats = self._count_furniture(parsed_tiles)

        # 检测风格
        building.detected_style = self._detect_style(parsed_tiles)

        # 尝试检测房间
        building.rooms = self._detect_rooms(parsed_tiles, building.width, building.height)

        # 验证NPC房屋
        for room in building.rooms:
            valid, issues = self._validate_npc_house(room)
            room.is_valid_house = valid
            room.issues = issues
            if valid:
                building.has_valid_npc_room = True

        # 判断是否是房屋
        building.is_house = self._is_building_house(parsed_tiles, building)

        # 生成语义描述
        building.semantic_description = self._generate_description(building)

        return building

    def _parse_tiles(self, tiles_data: List) -> List[List[ParsedTile]]:
        """解析方块数据"""
        parsed = []
        for x, col in enumerate(tiles_data):
            col_parsed = []
            for y, tile in enumerate(col):
                pt = ParsedTile(x=x, y=y)

                # 方块类型
                type_id = tile.get('type')
                if type_id is not None:
                    pt.type_id = type_id
                    if type_id in self.furniture_ids:
                        info = self.furniture_ids[type_id]
                        pt.type_name = info['name']
                        pt.category = info['category']
                        pt.functions = info.get('function', [])
                        pt.is_furniture = True
                        pt.is_light = info['category'] == 'light'
                        pt.is_door = info['category'] == 'door'
                    elif type_id in self.tile_ids:
                        info = self.tile_ids[type_id]
                        pt.type_name = info['name']
                        pt.style = info.get('style', [])
                        pt.is_solid = info.get('is_solid', False)
                        pt.category = 'solid' if pt.is_solid else 'decoration'
                    elif tile.get('type_name'):
                        pt.type_name = tile['type_name']
                        pt.category = 'unknown'
                        pt.is_solid = tile.get('active', False)

                # 墙壁
                wall_id = tile.get('wall')
                if wall_id is not None:
                    pt.wall_id = wall_id
                    if wall_id in self.wall_ids:
                        pt.wall_name = self.wall_ids[wall_id]['name']
                    elif tile.get('wall_name'):
                        pt.wall_name = tile['wall_name']

                # 油漆
                pt.paint = tile.get('tile_color')

                col_parsed.append(pt)
            parsed.append(col_parsed)
        return parsed

    def _count_tiles(self, tiles: List[List[ParsedTile]]) -> Dict[str, int]:
        """统计方块类型"""
        stats = {}
        for col in tiles:
            for tile in col:
                if tile.type_name:
                    stats[tile.type_name] = stats.get(tile.type_name, 0) + 1
        return stats

    def _count_walls(self, tiles: List[List[ParsedTile]]) -> Dict[str, int]:
        """统计墙壁类型"""
        stats = {}
        for col in tiles:
            for tile in col:
                if tile.wall_name:
                    stats[tile.wall_name] = stats.get(tile.wall_name, 0) + 1
        return stats

    def _count_furniture(self, tiles: List[List[ParsedTile]]) -> Dict[str, int]:
        """统计家具"""
        stats = {}
        for col in tiles:
            for tile in col:
                if tile.is_furniture:
                    stats[tile.type_name] = stats.get(tile.type_name, 0) + 1
        return stats

    def _detect_style(self, tiles: List[List[ParsedTile]]) -> str:
        """检测建筑风格"""
        style_counts = {}
        for col in tiles:
            for tile in col:
                for s in tile.style:
                    style_counts[s] = style_counts.get(s, 0) + 1

        if not style_counts:
            return "unknown"

        # 返回最常见的风格
        sorted_styles = sorted(style_counts.items(), key=lambda x: -x[1])
        return sorted_styles[0][0] if sorted_styles else "unknown"

    def _detect_rooms(self, tiles: List[List[ParsedTile]], width: int, height: int) -> List[ParsedRoom]:
        """检测房间（简化版本）"""
        rooms = []

        # 找到有墙壁覆盖的区域
        wall_tiles = []
        for col in tiles:
            for tile in col:
                if tile.wall_id is not None and tile.wall_id > 0:
                    wall_tiles.append((tile.x, tile.y))

        if len(wall_tiles) < 30:  # 墙壁太少，可能不是房间
            # 创建一个整体区域描述
            room = ParsedRoom(
                id=0,
                name="整体区域",
                bounds=(0, 0, width, height),
                width=width,
                height=height,
                interior_area=self._count_empty_tiles(tiles),
                wall_coverage=len(wall_tiles) / (width * height) if width * height > 0 else 0
            )
            self._find_furniture_in_room(room, tiles)
            rooms.append(room)
        else:
            # 尝试找到连续的墙壁区域作为房间
            # 简化处理：假设整个区域是一个房间
            room = ParsedRoom(
                id=0,
                name="主房间",
                bounds=(0, 0, width, height),
                width=width,
                height=height,
                interior_area=self._count_empty_tiles(tiles),
                wall_coverage=len(wall_tiles) / (width * height)
            )
            self._find_furniture_in_room(room, tiles)
            rooms.append(room)

        return rooms

    def _count_empty_tiles(self, tiles: List[List[ParsedTile]]) -> int:
        """计算空地数量"""
        count = 0
        for col in tiles:
            for tile in col:
                if not tile.is_solid and tile.category != 'decoration':
                    count += 1
        return count

    def _find_furniture_in_room(self, room: ParsedRoom, tiles: List[List[ParsedTile]):
        """在房间中查找家具"""
        for col in tiles:
            for tile in col:
                if tile.is_furniture:
                    item = {
                        'name': tile.type_name,
                        'position': (tile.x, tile.y),
                        'category': tile.category
                    }
                    room.furniture.append(item)

                    if tile.is_light:
                        room.light_sources.append(item)
                    if 'flat_surface' in tile.functions:
                        room.flat_surfaces.append(item)
                    if 'comfort' in tile.functions:
                        room.comfort_items.append(item)
                    if tile.is_door or 'entry' in tile.functions:
                        room.doors.append(item)

    def _validate_npc_house(self, room: ParsedRoom) -> Tuple[bool, List[str]]:
        """验证是否为有效的NPC房屋"""
        issues = []

        # 检查面积
        if room.interior_area < 60:
            issues.append(f"面积不足: {room.interior_area}格 (需60格)")

        # 检查光源
        if len(room.light_sources) < 1:
            issues.append("缺少光源")

        # 检查平坦表面
        if len(room.flat_surfaces) < 1:
            issues.append("缺少平坦表面(Table/WorkBench)")

        # 检查舒适物品
        if len(room.comfort_items) < 1:
            issues.append("缺少舒适物品(Chair/Bed)")

        # 检查入口
        if len(room.doors) < 1:
            issues.append("缺少入口(Door/Platform)")

        # 检查墙壁覆盖
        if room.wall_coverage < 0.7:
            issues.append(f"墙壁覆盖不足: {room.wall_coverage*100:.1f}%")

        return len(issues) == 0, issues

    def _is_building_house(self, tiles: List[List[ParsedTile]], building: ParsedBuilding) -> bool:
        """判断是否是房屋"""
        # 有家具、有墙壁、有内部空间的可能是房屋
        has_furniture = len(building.furniture_stats) > 0
        has_walls = len(building.wall_stats) > 0
        has_interior = any(room.interior_area > 30 for room in building.rooms)

        return has_furniture and has_walls and has_interior

    def _generate_description(self, building: ParsedBuilding) -> str:
        """生成语义描述"""
        lines = []

        # 基本信息行
        lines.append(f"[建筑] {building.name}")
        lines.append(f"尺寸: {building.width}x{building.height}格, 风格: {building.detected_style}")

        # 材料行
        if building.tile_stats:
            main_tiles = sorted(building.tile_stats.items(), key=lambda x: -x[1])[:3]
            lines.append(f"主要材料: {', '.join([f'{t}({c})' for t, c in main_tiles])}")

        if building.wall_stats:
            lines.append(f"墙壁: {', '.join([f'{w}({c})' for w, c in building.wall_stats.items()])}")

        # 房屋判断
        if building.is_house:
            lines.append("[判定] 这是一个房屋建筑")
            if building.has_valid_npc_room:
                lines.append("[NPC] 可作为NPC住所")
            else:
                lines.append("[NPC] 不满足NPC居住要求")
        else:
            lines.append("[判定] 这不是完整的房屋，可能是建筑片段或装饰")

        # 房间信息
        for room in building.rooms:
            lines.append(f"[房间{room.id}] {room.width}x{room.height}, 内部{room.interior_area}格")
            if room.furniture:
                lines.append(f"  家具: {', '.join([f['name'] for f in room.furniture])}")
            if room.issues:
                lines.append(f"  问题: {', '.join(room.issues)}")

        return "\n".join(lines)

    def export_json(self, building: ParsedBuilding, output_path: str):
        """导出为JSON"""
        def room_to_dict(room):
            return {
                'id': room.id,
                'name': room.name,
                'bounds': list(room.bounds),
                'size': {'width': room.width, 'height': room.height, 'area': room.interior_area},
                'wall_coverage': room.wall_coverage,
                'furniture': room.furniture,
                'light_sources': room.light_sources,
                'flat_surfaces': room.flat_surfaces,
                'comfort_items': room.comfort_items,
                'doors': room.doors,
                'is_valid_house': room.is_valid_house,
                'issues': room.issues
            }

        data = {
            'name': building.name,
            'source_file': building.source_file,
            'size': {'width': building.width, 'height': building.height, 'total': building.total_tiles},
            'detected_style': building.detected_style,
            'tile_stats': building.tile_stats,
            'wall_stats': building.wall_stats,
            'furniture_stats': building.furniture_stats,
            'is_house': building.is_house,
            'has_valid_npc_room': building.has_valid_npc_room,
            'rooms': [room_to_dict(r) for r in building.rooms],
            'semantic_description': building.semantic_description
        }

        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)


# ==================== 主函数 ====================

def parse_schematic(file_path: str, output_dir: str = None):
    """解析schematic文件"""
    parser = SchematicParser()
    building = parser.parse_file(file_path)

    print("\n" + "="*60)
    print(building.semantic_description)
    print("="*60)

    if output_dir:
        output_path = os.path.join(output_dir, f"{building.name}_parsed.json")
        parser.export_json(building, output_path)
        print(f"\n解析结果已保存: {output_path}")

    return building


if __name__ == "__main__":
    # 测试解析
    test_file = r"C:\Users\admin\Downloads\Game\1.TEditSch.json"
    if os.path.exists(test_file):
        parse_schematic(test_file, r"C:\Users\admin\Documents\My Games\Terraria\tModLoader\ModSources\trab\Data")
    else:
        print(f"文件不存在: {test_file}")