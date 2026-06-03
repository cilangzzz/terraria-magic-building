using System.Collections.Generic;
using System.Linq;

namespace trab.Core.KnowledgeBase
{
    /// <summary>
    /// 家具规则知识库
    /// </summary>
    public class FurnitureRuleBase
    {
        private Dictionary<string, FurnitureInfo> _furniture;

        public FurnitureRuleBase()
        {
            InitDefaultData();
        }

        private void InitDefaultData()
        {
            _furniture = new Dictionary<string, FurnitureInfo>
            {
                ["WorkBench"] = new FurnitureInfo { tile_id = 17, display_name = "工作台", category = "surface", width = 2, height = 1, npc_function = "crafting" },
                ["Tables"] = new FurnitureInfo { tile_id = 87, display_name = "桌子", category = "surface", width = 3, height = 1, npc_function = "flat_surface" },
                ["Chairs"] = new FurnitureInfo { tile_id = 88, display_name = "椅子", category = "comfort", width = 1, height = 2, npc_function = "comfort" },
                ["Beds"] = new FurnitureInfo { tile_id = 89, display_name = "床", category = "comfort", width = 4, height = 2, npc_function = "comfort" },
                ["Chests"] = new FurnitureInfo { tile_id = 21, display_name = "宝箱", category = "storage", width = 2, height = 1, npc_function = "storage" },
                ["Torches"] = new FurnitureInfo { tile_id = 4, display_name = "火把", category = "light", width = 1, height = 1, npc_function = "light_source" },
                ["ClosedDoor"] = new FurnitureInfo { tile_id = 10, display_name = "门", category = "door", width = 1, height = 3, npc_function = "door" }
            };
        }

        public List<KeyValuePair<string, FurnitureInfo>> SearchFurniture(string roomType, string npcType = null)
        {
            return _furniture.ToList();
        }

        public int FurnitureCount => _furniture.Count;
    }

    public class FurnitureInfo
    {
        public int tile_id { get; set; }
        public string display_name { get; set; }
        public string category { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string npc_function { get; set; }
    }
}