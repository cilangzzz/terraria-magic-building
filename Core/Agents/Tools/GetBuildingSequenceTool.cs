using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using trab.Core.KnowledgeBase;

namespace trab.Core.Agents.Tools
{
    /// <summary>
    /// 获取建造顺序工具
    /// 返回指定模板的建造步骤
    /// </summary>
    public class GetBuildingSequenceTool : BaseAgentTool
    {
        public override string Name => "get_building_sequence";

        public override string Description => "获取建筑的建造顺序。返回分步骤的建造指南，包括每步的动作、材料和说明。用于了解如何按顺序建造。";

        public override JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["template_id"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "模板ID"
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

            // 获取建造顺序
            var sequence = kb.Buildings.GetBuildingSequence(templateId);

            if (sequence == null || sequence.Count == 0)
            {
                // 返回默认建造顺序
                sequence = GetDefaultSequence();
            }

            var result = new
            {
                template_id = templateId,
                sequence = sequence,
                total_steps = sequence.Count,
                note = "建议按顺序执行，先框架后填充，最后装饰"
            };

            return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(result)));
        }

        private System.Collections.Generic.List<BuildingStep> GetDefaultSequence()
        {
            return new System.Collections.Generic.List<BuildingStep>
            {
                new BuildingStep
                {
                    step = 1,
                    action = "frame",
                    materials = new System.Collections.Generic.List<string> { "Stone", "Stone Slab" },
                    note = "搭建主体框架，确定建筑轮廓"
                },
                new BuildingStep
                {
                    step = 2,
                    action = "walls",
                    materials = new System.Collections.Generic.List<string> { "Gray Brick Wall", "Wood Wall" },
                    note = "铺设背景墙，填充框架内部"
                },
                new BuildingStep
                {
                    step = 3,
                    action = "floor",
                    materials = new System.Collections.Generic.List<string> { "Wood", "Gray Brick" },
                    note = "铺设地板和分隔楼层"
                },
                new BuildingStep
                {
                    step = 4,
                    action = "roof",
                    materials = new System.Collections.Generic.List<string> { "Stone Slab", "Wood" },
                    note = "建造屋顶结构"
                },
                new BuildingStep
                {
                    step = 5,
                    action = "doors",
                    materials = new System.Collections.Generic.List<string> { "Door", "Platform" },
                    note = "安装入口和通道"
                },
                new BuildingStep
                {
                    step = 6,
                    action = "lights",
                    materials = new System.Collections.Generic.List<string> { "Torch", "Chandelier" },
                    note = "布置光源，确保照明"
                },
                new BuildingStep
                {
                    step = 7,
                    action = "furniture",
                    materials = new System.Collections.Generic.List<string> { "Table", "Chair", "Bed" },
                    note = "摆放家具，满足NPC需求"
                },
                new BuildingStep
                {
                    step = 8,
                    action = "decor",
                    materials = new System.Collections.Generic.List<string> { "Painting", "Statue", "Plant" },
                    note = "添加装饰细节"
                }
            };
        }
    }
}
