using System.ComponentModel;
using Terraria.ModLoader.Config;
using trab.Core;

namespace trab.Config
{
    /// <summary>
    /// Agent生成模式
    /// </summary>
    public enum AgentMode
    {
        /// <summary>
        /// 单Agent模式 - 一次API调用生成完整建筑（默认，更稳定）
        /// </summary>
        SingleAgent,
        /// <summary>
        /// 多Agent协作模式 - 规划Agent + 5个模块Agent并行生成（更快，但可能出错）
        /// </summary>
        MultiAgent
    }

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

        [DefaultValue(AgentMode.SingleAgent)]
        [Label("Agent生成模式")]
        [Tooltip("SingleAgent: 单次API调用，稳定可靠\nMultiAgent: 多Agent协作，模块化生成")]
        public AgentMode AgentGenerationMode { get; set; } = AgentMode.SingleAgent;

        [DefaultValue(false)]
        [Label("使用Pipeline生成模式")]
        [Tooltip("启用4阶段生成流程：需求分析→材料检索→材料选择→设计生成。需要配合SingleAgent模式使用。")]
        public bool UsePipelineMode { get; set; } = false;

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