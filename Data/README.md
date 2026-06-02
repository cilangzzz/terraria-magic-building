# Data 数据目录

本目录存放知识库数据文件，不含代码。

---

## 文件列表

| 文件 | 类型 | 大小 | 说明 |
|------|------|------|------|
| terraria_kb.db | SQLite | 140KB | 主数据库（11张表） |
| terraria_kb_full.sql | SQL | 29KB | 数据库SQL备份 |
| tile_embeddings.json | JSON | 180KB | 方块向量（35个，384维） |
| style_embeddings.json | JSON | 54KB | 风格向量（10个，384维） |
| biome_embeddings.json | JSON | 68KB | 生物群落向量（13个，384维） |

---

## 数据库表

`terraria_kb.db` 包含以下表：

| 表名 | 记录数 | 内容 |
|------|--------|------|
| tiles | 35 | 建筑方块 |
| walls | 23 | 建筑墙壁 |
| paints | 19 | 油漆颜色 |
| slopes | 6 | 斜坡类型 |
| furniture | 11 | 家具类型 |
| light_sources | 5 | 光源 |
| doors | 3 | 门类型 |
| style_templates | 10 | 建筑风格模板 |
| npc_requirements | 18 | NPC偏好要求 |
| house_validation | 9 | 房屋验证规则 |
| biomes | 13 | 生物群落 |

---

## 数据生成

数据文件由 `Tools/` 目录下的Python脚本生成：

```
Tools/database/init_full_db.py    → terraria_kb.db
Tools/vector/generate_embeddings_smart.py → *_embeddings.json
```

---

## C#加载

数据由 `Core/` 目录下的C#类加载：

```csharp
// Core/KnowledgeBase.cs
KnowledgeBaseManager.Instance.Initialize();
// 加载 terraria_kb.db

// Core/VectorKnowledgeBase.cs  
Vectors.Initialize();
// 加载 *_embeddings.json
```

---

## 注意事项

1. **仅存放数据文件**，C#代码文件应放在 `Core/` 目录
2. **向量文件版本**: 当前为 v3.0（语义关键词方法）
3. **SQL备份**: 可用于重建数据库或手动查看数据