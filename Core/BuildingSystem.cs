"""
Terraria建筑语义体系定义
用于将schematic数据转换为AI可理解的建筑概念
"""

import json
from dataclasses import dataclass, field
from typing import List, Dict, Optional, Tuple, Set
from enum import Enum

# ==================== 基础枚举定义 ====================

class TileCategory(Enum):
    """方块类别"""
    SOLID = "solid"           # 实心方块（可作墙壁/地板）
    PLATFORM = "platform"     # 平台（可穿过）
    DECORATION = "decoration" # 装饰性方块
    LIGHT = "light"           # 光源
    FURNITURE = "furniture"   # 家具
    DOOR = "door"             # 门类
    LIQUID = "liquid"         # 液体
    PLANT = "plant"           # 植物
    EMPTY = "empty"           # 空气/空位

class WallCategory(Enum):
    """墙壁类别"""
    NATURAL = "natural"       # 自然墙壁（不能用于NPC房屋）
    PLACED = "placed"         # 玩家放置的墙壁（可用于房屋）
    SPECIAL = "special"       # 特殊墙壁

class BuildingComponent(Enum):
    """建筑组件"""
    FLOOR = "floor"           # 地板
    WALL = "wall"             # 墙壁（实心方块边界）
    BACKGROUND_WALL = "background_wall"  # 背景墙
    ROOF = "roof"             # 屋顶
    DOOR = "door"             # 入口
    WINDOW = "window"         # 户（玻璃）
    FURNITURE = "furniture"   # 家具
    LIGHT_SOURCE = "light"    # 光源
    DECORATION = "decoration" # 装饰
    FRAME = "frame"           # 框架（房屋边界）

class RoomType(Enum):
    """房间类型"""
    NPC_HOUSE = "npc_house"       # NPC房屋
    CRAFTING_ROOM = "crafting"    # 制作室
    STORAGE_ROOM = "storage"      # 储藏室
    BEDROOM = "bedroom"           # 卧室
    LIVING_ROOM = "living"        # 客厅
    KITCHEN = "kitchen"           # 厨房（装饰性）
    BATHROOM = "bathroom"         # 浴室（装饰性）
    LIBRARY = "library"           # 图书室
    ARMORY = "armory"             # 武器室
    TREASURE_ROOM = "treasure"    # 宝藏室
    OBSERVATORY = "observatory"   # 观测室
    GREENHOUSE = "greenhouse"     # 温室
    THRONE_ROOM = "throne"        # 王座室
    GENERIC = "generic"           # 通用房间

class BuildingType(Enum):
    """建筑类型"""
    HOUSE = "house"               # 房屋（单个NPC住所）
    VILLA = "villa"               # 别墅（多房间）
    CASTLE = "castle"             # 城堡
    TOWER = "tower"               # 塔楼
    TEMPLE = "temple"             # 神殿
    PALACE = "palace"             # 宫殿
    FORTRESS = "fortress"         # 堡垒
    VILLAGE = "village"           # 村庄（多个房屋）
    BASE = "base"                 # 基地（功能复合）
    ARENA = "arena"               # 竞技场
    FARM = "farm"                 # 农场
    SHOP = "shop"                 # 商店
    INN = "inn"                   # 旅店
    DECORATION = "decoration"     # 纯装饰建筑
    LANDMARK = "landmark"         # 地标建筑

class BuildingStyle(Enum):
    """建筑风格"""
    MEDIEVAL = "medieval"         # 中世纪
    MODERN = "modern"             # 现代
    ASIAN = "asian"               # 东方
    FANTASY = "fantasy"           # 奇幻
    STEAMPUNK = "steampunk"       # 蒸汽朋克
    NATURAL = "natural"           # 自然
    SNOW = "snow"                 # 冰雪
    DESERT = "desert"             # 沙漠
    OCEAN = "ocean"               # 海洋
    JUNGLE = "jungle"             # 丛林
    UNDERGROUND = "underground"   # 地下
    GOTHIC = "gothic"             # 哥特
    MUSHROOM = "mushroom"         # 蘑菇
    CORRUPTION = "corruption"     # 腐化
    HALLOW = "hallow"             # 神圣
    RUSSIAN = "russian"           # 俄式
    VILLAGE = "village"           # 乡村

# ==================== 方块ID映射 ====================

# 家具方块ID映射
FURNITURE_TILE_IDS = {
    # 基础家具
    14: {"name": "Table", "category": "furniture", "function": "flat_surface", "size": (3, 1)},
    15: {"name": "Chair", "category": "furniture", "function": "comfort", "size": (1, 2)},
    17: {"name": "WorkBench", "category": "furniture", "function": ["flat_surface", "crafting"], "size": (2, 1)},
    21: {"name": "Chest", "category": "furniture", "function": "storage", "size": (2, 2)},
    79: {"name": "Bed", "category": "furniture", "function": ["comfort", "spawn_point"], "size": (4, 2)},
    87: {"name": "Piano", "category": "furniture", "function": "flat_surface", "size": (3, 2)},
    88: {"name": "Dresser", "category": "furniture", "function": ["flat_surface", "storage"], "size": (3, 2)},
    89: {"name": "Sofa", "category": "furniture", "function": "comfort", "size": (3, 2)},
    90: {"name": "Bathtub", "category": "furniture", "function": "decoration", "size": (4, 2)},
    101: {"name": "Bookcase", "category": "furniture", "function": "flat_surface", "size": (3, 4)},

    # 光源
    4: {"name": "Torch", "category": "light", "function": "light_source", "size": (1, 1)},
    33: {"name": "Candle", "category": "light", "function": "light_source", "size": (1, 1)},
    34: {"name": "Chandelier", "category": "light", "function": "light_source", "size": (3, 3)},
    93: {"name": "Lamp", "category": "light", "function": "light_source", "size": (1, 3)},
    215: {"name": "Campfire", "category": "light", "function": ["light_source", "regeneration"], "size": (3, 2)},

    # 门类
    10: {"name": "Door", "category": "door", "function": "entry", "size": (1, 3)},
    387: {"name": "TrapDoor", "category": "door", "function": "entry", "size": (2, 1)},
    388: {"name": "TallGate", "category": "door", "function": "entry", "size": (1, 5)},

    # 平台
    19: {"name": "Platform", "category": "platform", "function": "entry", "size": (1, 1)},

    # 制作站
    16: {"name": "Anvil", "category": "furniture", "function": "crafting", "size": (2, 1)},
    77: {"name": "Furnace", "category": "furniture", "function": "crafting", "size": (3, 2)},
    94: {"name": "Bottle", "category": "furniture", "function": "crafting", "size": (1, 1)},
    133: {"name": "Loom", "category": "furniture", "function": "crafting", "size": (3, 2)},
    134: {"name": "Keg", "category": "furniture", "function": "crafting", "size": (2, 2)},
    237: {"name": "CookingPot", "category": "furniture", "function": "crafting", "size": (2, 2)},
    302: {"name": "AlchemyTable", "category": "furniture", "function": "crafting", "size": (3, 2)},
    319: {"name": "HeavyWorkBench", "category": "furniture", "function": "crafting", "size": (3, 1)},
    412: {"name": "Blend-o-matic", "category": "furniture", "function": "crafting", "size": (2, 2)},
    460: {"name": "ImbuingStation", "category": "furniture", "function": "crafting", "size": (2, 2)},
    487: {"name": "Autohammer", "category": "furniture", "function": "crafting", "size": (2, 2)},
    519: {"name": "CrystalBall", "category": "furniture", "function": "crafting", "size": (2, 2)},
}

# 建筑方块ID映射（用于识别建筑材料）
BUILDING_TILE_IDS = {
    # 基础方块
    0: {"name": "Dirt", "category": "solid", "style": ["natural", "underground"]},
    1: {"name": "Stone", "category": "solid", "style": ["medieval", "natural", "underground"]},
    2: {"name": "Grass", "category": "solid", "style": ["natural", "forest"]},
    5: {"name": "Wood", "category": "solid", "style": ["natural", "medieval", "village"]},
    6: {"name": "GrayBrick", "category": "solid", "style": ["medieval", "castle"]},
    7: {"name": "GoldBrick", "category": "solid", "style": ["luxury", "palace", "temple"]},
    8: {"name": "SilverBrick", "category": "solid", "style": ["ice", "modern"]},
    9: {"name": "CopperBrick", "category": "solid", "style": ["steampunk"]},
    10: {"name": "IronBrick", "category": "solid", "style": ["steampunk", "industrial"]},
    13: {"name": "Glass", "category": "solid", "style": ["modern", "fantasy", "ocean"]},
    38: {"name": "RedBrick", "category": "solid", "style": ["urban", "industrial"]},
    41: {"name": "Obsidian", "category": "solid", "style": ["gothic", "hell"]},
    42: {"name": "Marble", "category": "solid", "style": ["greek", "roman", "modern"]},
    43: {"name": "Granite", "category": "solid", "style": ["modern", "tech"]},
    44: {"name": "SnowBlock", "category": "solid", "style": ["snow", "winter"]},
    45: {"name": "IceBlock", "category": "solid", "style": ["snow", "ice"]},
    46: {"name": "Sandstone", "category": "solid", "style": ["desert", "egyptian"]},
    47: {"name": "RichMahogany", "category": "solid", "style": ["jungle", "luxury"]},
    48: {"name": "BorealWood", "category": "solid", "style": ["snow", "rustic"]},
    49: {"name": "PalmWood", "category": "solid", "style": ["ocean", "tropical"]},
    143: {"name": "StoneSlab", "category": "solid", "style": ["medieval", "temple"]},
    166: {"name": "Pearlstone", "category": "solid", "style": ["hallow", "fantasy"]},
    168: {"name": "Ebonstone", "category": "solid", "style": ["corruption", "gothic"]},
    169: {"name": "Crimstone", "category": "solid", "style": ["crimson", "gothic"]},
    179: {"name": "SandstoneSlab", "category": "solid", "style": ["desert", "egyptian"]},
    187: {"name": "LivingWood", "category": "solid", "style": ["natural", "treehouse"]},
    216: {"name": "MushroomBlock", "category": "solid", "style": ["mushroom"]},
    633: {"name": "DynastyWood", "category": "solid", "style": ["asian", "eastern"]},
}

# 墙壁ID映射
BUILDING_WALL_IDS = {
    0: {"name": "StoneWall", "category": "placed", "style": ["medieval", "natural"]},
    1: {"name": "DirtWall", "category": "natural", "style": ["natural", "underground"]},  # 自然墙
    2: {"name": "DirtWallPlaced", "category": "placed", "style": ["natural", "underground"]},  # 玩家放置
    4: {"name": "WoodWall", "category": "placed", "style": ["natural", "medieval", "village"]},
    5: {"name": "GrayBrickWall", "category": "placed", "style": ["medieval", "castle"]},
    6: {"name": "GoldBrickWall", "category": "placed", "style": ["luxury", "palace"]},
    7: {"name": "SilverBrickWall", "category": "placed", "style": ["ice"]},
    8: {"name": "CopperBrickWall", "category": "placed", "style": ["steampunk"]},
    9: {"name": "IronBrickWall", "category": "placed", "style": ["steampunk"]},
    10: {"name": "GlassWall", "category": "placed", "style": ["modern", "aquarium"]},
    11: {"name": "StoneSlabWall", "category": "placed", "style": ["medieval", "temple"]},
    12: {"name": "SandstoneWall", "category": "placed", "style": ["desert", "egyptian"]},
    13: {"name": "SnowWall", "category": "placed", "style": ["snow", "winter"]},
    14: {"name": "MarbleWall", "category": "placed", "style": ["greek", "roman"]},
    15: {"name": "GraniteWall", "category": "placed", "style": ["modern"]},
    16: {"name": "PearlstoneWall", "category": "placed", "style": ["hallow"]},
    17: {"name": "EbonstoneWall", "category": "placed", "style": ["corruption"]},
    18: {"name": "CrimstoneWall", "category": "placed", "style": ["crimson"]},
    19: {"name": "RedBrickWall", "category": "placed", "style": ["urban"]},
    20: {"name": "DynastyWall", "category": "placed", "style": ["asian"]},
    21: {"name": "MushroomWall", "category": "placed", "style": ["mushroom"]},
    22: {"name": "BorealWoodWall", "category": "placed", "style": ["snow"]},
    23: {"name": "PalmWoodWall", "category": "placed", "style": ["ocean"]},
    24: {"name": "RichMahoganyWall", "category": "placed", "style": ["jungle"]},
    59: {"name": "LivingWoodWall", "category": "placed", "style": ["natural", "treehouse"]},
}

# ==================== 数据类定义 ====================

@dataclass
class TileInfo:
    """单个方块信息"""
    x: int
    y: int
    type_id: Optional[int] = None
    type_name: Optional[str] = None
    wall_id: Optional[int] = None
    wall_name: Optional[str] = None
    category: TileCategory = TileCategory.EMPTY
    style: List[str] = field(default_factory=list)
    is_solid: bool = False
    has_wall: bool = False
    paint_color: Optional[int] = None
    wire_red: bool = False
    wire_blue: bool = False
    wire_green: bool = False
    liquid_type: Optional[str] = None
    liquid_amount: Optional[int] = None
    function: List[str] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            'position': (self.x, self.y),
            'tile': self.type_name,
            'wall': self.wall_name,
            'category': self.category.value,
            'style': self.style,
            'function': self.function,
            'paint': self.paint_color,
            'wires': {'red': self.wire_red, 'blue': self.wire_blue, 'green': self.wire_green},
            'liquid': {'type': self.liquid_type, 'amount': self.liquid_amount}
        }

@dataclass
class RoomInfo:
    """房间信息"""
    id: int
    name: str
    x1: int
    y1: int
    x2: int
    y2: int
    width: int
    height: int
    area: int  # 内部空地面积
    room_type: RoomType = RoomType.GENERIC
    components: Dict[str, List[Tuple[int, int]]] = field(default_factory=dict)
    furniture: List[dict] = field(default_factory=list)
    light_sources: List[dict] = field(default_factory=list)
    doors: List[dict] = field(default_factory=list)
    flat_surfaces: List[dict] = field(default_factory=list)
    comfort_items: List[dict] = field(default_factory=list)
    is_valid_npc_house: bool = False
    validation_issues: List[str] = field(default_factory=list)
    wall_coverage: float = 0.0  # 背景墙覆盖率

    def to_dict(self) -> dict:
        return {
            'id': self.id,
            'name': self.name,
            'bounds': {'x1': self.x1, 'y1': self.y1, 'x2': self.x2, 'y2': self.y2},
            'size': {'width': self.width, 'height': self.height, 'area': self.area},
            'type': self.room_type.value,
            'furniture': self.furniture,
            'light_sources': self.light_sources,
            'doors': self.doors,
            'flat_surfaces': self.flat_surfaces,
            'comfort_items': self.comfort_items,
            'is_valid_npc_house': self.is_valid_npc_house,
            'validation_issues': self.validation_issues,
            'wall_coverage': self.wall_coverage
        }

@dataclass
class BuildingInfo:
    """建筑信息"""
    id: int
    name: str
    schematic_file: str
    width: int
    height: int
    total_tiles: int
    building_type: BuildingType = BuildingType.DECORATION
    building_style: BuildingStyle = BuildingStyle.NATURAL
    rooms: List[RoomInfo] = field(default_factory=list)
    components: Dict[str, List[Tuple[int, int]]] = field(default_factory=dict)
    tile_stats: Dict[str, int] = field(default_factory=dict)
    wall_stats: Dict[str, int] = field(default_factory=dict)
    furniture_stats: Dict[str, int] = field(default_factory=dict)
    description: str = ""
    semantic_description: str = ""  # AI可理解的语义描述

    def to_dict(self) -> dict:
        return {
            'id': self.id,
            'name': self.name,
            'schematic_file': self.schematic_file,
            'size': {'width': self.width, 'height': self.height, 'total_tiles': self.total_tiles},
            'type': self.building_type.value,
            'style': self.building_style.value,
            'rooms': [r.to_dict() for r in self.rooms],
            'components': self.components,
            'tile_stats': self.tile_stats,
            'wall_stats': self.wall_stats,
            'furniture_stats': self.furniture_stats,
            'description': self.description,
            'semantic_description': self.semantic_description
        }

# ==================== 房屋验证规则 ====================

NPC_HOUSE_REQUIREMENTS = {
    'min_area': 60,  # 最少60格空地
    'min_width': 7,
    'min_height': 7,
    'max_width': 60,
    'max_height': 60,
    'required_furniture': {
        'light_source': {'min': 1, 'types': ['Torch', 'Candle', 'Chandelier', 'Lamp', 'Campfire']},
        'flat_surface': {'min': 1, 'types': ['Table', 'WorkBench', 'Dresser', 'Piano', 'Bookcase']},
        'comfort': {'min': 1, 'types': ['Chair', 'Bed', 'Sofa']},
        'entry': {'min': 1, 'types': ['Door', 'TrapDoor', 'Platform']}
    },
    'wall_required': True,  # 必须有背景墙
    'natural_wall_invalid': True,  # 自然墙无效
    'frame_required': True,  # 必须有实心方块框架
}

def validate_npc_house(room: RoomInfo) -> Tuple[bool, List[str]]:
    """验证房间是否符合NPC房屋要求"""
    issues = []

    # 检查面积
    if room.area < NPC_HOUSE_REQUIREMENTS['min_area']:
        issues.append(f"面积不足: {room.area}格 (需要至少{NPC_HOUSE_REQUIREMENTS['min_area']}格)")

    # 检查尺寸
    if room.width < NPC_HOUSE_REQUIREMENTS['min_width']:
        issues.append(f"宽度不足: {room.width}格 (需要至少{NPC_HOUSE_REQUIREMENTS['min_width']}格)")
    if room.height < NPC_HOUSE_REQUIREMENTS['min_height']:
        issues.append(f"高度不足: {room.height}格 (需要至少{NPC_HOUSE_REQUIREMENTS['min_height']}格)")

    # 检查光源
    if len(room.light_sources) < 1:
        issues.append("缺少光源")

    # 检查平坦表面
    if len(room.flat_surfaces) < 1:
        issues.append("缺少平坦表面(Table/WorkBench/Dresser)")

    # 检查舒适物品
    if len(room.comfort_items) < 1:
        issues.append("缺少舒适物品(Chair/Bed/Sofa)")

    # 检查入口
    if len(room.doors) < 1:
        issues.append("缺少入口(Door/Platform)")

    # 检查背景墙
    if room.wall_coverage < 0.9:  # 90%覆盖率
        issues.append(f"背景墙覆盖不足: {room.wall_coverage*100:.1f}% (需要90%以上)")

    is_valid = len(issues) == 0
    return is_valid, issues

# ==================== 建筑语义描述生成 ====================

def generate_semantic_description(building: BuildingInfo) -> str:
    """生成AI可理解的语义描述"""
    desc_parts = []

    # 基本描述
    desc_parts.append(f"这是一个{building.building_style.value}风格的{building.building_type.value}。")
    desc_parts.append(f"尺寸: {building.width}x{building.height}格，总面积{building.total_tiles}格。")

    # 材料描述
    if building.tile_stats:
        main_tiles = sorted(building.tile_stats.items(), key=lambda x: -x[1])[:3]
        tile_desc = "主要建筑材料: " + ", ".join([f"{t}({c}格)" for t, c in main_tiles])
        desc_parts.append(tile_desc)

    if building.wall_stats:
        main_walls = sorted(building.wall_stats.items(), key=lambda x: -x[1])[:2]
        wall_desc = "墙壁材料: " + ", ".join([f"{w}({c}格)" for w, c in main_walls])
        desc_parts.append(wall_desc)

    # 房间描述
    if building.rooms:
        desc_parts.append(f"包含{len(building.rooms)}个房间:")
        for room in building.rooms:
            room_desc = f"  - {room.name}: {room.room_type.value}, {room.width}x{room.height}格, {room.area}格空地"
            if room.is_valid_npc_house:
                room_desc += " (可居住NPC)"
            else:
                room_desc += " (不可居住NPC: " + "; ".join(room.validation_issues[:2]) + ")"
            desc_parts.append(room_desc)

    # 家具描述
    if building.furniture_stats:
        furniture_desc = "家具: " + ", ".join([f"{f}({c}个)" for f, c in building.furniture_stats.items()])
        desc_parts.append(furniture_desc)

    # 组件描述
    if building.components:
        component_descs = []
        for comp, positions in building.components.items():
            if len(positions) > 0:
                component_descs.append(f"{comp}: {len(positions)}处")
        if component_descs:
            desc_parts.append("建筑组件: " + ", ".join(component_descs))

    return "\n".join(desc_parts)

# ==================== 导出所有定义 ====================

BUILDING_SYSTEM_EXPORT = {
    'enums': {
        'TileCategory': [e.value for e in TileCategory],
        'WallCategory': [e.value for e in WallCategory],
        'BuildingComponent': [e.value for e in BuildingComponent],
        'RoomType': [e.value for e in RoomType],
        'BuildingType': [e.value for e in BuildingType],
        'BuildingStyle': [e.value for e in BuildingStyle],
    },
    'tile_mappings': {
        'furniture': FURNITURE_TILE_IDS,
        'building': BUILDING_TILE_IDS,
    },
    'wall_mappings': BUILDING_WALL_IDS,
    'npc_house_requirements': NPC_HOUSE_REQUIREMENTS,
}

if __name__ == "__main__":
    # 导出为JSON
    export_path = "building_system_definition.json"
    with open(export_path, 'w', encoding='utf-8') as f:
        json.dump(BUILDING_SYSTEM_EXPORT, f, ensure_ascii=False, indent=2)
    print(f"建筑体系定义已导出: {export_path}")