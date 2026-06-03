using System;
using System.Net.Http;

namespace trab.Core.API
{
    /// <summary>
    /// API服务基类 - 提供HTTP客户端和基础配置
    /// </summary>
    public abstract class ApiServiceBase : IDisposable
    {
        protected HttpClient _httpClient;
        protected string _apiKey;
        protected string _apiEndpoint;
        protected AIServiceType _serviceType;
        protected string _modelName;
        protected string _customEndpoint;
        protected bool _useOpenAIFormat;

        protected ApiServiceBase(string apiKey, AIServiceType serviceType, string modelName, string customEndpoint = null)
        {
            _apiKey = apiKey;
            _serviceType = serviceType;
            _modelName = modelName;
            _customEndpoint = customEndpoint;
            _httpClient = new HttpClient();
            ConfigureApiClient();
        }

        protected virtual void ConfigureApiClient()
        {
            _httpClient.DefaultRequestHeaders.Clear();

            // 如果提供了自定义端点，优先使用
            if (!string.IsNullOrEmpty(_customEndpoint))
            {
                _apiEndpoint = _customEndpoint;
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);
                _useOpenAIFormat = true;
                return;
            }

            if (_serviceType == AIServiceType.Claude)
            {
                _apiEndpoint = "https://api.anthropic.com/v1/messages";
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                _useOpenAIFormat = false;
            }
            else if (_serviceType == AIServiceType.DeepSeek)
            {
                _apiEndpoint = "https://api.deepseek.com/v1/chat/completions";
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);
                _useOpenAIFormat = true;
            }
            else if (_serviceType == AIServiceType.DashScope)
            {
                _apiEndpoint = "https://dashscope.aliyuncs.com/compatible-mode/v1/messages";
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                _useOpenAIFormat = false;
            }
            else
            {
                _apiEndpoint = "https://api.openai.com/v1/chat/completions";
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);
                _useOpenAIFormat = true;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
