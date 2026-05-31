using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using trab.Data;

namespace trab.Core
{
    public class BuildingExecutor
    {
        private readonly Mod modInst;
        private readonly Dictionary<string, int> tiles;
        private readonly Dictionary<string, int> walls;
        private readonly Dictionary<string, int> furniture;

        public BuildingExecutor(Mod mod)
        {
            modInst = mod;
            tiles = InitTiles();
            walls = InitWalls();
            furniture = InitFurniture();
        }

        public BuildingDesign ParseDesign(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                string ext = AIApiService.ExtractJsonFromResponse(json) ?? json;
                return JsonConvert.DeserializeObject<BuildingDesign>(ext);
            }
            catch (Exception ex)
            {
                modInst.Logger.Error("Parse: " + ex.Message);
                return null;
            }
        }

        public bool BuildAtLocation(BuildingDesign d, int startX, int startY, Player p = null)
        {
            if (d == null || d.Width <= 0 || d.Height <= 0)
            {
                Main.NewText("无效设计", Color.Red);
                return false;
            }

            if (!WorldGen.InWorld(startX, startY, 10) ||
                !WorldGen.InWorld(startX + d.Width, startY + d.Height, 10))
            {
                Main.NewText("超出边界", Color.Red);
                return false;
            }

            try
            {
                // 清空
                for (int x = startX; x < startX + d.Width; x++)
                    for (int y = startY; y < startY + d.Height; y++)
                        if (WorldGen.InWorld(x, y))
                            Main.tile[x, y].ClearEverything();

                // 墙壁
                foreach (var w in d.Walls)
                {
                    int wx = startX + w.X, wy = startY + w.Y;
                    if (WorldGen.InWorld(wx, wy))
                    {
                        int wt = GetWall(w.WallType);
                        if (wt > 0) WorldGen.PlaceWall(wx, wy, (ushort)wt);
                    }
                }

                // 墙壁范围 - 批量填充墙壁
                foreach (var wr in d.WallRanges)
                {
                    int wallType = GetWall(wr.WallType);
                    if (wallType > 0)
                    {
                        for (int x = startX + wr.X1; x <= startX + wr.X2; x++)
                        {
                            for (int y = startY + wr.Y1; y <= startY + wr.Y2; y++)
                            {
                                if (WorldGen.InWorld(x, y))
                                {
                                    WorldGen.PlaceWall(x, y, (ushort)wallType);
                                }
                            }
                        }
                    }
                }

                // 方块
                foreach (var t in d.Tiles)
                {
                    int tx = startX + t.X, ty = startY + t.Y;
                    if (WorldGen.InWorld(tx, ty))
                    {
                        int tt = GetTile(t.TileType);
                        if (tt > 0) WorldGen.PlaceTile(tx, ty, (ushort)tt);
                    }
                }

                // 门
                foreach (var dr in d.Doors)
                {
                    int dx = startX + dr.X, dy = startY + dr.Y;
                    if (WorldGen.InWorld(dx, dy + 2))
                    {
                        for (int y = dy; y < dy + 3; y++)
                            if (WorldGen.InWorld(dx, y))
                                Main.tile[dx, y].ClearTile();
                        WorldGen.PlaceDoor(dx, dy, TileID.ClosedDoor);
                    }
                }

                // 家具
                foreach (var f in d.Furniture)
                {
                    int fx = startX + f.X, fy = startY + f.Y;
                    if (WorldGen.InWorld(fx, fy))
                    {
                        int ft = GetFurniture(f.FurnitureType);
                        if (ft > 0) WorldGen.PlaceObject(fx, fy, (ushort)ft);
                    }
                }

                // 光源
                foreach (var l in d.LightSources)
                {
                    int lx = startX + l.X, ly = startY + l.Y;
                    if (WorldGen.InWorld(lx, ly))
                        WorldGen.PlaceTile(lx, ly, TileID.Torches);
                }

                // 刷新
                for (int x = startX; x < startX + d.Width; x++)
                    for (int y = startY; y < startY + d.Height; y++)
                        if (WorldGen.InWorld(x, y))
                            WorldGen.SquareTileFrame(x, y);

                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendTileSquare(-1, startX, startY, d.Width, d.Height);

                Main.NewText("建筑 '" + d.Name + "' 完成!", Color.Green);
                return true;
            }
            catch (Exception ex)
            {
                Main.NewText("错误: " + ex.Message, Color.Red);
                return false;
            }
        }

        private int GetTile(string n)
        {
            if (tiles.TryGetValue(n, out int i)) return i;
            if (int.TryParse(n, out int v)) return v;
            return -1;
        }

        private int GetWall(string n)
        {
            if (walls.TryGetValue(n, out int i)) return i;
            if (int.TryParse(n, out int v)) return v;
            return -1;
        }

        private int GetFurniture(string n)
        {
            if (furniture.TryGetValue(n, out int i)) return i;
            if (int.TryParse(n, out int v)) return v;
            return -1;
        }

        private Dictionary<string, int> InitTiles()
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                {"Stone", TileID.Stone},
                {"Dirt", TileID.Dirt},
                {"Wood", TileID.WoodBlock},
                {"Grass", TileID.Grass},
                {"GrayBrick", TileID.GrayBrick},
                {"GoldBrick", TileID.GoldBrick},
                {"SilverBrick", TileID.SilverBrick},
                {"CopperBrick", TileID.CopperBrick},
                {"IronBrick", TileID.IronBrick},
                {"StoneSlab", TileID.StoneSlab},
                {"Sandstone", TileID.Sandstone},
                {"Glass", TileID.Glass},
                {"SnowBlock", TileID.SnowBlock},
                {"Marble", TileID.MarbleBlock},
                {"Granite", TileID.GraniteBlock},
                {"Obsidian", TileID.Obsidian},
                {"Platform", TileID.Platforms},
                // 使用数值ID避免API变更
                {"Brick", 38}, // Red Brick
                {"WorkBench", 17}, // Work Bench
            };
        }

        private Dictionary<string, int> InitWalls()
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                {"StoneWall", WallID.Stone},
                {"DirtWall", WallID.Dirt},
                {"WoodWall", WallID.Wood},
                {"GrayBrickWall", WallID.GrayBrick},
                {"GoldBrickWall", WallID.GoldBrick},
                {"SilverBrickWall", WallID.SilverBrick},
                {"CopperBrickWall", WallID.CopperBrick},
                {"IronBrickWall", WallID.IronBrick},
                {"StoneSlabWall", WallID.StoneSlab},
                {"SandstoneWall", WallID.Sandstone},
                {"GlassWall", WallID.Glass},
                {"MarbleWall", WallID.MarbleBlock},
                {"GraniteWall", WallID.GraniteBlock},
                // 使用数值ID
                {"SnowWall", 64}, // Snow Wall
                {"BrickWall", 41}, // Red Brick Wall
            };
        }

        private Dictionary<string, int> InitFurniture()
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                {"WorkBench", 17}, // Work Bench tile
                {"Table", TileID.Tables},
                {"Chair", TileID.Chairs},
                {"Bed", TileID.Beds},
                {"Chest", TileID.Containers},
                {"Furnace", TileID.Furnaces},
                {"Anvil", TileID.Anvils},
                {"Piano", TileID.Pianos},
                {"Dresser", TileID.Dressers},
                {"Bathtub", TileID.Bathtubs},
            };
        }
    }
}