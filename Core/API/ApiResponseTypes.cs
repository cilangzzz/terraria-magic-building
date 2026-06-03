using Newtonsoft.Json;
using System.Collections.Generic;

namespace trab.Core.API
{
    /// <summary>
    /// OpenAI格式API响应结构
    /// </summary>
    public class OpenAIResponse
    {
        public OpenAIChoice[] choices { get; set; }
    }

    public class OpenAIChoice
    {
        public OpenAIMessage message { get; set; }
        public string finish_reason { get; set; }
    }

    public class OpenAIMessage
    {
        public string role { get; set; }
        public string content { get; set; }
        public string reasoning_content { get; set; }
        public OpenAIToolCall[] tool_calls { get; set; }
    }

    public class OpenAIToolCall
    {
        public string id { get; set; }
        public string type { get; set; }
        public OpenAIFunction function { get; set; }
    }

    public class OpenAIFunction
    {
        public string name { get; set; }
        public string arguments { get; set; }
    }

    /// <summary>
    /// Claude格式API响应结构
    /// </summary>
    public class ClaudeAgentResponse
    {
        public string id { get; set; }
        public string type { get; set; }
        public string role { get; set; }
        public List<ClaudeContentItem> content { get; set; }
        public string model { get; set; }
        public string stop_reason { get; set; }
        public string stop_sequence { get; set; }
        public ClaudeUsage usage { get; set; }
    }

    public class ClaudeContentItem
    {
        public string type { get; set; }
        public string text { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public object input { get; set; }
    }

    public class ClaudeUsage
    {
        public int input_tokens { get; set; }
        public int output_tokens { get; set; }
    }

    /// <summary>
    /// 工具调用结果
    /// </summary>
    public class ToolResult
    {
        public bool IsError { get; set; }
        public string Content { get; set; }
    }
}