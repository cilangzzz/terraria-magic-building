# Commands - 聊天命令层

本目录包含游戏内聊天命令的实现。

---

## 文件列表

| 文件 | 功能描述 |
|------|----------|
| BuildCommands.cs | AI建筑生成聊天命令，包含 /aibuild 和 /quickbuild |

---

## 命令说明

### /aibuild 命令

AI建筑生成主命令。

**用法**:
```
/aibuild <建筑描述>     - 使用AI生成建筑
/aibuild help           - 显示帮助信息
/aibuild list           - 显示可用材料列表
/aibuild config         - 显示当前配置
/aibuild stop           - 停止当前生成
```

**示例**:
```
/aibuild 一座中世纪风格的木屋
/aibuild 带有地下室的金砖别墅
/aibuild 雪地风格的冰屋
```

### /quickbuild 命令

快速生成预设建筑。

**用法**:
```
/quickbuild <预设名称>  - 快速生成预设建筑
/quickbuild list        - 显示预设列表
```

**预设列表**:
| 预设 | 名称 | 尺寸 | 材料 |
|------|------|------|------|
| house | 简单木屋 | 10x8 | 木材 |
| castle | 小型城堡 | 20x15 | 灰砖 |
| tower | 观察塔 | 8x20 | 红砖 |
| cave | 地下室 | 15x10 | 石头 |
| shop | 商店 | 12x8 | 木材 |

---

## 类结构

### AIBuildCommand

继承 `ModCommand`，处理 `/aibuild` 命令。

**关键方法**:
- `Action()` - 命令入口
- `ShowHelp()` - 显示帮助
- `ShowMaterialList()` - 显示材料列表
- `ShowConfig()` - 显示配置
- `StopGeneration()` - 停止生成
- `RequestBuilding()` - 请求AI生成
- `ProcessResponse()` - 处理AI响应

### QuickBuildCommand

继承 `ModCommand`，处理 `/quickbuild` 命令。

**关键方法**:
- `Action()` - 命令入口
- `GetPresetDesign()` - 获取预设设计
- `CreateSimpleHouse()` - 创建木屋预设
- `CreateSmallCastle()` - 创建城堡预设
- `CreateTower()` - 创建塔楼预设
- `CreateCaveRoom()` - 创建地下室预设
- `CreateShop()` - 创建商店预设

---

## 配置依赖

命令执行依赖 `AIBuildingConfig` 配置:
- `ApiKey` - API密钥（必须设置）
- `ServiceProvider` - API服务商
- `ModelName` - 模型名称
- `BuildOffsetX/Y` - 生成位置偏移
- `MaxBuildingSize` - 最大尺寸限制

---

## 相关文件

- [AIBuildingConfig.cs](../Config/AIBuildingConfig.cs) - 配置类
- [AIApiService.cs](../Core/AIApiService.cs) - API调用服务
- [BuildingExecutor.cs](../Core/BuildingExecutor.cs) - 建筑执行器