# Config - 配置层

本目录包含模组的客户端配置实现。

---

## 文件列表

| 文件 | 功能描述 |
|------|----------|
| AIBuildingConfig.cs | 模组客户端配置类，定义API和生成参数 |

---

## 配置项说明

### AIBuildingConfig

继承 `ModConfig`，客户端配置（ConfigScope.ClientSide）。

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| ApiKey | string | "" | API密钥，必须设置才能使用AI功能 |
| ServiceProvider | AIServiceType | DeepSeek | API服务商选择 |
| CustomEndpoint | string | "" | 自定义API端点（Custom模式使用） |
| ModelName | string | "deepseek-v4-flash" | AI模型名称 |
| BuildOffsetX | int | 5 | 建筑生成X方向偏移（格） |
| BuildOffsetY | int | 0 | 建筑生成Y方向偏移（格） |
| MaxBuildingSize | int | 50 | 建筑最大尺寸限制（格） |

---

## API服务商

`AIServiceType` 枚举:

| 值 | 说明 | 端点 |
|----|------|------|
| OpenAI | OpenAI官方API | https://api.openai.com/v1/chat/completions |
| Claude | Anthropic官方API | https://api.anthropic.com/v1/messages |
| DashScope | 阿里云DashScope | https://dashscope.aliyuncs.com/compatible-mode/v1/messages |
| DeepSeek | DeepSeek API | https://api.deepseek.com/v1/chat/completions |
| Custom | 自定义端点 | 用户配置 |

---

## 配置访问

通过模组实例获取配置:

```csharp
// 获取配置实例
var config = ModContent.GetInstance<AIBuildingConfig>();

// 检查API是否已配置
bool isConfigured = !string.IsNullOrEmpty(config.ApiKey);

// 获取服务商
AIServiceType provider = config.ServiceProvider;
```

---

## 配置界面

在游戏中通过以下方式访问:
1. 按 ESC 打开菜单
2. 点击 "模组配置"
3. 找到 "trab Config"
4. 设置 API 密钥和其他参数

---

## 相关文件

- [AIApiService.cs](../Core/AIApiService.cs) - 使用配置初始化API服务
- [AIBuildingPlayer.cs](../Players/AIBuildingPlayer.cs) - 读取配置进行生成
- [BuildCommands.cs](../Commands/BuildCommands.cs) - 命令中使用配置