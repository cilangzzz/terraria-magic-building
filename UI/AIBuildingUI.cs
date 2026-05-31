using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using trab.Config;
using trab.Core;
using trab.Data;

namespace trab.UI
{
    public class AIBuildingUISystem : ModSystem
    {
        private UserInterface ui;
        internal AIBuildingPanel panel;

        public bool Visible => ui?.CurrentState != null;

        public override void Load()
        {
            ui = new UserInterface();
        }

        public override void Unload()
        {
            panel = null;
            ui = null;
        }

        public void Toggle()
        {
            if (panel == null)
            {
                panel = new AIBuildingPanel();
                panel.Activate();
            }
            ui.SetState(Visible ? null : panel);
        }

        public override void UpdateUI(GameTime gt)
        {
            if (Visible) ui.Update(gt);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int i = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
            if (i >= 0)
            {
                layers.Insert(i + 1, new LegacyGameInterfaceLayer("AI:UI", () =>
                {
                    if (Visible && !Main.playerInventory)
                        ui.Draw(Main.spriteBatch, new GameTime());

                    // 绘制选区预览
                    if (Visible && panel != null)
                        panel.DrawAreaPreview();

                    return true;
                }, InterfaceScaleType.UI));
            }
        }
    }

    public enum BuildingStyle
    {
        Simple, Medieval, Modern, Japanese, Underground, Tower, Castle
    }

    public class AIBuildingPanel : UIState
    {
        private UIPanel bg;
        private UIText logText;
        private UIText statusText;
        private StringBuilder log = new StringBuilder();
        private List<string> logLines = new List<string>();
        private string lastResp = "";
        private bool busy = false;
        private int maxLogLines = 12;
        private Texture2D whitePixel;  // 用于绘制的单像素纹理

        public BuildingStyle currentStyle = BuildingStyle.Simple;
        public int currentSizeIndex = 0;

        // 选区状态
        public bool isSelectingArea = false;      // 是否正在选区
        public bool isDragging = false;           // 是否正在拖拽
        public Point? dragStart = null;           // 拖拽起点
        public Point? dragEnd = null;             // 拖拽终点
        public Point? confirmedAreaStart = null;  // 确认的选区起点
        public Point? confirmedAreaEnd = null;    // 确认的选区终点

        private int[] sizeWidths = { 10, 20, 35 };
        private int[] sizeHeights = { 8, 15, 25 };
        private string[] styleNames = { "简单", "中世纪", "现代", "日式", "地下", "塔楼", "城堡" };
        private string[] sizeNames = { "小", "中", "大" };

        private bool wasMouseLeftPressed = false;

        public override void OnInitialize()
        {
            bg = new UIPanel();
            bg.SetPadding(8);
            bg.Width.Set(400f, 0f);
            bg.Height.Set(280f, 0f);
            bg.Left.Set(Main.screenWidth - 420f, 0f);
            bg.Top.Set(80f, 0f);
            bg.BackgroundColor = new Color(25, 25, 45, 230);
            Append(bg);

            // 标题
            var title = new UIText("AI建筑助手 (P关闭)", 1.0f);
            title.Left.Set(10f, 0f);
            title.Top.Set(5f, 0f);
            bg.Append(title);

            // 日志区域 - 使用UIText，手动控制换行
            logText = new UIText("", 0.85f);
            logText.Width.Set(380f, 0f);
            logText.Height.Set(120f, 0f);
            logText.Left.Set(10f, 0f);
            logText.Top.Set(25f, 0f);
            logText.IsWrapped = true;
            bg.Append(logText);

            // 风格按钮行
            for (int i = 0; i < styleNames.Length; i++)
            {
                var btn = new UITextPanel<string>(styleNames[i], 0.7f);
                btn.Width.Set(52f, 0f);
                btn.Height.Set(18f, 0f);
                btn.Left.Set(5f + i * 54f, 0f);
                btn.Top.Set(155f, 0f);
                btn.BackgroundColor = i == 0 ? new Color(50, 100, 50) : new Color(40, 40, 60);
                int idx = i;
                btn.OnLeftClick += (evt, elem) => SelectStyle(idx);
                bg.Append(btn);
            }

            // 尺寸按钮行
            for (int i = 0; i < sizeNames.Length; i++)
            {
                var btn = new UITextPanel<string>(sizeNames[i], 0.7f);
                btn.Width.Set(55f, 0f);
                btn.Height.Set(18f, 0f);
                btn.Left.Set(5f + i * 58f, 0f);
                btn.Top.Set(178f, 0f);
                btn.BackgroundColor = i == 0 ? new Color(50, 100, 50) : new Color(40, 40, 60);
                int idx = i;
                btn.OnLeftClick += (evt, elem) => SelectSize(idx);
                bg.Append(btn);
            }

            // 功能按钮行
            var genBtn = new UITextPanel<string>("生成(G)", 0.8f);
            genBtn.Width.Set(75f, 0f);
            genBtn.Height.Set(22f, 0f);
            genBtn.Left.Set(5f, 0f);
            genBtn.Top.Set(202f, 0f);
            genBtn.BackgroundColor = new Color(60, 100, 60);
            genBtn.OnLeftClick += (evt, elem) => DoGenerate();
            bg.Append(genBtn);

            var placeBtn = new UITextPanel<string>("放置(B)", 0.8f);
            placeBtn.Width.Set(75f, 0f);
            placeBtn.Height.Set(22f, 0f);
            placeBtn.Left.Set(82f, 0f);
            placeBtn.Top.Set(202f, 0f);
            placeBtn.BackgroundColor = new Color(70, 70, 100);
            placeBtn.OnLeftClick += (evt, elem) => DoPlaceAtMouse();
            bg.Append(placeBtn);

            var areaBtn = new UITextPanel<string>("选区(M)", 0.8f);
            areaBtn.Width.Set(75f, 0f);
            areaBtn.Height.Set(22f, 0f);
            areaBtn.Left.Set(159f, 0f);
            areaBtn.Top.Set(202f, 0f);
            areaBtn.BackgroundColor = isSelectingArea ? new Color(100, 80, 50) : new Color(80, 60, 40);
            areaBtn.OnLeftClick += (evt, elem) => ToggleAreaMode();
            bg.Append(areaBtn);

            var clearBtn = new UITextPanel<string>("清屏", 0.8f);
            clearBtn.Width.Set(55f, 0f);
            clearBtn.Height.Set(22f, 0f);
            clearBtn.Left.Set(236f, 0f);
            clearBtn.Top.Set(202f, 0f);
            clearBtn.BackgroundColor = new Color(80, 50, 50);
            clearBtn.OnLeftClick += (evt, elem) => ClearLog();
            bg.Append(clearBtn);

            // 状态信息
            statusText = new UIText("风格:简单 尺寸:小 选区:无", 0.8f);
            statusText.Left.Set(5f, 0f);
            statusText.Top.Set(230f, 0f);
            statusText.TextColor = Color.LightGray;
            bg.Append(statusText);

            // 提示
            var hint = new UIText("按住鼠标拖拽选区 | 松开确认 | G生成匹配大小建筑", 0.7f);
            hint.Left.Set(5f, 0f);
            hint.Top.Set(250f, 0f);
            hint.TextColor = Color.Gray;
            bg.Append(hint);

            // 初始消息
            AddMessage("欢迎使用AI建筑助手!");
            AddMessage("M开启选区模式,拖拽选择区域");
            AddMessage("G生成建筑(匹配选区或预设尺寸)");
            AddMessage("B在鼠标位置放置建筑");
        }

        private void SelectStyle(int idx)
        {
            currentStyle = (BuildingStyle)idx;
            AddMessage($"风格: {styleNames[idx]}");
            UpdateStatus();
        }

        private void SelectSize(int idx)
        {
            currentSizeIndex = idx;
            AddMessage($"尺寸: {sizeNames[idx]} ({sizeWidths[idx]}x{sizeHeights[idx]})");
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            string areaStr = "无";
            if (confirmedAreaStart.HasValue && confirmedAreaEnd.HasValue)
            {
                int w = Math.Abs(confirmedAreaEnd.Value.X - confirmedAreaStart.Value.X) + 1;
                int h = Math.Abs(confirmedAreaEnd.Value.Y - confirmedAreaStart.Value.Y) + 1;
                areaStr = $"{w}x{h}";
            }
            else if (isSelectingArea)
            {
                areaStr = "选择中";
            }

            statusText.SetText($"风格:{styleNames[(int)currentStyle]} 尺寸:{sizeNames[currentSizeIndex]} 选区:{areaStr}");
        }

        public void ToggleAreaMode()
        {
            isSelectingArea = !isSelectingArea;
            isDragging = false;
            dragStart = null;
            dragEnd = null;

            if (isSelectingArea)
            {
                AddMessage("[选区模式] 按住鼠标拖拽选择区域");
                AddMessage("选完后按G生成匹配大小建筑");
            }
            else
            {
                AddMessage("[选区模式] 已关闭");
                confirmedAreaStart = null;
                confirmedAreaEnd = null;
            }
            UpdateStatus();
        }

        private string GetStylePrompt(BuildingStyle style, int w, int h)
        {
            return style switch
            {
                BuildingStyle.Simple => $"生成简单木屋 {w}x{h}",
                BuildingStyle.Medieval => $"生成中世纪石砖建筑 {w}x{h}",
                BuildingStyle.Modern => $"生成现代玻璃建筑 {w}x{h}",
                BuildingStyle.Japanese => $"生成日式木屋 {w}x{h}",
                BuildingStyle.Underground => $"生成地下基地 {w}x{h}",
                BuildingStyle.Tower => $"生成高塔 高{h}宽{w}",
                BuildingStyle.Castle => $"生成小型城堡 {w}x{h}",
                _ => $"生成建筑 {w}x{h}"
            };
        }

        public void DoGenerate()
        {
            if (busy) { AddMessage("请等待生成完成..."); return; }

            var cfg = ModContent.GetInstance<AIBuildingConfig>();
            if (string.IsNullOrEmpty(cfg.ApiKey))
            {
                AddMessage("[错误] 请先设置API密钥!");
                return;
            }

            // 根据选区或预设尺寸确定大小
            int w, h;
            if (confirmedAreaStart.HasValue && confirmedAreaEnd.HasValue)
            {
                w = Math.Abs(confirmedAreaEnd.Value.X - confirmedAreaStart.Value.X) + 1;
                h = Math.Abs(confirmedAreaEnd.Value.Y - confirmedAreaStart.Value.Y) + 1;
                w = Math.Min(w, cfg.MaxBuildingSize);
                h = Math.Min(h, cfg.MaxBuildingSize);
                AddMessage($"根据选区生成: {w}x{h}");
            }
            else
            {
                w = sizeWidths[currentSizeIndex];
                h = sizeHeights[currentSizeIndex];
                AddMessage($"根据预设生成: {w}x{h}");
            }

            string prompt = GetStylePrompt(currentStyle, w, h) + "，返回JSON格式建筑设计";
            AddMessage($"正在请求AI...");

            busy = true;
            Task.Run(async () =>
            {
                try
                {
                    var s = new AIApiService(cfg.ApiKey, cfg.ServiceProvider, cfg.CustomEndpoint, cfg.ModelName);
                    var r = await s.SendChatRequestAsync(prompt, CancellationToken.None);
                    Main.QueueMainThreadAction(() =>
                    {
                        lastResp = r ?? "";
                        if (!string.IsNullOrEmpty(r))
                        {
                            AddMessage("[成功] 建筑已生成!");
                            string j = AIApiService.ExtractJsonFromResponse(r);
                            if (j != null)
                            {
                                var d = new BuildingExecutor(trab.Instance).ParseDesign(j);
                                if (d != null)
                                    AddMessage($"{d.Name}: {d.Width}x{d.Height}");
                            }
                            AddMessage("按B在鼠标位置放置");
                        }
                        else
                            AddMessage("[失败] 空响应");
                        busy = false;
                    });
                }
                catch (Exception ex)
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        AddMessage("[错误] " + ex.Message);
                        busy = false;
                    });
                }
            });
        }

        /// <summary>
        /// 在鼠标位置放置建筑
        /// </summary>
        public void DoPlaceAtMouse()
        {
            if (string.IsNullOrEmpty(lastResp))
            {
                AddMessage("[提示] 先按G生成建筑");
                return;
            }

            string j = AIApiService.ExtractJsonFromResponse(lastResp);
            if (j == null) { AddMessage("[错误] JSON解析失败"); return; }

            var e = new BuildingExecutor(trab.Instance);
            var d = e.ParseDesign(j);
            if (d == null) { AddMessage("[错误] 无效建筑数据"); return; }

            // 使用鼠标位置
            int x = (int)(Main.MouseWorld.X / 16);
            int y = (int)(Main.MouseWorld.Y / 16);

            // 如果有选区，使用选区的左上角
            if (confirmedAreaStart.HasValue && confirmedAreaEnd.HasValue)
            {
                x = Math.Min(confirmedAreaStart.Value.X, confirmedAreaEnd.Value.X);
                y = Math.Min(confirmedAreaStart.Value.Y, confirmedAreaEnd.Value.Y);
                AddMessage($"放置到选区位置");
            }
            else
            {
                AddMessage($"放置到鼠标位置: ({x}, {y})");
            }

            e.BuildAtLocation(d, x, y, Main.LocalPlayer);
            AddMessage($"建筑 '{d.Name}' 已放置!");

            // 清除选区
            confirmedAreaStart = null;
            confirmedAreaEnd = null;
            isSelectingArea = false;
            UpdateStatus();
        }

        private void AddMessage(string msg)
        {
            logLines.Add(msg);

            // 限制行数
            while (logLines.Count > maxLogLines)
            {
                logLines.RemoveAt(0);
            }

            // 重建显示文本
            log.Clear();
            foreach (var line in logLines)
            {
                log.AppendLine(line);
            }
            logText.SetText(log.ToString());
        }

        private void ClearLog()
        {
            logLines.Clear();
            log.Clear();
            AddMessage("日志已清空");
        }

        public override void Update(GameTime gt)
        {
            base.Update(gt);
            bg.Left.Set(Main.screenWidth - 420f, 0f);

            // 判断鼠标是否在UI面板内（使用精确的矩形检测）
            Rectangle panelRect = new Rectangle(
                (int)(Main.screenWidth - 420f),
                80,
                400,
                280
            );
            bool mouseInUI = panelRect.Contains((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y);

            if (mouseInUI)
                Main.LocalPlayer.mouseInterface = true;

            // 处理选区拖拽（选区模式下且鼠标不在UI内）
            if (isSelectingArea && !mouseInUI)
            {
                int tx = (int)(Main.MouseWorld.X / 16);
                int ty = (int)(Main.MouseWorld.Y / 16);

                // 检测鼠标按下开始拖拽
                if (Main.mouseLeft && !wasMouseLeftPressed)
                {
                    isDragging = true;
                    dragStart = new Point(tx, ty);
                    dragEnd = new Point(tx, ty);
                    AddMessage($"开始选区: ({tx}, {ty})");
                    UpdateStatus();
                }

                // 拖拽中更新终点
                if (isDragging && Main.mouseLeft)
                {
                    dragEnd = new Point(tx, ty);
                }

                // 鼠标释放，确认选区
                if (!Main.mouseLeft && wasMouseLeftPressed && isDragging)
                {
                    isDragging = false;
                    if (dragStart.HasValue && dragEnd.HasValue)
                    {
                        confirmedAreaStart = dragStart;
                        confirmedAreaEnd = dragEnd;
                        int w = Math.Abs(dragEnd.Value.X - dragStart.Value.X) + 1;
                        int h = Math.Abs(dragEnd.Value.Y - dragStart.Value.Y) + 1;
                        AddMessage($"选区确认: {w}x{h}");
                        AddMessage("按G生成匹配大小建筑");
                    }
                    dragStart = null;
                    dragEnd = null;
                    UpdateStatus();
                }
            }

            wasMouseLeftPressed = Main.mouseLeft;
        }

        /// <summary>
        /// 绘制选区预览（在世界渲染时调用）
        /// </summary>
        public void DrawAreaPreview()
        {
            if (!isSelectingArea) return;

            Point? start = isDragging ? dragStart : confirmedAreaStart;
            Point? end = isDragging ? dragEnd : confirmedAreaEnd;

            if (start.HasValue && end.HasValue)
            {
                int x1 = Math.Min(start.Value.X, end.Value.X);
                int y1 = Math.Min(start.Value.Y, end.Value.Y);
                int x2 = Math.Max(start.Value.X, end.Value.X);
                int y2 = Math.Max(start.Value.Y, end.Value.Y);

                // 绘制选区边框
                for (int x = x1; x <= x2; x++)
                {
                    DrawTileHighlight(x, y1, Color.Yellow);
                    DrawTileHighlight(x, y2, Color.Yellow);
                }
                for (int y = y1; y <= y2; y++)
                {
                    DrawTileHighlight(x1, y, Color.Yellow);
                    DrawTileHighlight(x2, y, Color.Yellow);
                }
            }
        }

        private void DrawTileHighlight(int tileX, int tileY, Color color)
        {
            if (!WorldGen.InWorld(tileX, tileY)) return;

            // 初始化纹理（如果需要）
            if (whitePixel == null)
            {
                whitePixel = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                whitePixel.SetData(new[] { Color.White });
            }

            Vector2 pos = new Vector2(tileX * 16 - Main.screenPosition.X, tileY * 16 - Main.screenPosition.Y);

            // 绘制半透明填充
            Main.spriteBatch.Draw(whitePixel, new Rectangle((int)pos.X, (int)pos.Y, 16, 16), color * 0.3f);

            // 绘制边框线条
            Main.spriteBatch.Draw(whitePixel, new Rectangle((int)pos.X, (int)pos.Y, 16, 2), color);
            Main.spriteBatch.Draw(whitePixel, new Rectangle((int)pos.X, (int)pos.Y + 14, 16, 2), color);
            Main.spriteBatch.Draw(whitePixel, new Rectangle((int)pos.X, (int)pos.Y, 2, 16), color);
            Main.spriteBatch.Draw(whitePixel, new Rectangle((int)pos.X + 14, (int)pos.Y, 2, 16), color);
        }
    }
}