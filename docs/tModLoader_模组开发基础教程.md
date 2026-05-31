# tModLoader 模组开发基础教程

## 目录
1. [tModLoader的安装和配置](#1-tmodloader的安装和配置)
2. [模组项目结构](#2-模组项目结构)
3. [基本的Mod类和ModSystem类使用](#3-基本的mod类和modsystem类使用)
4. [如何创建自定义物品和工具](#4-如何创建自定义物品和工具)
5. [如何在游戏中添加UI界面](#5-如何在游戏中添加ui界面)

---

## 1. tModLoader的安装和配置

### 1.1 通过Steam安装（推荐）

tModLoader已作为Terraria的免费DLC在Steam上提供：

1. **购买Terraria** - 确保Steam库中有Terraria游戏
2. **下载tModLoader** - 在Steam商店搜索"tModLoader"并安装
3. **启动tModLoader** - 在Steam库中会显示为独立游戏条目
4. **自动更新** - Steam会自动处理tModLoader的更新

### 1.2 手动安装（备用方案）

如果Steam版本有问题：

1. 从GitHub下载最新版本：https://github.com/tModLoader/tModLoader/releases
2. 解压到Terraria游戏目录
3. 运行`tModLoader.exe`或相应的启动脚本

### 1.3 开发环境配置

**所需工具：**
- Visual Studio 2022 或 JetBrains Rider
- .NET 6.0 SDK 或更高版本
- C# 开发经验

**配置步骤：**
1. 安装Visual Studio，选择".NET桌面开发"工作负载
2. 确保tModLoader已正确安装并运行过一次
3. 在tModLoader中选择"开发模组"选项

---

## 2. 模组项目结构

### 2.1 基本项目文件

一个标准的tModLoader模组项目结构如下：

```
MyMod/
├── MyMod.csproj          # 项目文件
├── build.txt             # 模组元数据
├── description.txt       # 模组描述（支持Markdown）
├── MyMod.cs              # 主Mod类
├── Items/                # 物品文件夹
│   ├── Weapons/
│   ├── Tools/
│   └── Accessories/
├── NPCs/                 # NPC文件夹
├── Tiles/                # 方块文件夹
├── Walls/                # 墙壁文件夹
├── UI/                   # UI界面文件夹
├── Systems/              # 系统类文件夹
├── Properties/           # 项目属性
│   └── launchSettings.json
└── Assets/               # 资源文件
    ├── Textures/
    └── Sounds/
```

### 2.2 build.txt 文件格式

`build.txt`文件包含模组的基本信息：

```properties
name = MyMod                    # 模组名称
version = 1.0.0                # 版本号（语义化版本）
author = YourName              # 作者名称
homepage = https://example.com # 主页链接（可选）
description = A sample mod     # 简短描述
side = Both                    # 运行端：Both, Client, Server
download = https://...         # 下载链接（可选）
```

### 2.3 description.txt 文件

`description.txt`支持Markdown格式，用于在模组浏览器中显示详细描述：

```markdown
# MyMod - 泰拉瑞亚增强模组

## 功能特性
- 添加了10种新武器
- 5个新NPC
- 自定义UI界面

## 更新日志
### v1.0.0
- 首次发布
```

### 2.4 .csproj 项目文件

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="tModLoader">
      <HintPath>$(tMLPath)\tModLoader.dll</HintPath>
    </Reference>
    <Reference Include="Terraria">
      <HintPath>$(tMLPath)\Terraria.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```

---

## 3. 基本的Mod类和ModSystem类使用

### 3.1 主Mod类

每个模组都需要一个继承自`Mod`的主类：

```csharp
using Terraria.ModLoader;

namespace MyMod
{
    public class MyMod : Mod
    {
        // 模组唯一标识符
        public override string Name => "MyMod";

        // 模组加载时调用（最早的生命周期方法）
        public override void Load()
        {
            // 初始化UI、注册事件等
            // 此时游戏内容尚未加载
        }

        // 卸载模组时调用
        public override void Unload()
        {
            // 清理静态引用，防止内存泄漏
        }

        // 所有内容加载完成后调用
        public override void PostSetupContent()
        {
            // 可以安全地引用其他模组的内容
            // 此时所有物品、NPC等都已加载
        }

        // 添加配方后调用
        public override void PostAddRecipes()
        {
            // 修改已有配方或添加复杂配方
        }

        // 添加翻译文本
        public override void AddRecipes()
        {
            // 添加物品配方
        }
    }
}
```

### 3.2 ModSystem类

`ModSystem`用于添加游戏系统逻辑：

```csharp
using Terraria.ModLoader;
using Terraria;

public class MyModSystem : ModSystem
{
    // 世界加载时调用
    public override void OnWorldLoad()
    {
        // 初始化世界状态
    }

    // 世界卸载时调用
    public override void OnWorldUnload()
    {
        // 清理世界相关数据
    }

    // 每帧更新（服务端和客户端）
    public override void PreUpdateEntities()
    {
        // 在实体更新前执行
    }

    // 每帧更新（仅客户端）
    public override void PostUpdateInput()
    {
        // 处理输入后的逻辑
    }

    // 修改界面层
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        // 添加或修改UI层
    }

    // 保存世界数据
    public override void SaveWorldData(TagCompound tag)
    {
        // 保存自定义世界数据
    }

    // 加载世界数据
    public override void LoadWorldData(TagCompound tag)
    {
        // 加载自定义世界数据
    }
}
```

### 3.3 常用生命周期方法对照表

| 方法名 | 调用时机 | 用途 |
|--------|----------|------|
| `Load()` | 模组加载时 | 初始化资源、注册事件 |
| `Unload()` | 模组卸载时 | 清理资源 |
| `PostSetupContent()` | 内容加载完成 | 引用其他模组内容 |
| `PostAddRecipes()` | 配方添加完成 | 修改现有配方 |
| `PreUpdateEntities()` | 每帧 | 游戏逻辑更新 |
| `ModifyInterfaceLayers()` | UI绘制 | 添加自定义UI |

---

## 4. 如何创建自定义物品和工具

### 4.1 基本物品类 (ModItem)

创建自定义物品需要继承`ModItem`类：

```csharp
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace MyMod.Items
{
    public class MyCustomItem : ModItem
    {
        // 设置物品属性
        public override void SetDefaults()
        {
            Item.width = 20;                    // 物品宽度（像素）
            Item.height = 20;                   // 物品高度（像素）
            Item.maxStack = 999;                // 最大堆叠数
            Item.value = Item.buyPrice(gold: 1); // 价值（1金币）
            Item.rare = ItemRarityID.Blue;      // 稀有度
            Item.useStyle = ItemUseStyleID.HoldUp; // 使用方式
            Item.useAnimation = 30;             // 使用动画时间
            Item.useTime = 30;                   // 使用时间
            Item.consumable = true;             // 是否可消耗
        }

        // 添加配方
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 10)        // 10个木材
                .AddIngredient(ItemID.IronBar, 5)      // 5个铁锭
                .AddTile(TileID.WorkBench)             // 在工作台制作
                .Register();
        }

        // 使用物品时调用
        public override bool? UseItem(Player player)
        {
            // 物品效果逻辑
            player.Heal(50); // 恢复50点生命值
            return true;     // 返回true表示使用成功
        }
    }
}
```

### 4.2 创建近战武器

```csharp
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace MyMod.Items.Weapons
{
    public class CustomSword : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 50;                       // 伤害值
            Item.DamageType = DamageClass.Melee;     // 伤害类型：近战
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 20;                       // 使用时间（帧）
            Item.useAnimation = 20;                  // 动画时间
            Item.useStyle = ItemUseStyleID.Swing;    // 挥动动画
            Item.knockBack = 6;                      // 击退力
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item1;           // 使用音效
            Item.autoReuse = true;                   // 自动连击
        }

        // 击中NPC时触发
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            // 给敌人添加燃烧效果
            target.AddBuff(BuffID.OnFire, 180); // 3秒燃烧
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.HellstoneBar, 15)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
```

### 4.3 创建远程武器（弓/枪）

```csharp
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace MyMod.Items.Weapons
{
    public class CustomBow : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 35;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 24;
            Item.height = 28;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 2;
            Item.value = Item.buyPrice(gold: 3);
            Item.rare = ItemRarityID.Orange;
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.WoodenArrowFriendly; // 默认发射物
            Item.shootSpeed = 10f;                           // 发射速度
            Item.useAmmo = AmmoID.Arrow;                     // 使用箭矢弹药
            Item.noMelee = true;                             // 不造成近战伤害
        }

        // 修改发射的发射物
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // 可以修改发射物类型、伤害等
            if (type == ProjectileID.WoodenArrowFriendly)
            {
                type = ProjectileID.FireArrow; // 替换为火焰箭
            }
        }
    }
}
```

### 4.4 创建工具（镐/斧/锤）

```csharp
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace MyMod.Items.Tools
{
    public class CustomPickaxe : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 10;
            Item.DamageType = DamageClass.Melee;
            Item.width = 24;
            Item.height = 24;
            Item.useTime = 15;
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.pick = 100;               // 挖掘力（100可挖掘困难模式矿石）
            Item.axe = 0;                  // 斧力（0表示不是斧头）
            Item.hammer = 0;               // 锤力
            Item.knockBack = 3;
            Item.value = Item.buyPrice(gold: 3);
            Item.rare = ItemRarityID.LightRed;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.HellstoneBar, 20)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
```

### 4.5 创建饰品

```csharp
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace MyMod.Items.Accessories
{
    public class CustomAccessory : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.value = Item.buyPrice(gold: 10);
            Item.rare = ItemRarityID.Yellow;
            Item.accessory = true; // 标记为饰品
        }

        // 装备时触发的效果
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 增加属性
            player.moveSpeed += 0.1f;        // 移动速度+10%
            player.maxMinions += 1;          // 召唤上限+1
            player.GetDamage(DamageClass.Melee) += 0.15f; // 近战伤害+15%
        }

        // 添加饰品修饰语提示
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.SoulofFlight, 10)
                .AddIngredient(ItemID.SoulofMight, 5)
                .AddTile(TileID.TinkerersWorkbench)
                .Register();
        }
    }
}
```

### 4.6 ModItem常用方法

| 方法名 | 用途 |
|--------|------|
| `SetDefaults()` | 设置物品基础属性 |
| `AddRecipes()` | 添加配方 |
| `UseItem(Player)` | 使用物品时的效果 |
| `OnHitNPC(...)` | 击中NPC时的效果 |
| `HoldItem(Player)` | 持有时的效果 |
| `UpdateAccessory(Player, bool)` | 饰品装备效果 |
| `ModifyHitNPC(...)` | 修改伤害/击退 |
| `CanUseItem(Player)` | 判断是否可以使用 |
| `Shoot(...)` | 发射发射物 |

---

## 5. 如何在游戏中添加UI界面

### 5.1 基本UI结构

tModLoader使用Terraria的UI系统，核心类包括：
- `UIState` - UI状态基类
- `UserInterface` - 管理UI状态
- `UIPanel` - 面板容器
- `UIText` - 文本元素
- `UIInputTextField` - 输入框

### 5.2 创建UI界面类

```csharp
using Terraria.UI;
using Terraria.GameContent.UI.Elements;
using Microsoft.Xna.Framework;
using Terraria.ModLoader.UI;

namespace MyMod.UI
{
    public class MyCustomUI : UIState
    {
        // UI元素引用
        private UIPanel mainPanel;
        private UIText titleText;
        private UIInputTextField inputField;

        // 初始化UI
        public override void OnInitialize()
        {
            // 创建主面板
            mainPanel = new UIPanel();
            mainPanel.Width.Set(400f, 0f);          // 宽度：400像素
            mainPanel.Height.Set(300f, 0f);         // 高度：300像素
            mainPanel.HAlign = 0.5f;                // 水平居中
            mainPanel.VAlign = 0.5f;                // 垂直居中
            mainPanel.BackgroundColor = new Color(50, 50, 80, 200); // 背景色
            mainPanel.BorderColor = Color.Cyan;     // 边框色
            Append(mainPanel);                      // 添加到UIState

            // 创建标题文本
            titleText = new UIText("My Custom UI", 1.2f, true);
            titleText.HAlign = 0.5f;                // 居中
            titleText.Top.Set(10f, 0f);             // 距顶部10像素
            mainPanel.Append(titleText);            // 添加到面板

            // 创建按钮
            UITextPanel<string> button = new UITextPanel<string>("Close", 0.8f, true);
            button.Width.Set(100f, 0f);
            button.Height.Set(30f, 0f);
            button.HAlign = 0.5f;
            button.Top.Set(250f, 0f);
            button.OnClick += OnCloseButtonClick;   // 绑定点击事件
            mainPanel.Append(button);

            // 创建输入框
            inputField = new UIInputTextField("Type here...");
            inputField.Width.Set(300f, 0f);
            inputField.Height.Set(30f, 0f);
            inputField.HAlign = 0.5f;
            inputField.Top.Set(100f, 0f);
            mainPanel.Append(inputField);
        }

        // 按钮点击事件处理
        private void OnCloseButtonClick(UIMouseEvent evt, UIElement listeningElement)
        {
            // 关闭UI
            MyModSystem.Instance.myInterface?.SetState(null);
        }

        // 每帧更新
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            // 更新逻辑
        }
    }
}
```

### 5.3 在ModSystem中管理UI

```csharp
using Terraria.ModLoader;
using Terraria.UI;
using Terraria;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MyMod
{
    public class MyModSystem : ModSystem
    {
        // 单例实例
        public static MyModSystem Instance;

        // UI接口
        internal UserInterface myInterface;

        // UI状态
        private MyCustomUI myUI;

        // UI是否可见
        public bool showUI;

        public override void Load()
        {
            Instance = this;

            // 创建UI
            myInterface = new UserInterface();
            myUI = new MyCustomUI();
            myUI.Activate(); // 初始化UI
        }

        public override void Unload()
        {
            Instance = null;
            myInterface = null;
            myUI = null;
        }

        // 更新UI
        public override void UpdateUI(GameTime gameTime)
        {
            if (showUI)
            {
                myInterface?.Update(gameTime);
            }
        }

        // 修改界面层
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // 找到物品栏层的位置
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));

            if (inventoryIndex != -1)
            {
                // 在物品栏之后插入自定义UI
                layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
                    "MyMod: MyCustomUI",
                    delegate
                    {
                        if (showUI)
                        {
                            myInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI
                ));
            }
        }
    }
}
```

### 5.4 按键控制UI显示

```csharp
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.UI;

namespace MyMod
{
    public class MyModKeybind : ModSystem
    {
        public static ModKeybind ToggleUIKey { get; private set; }

        public override void Load()
        {
            // 注册按键绑定
            ToggleUIKey = KeybindLoader.RegisterKeybind(Mod, "ToggleMyUI", "P");
        }

        public override void Unload()
        {
            ToggleUIKey = null;
        }

        public override void PostUpdateInput()
        {
            // 检测按键
            if (ToggleUIKey.JustPressed)
            {
                MyModSystem.Instance.showUI = !MyModSystem.Instance.showUI;

                if (MyModSystem.Instance.showUI)
                {
                    // 显示UI
                    MyModSystem.Instance.myInterface.SetState(MyModSystem.Instance.myUI);
                }
                else
                {
                    // 隐藏UI
                    MyModSystem.Instance.myInterface.SetState(null);
                }
            }
        }
    }
}
```

### 5.5 常用UI元素

| 类名 | 用途 |
|------|------|
| `UIPanel` | 基础面板容器 |
| `UIText` | 纯文本显示 |
| `UITextPanel<T>` | 带背景的文本面板（常用于按钮） |
| `UIImage` | 图片显示 |
| `UIInputTextField` | 文本输入框 |
| `UIList` | 列表容器（支持滚动） |
| `UIScrollbar` | 滚动条 |
| `UIProgressBar` | 进度条 |
| `UISlider` | 滑动条 |

### 5.6 UI事件

```csharp
// 点击事件
button.OnClick += (evt, element) => {
    // 处理点击
};

// 鼠标进入
button.OnMouseOver += (evt, element) => {
    // 鼠标悬停效果
};

// 鼠标离开
button.OnMouseOut += (evt, element) => {
    // 恢复效果
};

// 双击事件
button.OnDoubleClick += (evt, element) => {
    // 双击处理
};

// 右键点击
button.OnRightClick += (evt, element) => {
    // 右键处理
};
```

---

## 附录：常用资源链接

### 官方资源
- **tModLoader GitHub**: https://github.com/tModLoader/tModLoader
- **官方Wiki**: https://github.com/tModLoader/tModLoader/wiki
- **API文档**: https://tmodloader.github.io/tModLoader-Docs/
- **ExampleMod**: https://github.com/tModLoader/tModLoader/tree/stable/ExampleMod

### 社区资源
- **tModLoader Discord**: 官方Discord服务器
- **Reddit**: r/TerrariaModding
- **YouTube教程**: 搜索"tModLoader tutorial"

### 常用ID参考
- **物品ID**: `ItemID.ItemName`
- **NPC ID**: `NPCID.NPCName`
- **方块ID**: `TileID.TileName`
- **Buff ID**: `BuffID.BuffName`
- **音效ID**: `SoundID.SoundName`
- **稀有度**: `ItemRarityID.RarityName`

---

*文档整理自tModLoader官方文档和开发者社区*