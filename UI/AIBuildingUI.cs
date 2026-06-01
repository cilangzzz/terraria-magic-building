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
using trab.Players;

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

                    if (Visible && panel != null)
                        panel.DrawAreaPreview();

                    return true;
                }, InterfaceScaleType.UI));
            }
        }
    }

    public enum BuildingStyle
    {
        Medieval, Modern, Japanese, Fantasy, Underground, Custom
    }

    public class AIBuildingPanel : UIState
    {
        // 主容器
        private UIPanel mainPanel;

        // 四个模块容器
        private UIPanel styleModule;      // 风格模块
        private UIPanel sizeModule;       // 大小模块
        private UIPanel functionModule;   // 功能模块
        private UIPanel logModule;        // 日志模块

        // 自定义风格弹窗
        private UIPanel customStylePopup;
        private UIText customStyleHint;
        private bool showCustomPopup = false;
        private bool waitingForCustomInput = false;
        private string customStyleText = "";

        // 日志
        private UIPanel logContainer;      // 日志裁剪容器
        private UIText logText;
        private StringBuilder log = new StringBuilder();
        private List<string> logLines = new List<string>();
        private string lastResp = "";
        private bool busy = false;
        private int maxLogLines = 10;       // 限制日志行数防止超出
        private Texture2D whitePixel;

        // Agent模式
        public bool useAgentMode = true;
        private UITextPanel<string> agentBtn;
        private UITextPanel<string>[] styleButtons;
        private UITextPanel<string>[] sizeButtons;

        public BuildingStyle currentStyle = BuildingStyle.Medieval;
        public int currentSizeIndex = 0;

        // 选区
        public bool isSelectingArea = false;
        public bool isDragging = false;
        public Point? dragStart = null;
        public Point? dragEnd = null;
        public Point? confirmedAreaStart = null;
        public Point? confirmedAreaEnd = null;

        private int[] sizeWidths = { 10, 20, 35 };
        private int[] sizeHeights = { 8, 15, 25 };
        private string[] styleNames = { "中世纪", "现代", "日式", "奇幻", "地下", "自定义" };
        private string[] sizeNames = { "小", "中", "大" };

        private bool wasMouseLeftPressed = false;

        // UI尺寸常量
        private const float PANEL_WIDTH = 320f;
        private const float PANEL_HEIGHT = 480f;
        private const float MODULE_MARGIN = 8f;
        private const float MODULE_PADDING = 6f;
        private const float MODULE_TOP_MARGIN = 10f;    // 模块上边距
        private const float MODULE_HEIGHT_STYLE = 44f;
        private const float MODULE_HEIGHT_SIZE = 44f;
        private const float MODULE_HEIGHT_FUNC = 48f;
        private const float MODULE_HEIGHT_LOG = 271f;   // 日志框高度，容器高度259f
        private const float BUTTON_HEIGHT = 24f;
        private const float BUTTON_VPADDING = 5f;

        public override void OnInitialize()
        {
            mainPanel = new UIPanel();
            mainPanel.SetPadding(10);
            mainPanel.Width.Set(PANEL_WIDTH, 0f);
            mainPanel.Height.Set(PANEL_HEIGHT, 0f);
            mainPanel.Left.Set(Main.screenWidth - PANEL_WIDTH - 20f, 0f);
            mainPanel.Top.Set(80f, 0f);
            mainPanel.BackgroundColor = new Color(20, 20, 35, 240);
            mainPanel.BorderColor = new Color(60, 60, 80);
            Append(mainPanel);

            float startY = MODULE_TOP_MARGIN;

            // ─────────────────────────────────────────
            // 模块1: 风格选择
            // ─────────────────────────────────────────
            styleModule = new UIPanel();
            styleModule.SetPadding(MODULE_PADDING);
            styleModule.Width.Set(-MODULE_MARGIN * 2, 1f);
            styleModule.Height.Set(MODULE_HEIGHT_STYLE, 0f);
            styleModule.Left.Set(MODULE_MARGIN, 0f);
            styleModule.Top.Set(startY, 0f);
            styleModule.BackgroundColor = new Color(35, 35, 55, 200);
            styleModule.BorderColor = new Color(70, 70, 90);
            mainPanel.Append(styleModule);

            var styleLabel = new UIText("风格", 0.9f);
            styleLabel.Left.Set(2f, 0f);
            styleLabel.Top.Set(8f, 0f);
            styleLabel.TextColor = Color.LightGray;
            styleModule.Append(styleLabel);

            // 风格按钮组 - 6个按钮（5个预设+自定义）
            // 内容宽度292f，标签约45f，剩余247f，6按钮+5间距约250f
            styleButtons = new UITextPanel<string>[styleNames.Length];
            float btnW = 38f;
            float btnGap = 2f;
            float startX = 46f;
            for (int i = 0; i < styleNames.Length; i++)
            {
                styleButtons[i] = new UITextPanel<string>(styleNames[i], 0.6f);
                styleButtons[i].Width.Set(btnW, 0f);
                styleButtons[i].Height.Set(BUTTON_HEIGHT, 0f);
                styleButtons[i].Left.Set(startX + i * (btnW + btnGap), 0f);
                styleButtons[i].Top.Set(10f, 0f);
                styleButtons[i].SetPadding(BUTTON_VPADDING);
                styleButtons[i].BackgroundColor = i == 0 ? new Color(50, 100, 50) :
                    (i == styleNames.Length - 1 ? new Color(80, 80, 120) : new Color(45, 45, 65));
                int idx = i;
                styleButtons[i].OnLeftClick += (evt, elem) => SelectStyle(idx);
                styleModule.Append(styleButtons[i]);
            }

            startY += MODULE_HEIGHT_STYLE + MODULE_MARGIN;

            // ─────────────────────────────────────────
            // 模块2: 尺寸选择
            // ─────────────────────────────────────────
            sizeModule = new UIPanel();
            sizeModule.SetPadding(MODULE_PADDING);
            sizeModule.Width.Set(-MODULE_MARGIN * 2, 1f);
            sizeModule.Height.Set(MODULE_HEIGHT_SIZE, 0f);
            sizeModule.Left.Set(MODULE_MARGIN, 0f);
            sizeModule.Top.Set(startY, 0f);
            sizeModule.BackgroundColor = new Color(35, 35, 55, 200);
            sizeModule.BorderColor = new Color(70, 70, 90);
            mainPanel.Append(sizeModule);

            var sizeLabel = new UIText("尺寸", 0.9f);
            sizeLabel.Left.Set(2f, 0f);
            sizeLabel.Top.Set(8f, 0f);
            sizeLabel.TextColor = Color.LightGray;
            sizeModule.Append(sizeLabel);

            sizeButtons = new UITextPanel<string>[sizeNames.Length];
            float sBtnW = 48f;
            float sBtnGap = 3f;
            float sStartX = 46f;
            for (int i = 0; i < sizeNames.Length; i++)
            {
                sizeButtons[i] = new UITextPanel<string>(sizeNames[i], 0.7f);
                sizeButtons[i].Width.Set(sBtnW, 0f);
                sizeButtons[i].Height.Set(BUTTON_HEIGHT, 0f);
                sizeButtons[i].Left.Set(sStartX + i * (sBtnW + sBtnGap), 0f);
                sizeButtons[i].Top.Set(10f, 0f);
                sizeButtons[i].SetPadding(BUTTON_VPADDING);
                sizeButtons[i].BackgroundColor = i == 0 ? new Color(50, 100, 50) : new Color(45, 45, 65);
                int idx = i;
                sizeButtons[i].OnLeftClick += (evt, elem) => SelectSize(idx);
                sizeModule.Append(sizeButtons[i]);
            }

            // 尺寸详情
            var sizeDetail = new UIText("10x8", 0.75f);
            sizeDetail.Left.Set(210f, 0f);
            sizeDetail.Top.Set(12f, 0f);
            sizeDetail.TextColor = Color.Cyan;
            sizeModule.Append(sizeDetail);

            startY += MODULE_HEIGHT_SIZE + MODULE_MARGIN;

            // ─────────────────────────────────────────
            // 模块3: 功能操作
            // ─────────────────────────────────────────
            functionModule = new UIPanel();
            functionModule.SetPadding(MODULE_PADDING);
            functionModule.Width.Set(-MODULE_MARGIN * 2, 1f);
            functionModule.Height.Set(MODULE_HEIGHT_FUNC, 0f);
            functionModule.Left.Set(MODULE_MARGIN, 0f);
            functionModule.Top.Set(startY, 0f);
            functionModule.BackgroundColor = new Color(35, 35, 55, 200);
            functionModule.BorderColor = new Color(70, 70, 90);
            mainPanel.Append(functionModule);

            var funcLabel = new UIText("操作", 0.9f);
            funcLabel.Left.Set(2f, 0f);
            funcLabel.Top.Set(8f, 0f);
            funcLabel.TextColor = Color.LightGray;
            functionModule.Append(funcLabel);

            // 功能按钮 - 4个按钮
            float fBtnW = 56f;
            float fBtnGap = 3f;
            float fStartX = 40f;

            var genBtn = new UITextPanel<string>("生成", 0.75f);
            genBtn.Width.Set(fBtnW, 0f);
            genBtn.Height.Set(BUTTON_HEIGHT, 0f);
            genBtn.Left.Set(fStartX, 0f);
            genBtn.Top.Set(10f, 0f);
            genBtn.SetPadding(BUTTON_VPADDING);
            genBtn.BackgroundColor = new Color(60, 100, 60);
            genBtn.OnLeftClick += (evt, elem) => DoGenerate();
            functionModule.Append(genBtn);

            var placeBtn = new UITextPanel<string>("放置", 0.75f);
            placeBtn.Width.Set(fBtnW, 0f);
            placeBtn.Height.Set(BUTTON_HEIGHT, 0f);
            placeBtn.Left.Set(fStartX + fBtnW + fBtnGap, 0f);
            placeBtn.Top.Set(10f, 0f);
            placeBtn.SetPadding(BUTTON_VPADDING);
            placeBtn.BackgroundColor = new Color(70, 70, 100);
            placeBtn.OnLeftClick += (evt, elem) => DoPlaceAtMouse();
            functionModule.Append(placeBtn);

            var areaBtn = new UITextPanel<string>("选区", 0.75f);
            areaBtn.Width.Set(fBtnW, 0f);
            areaBtn.Height.Set(BUTTON_HEIGHT, 0f);
            areaBtn.Left.Set(fStartX + 2 * (fBtnW + fBtnGap), 0f);
            areaBtn.Top.Set(10f, 0f);
            areaBtn.SetPadding(BUTTON_VPADDING);
            areaBtn.BackgroundColor = new Color(80, 60, 40);
            areaBtn.OnLeftClick += (evt, elem) => ToggleAreaMode();
            functionModule.Append(areaBtn);

            agentBtn = new UITextPanel<string>("Agent", 0.75f);
            agentBtn.Width.Set(48f, 0f);
            agentBtn.Height.Set(BUTTON_HEIGHT, 0f);
            agentBtn.Left.Set(fStartX + 3 * (fBtnW + fBtnGap), 0f);
            agentBtn.Top.Set(10f, 0f);
            agentBtn.SetPadding(BUTTON_VPADDING);
            agentBtn.BackgroundColor = useAgentMode ? new Color(60, 120, 60) : new Color(50, 50, 50);
            agentBtn.OnLeftClick += (evt, elem) => ToggleAgentMode();
            functionModule.Append(agentBtn);

            startY += MODULE_HEIGHT_FUNC + MODULE_MARGIN;

            // ─────────────────────────────────────────
            // 模块4: 日志显示（带裁剪容器）
            // ─────────────────────────────────────────
            logModule = new UIPanel();
            logModule.SetPadding(MODULE_PADDING);
            logModule.Width.Set(-MODULE_MARGIN * 2, 1f);
            logModule.Height.Set(MODULE_HEIGHT_LOG, 0f);
            logModule.Left.Set(MODULE_MARGIN, 0f);
            logModule.Top.Set(startY, 0f);
            logModule.BackgroundColor = new Color(25, 25, 40, 220);
            logModule.BorderColor = new Color(60, 60, 80);
            mainPanel.Append(logModule);

            var logLabel = new UIText("日志", 0.85f);
            logLabel.Left.Set(2f, 0f);
            logLabel.Top.Set(4f, 0f);
            logLabel.TextColor = Color.Gray;
            logModule.Append(logLabel);

            // 日志裁剪容器 - 防止日志超出
            logContainer = new UIPanel();
            logContainer.SetPadding(0f);
            logContainer.Width.Set(-MODULE_PADDING * 2, 1f);
            logContainer.Height.Set(MODULE_HEIGHT_LOG - 12f, 0f);  // 拉长容器高度，大胆增加
            logContainer.Left.Set(MODULE_PADDING, 0f);
            logContainer.Top.Set(20f, 0f);  // 容器位置
            logContainer.BackgroundColor = Color.Transparent;
            logContainer.BorderColor = Color.Transparent;
            logModule.Append(logContainer);

            // 日志文本放在裁剪容器内
            logText = new UIText("", 0.8f);
            logText.Width.Set(0f, 1f);
            logText.Left.Set(2f, 0f);
            logText.Top.Set(6f, 0f);  // 文本往下一点
            logText.IsWrapped = false;  // 不换行，每行独立显示
            logContainer.Append(logText);

            // 清屏按钮
            var clearBtn = new UITextPanel<string>("×", 0.8f);
            clearBtn.Width.Set(18f, 0f);
            clearBtn.Height.Set(16f, 0f);
            clearBtn.Left.Set(-22f, 1f);
            clearBtn.Top.Set(2f, 0f);
            clearBtn.SetPadding(3f);
            clearBtn.BackgroundColor = new Color(70, 50, 50);
            clearBtn.OnLeftClick += (evt, elem) => ClearLog();
            logModule.Append(clearBtn);

            AddMessage("AI建筑助手已启动");
            AddMessage("P关闭 | M选区 | 自定义风格在聊天输入");

            // ─────────────────────────────────────────
            // 自定义风格弹窗 - 居中显示
            // ─────────────────────────────────────────
            customStylePopup = new UIPanel();
            customStylePopup.SetPadding(15);
            customStylePopup.Width.Set(300f, 0f);
            customStylePopup.Height.Set(120f, 0f);
            customStylePopup.Left.Set(Main.screenWidth / 2 - 150f, 0f);  // 屏幕居中
            customStylePopup.Top.Set(Main.screenHeight / 2 - 60f, 0f);   // 屏幕居中
            customStylePopup.BackgroundColor = new Color(30, 30, 50, 250);
            customStylePopup.BorderColor = new Color(100, 150, 200);
            Append(customStylePopup);  // 直接添加到UIState，不在mainPanel内

            var popupTitle = new UIText("自定义建筑风格", 1.1f);
            popupTitle.HAlign = 0.5f;  // 标题居中
            popupTitle.Top.Set(10f, 0f);
            popupTitle.TextColor = Color.Cyan;
            customStylePopup.Append(popupTitle);

            customStyleHint = new UIText("请在聊天框输入风格描述\n例如: 生成一个带有花园的木质别墅\n按Enter确认 | 按Esc取消", 0.85f);
            customStyleHint.HAlign = 0.5f;  // 内容居中
            customStyleHint.Top.Set(45f, 0f);
            customStyleHint.TextColor = Color.LightGray;
            customStyleHint.IsWrapped = true;
            customStylePopup.Append(customStyleHint);

            customStylePopup.Remove();  // 初始隐藏
        }

        private void SelectStyle(int idx)
        {
            currentStyle = (BuildingStyle)idx;

            // 如果选择自定义，打开弹窗
            if (idx == styleNames.Length - 1)  // 自定义按钮
            {
                showCustomPopup = true;
                AddMessage("请输入自定义风格描述");
            }
            else
            {
                showCustomPopup = false;
                AddMessage($"风格: {styleNames[idx]}");
            }

            // 更新按钮颜色
            for (int i = 0; i < styleButtons.Length; i++)
            {
                if (i == idx)
                    styleButtons[i].BackgroundColor = new Color(50, 100, 50);
                else if (i == styleNames.Length - 1)  // 自定义按钮特殊颜色
                    styleButtons[i].BackgroundColor = new Color(80, 80, 120);
                else
                    styleButtons[i].BackgroundColor = new Color(45, 45, 65);
            }
        }

        private void SelectSize(int idx)
        {
            currentSizeIndex = idx;
            AddMessage($"尺寸: {sizeWidths[idx]}x{sizeHeights[idx]}");
            for (int i = 0; i < sizeButtons.Length; i++)
                sizeButtons[i].BackgroundColor = i == idx ? new Color(50, 100, 50) : new Color(45, 45, 65);

            foreach (var elem in sizeModule.Children)
            {
                if (elem is UIText t && t.TextColor == Color.Cyan)
                    t.SetText($"{sizeWidths[idx]}x{sizeHeights[idx]}");
            }
        }

        public void ToggleAreaMode()
        {
            isSelectingArea = !isSelectingArea;
            isDragging = false;
            dragStart = null;
            dragEnd = null;

            if (isSelectingArea)
            {
                AddMessage("[选区] 拖拽选择区域");
                AddMessage("松开确认 | G生成");
            }
            else
            {
                AddMessage("[选区] 关闭");
                confirmedAreaStart = null;
                confirmedAreaEnd = null;
            }
        }

        public void UpdateProgress(string progress, int round = 0)
        {
            // 根据内容选择颜色前缀
            string prefix = round > 0 ? $"[R{round}] " : "";
            AddMessage($"{prefix}{progress}");
        }

        public void ToggleAgentMode()
        {
            useAgentMode = !useAgentMode;
            AddMessage($"Agent模式: {(useAgentMode ? "开启" : "关闭")}");

            if (useAgentMode)
                AddMessage("Agent将检索知识库生成精美建筑");
            else
                AddMessage("传统模式直接生成简单建筑");

            if (agentBtn != null)
                agentBtn.BackgroundColor = useAgentMode ? new Color(60, 120, 60) : new Color(50, 50, 50);
        }

        private string GetStylePrompt(BuildingStyle style, int w, int h)
        {
            // 更丰富的提示词，包含方块和家具要求
            return style switch
            {
                BuildingStyle.Medieval =>
                    $"生成中世纪风格建筑 {w}x{h}。使用GrayBrick、StoneSlab方块，Wood地板，添加WorkBench、Chair、Table家具，放置Torch照明，WoodenDoor作为门。墙壁使用GrayBrickWall。返回JSON格式。",

                BuildingStyle.Modern =>
                    $"生成现代风格建筑 {w}x{h}。使用Glass、GrayBrick方块，Platform地板，添加Bed、Dresser、Table家具，放置Chandelier照明，使用各种Paint涂色。墙壁使用GlassWall。返回JSON格式。",

                BuildingStyle.Japanese =>
                    $"生成日式风格建筑 {w}x{h}。使用Wood、BorealWood方块，Wood地板，添加WorkBench、Chair、Table家具，放置Torch照明，使用红棕色Paint。墙壁使用WoodWall。屋顶用斜坡Slope。返回JSON格式。",

                BuildingStyle.Fantasy =>
                    $"生成奇幻风格建筑 {w}x{h}。使用GoldBrick、Marble方块，添加Piano、Bathtub、Bed豪华家具，放置红蓝Paint配色。墙壁使用GoldBrickWall。返回JSON格式。",

                BuildingStyle.Underground =>
                    $"生成地下基地建筑 {w}x{h}。使用Stone、Obsidian方块，添加Furnace、Anvil、WorkBench工坊家具，放置Torch照明。墙壁使用StoneWall。返回JSON格式。",

                BuildingStyle.Custom =>
                    !string.IsNullOrEmpty(customStyleText) ? $"{customStyleText} {w}x{h}，使用合适方块和家具，返回JSON格式。" : $"生成建筑 {w}x{h}，返回JSON格式。",

                _ => $"生成建筑 {w}x{h}，使用多种方块和家具，返回JSON格式。"
            };
        }

        public void DoGenerate()
        {
            if (busy) { AddMessage("等待中..."); return; }

            var cfg = ModContent.GetInstance<AIBuildingConfig>();
            if (string.IsNullOrEmpty(cfg.ApiKey))
            {
                AddMessage("[错误] 请设置API密钥");
                return;
            }

            int w, h;
            if (confirmedAreaStart.HasValue && confirmedAreaEnd.HasValue)
            {
                w = Math.Abs(confirmedAreaEnd.Value.X - confirmedAreaStart.Value.X) + 1;
                h = Math.Abs(confirmedAreaEnd.Value.Y - confirmedAreaStart.Value.Y) + 1;
                w = Math.Min(w, cfg.MaxBuildingSize);
                h = Math.Min(h, cfg.MaxBuildingSize);
                AddMessage($"选区: {w}x{h}");
            }
            else
            {
                w = sizeWidths[currentSizeIndex];
                h = sizeHeights[currentSizeIndex];
            }

            string prompt = GetStylePrompt(currentStyle, w, h);
            AddMessage($"生成: {styleNames[(int)currentStyle]}");

            if (useAgentMode)
            {
                AddMessage("[Agent] 启动智能生成...");
                busy = true;

                var player = Main.LocalPlayer.GetModPlayer<AIBuildingPlayer>();
                player.RequestBuildingDesignAgent(prompt);
            }
            else
            {
                prompt += "，返回JSON格式";
                AddMessage("请求API...");

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
                                AddMessage("[成功] 已生成");
                                string j = AIApiService.ExtractJsonFromResponse(r);
                                if (j != null)
                                {
                                    var d = new BuildingExecutor(trab.Instance).ParseDesign(j);
                                    if (d != null)
                                        AddMessage($"{d.Name} ({d.Width}x{d.Height})");
                                }
                                AddMessage("B键放置");
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
        }

        public void DoPlaceAtMouse()
        {
            var player = Main.LocalPlayer.GetModPlayer<AIBuildingPlayer>();
            var lastDesign = player.LastDesign;

            if (lastDesign == null && string.IsNullOrEmpty(lastResp))
            {
                AddMessage("[提示] 先G生成");
                return;
            }

            if (lastDesign == null && !string.IsNullOrEmpty(lastResp))
            {
                string j = AIApiService.ExtractJsonFromResponse(lastResp);
                if (j == null) { AddMessage("[错误] 解析失败"); return; }

                var e = new BuildingExecutor(trab.Instance);
                var d = e.ParseDesign(j);
                if (d == null) { AddMessage("[错误] 无效数据"); return; }
                lastDesign = d;
            }

            int x = (int)(Main.MouseWorld.X / 16);
            int y = (int)(Main.MouseWorld.Y / 16);

            if (confirmedAreaStart.HasValue && confirmedAreaEnd.HasValue)
            {
                x = Math.Min(confirmedAreaStart.Value.X, confirmedAreaEnd.Value.X);
                y = Math.Min(confirmedAreaStart.Value.Y, confirmedAreaEnd.Value.Y);
                AddMessage("放置到选区");
            }
            else
            {
                AddMessage($"放置: ({x},{y})");
            }

            var executor = trab.Instance.EnhancedBuilder;
            executor.BuildAtLocationEnhanced(lastDesign, x, y, Main.LocalPlayer);
            AddMessage($"'{lastDesign.Name}' 完成");

            confirmedAreaStart = null;
            confirmedAreaEnd = null;
            isSelectingArea = false;
        }

        private void AddMessage(string msg)
        {
            logLines.Add(msg);

            // 限制行数防止超出容器
            while (logLines.Count > maxLogLines)
                logLines.RemoveAt(0);

            log.Clear();
            foreach (var line in logLines)
                log.AppendLine(line);
            logText.SetText(log.ToString());
        }

        private void ClearLog()
        {
            logLines.Clear();
            log.Clear();
            AddMessage("已清空");
        }

        public override void Update(GameTime gt)
        {
            base.Update(gt);
            mainPanel.Left.Set(Main.screenWidth - PANEL_WIDTH - 20f, 0f);

            // 处理自定义风格弹窗显示
            if (showCustomPopup)
            {
                if (!HasChild(customStylePopup))
                    Append(customStylePopup);

                // 更新弹窗位置保持居中
                customStylePopup.Left.Set(Main.screenWidth / 2 - 150f, 0f);
                customStylePopup.Top.Set(Main.screenHeight / 2 - 60f, 0f);

                // 检测聊天输入
                if (Main.drawingPlayerChat)
                {
                    waitingForCustomInput = true;
                }
                else if (waitingForCustomInput)
                {
                    // 聊天框关闭时获取输入内容
                    string chatText = Main.chatText;
                    if (!string.IsNullOrEmpty(chatText) && chatText != "/")
                    {
                        customStyleText = chatText.TrimStart('/');
                        AddMessage($"自定义风格: {customStyleText}");
                    }
                    waitingForCustomInput = false;
                    showCustomPopup = false;
                    customStylePopup.Remove();
                }

                // Esc取消
                if (Main.keyState.IsKeyDown(Keys.Escape))
                {
                    showCustomPopup = false;
                    waitingForCustomInput = false;
                    customStylePopup.Remove();
                    AddMessage("已取消自定义风格");
                }
            }

            // 同步busy状态
            var player = Main.LocalPlayer.GetModPlayer<AIBuildingPlayer>();
            if (busy && !player.IsGenerating)
            {
                busy = false;
                if (player.LastDesign != null)
                    AddMessage("[完成] 可按B放置");
                else
                    AddMessage("[失败] 请重试");
            }

            // UI面板区域
            Rectangle panelRect = new Rectangle(
                (int)(Main.screenWidth - PANEL_WIDTH - 20f),
                80,
                (int)PANEL_WIDTH,
                (int)PANEL_HEIGHT
            );
            bool mouseInUI = panelRect.Contains((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y);

            if (mouseInUI)
                Main.LocalPlayer.mouseInterface = true;

            // 选区拖拽
            if (isSelectingArea && !mouseInUI)
            {
                int tx = (int)(Main.MouseWorld.X / 16);
                int ty = (int)(Main.MouseWorld.Y / 16);

                if (Main.mouseLeft && !wasMouseLeftPressed)
                {
                    isDragging = true;
                    dragStart = new Point(tx, ty);
                    dragEnd = new Point(tx, ty);
                    AddMessage($"起点: ({tx},{ty})");
                }

                if (isDragging && Main.mouseLeft)
                    dragEnd = new Point(tx, ty);

                if (!Main.mouseLeft && wasMouseLeftPressed && isDragging)
                {
                    isDragging = false;
                    if (dragStart.HasValue && dragEnd.HasValue)
                    {
                        confirmedAreaStart = dragStart;
                        confirmedAreaEnd = dragEnd;
                        int w = Math.Abs(dragEnd.Value.X - dragStart.Value.X) + 1;
                        int h = Math.Abs(dragEnd.Value.Y - dragStart.Value.Y) + 1;
                        AddMessage($"选区: {w}x{h}");
                        AddMessage("G生成匹配建筑");
                    }
                    dragStart = null;
                    dragEnd = null;
                }
            }

            wasMouseLeftPressed = Main.mouseLeft;
        }

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

            if (whitePixel == null)
            {
                whitePixel = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                whitePixel.SetData(new[] { Color.White });
            }

            Vector2 pos = new Vector2(tileX * 16 - Main.screenPosition.X, tileY * 16 - Main.screenPosition.Y);

            Main.spriteBatch.Draw(whitePixel, new Rectangle((int)pos.X, (int)pos.Y, 16, 16), color * 0.25f);
            Main.spriteBatch.Draw(whitePixel, new Rectangle((int)pos.X, (int)pos.Y, 16, 2), color);
            Main.spriteBatch.Draw(whitePixel, new Rectangle((int)pos.X, (int)pos.Y + 14, 16, 2), color);
            Main.spriteBatch.Draw(whitePixel, new Rectangle((int)pos.X, (int)pos.Y, 2, 16), color);
            Main.spriteBatch.Draw(whitePixel, new Rectangle((int)pos.X + 14, (int)pos.Y, 2, 16), color);
        }
    }
}