using System;
using Terraria.ModLoader;

namespace trab.Core.KnowledgeBase
{
    /// <summary>
    /// 知识库管理器 - 统一管理构件级建筑知识库
    /// </summary>
    public class KnowledgeBaseManager
    {
        private static KnowledgeBaseManager _instance;
        public static KnowledgeBaseManager Instance => _instance ??= new KnowledgeBaseManager();

        /// <summary>
        /// 构件级建筑知识库 - 核心数据存储
        /// </summary>
        public ComponentKnowledgeBase Components { get; private set; }

        /// <summary>
        /// 建筑实体知识库 - 旧格式兼容
        /// </summary>
        public BuildingEntityBase Buildings { get; private set; }

        private bool _initialized = false;

        public void Initialize()
        {
            if (_initialized) return;

            try
            {
                // 优先使用构件级知识库
                Components = new ComponentKnowledgeBase();
                Components.Initialize();

                // 保留旧格式兼容
                Buildings = new BuildingEntityBase();
                Buildings.Initialize();

                _initialized = true;

                trab.Instance?.Logger.Info($"知识库初始化完成: 构件库={Components.BuildingCount}建筑/{Components.ComponentCount}构件/{Components.StyleCount}风格, 实体库={Buildings.BuildingCount}建筑");
            }
            catch (Exception ex)
            {
                trab.Instance?.Logger.Error("知识库初始化失败: " + ex.Message);
            }
        }

        public bool IsInitialized => _initialized;

        public void Reset()
        {
            _initialized = false;
            Components = null;
            Buildings = null;
        }
    }
}