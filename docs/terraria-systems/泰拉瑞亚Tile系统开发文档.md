# 泰拉瑞亚 Tile（方块）系统开发文档

本文档整理自 tModLoader 官方文档、Terraria Wiki 和模组开发社区资料。

---

## 目录

1. [Tile 基本概念和类型](#1-tile-基本概念和类型)
2. [放置和移除 Tile](#2-放置和移除-tile)
3. [Tile 的重要属性](#3-tile-的重要属性)
4. [获取世界中的 Tile 信息](#4-获取世界中的-tile-信息)
5. [多 Tile 结构处理](#5-多-tile-结构处理)
6. [多人游戏同步](#6-多人游戏同步)

---

## 1. Tile 基本概念和类型

### 1.1 什么是 Tile

Tile（方块）是泰拉瑞亚世界中构成地形的基本单位。世界由二维网格组成，每个网格位置可以有一个 Tile。Tile 系统是泰拉瑞亚的核心系统之一，控制着地形生成、物理碰撞、视觉效果等重要功能。

### 1.2 Tile ID 分类

泰拉瑞亚中有 700+ 种不同的 Tile 类型，通过 `TileID` 类访问：

```csharp
// 常用 Tile ID 示例
TileID.Dirt      // 泥土块
TileID.Stone     // 石块
TileID.Grass     // 草地
TileID.WoodBlock // 木材
TileID.Torches   // 火把
TileID.Chairs    // 椅子
TileID.Tables    // 桌子
TileID.Beds      // 床
TileID.Chests    // 箱子
```

#### Tile ID 范围分类

| ID 范围 | 类别 |
|---------|------|
| 0-10 | 基础方块（泥土、石头、草地等） |
| 11-50 | 矿石和锭 |
| 51-100 | 木头类型、植物 |
| 101-200 | 砖块和特殊方块 |
| 201-300 | 家具、制作站 |
| 301-400 | 机关、电路 |
| 401-500 | 地牢、神庙方块 |
| 501-600 | 困难模式方块 |
| 601+ | 特殊事件方块 |

### 1.3 Tile 结构体

每个 Tile 位置存储的数据结构：

```csharp
// Tile 结构体主要属性
public struct Tile
{
    public ushort TileType;      // Tile 类型 ID
    public short TileFrameX;     // X 轴帧位置（用于精灵图）
    public short TileFrameY;     // Y 轴帧位置（用于精灵图）
    public byte LiquidAmount;    // 液体量 (0-255)
    public int LiquidType;       // 液体类型 (0=水, 1=岩浆, 2=蜂蜜)
    public ushort WallType;      // 墙壁类型
    public byte TileColor;       // 方块油漆颜色
    public byte WallColor;       // 墙壁油漆颜色
    public bool IsHalfBlock;     // 是否为半砖
    public int Slope;            // 斜坡类型
}
```

---

## 2. 放置和移除 Tile

### 2.1 使用 WorldGen.PlaceTile

放置单个 Tile 的基本方法：

```csharp
// 基本语法
WorldGen.PlaceTile(int x, int y, int type, bool mute = false, bool forced = false, int playerWhoAmI = 0, int style = 0);

// 示例：放置石块
WorldGen.PlaceTile(100, 200, TileID.Stone);

// 示例：放置火把（带样式）
WorldGen.PlaceTile(100, 200, TileID.Torches, style: 1);

// 示例：放置时播放声音
WorldGen.PlaceTile(100, 200, TileID.Dirt, mute: false);
```

### 2.2 使用 WorldGen.PlaceObject

放置多 Tile 对象（家具等）：

```csharp
// 基本语法
WorldGen.PlaceObject(int x, int y, int type, bool mute = false, int style = 0, int alternate = 0, int random = -1, int direction = -1);

// 示例：放置椅子
WorldGen.PlaceObject(100, 200, TileID.Chairs, style: 0);

// 示例：放置工作台
WorldGen.PlaceObject(100, 200, TileID.WorkBenches);
```

### 2.3 使用 WorldGen.KillTile

移除 Tile：

```csharp
// 基本语法
WorldGen.KillTile(int x, int y, bool fail = false, bool effectOnly = false, bool noItem = false);

// 示例：移除 Tile 并掉落物品
WorldGen.KillTile(100, 200);

// 示例：移除 Tile 不掉落物品
WorldGen.KillTile(100, 200, noItem: true);

// 示例：只播放效果不移除（用于视觉效果）
WorldGen.KillTile(100, 200, effectOnly: true);
```

### 2.4 直接操作 Tile

直接修改 Tile 数据：

```csharp
// 获取 Tile 引用（安全方式）
Tile tile = Framing.GetTileSafely(x, y);

// 直接访问 Tile
Tile tile = Main.tile[x, y];

// 设置 Tile
Main.tile[x, y].TileType = TileID.Stone;
Main.tile[x, y].HasTile = true;

// 移除 Tile
Main.tile[x, y].HasTile = false;
Main.tile[x, y].TileType = 0;

// 清除所有 Tile 数据
Main.tile[x, y].ClearEverything();

// 只清除 Tile（保留墙壁和液体）
Main.tile[x, y].ClearTile();
```

### 2.5 更新 Tile 帧

修改 Tile 后需要更新帧：

```csharp
// 更新单个 Tile 的帧
WorldGen.SquareTileFrame(int x, int y, bool resetFrame = true);

// 示例
WorldGen.KillTile(100, 200, noItem: true);
WorldGen.SquareTileFrame(100, 200);
```

### 2.6 批量操作示例

```csharp
public void PlaceArea(int startX, int startY, int width, int height, int tileType)
{
    for (int x = startX; x < startX + width; x++)
    {
        for (int y = startY; y < startY + height; y++)
        {
            if (WorldGen.InWorld(x, y))  // 确保坐标有效
            {
                WorldGen.PlaceTile(x, y, tileType);
            }
        }
    }
}

public void ClearArea(int startX, int startY, int width, int height)
{
    for (int x = startX; x < startX + width; x++)
    {
        for (int y = startY; y < startY + height; y++)
        {
            if (WorldGen.InWorld(x, y) && Main.tile[x, y].HasTile)
            {
                WorldGen.KillTile(x, y, noItem: true);
                WorldGen.SquareTileFrame(x, y);
            }
        }
    }
}
```

---

## 3. Tile 的重要属性

### 3.1 基础属性

```csharp
// 检查 Tile 是否存在
bool hasTile = Main.tile[x, y].HasTile;

// 获取 Tile 类型
ushort tileType = Main.tile[x, y].TileType;

// 获取帧位置
short frameX = Main.tile[x, y].TileFrameX;
short frameY = Main.tile[x, y].TileFrameY;
```

### 3.2 形状属性（斜坡和半砖）

```csharp
Tile tile = Main.tile[x, y];

// 检查是否为半砖
if (tile.IsHalfBlock)
{
    // 半砖只有正常高度的一半
}

// 获取斜坡类型
int slope = tile.Slope;
// slope 值:
// 0 = 完整方块
// 1 = 右下斜坡
// 2 = 左下斜坡
// 3 = 右上斜坡
// 4 = 左上斜坡

// 检查是否为斜坡
if (tile.Slope != SlopeType.Solid)
{
    // 这是一个斜坡 Tile
}
```

### 3.3 碰撞属性

```csharp
// 检查 Tile 是否为实心（可碰撞）
if (Main.tileSolid[tile.TileType])
{
    // 这是一个实心 Tile
}

// 检查 Tile 是否阻挡光线
if (Main.tileLighted[tile.TileType])
{
    // 这个 Tile 会发光
}

// 检查 Tile 是否可以合并到泥土
if (Main.tileMergeDirt[tile.TileType])
{
    // 可以与泥土合并
}

// 其他常用碰撞相关数组
Main.tileNoAttach[tileType];      // 不能附着其他物体
Main.tileNoFail[tileType];        // 挖掘不会失败
Main.tileBrick[tileType];         // 是砖块类型
Main.tileBlockLight[tileType];    // 阻挡光线
```

### 3.4 颜色属性（油漆）

```csharp
Tile tile = Main.tile[x, y];

// 获取方块油漆颜色
byte paintColor = tile.TileColor;

// 获取墙壁油漆颜色
byte wallPaintColor = tile.WallColor;

// 油漆颜色 ID
// 0 = 无油漆
// 1-30 = 各种颜色

// 设置油漆（需要同步）
tile.TileColor = 1;  // 红色油漆
```

### 3.5 液体属性

```csharp
Tile tile = Main.tile[x, y];

// 检查液体
if (tile.LiquidAmount > 0)
{
    // 获取液体类型
    int liquidType = tile.LiquidType;
    // 0 = 水
    // 1 = 岩浆
    // 2 = 蜂蜜
    
    // 液体量 (0-255)
    byte amount = tile.LiquidAmount;
}
```

### 3.6 墙壁属性

```csharp
Tile tile = Main.tile[x, y];

// 获取墙壁类型
ushort wallType = tile.WallType;

// 检查是否有墙壁
if (tile.WallType > 0)
{
    // 这个位置有墙壁
}

// 墙壁 ID 示例
WallID.Dirt;      // 泥土墙
WallID.Stone;     // 石墙
WallID.Wood;      // 木墙
```

---

## 4. 获取世界中的 Tile 信息

### 4.1 访问 Tile 数组

```csharp
// 方法 1: 直接索引
Tile tile = Main.tile[x, y];

// 方法 2: 安全访问（推荐）
Tile tile = Framing.GetTileSafely(x, y);

// 方法 3: 使用 Tilemap（现代 API）
Tile tile = Main.tile[x, y];  // 返回 Tile 结构体
```

### 4.2 检查坐标有效性

```csharp
// 检查坐标是否在世界范围内
if (WorldGen.InWorld(x, y))
{
    Tile tile = Main.tile[x, y];
    // 安全操作
}

// 也可以检查边界
if (x >= 0 && x < Main.maxTilesX && y >= 0 && y < Main.maxTilesY)
{
    // 坐标有效
}
```

### 4.3 搜索 Tile

```csharp
// 搜索特定类型的 Tile
public List<Point> FindTiles(int tileType, int startX, int startY, int radius)
{
    var results = new List<Point>();
    
    for (int x = startX - radius; x <= startX + radius; x++)
    {
        for (int y = startY - radius; y <= startY + radius; y++)
        {
            if (WorldGen.InWorld(x, y))
            {
                Tile tile = Main.tile[x, y];
                if (tile.HasTile && tile.TileType == tileType)
                {
                    results.Add(new Point(x, y));
                }
            }
        }
    }
    
    return results;
}
```

### 4.4 遍历世界区域

```csharp
// 遍历矩形区域
public void ProcessArea(Rectangle area)
{
    for (int x = area.X; x < area.X + area.Width; x++)
    {
        for (int y = area.Y; y < area.Y + area.Height; y++)
        {
            if (WorldGen.InWorld(x, y))
            {
                Tile tile = Main.tile[x, y];
                
                // 处理每个 Tile
                if (tile.HasTile)
                {
                    // 有 Tile
                }
                else
                {
                    // 空气
                }
            }
        }
    }
}
```

### 4.5 获取 Tile 实体

```csharp
// 对于有 TileEntity 的 Tile（如箱子、逻辑电路）
TileEntity entity = TileEntity.ByPosition[new Point16(x, y)];

// 检查 Tile 是否有 TileEntity
if (TileEntity.ByPosition.TryGetValue(new Point16(x, y), out TileEntity te))
{
    // 找到 TileEntity
}
```

---

## 5. 多 Tile 结构处理

### 5.1 TileObjectData 概述

多 Tile 结构（家具、大型装饰等）使用 `TileObjectData` 来定义。这是处理多 Tile 物体的核心系统。

### 5.2 基本配置

```csharp
public class MyFurnitureTile : ModTile
{
    public override void SetStaticDefaults()
    {
        // 基本属性
        Main.tileSolid[Type] = false;           // 不是实心方块
        Main.tileLavaDeath[Type] = true;        // 会被岩浆破坏
        Main.tileFrameImportant[Type] = true;   // 帧重要（用于家具）
        
        // 定义多 Tile 结构
        TileObjectData.newTile.Width = 2;       // 宽度（以 Tile 为单位）
        TileObjectData.newTile.Height = 2;      // 高度（以 Tile 为单位）
        
        // 坐标尺寸（像素）
        TileObjectData.newTile.CoordinateWidth = 16;
        TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16 };
        
        // 锚点设置
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 2, 0);
        
        // 注册 TileObjectData
        TileObjectData.addTile(Type);
    }
}
```

### 5.3 常见家具配置示例

#### 椅子 (1x2)

```csharp
public class MyChair : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = true;
        
        TileObjectData.newTile.Width = 1;
        TileObjectData.newTile.Height = 2;
        TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };
        TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeftRight;
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.StyleMultiplier = 2;
        TileObjectData.newTile.StyleLineSkip = 2;
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 0);
        
        TileObjectData.addTile(Type);
    }
}
```

#### 桌子 (2x2)

```csharp
public class MyTable : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileTable[Type] = true;  // 可以放置物品在上面
        
        TileObjectData.newTile.Width = 2;
        TileObjectData.newTile.Height = 2;
        TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16 };
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 2, 0);
        
        TileObjectData.addTile(Type);
    }
}
```

#### 床 (4x2)

```csharp
public class MyBed : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileLavaDeath[Type] = true;
        
        TileObjectData.newTile.Width = 4;
        TileObjectData.newTile.Height = 2;
        TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16 };
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 4, 0);
        
        TileObjectData.addTile(Type);
    }
}
```

#### 门 (1x3)

```csharp
public class MyDoor : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileBlockLight[Type] = true;
        Main.tileSolid[Type] = true;
        Main.tileNoAttach[Type] = true;
        
        TileObjectData.newTile.Width = 1;
        TileObjectData.newTile.Height = 3;
        TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 16 };
        TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 0);
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 0);
        
        TileObjectData.addTile(Type);
    }
}
```

### 5.4 TileObjectData 属性详解

```csharp
// 尺寸属性
TileObjectData.newTile.Width = 2;              // 宽度（Tile 数）
TileObjectData.newTile.Height = 2;             // 高度（Tile 数）
TileObjectData.newTile.CoordinateWidth = 16;   // 每格宽度（像素）
TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16 };  // 每行高度

// 锚点属性
TileObjectData.newTile.AnchorBottom;           // 底部锚点
TileObjectData.newTile.AnchorTop;              // 顶部锚点
TileObjectData.newTile.AnchorLeft;            // 左侧锚点
TileObjectData.newTile.AnchorRight;           // 右侧锚点

// 方向属性
TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeftRight;
TileObjectData.newTile.StyleHorizontal = true;  // 样式水平排列

// 行为属性
TileObjectData.newTile.usesCanFall = true;     // 可以下落
TileObjectData.newTile.OnlyPlaceOnSand = true;  // 只能放在沙子上
```

### 5.5 AnchorType 类型

```csharp
// 常用锚点类型
AnchorType.SolidTile       // 实心方块
AnchorType.SolidWithTop    // 顶部实心的方块（如平台）
AnchorType.Platform        // 平台
AnchorType.Table           // 桌子
AnchorType.Wall            // 墙壁
AnchorType.EmptySpace      // 空空间

// 锚点数据构造
// new AnchorData(锚点类型, 连接宽度, 偏移量)
new AnchorData(AnchorType.SolidTile, 2, 0);
```

### 5.6 获取多 Tile 结构信息

```csharp
// 检查 Tile 是否属于多 Tile 结构
Tile tile = Main.tile[x, y];
if (Main.tileFrameImportant[tile.TileType])
{
    // 这是多 Tile 结构的一部分
    
    // 获取 TileObjectData
    TileObjectData data = TileObjectData.GetTileData(tile.TileType, 0);
    
    if (data != null)
    {
        int width = data.Width;
        int height = data.Height;
        
        // 计算左上角坐标
        int originX = x - (tile.TileFrameX / 18) % width;
        int originY = y - (tile.TileFrameY / 18) % height;
    }
}
```

---

## 6. 多人游戏同步

### 6.1 发送 Tile 更新

```csharp
// 在多人游戏中修改 Tile 后必须同步
public void PlaceTileMultiplayer(int x, int y, int type)
{
    if (Main.netMode == NetmodeID.MultiplayerClient)
    {
        // 客户端发送请求给服务器
        NetMessage.SendTileSquare(-1, x, y, 1);
    }
    else
    {
        // 服务器直接放置
        WorldGen.PlaceTile(x, y, type);
        NetMessage.SendTileSquare(-1, x, y, 1);
    }
}

// 同步更大区域
NetMessage.SendTileSquare(-1, x, y, 3);  // 同步 3x3 区域
```

### 6.2 参数说明

```csharp
// SendTileSquare 参数
NetMessage.SendTileSquare(
    int whoAmI,       // 发送者 ID（-1 = 广播给所有玩家）
    int tileX,        // 中心 X 坐标
    int tileY,        // 中心 Y 坐标
    int squareSize    // 区域大小
);
```

### 6.3 完整示例

```csharp
public void ModifyTileArea(int centerX, int centerY, int radius)
{
    // 检查是否在服务器端
    if (Main.netMode == NetmodeID.MultiplayerClient)
    {
        return; // 客户端不应该直接修改世界
    }
    
    // 修改 Tile
    for (int x = centerX - radius; x <= centerX + radius; x++)
    {
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            if (WorldGen.InWorld(x, y))
            {
                WorldGen.PlaceTile(x, y, TileID.Dirt);
            }
        }
    }
    
    // 同步给所有客户端
    NetMessage.SendTileSquare(-1, centerX, centerY, radius * 2 + 1);
}
```

---

## 附录：常用 API 参考

### WorldGen 类

| 方法 | 描述 |
|------|------|
| `PlaceTile(x, y, type)` | 放置单个 Tile |
| `PlaceObject(x, y, type)` | 放置多 Tile 物体 |
| `KillTile(x, y)` | 移除 Tile |
| `SquareTileFrame(x, y)` | 更新 Tile 帧 |
| `InWorld(x, y)` | 检查坐标是否有效 |
| `TileRunner(x, y, strength, steps, type)` | 生成矿脉 |

### Tile 结构体属性

| 属性 | 类型 | 描述 |
|------|------|------|
| `HasTile` | bool | 是否有 Tile |
| `TileType` | ushort | Tile 类型 ID |
| `TileFrameX` | short | X 帧位置 |
| `TileFrameY` | short | Y 帧位置 |
| `LiquidAmount` | byte | 液体量 |
| `LiquidType` | int | 液体类型 |
| `WallType` | ushort | 墙壁类型 |
| `TileColor` | byte | 油漆颜色 |
| `IsHalfBlock` | bool | 是否半砖 |
| `Slope` | int | 斜坡类型 |

### Main 类数组

| 数组 | 描述 |
|------|------|
| `Main.tile[x, y]` | Tile 数组 |
| `Main.tileSolid[type]` | 是否实心 |
| `Main.tileMergeDirt[type]` | 是否合并到泥土 |
| `Main.tileLighted[type]` | 是否发光 |
| `Main.tileNoAttach[type]` | 是否不能附着 |
| `Main.tileBlockLight[type]` | 是否阻挡光线 |

---

## 参考资料

- [tModLoader GitHub Wiki](https://github.com/tModLoader/tModLoader/wiki)
- [Terraria Wiki - Tile IDs](https://terraria.wiki.gg/wiki/Tile_IDs)
- [tModLoader ExampleMod](https://github.com/tModLoader/tModLoader/tree/stable/ExampleMod)
- [tModLoader 论坛](https://forums.terraria.org/index.php?forums/tmodloader.106/)

---

*文档整理日期：2026/05/31*