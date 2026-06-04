using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using trab.Core.KnowledgeBase;
using trab.Data;

namespace trab.Core.Agents.Tools
{
    /// <summary>
    /// 生成设计规则工具
    /// 这是终止工具，AI调用此工具表示设计完成
    /// </summary>
    public class GenerateDesignRulesTool : BaseAgentTool
    {
        public override string Name => "generate_design_rules";

        public override string Description => "生成建筑设计规则。这是终止工具，调用此工具表示设计完成。输出BuildingRules格式的JSON，程序将根据规则生成建筑。";

        public override JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["name"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "建筑名称"
                },
                ["width"] = new JObject
                {
                    ["type"] = "integer",
                    ["description"] = "建筑宽度（格）",
                    ["minimum"] = 1
                },
                ["height"] = new JObject
                {
                    ["type"] = "integer",
                    ["description"] = "建筑高度（格）",
                    ["minimum"] = 1
                },
                ["style"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "建筑风格"
                },
                ["template_id"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "参考模板ID（可选）"
                },
                ["structure"] = new JObject
                {
                    ["type"] = "object",
                    ["description"] = "结构规则",
                    ["properties"] = new JObject
                    {
                        ["frame"] = new JObject { ["type"] = "object", ["description"] = "框架规则" },
                        ["walls"] = new JObject { ["type"] = "object", ["description"] = "墙壁规则" },
                        ["floors"] = new JObject { ["type"] = "array", ["description"] = "楼层规则列表" },
                        ["roof"] = new JObject { ["type"] = "object", ["description"] = "屋顶规则" }
                    }
                },
                ["decorations"] = new JObject
                {
                    ["type"] = "array",
                    ["description"] = "装饰规则列表",
                    ["items"] = new JObject { ["type"] = "object" }
                },
                ["materials"] = new JObject
                {
                    ["type"] = "object",
                    ["description"] = "材料调色板"
                },
                ["modifications"] = new JObject
                {
                    ["type"] = "object",
                    ["description"] = "模板修改指令（当template_id指定时使用）"
                }
            },
            ["required"] = new JArray { "name", "width", "height" }
        };

        public override Task<ToolResult> ExecuteAsync(JObject input, KnowledgeBaseManager kb)
        {
            // 解析BuildingRules
            BuildingRules rules;
            try
            {
                rules = input.ToObject<BuildingRules>();
            }
            catch (System.Exception ex)
            {
                return Task.FromResult(ToolResult.Error($"解析设计规则失败: {ex.Message}"));
            }

            // 验证基本参数（仅验证最小值）
            if (rules.width < 1)
            {
                return Task.FromResult(ToolResult.Error($"宽度无效: {rules.width}，应大于0"));
            }
            if (rules.height < 1)
            {
                return Task.FromResult(ToolResult.Error($"高度无效: {rules.height}，应大于0"));
            }

            // 估算材料
            int estimatedTiles = EstimateTileCount(rules);
            int estimatedWalls = rules.width * rules.height;

            var result = new
            {
                status = "success",
                rules = rules,
                estimate = new
                {
                    active_tiles = estimatedTiles,
                    tiles_with_wall = estimatedWalls,
                    size = new
                    {
                        width = rules.width,
                        height = rules.height,
                        area = rules.width * rules.height
                    }
                },
                message = "设计规则已生成，等待程序化生成器构建建筑...",
                next_step = "ProceduralBuilder.GenerateFromRules()"
            };

            // 存储元数据，供后续ProceduralBuilder使用
            var metadata = new Dictionary<string, object>
            {
                ["building_rules"] = rules
            };

            return Task.FromResult(new ToolResult
            {
                IsError = false,
                Content = JsonConvert.SerializeObject(result),
                Metadata = metadata,
                ToolName = Name
            });
        }

        private int EstimateTileCount(BuildingRules rules)
        {
            int count = 0;

            // 框架
            if (rules.structure?.frame != null)
            {
                int thickness = rules.structure.frame.thickness > 0 ? rules.structure.frame.thickness : 1;
                count += rules.width * 2 * thickness; // 顶部和底部
                count += rules.height * 2 * thickness; // 左右两侧
            }

            // 楼层
            if (rules.structure?.floors != null)
            {
                foreach (var floor in rules.structure.floors)
                {
                    int floorHeight = floor.y_end - floor.y_start;
                    count += rules.width * (floorHeight > 0 ? floorHeight : 1);
                }
            }

            // 屋顶
            if (rules.structure?.roof != null)
            {
                int overhang = rules.structure.roof.overhang > 0 ? rules.structure.roof.overhang : 1;
                count += (rules.width + overhang * 2) * 2;
            }

            // 装饰
            if (rules.decorations != null)
            {
                foreach (var decor in rules.decorations)
                {
                    count += decor.count > 0 ? decor.count : 1;
                }
            }

            return count;
        }
    }
}
