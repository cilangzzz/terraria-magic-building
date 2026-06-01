using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using trab.Data;

namespace trab.Core
{
    /// <summary>
    /// 增强版建筑执行器
    /// </summary>
    public class EnhancedBuildingExecutor : BuildingExecutor
    {
        public EnhancedBuildingExecutor(Mod mod) : base(mod) { }

        /// <summary>
        /// 增强版建筑生成
        /// </summary>
        public bool BuildAtLocationEnhanced(BuildingDesign d, int startX, int startY, Player p = null)
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
                Main.NewText("开始生成: " + d.Name + " (" + d.Width + "x" + d.Height + ")", Color.Yellow);

                // 使用基础执行器
                return BuildAtLocation(d, startX, startY, p);
            }
            catch (Exception ex)
            {
                Main.NewText("错误: " + ex.Message, Color.Red);
                return false;
            }
        }

        /// <summary>
        /// 验证NPC房屋
        /// </summary>
        public NpcSuitability ValidateNpcHouse(int startX, int startY, int width, int height)
        {
            var suitability = new NpcSuitability();
            bool hasLight = false;
            bool hasTable = false;
            bool hasChair = false;
            bool hasDoor = false;
            int tileCount = 0;

            for (int x = startX; x < startX + width; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    if (!WorldGen.InWorld(x, y)) continue;

                    var tile = Main.tile[x, y];

                    if (!tile.HasTile && tile.WallType > 0)
                        tileCount++;

                    if (tile.HasTile)
                    {
                        if (tile.TileType == TileID.Torches) hasLight = true;
                        if (tile.TileType == TileID.Tables || tile.TileType == TileID.WorkBenches) hasTable = true;
                        if (tile.TileType == TileID.Chairs) hasChair = true;
                        if (tile.TileType == TileID.ClosedDoor) hasDoor = true;
                    }
                }
            }

            suitability.TileCount = tileCount;
            suitability.HasLight = hasLight;
            suitability.HasFlatSurface = hasTable;
            suitability.HasComfort = hasChair;
            suitability.HasDoor = hasDoor;
            suitability.IsValidHouse = tileCount >= 60 && hasLight && hasTable && hasChair && hasDoor;

            if (!hasLight) suitability.MissingRequirements.Add("光源");
            if (!hasTable) suitability.MissingRequirements.Add("平坦表面");
            if (!hasChair) suitability.MissingRequirements.Add("舒适物品");
            if (!hasDoor) suitability.MissingRequirements.Add("门");

            return suitability;
        }

        /// <summary>
        /// 获取建筑预览信息
        /// </summary>
        public string GetBuildingPreview(BuildingDesign design)
        {
            var sb = new StringBuilder();
            sb.AppendLine("建筑名称: " + design.Name);
            sb.AppendLine("尺寸: " + design.Width + "x" + design.Height);
            sb.AppendLine("风格: " + design.Style);
            sb.AppendLine("方块数: " + design.Tiles.Count);
            sb.AppendLine("家具数: " + design.Furniture.Count);
            return sb.ToString();
        }
    }
}