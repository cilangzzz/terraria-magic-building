# Players - 玩家扩展层

本目录包含玩家ModPlayer实现和快捷键绑定。

---

## 文件列表

| 文件 | 功能描述 |
|------|----------|
| AIBuildingPlayer.cs | 玩家ModPlayer扩展，管理生成状态、快捷键和建筑放置 |

---

## 类说明

### AIBuildingPlayer

继承 `ModPlayer`，为每个玩家提供AI建筑生成功能。

**主要职责**:
- 管理AI服务实例
- 存储最后一次生成的建筑设计
- 处理快捷键输入
- 执行建筑放置

**关键属性**:
| 属性 | 类型 | 说明 |
|------|------|------|
| LastDesign | BuildingDesign | 最后生成的建筑设计 |
| LastAIResponse | string | 最后的AI响应文本 |
| IsGenerating | bool | 是否正在生成 |
| AgentProgress | string | Agent进度信息 |
| ToolCallHistory | List<string> | 工具调用历史 |

**关键方法**:
| 方法 | 功能 |
|------|------|
| `RequestBuildingDesignAgent(prompt)` | Agent模式生成建筑（推荐） |
| `RequestBuildingDesign(prompt)` | 传统API模式生成建筑 |
| `ProcessAgentDesign(design)` | 处理Agent返回的设计 |
| `PlaceLastDesign()` | 在玩家位置放置最后的建筑 |
| `PlaceDesignAt(design, x, y)` | 在指定位置放置建筑 |
| `StopGeneration()` | 停止当前生成 |
| `GetKnowledgeBaseStatus()` | 获取知识库状态信息 |

---

## 快捷键绑定

### AIBuildingKeybindSystem

继承 `ModSystem`，注册和管理快捷键。

| 快捷键 | 注册名 | 功能 |
|--------|--------|------|
| P | ToggleAIUI | 打开/关闭AI建筑UI面板 |
| B | PlaceBuilding | 在当前位置放置最后的建筑 |

**使用条件**:
- 不在聊天模式 (`!Main.drawingPlayerChat`)
- 不在游戏菜单 (`!Main.gameMenu`)

### UI内快捷键

UI面板打开时可用的快捷键:

| 键 | 功能 |
|----|------|
| G | 执行生成 |
| B | 在鼠标位置放置 |
| M | 切换选区模式 |
| S | 停止生成 |

---

## PreUpdate 流程

每帧更新检查快捷键:

```csharp
public override void PreUpdate()
{
    // P键开关UI
    if (ToggleUIKey.JustPressed && !Main.drawingPlayerChat)
        uiSys.Toggle();
    
    // B键快速放置（UI关闭时）
    if (PlaceBuildingKey.JustPressed && !uiSys.Visible)
        PlaceLastDesign();
    
    // UI内的快捷键处理...
}
```

---

## 相关文件

- [AIBuildingUI.cs](../UI/AIBuildingUI.cs) - UI面板
- [AIAgentService.cs](../Core/AIAgentService.cs) - Agent服务
- [EnhancedBuildingExecutor.cs](../Core/EnhancedBuildingExecutor.cs) - 建筑执行器