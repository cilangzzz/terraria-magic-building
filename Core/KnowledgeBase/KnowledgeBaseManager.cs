using System;
using Terraria.ModLoader;

namespace trab.Core.KnowledgeBase
{
    /// <summary>
    /// 知识库管理器 - 统一管理建筑实体知识库
    /// </summary>
    public class KnowledgeBaseManager
    {
        private static KnowledgeBaseManager _instance;
        public static KnowledgeBaseManager Instance => _instance ??= new KnowledgeBaseManager();

        /// <summary>
        /// 建筑实体知识库 - 存储玩家建筑的完整数据
        /// </summary>
        public BuildingEntityBase Buildings { get; private set; }

        private bool _initialized = false;

        public void Initialize()
        {
            if (_initialized) return;

            try
            {
                Buildings = new BuildingEntityBase();
                Buildings.Initialize();

                _initialized = true;

                trab.Instance?.Logger.Info($"知识库初始化完成: Buildings={Buildings.BuildingCount}");
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
            Buildings = null;
        }
    }
}
