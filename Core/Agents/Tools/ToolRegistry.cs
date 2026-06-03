using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using trab.Core.KnowledgeBase;

namespace trab.Core.Agents.Tools
{
    /// <summary>
    /// Agent工具接口
    /// </summary>
    public interface IAgentTool
    {
        /// <summary>
        /// 工具名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 工具描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 输入参数Schema (JSON Schema格式)
        /// </summary>
        JObject InputSchema { get; }

        /// <summary>
        /// 执行工具
        /// </summary>
        /// <param name="input">输入参数</param>
        /// <param name="kb">知识库管理器</param>
        /// <returns>工具执行结果</returns>
        Task<ToolResult> ExecuteAsync(JObject input, KnowledgeBaseManager kb);
    }

    /// <summary>
    /// 工具执行结果
    /// </summary>
    public class ToolResult
    {
        /// <summary>
        /// 是否出错
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// 结果内容 (JSON字符串)
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 附加元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// 工具名称
        /// </summary>
        public string ToolName { get; set; }

        /// <summary>
        /// 工具调用ID
        /// </summary>
        public string ToolCallId { get; set; }

        public static ToolResult Success(string content, Dictionary<string, object> metadata = null)
        {
            return new ToolResult
            {
                IsError = false,
                Content = content,
                Metadata = metadata
            };
        }

        public static ToolResult Error(string errorMessage)
        {
            return new ToolResult
            {
                IsError = true,
                Content = Newtonsoft.Json.JsonConvert.SerializeObject(new { error = errorMessage })
            };
        }
    }

    /// <summary>
    /// 工具注册中心
    /// 管理所有Agent工具的注册、定义和执行
    /// </summary>
    public class ToolRegistry
    {
        private readonly Dictionary<string, IAgentTool> _tools;

        public ToolRegistry()
        {
            _tools = new Dictionary<string, IAgentTool>();
        }

        /// <summary>
        /// 注册工具
        /// </summary>
        public void RegisterTool(IAgentTool tool)
        {
            if (string.IsNullOrEmpty(tool.Name))
            {
                throw new ArgumentException("工具名称不能为空");
            }
            _tools[tool.Name] = tool;
        }

        /// <summary>
        /// 批量注册工具
        /// </summary>
        public void RegisterTools(IEnumerable<IAgentTool> tools)
        {
            foreach (var tool in tools)
            {
                RegisterTool(tool);
            }
        }

        /// <summary>
        /// 获取工具
        /// </summary>
        public IAgentTool GetTool(string name)
        {
            return _tools.TryGetValue(name, out var tool) ? tool : null;
        }

        /// <summary>
        /// 检查工具是否存在
        /// </summary>
        public bool HasTool(string name)
        {
            return _tools.ContainsKey(name);
        }

        /// <summary>
        /// 获取所有工具名称
        /// </summary>
        public IEnumerable<string> GetToolNames()
        {
            return _tools.Keys;
        }

        /// <summary>
        /// 执行工具
        /// </summary>
        public async Task<ToolResult> ExecuteToolAsync(string name, JObject input, KnowledgeBaseManager kb)
        {
            var tool = GetTool(name);
            if (tool == null)
            {
                return ToolResult.Error($"未知工具: {name}");
            }

            try
            {
                return await tool.ExecuteAsync(input, kb);
            }
            catch (Exception ex)
            {
                return ToolResult.Error($"工具执行错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取Anthropic格式的工具定义
        /// </summary>
        public JArray GetAnthropicToolDefinitions()
        {
            var tools = new JArray();

            foreach (var tool in _tools.Values)
            {
                tools.Add(new JObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["input_schema"] = tool.InputSchema
                });
            }

            return tools;
        }

        /// <summary>
        /// 获取OpenAI格式的工具定义
        /// </summary>
        public JArray GetOpenAIToolDefinitions()
        {
            var tools = new JArray();

            foreach (var tool in _tools.Values)
            {
                tools.Add(new JObject
                {
                    ["type"] = "function",
                    ["function"] = new JObject
                    {
                        ["name"] = tool.Name,
                        ["description"] = tool.Description,
                        ["parameters"] = tool.InputSchema
                    }
                });
            }

            return tools;
        }

        /// <summary>
        /// 创建默认工具注册表
        /// </summary>
        public static ToolRegistry CreateDefault()
        {
            var registry = new ToolRegistry();

            // 注册核心工具
            registry.RegisterTool(new SearchBuildingTemplatesTool());
            registry.RegisterTool(new GetTemplateDetailsTool());
            registry.RegisterTool(new SearchMaterialsTool());
            registry.RegisterTool(new GetMaterialRecommendationTool());
            registry.RegisterTool(new ValidateRequirementsTool());
            registry.RegisterTool(new GenerateDesignRulesTool());
            registry.RegisterTool(new GetBuildingSequenceTool());

            return registry;
        }
    }

    #region 工具基类

    /// <summary>
    /// 工具基类，提供常用辅助方法
    /// </summary>
    public abstract class BaseAgentTool : IAgentTool
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract JObject InputSchema { get; }

        public abstract Task<ToolResult> ExecuteAsync(JObject input, KnowledgeBaseManager kb);

        /// <summary>
        /// 安全获取字符串参数
        /// </summary>
        protected string GetStringParam(JObject input, string key, string defaultValue = null)
        {
            return input[key]?.ToString() ?? defaultValue;
        }

        /// <summary>
        /// 安全获取整数参数
        /// </summary>
        protected int? GetIntParam(JObject input, string key)
        {
            return input[key]?.Value<int?>();
        }

        /// <summary>
        /// 安全获取布尔参数
        /// </summary>
        protected bool? GetBoolParam(JObject input, string key)
        {
            return input[key]?.Value<bool?>();
        }

        /// <summary>
        /// 安全获取数组参数
        /// </summary>
        protected List<string> GetStringArrayParam(JObject input, string key)
        {
            var arr = input[key] as JArray;
            if (arr == null) return null;

            var result = new List<string>();
            foreach (var item in arr)
            {
                result.Add(item.ToString());
            }
            return result;
        }

        /// <summary>
        /// 创建简单的JSON Schema
        /// </summary>
        protected JObject CreateSchema(Dictionary<string, (string type, string description, bool required)> properties)
        {
            var props = new JObject();
            var required = new JArray();

            foreach (var kvp in properties)
            {
                props[kvp.Key] = new JObject
                {
                    ["type"] = kvp.Value.type,
                    ["description"] = kvp.Value.description
                };

                if (kvp.Value.required)
                {
                    required.Add(kvp.Key);
                }
            }

            return new JObject
            {
                ["type"] = "object",
                ["properties"] = props,
                ["required"] = required
            };
        }
    }

    #endregion

    #region 工具类引用

    // 工具实现在独立文件中
    // - SearchBuildingTemplatesTool.cs
    // - GetTemplateDetailsTool.cs
    // - SearchMaterialsTool.cs
    // - GetMaterialRecommendationTool.cs
    // - ValidateRequirementsTool.cs
    // - GenerateDesignRulesTool.cs
    // - GetBuildingSequenceTool.cs

    #endregion
}
