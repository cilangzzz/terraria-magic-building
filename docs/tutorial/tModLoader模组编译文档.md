# tModLoader 模组编译文档

## 编译方式概览

tModLoader 支持两种模组编译方式：

| 方式 | 适用场景 | 优点 | 缺点 |
|------|----------|------|------|
| **游戏内编译** | 开发调试 | 可视化界面、实时反馈 | 需要启动游戏、无法自动化 |
| **命令行编译** | CI/CD、批量编译 | 自动化、无需图形界面 | 需要熟悉命令行参数 |

---

## 一、命令行编译

### 1.1 基础命令

#### 方法一：dotnet build（推荐）

```bash
# 进入模组源码目录
cd "C:/Users/admin/Documents/My Games/Terraria/tModLoader/ModSources/trab"

# Debug 编译
dotnet build

# Release 编译
dotnet build -c Release
```

#### 方法二：tModLoader.dll 直接编译

```bash
# 进入 tModLoader 安装目录
cd "E:/Game/steam/steamapps/common/tModLoader"

# 编译指定模组
dotnet tModLoader.dll -server -build "<模组路径>"
```

### 1.2 完整编译命令示例

```bash
# Windows 完整命令
cd "E:/Game/steam/steamapps/common/tModLoader"
dotnet tModLoader.dll -server \
    -build "C:/Users/admin/Documents/My Games/Terraria/tModLoader/ModSources/trab" \
    -tmlsavedirectory "C:/Users/admin/Documents/My Games/Terraria/tModLoader"
```

### 1.3 命令行参数详解

| 参数 | 说明 | 示例 |
|------|------|------|
| `-build <path>` | 模组源码目录路径 | `-build "./MyMod"` |
| `-server` | 服务器模式（无图形界面） | 必须用于命令行编译 |
| `-eac <path>` | 输出程序集路径 | `-eac "./bin/MyMod.dll"` |
| `-define <symbol>` | 条件编译符号 | `-define DEBUG` |
| `-unsafe <bool>` | 允许 unsafe 代码 | `-unsafe true` |
| `-tmlsavedirectory <path>` | 存档目录（Mods/Worlds 所在位置） | `-tmlsavedirectory "D:/Terraria"` |
| `-steamworkshopfolder <path>` | Steam Workshop 目录 | `-steamworkshopfolder "./workshop"` |
| `-config <file>` | 服务器配置文件 | `-config serverconfig.txt` |
| `-ignoreMod <name>` | 编译时忽略的模组 | `-ignoreMod CalamityMod` |

### 1.4 编译输出位置

编译成功后，`.tmod` 文件输出到以下位置：

```
# 默认输出路径
C:/Users/admin/Documents/My Games/Terraria/tModLoader/Mods/trab.tmod

# 或模组目录下的 bin 目录
C:/Users/admin/Documents/My Games/Terraria/tModLoader/ModSources/trab/bin/Debug/net8.0/
```

---

## 二、编译流程详解

### 2.1 编译阶段

tModLoader 编译模组经过以下阶段：

```
┌─────────────────┐
│  1. 解析配置    │  ← 读取 build.txt
└─────────────────┘
        ↓
┌─────────────────┐
│  2. C# 编译     │  ← dotnet compile → DLL
└─────────────────┘
        ↓
┌─────────────────┐
│  3. 资源打包    │  ← 图片、音效、本地化文件
└─────────────────┘
        ↓
┌─────────────────┐
│  4. 生成 .tmod  │  ← 最终模组包
└─────────────────┘
        ↓
┌─────────────────┐
│  5. 复制到 Mods │  ← 自动部署
└─────────────────┘
```

### 2.2 build.txt 配置文件

模组根目录必须包含 `build.txt`：

```txt
displayName = AI Building Generator    # 显示名称
author = cilang                         # 作者
version = 0.1                           # 版本号（建议语义化）
modSide = Both                          # 运行端
# 可选配置
homepage = https://github.com/xxx       # 主页链接
description = 模组描述                   # 简短描述
```

**modSide 取值说明：**

| 值 | 说明 |
|---|------|
| `Both` | 客户端和服务器都需要 |
| `Client` | 仅客户端需要 |
| `Server` | 仅服务器需要 |
| `NoSync` | 客户端本地，不同步到服务器 |

### 2.3 项目文件结构

**trab.csproj 示例：**

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <!-- 引用 tModLoader 编译配置 -->
    <Import Project="..\tModLoader.targets" />
    
    <PropertyGroup>
        <!-- 可添加自定义配置 -->
    </PropertyGroup>
    
    <ItemGroup>
        <!-- 添加额外的 DLL 引用 -->
        <!-- <Reference Include="YourLibrary.dll" /> -->
    </ItemGroup>
</Project>
```

**tModLoader.targets 位置：**

```
C:/Users/admin/Documents/My Games/Terraria/tModLoader/ModSources/tModLoader.targets
```

或使用 Steam 安装目录：

```
E:/Game/steam/steamapps/common/tModLoader/tMLMod.targets
```

---

## 三、模组目录结构规范

### 3.1 标准目录结构

```
trab/
├── trab.cs              # 主模组类（继承 Mod 类）
├── trab.csproj          # 项目文件
├── build.txt            # 编译配置
├── description.txt      # 详细描述
├── icon.png             # 模组图标（建议 80x80）
├── icon_small.png       # 小图标
│
├── Commands/            # 命令类
│   └── BuildCommands.cs
│
├── Config/              # 配置类
│   └── AIBuildingConfig.cs
│
├── Core/                # 核心逻辑
│   ├── BuildingExecutor.cs
│   └── AIApiService.cs
│
├── Players/             # 玩家相关
│   └── AIBuildingPlayer.cs
│
├── UI/                  # UI 相关
│   └── AIBuildingUI.cs
│
├── Data/                # 数据文件
│   └── templates.json
│
├── Localization/        # 本地化
│   ├── en-US.hjson
│   └── zh-Hans.hjson
│
├── Properties/          # 属性配置
│   └── AssemblyInfo.cs
│
└── docs/                # 文档
    └── 编译文档.md
```

### 3.2 必需文件清单

| 文件 | 必须 | 说明 |
|------|------|------|
| `*.cs` | ✅ | 至少一个 Mod 类 |
| `*.csproj` | ✅ | 项目配置 |
| `build.txt` | ✅ | 编译元数据 |
| `description.txt` | ⚪ | 详细描述（发布时需要） |
| `icon.png` | ⚪ | 模组图标（建议添加） |
| `Localization/*.hjson` | ⚪ | 本地化文件 |

---

## 四、CI/CD 自动化编译

### 4.1 基础自动化脚本

**build.sh（Linux/Mac）：**

```bash
#!/bin/bash
# tModLoader 模组自动化编译脚本

set -e

# 配置变量
TML_PATH="E:/Game/steam/steamapps/common/tModLoader"
MOD_NAME="trab"
MOD_PATH="C:/Users/admin/Documents/My Games/Terraria/tModLoader/ModSources/$MOD_NAME"
SAVE_PATH="C:/Users/admin/Documents/My Games/Terraria/tModLoader"

echo "=== 开始编译模组: $MOD_NAME ==="
echo "源码路径: $MOD_PATH"
echo "输出路径: $SAVE_PATH/Mods"

# 进入 tModLoader 目录
cd "$TML_PATH"

# 执行编译
dotnet tModLoader.dll -server \
    -build "$MOD_PATH" \
    -tmlsavedirectory "$SAVE_PATH"

# 检查编译结果
if [ -f "$SAVE_PATH/Mods/$MOD_NAME.tmod" ]; then
    echo "✅ 编译成功!"
    echo "输出文件: $SAVE_PATH/Mods/$MOD_NAME.tmod"
    ls -lh "$SAVE_PATH/Mods/$MOD_NAME.tmod"
else
    echo "❌ 编译失败!"
    echo "请查看日志: $TML_PATH/tModLoader-Logs/server.log"
    exit 1
fi
```

**build.ps1（Windows PowerShell）：**

```powershell
# tModLoader 模组自动化编译脚本

$TML_PATH = "E:\Game\steam\steamapps\common\tModLoader"
$MOD_NAME = "trab"
$MOD_PATH = "C:\Users\admin\Documents\My Games\Terraria\tModLoader\ModSources\$MOD_NAME"
$SAVE_PATH = "C:\Users\admin\Documents\My Games\Terraria\tModLoader"

Write-Host "=== 开始编译模组: $MOD_NAME ===" -ForegroundColor Cyan

# 进入 tModLoader 目录
Set-Location $TML_PATH

# 执行编译
dotnet tModLoader.dll -server -build $MOD_PATH -tmlsavedirectory $SAVE_PATH

# 检查结果
$tmodFile = "$SAVE_PATH\Mods\$MOD_NAME.tmod"
if (Test-Path $tmodFile) {
    Write-Host "✅ 编译成功!" -ForegroundColor Green
    Write-Host "输出文件: $tmodFile"
    Get-Item $tmodFile | Select-Object Name, Length, LastWriteTime
} else {
    Write-Host "❌ 编译失败!" -ForegroundColor Red
    Write-Host "请查看日志: $TML_PATH\tModLoader-Logs\server.log"
    exit 1
}
```

### 4.2 GitHub Actions 配置

**.github/workflows/build.yml：**

```yaml
name: Build tModLoader Mod

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: Download tModLoader
      run: |
        curl -L -o tModLoader.zip "https://github.com/tModLoader/tModLoader/releases/latest/download/tModLoader.zip"
        unzip tModLoader.zip -d tModLoader
    
    - name: Build Mod
      run: |
        cd tModLoader
        dotnet tModLoader.dll -server -build "${{ github.workspace }}" -tmlsavedirectory "${{ github.workspace }}"
    
    - name: Upload Artifact
      uses: actions/upload-artifact@v4
      with:
        name: mod-artifact
        path: Mods/*.tmod
        retention-days: 30
```

### 4.3 批量编译脚本

**build-all.sh：**

```bash
#!/bin/bash
# 批量编译所有模组

MODS_DIR="C:/Users/admin/Documents/My Games/Terraria/tModLoader/ModSources"
TML_PATH="E:/Game/steam/steamapps/common/tModLoader"

cd "$TML_PATH"

for mod_folder in "$MODS_DIR"/*/; do
    mod_name=$(basename "$mod_folder")
    
    # 检查是否有 build.txt
    if [ -f "$mod_folder/build.txt" ]; then
        echo "编译模组: $mod_name"
        dotnet tModLoader.dll -server -build "$mod_folder"
        
        if [ $? -eq 0 ]; then
            echo "✅ $mod_name 编译成功"
        else
            echo "❌ $mod_name 编译失败"
        fi
    fi
done

echo "=== 批量编译完成 ==="
```

---

## 五、编译错误排查

### 5.1 常见错误类型

| 错误代码 | 含义 | 典型原因 | 解决方案 |
|----------|------|----------|----------|
| **CS0234** | 命名空间不存在 | 目录结构错误 | 检查文件夹命名和 namespace |
| **CS0246** | 类型未找到 | 缺少 using 或类型不存在 | 添加正确引用 |
| **CS0117** | 成员不存在 | API 版本变更 | 查阅 tModLoader 文档更新 API |
| **CS1061** | 方法不存在 | 方法签名变更或扩展方法未引入 | 使用新的 API |
| **CS0115** | 无法重写方法 | 方法不是 virtual 或不存在 | 检查基类定义 |
| **CS1612** | 无法修改返回值 | 值类型返回值无法直接修改 | 使用临时变量 |
| **CS0120** | 静态访问错误 | 非静态成员被静态访问 | 使用实例引用 |
| **CS0618** | 已过时警告 | 使用废弃 API | 迁移到新 API |

### 5.2 编译日志位置

```
E:/Game/steam/steamapps/common/tModLoader/tModLoader-Logs/
├── client.log      # 客户端编译日志（游戏内编译）
├── server.log      # 服务器编译日志（命令行编译）
├── Launch.log      # 启动日志
└── environment-client.log  # 环境配置
```

### 5.3 错误排查流程

```
编译失败排查流程：

1. 查看编译日志
   └─ 找到 ERROR 行 → 定位文件和行号

2. 分析错误代码
   └─ 对照错误类型表 → 确定问题类别

3. 检查源码
   └─ 打开错误文件 → 检查语法和引用

4. 修复问题
   └─ 根据解决方案修改代码

5. 重新编译验证
   └─ 执行编译命令 → 确认错误已解决
```

### 5.4 快速诊断命令

```bash
# 查看最近的编译错误
grep -i "error" "E:/Game/steam/steamapps/common/tModLoader/tModLoader-Logs/server.log" | tail -20

# 查看特定模组的编译结果
grep "trab" "E:/Game/steam/steamapps/common/tModLoader/tModLoader-Logs/server.log"

# 统计错误数量
grep -c "error CS" "E:/Game/steam/steamapps/common/tModLoader/tModLoader-Logs/server.log"
```

---

## 六、版本兼容性

### 6.1 tModLoader 版本对应

| tModLoader 版本 | Terraria 版本 | .NET 版本 | C# 版本 |
|-----------------|---------------|-----------|---------|
| 2026.03.x (1.4.4.9) | 1.4.4 | .NET 8.0 | C# 12 |
| 2025.12.x | 1.4.4 | .NET 8.0 | C# 12 |
| 2022.09.x (LTS) | 1.4.3 | .NET 6.0 | C# 10 |

### 6.2 版本检查命令

```bash
# 查看当前 tModLoader 版本
grep "Starting tModLoader" "E:/Game/steam/steamapps/common/tModLoader/tModLoader-Logs/client.log"

# 查看 .NET 版本要求
grep "dotnet_version" "E:/Game/steam/steamapps/common/tModLoader/tModLoader-Logs/environment-client.log"
```

### 6.3 条件编译符号

在 `tMLMod.targets` 中定义的符号：

```csharp
// 可用于版本检测的条件编译符号
#if TML_2026_03
    // 2026.03 版本特有代码
#endif
```

---

## 七、游戏内编译

### 7.1 编译步骤

1. 启动 tModLoader
2. 点击 **"模组"** → **"开发"**
3. 点击 **"编译所有本地模组"** 或选择特定模组
4. 查看编译结果窗口
5. 成功后点击 **"启用"**

### 7.2 快捷键

| 操作 | 快捷键 |
|------|--------|
| 打开模组菜单 | `Mod Sources` 按钮 |
| 编译选中模组 | 点击模组 → `Build` |
| 编译所有模组 | `Build All` |

### 7.3 开发者模式开启

确保 `build.txt` 存在且格式正确，tModLoader 会自动识别开发模组。

---

## 八、发布与分发

### 8.1 发布到 Steam Workshop

1. 编译成功后确保无错误
2. 准备完整描述文件 `description.txt` 和 `description_workshop.txt`
3. 游戏内点击 **"发布模组"**
4. 填写 Workshop 信息并上传

### 8.2 本地分发

将 `.tmod` 文件复制到其他玩家的 Mods 目录即可：

```
目标路径: C:/Users/<用户名>/Documents/My Games/Terraria/tModLoader/Mods/
```

---

## 九、最佳实践

### 9.1 开发建议

1. **频繁编译** - 开发过程中定期编译检查错误
2. **版本控制** - 使用 Git 管理源码
3. **增量编译** - 只修改必要文件后重新编译
4. **日志监控** - 关注警告信息，及时优化

### 9.2 性能优化

- 使用 `dotnet build -c Release` 发布版本
- 避免不必要的 DLL 引用
- 压缩资源文件（PNG、音频）
- 使用本地化代替硬编码字符串

### 9.3 调试技巧

```csharp
// 使用 Mod.Logger 输出调试信息
Logger.Info("调试信息");
Logger.Warn("警告信息");
Logger.Error("错误信息");

// 条件输出
#if DEBUG
    Logger.Info("仅 Debug 模式输出");
#endif
```

---

## 十、参考资料

### 10.1 官方文档

- [tModLoader Wiki](https://github.com/tModLoader/tModLoader/wiki)
- [tModLoader API 文档](https://docs.tmodloader.net/)
- [Terraria Wiki - Modding](https://terraria.wiki.gg/wiki/Modding)

### 10.2 本项目相关文档

- [tModLoader日志排查文档.md](tModLoader日志排查文档.md)
- [tModLoader_模组开发基础教程.md](tModLoader_模组开发基础教程.md)
- [WorldGen系统文档.md](WorldGen系统文档.md)

### 10.3 常用路径

| 路径 | 说明 |
|------|------|
| `E:/Game/steam/steamapps/common/tModLoader/` | tModLoader 安装目录 |
| `C:/Users/admin/Documents/My Games/Terraria/tModLoader/` | 存档目录 |
| `ModSources/` | 模组源码目录 |
| `Mods/` | 已编译模组目录 |
| `tModLoader-Logs/` | 日志目录 |

---

*文档版本: 1.0*
*创建日期: 2026-05-31*
*适用 tModLoader 版本: 2026.03.3.0 (1.4.4.9)*