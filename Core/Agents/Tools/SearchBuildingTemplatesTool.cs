using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using trab.Core.KnowledgeBase;

namespace trab.Core.Agents.Tools
{
    /// <summary>
    /// 检索建筑模板工具
    /// 根据风格、类型、特征等条件检索相似的建筑模板
    /// </summary>
    public class SearchBuildingTemplatesTool : BaseAgentTool
    {
        public override string Name => "search_building_templates";

        public override string Description => "检索与用户需求相似的建筑模板。返回模板摘要，包含ID、尺寸、风格标签、描述。用于找到参考建筑。";

        public override JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["style"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "建筑风格: asian(中式), medieval(中世纪), fantasy(奇幻), snow(雪地), desert(沙漠), modern(现代), natural(自然)"
                },
                ["building_type"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "建筑类型: house(住宅), castle(城堡), tower(塔楼), shop(商店), temple(神庙), workshop(工坊)"
                },
                ["features"] = new JObject
                {
                    ["type"] = "array",
                    ["items"] = new JObject { ["type"] = "string" },
                    ["description"] = "特征标签: lantern(灯笼), gold(金色), marble(大理石), multi-story(多层)"
                },
                ["min_width"] = new JObject
                {
                    ["type"] = "integer",
                    ["description"] = "最小宽度"
                },
                ["max_width"] = new JObject
                {
                    ["type"] = "integer",
                    ["description"] = "最大宽度"
                },
                ["min_height"] = new JObject
                {
                    ["type"] = "integer",
                    ["description"] = "最小高度"
                },
                ["max_height"] = new JObject
                {
                    ["type"] = "integer",
                    ["description"] = "最大高度"
                },
                ["top_k"] = new JObject
                {
                    ["type"] = "integer",
                    ["description"] = "返回数量，默认3",
                    ["default"] = 3
                }
            },
            ["required"] = new JArray { "style" }
        };

        public override Task<ToolResult> ExecuteAsync(JObject input, KnowledgeBaseManager kb)
        {
            string style = GetStringParam(input, "style");
            string buildingType = GetStringParam(input, "building_type");
            var features = GetStringArrayParam(input, "features");
            int? minWidth = GetIntParam(input, "min_width");
            int? maxWidth = GetIntParam(input, "max_width");
            int? minHeight = GetIntParam(input, "min_height");
            int? maxHeight = GetIntParam(input, "max_height");
            int topK = GetIntParam(input, "top_k") ?? 3;

            // 构建检索条件
            var criteria = new TemplateSearchCriteria
            {
                style = style,
                buildingType = buildingType,
                features = features,
                minWidth = minWidth ?? 0,
                maxWidth = maxWidth ?? 0,
                minHeight = minHeight ?? 0,
                maxHeight = maxHeight ?? 0
            };

            // 检索建筑模板
            var entities = kb.Buildings.SearchTemplates(criteria, topK);

            // 生成精简摘要
            var summaries = entities.Select(e => new
            {
                id = e.id,
                dimensions = new
                {
                    width = e.dimensions?.width ?? 0,
                    height = e.dimensions?.height ?? 0
                },
                style_tags = e.style_tags,
                features = new
                {
                    type = e.features?.type,
                    style = e.features?.style,
                    structure = e.features?.structure
                },
                summary = e.summary,
                score = e.score
            }).ToList();

            var result = new
            {
                templates = summaries,
                total = summaries.Count,
                note = summaries.Count > 0
                    ? "使用 get_template_details 获取完整信息"
                    : "未找到匹配模板，建议使用 search_materials 获取材料后直接生成"
            };

            return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(result)));
        }
    }
}
