# 泰拉瑞亚 WorldGen（世界生成）系统文档

## 目录
1. [WorldGen类概述](#1-worldgen类概述)
2. [WorldGen主要方法](#2-worldgen主要方法)
3. [程序化生成建筑的标准方式](#3-程序化生成建筑的标准方式)
4. [结构保护机制](#4-结构保护机制)
5. [在已存在世界中生成建筑](#5-在已存在世界中生成建筑)
6. [房屋检测算法](#6-房屋检测算法)

---

## 1. WorldGen类概述

`WorldGen` 是泰拉瑞亚中用于世界生成和地形操作的核心静态类。它包含了大量用于创建、修改和管理世界Tile的方法。

### 1.1 命名空间
```csharp
namespace Terraria
{
    public static class WorldGen
    {
        // 所有世界生成相关方法
    }
}
```

### 1.2 核心属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `genRand` | Random | 世界生成专用的随机数生成器 |
| `worldSurface` | double | 地表高度（地表层） |
| `rockLayer` | double | 岩石层起始高度 |
| `structures` | List<Rectangle> | 已生成结构的区域列表 |
| `protectedTile` | bool[,] | 受保护的tile标记数组 |

### 1.3 世界尺寸常量
```csharp
Main.maxTilesX  // 世界宽度（小世界: 4200, 中世界: 6400, 大世界: 8400）
Main.maxTilesY  // 世界高度（小世界: 1200, 中世界: 1800, 大世界: 2400）
```

---

## 2. WorldGen主要方法

### 2.1 地形生成方法

#### WorldGen.TileRunner
创建矿脉、洞穴等地形特征的核心方法。

```csharp
// 方法签名
public static void TileRunner(
    int i,           // 起始X坐标（tile坐标）
    int j,           // 起始Y坐标（tile坐标）
    double strength, // 强度：控制生成范围大小
    int steps,       // 步数：控制迭代次数
    ushort type,     // Tile类型ID
    bool addTile,    // true=添加tile, false=移除tile（挖洞）
    float speedX,    // 初始水平方向速度
    float speedY     // 初始垂直方向速度
)

// 示例：生成矿脉
WorldGen.TileRunner(
    x, y,
    WorldGen.genRand.Next(5, 10),    // 随机强度
    WorldGen.genRand.Next(5, 15),    // 随机步数
    (ushort)ModContent.TileType<CustomOre>(),
    true, 0f, 0f
);

// 示例：挖洞穴
WorldGen.TileRunner(x, y, 8, 20, 0, false, 0f, 0f);
```

#### WorldGen.digTile
挖掘单个Tile。

```csharp
public static bool digTile(int x, int y)

// 示例
bool success = WorldGen.digTile(x, y);
```

#### WorldGen.KillTile
移除Tile（可选择是否掉落物品）。

```csharp
public static void KillTile(int x, int y, bool fail = false, bool effectOnly = false, bool noItem = false)

// 示例：移除tile但不掉落物品
WorldGen.KillTile(x, y, false, false, true);
```

### 2.2 Tile放置方法

#### WorldGen.PlaceTile
放置单个Tile。

```csharp
public static bool PlaceTile(int x, int y, ushort type, bool mute = false, bool forced = false, int style = 0)

// 示例
WorldGen.PlaceTile(x, y, TileID.Stone);
WorldGen.PlaceTile(x, y, TileID.DiamondGemspark, false, false, 0);
```

#### WorldGen.PlaceWall
放置背景墙。

```csharp
public static void PlaceWall(int x, int y, ushort type, bool mute = false)

// 示例
WorldGen.PlaceWall(x, y, WallID.Stone);
```

#### WorldGen.PlaceObject
放置多格物体（家具、宝箱等）。

```csharp
public static bool PlaceObject(int x, int y, ushort type, int style = 0, int alternate = 0, int direction = -1)

// 示例：放置工作台（2格宽）
WorldGen.PlaceObject(x, y, TileID.WorkBench);
```

#### WorldGen.PlaceChest
放置宝箱。

```csharp
public static int PlaceChest(int x, int y, ushort type = TileID.Containers, int style = 0, int direction = -1)

// 示例
int chestIndex = WorldGen.PlaceChest(x, y, TileID.Containers2, 0);
if (chestIndex >= 0)
{
    Chest chest = Main.chest[chestIndex];
    chest.item[0].SetDefaults(ItemID.GoldCoin);
    chest.item[0].stack = 50;
}
```

### 2.3 清理与挖掘方法

#### WorldGen.EmptyTile / WorldGen.EmptyRect
清空区域。

```csharp
// 清空单个tile
WorldGen.EmptyTile(x, y, true);  // 同时清除背景墙

// 挖掘矩形区域
WorldGen.digHole(x, y, width, height);
```

#### WorldGen.SquareTile / WorldGen.SquareTiles
清理正方形区域。

```csharp
public static void SquareTiles(int x, int y, int size)

// 清理区域
for (int i = x; i < x + width; i++)
{
    for (int j = y; j < y + height; j++)
    {
        WorldGen.EmptyTile(i, j, true);
    }
}
```

### 2.4 结构生成方法

#### WorldGen.PlaceHouse
放置房屋结构（用于NPC房屋）。

```csharp
public static void PlaceHouse(int x, int y, int width, int height, ushort wallType)
```

#### WorldGen.AddLifeCrystal
添加生命水晶。

```csharp
public static void AddLifeCrystal(int x, int y)
```

#### WorldGen.AddTree
种树。

```csharp
public static bool AddTree(int x, int y, bool noGroundCheck = false)
```

### 2.5 检测方法

#### WorldGen.SolidTile
检测是否为实心Tile。

```csharp
public static bool SolidTile(int x, int y)
public static bool SolidTile(Tile tile)
```

#### WorldGen.CanKillTile
检测Tile是否可被破坏。

```csharp
public static bool CanKillTile(int x, int y)
```

#### WorldGen.InWorld
检测坐标是否在世界范围内。

```csharp
public static bool InWorld(int x, int y, int fluff = 0)
```

---

## 3. 程序化生成建筑的标准方式

### 3.1 在世界生成时添加建筑

使用 `ModSystem.ModifyWorldGenTasks` 钩子在世界生成过程中添加自定义结构。

```csharp
public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
{
    // 找到特定生成阶段的索引
    int shiniesIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Shinies"));

    if (shiniesIndex != -1)
    {
        // 在矿石生成之后插入自定义生成
        tasks.Insert(shiniesIndex + 1, new PassLegacy("MyCustomStructure", (progress, configuration) =>
        {
            progress.Message = "生成自定义结构...";

            // 生成逻辑
            for (int i = 0; i < 5; i++)
            {
                int x = WorldGen.genRand.Next(200, Main.maxTilesX - 200);
                int y = WorldGen.genRand.Next((int)GenVars.worldSurface, (int)GenVars.rockLayer);

                if (IsValidLocation(x, y))
                {
                    GenerateMyStructure(x, y);
                }
            }
        }));
    }
}
```

### 3.2 生成阶段顺序

世界生成的主要阶段（按顺序）：

| 阶段名称 | 说明 |
|---------|------|
| `Surface` | 地表生成 |
| `Dungeon` | 地牢生成 |
| `Rocks` | 岩石层生成 |
| `Tunnels` | 洞穴隧道生成 |
| `Caves` | 洞穴系统生成 |
| `Shinies` | 矿石生成 |
| `Trees` | 树木生成 |
| `Planting` | 草和植物生成 |
| `LifeCrystals` | 生命水晶生成 |
| `Floating Islands` | 浮空岛生成 |
| `Micro Biomes` | 微型生物群落 |

### 3.3 标准建筑生成模板

```csharp
private void GenerateMyStructure(int startX, int startY)
{
    // 1. 清理空间
    int width = 20;
    int height = 15;

    for (int x = startX; x < startX + width; x++)
    {
        for (int y = startY; y < startY + height; y++)
        {
            WorldGen.EmptyTile(x, y, true);
        }
    }

    // 2. 放置地板
    for (int x = startX; x < startX + width; x++)
    {
        WorldGen.PlaceTile(x, startY + height - 1, TileID.StoneSlab);
    }

    // 3. 放置墙壁
    for (int x = startX; x < startX + width; x++)
    {
        for (int y = startY; y < startY + height - 1; y++)
        {
            if (x == startX || x == startX + width - 1)
            {
                WorldGen.PlaceTile(x, y, TileID.StoneSlab);
            }
            else
            {
                WorldGen.PlaceWall(x, y, WallID.StoneSlab);
            }
        }
    }

    // 4. 放置家具
    WorldGen.PlaceObject(startX + 5, startY + height - 2, TileID.WorkBench);
    WorldGen.PlaceObject(startX + 8, startY + height - 2, TileID.Chairs);
    WorldGen.PlaceObject(startX + 10, startY + height - 2, TileID.Torches);

    // 5. 保护结构（可选）
    WorldGen.ProtectArea(startX, startY, width, height);

    // 6. 记录结构位置
    GenVars.structures.AddStructure(new Rectangle(startX, startY, width, height));
}

private bool IsValidLocation(int x, int y)
{
    // 检查是否在有效区域
    if (!WorldGen.InWorld(x, y, 50)) return false;

    // 检查是否与现有结构冲突
    foreach (var structure in GenVars.structures)
    {
        if (structure.Intersects(new Rectangle(x - 10, y - 10, 20, 20)))
            return false;
    }

    return true;
}
```

---

## 4. 结构保护机制

### 4.1 WorldGen.Protect方法

用于保护特定区域不被后续的生成过程修改。

```csharp
// 保护单个Tile
WorldGen.Protect(x, y, true);   // true = 保护
WorldGen.Protect(x, y, false);  // false = 取消保护

// 保护区域
public static void ProtectArea(int startX, int startY, int width, int height)
{
    for (int x = startX; x < startX + width; x++)
    {
        for (int y = startY; y < startY + height; y++)
        {
            WorldGen.Protect(x, y, true);
        }
    }
}
```

### 4.2 GenVars.structures

记录已生成结构的位置，防止重叠。

```csharp
// 添加结构到保护列表
GenVars.structures.AddStructure(new Rectangle(x, y, width, height));

// 检查位置是否可用
foreach (var structure in GenVars.structures)
{
    if (structure.Intersects(newArea))
        return false; // 位置已被占用
}
```

### 4.3 特殊保护区域

| 区域类型 | 保护机制 |
|---------|---------|
| 丛林神庙 | Lihzahrd砖块 + Plantera检测 |
| 地牢 | Dungeon Guardian生成检测 |
| 浮空岛 | WorldGen.protectedTile标记 |
| 出生点 | 固定安全区域 |
| NPC房屋 | 玩家放置的家具自动保护 |

### 4.4 神庙保护机制详解

```csharp
// 丛林神庙的Lihzahrd砖块在Plantera被击败前无法破坏
if (Main.hardMode && NPC.downedPlantBoss)
{
    // 允许破坏神庙砖块
    tile.HasActuator = true;  // 可以使用致动器
}

// 检测是否可以破坏
public static bool CanBreakTempleBrick(int x, int y)
{
    return NPC.downedPlantBoss; // 只有击败Plantera后才能破坏
}
```

---

## 5. 在已存在世界中生成建筑

### 5.1 使用PostWorldGen钩子

在完全生成的世界后添加内容（适用于新世界）。

```csharp
public override void PostWorldGen()
{
    // 在世界生成完成后执行
    // 适合添加宝箱内容、修改现有结构等

    for (int i = 0; i < Main.maxTilesX; i++)
    {
        for (int j = 0; j < Main.maxTilesY; j++)
        {
            // 遍历世界tile进行修改
        }
    }
}
```

### 5.2 游戏运行时生成结构

在游戏进行中生成建筑（适用于玩家触发生成）。

```csharp
public override void MyModFunction()
{
    // 确保只在服务器端执行
    if (Main.netMode == NetmodeID.MultiplayerClient)
        return;

    int x = Player.position.X / 16 + 10;
    int y = Player.position.Y / 16;

    GenerateBuilding(x, y);

    // 多人模式下需要同步
    if (Main.netMode == NetmodeID.Server)
    {
        NetMessage.SendTileSquare(-1, x, y, 20, 15);
    }
}
```

### 5.3 使用Schematic系统（推荐）

使用结构文件可以保存和加载预定义的建筑。

#### 安装StructureHelper模组
```
StructureHelper模组可以从Steam创意工坊下载
```

#### 保存结构
```csharp
// 使用StructureHelper保存当前区域的建筑
StructureHelper.SchematicManager.SaveSchematic("MyBuilding", x, y, width, height);
```

#### 加载并放置结构
```csharp
public void PlaceMyBuilding(int x, int y)
{
    // 加载schematic文件
    var schematic = StructureHelper.SchematicManager.LoadSchematic("MyBuilding");

    // 放置结构
    StructureHelper.SchematicManager.PlaceSchematic(schematic, x, y,
        StructureHelper.Mod,  // 目标mod
        true  // 是否同步到客户端
    );
}
```

### 5.4 直接Tile操作方式

```csharp
/// <summary>
/// 在运行时生成完整的房屋
/// </summary>
public void GenerateHouse(int x, int y, int width = 10, int height = 8)
{
    // 1. 清理空间
    for (int i = x; i < x + width; i++)
    {
        for (int j = y; j < y + height; j++)
        {
            Main.tile[i, j].ClearTile();
            Main.tile[i, j].WallType = 0;
        }
    }

    // 2. 建造墙壁
    for (int i = x; i < x + width; i++)
    {
        // 地板
        WorldGen.PlaceTile(i, y + height - 1, TileID.Wood);
        // 天花板
        WorldGen.PlaceTile(i, y, TileID.Wood);
    }

    for (int j = y; j < y + height; j++)
    {
        // 左墙
        WorldGen.PlaceTile(x, j, TileID.Wood);
        // 右墙
        WorldGen.PlaceTile(x + width - 1, j, TileID.Wood);
    }

    // 3. 放置门
    WorldGen.PlaceDoor(x + width / 2, y + height - 3, TileID.ClosedDoor);

    // 4. 放置背景墙
    for (int i = x + 1; i < x + width - 1; i++)
    {
        for (int j = y + 1; j < y + height - 1; j++)
        {
            WorldGen.PlaceWall(i, j, WallID.Wood);
        }
    }

    // 5. 放置家具
    WorldGen.PlaceObject(x + 2, y + height - 2, TileID.Tables);
    WorldGen.PlaceObject(x + 4, y + height - 2, TileID.Chairs);
    WorldGen.PlaceObject(x + 6, y + height - 2, TileID.Torches);

    // 6. 同步到客户端（多人游戏必需）
    if (Main.netMode == NetmodeID.Server)
    {
        NetMessage.SendTileSquare(-1, x, y, width, height);
    }
}
```

### 5.5 检测并适配地形

```csharp
/// <summary>
/// 找到适合生成建筑的地表位置
/// </summary>
public int FindSurface(int x)
{
    int y = (int)Main.worldSurface;

    while (y < Main.maxTilesY)
    {
        if (WorldGen.SolidTile(x, y))
        {
            return y; // 找到地面
        }
        y++;
    }

    return -1; // 未找到
}

/// <summary>
/// 平整地面以生成建筑
/// </summary>
public void FlattenGround(int startX, int endX, int targetY)
{
    for (int x = startX; x < endX; x++)
    {
        int surfaceY = FindSurface(x);

        if (surfaceY > targetY)
        {
            // 填充到目标高度
            for (int y = targetY; y < surfaceY; y++)
            {
                WorldGen.PlaceTile(x, y, TileID.Dirt);
            }
        }
        else if (surfaceY < targetY && surfaceY != -1)
        {
            // 移除多余方块
            for (int y = surfaceY; y < targetY; y++)
            {
                WorldGen.KillTile(x, y, false, false, true);
            }
        }
    }
}
```

---

## 6. 房屋检测算法

### 6.1 房屋有效性要求

游戏判断一个房间是否为有效房屋的条件：

| 要求 | 详情 |
|------|------|
| **尺寸** | 最少60个tile，内部尺寸至少 8x7 或 10x6 或 7x9 |
| **墙壁** | 必须有玩家放置的背景墙（天然墙不算） |
| **框架** | 必须被实心方块包围 |
| **入口** | 必须有门、平台或高门作为入口 |
| **光源** | 必须有一个光源（火把、蜡烛、吊灯等） |
| **平坦表面** | 必须有一个桌子或工作台 |
| **舒适物品** | 必须有一个椅子或床 |
| **生物群落** | 不能处于腐化/猩红生物群落中 |

### 6.2 房屋检测算法流程

```
1. 从查询点开始进行洪水填充
   └─> 检查背景墙是否有效
   └─> 计算房间总面积

2. 验证房间尺寸
   └─> 最小60个tile
   └─> 尺寸满足最低要求

3. 扫描房间内部
   └─> 检测光源
   └─> 检测平坦表面（桌子）
   └─> 检测舒适物品（椅子）

4. 检测是否被包围
   └─> 四周有实心方块

5. 生物群落检测
   └─> 附近没有腐化/猩红方块

6. 返回结果
   └─> 有效/无效 + 原因
```

### 6.3 房屋检测核心代码

```csharp
// 房屋检测的主要入口
public static bool CheckHouse(int x, int y)
{
    // 开始房屋检测
    return WorldGen.StartRoomCheck(x, y);
}

// 房屋检测的主函数
public static bool StartRoomCheck(int x, int y)
{
    // 重置检测数据
    housing.CompletelyWalled = false;
    housing.isRoom = false;
    housing.hasTable = false;
    housing.hasChair = false;
    housing.hasTorch = false;
    housing.tileCount = 0;

    // 执行洪水填充
    FloodFillRoom(x, y);

    // 验证结果
    return ValidateRoom();
}
```

### 6.4 洪水填充算法

```csharp
// 房屋检测使用的洪水填充算法
private static void FloodFillRoom(int startX, int startY)
{
    Queue<Point> queue = new Queue<Point>();
    HashSet<Point> visited = new HashSet<Point>();

    queue.Enqueue(new Point(startX, startY));
    visited.Add(new Point(startX, startY));

    int[] dx = { 0, 1, 0, -1 };
    int[] dy = { 1, 0, -1, 0 };

    while (queue.Count > 0)
    {
        Point current = queue.Dequeue();
        int x = current.X;
        int y = current.Y;

        // 检查边界
        if (!WorldGen.InWorld(x, y)) continue;

        Tile tile = Main.tile[x, y];

        // 遇到实心方块，停止扩散
        if (tile.HasTile && Main.tileSolid[tile.TileType]) continue;

        // 计数
        housing.tileCount++;

        // 检查背景墙
        if (tile.WallType == 0)
        {
            // 没有背景墙，不是有效房间
            housing.CompletelyWalled = false;
            continue;
        }

        // 检测家具
        CheckFurniture(x, y, tile);

        // 继续向四个方向扩散
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];
            Point next = new Point(nx, ny);

            if (!visited.Contains(next))
            {
                visited.Add(next);
                queue.Enqueue(next);
            }
        }
    }
}
```

### 6.5 家具检测

```csharp
// 家具ID检测
private static void CheckFurniture(int x, int y, Tile tile)
{
    ushort type = tile.TileType;

    // 光源检测
    if (Main.tileLighted[type] && Main.tileLavaDeath[type] == false)
    {
        // 火把、蜡烛等光源
        if (IsLightSource(type))
        {
            housing.hasTorch = true;
        }
    }

    // 桌子检测
    if (IsTable(type))
    {
        housing.hasTable = true;
    }

    // 椅子检测
    if (IsChair(type))
    {
        housing.hasChair = true;
    }
}

// 光源类型
private static bool IsLightSource(ushort type)
{
    return type == TileID.Torches ||
           type == TileID.Candles ||
           type == TileID.Chandeliers ||
           type == TileID.Lamps ||
           type == TileID.HangingLanterns ||
           type == TileID.ChineseLanterns ||
           // ... 更多光源类型
           Main.tileLight[type];
}

// 桌子类型
private static bool IsTable(ushort type)
{
    return type == TileID.Tables ||
           type == TileID.WorkBenches ||
           type == TileID.Dressers ||
           type == TileID.PicnicTables ||
           type == TileID.Benches ||
           // ... 更多桌子类型
           TileID.Sets.HasOutfitTable[type];
}

// 椅子类型
private static bool IsChair(ushort type)
{
    return type == TileID.Chairs ||
           type == TileID.Beds ||
           type == TileID.Thrones ||
           type == TileID.Benches ||
           // ... 更多椅子类型
           TileID.Sets.HasOutfitChair[type];
}
```

### 6.6 房间验证

```csharp
private static bool ValidateRoom()
{
    // 1. 检查是否有背景墙
    if (!housing.CompletelyWalled)
    {
        housing.error = "缺少背景墙";
        return false;
    }

    // 2. 检查尺寸
    if (housing.tileCount < 60)
    {
        housing.error = "房间太小";
        return false;
    }

    // 3. 检查家具
    if (!housing.hasTorch)
    {
        housing.error = "缺少光源";
        return false;
    }

    if (!housing.hasTable)
    {
        housing.error = "缺少平坦表面（桌子）";
        return false;
    }

    if (!housing.hasChair)
    {
        housing.error = "缺少舒适物品（椅子）";
        return false;
    }

    // 4. 检查生物群落
    if (IsCorruptOrCrimson(housing.centerX, housing.centerY))
    {
        housing.error = "生物群落不适合";
        return false;
    }

    // 所有检查通过
    housing.isRoom = true;
    return true;
}
```

### 6.7 有效家具列表

| 类别 | 有效物品 |
|------|---------|
| **光源** | 火把、蜡烛、吊灯、灯笼、中灯笼、提基火把、灯、节日灯等 |
| **平坦表面** | 桌子、工作台、梳妆台、书架、钢琴、野餐桌、长椅等 |
| **舒适物品** | 椅子、床、王座、长椅、沙发、扶手椅等 |

### 6.8 NPC特殊要求

某些NPC有额外的住房要求：

| NPC | 特殊要求 |
|-----|---------|
| **商人** | 玩家物品栏有超过50银币 |
| **炸弹专家** | 玩家物品栏有炸弹 |
| **树妖** | 已击败任何Boss |
| **军火商** | 玩家物品栏有枪或子弹 |
| **护士** | 玩家生命值超过100 |
| **电工** | 已击败骷髅王 |
| **服装商** | 已击败骷髅王 |
| **哥布林工匠** | 已击败哥布林军队 |

---

## 附录A：常用Tile ID参考

```csharp
// 常用方块ID
TileID.Dirt           // 泥土
TileID.Stone          // 石头
TileID.Wood           // 木材
TileID.Brick          // 砖块
TileID.GrayBrick     // 灰砖
TileID.StoneSlab      // 石板

// 常用家具ID
TileID.WorkBench     // 工作台
TileID.Tables        // 桌子
TileID.Chairs        // 椅子
TileID.Beds          // 床
TileID.Torches       // 火把
TileID.Candles       // 蜡烛
TileID.Chandeliers   // 吊灯

// 容器
TileID.Containers    // 宝箱
TileID.Dressers      // 梳妆台

// 常用墙壁ID
WallID.Dirt          // 泥土墙
WallID.Stone         // 石墙
WallID.Wood          // 木墙
WallID.Brick         // 砖墙
```

## 附录B：世界生成常用变量

```csharp
// GenVars类中的常用变量
GenVars.worldSurface     // 地表高度
GenVars.rockLayer        // 岩石层高度
GenVars.structures       // 结构保护列表
GenVars.lakes           // 湖泊位置
GenVars.oceanSide       // 海洋位置

// 世界尺寸
Main.maxTilesX          // 世界宽度
Main.maxTilesY          // 世界高度
Main.worldSurface       // 地表Y坐标
Main.rockLayer          // 岩石层Y坐标
Main.hellLayer          // 地狱层Y坐标
```

---

## 参考资源

1. **tModLoader官方文档**: https://github.com/tModLoader/tModLoader/wiki
2. **tModLoader API文档**: https://docs.tmodloader.net/
3. **Terraria Wiki**: https://terraria.wiki.gg/
4. **tModLoader GitHub**: https://github.com/tModLoader/tModLoader
5. **StructureHelper模组**: Steam创意工坊