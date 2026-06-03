using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using trab.Core.API;
using trab.Core.Building;
using trab.Data;
using trab.Config;
using trab.Players;

namespace trab.Commands
{
    /// <summary>
    /// AI建筑生成聊天命令
    /// </summary>
    public class AIBuildCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "aibuild";

        public override string Usage => "/aibuild <建筑描述> - 使用AI生成建筑\n" +
                                         "/aibuild help - 显示帮助信息\n" +
                                         "/aibuild list - 显示可用材料列表\n" +
                                         "/aibuild config - 显示当前配置\n" +
                                         "/aibuild stop - 停止当前生成";

        public override string Description => "使用AI生成建筑结构";

        private CancellationTokenSource _currentRequest;

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0)
            {
                caller.Reply("请输入建筑描述，例如: /aibuild 一座中世纪风格的木屋", Color.Red);
                return;
            }

            string subCommand = args[0].ToLower();

            switch (subCommand)
            {
                case "help":
                    ShowHelp(caller);
                    return;

                case "list":
                    ShowMaterialList(caller);
                    return;

                case "config":
                    ShowConfig(caller);
                    return;

                case "stop":
                    StopGeneration(caller);
                    return;

                default:
                    // 将所有参数合并为建筑描述
                    string prompt = string.Join(" ", args);
                    RequestBuilding(caller, prompt);
                    return;
            }
        }

        /// <summary>
        /// 显示帮助信息
        /// </summary>
        private void ShowHelp(CommandCaller caller)
        {
            caller.Reply("=== AI建筑生成帮助 ===", Color.Cyan);
            caller.Reply("用法: /aibuild <建筑描述>", Color.White);
            caller.Reply("", Color.White);
            caller.Reply("建筑描述示例:", Color.Yellow);
            caller.Reply("  - 一座简单的木屋", Color.White);
            caller.Reply("  - 中世纪风格的石头城堡", Color.White);
            caller.Reply("  - 带有地下室的金砖别墅", Color.White);
            caller.Reply("  - 雪地风格的冰屋", Color.White);
            caller.Reply("", Color.White);
            caller.Reply("提示: 描述越详细，生成的建筑越符合你的期望", Color.Gray);
            caller.Reply("", Color.White);
            caller.Reply("其他命令:", Color.Yellow);
            caller.Reply("  /aibuild list - 显示可用材料", Color.White);
            caller.Reply("  /aibuild config - 显示当前配置", Color.White);
            caller.Reply("  /aibuild stop - 停止当前生成", Color.White);
        }

        /// <summary>
        /// 显示可用材料列表
        /// </summary>
        private void ShowMaterialList(CommandCaller caller)
        {
            caller.Reply("=== 可用材料列表 ===", Color.Cyan);
            caller.Reply("", Color.White);

            caller.Reply("方块类型:", Color.Yellow);
            caller.Reply("  Stone, Dirt, Wood, Brick, GrayBrick, GoldBrick", Color.White);
            caller.Reply("  StoneSlab, Glass, SnowBlock, Sandstone, Marble, Granite", Color.White);
            caller.Reply("", Color.White);

            caller.Reply("墙壁类型:", Color.Yellow);
            caller.Reply("  StoneWall, DirtWall, WoodWall, BrickWall, GlassWall", Color.White);
            caller.Reply("", Color.White);

            caller.Reply("家具类型:", Color.Yellow);
            caller.Reply("  WorkBench, Table, Chair, Bed, Chest, Furnace, Anvil", Color.White);
            caller.Reply("  Bookshelf, Piano, Dresser, Sofa, Bathtub", Color.White);
            caller.Reply("", Color.White);

            caller.Reply("光源类型:", Color.Yellow);
            caller.Reply("  Torch, Candle, Chandelier, Lantern, Lamp", Color.White);
            caller.Reply("", Color.White);

            caller.Reply("门类型:", Color.Yellow);
            caller.Reply("  WoodenDoor, IronDoor, GlassDoor", Color.White);
        }

        /// <summary>
        /// 显示当前配置
        /// </summary>
        private void ShowConfig(CommandCaller caller)
        {
            var config = ModContent.GetInstance<AIBuildingConfig>();

            caller.Reply("=== 当前配置 ===", Color.Cyan);
            caller.Reply($"API服务商: {config.ServiceProvider}", Color.White);
            caller.Reply($"API密钥: {(string.IsNullOrEmpty(config.ApiKey) ? "未设置" : "已设置")}", Color.White);
            caller.Reply($"模型: {config.ModelName}", Color.White);
            caller.Reply($"生成偏移: X={config.BuildOffsetX}, Y={config.BuildOffsetY}", Color.White);
            caller.Reply($"最大尺寸: {config.MaxBuildingSize}格", Color.White);
        }

        /// <summary>
        /// 停止当前生成
        /// </summary>
        private void StopGeneration(CommandCaller caller)
        {
            if (_currentRequest != null && !_currentRequest.IsCancellationRequested)
            {
                _currentRequest.Cancel();
                caller.Reply("已停止当前AI生成请求", Color.Yellow);
            }
            else
            {
                caller.Reply("当前没有正在进行的生成请求", Color.Gray);
            }
        }

        /// <summary>
        /// 请求AI生成建筑
        /// </summary>
        private void RequestBuilding(CommandCaller caller, string prompt)
        {
            var config = ModContent.GetInstance<AIBuildingConfig>();

            // 检查API密钥
            if (string.IsNullOrEmpty(config.ApiKey))
            {
                caller.Reply("请先在模组配置中设置API密钥!", Color.Red);
                caller.Reply("按ESC -> 模组配置 -> AI Building Config", Color.Yellow);
                return;
            }

            // 验证输入
            if (string.IsNullOrWhiteSpace(prompt))
            {
                caller.Reply("建筑描述不能为空", Color.Red);
                return;
            }

            if (prompt.Length > 500)
            {
                caller.Reply("建筑描述过长，请简化描述（最多500字符）", Color.Red);
                return;
            }

            // 取消之前的请求
            _currentRequest?.Cancel();
            _currentRequest = new CancellationTokenSource();

            caller.Reply($"正在请求AI生成建筑: {prompt}", Color.Yellow);
            caller.Reply("请稍候...", Color.Gray);

            // 获取玩家ModPlayer
            var player = caller.Player.GetModPlayer<AIBuildingPlayer>();

            // 异步请求
            Task.Run(async () =>
            {
                try
                {
                    var service = new AIApiService(config.ApiKey, config.ServiceProvider, config.CustomEndpoint, config.ModelName);
                    var response = await service.SendChatRequestAsync(prompt, _currentRequest.Token);

                    if (_currentRequest.Token.IsCancellationRequested)
                    {
                        Main.QueueMainThreadAction(() =>
                        {
                            caller.Reply("生成请求已取消", Color.Yellow);
                        });
                        return;
                    }

                    if (response != null)
                    {
                        Main.QueueMainThreadAction(() =>
                        {
                            ProcessResponse(caller, response, prompt);
                        });
                    }
                    else
                    {
                        Main.QueueMainThreadAction(() =>
                        {
                            caller.Reply("AI返回了空响应，请重试", Color.Red);
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        caller.Reply("生成请求已取消", Color.Yellow);
                    });
                }
                catch (Exception ex)
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        caller.Reply($"生成失败: {ex.Message}", Color.Red);
                        Mod.Logger.Error($"AI生成失败: {ex}");
                    });
                }
            });
        }

        /// <summary>
        /// 处理AI响应
        /// </summary>
        private void ProcessResponse(CommandCaller caller, string response, string prompt)
        {
            var config = ModContent.GetInstance<AIBuildingConfig>();
            var executor = new BuildingExecutor(Mod);

            // 提取JSON
            string json = AIApiService.ExtractJsonFromResponse(response);
            if (json == null)
            {
                caller.Reply("无法从AI响应中提取建筑数据", Color.Red);
                caller.Reply($"AI响应: {response.Substring(0, Math.Min(200, response.Length))}...", Color.Gray);
                return;
            }

            // 尝试解析TEditSch格式
            var design = executor.ParseTEditSchDesign(json);
            if (design == null)
            {
                // 回退到旧格式
                var oldDesign = executor.ParseDesign(json);
                if (oldDesign == null)
                {
                    caller.Reply("建筑数据解析失败", Color.Red);
                    return;
                }

                // 转换为TEditSch格式
                design = ConvertToTEditSch(oldDesign);
            }

            // 显示建筑信息
            caller.Reply($"=== 建筑设计 (TEditSch) ===", Color.Cyan);
            caller.Reply($"名称: {design.name}", Color.Green);
            caller.Reply($"尺寸: {design.width}x{design.height}", Color.White);
            caller.Reply($"活跃方块: {design.stats?.active_tiles ?? 0}", Color.White);
            caller.Reply($"墙壁数: {design.stats?.tiles_with_wall ?? 0}", Color.White);

            // 检查尺寸限制
            if (design.width > config.MaxBuildingSize || design.height > config.MaxBuildingSize)
            {
                caller.Reply($"建筑尺寸超出限制（最大{config.MaxBuildingSize}格）", Color.Red);
                return;
            }

            // 在玩家位置附近生成建筑
            int startX = (int)(caller.Player.position.X / 16) + config.BuildOffsetX;
            int startY = (int)(caller.Player.position.Y / 16) + config.BuildOffsetY;

            // 调整Y坐标，使建筑底部对齐玩家位置
            startY = startY - design.height / 2;

            caller.Reply($"准备在位置 ({startX}, {startY}) 生成建筑...", Color.Yellow);

            // 执行建筑生成
            bool success = executor.BuildTEditSch(design, startX, startY, caller.Player);

            if (success)
            {
                caller.Reply($"建筑 '{design.name}' 已成功生成!", Color.Green);
            }
        }

        /// <summary>
        /// 将旧格式转换为TEditSch格式
        /// </summary>
        private TEditSchDesign ConvertToTEditSch(BuildingDesign old)
        {
            var design = new TEditSchDesign
            {
                name = old.Name,
                width = old.Width,
                height = old.Height
            };

            // 初始化空网格
            for (int y = 0; y < old.Height; y++)
            {
                var row = new List<TEditTile>();
                for (int x = 0; x < old.Width; x++)
                    row.Add(new TEditTile());
                design.tiles.Add(row);
            }

            // 放置墙壁范围
            foreach (var range in old.WallRanges)
            {
                for (int y = range.Y1; y <= range.Y2; y++)
                {
                    for (int x = range.X1; x <= range.X2; x++)
                    {
                        if (y >= 0 && y < old.Height && x >= 0 && x < old.Width)
                        {
                            design.tiles[y][x].wall = range.WallId;
                        }
                    }
                }
            }

            // 放置方块
            foreach (var tile in old.Tiles)
            {
                if (tile.Y >= 0 && tile.Y < old.Height && tile.X >= 0 && tile.X < old.Width)
                {
                    design.tiles[tile.Y][tile.X] = new TEditTile
                    {
                        active = true,
                        type = tile.TileId,
                        wall = design.tiles[tile.Y][tile.X].wall
                    };
                }
            }

            // 放置家具、门、光源
            foreach (var f in old.Furniture)
            {
                if (f.Y >= 0 && f.Y < old.Height && f.X >= 0 && f.X < old.Width)
                {
                    design.tiles[f.Y][f.X] = new TEditTile { active = true, type = f.TileId };
                }
            }

            foreach (var d in old.Doors)
            {
                if (d.Y >= 0 && d.Y < old.Height && d.X >= 0 && d.X < old.Width)
                {
                    design.tiles[d.Y][d.X] = new TEditTile { active = true, type = d.TileId };
                }
            }

            foreach (var l in old.LightSources)
            {
                if (l.Y >= 0 && l.Y < old.Height && l.X >= 0 && l.X < old.Width)
                {
                    design.tiles[l.Y][l.X] = new TEditTile { active = true, type = l.TileId };
                }
            }

            design.CalculateStats();
            return design;
        }
    }

    /// <summary>
    /// 快速生成预设建筑命令
    /// </summary>
    public class QuickBuildCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "quickbuild";

        public override string Usage => "/quickbuild <预设名称> - 快速生成预设建筑\n" +
                                         "/quickbuild list - 显示预设列表";

        public override string Description => "快速生成预设建筑";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0)
            {
                caller.Reply("请指定预设名称，例如: /quickbuild house", Color.Red);
                return;
            }

            string subCommand = args[0].ToLower();

            if (subCommand == "list")
            {
                caller.Reply("=== 预设建筑列表 ===", Color.Cyan);
                caller.Reply("  house - 简单木屋", Color.White);
                caller.Reply("  castle - 小型城堡", Color.White);
                caller.Reply("  tower - 观察塔", Color.White);
                caller.Reply("  cave - 地下室", Color.White);
                caller.Reply("  shop - 商店建筑", Color.White);
                return;
            }

            // 生成预设建筑
            var preset = GetPresetDesign(subCommand);
            if (preset == null)
            {
                caller.Reply($"未找到预设: {subCommand}", Color.Red);
                caller.Reply("使用 /quickbuild list 查看所有预设", Color.Yellow);
                return;
            }

            var executor = new BuildingExecutor(Mod);
            int startX = (int)(caller.Player.position.X / 16) + 5;
            int startY = (int)(caller.Player.position.Y / 16) - preset.Height / 2;

            caller.Reply($"正在生成预设建筑: {preset.Name}", Color.Yellow);
            bool success = executor.BuildAtLocation(preset, startX, startY, caller.Player);

            if (success)
            {
                caller.Reply($"预设建筑 '{preset.Name}' 已生成!", Color.Green);
            }
        }

        /// <summary>
        /// 获取预设建筑设计
        /// </summary>
        private BuildingDesign GetPresetDesign(string name)
        {
            switch (name)
            {
                case "house":
                    return CreateSimpleHouse();

                case "castle":
                    return CreateSmallCastle();

                case "tower":
                    return CreateTower();

                case "cave":
                    return CreateCaveRoom();

                case "shop":
                    return CreateShop();

                default:
                    return null;
            }
        }

        /// <summary>
        /// 创建简单木屋预设
        /// </summary>
        private BuildingDesign CreateSimpleHouse()
        {
            var design = new BuildingDesign
            {
                Name = "简单木屋",
                Description = "一个基础的NPC房屋",
                Width = 10,
                Height = 8
            };

            // 地板
            for (int x = 0; x < design.Width; x++)
            {
                design.Tiles.Add(new TileData { X = x, Y = design.Height - 1, TileType = "Wood" });
            }

            // 天花板
            for (int x = 0; x < design.Width; x++)
            {
                design.Tiles.Add(new TileData { X = x, Y = 0, TileType = "Wood" });
            }

            // 左墙
            for (int y = 0; y < design.Height; y++)
            {
                design.Tiles.Add(new TileData { X = 0, Y = y, TileType = "Wood" });
            }

            // 右墙
            for (int y = 0; y < design.Height; y++)
            {
                design.Tiles.Add(new TileData { X = design.Width - 1, Y = y, TileType = "Wood" });
            }

            // 背景墙
            for (int x = 1; x < design.Width - 1; x++)
            {
                for (int y = 1; y < design.Height - 1; y++)
                {
                    design.Walls.Add(new WallData { X = x, Y = y, WallType = "WoodWall" });
                }
            }

            // 门
            design.Doors.Add(new DoorData { X = design.Width / 2, Y = design.Height - 4, DoorType = "WoodenDoor" });

            // 家具
            design.Furniture.Add(new FurnitureData { X = 2, Y = design.Height - 2, FurnitureType = "WorkBench" });
            design.Furniture.Add(new FurnitureData { X = 4, Y = design.Height - 2, FurnitureType = "Chair" });
            design.Furniture.Add(new FurnitureData { X = 7, Y = design.Height - 2, FurnitureType = "Table" });

            // 光源
            design.LightSources.Add(new LightSourceData { X = 6, Y = 2, LightType = "Torch" });

            return design;
        }

        /// <summary>
        /// 创建小型城堡预设
        /// </summary>
        private BuildingDesign CreateSmallCastle()
        {
            var design = new BuildingDesign
            {
                Name = "小型城堡",
                Description = "一个石头建造的小城堡",
                Width = 20,
                Height = 15
            };

            // 地板
            for (int x = 0; x < design.Width; x++)
            {
                design.Tiles.Add(new TileData { X = x, Y = design.Height - 1, TileType = "StoneSlab" });
            }

            // 天花板
            for (int x = 0; x < design.Width; x++)
            {
                design.Tiles.Add(new TileData { X = x, Y = 0, TileType = "StoneSlab" });
            }

            // 墙壁
            for (int y = 0; y < design.Height; y++)
            {
                design.Tiles.Add(new TileData { X = 0, Y = y, TileType = "GrayBrick" });
                design.Tiles.Add(new TileData { X = design.Width - 1, Y = y, TileType = "GrayBrick" });
            }

            // 内部墙壁
            for (int x = 1; x < design.Width - 1; x++)
            {
                for (int y = 1; y < design.Height - 1; y++)
                {
                    design.Walls.Add(new WallData { X = x, Y = y, WallType = "StoneWall" });
                }
            }

            // 门
            design.Doors.Add(new DoorData { X = design.Width / 2, Y = design.Height - 4, DoorType = "IronDoor" });

            // 家具
            design.Furniture.Add(new FurnitureData { X = 3, Y = design.Height - 2, FurnitureType = "WorkBench" });
            design.Furniture.Add(new FurnitureData { X = 6, Y = design.Height - 2, FurnitureType = "Chair" });
            design.Furniture.Add(new FurnitureData { X = 10, Y = design.Height - 2, FurnitureType = "Table" });
            design.Furniture.Add(new FurnitureData { X = 14, Y = design.Height - 2, FurnitureType = "Bed" });
            design.Furniture.Add(new FurnitureData { X = 17, Y = design.Height - 2, FurnitureType = "Chest" });

            // 光源
            design.LightSources.Add(new LightSourceData { X = 5, Y = 2, LightType = "Chandelier" });
            design.LightSources.Add(new LightSourceData { X = 15, Y = 2, LightType = "Chandelier" });

            return design;
        }

        /// <summary>
        /// 创建观察塔预设
        /// </summary>
        private BuildingDesign CreateTower()
        {
            var design = new BuildingDesign
            {
                Name = "观察塔",
                Description = "一个高塔建筑",
                Width = 8,
                Height = 20
            };

            // 地板
            for (int x = 0; x < design.Width; x++)
            {
                design.Tiles.Add(new TileData { X = x, Y = design.Height - 1, TileType = "Brick" });
            }

            // 天花板
            for (int x = 0; x < design.Width; x++)
            {
                design.Tiles.Add(new TileData { X = x, Y = 0, TileType = "Brick" });
            }

            // 墙壁
            for (int y = 0; y < design.Height; y++)
            {
                design.Tiles.Add(new TileData { X = 0, Y = y, TileType = "Brick" });
                design.Tiles.Add(new TileData { X = design.Width - 1, Y = y, TileType = "Brick" });
            }

            // 内部平台
            for (int y = 5; y < design.Height - 1; y += 5)
            {
                for (int x = 1; x < design.Width - 1; x++)
                {
                    design.Tiles.Add(new TileData { X = x, Y = y, TileType = "Wood" });
                }
            }

            // 背景墙
            for (int x = 1; x < design.Width - 1; x++)
            {
                for (int y = 1; y < design.Height - 1; y++)
                {
                    design.Walls.Add(new WallData { X = x, Y = y, WallType = "BrickWall" });
                }
            }

            // 门
            design.Doors.Add(new DoorData { X = design.Width / 2, Y = design.Height - 4, DoorType = "WoodenDoor" });

            // 光源
            design.LightSources.Add(new LightSourceData { X = design.Width / 2, Y = 3, LightType = "Torch" });
            design.LightSources.Add(new LightSourceData { X = design.Width / 2, Y = 8, LightType = "Torch" });
            design.LightSources.Add(new LightSourceData { X = design.Width / 2, Y = 13, LightType = "Torch" });

            return design;
        }

        /// <summary>
        /// 创建地下室预设
        /// </summary>
        private BuildingDesign CreateCaveRoom()
        {
            var design = new BuildingDesign
            {
                Name = "地下室",
                Description = "一个地下房间",
                Width = 15,
                Height = 10
            };

            // 使用泥土和石头
            for (int x = 0; x < design.Width; x++)
            {
                design.Tiles.Add(new TileData { X = x, Y = design.Height - 1, TileType = "Stone" });
                design.Tiles.Add(new TileData { X = x, Y = 0, TileType = "Dirt" });
            }

            for (int y = 0; y < design.Height; y++)
            {
                design.Tiles.Add(new TileData { X = 0, Y = y, TileType = "Stone" });
                design.Tiles.Add(new TileData { X = design.Width - 1, Y = y, TileType = "Stone" });
            }

            // 背景墙
            for (int x = 1; x < design.Width - 1; x++)
            {
                for (int y = 1; y < design.Height - 1; y++)
                {
                    design.Walls.Add(new WallData { X = x, Y = y, WallType = "StoneWall" });
                }
            }

            // 家具
            design.Furniture.Add(new FurnitureData { X = 3, Y = design.Height - 2, FurnitureType = "WorkBench" });
            design.Furniture.Add(new FurnitureData { X = 6, Y = design.Height - 2, FurnitureType = "Chair" });
            design.Furniture.Add(new FurnitureData { X = 10, Y = design.Height - 2, FurnitureType = "Chest" });
            design.Furniture.Add(new FurnitureData { X = 13, Y = design.Height - 2, FurnitureType = "Furnace" });

            // 光源
            design.LightSources.Add(new LightSourceData { X = 5, Y = 2, LightType = "Torch" });
            design.LightSources.Add(new LightSourceData { X = 10, Y = 2, LightType = "Torch" });

            return design;
        }

        /// <summary>
        /// 创建商店预设
        /// </summary>
        private BuildingDesign CreateShop()
        {
            var design = new BuildingDesign
            {
                Name = "商店",
                Description = "一个商店建筑",
                Width = 12,
                Height = 8
            };

            // 主体结构
            for (int x = 0; x < design.Width; x++)
            {
                design.Tiles.Add(new TileData { X = x, Y = design.Height - 1, TileType = "Wood" });
                design.Tiles.Add(new TileData { X = x, Y = 0, TileType = "Wood" });
            }

            for (int y = 0; y < design.Height; y++)
            {
                design.Tiles.Add(new TileData { X = 0, Y = y, TileType = "Wood" });
                design.Tiles.Add(new TileData { X = design.Width - 1, Y = y, TileType = "Wood" });
            }

            // 背景墙
            for (int x = 1; x < design.Width - 1; x++)
            {
                for (int y = 1; y < design.Height - 1; y++)
                {
                    design.Walls.Add(new WallData { X = x, Y = y, WallType = "WoodWall" });
                }
            }

            // 门
            design.Doors.Add(new DoorData { X = 2, Y = design.Height - 4, DoorType = "WoodenDoor" });
            design.Doors.Add(new DoorData { X = design.Width - 3, Y = design.Height - 4, DoorType = "WoodenDoor" });

            // 家具 - 商店风格
            design.Furniture.Add(new FurnitureData { X = 5, Y = design.Height - 2, FurnitureType = "Table" });
            design.Furniture.Add(new FurnitureData { X = 8, Y = design.Height - 2, FurnitureType = "Chest" });
            design.Furniture.Add(new FurnitureData { X = 10, Y = design.Height - 2, FurnitureType = "Chair" });

            // 光源
            design.LightSources.Add(new LightSourceData { X = 6, Y = 2, LightType = "Chandelier" });

            return design;
        }
    }
}