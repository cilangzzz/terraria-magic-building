using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using trab.Core;
using trab.Data;
using trab.Config;
using trab.UI;

namespace trab.Players
{
    /// <summary>
    /// AI建筑玩家扩展类
    /// </summary>
    public class AIBuildingPlayer : ModPlayer
    {
        private AIApiService _aiService;
        private CancellationTokenSource _currentRequest;

        // 存储最后一次生成的建筑设计
        public BuildingDesign LastDesign { get; private set; }

        // 存储最后一次的AI响应
        public string LastAIResponse { get; private set; }

        // 是否正在生成
        public bool IsGenerating { get; private set; }

        public override void Initialize()
        {
            _aiService = null;
            _currentRequest = null;
            LastDesign = null;
            LastAIResponse = "";
            IsGenerating = false;
        }

        /// <summary>
        /// 请求AI生成建筑设计
        /// </summary>
        public void RequestBuildingDesign(string prompt)
        {
            var config = ModContent.GetInstance<AIBuildingConfig>();

            // 检查API密钥
            if (string.IsNullOrEmpty(config.ApiKey))
            {
                Main.NewText("请先在模组配置中设置API密钥!", Color.Red);
                return;
            }

            // 取消之前的请求
            _currentRequest?.Cancel();
            _currentRequest = new CancellationTokenSource();

            IsGenerating = true;
            Main.NewText($"正在请求AI生成建筑: {prompt}", Color.Yellow);

            // 初始化服务
            if (_aiService == null)
            {
                _aiService = new AIApiService(config.ApiKey, config.ServiceProvider, config.CustomEndpoint, config.ModelName);
            }

            // 异步请求
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
        /// 处理AI响应
        /// </summary>
        private void ProcessBuildingResponse(string jsonResponse)
        {
            LastAIResponse = jsonResponse;

            // 提取JSON
            string json = AIApiService.ExtractJsonFromResponse(jsonResponse);
            if (json == null)
            {
                Main.NewText("无法从AI响应中提取建筑数据", Color.Red);
                return;
            }

            // 解析建筑数据
            var executor = new BuildingExecutor(Mod);
            var design = executor.ParseDesign(json);

            if (design != null)
            {
                LastDesign = design;
                Main.NewText($"=== 建筑设计 ===", Color.Cyan);
                Main.NewText($"名称: {design.Name}", Color.Green);
                Main.NewText($"描述: {design.Description}", Color.White);
                Main.NewText($"尺寸: {design.Width}x{design.Height}", Color.White);
                Main.NewText($"方块数: {design.Tiles.Count}", Color.White);
                Main.NewText($"墙壁数: {design.Walls.Count}", Color.White);
                Main.NewText($"家具数: {design.Furniture.Count}", Color.White);
                Main.NewText("使用 /aibuild place 在当前位置生成建筑", Color.Yellow);
            }
            else
            {
                Main.NewText("建筑数据解析失败", Color.Red);
            }
        }

        /// <summary>
        /// 在玩家位置生成最后设计的建筑
        /// </summary>
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
            int startY = (int)(Player.position.Y / 16) + config.BuildOffsetY - LastDesign.Height / 2;

            Main.NewText($"正在在位置 ({startX}, {startY}) 生成建筑...", Color.Yellow);

            bool success = executor.BuildAtLocation(LastDesign, startX, startY, Player);

            if (success)
            {
                Main.NewText($"建筑 '{LastDesign.Name}' 已成功生成!", Color.Green);
            }
        }

        /// <summary>
        /// 停止当前生成
        /// </summary>
        public void StopGeneration()
        {
            if (_currentRequest != null && !_currentRequest.IsCancellationRequested)
            {
                _currentRequest.Cancel();
                Main.NewText("已停止当前AI生成请求", Color.Yellow);
                IsGenerating = false;
            }
            else
            {
                Main.NewText("当前没有正在进行的生成请求", Color.Gray);
            }
        }

        public override void PreUpdate()
        {
            var uiSys = ModContent.GetInstance<AIBuildingUISystem>();

            // P键打开/关闭UI
            if (AIBuildingKeybindSystem.ToggleUIKey.JustPressed)
            {
                uiSys.Toggle();
            }

            // UI打开时的操作
            if (uiSys.Visible)
            {
                // G键生成
                if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.G) &&
                    !Main.oldKeyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.G))
                {
                    if (uiSys.panel != null)
                        uiSys.panel.DoGenerate();
                }
                // B键放置
                if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.B) &&
                    !Main.oldKeyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.B))
                {
                    if (uiSys.panel != null)
                        uiSys.panel.DoPlace();
                }
            }

            // B键快速放置（UI关闭时）
            if (!uiSys.Visible && AIBuildingKeybindSystem.PlaceBuildingKey.JustPressed)
            {
                PlaceLastDesign();
            }
        }
    }

    /// <summary>
    /// AI建筑快捷键绑定系统
    /// </summary>
    public class AIBuildingKeybindSystem : ModSystem
    {
        public static ModKeybind ToggleUIKey { get; private set; }
        public static ModKeybind PlaceBuildingKey { get; private set; }

        public override void Load()
        {
            // 注册快捷键
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