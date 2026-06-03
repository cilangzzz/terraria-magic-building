using System;
using System.Threading;
using System.Threading.Tasks;
using Terraria.ModLoader;
using trab.Config;
using trab.Core.Agents.MultiAgent;
using trab.Core.Agents.SingleAgent;
using trab.Core.API;
using trab.Core.KnowledgeBase;
using trab.Data;

namespace trab.Core.Agents
{
    /// <summary>
    /// AI Agent建筑生成服务 - 统一入口，根据配置选择SingleAgent或MultiAgent模式
    /// </summary>
    public class AIAgentService
    {
        /// <summary>
        /// Agent主入口 - 根据配置选择SingleAgent或MultiAgent模式，返回TEditSch格式
        /// </summary>
        public async Task<TEditSchDesign> GenerateBuildingAsync(
            string userPrompt,
            Action<string, int> progressCallback = null,
            CancellationToken ct = default)
        {
            try
            {
                progressCallback?.Invoke("Agent启动...", 0);

                // 初始化知识库
                KnowledgeBaseManager.Instance.Initialize();

                // 获取配置
                var config = ModContent.GetInstance<AIBuildingConfig>();
                string apiKey = config.ApiKey;
                AIServiceType serviceType = config.ServiceProvider;
                string modelName = config.ModelName;

                // 根据配置选择生成模式
                if (config.UsePipelineMode || config.AgentGenerationMode == AgentMode.SingleAgent)
                {
                    trab.Instance?.Logger.Info("使用SingleAgent模式生成建筑");
                    if (config.UsePipelineMode)
                    {
                        progressCallback?.Invoke("Pipeline模式启动...", 0);
                    }

                    var singleAgent = new BuildingSingleAgent(apiKey, serviceType, modelName, config.MaxBuildingSize);
                    return await singleAgent.GenerateBuildingAsync(userPrompt, progressCallback, ct);
                }
                else
                {
                    trab.Instance?.Logger.Info("使用MultiAgent协作模式生成建筑");

                    var multiAgent = new BuildingMultiAgent(apiKey, serviceType, modelName);
                    return await multiAgent.GenerateBuildingAsync(userPrompt, progressCallback, ct);
                }
            }
            catch (Exception ex)
            {
                progressCallback?.Invoke($"错误: {ex.Message}", 0);
                trab.Instance?.Logger.Error($"Agent错误: {ex.Message}");
                return null;
            }
        }
    }
}