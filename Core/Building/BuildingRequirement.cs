using Newtonsoft.Json;
using System.Collections.Generic;

namespace trab.Data
{
    /// <summary>
    /// 建筑需求分析结果 - 阶段1输出
    /// </summary>
    public class BuildingRequirement
    {
        [JsonProperty("building_type")]
        public string BuildingType { get; set; } = "house";  // house, tower, castle, shop, temple

        [JsonProperty("style")]
        public string Style { get; set; } = "medieval";  // medieval, fantasy, natural, steampunk, asian, snow, desert, modern, dark

        [JsonProperty("biome")]
        public string Biome { get; set; } = "forest";  // forest, desert, snow, jungle, ocean, underground

        [JsonProperty("floor_count")]
        public int FloorCount { get; set; } = 1;

        [JsonProperty("width")]
        public int? PreferredWidth { get; set; }

        [JsonProperty("height")]
        public int? PreferredHeight { get; set; }

        [JsonProperty("materials")]
        public MaterialCategoryRequirement Materials { get; set; } = new MaterialCategoryRequirement();

        [JsonProperty("special_features")]
        public List<string> SpecialFeatures { get; set; } = new List<string>();

        [JsonProperty("need_npc_house")]
        public bool NeedNpcHouse { get; set; } = true;

        [JsonProperty("target_npcs")]
        public List<string> TargetNpcs { get; set; } = new List<string>();

        [JsonProperty("reasoning")]
        public string AnalysisReasoning { get; set; }
    }

    /// <summary>
    /// 材料类别需求 - 定义各类材料的要求
    /// </summary>
    public class MaterialCategoryRequirement
    {
        [JsonProperty("main_block_category")]
        public string MainBlockCategory { get; set; } = "brick";  // basic, wood, brick, slab, luxury, transparent

        [JsonProperty("secondary_block_category")]
        public string SecondaryBlockCategory { get; set; }

        [JsonProperty("roof_block_category")]
        public string RoofBlockCategory { get; set; } = "slab";

        [JsonProperty("floor_block_category")]
        public string FloorBlockCategory { get; set; } = "wood";

        [JsonProperty("accent_block_category")]
        public string AccentBlockCategory { get; set; }

        [JsonProperty("main_wall_category")]
        public string MainWallCategory { get; set; } = "brick";

        [JsonProperty("required_furniture_categories")]
        public List<string> RequiredFurnitureCategories { get; set; } = new List<string> { "light", "surface", "comfort", "door" };

        [JsonProperty("optional_furniture_categories")]
        public List<string> OptionalFurnitureCategories { get; set; } = new List<string>();

        [JsonProperty("color_theme")]
        public string ColorTheme { get; set; }  // warm, cold, dark, bright

        [JsonProperty("use_shadow_paint")]
        public bool UseShadowPaint { get; set; } = true;
    }

    /// <summary>
    /// 材料候选集 - 阶段2输出
    /// </summary>
    public class MaterialCandidates
    {
        [JsonProperty("main_blocks")]
        public List<MaterialCandidateItem> MainBlocks { get; set; } = new List<MaterialCandidateItem>();

        [JsonProperty("secondary_blocks")]
        public List<MaterialCandidateItem> SecondaryBlocks { get; set; } = new List<MaterialCandidateItem>();

        [JsonProperty("roof_blocks")]
        public List<MaterialCandidateItem> RoofBlocks { get; set; } = new List<MaterialCandidateItem>();

        [JsonProperty("floor_blocks")]
        public List<MaterialCandidateItem> FloorBlocks { get; set; } = new List<MaterialCandidateItem>();

        [JsonProperty("accent_blocks")]
        public List<MaterialCandidateItem> AccentBlocks { get; set; } = new List<MaterialCandidateItem>();

        [JsonProperty("main_walls")]
        public List<MaterialCandidateItem> MainWalls { get; set; } = new List<MaterialCandidateItem>();

        [JsonProperty("furniture_by_category")]
        public Dictionary<string, List<MaterialCandidateItem>> FurnitureByCategory { get; set; }
            = new Dictionary<string, List<MaterialCandidateItem>>();
    }

    /// <summary>
    /// 单个材料候选项
    /// </summary>
    public class MaterialCandidateItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("similarity_score")]
        public float SimilarityScore { get; set; }

        [JsonProperty("styles")]
        public List<string> Styles { get; set; } = new List<string>();

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    /// <summary>
    /// 选定材料集 - 阶段3输出
    /// </summary>
    public class SelectedMaterials
    {
        [JsonProperty("main_block_id")]
        public int MainBlockId { get; set; } = 4;  // 默认灰砖

        [JsonProperty("secondary_block_id")]
        public int? SecondaryBlockId { get; set; }

        [JsonProperty("roof_block_id")]
        public int RoofBlockId { get; set; } = 143;  // 默认石板

        [JsonProperty("floor_block_id")]
        public int FloorBlockId { get; set; } = 5;  // 默认木材

        [JsonProperty("accent_block_id")]
        public int? AccentBlockId { get; set; }

        [JsonProperty("main_wall_id")]
        public int MainWallId { get; set; } = 6;  // 默认灰砖墙

        [JsonProperty("furniture_ids")]
        public Dictionary<string, int> FurnitureIds { get; set; } = new Dictionary<string, int>
        {
            ["light"] = 4,      // 火把
            ["surface"] = 17,   // 工作台
            ["comfort"] = 88,   // 椅子
            ["door"] = 10       // 门
        };

        [JsonProperty("primary_paint")]
        public int PrimaryPaint { get; set; } = 0;

        [JsonProperty("shadow_paint")]
        public int ShadowPaint { get; set; } = 28;

        [JsonProperty("reasoning")]
        public string SelectionReasoning { get; set; }

        [JsonProperty("cohesion_score")]
        public float CohesionScore { get; set; }
    }
}