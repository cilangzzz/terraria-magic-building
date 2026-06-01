# tModLoader 日志排查与命令行编译文档

## 命令行编译模组

tModLoader 支持通过命令行直接编译模组，无需在游戏 UI 中手动操作。

### 方法一：使用 dotnet build（推荐）

```bash
# 进入模组源码目录
cd "C:/Users/admin/Documents/My Games/Terraria/tModLoader/ModSources/trab"

# 编译模组（Debug 模式）
dotnet build

# 编译模组（Release 模式）
dotnet build -c Release
```

**编译输出位置：**
- Debug: `bin/Debug/net8.0/trab.tmod`
- Release: `bin/Release/net8.0/trab.tmod`

### 方法二：使用 tModLoader.dll 直接编译

```bash
# 进入 tModLoader 安装目录
cd "E:/Game/steam/steamapps/common/tModLoader"

# 编译指定模组
dotnet tModLoader.dll -server -build "C:/Users/admin/Documents/My Games/Terraria/tModLoader/ModSources/trab"
```

### 命令行参数说明

| 参数 | 说明 |
|------|------|
| `-build <path>` | 指定要编译的模组源码路径 |
| `-server` | 以服务器模式运行（无图形界面，适合 CI/CD） |
| `-eac <path>` | 指定输出程序集路径 |
| `-define <symbol>` | 添加条件编译符号 |
| `-unsafe <bool>` | 是否允许 unsafe 代码 |
| `-config <file>` | 指定服务器配置文件 |
| `-tmlsavedirectory <path>` | 指定存档目录 |
| `-steamworkshopfolder <path>` | 指定 Steam Workshop 目录 |

### 编译命令示例

```bash
# 基础编译
dotnet build trab.csproj

# 带条件编译符号
dotnet build trab.csproj -p:DefineConstants=DEBUG

# 完整的 tModLoader 编译命令
cd "E:/Game/steam/steamapps/common/tModLoader"
dotnet tModLoader.dll -server -build "C:/Users/admin/Documents/My Games/Terraria/tModLoader/ModSources/trab" -tmlsavedirectory "C:/Users/admin/Documents/My Games/Terraria/tModLoader"
```

### CI/CD 自动化编译脚本示例

```bash
#!/bin/bash
# 自动编译 trab 模组

TML_PATH="E:/Game/steam/steamapps/common/tModLoader"
MOD_PATH="C:/Users/admin/Documents/My Games/Terraria/tModLoader/ModSources/trab"
SAVE_PATH="C:/Users/admin/Documents/My Games/Terraria/tModLoader"

cd "$TML_PATH"

# 编译模组
dotnet tModLoader.dll -server \
    -build "$MOD_PATH" \
    -tmlsavedirectory "$SAVE_PATH" \
    -config serverconfig.txt

# 检查编译结果
if [ -f "$SAVE_PATH/Mods/trab.tmod" ]; then
    echo "编译成功: trab.tmod"
else
    echo "编译失败，请检查日志"
    cat "$TML_PATH/tModLoader-Logs/server.log"
fi
```

### 编译流程说明

tModLoader 编译模组的完整流程（参考 `tMLMod.targets`）：

1. **预处理阶段**：解析 `build.txt` 配置
2. **编译阶段**：使用 dotnet 编译 C# 代码为 DLL
3. **打包阶段**：将 DLL + 资源文件打包为 `.tmod` 文件
4. **输出阶段**：将 `.tmod` 文件复制到 Mods 目录

### 模组配置文件 (build.txt)

```
displayName = AI Building Generator  # 模组显示名称
author = cilang                       # 作者
version = 0.1                         # 版本号
modSide = Both                        # 运行端（Both/Client/Server）
```

### 项目文件结构 (trab.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <!-- 引用 tModLoader 配置 -->
    <Import Project="..\tModLoader.targets" />
    
    <PropertyGroup>
        <!-- 可添加额外配置 -->
    </PropertyGroup>
    
    <ItemGroup>
        <!-- 可添加额外引用 -->
    </ItemGroup>
</Project>
```

---

## 日志文件结构

tModLoader 的日志文件位于：`E:\Game\steam\steamapps\common\tModLoader\tModLoader-Logs\`

| 文件名 | 大小 | 作用 |
|--------|------|------|
| `Launch.log` | ~1KB | 启动流程日志，记录环境检测和 .NET 初始化 |
| `Natives.log` | ~263B | Steam 原生库初始化日志 |
| `client.log` | ~140KB | **主日志文件**，记录模组加载、编译、运行时的所有信息 |
| `environment-client.log` | ~6KB | 环境变量配置日志 |
| `terrariasteamclient.log` | ~901B | Steam 客户端通信日志 |
| `Old/` | 目录 | 存放旧日志备份 |

---

## 日志文件详解

### 1. Launch.log - 启动日志

记录 tModLoader 启动过程中的关键步骤：

```
关键信息：
- Windows 版本检测
- .NET 版本验证 (当前: 8.0.0)
- 安装目录验证
- 启动命令记录
```

**常见问题排查：**
- 如果 `.NET 验证失败` → 检查是否安装了正确版本的 .NET SDK
- 如果 `安装目录验证失败` → 检查 Steam 安装路径是否正确

---

### 2. client.log - 主日志文件（最重要）

#### 日志格式说明

```
[时间] [线程名/日志级别] [来源]: 日志内容
```

日志级别：
- `INFO` - 普通信息
- `WARN` - 警告
- `ERROR` - 错误
- `DEBUG` - 调试信息

#### 关键信息提取

**系统信息（启动时）：**
```
[21:56:50.773] [Main Thread/INFO] [tML]: Starting tModLoader client 1.4.4.9+2026.03.3.0
[21:56:50.779] [Main Thread/INFO] [tML]: Running on Windows (v10.0.26200.0) X64 NetCore 8.0.0
[21:56:50.782] [Main Thread/INFO] [tML]: CPU: 12 processors. RAM: 31.8 GB
```

**模组加载过程：**
```
[21:57:31.516] [.NET TP Worker/DEBUG] [tML]: Selected CalamityMod 2.1.2 for tML 2025.12.3.1 from Workshop.
[21:57:31.517] [.NET TP Worker/DEBUG] [tML]: Skipped CalamityMod 2.0.2.3 - mod is for a different Terraria version
```

**模组编译结果：**
```
[22:11:46.541] [.NET TP Worker/INFO] [tML]: 编译完成，出现0个错误与0个警告  ← 成功
[22:24:24.896] [.NET TP Worker/INFO] [tML]: 编译完成，出现46个错误与127个警告  ← 失败
```

---

### 3. environment-client.log - 环境变量日志

记录运行时的环境变量配置，包括：
- Steam 相关路径
- .NET 配置
- 系统路径
- GPU 着色器缓存路径

**关键环境变量：**
```
AI_API_KEY=sk-123e66af01b64e12a05fb047fa9af71b  ← AI API 密钥配置
dotnet_version=8.0.0
STEAMPATH=E:/Game/steam
```

---

## 当前项目 (trab) 编译错误分析

### 错误汇总（46个错误，127个警告）

#### 1. 命名空间/类型缺失错误 (CS0234, CS0246)

| 文件 | 错误 | 原因 |
|------|------|------|
| `Commands/BuildCommands.cs:9` | `trab.Players` 命名空间不存在 | Players 目录可能被删除或命名空间配置错误 |
| `Commands/BuildCommands.cs:125,157,248` | `AIBuildingConfig` 类型未找到 | Config 类可能未被正确引用 |
| `Players/AIBuildingPlayer.cs:44,160` | `AIBuildingConfig` 类型未找到 | 同上 |

**解决方案：**
- 检查 `Players/` 目录是否存在
- 确保 `AIBuildingConfig.cs` 的命名空间正确 (`trab.Config`)
- 添加正确的 `using trab.Config;` 引用

---

#### 2. API 版本不兼容错误 (CS0117)

**TileID/WallID 常量不存在：**
```
BuildingExecutor.cs: TileID.Brick, TileID.AdamantiteBrick, TileID.SandstoneSlab, TileID.Ice
BuildingExecutor.cs: TileID.GoldPlating, TileID.SilverPlating
BuildingExecutor.cs: WallID.Brick, WallID.SandstoneSlab, WallID.Snow, WallID.Ice
BuildingExecutor.cs: TileID.WorkBench, TileID.Sofas, TileID.Bookshelves, TileID.Paintings...
```

**原因：** Terraria 1.4.4 版本中部分 Tile/Wall ID 常量名称已更改

**解决方案：**
- 使用 `TileID.GetTileName(tileType)` 查找正确的常量名
- 参考 Terraria 官方 Wiki 或 tModLoader 源码中的 TileID.cs

---

#### 3. WorldGen API 错误 (CS0117)

```
BuildingExecutor.cs:161: WorldGen.EmptyTile 不存在
BuildingExecutor.cs:241: WorldGen.EmptyTile 不存在
```

**解决方案：**
- 使用替代方法：`Main.tile[x, y].active()` 检查 tile 是否存在
- 或使用 `!WorldGen.SolidTile(x, y)` 检查是否为空

---

#### 4. UI 系统错误 (CS1061, CS0115, CS0103)

| 错误 | 文件 | 原因 |
|------|------|------|
| `OnClick` 方法不存在 | `AIBuildingUI.cs:167,178,188` | tModLoader UI 系统已更新，使用新的事件绑定方式 |
| `Click` 方法无法重写 | `AIBuildingUI.cs:503` | `UIInputTextField` 没有 `Click` 虚方法 |
| `TextureAssets` 不存在 | `AIBuildingUI.cs:458-472` | 需要添加 `using Terraria.GameContent;` |
| `FontAssets` 不存在 | `AIBuildingUI.cs:492` | 同上 |
| `IsVisible` 不存在 | `AIBuildingUI.cs:412` | 应使用 `Visible` 或 `IsVisible` 属性检查 |

**解决方案：**
- UI 事件绑定：使用 `OnClick += (evt, element) => { }` 替代直接方法调用
- 添加引用：`using Terraria.GameContent;`
- 检查 UIElement 的正确属性名

---

#### 5. Tile 访问错误 (CS1612)

```
BuildingExecutor.cs:187,214: 无法修改 Tilemap.this[int, int] 的返回值
```

**原因：** `Main.tile[x, y]` 返回的是值类型，不能直接修改其属性

**解决方案：**
```csharp
// 错误写法
Main.tile[x, y].active = true;

// 正确写法
Tile tile = Main.tile[x, y];
tile.active = true;
Main.tile[x, y] = tile;

// 或使用 WorldGen.PlaceTile(x, y, tileType);
```

---

#### 6. 静态访问错误 (CS0120, CS0119)

```
AIApiService.cs:172: Mod.Logger 需要对象引用
AIBuildingUI.cs:266,313: Mod 类型在上下文中无效
```

**解决方案：**
- 使用 `ModContent.GetInstance<YourMod>().Logger` 获取 Logger
- 或在 Mod 类内部使用 `this.Logger`

---

#### 7. 变量不存在错误 (CS0103)

```
AIBuildingUI.cs:59: gameTime 变量不存在
```

**解决方案：**
- 检查方法签名，确保 `GameTime gameTime` 参数存在
- 或传入正确的参数名

---

## 警告分析（127个警告）

### 1. TooltipAttribute 已过时 (CS0618)

```
AIBuildingConfig.cs 多处: TooltipAttribute 已过时
```

**解决方案：**
- 使用 `TooltipKeyAttribute` 替代
- 或直接在本地化文件中定义 tooltip

### 2. Newtonsoft.Json 版本匹配警告 (CS1701) - **tModLoader 框架问题**

```
warning CS1701: 假定"Newtonsoft.Json"使用的程序集引用"System.Runtime, Version=6.0.0.0"与"System.Runtime"的标识"System.Runtime, Version=8.0.0.0"匹配
```

**根本原因：**
- tModLoader 使用 Newtonsoft.Json 13.0.3 (针对 .NET 6.0 编译)
- 运行环境是 .NET 8.0
- 编译时 Roslyn 检测到版本绑定差异并发出警告

**问题性质：**
- 这是 **tModLoader 框架本身的问题**，不是模组代码问题
- 所有使用 tModLoader 2026.03 版本的模组都会遇到
- 警告重复出现 24+ 次是因为编译器多次加载程序集

**影响：** 仅警告，不影响编译和运行

**解决方案（按优先级）：**

1. **等待 tModLoader 更新** - 最根本的解决方案
   - tModLoader 需要更新 Newtonsoft.Json 到 .NET 8.0 兼容版本
   - 可向 tModLoader 提交 issue: https://github.com/tModLoader/tModLoader/issues

2. **日志过滤** - 实用方案
   ```bash
   # 过滤掉 CS1701 警告查看日志
   grep -v "CS1701" client.log | less
   ```

3. **runtimeconfig.json** - 尝试运行时绑定重定向（效果有限）
   - 在模组目录创建 `modname.runtimeconfig.json`
   - 配置 System.Runtime 绑定到 8.0.0.0

**注意：** 模组的 `NoWarn` 配置无法抑制此警告，因为警告来自 tModLoader 的内部编译器 (BuildHost)，不是模组的编译流程。

---

## 日志排查流程

### 1. 模组无法加载

```
排查步骤：
1. 检查 client.log 中的 "Finding Mods..." 部分
2. 查看是否有 "Skipped" 记录，确认版本兼容性
3. 检查 "enabled.json" 文件是否存在
```

### 2. 模组编译失败

```
排查步骤：
1. 查看 client.log 末尾的错误列表
2. 搜索 [ERROR] 标记
3. 根据错误代码 (CS0xxx) 定位问题类型
4. 修复代码后重新编译
```

### 3. 运行时崩溃

```
排查步骤：
1. 查看 client.log 中的异常堆栈
2. 搜索 "Exception" 或 "Error" 关键词
3. 定位崩溃的代码位置
4. 检查相关文件和调用链
```

---

## 快速命令参考

### 日志搜索命令

```bash
# 搜索所有错误
grep -i "ERROR" client.log

# 搜索特定模组的错误
grep -i "trab" client.log | grep -i "ERROR"

# 搜索编译结果
grep -i "编译完成" client.log
```

### 常用错误代码参考

| 错误代码 | 含义 | 典型原因 |
|----------|------|----------|
| CS0234 | 命名空间不存在 | 目录结构错误或命名空间配置错误 |
| CS0246 | 类型未找到 | 缺少 using 引用或类型不存在 |
| CS0117 | 成员不存在 | API 版本变更，成员被移除或重命名 |
| CS1061 | 方法不存在 | API 变更或扩展方法未引入 |
| CS0115 | 无法重写方法 | 方法不是虚方法或不存在 |
| CS1612 | 无法修改返回值 | 值类型返回值无法直接修改 |
| CS0120 | 静态访问错误 | 非静态成员被静态方法访问 |
| CS0618 | 已过时警告 | 使用了废弃的 API |

---

## 相关资源

- **tModLoader Wiki**: https://github.com/tModLoader/tModLoader/wiki
- **Terraria ID 参考**: https://terraria.wiki.gg/wiki/Tile_IDs
- **日志文件路径**: `E:\Game\steam\steamapps\common\tModLoader\tModLoader-Logs\`
- **模组源码路径**: `C:\Users\admin\Documents\My Games\Terraria\tModLoader\ModSources\`

---

## 附录：当前系统配置

```
tModLoader 版本: 1.4.4.9+2026.03.3.0 (stable)
运行平台: Windows 10.0.26200 X64
.NET 版本: 8.0.0
CPU: 12 核处理器
RAM: 31.8 GB 总内存
显卡: AMD Radeon RX 6750 GRE 10GB
显示分辨率: 2560 x 1440
Steam 云存储: 953.7 MB 可用
```

---

*文档生成日期: 2026-05-31*
*日志分析基于: tModLoader-Logs/client.log (最新)*