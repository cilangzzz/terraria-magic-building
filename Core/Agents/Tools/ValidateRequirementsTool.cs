using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using trab.Core.KnowledgeBase;

namespace trab.Core.Agents.Tools
{
    /// <summary>
    /// NPC房屋验证工具
    /// 验证建筑设计是否满足NPC房屋要求
    /// </summary>
    public class ValidateRequirementsTool : BaseAgentTool
    {
        public override string Name => "validate_requirements";

        public override string Description => "验证建筑是否满足NPC房屋要求。返回验证结果和缺失项。用于确保建筑可用于NPC入住。";

        public override JObject InputSchema => new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["width"] = new JObject
                {
                    ["type"] = "integer",
                    ["description"] = "建筑宽度"
                },
                ["height"] = new JObject
                {
                    ["type"] = "integer",
                    ["description"] = "建筑高度"
                },
                ["has_light"] = new JObject
                {
                    ["type"] = "boolean",
                    ["description"] = "是否有光源（火把、蜡烛、吊灯等）"
                },
                ["has_door"] = new JObject
                {
                    ["type"] = "boolean",
                    ["description"] = "是否有门（门、平台、活板门）"
                },
                ["has_table"] = new JObject
                {
                    ["type"] = "boolean",
                    ["description"] = "是否有平坦表面（桌子、工作台、梳妆台等）"
                },
                ["has_chair"] = new JObject
                {
                    ["type"] = "boolean",
                    ["description"] = "是否有舒适物品（椅子、床、沙发等）"
                },
                ["has_walls"] = new JObject
                {
                    ["type"] = "boolean",
                    ["description"] = "是否有背景墙（玩家可放置的墙）"
                }
            }
        };

        public override Task<ToolResult> ExecuteAsync(JObject input, KnowledgeBaseManager kb)
        {
            int? width = GetIntParam(input, "width");
            int? height = GetIntParam(input, "height");
            bool? hasLight = GetBoolParam(input, "has_light");
            bool? hasDoor = GetBoolParam(input, "has_door");
            bool? hasTable = GetBoolParam(input, "has_table");
            bool? hasChair = GetBoolParam(input, "has_chair");
            bool? hasWalls = GetBoolParam(input, "has_walls");

            // 验证规则
            const int MIN_WIDTH = 7;
            const int MIN_HEIGHT = 7;
            const int MIN_AREA = 60;

            var issues = new System.Collections.Generic.List<string>();
            var warnings = new System.Collections.Generic.List<string>();

            // 尺寸验证
            int actualWidth = width ?? 0;
            int actualHeight = height ?? 0;
            int area = actualWidth * actualHeight;

            if (actualWidth < MIN_WIDTH)
            {
                issues.Add($"宽度不足: 当前{actualWidth}格，最少需要{MIN_WIDTH}格");
            }
            if (actualHeight < MIN_HEIGHT)
            {
                issues.Add($"高度不足: 当前{actualHeight}格，最少需要{MIN_HEIGHT}格");
            }
            if (area < MIN_AREA)
            {
                warnings.Add($"面积偏小: 当前{area}格，建议至少{MIN_AREA}格");
            }

            // 家具验证
            if (hasLight == false)
            {
                issues.Add("缺少光源: 需要火把、蜡烛、吊灯、营火等");
            }
            if (hasDoor == false)
            {
                issues.Add("缺少入口: 需要门、平台或活板门");
            }
            if (hasTable == false)
            {
                issues.Add("缺少平坦表面: 需要桌子、工作台、梳妆台或钢琴");
            }
            if (hasChair == false)
            {
                issues.Add("缺少舒适物品: 需要椅子、床或沙发");
            }
            if (hasWalls == false)
            {
                issues.Add("缺少背景墙: 需要玩家可放置的墙壁（天然墙壁无效）");
            }

            bool isValid = issues.Count == 0;

            var result = new
            {
                is_valid = isValid,
                size_check = new
                {
                    width = actualWidth,
                    height = actualHeight,
                    area = area,
                    min_width = MIN_WIDTH,
                    min_height = MIN_HEIGHT,
                    min_area = MIN_AREA,
                    passed = actualWidth >= MIN_WIDTH && actualHeight >= MIN_HEIGHT
                },
                furniture_check = new
                {
                    has_light = hasLight ?? false,
                    has_door = hasDoor ?? false,
                    has_table = hasTable ?? false,
                    has_chair = hasChair ?? false,
                    has_walls = hasWalls ?? false
                },
                issues = issues,
                warnings = warnings,
                summary = isValid
                    ? "✓ 建筑满足NPC房屋要求"
                    : $"✗ 建筑不满足NPC房屋要求，有{issues.Count}个问题需要解决"
            };

            return Task.FromResult(ToolResult.Success(JsonConvert.SerializeObject(result)));
        }
    }
}
