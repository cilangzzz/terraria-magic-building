-- Terraria 建筑知识库数据库 (完整版)
-- 更新时间: 2026-06-01T20:40:54.363556

-- === tiles ===
INSERT OR IGNORE INTO tiles VALUES (1, 'StoneBlock', '石头', 'brick', '["medieval","natural","underground"]', '["forest","underground"]', 1, 1, 50, 0, '基础建筑材料', 'manual');
INSERT OR IGNORE INTO tiles VALUES (2, 'DirtBlock', '泥土', 'natural', '["natural","underground"]', '["forest","underground"]', 1, 1, 30, 0, '自然建筑材料', 'manual');
INSERT OR IGNORE INTO tiles VALUES (3, 'Grass', '草地', 'natural', '["natural"]', '["forest"]', 0, 0, 30, 0, '地表草地', 'manual');
INSERT OR IGNORE INTO tiles VALUES (4, 'Torch', '火把', 'light', '["any"]', '["any"]', 0, 0, 30, 10, '基础光源', 'manual');
INSERT OR IGNORE INTO tiles VALUES (5, 'Wood', '木材', 'wood', '["natural","medieval","rustic"]', '["forest"]', 1, 1, 50, 0, '基础木材', 'manual');
INSERT OR IGNORE INTO tiles VALUES (6, 'GrayBrick', '灰砖', 'brick', '["medieval","castle","urban"]', '["forest"]', 1, 0, 50, 0, '经典砖块', 'manual');
INSERT OR IGNORE INTO tiles VALUES (7, 'GoldBrick', '金砖', 'luxury', '["luxury","palace","treasure"]', '["any"]', 1, 0, 50, 0, '豪华金砖', 'manual');
INSERT OR IGNORE INTO tiles VALUES (8, 'SilverBrick', '银砖', 'luxury', '["luxury","ice"]', '["snow","ice"]', 1, 0, 50, 0, '银色砖块', 'manual');
INSERT OR IGNORE INTO tiles VALUES (9, 'CopperBrick', '铜砖', 'brick', '["steampunk","industrial"]', '["any"]', 1, 0, 50, 0, '铜色砖块', 'manual');
INSERT OR IGNORE INTO tiles VALUES (10, 'IronBrick', '铁砖', 'brick', '["steampunk","industrial"]', '["any"]', 1, 0, 50, 0, '铁色砖块', 'manual');
INSERT OR IGNORE INTO tiles VALUES (13, 'Glass', '玻璃', 'transparent', '["modern","aquarium"]', '["any"]', 1, 0, 20, 0, '透明方块', 'manual');
INSERT OR IGNORE INTO tiles VALUES (14, 'Platforms', '平台', 'platform', '["any"]', '["any"]', 1, 0, 20, 0, '可穿过的平台', 'manual');
INSERT OR IGNORE INTO tiles VALUES (33, 'Candle', '蜡烛', 'light', '["luxury"]', '["any"]', 0, 0, 30, 5, '小型光源', 'manual');
INSERT OR IGNORE INTO tiles VALUES (34, 'Chandelier', '吊灯', 'light', '["luxury","palace"]', '["any"]', 0, 0, 50, 15, '大型光源', 'manual');
INSERT OR IGNORE INTO tiles VALUES (38, 'RedBrick', '红砖', 'brick', '["urban","industrial"]', '["forest","desert"]', 1, 0, 50, 0, '红色砖块', 'manual');
INSERT OR IGNORE INTO tiles VALUES (41, 'Obsidian', '黑曜石', 'luxury', '["hell","dark","volcanic"]', '["hell"]', 1, 1, 100, 0, '地狱材料', 'manual');
INSERT OR IGNORE INTO tiles VALUES (42, 'Marble', '大理石', 'luxury', '["greek","roman","temple"]', '["underground"]', 1, 1, 50, 0, '古典风格材料', 'manual');
INSERT OR IGNORE INTO tiles VALUES (43, 'Granite', '花岗岩', 'luxury', '["modern","tech"]', '["underground"]', 1, 1, 50, 0, '现代风格材料', 'manual');
INSERT OR IGNORE INTO tiles VALUES (44, 'SnowBlock', '雪块', 'natural', '["snow","winter"]', '["snow","tundra"]', 1, 1, 50, 0, '雪地方块', 'manual');
INSERT OR IGNORE INTO tiles VALUES (45, 'IceBlock', '冰块', 'transparent', '["ice","frozen"]', '["snow","ice"]', 1, 1, 50, 0, '冰冻方块', 'manual');
INSERT OR IGNORE INTO tiles VALUES (46, 'Sandstone', '砂岩', 'brick', '["desert","egyptian"]', '["desert"]', 1, 1, 50, 0, '沙漠风格材料', 'manual');
INSERT OR IGNORE INTO tiles VALUES (47, 'RichMahogany', '红木', 'wood', '["jungle","luxury"]', '["jungle"]', 1, 1, 50, 0, '丛林木材', 'manual');
INSERT OR IGNORE INTO tiles VALUES (48, 'BorealWood', '针叶木', 'wood', '["snow","rustic"]', '["snow"]', 1, 1, 50, 0, '雪地木材', 'manual');
INSERT OR IGNORE INTO tiles VALUES (49, 'PalmWood', '棕榈木', 'wood', '["tropical","beach"]', '["ocean","desert"]', 1, 1, 50, 0, '海滩木材', 'manual');
INSERT OR IGNORE INTO tiles VALUES (93, 'Lamp', '立灯', 'light', '["modern"]', '["any"]', 0, 0, 50, 10, '立式灯具', 'manual');
INSERT OR IGNORE INTO tiles VALUES (143, 'StoneSlab', '石板', 'brick', '["medieval","castle","temple"]', '["forest"]', 1, 0, 50, 0, '平整石板', 'manual');
INSERT OR IGNORE INTO tiles VALUES (166, 'Pearlstone', '珍珠石', 'luxury', '["hallow","divine","fantasy"]', '["hallow"]', 1, 1, 50, 1, '神圣风格石', 'manual');
INSERT OR IGNORE INTO tiles VALUES (168, 'Ebonstone', '黑檀石', 'brick', '["corruption","dark"]', '["corruption"]', 1, 1, 50, 0, '腐化风格石材', 'manual');
INSERT OR IGNORE INTO tiles VALUES (169, 'Crimstone', '猩红石', 'brick', '["crimson","blood"]', '["crimson"]', 1, 1, 50, 0, '猩红风格石材', 'manual');
INSERT OR IGNORE INTO tiles VALUES (179, 'SandstoneSlab', '砂岩板', 'brick', '["desert","egyptian"]', '["desert"]', 1, 0, 50, 0, '沙漠石板', 'manual');
INSERT OR IGNORE INTO tiles VALUES (215, 'Campfire', '篝火', 'light', '["outdoor"]', '["any"]', 0, 0, 50, 20, '提供生命恢复', 'manual');
INSERT OR IGNORE INTO tiles VALUES (216, 'MushroomBlock', '蘑菇块', 'brick', '["glowing_mushroom"]', '["glowing_mushroom"]', 1, 1, 50, 5, '发光蘑菇方块', 'manual');
INSERT OR IGNORE INTO tiles VALUES (386, 'TrapDoor', '活板门', 'door', '["any"]', '["any"]', 1, 0, 50, 0, '水平门', 'manual');
INSERT OR IGNORE INTO tiles VALUES (389, 'TallGate', '大门', 'door', '["farm"]', '["any"]', 1, 0, 50, 0, '农场大门', 'manual');
INSERT OR IGNORE INTO tiles VALUES (633, 'DynastyWood', '王朝木', 'wood', '["asian","eastern"]', '["any"]', 1, 1, 50, 0, '东方风格木材', 'manual');

-- === walls ===
INSERT OR IGNORE INTO walls VALUES (1, 'StoneWall', '石墙', 'brick', '["medieval","natural"]', 1, 0, '基础石墙', 'manual');
INSERT OR IGNORE INTO walls VALUES (2, 'DirtWall', '泥墙', 'natural', '["natural","underground"]', 1, 1, '泥土墙', 'manual');
INSERT OR IGNORE INTO walls VALUES (4, 'WoodWall', '木墙', 'wood', '["natural","medieval"]', 1, 0, '基础木墙', 'manual');
INSERT OR IGNORE INTO walls VALUES (5, 'GrayBrickWall', '灰砖墙', 'brick', '["medieval","castle"]', 1, 0, '灰砖墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (6, 'GoldBrickWall', '金砖墙', 'luxury', '["luxury","palace"]', 1, 0, '金色墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (7, 'SilverBrickWall', '银砖墙', 'luxury', '["ice"]', 1, 0, '银色墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (8, 'CopperBrickWall', '铜砖墙', 'brick', '["steampunk"]', 1, 0, '铜色墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (9, 'IronBrickWall', '铁砖墙', 'brick', '["steampunk"]', 1, 0, '铁色墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (10, 'GlassWall', '玻璃墙', 'transparent', '["modern","aquarium"]', 1, 0, '透明墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (11, 'StoneSlabWall', '石板墙', 'brick', '["medieval","temple"]', 1, 0, '石板墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (12, 'SandstoneWall', '砂岩墙', 'brick', '["desert","egyptian"]', 1, 0, '沙漠墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (13, 'SnowWall', '雪墙', 'natural', '["snow","winter"]', 1, 0, '雪地墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (14, 'MarbleWall', '大理石墙', 'luxury', '["greek","roman"]', 1, 0, '古典墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (15, 'GraniteWall', '花岗岩墙', 'luxury', '["modern"]', 1, 0, '现代墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (16, 'PearlstoneWall', '珍珠石墙', 'luxury', '["hallow","divine"]', 1, 0, '神圣墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (17, 'EbonstoneWall', '黑檀石墙', 'brick', '["corruption","dark"]', 1, 0, '腐化墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (18, 'CrimstoneWall', '猩红石墙', 'brick', '["crimson","blood"]', 1, 0, '猩红墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (19, 'RedBrickWall', '红砖墙', 'brick', '["urban"]', 1, 0, '红色墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (20, 'DynastyWall', '王朝墙', 'wood', '["asian","eastern"]', 1, 0, '东方墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (21, 'MushroomWall', '蘑菇墙', 'brick', '["glowing_mushroom"]', 1, 0, '发光蘑菇墙壁', 'manual');
INSERT OR IGNORE INTO walls VALUES (22, 'BorealWoodWall', '针叶木墙', 'wood', '["snow","rustic"]', 1, 0, '雪地木墙', 'manual');
INSERT OR IGNORE INTO walls VALUES (23, 'PalmWoodWall', '棕榈木墙', 'wood', '["tropical","beach"]', 1, 0, '海滩木墙', 'manual');
INSERT OR IGNORE INTO walls VALUES (24, 'RichMahoganyWall', '红木墙', 'wood', '["jungle","luxury"]', 1, 0, '丛林木墙', 'manual');


INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (0, 'None', '无油漆', NULL, 'none', '无油漆效果');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (1, 'Red', '红色', '#FF0000', 'color', '红色油漆');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (2, 'Orange', '橙色', '#FF8000', 'color', '橙色油漆');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (3, 'Yellow', '黄色', '#FFFF00', 'color', '黄色油漆');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (4, 'Lime', '青柠', '#80FF00', 'color', '青柠色油漆');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (5, 'Green', '绿色', '#00FF00', 'color', '绿色油漆');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (6, 'Teal', '青色', '#00FF80', 'color', '青色油漆');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (7, 'Cyan', '蓝绿', '#00FFFF', 'color', '蓝绿色油漆');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (8, 'SkyBlue', '天蓝', '#0080FF', 'color', '天蓝色油漆');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (9, 'Blue', '蓝色', '#0000FF', 'color', '蓝色油漆');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (10, 'Purple', '紫色', '#8000FF', 'color', '紫色油漆');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (11, 'Violet', '紫罗兰', '#FF00FF', 'color', '紫罗兰色油漆');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (12, 'Pink', '粉色', '#FF0080', 'color', '粉色油漆');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (13, 'Crimson', '深红', '#FF4040', 'color', '深红色油漆');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (14, 'Azure', '蔚蓝', '#4040FF', 'color', '蔚蓝色油漆');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (28, 'Shadow', '阴影', NULL, 'depth', '阴影油漆，增加建筑层次感');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (29, 'Negative', '反转', NULL, 'special', '反转颜色效果');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (30, 'White', '白色', '#FFFFFF', 'color', '白色油漆');
INSERT OR IGNORE INTO paints (id, name, display_name, color_hex, effect_type, description) VALUES (31, 'Black', '黑色', '#000000', 'color', '黑色油漆');

-- === slopes ===
INSERT OR IGNORE INTO slopes (id, name, display_name, description) VALUES (0, 'Solid', '完整方块', '标准完整方块');
INSERT OR IGNORE INTO slopes (id, name, display_name, description) VALUES (1, 'HalfBlock', '半砖', '只有下半部分，可作为台阶');
INSERT OR IGNORE INTO slopes (id, name, display_name, description) VALUES (2, 'SlopeDownRight', '右上斜坡', '从左上到右下的斜坡');
INSERT OR IGNORE INTO slopes (id, name, display_name, description) VALUES (3, 'SlopeDownLeft', '左上斜坡', '从右上到左下的斜坡');
INSERT OR IGNORE INTO slopes (id, name, display_name, description) VALUES (4, 'SlopeUpRight', '右下斜坡', '从左下到右上的斜坡');
INSERT OR IGNORE INTO slopes (id, name, display_name, description) VALUES (5, 'SlopeUpLeft', '左下斜坡', '从右下到左上的斜坡');

-- === furniture ===
INSERT OR IGNORE INTO furniture (id, name, display_name, category, width, height, npc_function, paint_compatible, description, source) VALUES (14, 'Tables', '桌子', 'table', 3, 1, '["flat_surface"]', 1, '平坦表面家具', NULL);
INSERT OR IGNORE INTO furniture (id, name, display_name, category, width, height, npc_function, paint_compatible, description, source) VALUES (15, 'Chairs', '椅子', 'chair', 1, 2, '["comfort"]', 1, '舒适物品', NULL);
INSERT OR IGNORE INTO furniture (id, name, display_name, category, width, height, npc_function, paint_compatible, description, source) VALUES (17, 'WorkBench', '工作台', 'crafting', 2, 1, '["flat_surface", "crafting"]', 1, '基础制作站', NULL);
INSERT OR IGNORE INTO furniture (id, name, display_name, category, width, height, npc_function, paint_compatible, description, source) VALUES (19, 'Platforms', '平台', 'platform', 1, 1, '["door"]', 1, '可作为入口', NULL);
INSERT OR IGNORE INTO furniture (id, name, display_name, category, width, height, npc_function, paint_compatible, description, source) VALUES (21, 'Chests', '宝箱', 'storage', 2, 2, '["storage"]', 0, '存储容器', NULL);
INSERT OR IGNORE INTO furniture (id, name, display_name, category, width, height, npc_function, paint_compatible, description, source) VALUES (79, 'Beds', '床', 'bed', 4, 2, '["comfort", "spawn_point"]', 1, '可设置重生点', NULL);
INSERT OR IGNORE INTO furniture (id, name, display_name, category, width, height, npc_function, paint_compatible, description, source) VALUES (87, 'Pianos', '钢琴', 'decoration', 3, 2, '["flat_surface"]', 1, '装饰家具', NULL);
INSERT OR IGNORE INTO furniture (id, name, display_name, category, width, height, npc_function, paint_compatible, description, source) VALUES (88, 'Dressers', '梳妆台', 'storage', 3, 2, '["flat_surface", "storage"]', 1, '存储+平坦表面', NULL);
INSERT OR IGNORE INTO furniture (id, name, display_name, category, width, height, npc_function, paint_compatible, description, source) VALUES (89, 'Sofas', '沙发', 'comfort', 3, 2, '["comfort"]', 1, '舒适物品', NULL);
INSERT OR IGNORE INTO furniture (id, name, display_name, category, width, height, npc_function, paint_compatible, description, source) VALUES (90, 'Bathtubs', '浴缸', 'decoration', 4, 2, NULL, 1, '浴室装饰', NULL);
INSERT OR IGNORE INTO furniture (id, name, display_name, category, width, height, npc_function, paint_compatible, description, source) VALUES (101, 'Bookcases', '书架', 'decoration', 3, 4, '["flat_surface"]', 1, '书籍存储', NULL);

-- === light_sources ===
INSERT OR IGNORE INTO light_sources (id, name, display_name, category, width, height, light_radius, npc_function, placement_type, description) VALUES (4, 'Torches', '火把', 'torch', 1, 1, 10, '["light_source"]', 'wall/floor', '基础光源');
INSERT OR IGNORE INTO light_sources (id, name, display_name, category, width, height, light_radius, npc_function, placement_type, description) VALUES (33, 'Candles', '蜡烛', 'candle', 1, 1, 5, '["light_source"]', 'table', '小型光源');
INSERT OR IGNORE INTO light_sources (id, name, display_name, category, width, height, light_radius, npc_function, placement_type, description) VALUES (34, 'Chandeliers', '吊灯', 'chandelier', 3, 3, 15, '["light_source"]', 'ceiling', '大型光源');
INSERT OR IGNORE INTO light_sources (id, name, display_name, category, width, height, light_radius, npc_function, placement_type, description) VALUES (93, 'Lamps', '立灯', 'lamp', 1, 3, 10, '["light_source"]', 'floor', '立式灯具');
INSERT OR IGNORE INTO light_sources (id, name, display_name, category, width, height, light_radius, npc_function, placement_type, description) VALUES (215, 'Campfires', '篝火', 'campfire', 3, 2, 20, '["light_source", "regeneration"]', 'floor', '提供生命恢复');

-- === doors ===
INSERT OR IGNORE INTO doors (id, name, display_name, category, width, height, npc_function, paint_compatible, description) VALUES (10, 'Doors', '门', 'standard', 1, 3, '["door"]', 1, '标准门');
INSERT OR IGNORE INTO doors (id, name, display_name, category, width, height, npc_function, paint_compatible, description) VALUES (387, 'TrapDoor', '活板门', 'trapdoor', 2, 1, '["door"]', 1, '水平门');
INSERT OR IGNORE INTO doors (id, name, display_name, category, width, height, npc_function, paint_compatible, description) VALUES (388, 'TallGate', '大门', 'gate', 1, 5, '["door"]', 1, '农场大门');

-- === style_templates ===
INSERT OR IGNORE INTO style_templates (id, name, display_name, description, primary_tiles, primary_walls, accent_tiles, roof_style, roof_tiles, furniture_style, paint_scheme, architectural_rules, biome_recommendations, difficulty) VALUES (1, 'medieval', '中世纪风格', '经典欧洲中世纪建筑风格，适合城堡、村庄和堡垒', '["GrayBrick", "StoneSlab", "Stone", "Wood"]', '["GrayBrickWall", "StoneWall", "WoodWall"]', '["GoldBrick", "IronBrick"]', 'triangular', '["Wood", "GrayBrick"]', '["WorkBench", "Tables", "Chairs", "Torches", "Banners"]', '{"primary": 0, "shadow": 28}', '["\u4f7f\u7528\u7070\u7816\u4f5c\u4e3a\u4e3b\u8981\u5899\u58c1", "\u6dfb\u52a0\u6728\u8d28\u5c4b\u9876", "\u4f7f\u7528\u9634\u5f71\u6cb9\u6f06\u589e\u52a0\u5c42\u6b21", "\u653e\u7f6e\u706b\u628a\u4f5c\u4e3a\u5149\u6e90"]', '["forest", "plain"]', 'medium');
INSERT OR IGNORE INTO style_templates (id, name, display_name, description, primary_tiles, primary_walls, accent_tiles, roof_style, roof_tiles, furniture_style, paint_scheme, architectural_rules, biome_recommendations, difficulty) VALUES (2, 'fantasy', '奇幻风格', '魔法与幻想风格，适合神圣之地建筑', '["Pearlstone", "Glass", "GoldBrick"]', '["PearlstoneWall", "GlassWall"]', '["CrystalBlock", "RainbowBrick"]', 'dome', '["Pearlstone", "Glass"]', '["CrystalChandelier", "Tables", "Chairs"]', '{}', '["\u4f7f\u7528\u73cd\u73e0\u77f3\u4f5c\u4e3a\u4e3b\u4f53", "\u5927\u91cf\u4f7f\u7528\u73bb\u7483", "\u5706\u9876\u5c4b\u9876\u8bbe\u8ba1"]', '["hallow"]', 'hard');
INSERT OR IGNORE INTO style_templates (id, name, display_name, description, primary_tiles, primary_walls, accent_tiles, roof_style, roof_tiles, furniture_style, paint_scheme, architectural_rules, biome_recommendations, difficulty) VALUES (3, 'steampunk', '蒸汽朋克风格', '工业革命风格，适合机械建筑', '["CopperBrick", "IronBrick", "GearBlock"]', '["CopperBrickWall", "IronBrickWall"]', '["Cogs", "MetalBars"]', 'flat', '["CopperBrick", "IronBrick"]', '["WorkBench", "Anvil", "Furnaces"]', '{"primary": 13, "metal": 14}', '["\u4f7f\u7528\u94dc\u7816\u548c\u94c1\u7816", "\u6dfb\u52a0\u9f7f\u8f6e\u88c5\u9970", "\u5de5\u4e1a\u98ce\u683c\u5e73\u9876"]', '["any"]', 'medium');
INSERT OR IGNORE INTO style_templates (id, name, display_name, description, primary_tiles, primary_walls, accent_tiles, roof_style, roof_tiles, furniture_style, paint_scheme, architectural_rules, biome_recommendations, difficulty) VALUES (4, 'natural', '自然风格', '与自然融合的建筑风格', '["Wood", "Dirt", "Stone"]', '["WoodWall", "DirtWall", "LivingWoodWall"]', '["LeafBlock", "Flowers"]', 'curved', '["Wood", "LivingWood"]', '["WorkBench", "Tables", "Chairs"]', '{"primary": 0}', '["\u4f7f\u7528\u6728\u6750\u548c\u6ce5\u571f", "\u4e0e\u5468\u56f4\u5730\u5f62\u878d\u5408", "\u4e0d\u89c4\u5219\u5f62\u72b6"]', '["forest", "jungle"]', 'easy');
INSERT OR IGNORE INTO style_templates (id, name, display_name, description, primary_tiles, primary_walls, accent_tiles, roof_style, roof_tiles, furniture_style, paint_scheme, architectural_rules, biome_recommendations, difficulty) VALUES (5, 'asian', '东方风格', '中日式建筑风格', '["DynastyWood", "Wood", "BambooBlock"]', '["DynastyWall", "BambooWall"]', '["PaperLantern", "Teapot"]', 'curved', '["DynastyWood"]', '["ChineseLantern", "Teacup", "Tables"]', '{"primary": 0, "accent": 1}', '["\u4f7f\u7528\u738b\u671d\u6728", "\u5f2f\u66f2\u5c4b\u9876", "\u60ac\u6302\u706f\u7b3c"]', '["any"]', 'medium');
INSERT OR IGNORE INTO style_templates (id, name, display_name, description, primary_tiles, primary_walls, accent_tiles, roof_style, roof_tiles, furniture_style, paint_scheme, architectural_rules, biome_recommendations, difficulty) VALUES (6, 'snow', '冰雪风格', '冬季风格建筑', '["SnowBlock", "IceBlock", "BorealWood"]', '["SnowWall", "IceWall", "BorealWoodWall"]', '["FrozenFurniture"]', 'triangular', '["SnowBlock", "BorealWood"]', '["IceChandelier", "Tables", "Chairs"]', '{"primary": 0, "ice": 7}', '["\u4f7f\u7528\u96ea\u5757\u548c\u51b0\u5757", "\u9488\u53f6\u6728\u7ed3\u6784", "\u5723\u8bde\u88c5\u9970"]', '["snow", "tundra"]', 'easy');
INSERT OR IGNORE INTO style_templates (id, name, display_name, description, primary_tiles, primary_walls, accent_tiles, roof_style, roof_tiles, furniture_style, paint_scheme, architectural_rules, biome_recommendations, difficulty) VALUES (7, 'desert', '沙漠风格', '沙漠和古埃及风格', '["Sandstone", "SandstoneSlab", "PalmWood"]', '["SandstoneWall"]', '["GoldBrick", "Scarab"]', 'flat', '["Sandstone"]', '["Tables", "Chairs", "Candelabra"]', '{"primary": 2, "accent": 3}', '["\u4f7f\u7528\u7802\u5ca9", "\u5e73\u9876\u6216\u91d1\u5b57\u5854", "\u91d1\u8272\u88c5\u9970"]', '["desert"]', 'medium');
INSERT OR IGNORE INTO style_templates (id, name, display_name, description, primary_tiles, primary_walls, accent_tiles, roof_style, roof_tiles, furniture_style, paint_scheme, architectural_rules, biome_recommendations, difficulty) VALUES (8, 'underground', '地下风格', '地下洞穴风格', '["Stone", "Dirt", "Wood"]', '["StoneWall", "DirtWall"]', '["GemsparkBlocks"]', 'irregular', '["Wood"]', '["Torches", "WorkBench", "Chairs"]', '{"primary": 0, "shadow": 28}', '["\u5229\u7528\u81ea\u7136\u6d1e\u7a74", "\u6728\u8d28\u5e73\u53f0\u697c\u68af", "\u5927\u91cf\u706b\u628a\u7167\u660e"]', '["underground", "cavern"]', 'easy');
INSERT OR IGNORE INTO style_templates (id, name, display_name, description, primary_tiles, primary_walls, accent_tiles, roof_style, roof_tiles, furniture_style, paint_scheme, architectural_rules, biome_recommendations, difficulty) VALUES (9, 'ocean', '海洋风格', '海洋和海滩风格', '["PalmWood", "Glass", "Coral"]', '["GlassWall"]', '["Seashell", "Starfish"]', 'triangular', '["PalmWood"]', '["Tables", "Chairs", "Lanterns"]', '{"primary": 0, "ocean": 8}', '["\u4f7f\u7528\u68d5\u6988\u6728", "\u5927\u91cf\u73bb\u7483", "\u6d77\u6d0b\u88c5\u9970"]', '["ocean", "beach"]', 'easy');
INSERT OR IGNORE INTO style_templates (id, name, display_name, description, primary_tiles, primary_walls, accent_tiles, roof_style, roof_tiles, furniture_style, paint_scheme, architectural_rules, biome_recommendations, difficulty) VALUES (10, 'modern', '现代风格', '现代简约风格', '["Granite", "Glass", "Marble"]', '["GraniteWall", "GlassWall"]', '["MetalBars"]', 'flat', '["Granite"]', '["Tables", "Sofas", "Lamps"]', '{"primary": 30, "accent": 14}', '["\u4f7f\u7528\u82b1\u5c97\u5ca9\u548c\u5927\u7406\u77f3", "\u5927\u91cf\u73bb\u7483", "\u7b80\u7ea6\u7ebf\u6761"]', '["any"]', 'medium');

-- === npc_requirements ===
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (1, 'merchant', '商人', '总资产超过50银币', 'forest', NULL, '["nurse", "goblin_tinkerer"]', '["tax_collector"]', NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (2, 'nurse', '护士', '生命值超过100', 'forest', NULL, '["merchant"]', '["tax_collector"]', NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (3, 'demolitionist', '炸弹专家', '背包中有炸弹', 'underground', NULL, '["merchant"]', NULL, NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (4, 'gunsmith', '军火商', '背包中有枪或子弹', 'forest', NULL, '["nurse"]', '["demolitionist"]', NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (5, 'dryad', '树妖', '击败任何Boss', 'forest', NULL, '["witch_doctor"]', '["truffle"]', NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (6, 'wizard', '巫师', '击败骷髅王', 'hallow', NULL, '["nurse"]', NULL, NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (7, 'goblin_tinkerer', '哥布林工匠', '击败哥布林军队后地下找到', 'underground', NULL, '["mechanic", "cyborg"]', NULL, NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (8, 'mechanic', '电工', '地牢中解救', 'underground', NULL, '["goblin_tinkerer", "cyborg"]', NULL, NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (9, 'stylist', '发型师', '蜘蛛巢中解救', 'forest', NULL, '["dye_trader"]', NULL, NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (10, 'angler', '钓鱼佬', '海边对话', 'ocean', NULL, NULL, '["tax_collector", "merchant"]', NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (11, 'witch_doctor', '巫医', '击败骷髅王', 'jungle', NULL, '["dryad"]', NULL, NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (12, 'truffle', '松露人', '地表蘑菇生物群落', 'glowing_mushroom', NULL, NULL, '["dryad"]', NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (13, 'steampunker', '蒸汽朋克人', '击败机械Boss', 'forest', NULL, '["cyborg", "mechanic"]', NULL, NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (14, 'cyborg', '半机械人', '击败火星入侵', 'forest', NULL, '["mechanic", "goblin_tinkerer"]', NULL, NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (15, 'tax_collector', '税务官', '地狱中用净化粉末解救', NULL, NULL, NULL, NULL, NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (16, 'painter', '画家', '背包中有油漆', 'forest', NULL, '["stylist"]', NULL, NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (17, 'party_girl', '派对女孩', '有8个NPC后随机出现', 'hallow', NULL, NULL, NULL, NULL);
INSERT OR IGNORE INTO npc_requirements (id, npc_name, display_name, spawn_condition, preferred_biome, disliked_biome, preferred_neighbors, disliked_neighbors, special_furniture) VALUES (18, 'dye_trader', '染料商', '背包中有染料材料', 'desert', NULL, '["stylist"]', NULL, NULL);

-- === house_validation ===
INSERT OR IGNORE INTO house_validation (id, rule_name, rule_type, requirement, minimum_value, required_elements, description) VALUES (1, 'minimum_tiles', 'size', '房间内部至少60格空地', 60, NULL, '房间必须有足够空间');
INSERT OR IGNORE INTO house_validation (id, rule_name, rule_type, requirement, minimum_value, required_elements, description) VALUES (2, 'light_source', 'furniture', '必需一个光源', 1, '["Torches", "Candles", "Chandeliers"]', '火把、蜡烛、吊灯等');
INSERT OR IGNORE INTO house_validation (id, rule_name, rule_type, requirement, minimum_value, required_elements, description) VALUES (3, 'flat_surface', 'furniture', '必需一个平坦表面', 1, '["Tables", "WorkBench", "Dressers"]', '桌子、工作台、梳妆台');
INSERT OR IGNORE INTO house_validation (id, rule_name, rule_type, requirement, minimum_value, required_elements, description) VALUES (4, 'comfort', 'furniture', '必需一个舒适物品', 1, '["Chairs", "Beds", "Sofas"]', '椅子、床、沙发');
INSERT OR IGNORE INTO house_validation (id, rule_name, rule_type, requirement, minimum_value, required_elements, description) VALUES (5, 'door', 'furniture', '必需一个入口', 1, '["Doors", "TrapDoor", "Platforms"]', '门、活板门、平台');
INSERT OR IGNORE INTO house_validation (id, rule_name, rule_type, requirement, minimum_value, required_elements, description) VALUES (6, 'wall_required', 'wall', '必须有背景墙', NULL, NULL, '天然墙无效，需要玩家放置的墙');
INSERT OR IGNORE INTO house_validation (id, rule_name, rule_type, requirement, minimum_value, required_elements, description) VALUES (7, 'frame_required', 'frame', '必须被实心方块包围', NULL, NULL, '四周有实心方块作为框架');
INSERT OR IGNORE INTO house_validation (id, rule_name, rule_type, requirement, minimum_value, required_elements, description) VALUES (8, 'max_size', 'size', '房间最大尺寸限制', NULL, NULL, '不宜过大以免影响判定');
INSERT OR IGNORE INTO house_validation (id, rule_name, rule_type, requirement, minimum_value, required_elements, description) VALUES (9, 'biome_check', 'biome', '不能处于邪恶生物群落', NULL, NULL, '腐化/猩红/神圣生物群落会影响NPC');

-- === biomes ===
INSERT OR IGNORE INTO biomes (id, name, display_name, category, characteristic_tiles, characteristic_walls, description) VALUES (1, 'forest', '森林', 'surface', '["Grass", "Wood", "Stone"]', '["WoodWall", "StoneWall"]', '基础森林生物群落');
INSERT OR IGNORE INTO biomes (id, name, display_name, category, characteristic_tiles, characteristic_walls, description) VALUES (2, 'desert', '沙漠', 'surface', '["Sand", "Sandstone", "HardenedSand"]', '["SandstoneWall"]', '沙漠生物群落');
INSERT OR IGNORE INTO biomes (id, name, display_name, category, characteristic_tiles, characteristic_walls, description) VALUES (3, 'snow', '雪地', 'surface', '["SnowBlock", "IceBlock", "BorealWood"]', '["SnowWall", "IceWall"]', '雪地生物群落');
INSERT OR IGNORE INTO biomes (id, name, display_name, category, characteristic_tiles, characteristic_walls, description) VALUES (4, 'jungle', '丛林', 'surface', '["JungleGrass", "RichMahogany", "Mud"]', '["JungleWall"]', '丛林生物群落');
INSERT OR IGNORE INTO biomes (id, name, display_name, category, characteristic_tiles, characteristic_walls, description) VALUES (5, 'ocean', '海洋', 'surface', '["Sand", "Coral", "PalmWood"]', '["GlassWall"]', '海洋生物群落');
INSERT OR IGNORE INTO biomes (id, name, display_name, category, characteristic_tiles, characteristic_walls, description) VALUES (6, 'underground', '地下', 'underground', '["Stone", "Dirt"]', '["StoneWall", "DirtWall"]', '地下生物群落');
INSERT OR IGNORE INTO biomes (id, name, display_name, category, characteristic_tiles, characteristic_walls, description) VALUES (7, 'cavern', '洞穴', 'underground', '["Stone", "Marble", "Granite"]', '["StoneWall"]', '深层洞穴');
INSERT OR IGNORE INTO biomes (id, name, display_name, category, characteristic_tiles, characteristic_walls, description) VALUES (8, 'hallow', '神圣', 'special', '["Pearlstone", "Pearlwood"]', '["PearlstoneWall"]', '神圣生物群落');
INSERT OR IGNORE INTO biomes (id, name, display_name, category, characteristic_tiles, characteristic_walls, description) VALUES (9, 'corruption', '腐化', 'special', '["Ebonstone", "Ebonsand"]', '["EbonstoneWall"]', '腐化生物群落');
INSERT OR IGNORE INTO biomes (id, name, display_name, category, characteristic_tiles, characteristic_walls, description) VALUES (10, 'crimson', '猩红', 'special', '["Crimstone", "Crimsand"]', '["CrimstoneWall"]', '猩红生物群落');
INSERT OR IGNORE INTO biomes (id, name, display_name, category, characteristic_tiles, characteristic_walls, description) VALUES (11, 'glowing_mushroom', '发光蘑菇', 'special', '["MushroomBlock", "GlowingMushroom"]', '["MushroomWall"]', '发光蘑菇生物群落');
INSERT OR IGNORE INTO biomes (id, name, display_name, category, characteristic_tiles, characteristic_walls, description) VALUES (12, 'dungeon', '地牢', 'special', '["DungeonBrick"]', '["DungeonWall"]', '地牢生物群落');
INSERT OR IGNORE INTO biomes (id, name, display_name, category, characteristic_tiles, characteristic_walls, description) VALUES (13, 'hell', '地狱', 'underground', '["Obsidian", "Hellstone", "Ash"]', '["ObsidianWall"]', '地狱生物群落');
