using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using trab.Core.KnowledgeBase;

namespace trab.Core.Agents.Tools
{
    /// <summary>
    /// 获取模板详情工具
    /// 返回指定建筑模板的完整信息，包括材料列表和建造顺序
    /// </summary>
    public class GetTemplateDetailsTool : BaseAgentTool
    {
        public override string Name => "get_template_details";

        public override string Description => "获取指定建筑模板的完整信息，包括材料列表、建造顺序、NPC房屋验证结果。用于了解模板细节并决定修改策略。";

        public override JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["template_id"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "模板ID，从 search_building_templates 返回的ID"
                }
            },
            ["required"] = new JArray { "template_id" }
        };

        public override Task<ToolResult> ExecuteAsync(JObject input, KnowledgeBaseManager kb)
        {
            string templateId = GetStringParam(input, "template_id");

            if (string.IsNullOrEmpty(templateId))
            {
                return Task.FromResult(ToolResult.Error("template_id 参数为空"));
            }

            // 获取建筑实体
            var entity = kb.Buildings.GetBuilding(templateId);
            if (entity == null)
            {
                return Task.FromResult(ToolResult.Error($"未找到模板: {templateId}"));
            }

            // 获取详细数据
            var detail = kb.Buildings.GetBuildingDetail(templateId);

            // 构建完整信息
            var result = new
            {
                id = entity.id,
                source = entity.source,
                dimensions = new
                {
                    width = entity.dimensions?.width ?? 0,
                    height = entity.dimensions?.height ?? 0
                },
                features = entity.features,
                materials = entity.materials,
                functions = entity.functions,
                style_tags = entity.style_tags,
                building_sequence = entity.building_sequence,
                summary = entity.summary,
                // 详细数据（如果有）
                detail = detail != null ? new
                {
                    total_tiles = detail.total_tiles,
                    active_tiles = detail.active_tiles,
                    unique_tile_types = detail.unique_tile_types,
                    unique_wall_types = detail.unique_wall_types,
                    tile_distribution = detail.tile_distribution,
                    wall_distribution = detail.wall_distribution
                } : null
            };

            return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(result)));
        }
    }
}
