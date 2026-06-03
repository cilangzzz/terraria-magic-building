using System;
using System.Threading;
using System.Threading.Tasks;
using Terraria.ModLoader;
using trab.Config;
using trab.Core.API;
using trab.Core.KnowledgeBase;
using trab.Data;

namespace trab.Core.Agents
{
    /// <summary>
    /// AI Agent建筑生成服务 - 统一入口
    /// 使用TrueAgentCore进行建筑生成
    /// </summary>
    public class AIAgentService
    {
        /// <summary>
        /// Agent主入口 - 使用TrueAgentCore生成建筑
        /// </summary>
        public async Task<TEditSchDesign> GenerateBuildingAsync(
            string userPrompt,
            Action<string, int> progressCallback = null,
            CancellationToken ct = default)
        {
            try
            {
                progressCallback?.Invoke("初始化AI Agent...", 0);

                // 初始化知识库
                KnowledgeBaseManager.Instance.Initialize();

                // 获取配置
                var config = ModContent.GetInstance<AIBuildingConfig>();
                string apiKey = config.ApiKey;
                AIServiceType serviceType = config.ServiceProvider;
                string modelName = config.ModelName;

                // 创建TrueAgentCore实例
                var agentCore = new TrueAgentCore(apiKey, serviceType, modelName);

                trab.Instance?.Logger.Info($"TrueAgentCore启动 - 模型: {modelName}, 服务: {serviceType}");

                // 执行Agent循环
                var result = await agentCore.RunAgentLoop(userPrompt, progressCallback, ct);

                if (result == null)
                {
                    progressCallback?.Invoke("生成失败: Agent未能返回有效结果", 0);
                    trab.Instance?.Logger.Warn("TrueAgentCore返回空结果");
                    return null;
                }

                progressCallback?.Invoke("建筑生成完成!", 100);
                return result;
            }
            catch (Exception ex)
            {
                progressCallback?.Invoke($"错误: {ex.Message}", 0);
                trab.Instance?.Logger.Error($"Agent错误: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
    }
}
