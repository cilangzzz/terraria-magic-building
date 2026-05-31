# 泰拉瑞亚模组集成AI技术方案

本文档详细介绍如何在泰拉瑞亚tModLoader模组中集成AI功能，包括HTTP请求、聊天界面、数据处理和安全性等方面。

---

## 目录

1. [HTTP请求调用外部AI API](#1-http请求调用外部ai-api)
2. [游戏内聊天界面实现](#2-游戏内聊天界面实现)
3. [AI返回的结构化建筑数据处理](#3-ai返回的结构化建筑数据处理)
4. [流式输出处理](#4-流式输出处理)
5. [安全性考虑](#5-安全性考虑)
6. [完整实现示例](#6-完整实现示例)
7. [参考资源](#7-参考资源)

---

## 1. HTTP请求调用外部AI API

### 1.1 tModLoader中的网络请求能力

tModLoader基于.NET框架，可以使用标准的`HttpClient`或`WebClient`进行HTTP请求。由于游戏运行在主线程上，**必须使用异步方式**避免阻塞游戏。

### 1.2 推荐方案：使用HttpClient

```csharp
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class AIApiService
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private string _apiKey;
    private string _apiEndpoint = "https://api.openai.com/v1/chat/completions";

    public AIApiService(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    /// <summary>
    /// 异步发送请求到AI API
    /// </summary>
    public async Task<AIResponse> SendChatRequestAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = "你是一个泰拉瑞亚建筑助手..." },
                new { role = "user", content = userMessage }
            },
            temperature = 0.7
        };

        var json = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(_apiEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<AIResponse>(responseJson);
        }
        catch (Exception ex)
        {
            Mod.Logger.Error($"AI API请求失败: {ex.Message}");
            return null;
        }
    }
}

// 响应数据结构
public class AIResponse
{
    public string id { get; set; }
    public Choice[] choices { get; set; }
}

public class Choice
{
    public Message message { get; set; }
    public string finish_reason { get; set; }
}

public class Message
{
    public string role { get; set; }
    public string content { get; set; }
}
```

### 1.3 在模组中正确调用异步方法

**重要**：不能直接在游戏主线程中调用`async`方法，需要使用`Task.Run`包装：

```csharp
public class AIBuildingMod : Mod
{
    private AIApiService _aiService;

    public override void Load()
    {
        _aiService = new AIApiService(Config.ApiKey);
    }

    // 从UI或命令中调用
    public void RequestBuildingDesign(string prompt, Action<BuildingDesign> callback)
    {
        // 在后台线程执行，避免阻塞游戏
        Task.Run(async () =>
        {
            try
            {
                var response = await _aiService.SendChatRequestAsync(prompt);

                // 切回主线程执行回调
                Main.QueueMainThreadAction(() =>
                {
                    var design = ParseBuildingDesign(response);
                    callback?.Invoke(design);
                });
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"请求失败: {ex.Message}");
            }
        });
    }
}
```

### 1.4 Claude API集成示例

```csharp
public class ClaudeApiService
{
    private const string API_ENDPOINT = "https://api.anthropic.com/v1/messages";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ClaudeApiService(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<string> SendMessageAsync(string userMessage, CancellationToken ct = default)
    {
        var requestBody = new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 4096,
            messages = new[]
            {
                new { role = "user", content = userMessage }
            }
        };

        var json = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(API_ENDPOINT, content, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(ct);
    }
}
```

---

## 2. 游戏内聊天界面实现

### 2.1 方案一：使用ModCommand（聊天命令）

最简单的方式是通过游戏内置聊天框接收用户输入：

```csharp
using Terraria.ModLoader;

[Command("aibuild")]
public class AIBuildCommand : ModCommand
{
    public override CommandType Type => CommandType.Chat;

    public override string Usage => "/aibuild <建筑描述> - 使用AI生成建筑";

    public override string Description => "使用AI生成建筑结构";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length == 0)
        {
            caller.Reply("请输入建筑描述，例如: /aibuild 一座中世纪风格的城堡", Color.Red);
            return;
        }

        string prompt = string.Join(" ", args);
        caller.Reply($"正在请求AI生成建筑: {prompt}...", Color.Yellow);

        // 调用AI服务
        var mod = caller.Player.GetModPlayer<AIBuildingPlayer>();
        mod.RequestBuildingDesign(prompt);
    }
}
```

### 2.2 方案二：自定义UI聊天面板

创建一个完全自定义的聊天界面：

```csharp
using Terraria.UI;
using Terraria.GameContent.UI.Elements;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

public class AIChatUI : UIState
{
    private UIPanel _mainPanel;
    private UITextPanel<string> _inputField;
    private UIText _outputText;
    private List<ChatMessage> _messageHistory = new List<ChatMessage>();

    public override void OnInitialize()
    {
        // 主面板
        _mainPanel = new UIPanel();
        _mainPanel.SetPadding(10);
        _mainPanel.Width.Set(400f, 0f);
        _mainPanel.Height.Set(500f, 0f);
        _mainPanel.Left.Set(Main.screenWidth / 2 - 200f, 0f);
        _mainPanel.Top.Set(Main.screenHeight / 2 - 250f, 0f);
        _mainPanel.BackgroundColor = new Color(30, 30, 50, 200);

        Append(_mainPanel);

        // 标题
        var title = new UIText("AI建筑助手", 1.2f);
        title.Left.Set(10f, 0f);
        title.Top.Set(10f, 0f);
        _mainPanel.Append(title);

        // 消息显示区域
        var messageArea = new UIPanel();
        messageArea.Width.Set(-20f, 1f);
        messageArea.Height.Set(-100f, 1f);
        messageArea.Left.Set(0f, 0f);
        messageArea.Top.Set(40f, 0f);
        messageArea.BackgroundColor = new Color(20, 20, 30, 150);
        _mainPanel.Append(messageArea);

        _outputText = new UIText("");
        _outputText.Width.Set(-10f, 1f);
        _outputText.Left.Set(5f, 0f);
        _outputText.Top.Set(5f, 0f);
        _outputText.IsWrapped = true;
        messageArea.Append(_outputText);

        // 输入框
        _inputField = new UITextPanel<string>("");
        _inputField.SetPadding(5);
        _inputField.Width.Set(-80f, 1f);
        _inputField.Height.Set(40f, 0f);
        _inputField.Left.Set(0f, 0f);
        _inputField.Top.Set(-50f, 1f);
        _inputField.BackgroundColor = new Color(50, 50, 70);
        _mainPanel.Append(_inputField);

        // 发送按钮
        var sendButton = new UITextPanel<string>("发送");
        sendButton.SetPadding(5);
        sendButton.Width.Set(70f, 0f);
        sendButton.Height.Set(40f, 0f);
        sendButton.Left.Set(-75f, 1f);
        sendButton.Top.Set(-50f, 1f);
        sendButton.OnClick += OnSendButtonClick;
        _mainPanel.Append(sendButton);
    }

    private void OnSendButtonClick(UIMouseEvent evt, UIElement listeningElement)
    {
        string message = _inputField.Text;
        if (string.IsNullOrWhiteSpace(message)) return;

        AddMessage("你", message, Color.LightBlue);
        _inputField.SetText("");

        // 发送到AI API
        ProcessUserInput(message);
    }

    public void AddMessage(string sender, string message, Color color)
    {
        _messageHistory.Add(new ChatMessage(sender, message, color));
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var sb = new StringBuilder();
        foreach (var msg in _messageHistory)
        {
            sb.AppendLine($"[{msg.Sender}]: {msg.Text}");
        }
        _outputText.SetText(sb.ToString());
    }

    private void ProcessUserInput(string input)
    {
        AddMessage("AI", "正在思考...", Color.Yellow);

        Task.Run(async () =>
        {
            var response = await AIApiService.SendChatRequestAsync(input);

            Main.QueueMainThreadAction(() =>
            {
                _messageHistory.RemoveAt(_messageHistory.Count - 1);
                AddMessage("AI", response, Color.LightGreen);
            });
        });
    }
}

public struct ChatMessage
{
    public string Sender;
    public string Text;
    public Color Color;

    public ChatMessage(string sender, string text, Color color)
    {
        Sender = sender;
        Text = text;
        Color = color;
    }
}
```

### 2.3 使用UIInputTextField实现文本输入

```csharp
using Terraria.GameContent.UI.Elements;

public class ChatInputField : UIElement
{
    private UIInputTextField _inputField;
    private string _currentText = "";

    public event Action<string> OnTextSubmitted;

    public override void OnInitialize()
    {
        _inputField = new UIInputTextField("输入消息...");
        _inputField.SetText(_currentText);
        _inputField.Width.Set(300f, 0f);
        _inputField.Height.Set(30f, 0f);

        _inputField.OnTextChange += (text) =>
        {
            _currentText = text;
        };

        Append(_inputField);
    }

    public void SubmitText()
    {
        if (!string.IsNullOrWhiteSpace(_currentText))
        {
            OnTextSubmitted?.Invoke(_currentText);
            _inputField.SetText("");
            _currentText = "";
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // 监听Enter键提交
        if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Enter))
        {
            SubmitText();
        }
    }
}
```

### 2.4 UI显示与隐藏管理

```csharp
public class AIChatUISystem : ModSystem
{
    private AIChatUI _chatUI;
    private UserInterface _userInterface;

    public override void Load()
    {
        _chatUI = new AIChatUI();
        _chatUI.Activate();
        _userInterface = new UserInterface();
        _userInterface.SetState(_chatUI);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        _userInterface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
        if (mouseTextIndex != -1)
        {
            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "AIBuilding: Chat UI",
                () =>
                {
                    _userInterface.Draw(Main.spriteBatch, gameTime);
                    return true;
                },
                InterfaceScaleType.UI
            ));
        }
    }

    public void ToggleUI()
    {
        if (_userInterface.CurrentState == null)
            _userInterface.SetState(_chatUI);
        else
            _userInterface.SetState(null);
    }
}
```

---

## 3. AI返回的结构化建筑数据处理

### 3.1 定义建筑数据结构

```csharp
using Newtonsoft.Json;
using System.Collections.Generic;

/// <summary>
/// AI返回的建筑设计数据
/// </summary>
public class BuildingDesign
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("width")]
    public int Width { get; set; }

    [JsonProperty("height")]
    public int Height { get; set; }

    [JsonProperty("tiles")]
    public List<TileData> Tiles { get; set; }

    [JsonProperty("walls")]
    public List<WallData> Walls { get; set; }

    [JsonProperty("furniture")]
    public List<FurnitureData> Furniture { get; set; }
}

public class TileData
{
    [JsonProperty("x")]
    public int X { get; set; }

    [JsonProperty("y")]
    public int Y { get; set; }

    [JsonProperty("type")]
    public string TileType { get; set; } // 如 "Stone", "Wood", "GoldBrick"

    [JsonProperty("style")]
    public int Style { get; set; }
}

public class WallData
{
    [JsonProperty("x")]
    public int X { get; set; }

    [JsonProperty("y")]
    public int Y { get; set; }

    [JsonProperty("type")]
    public string WallType { get; set; }
}

public class FurnitureData
{
    [JsonProperty("x")]
    public int X { get; set; }

    [JsonProperty("y")]
    public int Y { get; set; }

    [JsonProperty("type")]
    public string FurnitureType { get; set; } // 如 "WorkBench", "Chest", "Bed"
}
```

### 3.2 配置AI输出结构化JSON

使用OpenAI的Structured Outputs或Claude的工具调用功能确保返回格式化的JSON：

```csharp
// OpenAI Structured Output 配置
public class AIBuildingRequest
{
    public static object CreateRequest(string userPrompt)
    {
        return new
        {
            model = "gpt-4o",
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = @"你是一个泰拉瑞亚建筑设计AI。用户会描述他们想要的建筑，你需要返回一个JSON格式的建筑设计。
建筑设计应包含:
- name: 建筑名称
- description: 简短描述
- width: 宽度(格数)
- height: 高度(格数)
- tiles: 方块列表，每个包含x,y坐标和类型
- walls: 墙壁列表
- furniture: 家具列表

方块类型使用英文，如: Stone, Wood, GoldBrick, DemoniteBrick 等。
家具类型如: WorkBench, Chest, Bed, Chair, Table, Furnace, Anvil 等。"
                },
                new { role = "user", content = userPrompt }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "building_design",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string" },
                            description = new { type = "string" },
                            width = new { type = "integer" },
                            height = new { type = "integer" },
                            tiles = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        x = new { type = "integer" },
                                        y = new { type = "integer" },
                                        type = new { type = "string" },
                                        style = new { type = "integer" }
                                    },
                                    required = new[] { "x", "y", "type" }
                                }
                            }
                        },
                        required = new[] { "name", "width", "height", "tiles" }
                    }
                }
            }
        };
    }
}
```

### 3.3 解析AI响应并执行建筑

```csharp
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

public class BuildingExecutor
{
    private Mod _mod;

    // 方块名称到ID的映射
    private Dictionary<string, int> _tileNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        { "Stone", TileID.Stone },
        { "Wood", TileID.WoodBlock },
        { "GoldBrick", TileID.GoldBrick },
        { "DemoniteBrick", TileID.DemoniteBrick },
        { "Glass", TileID.Glass },
        { "Iron", TileID.IronBrick },
        { "Pearlstone", TileID.PearlstoneBrick },
        { "Ebonstone", TileID.EbonstoneBrick },
        // 添加更多映射...
    };

    // 墙壁名称到ID的映射
    private Dictionary<string, int> _wallNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        { "StoneWall", WallID.Stone },
        { "WoodWall", WallID.Wood },
        { "GlassWall", WallID.Glass },
        // 添加更多映射...
    };

    // 家具名称到放置方法的映射
    private Dictionary<string, int> _furnitureToItemId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        { "WorkBench", ItemID.WorkBench },
        { "Chest", ItemID.Chest },
        { "Bed", ItemID.Bed },
        { "Chair", ItemID.Chair },
        { "Table", ItemID.Table },
        { "Furnace", ItemID.Furnace },
        { "Anvil", ItemID.IronAnvil },
        // 添加更多映射...
    };

    public BuildingExecutor(Mod mod)
    {
        _mod = mod;
    }

    /// <summary>
    /// 解析AI返回的JSON并执行建筑
    /// </summary>
    public BuildingDesign ParseDesign(string jsonResponse)
    {
        try
        {
            return JsonConvert.DeserializeObject<BuildingDesign>(jsonResponse);
        }
        catch (Exception ex)
        {
            _mod.Logger.Error($"解析建筑设计失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 在指定位置生成建筑
    /// </summary>
    public void BuildAtLocation(BuildingDesign design, int startX, int startY, Player player)
    {
        if (design == null) return;

        // 放置墙壁（先放墙壁，因为方块会覆盖墙壁）
        foreach (var wall in design.Walls)
        {
            int wallType = GetWallId(wall.WallType);
            if (wallType > 0)
            {
                WorldGen.PlaceWall(startX + wall.X, startY + wall.Y, wallType);
            }
        }

        // 放置方块
        foreach (var tile in design.Tiles)
        {
            int tileType = GetTileId(tile.TileType);
            if (tileType > 0)
            {
                WorldGen.PlaceTile(startX + tile.X, startY + tile.Y, tileType, style: tile.Style);
            }
        }

        // 放置家具
        foreach (var furniture in design.Furniture)
        {
            PlaceFurniture(furniture, startX + furniture.X, startY + furniture.Y, player);
        }

        // 刷新世界显示
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            // 单人模式直接刷新
            for (int i = startX; i < startX + design.Width; i++)
            {
                for (int j = startY; j < startY + design.Height; j++)
                {
                    WorldGen.SquareTileFrame(i, j);
                }
            }
        }
        else
        {
            // 多人模式需要同步
            NetMessage.SendTileSquare(-1, startX, startY, design.Width, design.Height);
        }
    }

    private int GetTileId(string typeName)
    {
        if (_tileNameToId.TryGetValue(typeName, out int id))
            return id;

        // 尝试从模组中查找
        if (ModLoader.TryGetMod(typeName, out Mod mod))
        {
            // 模组方块的查找逻辑
        }

        return -1;
    }

    private int GetWallId(string typeName)
    {
        if (_wallNameToId.TryGetValue(typeName, out int id))
            return id;
        return -1;
    }

    private void PlaceFurniture(FurnitureData furniture, int x, int y, Player player)
    {
        if (_furnitureToItemId.TryGetValue(furniture.FurnitureType, out int itemId))
        {
            // 创建临时物品并放置
            Item item = new Item();
            item.SetDefaults(itemId);

            // 放置家具的逻辑（具体实现取决于家具类型）
            // 某些家具需要特定条件才能放置
        }
    }
}
```

### 3.4 完整的建筑生成流程

```csharp
public class AIBuildingPlayer : ModPlayer
{
    private BuildingExecutor _executor;
    private AIApiService _aiService;

    public override void Initialize()
    {
        _executor = new BuildingExecutor(Mod);
        _aiService = new AIApiService(ModContent.GetInstance<AIBuildingConfig>().ApiKey);
    }

    public void RequestBuildingDesign(string prompt)
    {
        Main.NewText("正在生成建筑设计...", Color.Yellow);

        Task.Run(async () =>
        {
            try
            {
                var response = await _aiService.SendChatRequestAsync(prompt);

                if (response?.choices?.Length > 0)
                {
                    string jsonContent = response.choices[0].message.content;

                    Main.QueueMainThreadAction(() =>
                    {
                        ProcessBuildingResponse(jsonContent);
                    });
                }
            }
            catch (Exception ex)
            {
                Main.QueueMainThreadAction(() =>
                {
                    Main.NewText($"AI请求失败: {ex.Message}", Color.Red);
                });
            }
        });
    }

    private void ProcessBuildingResponse(string jsonResponse)
    {
        var design = _executor.ParseDesign(jsonResponse);

        if (design != null)
        {
            Main.NewText($"生成建筑: {design.Name}", Color.Green);
            Main.NewText($"尺寸: {design.Width}x{design.Height}", Color.White);

            // 在玩家位置附近生成
            int startX = (int)Player.position.X / 16 + 5;
            int startY = (int)Player.position.Y / 16;

            _executor.BuildAtLocation(design, startX, startY, Player);
            Main.NewText("建筑已完成!", Color.Green);
        }
        else
        {
            Main.NewText("建筑数据解析失败", Color.Red);
        }
    }
}
```

---

## 4. 流式输出处理

### 4.1 为什么需要流式输出

AI API（如OpenAI、Claude）支持流式响应（Streaming），可以逐字符或逐块返回内容。这对于游戏内实时显示AI回复非常重要，避免长时间等待。

### 4.2 SSE（Server-Sent Events）协议

AI API的流式输出通常使用SSE协议。响应格式如下：

```
data: {"id":"chatcmpl-xxx","choices":[{"delta":{"content":"你"},"index":0}]}

data: {"id":"chatcmpl-xxx","choices":[{"delta":{"content":"好"},"index":0}]}

data: [DONE]
```

### 4.3 C# HttpClient流式处理实现

```csharp
using System.Net.Http;
using System.IO;
using System.Text;

public class StreamingAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public event Action<string> OnTextReceived;  // 收到文本片段时触发
    public event Action OnStreamComplete;        // 流结束时触发
    public event Action<Exception> OnError;      // 出错时触发

    public StreamingAIService(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    /// <summary>
    /// 流式请求AI API
    /// </summary>
    public async Task StreamChatAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            stream = true  // 启用流式输出
        };

        var json = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
            using (var reader = new StreamReader(stream))
            {
                StringBuilder fullResponse = new StringBuilder();

                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();

                    if (string.IsNullOrEmpty(line)) continue;
                    if (!line.StartsWith("data: ")) continue;

                    var data = line.Substring(6); // 去掉 "data: " 前缀

                    if (data == "[DONE]")
                    {
                        OnStreamComplete?.Invoke();
                        break;
                    }

                    try
                    {
                        var chunk = JsonConvert.DeserializeObject<StreamChunk>(data);

                        if (chunk?.choices?.Length > 0)
                        {
                            var delta = chunk.choices[0].delta;
                            if (delta?.content != null)
                            {
                                fullResponse.Append(delta.content);

                                // 在主线程触发事件
                                var text = delta.content;
                                Main.QueueMainThreadAction(() =>
                                {
                                    OnTextReceived?.Invoke(text);
                                });
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // 忽略解析错误
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Main.QueueMainThreadAction(() =>
            {
                OnError?.Invoke(ex);
            });
        }
    }
}

// 流式响应数据结构
public class StreamChunk
{
    public string id { get; set; }
    public StreamChoice[] choices { get; set; }
}

public class StreamChoice
{
    public StreamDelta delta { get; set; }
    public int index { get; set; }
    public string finish_reason { get; set; }
}

public class StreamDelta
{
    public string content { get; set; }
}
```

### 4.4 在UI中使用流式输出

```csharp
public class StreamingChatUI : UIState
{
    private UIText _responseText;
    private StringBuilder _currentResponse = new StringBuilder();
    private StreamingAIService _streamingService;

    public override void OnInitialize()
    {
        _responseText = new UIText("");
        _responseText.Width.Set(-20f, 1f);
        _responseText.IsWrapped = true;
        Append(_responseText);

        _streamingService = new StreamingAIService(Config.ApiKey);
        _streamingService.OnTextReceived += OnTextChunkReceived;
        _streamingService.OnStreamComplete += OnStreamFinished;
        _streamingService.OnError += OnStreamError;
    }

    private void OnTextChunkReceived(string text)
    {
        // 追加文本到当前响应
        _currentResponse.Append(text);
        _responseText.SetText(_currentResponse.ToString());
    }

    private void OnStreamFinished()
    {
        Main.NewText("AI响应完成", Color.Green);
    }

    private void OnStreamError(Exception ex)
    {
        Main.NewText($"错误: {ex.Message}", Color.Red);
    }

    public void SendStreamingRequest(string prompt)
    {
        _currentResponse.Clear();
        _responseText.SetText("正在思考...");

        // 取消之前的请求（如果有）
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        Task.Run(() => _streamingService.StreamChatAsync(prompt, _cancellationTokenSource.Token));
    }

    private CancellationTokenSource _cancellationTokenSource;

    public override void OnDeactivate()
    {
        _cancellationTokenSource?.Cancel();
        base.OnDeactivate();
    }
}
```

### 4.5 Claude API流式处理

```csharp
public class ClaudeStreamingService
{
    private const string API_ENDPOINT = "https://api.anthropic.com/v1/messages";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public event Action<string> OnTextReceived;
    public event Action OnStreamComplete;

    public ClaudeStreamingService(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
    }

    public async Task StreamMessageAsync(string prompt, CancellationToken ct = default)
    {
        var requestBody = new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 4096,
            stream = true,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, API_ENDPOINT)
        {
            Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using (var stream = await response.Content.ReadAsStreamAsync(ct))
        using (var reader = new StreamReader(stream))
        {
            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrEmpty(line)) continue;

                // Claude使用不同的SSE格式
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);

                    try
                    {
                        var eventData = JsonConvert.DeserializeObject<ClaudeStreamEvent>(data);

                        if (eventData?.type == "content_block_delta" &&
                            eventData.delta?.type == "text_delta")
                        {
                            var text = eventData.delta.text;
                            Main.QueueMainThreadAction(() => OnTextReceived?.Invoke(text));
                        }
                        else if (eventData?.type == "message_stop")
                        {
                            Main.QueueMainThreadAction(() => OnStreamComplete?.Invoke());
                        }
                    }
                    catch { }
                }
            }
        }
    }
}

// Claude流式事件结构
public class ClaudeStreamEvent
{
    public string type { get; set; }
    public int index { get; set; }
    public ClaudeDelta delta { get; set; }
}

public class ClaudeDelta
{
    public string type { get; set; }
    public string text { get; set; }
}
```

---

## 5. 安全性考虑

### 5.1 API密钥管理

**绝对不要将API密钥硬编码在模组代码中！**

#### 方案一：配置文件存储

```csharp
using Terraria.ModLoader.Config;
using System.ComponentModel;

// 配置类定义
public class AIBuildingConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("API设置")]
    [Tooltip("输入你的API密钥（不会上传到服务器）")]
    [PasswordPropertyText]  // 在UI中显示为密码形式
    public string ApiKey { get; set; } = "";

    [Tooltip("选择AI服务商")]
    public AIServiceProvider ServiceProvider { get; set; } = AIServiceProvider.OpenAI;

    [Tooltip("API端点（可选，用于自定义服务）")]
    [DefaultValue("")]
    public string CustomEndpoint { get; set; } = "";

    [Tooltip("最大Token数量")]
    [Range(100, 8000)]
    public int MaxTokens { get; set; } = 2000;

    [Tooltip("温度参数（创造性程度）")]
    [Range(0.0, 2.0)]
    public float Temperature { get; set; } = 0.7f;
}

public enum AIServiceProvider
{
    OpenAI,
    Claude,
    Custom
}
```

配置文件位置：`Documents/My Games/Terraria/tModLoader/ModConfigs/AIBuildingConfig.json`

#### 方案二：环境变量（开发环境）

```csharp
public class SecureApiKeyProvider
{
    private const string ENV_VAR_NAME = "TERRARIA_AI_API_KEY";
    private const string CONFIG_FILE_NAME = "ai_api_key.txt";

    public static string GetApiKey()
    {
        // 优先使用环境变量
        string envKey = Environment.GetEnvironmentVariable(ENV_VAR_NAME);
        if (!string.IsNullOrEmpty(envKey))
        {
            return envKey;
        }

        // 其次使用本地配置文件
        string configPath = Path.Combine(Main.SavePath, CONFIG_FILE_NAME);
        if (File.Exists(configPath))
        {
            return File.ReadAllText(configPath).Trim();
        }

        // 返回空，让用户配置
        return "";
    }

    public static void SaveApiKey(string apiKey)
    {
        string configPath = Path.Combine(Main.SavePath, CONFIG_FILE_NAME);
        Directory.CreateDirectory(Path.GetDirectoryName(configPath));
        File.WriteAllText(configPath, apiKey);

        // 设置文件权限（仅Windows）
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var fileInfo = new FileInfo(configPath);
            fileInfo.Attributes |= FileAttributes.Hidden;
        }
    }
}
```

### 5.2 API密钥加密存储

```csharp
using System.Security.Cryptography;
using System.Text;

public class SecureStorage
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("AIBuildingMod_Salt_2024");

    /// <summary>
    /// 加密存储API密钥（Windows DPAPI）
    /// </summary>
    public static void StoreApiKeySecure(string apiKey)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: 使用DPAPI加密
            byte[] encrypted = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(apiKey),
                Entropy,
                DataProtectionScope.CurrentUser
            );

            string configPath = GetSecureConfigPath();
            File.WriteAllBytes(configPath, encrypted);
        }
        else
        {
            // 其他平台：使用简单混淆（建议用户自行保护）
            string obfuscated = Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey));
            File.WriteAllText(GetSecureConfigPath(), obfuscated);
        }
    }

    /// <summary>
    /// 解密读取API密钥
    /// </summary>
    public static string RetrieveApiKey()
    {
        string configPath = GetSecureConfigPath();
        if (!File.Exists(configPath)) return "";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            byte[] encrypted = File.ReadAllBytes(configPath);
            byte[] decrypted = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        else
        {
            string obfuscated = File.ReadAllText(configPath);
            return Encoding.UTF8.GetString(Convert.FromBase64String(obfuscated));
        }
    }

    private static string GetSecureConfigPath()
    {
        return Path.Combine(Main.SavePath, "ai_config.bin");
    }
}
```

### 5.3 网络安全

```csharp
public class SecureHttpClient
{
    private static HttpClient _sharedClient;

    public static HttpClient GetClient()
    {
        if (_sharedClient == null)
        {
            _sharedClient = new HttpClient(new HttpClientHandler
            {
                // 强制TLS 1.2/1.3
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                // 允许重定向
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5
            });

            // 设置超时
            _sharedClient.Timeout = TimeSpan.FromSeconds(30);

            // 安全头
            _sharedClient.DefaultRequestHeaders.Add("User-Agent", "TerrariaAIBuildingMod/1.0");
        }

        return _sharedClient;
    }

    /// <summary>
    /// 验证API响应来源
    /// </summary>
    public static async Task<bool> ValidateResponse(HttpResponseMessage response, string expectedDomain)
    {
        if (response == null) return false;

        // 检查响应来源
        if (response.RequestMessage?.RequestUri?.Host != expectedDomain)
        {
            return false;
        }

        return true;
    }
}
```

### 5.4 输入验证与过滤

```csharp
public static class InputValidator
{
    private static readonly int MAX_PROMPT_LENGTH = 2000;
    private static readonly string[] FORBIDDEN_PATTERNS = new[]
    {
        "password", "secret", "token", "credential",
        "api_key", "apikey", "private_key"
    };

    /// <summary>
    /// 验证用户输入
    /// </summary>
    public static ValidationResult ValidatePrompt(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return ValidationResult.Error("输入不能为空");
        }

        if (input.Length > MAX_PROMPT_LENGTH)
        {
            return ValidationResult.Error($"输入过长，最多{MAX_PROMPT_LENGTH}字符");
        }

        // 检查是否包含敏感信息
        foreach (var pattern in FORBIDDEN_PATTERNS)
        {
            if (input.ToLower().Contains(pattern))
            {
                return ValidationResult.Warning("检测到可能的敏感信息，请勿在提示中包含API密钥等敏感数据");
            }
        }

        // 检查SQL注入模式（虽然AI API不会有SQL问题，但作为安全实践）
        if (ContainsSqlInjectionPattern(input))
        {
            return ValidationResult.Warning("输入包含可疑字符，请简化描述");
        }

        return ValidationResult.Success();
    }

    private static bool ContainsSqlInjectionPattern(string input)
    {
        string[] sqlPatterns = { "'; ", "DROP TABLE", "UNION SELECT", "--", "/*" };
        foreach (var pattern in sqlPatterns)
        {
            if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; }
    public bool IsWarning { get; set; }

    public static ValidationResult Success() => new ValidationResult { IsValid = true };
    public static ValidationResult Error(string msg) => new ValidationResult { IsValid = false, Message = msg };
    public static ValidationResult Warning(string msg) => new ValidationResult { IsValid = true, Message = msg, IsWarning = true };
}
```

### 5.5 多人模式安全考虑

```csharp
public class MultiplayerSecurity
{
    /// <summary>
    /// 验证玩家是否有权限使用AI建筑功能
    /// </summary>
    public static bool CanPlayerUseAI(Player player)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            return true; // 单人模式无条件允许
        }

        // 多人模式检查
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            // 客户端：检查是否是服务器管理员
            return Netplay.IsServerPlayer(player.whoAmI);
        }

        if (Main.netMode == NetmodeID.Server)
        {
            // 服务器端：验证玩家权限
            return IsPlayerAuthorized(player);
        }

        return false;
    }

    private static bool IsPlayerAuthorized(Player player)
    {
        // 实现权限检查逻辑
        // 可以检查：
        // - 是否是OP
        // - 是否在白名单中
        // - 是否有特定权限节点

        return false;
    }

    /// <summary>
    /// 在多人模式下同步建筑数据（不发送API密钥！）
    /// </summary>
    public static void SyncBuildingToClients(BuildingDesign design, int x, int y)
    {
        if (Main.netMode == NetmodeID.Server)
        {
            // 使用ModPacket发送建筑数据到所有客户端
            var packet = ModContent.GetInstance<AIBuildingMod>().GetPacket();
            packet.Write((byte)AIBuildingPacketType.BuildingData);
            packet.Write(x);
            packet.Write(y);
            packet.Write(JsonConvert.SerializeObject(design));
            packet.Send(-1, -1); // 发送给所有客户端
        }
    }
}

public enum AIBuildingPacketType : byte
{
    BuildingData = 1,
    RequestBuilding = 2,
    BuildingComplete = 3
}
```

### 5.6 日志与审计

```csharp
public class SecurityLogger
{
    private static string LogPath => Path.Combine(Main.SavePath, "Logs", "ai_building.log");

    public static void LogApiRequest(string endpoint, int tokens, bool success)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                             $"Endpoint: {endpoint.Replace("<api_key>", "***")}, " +
                             $"Tokens: {tokens}, " +
                             $"Success: {success}";
            File.AppendAllText(LogPath, logEntry + Environment.NewLine);
        }
        catch { /* 静默失败 */ }
    }

    public static void LogSecurityEvent(string eventType, string details)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                             $"SECURITY: {eventType} - {details}";
            File.AppendAllText(LogPath, logEntry + Environment.NewLine);
        }
        catch { /* 静默失败 */ }
    }
}
```

---

## 6. 完整实现示例

### 6.1 模组主类

```csharp
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

public class AIBuildingMod : Mod
{
    public static AIBuildingMod Instance { get; private set; }
    public AIApiService ApiService { get; private set; }
    public BuildingExecutor Builder { get; private set; }

    public override void Load()
    {
        Instance = this;

        // 初始化服务
        var config = ModContent.GetInstance<AIBuildingConfig>();
        ApiService = new AIApiService(config.ApiKey);
        Builder = new BuildingExecutor(this);

        // 注册UI
        AIChatUISystem chatUI = new AIChatUISystem();
        AddContent(chatUI);
    }

    public override void Unload()
    {
        Instance = null;
    }
}
```

### 6.2 模组配置

```csharp
using Terraria.ModLoader.Config;
using System.ComponentModel;

public class AIBuildingConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("AI服务设置")]
    [Tooltip("OpenAI/ Claude API密钥")]
    [PasswordPropertyText]
    public string ApiKey { get; set; } = "";

    [Tooltip("AI服务提供商")]
    public AIServiceType ServiceProvider { get; set; } = AIServiceType.OpenAI;

    [Tooltip("自定义API端点（可选）")]
    [DefaultValue("")]
    public string CustomEndpoint { get; set; } = "";

    [Header("生成设置")]
    [Range(100, 8000)]
    [DefaultValue(2000)]
    public int MaxTokens { get; set; }

    [Range(0f, 2f)]
    [DefaultValue(0.7f)]
    public float Temperature { get; set; }

    [Header("UI设置")]
    [DefaultValue(true)]
    public bool ShowUIOnStartup { get; set; }

    [DefaultValue(400)]
    public int UIWidth { get; set; }

    [DefaultValue(500)]
    public int UIHeight { get; set; }
}

public enum AIServiceType
{
    OpenAI,
    Claude,
    Custom
}
```

### 6.3 完整UI系统

```csharp
using Terraria.UI;
using Terraria.GameContent.UI.Elements;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

public class AIBuildingUISystem : ModSystem
{
    private UserInterface _userInterface;
    private AIChatPanel _chatPanel;
    internal bool IsVisible => _userInterface?.CurrentState != null;

    public override void Load()
    {
        _userInterface = new UserInterface();
        _chatPanel = new AIChatPanel();
        _chatPanel.Activate();
    }

    public override void UpdateUI(GameTime gameTime)
    {
        _userInterface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
        if (inventoryIndex != -1)
        {
            layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
                "AIBuilding: Chat Panel",
                () =>
                {
                    if (IsVisible) _userInterface.Draw(Main.spriteBatch, gameTime);
                    return true;
                },
                InterfaceScaleType.UI
            ));
        }
    }

    public void ToggleUI()
    {
        if (IsVisible)
            _userInterface.SetState(null);
        else
            _userInterface.SetState(_chatPanel);
    }
}

public class AIChatPanel : UIState
{
    private UIPanel _panel;
    private UIText _chatLog;
    private UITextBox _inputBox;
    private UITextPanel<string> _sendButton;
    private UITextPanel<string> _buildButton;

    private StringBuilder _messageLog = new StringBuilder();
    private StreamingAIService _streamingService;
    private CancellationTokenSource _currentRequest;

    public override void OnInitialize()
    {
        // 主面板
        _panel = new UIPanel();
        _panel.SetPadding(10);
        _panel.Width.Set(400f, 0f);
        _panel.Height.Set(500f, 0f);
        _panel.Left.Set(100f, 0f);
        _panel.Top.Set(100f, 0f);
        _panel.BackgroundColor = new Color(20, 20, 40, 230);
        _panel.BorderColor = new Color(80, 80, 120);
        Append(_panel);

        // 标题
        var title = new UIText("AI建筑助手", 1.2f, true);
        title.HAlign = 0.5f;
        title.Top.Set(5f, 0f);
        _panel.Append(title);

        // 聊天记录显示
        var logPanel = new UIPanel();
        logPanel.Width.Set(-20f, 1f);
        logPanel.Height.Set(-120f, 1f);
        logPanel.Top.Set(35f, 0f);
        logPanel.Left.Set(10f, 0f);
        logPanel.BackgroundColor = new Color(10, 10, 20, 200);
        _panel.Append(logPanel);

        _chatLog = new UIText("");
        _chatLog.Width.Set(-10f, 1f);
        _chatLog.Left.Set(5f, 0f);
        _chatLog.Top.Set(5f, 0f);
        _chatLog.IsWrapped = true;
        logPanel.Append(_chatLog);

        // 输入框
        _inputBox = new UITextBox();
        _inputBox.Width.Set(-100f, 1f);
        _inputBox.Height.Set(35f, 0f);
        _inputBox.Top.Set(-50f, 1f);
        _inputBox.Left.Set(0f, 0f);
        _panel.Append(_inputBox);

        // 发送按钮
        _sendButton = new UITextPanel<string>("发送");
        _sendButton.Width.Set(80f, 0f);
        _sendButton.Height.Set(35f, 0f);
        _sendButton.Top.Set(-50f, 1f);
        _sendButton.Left.Set(-90f, 1f);
        _sendButton.OnClick += (evt, elem) => SendMessage();
        _panel.Append(_sendButton);

        // 建筑按钮
        _buildButton = new UITextPanel<string>("生成建筑");
        _buildButton.Width.Set(-20f, 1f);
        _buildButton.Height.Set(30f, 0f);
        _buildButton.Top.Set(-90f, 1f);
        _buildButton.Left.Set(10f, 0f);
        _buildButton.BackgroundColor = new Color(40, 100, 40);
        _buildButton.OnClick += (evt, elem) => BuildFromLastResponse();
        _panel.Append(_buildButton);

        // 初始化流式服务
        InitializeStreamingService();
    }

    private void InitializeStreamingService()
    {
        var config = ModContent.GetInstance<AIBuildingConfig>();
        _streamingService = new StreamingAIService(config.ApiKey, config.ServiceProvider);

        _streamingService.OnTextReceived += (text) =>
        {
            _messageLog.Append(text);
            UpdateChatDisplay();
        };

        _streamingService.OnStreamComplete += () =>
        {
            Main.NewText("AI响应完成", Color.Green);
        };

        _streamingService.OnError += (ex) =>
        {
            _messageLog.AppendLine($"\n[错误: {ex.Message}]");
            UpdateChatDisplay();
        };
    }

    private void SendMessage()
    {
        string message = _inputBox.Text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        // 添加用户消息
        _messageLog.AppendLine($"[你]: {message}");
        _messageLog.AppendLine("[AI]: ");
        _inputBox.SetText("");
        UpdateChatDisplay();

        // 取消之前的请求
        _currentRequest?.Cancel();
        _currentRequest = new CancellationTokenSource();

        // 发送流式请求
        Task.Run(async () =>
        {
            await _streamingService.StreamChatAsync(message, _currentRequest.Token);
        });
    }

    private void UpdateChatDisplay()
    {
        _chatLog.SetText(_messageLog.ToString());
    }

    private void BuildFromLastResponse()
    {
        // 解析最后的AI响应并生成建筑
        string lastResponse = GetLastAIResponse();
        if (string.IsNullOrEmpty(lastResponse))
        {
            Main.NewText("没有可用的建筑设计", Color.Red);
            return;
        }

        // 尝试提取JSON
        string json = ExtractJsonFromResponse(lastResponse);
        if (json == null)
        {
            Main.NewText("无法从AI响应中提取建筑数据", Color.Red);
            return;
        }

        var design = JsonConvert.DeserializeObject<BuildingDesign>(json);
        if (design != null)
        {
            Player player = Main.LocalPlayer;
            int startX = (int)player.position.X / 16 + 3;
            int startY = (int)player.position.Y / 16 - design.Height / 2;

            AIBuildingMod.Instance.Builder.BuildAtLocation(design, startX, startY, player);
            Main.NewText($"已生成建筑: {design.Name}", Color.Green);
        }
    }

    private string GetLastAIResponse()
    {
        // 从消息日志中提取最后的AI响应
        var lines = _messageLog.ToString().Split('\n');
        var aiResponse = new StringBuilder();
        bool inAiResponse = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("[AI]:"))
            {
                inAiResponse = true;
                aiResponse.Clear();
                continue;
            }
            if (line.StartsWith("[你]:"))
            {
                inAiResponse = false;
            }
            if (inAiResponse)
            {
                aiResponse.AppendLine(line);
            }
        }

        return aiResponse.ToString();
    }

    private string ExtractJsonFromResponse(string response)
    {
        // 尝试提取JSON代码块
        int jsonStart = response.IndexOf("```json");
        if (jsonStart >= 0)
        {
            jsonStart += 7;
            int jsonEnd = response.IndexOf("```", jsonStart);
            if (jsonEnd > jsonStart)
            {
                return response.Substring(jsonStart, jsonEnd - jsonStart).Trim();
            }
        }

        // 尝试提取大括号包围的JSON
        int braceStart = response.IndexOf('{');
        int braceEnd = response.LastIndexOf('}');
        if (braceStart >= 0 && braceEnd > braceStart)
        {
            return response.Substring(braceStart, braceEnd - braceStart + 1);
        }

        return null;
    }

    // 允许拖动面板
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (ContainsPoint(Main.MouseScreen) && Main.mouseLeft)
        {
            // 阻止点击穿透到游戏
            Main.LocalPlayer.mouseInterface = true;
        }
    }
}
```

### 6.4 快捷键绑定

```csharp
using Terraria.ModLoader;

public class AIBuildingKeybindSystem : ModSystem
{
    public static ModKeybind ToggleUIKey { get; private set; }

    public override void Load()
    {
        ToggleUIKey = KeybindLoader.RegisterKeybind(Mod, "Toggle AI UI", "P");
    }

    public override void Unload()
    {
        ToggleUIKey = null;
    }
}

public class AIBuildingPlayer : ModPlayer
{
    public override void PreUpdate()
    {
        if (AIBuildingKeybindSystem.ToggleUIKey.JustPressed)
        {
            ModContent.GetInstance<AIBuildingUISystem>().ToggleUI();
        }
    }
}
```

---

## 7. 参考资源

### 官方文档
- [tModLoader官方文档](https://docs.tmodloader.net/)
- [tModLoader GitHub Wiki](https://github.com/tModLoader/tModLoader/wiki)
- [ExampleMod源码](https://github.com/tModLoader/tModLoader/tree/1.4/ExampleMod)

### API文档
- [OpenAI API文档](https://platform.openai.com/docs/api-reference)
- [Anthropic Claude API文档](https://docs.anthropic.com/claude/reference)

### 网络请求
- [Microsoft HttpClient文档](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient)
- [C#异步编程最佳实践](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/)

### UI开发
- [tModLoader UI指南](https://github.com/tModLoader/tModLoader/wiki/Basic-UI)
- [Terraria UI源码](https://github.com/tModLoader/tModLoader/tree/master/Terraria/GameContent/UI/Elements)

### 安全性
- [OWASP API安全最佳实践](https://owasp.org/www-project-api-security/)
- [DPAPI安全存储](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata)

---

## 总结

本文档详细介绍了在泰拉瑞亚tModLoader模组中集成AI功能的完整方案：

1. **HTTP请求**：使用`HttpClient`进行异步请求，通过`Task.Run`和`Main.QueueMainThreadAction`处理跨线程操作
2. **聊天界面**：可以使用简单的`ModCommand`或自定义`UIState`实现完整的聊天面板
3. **结构化数据**：定义清晰的JSON数据结构，使用`Newtonsoft.Json`进行序列化，通过`WorldGen`方法放置方块
4. **流式输出**：使用SSE协议处理AI API的流式响应，实现实时显示
5. **安全性**：配置文件存储密钥、DPAPI加密、输入验证、多人权限控制

建议开发时参考tModLoader官方ExampleMod中的UI和网络实现，并根据实际需求调整方案。