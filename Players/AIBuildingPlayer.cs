using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using trab.Core.Agents;
using trab.Core.API;
using trab.Core.Building;
using trab.Core.KnowledgeBase;
using trab.Data;
using trab.Config;
using trab.UI;

namespace trab.Players
{
    /// <summary>
    /// AI建筑玩家扩展类 - TEditSch格式版本
    /// </summary>
    public class AIBuildingPlayer : ModPlayer
    {
        private AIApiService _aiService;
        private AIAgentService _agentService;
        private CancellationTokenSource _currentRequest;

        // 存储最后一次生成的建筑设计（TEditSch格式）
        public TEditSchDesign LastDesign { get; private set; }

        // 存储最后一次的AI响应
        public string LastAIResponse { get; private set; }

        // 是否正在生成
        public bool IsGenerating { get; private set; }

        // Agent模式进度信息
        public string AgentProgress { get; private set; } = "";

        // 工具调用历史
        public List<string> ToolCallHistory { get; private set; } = new List<string>();

        public override void Initialize()
        {
            _aiService = null;
            _agentService = null;
            _currentRequest = null;
            LastDesign = null;
            LastAIResponse = "";
            IsGenerating = false;
            AgentProgress = "";
            ToolCallHistory.Clear();
        }

        /// <summary>
        /// 请求AI生成建筑设计（传统API模式）
        /// </summary>
        public void RequestBuildingDesign(string prompt)
        {
            var config = ModContent.GetInstance<AIBuildingConfig>();

            if (string.IsNullOrEmpty(config.ApiKey))
            {
                Main.NewText("请先在模组配置中设置API密钥!", Color.Red);
                return;
            }

            _currentRequest?.Cancel();
            _currentRequest = new CancellationTokenSource();

            IsGenerating = true;
            Main.NewText($"正在请求AI生成建筑: {prompt}", Color.Yellow);

            if (_aiService == null)
            {
                _aiService = new AIApiService(config.ApiKey, config.ServiceProvider, config.ModelName);
            }

            Task.Run(async () =>
            {
                try
                {
                    var response = await _aiService.SendChatRequestAsync(prompt, _currentRequest.Token);

                    if (_currentRequest.Token.IsCancellationRequested)
                    {
                        Main.QueueMainThreadAction(() =>
                        {
                            Main.NewText("生成请求已取消", Color.Yellow);
                            IsGenerating = false;
                        });
                        return;
                    }

                    if (response != null)
                    {
                        Main.QueueMainThreadAction(() =>
                        {
                            ProcessBuildingResponse(response);
                            IsGenerating = false;
                        });
                    }
                    else
                    {
                        Main.QueueMainThreadAction(() =>
                        {
                            Main.NewText("AI返回空响应", Color.Red);
                            IsGenerating = false;
                        });
                    }
                }
                catch (Exception ex)
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        Main.NewText($"生成失败: {ex.Message}", Color.Red);
                        IsGenerating = false;
                    });
                }
            });
        }

        /// <summary>
        /// 请求AI生成建筑设计（Agent模式 - 推荐）
        /// </summary>
        public void RequestBuildingDesignAgent(string prompt)
        {
            var config = ModContent.GetInstance<AIBuildingConfig>();

            if (string.IsNullOrEmpty(config.ApiKey))
            {
                Main.NewText("请先在模组配置中设置API密钥!", Color.Red);
                return;
            }

            _currentRequest?.Cancel();
            _currentRequest = new CancellationTokenSource();

            IsGenerating = true;
            AgentProgress = "启动Agent...";
            ToolCallHistory.Clear();

            Main.NewText($"[Agent模式] 正在生成建筑: {prompt}", Color.Cyan);

            if (_agentService == null)
            {
                _agentService = new AIAgentService();
            }

            Task.Run(async () =>
            {
                try
                {
                    var design = await _agentService.GenerateBuildingAsync(
                        prompt,
                        (progress, round) =>
                        {
                            Main.QueueMainThreadAction(() =>
                            {
                                AgentProgress = progress;
                                ToolCallHistory.Add(progress);

                                var uiSys = ModContent.GetInstance<AIBuildingUISystem>();
                                if (uiSys.Visible && uiSys.panel != null)
                                {
                                    uiSys.panel.UpdateProgress(progress, round);
                                }
                                else
                                {
                                    Color msgColor = round > 0 ? Color.LightBlue : Color.Green;
                                    Main.NewText($"[Agent] {progress}", msgColor);
                                }
                            });
                        },
                        _currentRequest.Token
                    );

                    if (_currentRequest.Token.IsCancellationRequested)
                    {
                        Main.QueueMainThreadAction(() =>
                        {
                            Main.NewText("Agent生成请求已取消", Color.Yellow);
                            IsGenerating = false;
                            AgentProgress = "已取消";
                        });
                        return;
                    }

                    if (design != null)
                    {
                        Main.QueueMainThreadAction(() =>
                        {
                            ProcessAgentDesign(design);
                            IsGenerating = false;
                            AgentProgress = "完成";
                        });
                    }
                    else
                    {
                        Main.QueueMainThreadAction(() =>
                        {
                            Main.NewText("Agent返回空设计", Color.Red);
                            IsGenerating = false;
                            AgentProgress = "失败";
                        });
                    }
                }
                catch (Exception ex)
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        Main.NewText($"Agent生成失败: {ex.Message}", Color.Red);
                        IsGenerating = false;
                        AgentProgress = $"错误: {ex.Message}";
                    });
                }
            });
        }

        private void ProcessAgentDesign(TEditSchDesign design)
        {
            LastDesign = design;

            Main.NewText($"=== Agent建筑设计 (TEditSch) ===", Color.Cyan);
            Main.NewText($"名称: {design.name}", Color.Green);
            Main.NewText($"尺寸: {design.width}x{design.height}", Color.White);

            if (design.stats != null)
            {
                Main.NewText($"活跃方块: {design.stats.active_tiles}", Color.White);
                Main.NewText($"墙壁数: {design.stats.tiles_with_wall}", Color.White);

                if (design.stats.tile_type_distribution != null && design.stats.tile_type_distribution.Count > 0)
                {
                    var topTiles = new List<string>();
                    foreach (var kv in design.stats.tile_type_distribution)
                    {
                        topTiles.Add($"Tile_{kv.Key}:{kv.Value}");
                    }
                    Main.NewText($"方块分布: {string.Join(", ", topTiles.Take(5))}", Color.Gray);
                }
            }

            Main.NewText("按 B 键或使用 /aibuild place 在当前位置生成建筑", Color.Yellow);
        }

        private void ProcessBuildingResponse(string jsonResponse)
        {
            LastAIResponse = jsonResponse;

            string json = AIApiService.ExtractJsonFromResponse(jsonResponse);
            if (json == null)
            {
                Main.NewText("无法从AI响应中提取建筑数据", Color.Red);
                return;
            }

            var executor = new BuildingExecutor(Mod);
            var design = executor.ParseTEditSchDesign(json);

            if (design != null)
            {
                LastDesign = design;
                Main.NewText($"=== 建筑设计 (TEditSch) ===", Color.Cyan);
                Main.NewText($"名称: {design.name}", Color.Green);
                Main.NewText($"尺寸: {design.width}x{design.height}", Color.White);
                Main.NewText($"活跃方块: {design.stats?.active_tiles ?? 0}", Color.White);
                Main.NewText("使用 /aibuild place 在当前位置生成建筑", Color.Yellow);
            }
            else
            {
                Main.NewText("建筑数据解析失败", Color.Red);
            }
        }

        public void PlaceLastDesign()
        {
            if (LastDesign == null)
            {
                Main.NewText("没有可用的建筑设计", Color.Red);
                Main.NewText("请先使用 /aibuild <描述> 生成设计", Color.Yellow);
                return;
            }

            var config = ModContent.GetInstance<AIBuildingConfig>();
            var executor = new BuildingExecutor(Mod);

            int startX = (int)(Player.position.X / 16) + config.BuildOffsetX;
            int startY = (int)(Player.position.Y / 16) + config.BuildOffsetY - LastDesign.height / 2;

            Main.NewText($"正在在位置 ({startX}, {startY}) 生成建筑...", Color.Yellow);

            bool success = executor.BuildTEditSch(LastDesign, startX, startY, Player);

            if (success)
            {
                Main.NewText($"建筑 '{LastDesign.name}' 已成功生成!", Color.Green);
            }
        }

        public void PlaceDesignAt(TEditSchDesign design, int startX, int startY)
        {
            if (design == null)
            {
                Main.NewText("无效的设计", Color.Red);
                return;
            }

            var executor = new BuildingExecutor(Mod);
            executor.BuildTEditSch(design, startX, startY, Player);
        }

        public void StopGeneration()
        {
            if (_currentRequest != null && !_currentRequest.IsCancellationRequested)
            {
                _currentRequest.Cancel();
                Main.NewText("已停止当前AI生成请求", Color.Yellow);
                IsGenerating = false;
                AgentProgress = "已取消";
            }
            else
            {
                Main.NewText("当前没有正在进行的生成请求", Color.Gray);
            }
        }

        public string GetKnowledgeBaseStatus()
        {
            var kb = KnowledgeBaseManager.Instance;
            if (!kb.IsInitialized)
            {
                return "知识库未初始化";
            }

            return $"方块: {kb.Tiles.TileCount} | 墙壁: {kb.Tiles.WallCount} | 油漆: {kb.Tiles.PaintCount} | 风格: {kb.Styles.StyleCount} | 家具: {kb.Furniture.FurnitureCount}";
        }

        public override void PreUpdate()
        {
            var uiSys = ModContent.GetInstance<AIBuildingUISystem>();

            if (AIBuildingKeybindSystem.ToggleUIKey.JustPressed && !Main.drawingPlayerChat && !Main.gameMenu)
            {
                uiSys.Toggle();
            }

            if (uiSys.Visible && !Main.drawingPlayerChat)
            {
                if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.G) &&
                    !Main.oldKeyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.G))
                {
                    if (uiSys.panel != null)
                        uiSys.panel.DoGenerate();
                }
                if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.B) &&
                    !Main.oldKeyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.B))
                {
                    if (uiSys.panel != null)
                        uiSys.panel.DoPlaceAtMouse();
                }
                if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.M) &&
                    !Main.oldKeyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.M))
                {
                    if (uiSys.panel != null)
                        uiSys.panel.ToggleAreaMode();
                }
            }

            if (!uiSys.Visible && !Main.drawingPlayerChat && !Main.gameMenu && AIBuildingKeybindSystem.PlaceBuildingKey.JustPressed)
            {
                PlaceLastDesign();
            }
        }
    }

    public class AIBuildingKeybindSystem : ModSystem
    {
        public static ModKeybind ToggleUIKey { get; private set; }
        public static ModKeybind PlaceBuildingKey { get; private set; }

        public override void Load()
        {
            ToggleUIKey = KeybindLoader.RegisterKeybind(Mod, "ToggleAIUI", "P");
            PlaceBuildingKey = KeybindLoader.RegisterKeybind(Mod, "PlaceBuilding", "B");
        }

        public override void Unload()
        {
            ToggleUIKey = null;
            PlaceBuildingKey = null;
        }
    }
}