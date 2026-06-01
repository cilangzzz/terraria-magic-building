using System;
using Terraria.ModLoader;
using trab.Core;
using trab.Config;

namespace trab
{
    /// <summary>
    /// AI建筑生成模组主类
    ///
    /// 功能概述:
    /// - 通过AI API生成建筑设计
    /// - 支持OpenAI和Claude API
    /// - 在游戏中快速生成建筑
    /// - 提供聊天命令和UI界面
    ///
    /// 使用方法:
    /// 1. 在模组配置中设置API密钥
    /// 2. 使用 /aibuild <描述> 命令生成建筑
    /// 3. 或按P键打开UI界面
    /// 4. 使用 /quickbuild <预设> 快速生成预设建筑
    /// </summary>
    public class trab : Mod
    {
        /// <summary>
        /// 模组单例实例
        /// </summary>
        public static trab Instance { get; private set; }

        /// <summary>
        /// 建筑执行器实例（基础版）
        /// </summary>
        public BuildingExecutor Builder { get; private set; }

        /// <summary>
        /// 增强版建筑执行器实例（Agent模式专用）
        /// </summary>
        public EnhancedBuildingExecutor EnhancedBuilder { get; private set; }

        public override void Load()
        {
            Instance = this;

            // 初始化建筑执行器
            Builder = new BuildingExecutor(this);
            EnhancedBuilder = new EnhancedBuildingExecutor(this);

            // 初始化知识库管理器
            KnowledgeBaseManager.Instance.Initialize();

            // 日志信息
            Logger.Info("AI建筑模组已加载 (Agent模式)");
            Logger.Info("使用 /aibuild help 查看帮助");
            Logger.Info("按 P 键打开AI建筑UI");
            Logger.Info($"知识库状态: 方块{KnowledgeBaseManager.Instance.Tiles.TileCount}个, 风格{KnowledgeBaseManager.Instance.Styles.StyleCount}种, 向量{KnowledgeBaseManager.Instance.Vectors.TileVectorCount}个");
        }

        public override void Unload()
        {
            Instance = null;
            Builder = null;
            EnhancedBuilder = null;
            KnowledgeBaseManager.Instance.Reset();  // 重置知识库状态
        }

        /// <summary>
        /// 获取模组配置
        /// </summary>
        public static AIBuildingConfig GetConfig()
        {
            return ModContent.GetInstance<AIBuildingConfig>();
        }

        /// <summary>
        /// 检查API是否已配置
        /// </summary>
        public static bool IsApiConfigured()
        {
            var config = GetConfig();
            return !string.IsNullOrEmpty(config.ApiKey);
        }

        /// <summary>
        /// 显示配置提示
        /// </summary>
        public static void ShowConfigHint()
        {
            Terraria.Main.NewText("请先在模组配置中设置API密钥!", Microsoft.Xna.Framework.Color.Red);
            Terraria.Main.NewText("按 ESC -> 模组配置 -> trab Config", Microsoft.Xna.Framework.Color.Yellow);
        }
    }
}