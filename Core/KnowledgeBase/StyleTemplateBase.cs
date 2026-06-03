using System.Collections.Generic;
using System.Linq;

namespace trab.Core.KnowledgeBase
{
    /// <summary>
    /// 风格模板知识库
    /// </summary>
    public class StyleTemplateBase
    {
        private Dictionary<string, StyleTemplate> _styles;

        public StyleTemplateBase()
        {
            InitDefaultData();
        }

        private void InitDefaultData()
        {
            _styles = new Dictionary<string, StyleTemplate>
            {
                ["medieval"] = new StyleTemplate {
                    name = "中世纪风格",
                    display_name = "Medieval",
                    description = "经典欧洲中世纪建筑风格，适合城堡、村庄和堡垒。使用灰砖作为主要材料。"
                },
                ["fantasy"] = new StyleTemplate {
                    name = "奇幻风格",
                    display_name = "Fantasy",
                    description = "魔法与幻想风格，适合精灵建筑、魔法塔。使用珍珠石和玻璃。"
                },
                ["natural"] = new StyleTemplate {
                    name = "自然风格",
                    display_name = "Natural",
                    description = "与自然融合的建筑风格，适合树屋、田园小屋。使用木材和泥土。"
                },
                ["steampunk"] = new StyleTemplate {
                    name = "蒸汽朋克风格",
                    display_name = "Steampunk",
                    description = "工业革命风格，适合工厂、机械建筑。使用铜砖和铁砖。"
                },
                ["asian"] = new StyleTemplate {
                    name = "东方风格",
                    display_name = "Asian",
                    description = "中日式建筑风格，适合茶室、寺庙。使用王朝木。"
                },
                ["snow"] = new StyleTemplate {
                    name = "冰雪风格",
                    display_name = "Snow",
                    description = "冬季风格，适合雪屋、冰堡。使用雪块和冰块。"
                },
                ["desert"] = new StyleTemplate {
                    name = "沙漠风格",
                    display_name = "Desert",
                    description = "沙漠和古埃及风格，适合金字塔。使用砂岩。"
                },
                ["modern"] = new StyleTemplate {
                    name = "现代风格",
                    display_name = "Modern",
                    description = "现代简约风格，适合现代住宅。使用花岗岩和大理石。"
                },
                ["dark"] = new StyleTemplate {
                    name = "黑暗风格",
                    display_name = "Dark",
                    description = "腐化/猩红/地狱风格，适合邪恶建筑。使用黑檀石和黑曜石。"
                }
            };
        }

        public StyleTemplate GetTemplate(string styleName, string buildingType = null)
        {
            if (styleName == null) return null;
            _styles.TryGetValue(styleName.ToLower(), out var template);
            return template;
        }

        public List<string> GetAllStyleNames() => _styles.Keys.ToList();

        public int StyleCount => _styles.Count;
    }

    public class StyleTemplate
    {
        public string name { get; set; }
        public string display_name { get; set; }
        public string description { get; set; }
    }
}