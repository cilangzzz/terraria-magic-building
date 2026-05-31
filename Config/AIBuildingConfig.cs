using System.ComponentModel;
using Terraria.ModLoader.Config;
using trab.Core;

namespace trab.Config
{
    public class AIBuildingConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue("")]
        public string ApiKey { get; set; } = "";

        [DefaultValue(AIServiceType.DeepSeek)]
        public AIServiceType ServiceProvider { get; set; } = AIServiceType.DeepSeek;

        [DefaultValue("")]
        public string CustomEndpoint { get; set; } = "";

        [DefaultValue("deepseek-v4-flash")]
        public string ModelName { get; set; } = "deepseek-v4-flash";

        [DefaultValue(5)]
        [Range(0, 50)]
        public int BuildOffsetX { get; set; } = 5;

        [DefaultValue(0)]
        [Range(-50, 50)]
        public int BuildOffsetY { get; set; } = 0;

        [DefaultValue(50)]
        [Range(10, 100)]
        public int MaxBuildingSize { get; set; } = 50;
    }
}