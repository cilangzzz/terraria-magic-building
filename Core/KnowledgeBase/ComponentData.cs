using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace trab.Core.KnowledgeBase
{
    /// <summary>
    /// 建筑复杂性层次
    /// </summary>
    public enum BuildingComplexity
    {
        Atomic,      // 原子构件：单个屋顶、墙壁、装饰
        Composite,   // 复合构件：房间、楼层
        Building,    // 完整建筑：住宅、塔楼
        Complex      // 建筑群：村庄、基地
    }

    /// <summary>
    /// 构件定义 - 核心数据结构
    /// 使用规则而非坐标描述构件
    /// </summary>
    public class ComponentDefinition
    {
        public string id { get; set; }
        public string type { get; set; }      // roof, wall, floor, decoration, foundation
        public string subtype { get; set; }   // pagoda, outer_wall, lantern, etc.

        /// <summary>
        /// 相对边界（可缩放）
        /// </summary>
        public ComponentBounds bounds_relative { get; set; }

        /// <summary>
        /// 绝对边界（原始建筑坐标）
        /// </summary>
        public ComponentBounds bounds_absolute { get; set; }

        /// <summary>
        /// 材料配置
        /// </summary>
        public ComponentMaterials materials { get; set; }

        /// <summary>
        /// 生成规则
        /// </summary>
        public GenerationRule generation_rule { get; set; }

        /// <summary>
        /// 构件参数（如tier_count, spacing等）
        /// </summary>
        public Dictionary<string, object> parameters { get; set; }
    }

    /// <summary>
    /// 构件边界
    /// </summary>
    public class ComponentBounds
    {
        public int? x_start { get; set; }
        public int? x_end { get; set; }
        public int? y_start { get; set; }
        public int? y_end { get; set; }
        public int? thickness { get; set; }
        public int? width { get; set; }
        public int? height { get; set; }
        public int? x_offset { get; set; }
        public int? y_offset { get; set; }
    }

    /// <summary>
    /// 构件材料配置
    /// </summary>
    public class ComponentMaterials
    {
        public MaterialRef primary { get; set; }
        public MaterialRef secondary { get; set; }
        public MaterialRef accent { get; set; }
        public MaterialRef frame { get; set; }
    }

    /// <summary>
    /// 材料引用
    /// </summary>
    public class MaterialRef
    {
        public int? tile_id { get; set; }
        public int? wall_id { get; set; }
        public string name { get; set; }
        public string use { get; set; }  // 用途描述
    }

    /// <summary>
    /// 生成规则
    /// </summary>
    public class GenerationRule
    {
        public string pattern { get; set; }   // pyramid_tiered, filled_rectangle, linear_spacing
        public string formula { get; set; }  // 公式描述
        public List<string> params_required { get; set; }  // 必需参数
        public Dictionary<string, object> default_params { get; set; }  // 默认参数
    }

    /// <summary>
    /// 建筑实体V2 - 构件级数据
    /// </summary>
    public class BuildingEntityV2
    {
        public string id { get; set; }
        public string name { get; set; }

        /// <summary>
        /// 建筑复杂性层次
        /// </summary>
        public BuildingComplexity complexity { get; set; }

        /// <summary>
        /// 建筑类型：house, tower, castle, shop, temple
        /// </summary>
        public string building_type { get; set; }

        /// <summary>
        /// 尺寸
        /// </summary>
        public Dimensions dimensions { get; set; }

        /// <summary>
        /// 风格标签
        /// </summary>
        public List<string> style_tags { get; set; }

        /// <summary>
        /// 结构组成
        /// </summary>
        public BuildingStructure structure { get; set; }

        /// <summary>
        /// 包含的构件引用
        /// </summary>
        public List<ComponentReference> components { get; set; }

        /// <summary>
        /// 建造顺序
        /// </summary>
        public List<string> build_sequence { get; set; }

        /// <summary>
        /// NPC房屋验证
        /// </summary>
        public NPCRequirements npc_requirements { get; set; }

        /// <summary>
        /// 描述摘要
        /// </summary>
        public string summary { get; set; }

        /// <summary>
        /// 向量嵌入（用于RAG检索）
        /// </summary>
        [JsonIgnore]
        public float[] vector { get; set; }
    }

    /// <summary>
    /// 建筑结构
    /// </summary>
    public class BuildingStructure
    {
        public ComponentReference foundation { get; set; }
        public List<ComponentReference> stories { get; set; }
        public ComponentReference roof { get; set; }
        public List<ComponentReference> decorations { get; set; }
        public List<ComponentReference> walls { get; set; }
    }

    /// <summary>
    /// 构件引用
    /// </summary>
    public class ComponentReference
    {
        public string ref_id { get; set; }    // 构件ID
        public string type { get; set; }       // 构件类型
        public string role { get; set; }       // 角色描述
        public int? level { get; set; }        // 楼层编号
        public Bounds bounds { get; set; }     // 边界范围
    }

    /// <summary>
    /// NPC房屋要求
    /// </summary>
    public class NPCRequirements
    {
        public bool has_light_source { get; set; }
        public bool has_door { get; set; }
        public bool has_table { get; set; }
        public bool has_chair { get; set; }
        public bool has_walls { get; set; }
        public bool valid_house { get; set; }
    }

    /// <summary>
    /// 风格材料映射
    /// </summary>
    public class StyleMaterialMapping
    {
        public string style { get; set; }

        /// <summary>
        /// 推荐方块材料
        /// </summary>
        public List<StyleMaterialItem> tiles { get; set; }

        /// <summary>
        /// 推荐墙壁材料
        /// </summary>
        public List<StyleMaterialItem> walls { get; set; }

        /// <summary>
        /// 推荐装饰
        /// </summary>
        public List<StyleMaterialItem> decorations { get; set; }

        /// <summary>
        /// 推荐门
        /// </summary>
        public List<StyleMaterialItem> doors { get; set; }

        /// <summary>
        /// 推荐家具
        /// </summary>
        public List<StyleMaterialItem> furniture { get; set; }

        /// <summary>
        /// 色调
        /// </summary>
        public string color_tone { get; set; }

        /// <summary>
        /// 颜色列表
        /// </summary>
        public List<string> colors { get; set; }
    }

    /// <summary>
    /// 风格材料项
    /// </summary>
    public class StyleMaterialItem
    {
        public int id { get; set; }
        public string name { get; set; }
        public string use { get; set; }      // 用途: roof, wall, floor, accent
        public string category { get; set; }  // 类别: light, surface, comfort
        public string material { get; set; }  // 材质描述
        public string alternative { get; set; }  // 替代选项
    }

    /// <summary>
    /// 建筑检索条件
    /// </summary>
    public class BuildingSearchCriteria
    {
        public string style { get; set; }
        public string building_type { get; set; }
        public BuildingComplexity? complexity { get; set; }
        public int? min_width { get; set; }
        public int? max_width { get; set; }
        public int? min_height { get; set; }
        public int? max_height { get; set; }
        public List<string> required_features { get; set; }
        public int top_k { get; set; } = 5;

        /// <summary>
        /// 是否需要NPC房屋功能
        /// </summary>
        public bool? npc_valid { get; set; }
    }

    /// <summary>
    /// 建筑检索结果
    /// </summary>
    public class BuildingSearchResult
    {
        public string id { get; set; }
        public string name { get; set; }
        public BuildingComplexity complexity { get; set; }
        public string building_type { get; set; }
        public Dimensions dimensions { get; set; }
        public List<string> style_tags { get; set; }
        public string summary { get; set; }
        public float similarity { get; set; }
        public List<string> available_components { get; set; }
    }
}
