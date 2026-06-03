using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using trab.Core.API;
using trab.Data;

namespace trab.Core.Building
{
    /// <summary>
    /// 建筑执行器 - 解析设计并在游戏中生成建筑
    /// </summary>
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
                        int wt = w.WallId > 0 ? w.WallId : GetWall(w.WallType);
                        if (wt > 0)
                        {
                            WorldGen.PlaceWall(wx, wy, (ushort)wt);
                            if (w.GetPaintId() > 0)
                                SetWallPaint(wx, wy, w.GetPaintId());
                        }
                    }
                }

                // 墙壁范围
                foreach (var wr in d.WallRanges)
                {
                    int wallType = wr.WallId > 0 ? wr.WallId : GetWall(wr.WallType);
                    if (wallType > 0)
                    {
                        for (int x = startX + wr.X1; x <= startX + wr.X2; x++)
                        {
                            for (int y = startY + wr.Y1; y <= startY + wr.Y2; y++)
                            {
                                if (WorldGen.InWorld(x, y))
                                {
                                    WorldGen.PlaceWall(x, y, (ushort)wallType);
                                    if (wr.Paint > 0)
                                        SetWallPaint(x, y, wr.Paint);
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
                        int tt = t.TileId > 0 ? t.TileId : GetTile(t.TileType);
                        if (tt > 0)
                        {
                            WorldGen.PlaceTile(tx, ty, (ushort)tt);

                            if (t.Slope > 0 && t.Slope <= 5)
                                SetTileSlope(tx, ty, t.Slope);

                            if (t.GetPaintId() > 0)
                                SetTilePaint(tx, ty, t.GetPaintId());
                        }
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

                        int doorType = dr.TileId > 0 ? dr.TileId : TileID.ClosedDoor;
                        WorldGen.PlaceDoor(dx, dy, (ushort)doorType);
                    }
                }

                // 家具
                foreach (var f in d.Furniture)
                {
                    int fx = startX + f.X, fy = startY + f.Y;
                    if (WorldGen.InWorld(fx, fy))
                    {
                        int ft = f.TileId > 0 ? f.TileId : GetFurniture(f.FurnitureType);
                        if (ft > 0)
                        {
                            WorldGen.PlaceObject(fx, fy, (ushort)ft);
                            if (f.Paint > 0)
                                SetTilePaint(fx, fy, f.Paint);
                        }
                    }
                }

                // 光源
                foreach (var l in d.LightSources)
                {
                    int lx = startX + l.X, ly = startY + l.Y;
                    if (WorldGen.InWorld(lx, ly))
                    {
                        int lt = l.TileId > 0 ? l.TileId : TileID.Torches;
                        WorldGen.PlaceTile(lx, ly, (ushort)lt);
                    }
                }

                // 刷新
                for (int x = startX; x < startX + d.Width; x++)
                    for (int y = startY; y < startY + d.Height; y++)
                        if (WorldGen.InWorld(x, y))
                            WorldGen.SquareTileFrame(x, y);

                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendTileSquare(-1, startX, startY, d.Width, d.Height);

                Main.NewText("建筑 '" + d.Name + "' 完成! 方块:" + d.Tiles.Count + " 家具:" + d.Furniture.Count, Color.Green);
                return true;
            }
            catch (Exception ex)
            {
                Main.NewText("错误: " + ex.Message, Color.Red);
                return false;
            }
        }

        private void SetTileSlope(int x, int y, int slope)
        {
            if (!WorldGen.InWorld(x, y)) return;

            SlopeType slopeType = (SlopeType)slope;

            var tile = Main.tile[x, y];
            if (tile.HasTile)
            {
                Tile.SmoothSlope(x, y, false);
                if (slope > 0)
                {
                    WorldGen.SlopeTile(x, y, (int)slopeType);
                }
            }
        }

        private void SetTilePaint(int x, int y, int paintId)
        {
            if (!WorldGen.InWorld(x, y) || paintId <= 0 || paintId > 31) return;

            var tile = Main.tile[x, y];
            if (tile.HasTile)
            {
                tile.TileColor = (byte)paintId;
                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendTileSquare(-1, x, y, 1, 1);
            }
        }

        private void SetWallPaint(int x, int y, int paintId)
        {
            if (!WorldGen.InWorld(x, y) || paintId <= 0 || paintId > 31) return;

            var tile = Main.tile[x, y];
            if (tile.WallType > 0)
            {
                tile.WallColor = (byte)paintId;
                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendTileSquare(-1, x, y, 1, 1);
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
                {"Brick", 38},
                {"WorkBench", 17},
                {"BorealWood", 250},
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
                {"SnowWall", 64},
                {"BrickWall", 41},
                {"BorealWoodWall", 216},
            };
        }

        private Dictionary<string, int> InitFurniture()
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                {"WorkBench", 17},
                {"Table", TileID.Tables},
                {"Chair", TileID.Chairs},
                {"Bed", TileID.Beds},
                {"Chest", TileID.Containers},
                {"Furnace", TileID.Furnaces},
                {"Anvil", TileID.Anvils},
                {"Piano", TileID.Pianos},
                {"Dresser", TileID.Dressers},
                {"Bathtub", TileID.Bathtubs},
                {"Torch", TileID.Torches},
            };
        }
    }
}