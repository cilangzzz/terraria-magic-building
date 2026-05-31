using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
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
        internal SimplePanel panel;

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
                panel = new SimplePanel();
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
                    return true;
                }, InterfaceScaleType.UI));
            }
        }
    }

    public class SimplePanel : UIState
    {
        private UIPanel bg;
        private UIText txt;
        private StringBuilder log = new StringBuilder();
        private string lastResp = "";
        private bool busy = false;

        public override void OnInitialize()
        {
            // 主面板 - 设置固定宽高
            bg = new UIPanel();
            bg.SetPadding(10);
            bg.Width.Set(400f, 0f);  // 固定宽度400像素
            bg.Height.Set(350f, 0f); // 固定高度350像素
            bg.Left.Set(100f, 0f);
            bg.Top.Set(100f, 0f);
            bg.BackgroundColor = new Microsoft.Xna.Framework.Color(30, 30, 50, 200);
            Append(bg);

            // 标题文本
            var title = new UIText("AI建筑助手 (P关闭 G生成 B放置)", 1.0f);
            title.Width.Set(380f, 0f);
            title.Left.Set(5f, 0f);
            title.Top.Set(5f, 0f);
            bg.Append(title);

            // 日志文本区域
            txt = new UIText("", 0.85f);
            txt.Width.Set(380f, 0f);  // 设置宽度
            txt.Height.Set(280f, 0f); // 设置高度
            txt.Left.Set(5f, 0f);
            txt.Top.Set(30f, 0f);
            txt.IsWrapped = true;
            bg.Append(txt);

            Msg("按G生成建筑，按B放置，按P关闭");
            Msg("API: DeepSeek v4-flash");
        }

        public void DoGenerate()
        {
            if (busy) return;
            var cfg = ModContent.GetInstance<AIBuildingConfig>();
            if (string.IsNullOrEmpty(cfg.ApiKey))
            {
                Msg("请设置API密钥");
                return;
            }

            busy = true;
            Msg("正在请求API...");

            Task.Run(async () =>
            {
                try
                {
                    var s = new AIApiService(cfg.ApiKey, cfg.ServiceProvider, cfg.CustomEndpoint, cfg.ModelName);
                    var r = await s.SendChatRequestAsync("生成一座简单木屋，JSON格式", CancellationToken.None);
                    Main.QueueMainThreadAction(() =>
                    {
                        lastResp = r ?? "";
                        if (!string.IsNullOrEmpty(r))
                        {
                            Msg("生成完成! 按B放置");
                        }
                        else
                        {
                            Msg("生成失败-空响应");
                        }
                        busy = false;
                    });
                }
                catch (Exception ex)
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        Msg("错误:" + ex.Message);
                        busy = false;
                    });
                }
            });
        }

        public void DoPlace()
        {
            if (string.IsNullOrEmpty(lastResp))
            {
                Msg("无设计-先按G生成");
                return;
            }

            string j = AIApiService.ExtractJsonFromResponse(lastResp);
            if (j == null) { Msg("解析失败"); return; }

            var e = new BuildingExecutor(trab.Instance);
            var d = e.ParseDesign(j);
            if (d == null) { Msg("无效数据"); return; }

            int x = (int)(Main.LocalPlayer.position.X / 16) + 5;
            int y = (int)(Main.LocalPlayer.position.Y / 16) - d.Height / 2;
            e.BuildAtLocation(d, x, y, Main.LocalPlayer);
            Msg("已放置!");
        }

        private void Msg(string m)
        {
            log.AppendLine(m);
            txt.SetText(log.ToString());
        }

        public override void Update(GameTime gt)
        {
            base.Update(gt);
            // 跟随屏幕右侧
            bg.Left.Set(Main.screenWidth - 420f, 0f);
            bg.Top.Set(50f, 0f);

            if (ContainsPoint(Main.MouseScreen))
                Main.LocalPlayer.mouseInterface = true;
        }
    }
}