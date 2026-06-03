#!/usr/bin/env python3
"""
Terraria建筑实体解析与描述生成工具
解析schematic文件，生成精简的建筑语义描述
"""

import json
import os
import sys
import glob
from typing import List, Dict, Tuple, Optional
from dataclasses import dataclass, field, asdict
from datetime import datetime

sys.stdout.reconfigure(encoding='utf-8')

# ==================== 方块ID映射表 ====================

# 完整的方块ID到名称映射
TILE_ID_MAP = {
    # 基础方块
    0: "Dirt", 1: "Stone", 2: "Grass", 3: "Weeds", 4: "Torch",
    5: "Wood", 6: "GrayBrick", 7: "GoldBrick", 8: "SilverBrick", 9: "CopperBrick",
    10: "IronBrick", 11: "SilverCoin", 12: "GoldCoin", 13: "Glass",
    14: "Table", 15: "Chair", 16: "Anvil", 17: "WorkBench", 18: "Platform",
    19: "Platform", 20: "Sunflower", 21: "Chest", 22: "DemoniteOre",
    30: "Sign", 31: "Books", 32: "Cobweb", 33: "Candle", 34: "Chandelier",
    35: "Meteorite", 37: "Ebonstone", 38: "RedBrick", 41: "Obsidian",
    42: "Marble", 43: "Granite", 44: "SnowBlock", 45: "IceBlock",
    46: "Sandstone", 47: "RichMahogany", 48: "BorealWood", 49: "PalmWood",
    50: "Pearlwood", 51: "AdamantiteBeam", 52: "DemoniteBrick", 53: "CrimtaneBrick",
    54: "SandstoneBrick", 55: "EbonstoneBrick", 56: "PearlstoneBrick",
    57: "RainCloud", 58: "Sand", 59: "Crimsand", 60: "Ebonsand",
    61: "Pearlsand", 62: "TinBrick", 63: "TungstenBrick", 64: "PlatinumBrick",
    65: "Cactus", 66: "Coral", 67: "HermesBoots", 68: "EnchantedSword",
    77: "Furnace", 79: "Bed", 80: "CopperOre", 81: "IronOre",
    82: "SilverOre", 83: "GoldOre", 84: "PiggyBank", 85: "TinOre",
    86: "LeadOre", 87: "Piano", 88: "Dresser", 89: "Sofa", 90: "Bathtub",
    91: "TrapdoorClosed", 92: "TrapdoorOpen", 93: "Lamp", 94: "Bottle",
    95: "Bowl", 96: "Keg", 97: "Fireplace", 98: "ChristmasTree",
    99: "Presents", 100: "MinecartTrack", 101: "Bookcase", 102: "Throne",
    104: "TikiTorch", 105: "Statue", 106: "Statue", 107: "Statue",
    108: "Statue", 109: "Statue", 110: "Statue", 111: "Statue",
    112: "Statue", 113: "Statue", 114: "Statue", 115: "Statue",
    116: "Statue", 117: "Statue", 118: "Statue", 119: "Statue",
    120: "Statue", 121: "Statue", 122: "Statue", 123: "Statue",
    124: "BlueFlare", 125: "Flare", 126: "PlanterBox", 127: "BlueStarryGlass",
    128: "OrangeStarryGlass", 129: "Waterfall", 130: "Confetti",
    133: "Loom", 134: "Keg", 135: "Amethyst", 136: "Topaz",
    137: "Sapphire", 138: "Emerald", 139: "Ruby", 140: "Diamond",
    141: "Amber", 142: "AmethystGemspark", 143: "StoneSlab",
    144: "SandstoneSlab", 145: "EbonstoneSlab", 146: "PearlstoneSlab",
    147: "AmethystGemsparkOff", 148: "TopazGemsparkOff", 149: "SapphireGemsparkOff",
    150: "EmeraldGemsparkOff", 151: "RubyGemsparkOff", 152: "DiamondGemsparkOff",
    153: "AmberGemsparkOff", 154: "TopazGemspark", 155: "SapphireGemspark",
    156: "EmeraldGemspark", 157: "RubyGemspark", 158: "DiamondGemspark",
    159: "AmberGemspark", 160: "CrackedBlueDungeonBrick", 161: "CrackedGreenDungeonBrick",
    162: "CrackedPinkDungeonBrick", 163: "PipeBlock", 164: "SmokeBlock",
    165: "BorealBeam", 166: "Pearlstone", 167: "ReefBlock", 168: "Ebonstone",
    169: "Crimstone", 170: "Voidstone", 171: "TreacherousHardenedSand",
    172: "DesertHardenedSand", 173: "CorruptHardenedSand", 174: "CrimsonHardenedSand",
    175: "HallowHardenedSand", 176: "Lavastone", 177: "CorruptJungleGrass",
    178: "CrimsonJungleGrass", 179: "SandstoneSlab", 180: "CorruptionThorns",
    181: "CrimsonThorns", 182: "IlluminantPetals", 183: "LivingLeaf",
    184: "LivingMahoganyLeaf", 185: "LivingWood", 186: "LivingMahogany",
    187: "LivingWood", 188: "VineRope", 189: "BambooBlock",
    190: "BambooWall", 191: "LargeBambooBlock", 192: "LargeBambooWall",
    193: "Pots", 194: "Pots", 195: "Pots", 196: "Pots",
    197: "Pots", 198: "Pots", 199: "Pots", 200: "Pots",
    201: "BlueDungeonBrick", 202: "GreenDungeonBrick", 203: "PinkDungeonBrick",
    204: "Ebonsandstone", 205: "Crimsandstone", 206: "Pearlsandstone",
    207: "CorruptSandstone", 208: "CrimsonSandstone", 209: "HallowSandstone",
    210: "DesertFossil", 211: "CopperPlating", 212: "Snail",
    213: "LavaMoss", 214: "VineFlowers", 215: "Campfire",
    216: "MushroomBlock", 217: "HardenedSand", 218: "SandstoneColumn",
    219: "Bamboo", 220: "GlowingMoss", 221: "CorruptMushroomGrass",
    222: "CrimsonMushroomGrass", 223: "CrimsonPlants", 224: "HallowedPlants",
    225: "CorruptPlants", 226: "CorruptionThorns", 227: "CrimsonThorns",
    228: "SunplateBlock", 229: "BugHive", 230: "BreakableIce",
    231: "AncientBlueDungeonBrick", 232: "AncientGreenDungeonBrick",
    233: "AncientPinkDungeonBrick", 234: "LihzahrdBrick", 235: "Terrarium",
    236: "HoneyfallBlock", 237: "CookingPot", 238: "BewitchingTable",
    239: "AlchemyLamp", 240: "TatteredWoodSign", 241: "RuneStone",
    242: "DeadlySunstone", 243: "SolarBrick", 244: "VortexBrick",
    245: "NebulaBrick", 246: "StardustBrick", 247: "LunarBrick",
    248: "DemoniteBrick", 249: "CrimsandBrick", 250: "AmethystBunnyCage",
    267: "Larva", 268: "CrimtaneOre", 269: "CobaltOre", 270: "MythrilOre",
    271: "AdamantiteOre", 272: "PalladiumOre", 273: "OrichalcumOre",
    274: "TitaniumOre", 275: "ChlorophyteOre", 276: "CrystalBlock",
    277: "Rope", 278: "Chain", 279: "MithrilOre", 280: "BlueDungeonBrick",
    287: "CobaltBrick", 288: "MythrilBrick", 289: "ChlorophyteBrick",
    290: "PalladiumBrick", 291: "OrichalcumBrick", 292: "TitaniumBrick",
    293: "ShroomitePlating", 294: "MartianConduitPlating", 295: "SpookyWood",
    296: "SpookyWoodPlatform", 297: "SpookyWoodChair", 298: "SpookyWoodTable",
    299: "SpookyWoodWorkBench", 300: "SpookyWoodDoor", 301: "SpookyWoodBed",
    302: "AlchemyTable", 303: "SillyPinkBalloonTile", 304: "SillyPurpleBalloonTile",
    305: "SillyGreenBalloonTile", 306: "TeamBlockRed", 307: "TeamBlockRedPlatform",
    319: "HeavyWorkBench", 320: "CopperCoinPile", 321: "SilverCoinPile",
    322: "GoldCoinPile", 323: "PlatinumCoinPile", 324: "Chimney",
    325: "Coal", 326: "Presents2", 327: "Ornament", 328: "HolidayLights",
    329: "PineTree", 330: "DeadTree", 331: "ChristmasPalmTree",
    386: "TrapDoor", 387: "TrapDoor", 388: "TallGate", 389: "TallGate",
    390: "LunarMonolith", 391: "LunarMonolith", 392: "LunarMonolith",
    393: "LunarMonolith", 394: "LunarMonolith", 395: "Monoliths",
    412: "BlendOMatic", 413: "MeatGrinder", 414: "Extractinator",
    415: "Solidifier", 416: "Clentaminator", 417: "Autohammer",
    460: "ImbuingStation", 461: "AncientForge", 462: "AncientHMTile",
    487: "Autohammer", 488: "DefendersForge", 489: "VoidVault",
    519: "CrystalBall", 520: "ArcaneRune", 521: "DefenderOrb",
    633: "DynastyWood", 634: "DynastyWall", 635: "RedDynastyShingles",
    636: "BlueDynastyShingles", 637: "WhiteDynastyShingles",
}

# 墙壁ID映射
WALL_ID_MAP = {
    0: "StoneWall", 1: "DirtWall", 2: "DirtWall", 3: "StoneWall",
    4: "WoodWall", 5: "GrayBrickWall", 6: "GoldBrickWall", 7: "SilverBrickWall",
    8: "CopperBrickWall", 9: "IronBrickWall", 10: "GlassWall",
    11: "StoneSlabWall", 12: "SandstoneWall", 13: "SnowWall",
    14: "MarbleWall", 15: "GraniteWall", 16: "PearlstoneWall",
    17: "EbonstoneWall", 18: "CrimstoneWall", 19: "RedBrickWall",
    20: "DynastyWall", 21: "MushroomWall", 22: "BorealWoodWall",
    23: "PalmWoodWall", 24: "RichMahoganyWall", 25: "PearlwoodWall",
    26: "EbonsandstoneWall", 27: "CrimsandstoneWall", 28: "PearlsandstoneWall",
    29: "CorruptSandstoneWall", 30: "CrimsonSandstoneWall", 31: "HallowSandstoneWall",
    32: "CorruptHardenedSandWall", 33: "CrimsonHardenedSandWall", 34: "HallowHardenedSandWall",
    35: "DesertHardenedSandWall", 36: "DesertSandstoneWall", 37: "DesertFossilWall",
    40: "WoodenFence", 41: "MetalFence", 42: "BlueDungeonBrickWall",
    43: "GreenDungeonBrickWall", 44: "PinkDungeonBrickWall", 45: "BlueDungeonSlabWall",
    46: "GreenDungeonSlabWall", 47: "PinkDungeonSlabWall", 48: "BlueDungeonTileWall",
    49: "GreenDungeonTileWall", 50: "PinkDungeonTileWall",
    59: "LivingWoodWall", 60: "LivingMahoganyWall",
    62: "EbonstoneBrickWall", 63: "CrimstoneBrickWall", 64: "PearlstoneBrickWall",
    69: "PlankedWall", 70: "PurpleStainedGlass", 71: "YellowStainedGlass",
    72: "BlueStainedGlass", 73: "GreenStainedGlass", 74: "RedStainedGlass",
    75: "MulticoloredStainedGlass", 76: "BorealWoodFence", 77: "PalmWoodFence",
    81: "SpiderWall", 82: "Leather", 83: "LavaLampWall",
    87: "WaterfallWall", 88: "LeafBlockWall",
    90: "AmethystGemsparkWall", 91: "TopazGemsparkWall", 92: "SapphireGemsparkWall",
    93: "EmeraldGemsparkWall", 94: "RubyGemsparkWall", 95: "DiamondGemsparkWall",
    96: "AmberGemsparkWall", 97: "AmethystGemsparkWallOff", 98: "TopazGemsparkWallOff",
    99: "SapphireGemsparkWallOff", 100: "EmeraldGemsparkWallOff",
    101: "RubyGemsparkWallOff", 102: "DiamondGemsparkWallOff", 103: "AmberGemsparkWallOff",
}

# 家具功能映射
FURNITURE_FUNCTIONS = {
    "light_source": ["Torch", "Candle", "Chandelier", "Lamp", "Campfire", "TikiTorch", "Fireplace", "ChineseLantern", "JapaneseLantern", "LavaLamp"],
    "flat_surface": ["Table", "WorkBench", "Dresser", "Piano", "Bookcase", "HeavyWorkBench"],
    "comfort": ["Chair", "Bed", "Sofa", "Throne", "Bench"],
    "entry": ["Door", "TrapDoor", "Platform", "TallGate"],
    "storage": ["Chest", "PiggyBank", "Safe", "DefendersForge", "VoidVault"],
    "crafting": ["WorkBench", "Anvil", "Furnace", "Loom", "Keg", "CookingPot", "AlchemyTable", "HeavyWorkBench", "ImbuingStation", "Autohammer", "CrystalBall", "BlendOMatic"],
}

# 风格映射
TILE_STYLES = {
    "medieval": ["GrayBrick", "StoneSlab", "Stone", "Wood"],
    "natural": ["Wood", "Dirt", "Grass", "LivingWood", "LeafBlock"],
    "asian": ["DynastyWood", "BambooBlock", "Bamboo"],
    "snow": ["SnowBlock", "IceBlock", "BorealWood"],
    "desert": ["Sandstone", "SandstoneSlab", "PalmWood"],
    "ocean": ["PalmWood", "Glass", "Coral"],
    "jungle": ["RichMahogany", "LivingMahogany"],
    "hallow": ["Pearlstone", "Pearlwood", "CrystalBlock"],
    "corruption": ["Ebonstone", "Ebonsand"],
    "crimson": ["Crimstone", "Crimsand"],
    "gothic": ["Obsidian", "Ebonstone", "RedBrick"],
    "modern": ["Granite", "Marble", "Glass"],
    "steampunk": ["CopperBrick", "IronBrick"],
    "luxury": ["GoldBrick", "SilverBrick", "PlatinumBrick"],
    "mushroom": ["MushroomBlock", "GlowingMoss"],
}

# ==================== 数据类 ====================

@dataclass
class BuildingTile:
    """建筑方块"""
    x: int
    y: int
    type_id: int
    type_name: str
    wall_id: int = 0
    wall_name: str = ""
    paint: int = 0

@dataclass
class BuildingEntity:
    """建筑实体"""
    name: str
    source_file: str
    width: int
    height: int
    tiles: List[BuildingTile] = field(default_factory=list)
    tile_counts: Dict[str, int] = field(default_factory=dict)
    wall_counts: Dict[str, int] = field(default_factory=dict)
    detected_style: str = "unknown"
    has_room: bool = False
    is_valid_house: bool = False
    furniture: Dict[str, int] = field(default_factory=dict)
    functions: Dict[str, int] = field(default_factory=dict)

    def to_dict(self) -> dict:
        return {
            "name": self.name,
            "source": self.source_file,
            "size": {"w": self.width, "h": self.height},
            "style": self.detected_style,
            "is_house": self.has_room,
            "is_valid_npc_house": self.is_valid_house,
            "materials": self.tile_counts,
            "walls": self.wall_counts,
            "furniture": self.furniture,
            "functions": self.functions,
        }

# ==================== 解析器 ====================

class SchematicParser:
    """Schematic解析器"""

    def parse(self, file_path: str) -> BuildingEntity:
        """解析schematic文件"""
        with open(file_path, 'r', encoding='utf-8') as f:
            data = json.load(f)

        building = BuildingEntity(
            name=data.get('name', os.path.basename(file_path)),
            source_file=file_path,
            width=data.get('width', 0),
            height=data.get('height', 0)
        )

        # 解析方块
        tiles_data = data.get('tiles', [])
        for x, col in enumerate(tiles_data):
            for y, tile in enumerate(col):
                type_id = tile.get('type')
                if type_id is not None and type_id > 0:
                    type_name = TILE_ID_MAP.get(type_id, f"Tile_{type_id}")
                    wall_id = tile.get('wall') or 0
                    wall_name = WALL_ID_MAP.get(wall_id, f"Wall_{wall_id}")
                    paint = tile.get('tile_color') or 0

                    bt = BuildingTile(x, y, type_id, type_name, wall_id, wall_name, paint)
                    building.tiles.append(bt)

                    # 统计
                    building.tile_counts[type_name] = building.tile_counts.get(type_name, 0) + 1
                    if wall_id > 0:
                        building.wall_counts[wall_name] = building.wall_counts.get(wall_name, 0) + 1

                    # 家具检测
                    self._check_furniture(building, type_name)

        # 检测风格
        building.detected_style = self._detect_style(building)

        # 检测房间
        building.has_room = self._has_room(building)

        # 验证NPC房屋
        building.is_valid_house = self._validate_house(building)

        # 生成功能统计
        building.functions = self._count_functions(building)

        return building

    def _check_furniture(self, building: BuildingEntity, type_name: str):
        """检测家具"""
        for func, names in FURNITURE_FUNCTIONS.items():
            if type_name in names:
                building.furniture[type_name] = building.furniture.get(type_name, 0) + 1

    def _detect_style(self, building: BuildingEntity) -> str:
        """检测风格"""
        style_scores = {}
        for style, materials in TILE_STYLES.items():
            score = sum(building.tile_counts.get(m, 0) for m in materials)
            if score > 0:
                style_scores[style] = score

        if not style_scores:
            return "unknown"

        return max(style_scores.items(), key=lambda x: x[1])[0]

    def _has_room(self, building: BuildingEntity) -> bool:
        """检测是否有房间"""
        # 有墙壁覆盖且有内部空间
        has_walls = len(building.wall_counts) > 0
        has_space = building.width >= 7 and building.height >= 7
        has_furniture = len(building.furniture) > 0
        return has_walls and has_space and has_furniture

    def _validate_house(self, building: BuildingEntity) -> bool:
        """验证是否为有效NPC房屋"""
        if not building.has_room:
            return False

        # 检查必要元素
        has_light = building.functions.get("light_source", 0) > 0
        has_surface = building.functions.get("flat_surface", 0) > 0
        has_comfort = building.functions.get("comfort", 0) > 0
        has_entry = building.functions.get("entry", 0) > 0

        return has_light and has_surface and has_comfort and has_entry

    def _count_functions(self, building: BuildingEntity) -> Dict[str, int]:
        """统计功能"""
        functions = {}
        for name, count in building.furniture.items():
            for func, names in FURNITURE_FUNCTIONS.items():
                if name in names:
                    functions[func] = functions.get(func, 0) + count
        return functions

# ==================== 描述生成器 ====================

def generate_description(building: BuildingEntity, format: str = "compact") -> str:
    """生成精简描述"""
    if format == "compact":
        return _generate_compact(building)
    elif format == "json":
        return json.dumps(building.to_dict(), ensure_ascii=False, indent=2)
    else:
        return _generate_full(building)

def _generate_compact(building: BuildingEntity) -> str:
    """生成精简描述"""
    lines = []

    # 基本信息
    lines.append(f"【{building.name}】{building.width}x{building.height}, {building.detected_style}风格")

    # 材料（仅列出主要的）
    if building.tile_counts:
        main = sorted(building.tile_counts.items(), key=lambda x: -x[1])[:3]
        lines.append(f"材料: {', '.join([f'{n}({c})' for n, c in main])}")

    # 墙壁
    if building.wall_counts:
        main_w = sorted(building.wall_counts.items(), key=lambda x: -x[1])[:2]
        lines.append(f"墙壁: {', '.join([n for n, c in main_w])}")

    # 家具
    if building.furniture:
        lines.append(f"家具: {', '.join([f'{n}({c})' for n, c in building.furniture.items()])}")

    # 判定
    if building.is_valid_house:
        lines.append("✓ 有效NPC房屋")
    elif building.has_room:
        lines.append("△ 部分符合房屋要求")
    else:
        lines.append("✗ 非完整房屋")

    return "\n".join(lines)

def _generate_full(building: BuildingEntity) -> str:
    """生成完整描述"""
    lines = []

    lines.append(f"# {building.name}")
    lines.append(f"尺寸: {building.width}x{building.height}")
    lines.append(f"风格: {building.detected_style}")
    lines.append("")

    # 方块统计
    lines.append("## 方块")
    for name, count in sorted(building.tile_counts.items(), key=lambda x: -x[1]):
        lines.append(f"- {name}: {count}")

    # 墙壁统计
    if building.wall_counts:
        lines.append("\n## 墙壁")
        for name, count in sorted(building.wall_counts.items(), key=lambda x: -x[1]):
            lines.append(f"- {name}: {count}")

    # 家具
    if building.furniture:
        lines.append("\n## 家具")
        for name, count in building.furniture.items():
            lines.append(f"- {name}: {count}")

    # 功能
    if building.functions:
        lines.append("\n## 功能")
        for name, count in building.functions.items():
            lines.append(f"- {name}: {count}")

    # 房屋验证
    lines.append("\n## 房屋验证")
    lines.append(f"- 有房间结构: {'是' if building.has_room else '否'}")
    lines.append(f"- 有效NPC房屋: {'是' if building.is_valid_house else '否'}")

    return "\n".join(lines)

def render_tile_grid(building: BuildingEntity) -> str:
    """渲染方块ID网格"""
    grid = [[0] * building.height for _ in range(building.width)]

    for tile in building.tiles:
        if 0 <= tile.x < building.width and 0 <= tile.y < building.height:
            grid[tile.x][tile.y] = tile.type_id

    lines = []
    lines.append(f"// Tile ID Grid for {building.name}")
    lines.append(f"// Size: {building.width}x{building.height}")
    lines.append("[")

    for y in range(building.height):
        row = []
        for x in range(building.width):
            row.append(str(grid[x][y]))
        lines.append("  [" + ", ".join(row) + "],")

    lines.append("]")
    return "\n".join(lines)

# ==================== 批量处理 ====================

def process_directory(input_dir: str, output_file: str = None):
    """批量处理schematic文件"""
    parser = SchematicParser()
    buildings = []

    # 查找所有json文件
    for file_path in glob.glob(os.path.join(input_dir, "*.json")):
        try:
            building = parser.parse(file_path)
            buildings.append(building)
            print(f"解析: {building.name}")
        except Exception as e:
            print(f"错误: {file_path} - {e}")

    # 生成汇总
    if output_file:
        output = {
            "generated_at": datetime.now().isoformat(),
            "total_buildings": len(buildings),
            "buildings": [b.to_dict() for b in buildings]
        }
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(output, f, ensure_ascii=False, indent=2)
        print(f"\n已保存到: {output_file}")

    return buildings

# ==================== 主函数 ====================

if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Terraria建筑解析工具")
    parser.add_argument("input", help="输入文件或目录")
    parser.add_argument("-o", "--output", help="输出文件")
    parser.add_argument("-f", "--format", choices=["compact", "full", "json"], default="compact")
    parser.add_argument("--grid", action="store_true", help="输出方块ID网格")

    args = parser.parse_args()

    if os.path.isdir(args.input):
        buildings = process_directory(args.input, args.output)
        for b in buildings:
            print("\n" + generate_description(b, args.format))
    else:
        schematic_parser = SchematicParser()
        building = schematic_parser.parse(args.input)
        print(generate_description(building, args.format))

        if args.grid:
            print("\n" + render_tile_grid(building))

        if args.output:
            with open(args.output, 'w', encoding='utf-8') as f:
                f.write(generate_description(building, "json"))