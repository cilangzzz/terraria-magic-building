using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Terraria.ModLoader;

namespace trab.Core.KnowledgeBase
{
    /// <summary>
    /// 知识库管理器 - 统一管理所有知识库模块
    /// </summary>
    public class KnowledgeBaseManager
    {
        private static KnowledgeBaseManager _instance;
        public static KnowledgeBaseManager Instance => _instance ??= new KnowledgeBaseManager();

        public TileKnowledgeBase Tiles { get; private set; }
        public StyleTemplateBase Styles { get; private set; }
        public FurnitureRuleBase Furniture { get; private set; }
        public VectorKnowledgeBase Vectors { get; private set; }
        public RoofTemplateBase Roofs { get; private set; }
        public WindowTemplateBase Windows { get; private set; }
        public FloorStructureBase Floors { get; private set; }
        public BuildingEntityBase Buildings { get; private set; }

        private bool _initialized = false;

        public void Initialize()
        {
            if (_initialized) return;

            try
            {
                Tiles = new TileKnowledgeBase();
                Styles = new StyleTemplateBase();
                Furniture = new FurnitureRuleBase();
                Vectors = new VectorKnowledgeBase();
                Vectors.Initialize();
                Roofs = new RoofTemplateBase();
                Windows = new WindowTemplateBase();
                Floors = new FloorStructureBase();
                Buildings = new BuildingEntityBase();
                Buildings.Initialize();

                _initialized = true;

                trab.Instance?.Logger.Info($"知识库初始化完成: Tiles={Tiles.TileCount}, Styles={Styles.StyleCount}, Vectors={Vectors.TileVectorCount}, Roofs={Roofs.RoofCount}, Windows={Windows.WindowCount}, Floors={Floors.FloorCount}, Buildings={Buildings.BuildingCount}");
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
            Tiles = null;
            Styles = null;
            Furniture = null;
            Vectors = null;
            Buildings = null;
        }
    }
}