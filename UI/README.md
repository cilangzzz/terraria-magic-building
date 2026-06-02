# UI - 用户界面层

本目录包含AI建筑生成模组的用户界面实现。

---

## 文件列表

| 文件 | 行数 | 功能描述 |
|------|------|----------|
| AIBuildingUI.cs | 799 | AI建筑UI面板，包含风格/尺寸选择、操作按钮、日志显示、选区预览 |

---

## 类结构

### AIBuildingUISystem

继承 `ModSystem`，管理UI生命周期。

**功能**:
- 创建和管理 `UserInterface`
- 初始化 `AIBuildingPanel`
- Toggle切换UI显示
- UpdateUI更新UI状态
- ModifyInterfaceLayers插入UI绘制层

**关键属性**:
| 属性 | 类型 | 说明 |
|------|------|------|
| ui | UserInterface | 用户界面管理器 |
| panel | AIBuildingPanel | UI面板实例 |
| Visible | bool | UI是否可见 |

### BuildingStyle

建筑风格枚举。

| 值 | 说明 | 提示词特征 |
|----|------|------------|
| Medieval | 中世纪风格 | GrayBrick, StoneSlab, Wood地板 |
| Modern | 现代风格 | Glass, GrayBrick, Platform |
| Japanese | 日式风格 | Wood, BorealWood, 斜坡屋顶 |
| Fantasy | 奇幻风格 | GoldBrick, Marble, 豪华家具 |
| Underground | 地下风格 | Stone, Obsidian, 工坊家具 |
| Custom | 自定义风格 | 用户输入描述 |

### AIBuildingPanel

继承 `UIState`，主UI面板。

**UI尺寸常量**:
| 常量 | 值 | 说明 |
|------|-----|------|
| PANEL_WIDTH | 320f | 面板宽度 |
| PANEL_HEIGHT | 480f | 面板高度 |
| MODULE_HEIGHT_LOG | 271f | 日志模块高度 |

**UI模块**:
| 模块 | 高度 | 内容 |
|------|------|------|
| styleModule | 44f | 风格选择（6个按钮） |
| sizeModule | 44f | 尺寸选择（3个按钮） |
| functionModule | 48f | 操作按钮（生成/放置/选区/Agent） |
| logModule | 271f | 日志显示（10行） |

**关键方法**:
| 方法 | 功能 |
|------|------|
| `SelectStyle(idx)` | 选择风格 |
| `SelectSize(idx)` | 选择尺寸 |
| `ToggleAgentMode()` | 切换Agent模式 |
| `ToggleAreaMode()` | 切换选区模式 |
| `DoGenerate()` | 执行生成 |
| `DoPlaceAtMouse()` | 在鼠标位置放置 |
| `AddMessage(msg)` | 添加日志消息 |
| `DrawAreaPreview()` | 绘制选区预览 |

---

## 界面布局

```
┌─────────────────────────────┐
│  风格  [中世纪][现代][日式]... │  ← styleModule (44px)
├─────────────────────────────┤
│  尺寸  [小] [中] [大]  10x8   │  ← sizeModule (44px)
├─────────────────────────────┤
│  操作  [生成][放置][选区][Ag] │  ← functionModule (48px)
├─────────────────────────────┤
│  日志                        │
│  [AI建筑助手已启动]           │
│  [P关闭 | M选区...]          │  ← logModule (271px)
│  ...                         │
│                        [×]   │  ← 清屏按钮
└─────────────────────────────┘
```

---

## 选区模式

**功能**: 拖拽选择世界中的区域，生成匹配尺寸的建筑。

**操作流程**:
1. 点击 "选区" 或按 M 进入选区模式
2. 在世界中拖拽选择区域（黄色边框预览）
3. 松开鼠标确认选区
4. 按 G 或点击 "生成" 生成匹配建筑
5. 按 B 或点击 "放置" 放置到选区

**选区数据**:
| 属性 | 类型 | 说明 |
|------|------|------|
| isSelectingArea | bool | 是否处于选区模式 |
| isDragging | bool | 是否正在拖拽 |
| dragStart | Point? | 拖拽起点 |
| dragEnd | Point? | 拖拽终点 |
| confirmedAreaStart | Point? | 确认的选区起点 |
| confirmedAreaEnd | Point? | 确认的选区终点 |

---

## 自定义风格弹窗

选择 "自定义" 风格时显示居中弹窗:
- 提示用户在聊天框输入风格描述
- 按 Enter 确认输入
- 按 Esc 取消

---

## 相关文件

- [AIBuildingPlayer.cs](../Players/AIBuildingPlayer.cs) - 快捷键处理
- [AIAgentService.cs](../Core/AIAgentService.cs) - Agent服务调用
- [AIBuildingConfig.cs](../Config/AIBuildingConfig.cs) - 配置读取