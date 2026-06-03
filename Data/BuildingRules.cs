using System.Collections.Generic;
using Newtonsoft.Json;

namespace trab.Data
{
    /// <summary>
    /// 建筑设计规则 - AI输出格式
    /// AI输出精简的设计规则，程序化生成器展开为完整方块数据
    /// </summary>
    public class BuildingRules
    {
        [JsonProperty("name")]
        public string name { get; set; }

        [JsonProperty("width")]
        public int width { get; set; }

        [JsonProperty("height")]
        public int height { get; set; }

        [JsonProperty("style")]
        public string style { get; set; }

        /// <summary>
        /// 模板引用（可选），如果指定则基于模板修改
        /// </summary>
        [JsonProperty("template_id")]
        public string template_id { get; set; }

        /// <summary>
        /// 结构规则
        /// </summary>
        [JsonProperty("structure")]
        public StructureRules structure { get; set; }

        /// <summary>
        /// 装饰规则列表
        /// </summary>
        [JsonProperty("decorations")]
        public List<DecorationRule> decorations { get; set; }

        /// <summary>
        /// 材料调色板
        /// </summary>
        [JsonProperty("materials")]
        public MaterialPalette materials { get; set; }

        /// <summary>
        /// 模板修改指令（当template_id指定时使用）
        /// </summary>
        [JsonProperty("modifications")]
        public TemplateModifications modifications { get; set; }
    }

    #region 结构规则

    /// <summary>
    /// 结构规则 - 定义建筑的基本结构
    /// </summary>
    public class StructureRules
    {
        /// <summary>
        /// 框架规则
        /// </summary>
        [JsonProperty("frame")]
        public FrameRule frame { get; set; }

        /// <summary>
        /// 墙壁规则
        /// </summary>
        [JsonProperty("walls")]
        public WallRule walls { get; set; }

        /// <summary>
        /// 楼层规则列表
        /// </summary>
        [JsonProperty("floors")]
        public List<FloorRule> floors { get; set; }

        /// <summary>
        /// 屋顶规则
        /// </summary>
        [JsonProperty("roof")]
        public RoofRule roof { get; set; }

        /// <summary>
        /// 房间规则列表
        /// </summary>
        [JsonProperty("rooms")]
        public List<RoomRule> rooms { get; set; }
    }

    /// <summary>
    /// 框架规则
    /// </summary>
    public class FrameRule
    {
        [JsonProperty("material")]
        public string material { get; set; }

        [JsonProperty("material_id")]
        public int? material_id { get; set; }

        [JsonProperty("thickness")]
        public int thickness { get; set; } = 1;

        /// <summary>
        /// 框架模式: rectangle(矩形), arch(拱形), dome(圆顶), pagoda(宝塔)
        /// </summary>
        [JsonProperty("pattern")]
        public string pattern { get; set; } = "rectangle";

        /// <summary>
        /// 角落装饰
        /// </summary>
        [JsonProperty("corners")]
        public List<CornerStyle> corners { get; set; }
    }

    /// <summary>
    /// 墙壁规则
    /// </summary>
    public class WallRule
    {
        [JsonProperty("primary_material")]
        public string primary_material { get; set; }

        [JsonProperty("primary_wall_id")]
        public int? primary_wall_id { get; set; }

        [JsonProperty("secondary_material")]
        public string secondary_material { get; set; }

        [JsonProperty("secondary_wall_id")]
        public int? secondary_wall_id { get; set; }

        /// <summary>
        /// 填充模式: solid(实心), checkered(棋盘), striped(条纹), gradient(渐变)
        /// </summary>
        [JsonProperty("fill_pattern")]
        public string fill_pattern { get; set; } = "solid";

        [JsonProperty("paint")]
        public int? paint { get; set; }
    }

    /// <summary>
    /// 楼层规则
    /// </summary>
    public class FloorRule
    {
        [JsonProperty("y_start")]
        public int y_start { get; set; }

        [JsonProperty("y_end")]
        public int y_end { get; set; }

        [JsonProperty("material")]
        public string material { get; set; }

        [JsonProperty("material_id")]
        public int? material_id { get; set; }

        /// <summary>
        /// 模式: solid(实心), checkered(棋盘), bordered(带边框)
        /// </summary>
        [JsonProperty("pattern")]
        public string pattern { get; set; } = "solid";

        [JsonProperty("thickness")]
        public int thickness { get; set; } = 1;
    }

    /// <summary>
    /// 屋顶规则
    /// </summary>
    public class RoofRule
    {
        /// <summary>
        /// 屋顶类型: gable(人字), flat(平顶), dome(圆顶), pagoda(宝塔)
        /// </summary>
        [JsonProperty("type")]
        public string type { get; set; }

        [JsonProperty("material")]
        public string material { get; set; }

        [JsonProperty("material_id")]
        public int? material_id { get; set; }

        /// <summary>
        /// 屋檐伸出格数
        /// </summary>
        [JsonProperty("overhang")]
        public int overhang { get; set; } = 1;

        [JsonProperty("paint")]
        public int? paint { get; set; }
    }

    /// <summary>
    /// 房间规则
    /// </summary>
    public class RoomRule
    {
        [JsonProperty("x")]
        public int x { get; set; }

        [JsonProperty("y")]
        public int y { get; set; }

        [JsonProperty("width")]
        public int width { get; set; }

        [JsonProperty("height")]
        public int height { get; set; }

        /// <summary>
        /// 房间类型: living(起居), bedroom(卧室), workshop(工坊), storage(储藏)
        /// </summary>
        [JsonProperty("type")]
        public string type { get; set; }

        [JsonProperty("has_door")]
        public bool has_door { get; set; } = true;
    }

    /// <summary>
    /// 角落样式
    /// </summary>
    public class CornerStyle
    {
        [JsonProperty("position")]
        public string position { get; set; } // top_left, top_right, bottom_left, bottom_right

        [JsonProperty("material")]
        public string material { get; set; }

        [JsonProperty("material_id")]
        public int? material_id { get; set; }
    }

    #endregion

    #region 装饰规则

    /// <summary>
    /// 装饰规则
    /// </summary>
    public class DecorationRule
    {
        /// <summary>
        /// 装饰类型: lantern(灯笼), torch(火炬), painting(画), statue(雕像), plant(植物)
        /// </summary>
        [JsonProperty("type")]
        public string type { get; set; }

        [JsonProperty("material")]
        public string material { get; set; }

        [JsonProperty("tile_id")]
        public int? tile_id { get; set; }

        /// <summary>
        /// 放置方式: corners(角落), center(中心), edges(边缘), every_n_tiles(等距), custom(自定义)
        /// </summary>
        [JsonProperty("placement")]
        public string placement { get; set; }

        /// <summary>
        /// 等距放置时的间隔
        /// </summary>
        [JsonProperty("spacing")]
        public int? spacing { get; set; }

        /// <summary>
        /// 自定义位置列表
        /// </summary>
        [JsonProperty("positions")]
        public List<Position> positions { get; set; }

        [JsonProperty("count")]
        public int count { get; set; } = 1;
    }

    /// <summary>
    /// 位置坐标
    /// </summary>
    public class Position
    {
        [JsonProperty("x")]
        public int x { get; set; }

        [JsonProperty("y")]
        public int y { get; set; }
    }

    #endregion

    #region 材料调色板

    /// <summary>
    /// 材料调色板
    /// </summary>
    public class MaterialPalette
    {
        [JsonProperty("primary_tile")]
        public MaterialRef primary_tile { get; set; }

        [JsonProperty("secondary_tile")]
        public MaterialRef secondary_tile { get; set; }

        [JsonProperty("primary_wall")]
        public MaterialRef primary_wall { get; set; }

        [JsonProperty("secondary_wall")]
        public MaterialRef secondary_wall { get; set; }

        [JsonProperty("accent_tile")]
        public MaterialRef accent_tile { get; set; }

        [JsonProperty("floor_tile")]
        public MaterialRef floor_tile { get; set; }

        [JsonProperty("roof_tile")]
        public MaterialRef roof_tile { get; set; }

        [JsonProperty("paint")]
        public PaintScheme paint { get; set; }
    }

    /// <summary>
    /// 材料引用
    /// </summary>
    public class MaterialRef
    {
        [JsonProperty("name")]
        public string name { get; set; }

        [JsonProperty("id")]
        public int? id { get; set; }

        [JsonProperty("paint")]
        public int? paint { get; set; }
    }

    /// <summary>
    /// 油漆方案
    /// </summary>
    public class PaintScheme
    {
        [JsonProperty("primary_paint")]
        public int? primary_paint { get; set; }

        [JsonProperty("shadow_paint")]
        public int? shadow_paint { get; set; }

        [JsonProperty("accent_paint")]
        public int? accent_paint { get; set; }
    }

    #endregion

    #region 模板修改

    /// <summary>
    /// 模板修改指令
    /// </summary>
    public class TemplateModifications
    {
        /// <summary>
        /// 缩放比例 (0.5 = 缩小一半)
        /// </summary>
        [JsonProperty("scale")]
        public float? scale { get; set; }

        /// <summary>
        /// X轴镜像
        /// </summary>
        [JsonProperty("mirror_x")]
        public bool? mirror_x { get; set; }

        /// <summary>
        /// Y轴镜像
        /// </summary>
        [JsonProperty("mirror_y")]
        public bool? mirror_y { get; set; }

        /// <summary>
        /// 材料替换列表
        /// </summary>
        [JsonProperty("material_replacements")]
        public List<MaterialReplacement> material_replacements { get; set; }

        /// <summary>
        /// 新增结构
        /// </summary>
        [JsonProperty("additions")]
        public List<StructureAddition> additions { get; set; }

        /// <summary>
        /// 移除结构
        /// </summary>
        [JsonProperty("removals")]
        public List<StructureRemoval> removals { get; set; }
    }

    /// <summary>
    /// 材料替换
    /// </summary>
    public class MaterialReplacement
    {
        [JsonProperty("original")]
        public string original { get; set; }

        [JsonProperty("replacement")]
        public string replacement { get; set; }

        [JsonProperty("replacement_id")]
        public int? replacement_id { get; set; }
    }

    /// <summary>
    /// 结构新增
    /// </summary>
    public class StructureAddition
    {
        [JsonProperty("type")]
        public string type { get; set; } // room, floor, decoration

        [JsonProperty("data")]
        public object data { get; set; }
    }

    /// <summary>
    /// 结构移除
    /// </summary>
    public class StructureRemoval
    {
        [JsonProperty("type")]
        public string type { get; set; }

        [JsonProperty("position")]
        public Position position { get; set; }
    }

    #endregion
}
