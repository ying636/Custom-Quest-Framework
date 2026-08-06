---
name: "cqf-def-catalog"
description: "RimWorld 原版 + CQF 有效 defName 速查表。编写 CQF 地图/任务/交互时调用，避免使用不存在的物品名或拼错 defName。"
---

# CQF Def 速查手册

本 Skill 提供编写 CQF 地图时所需的有效 defName 速查，**覆盖常见的建筑、物品、地形、技能**。所有 defName 均来自 RimWorld 源码 `ThingDefOf`/`TerrainDefOf`/`SkillDefOf` 或 CQF 的 XML 定义。

## 来源标记说明

| 标记 | 含义 |
|------|------|
| **Core** | 原版核心（无 DLC 需求） |
| **Royalty** | 皇家 DLC |
| **Ideology** | 文化 DLC |
| **Biotech** | 生物科技 DLC |
| **Anomaly** | 异象 DLC |
| **CQF** | CQF Mod 提供 |

---

## 总原则

**永远不要靠直觉拼接 defName。** 以下是不存在的 defName（实战反例）：
- `SteelWall` → 正确是 `Wall` + `<stuff>Steel</stuff>`
- `MarbleWall` → 正确是 `Wall` + `<stuff>Marble</stuff>`
- `TileFine` → 正确是 `FineTileMarble`
- `ConcreteTile` → 正确是 `Concrete`
- `MetalTile` → 正确是 `MetalTile`（TerrainDef，label 为"steel tile"）
- ❌ `SteelTile` 不存在！不是 TerrainDef，也不是 ThingDef

### Stuff 核心规则

| 规则 | 说明 | 反例 |
|------|------|------|
| **石质用砖块名** | 石材做 stuff 用 `BlocksMarble`、`BlocksGranite` 等 | ❌ `Marble`、`Granite`（岩石本体无 `IsStuff`） |
| **金属用基础名** | 金属用 `Steel`、`Plasteel`、`Gold` | ❌ `BlocksSteel`（不存在） |
| **木质用 WoodLog** | 木材用 `WoodLog` | ❌ `Wood`（不存在） |
| **织物用布料/皮革名** | 织物用 `Cloth`、`Leather_Plain` | |
| **CQF 物件需要 `<stuff>`** | 有 `stuffCategories` 的必须写，否则报 `MakeThing error: ... is madeFromStuff but stuff=null` | ❌ 缺少 `<stuff>` |

> `IsStuff = true` 的物体才能用作 `<stuff>`。岩石本体（`Marble`/`Granite`/`Sandstone` 等）是 Mineable 山体，**不是 stuff**。

> ⚠️ **如何判断一个 ThingDef 是否需要 `<stuff>`**：看它的 XML 定义里有没有 `<stuffCategories>` 节点。**有就必须写 `<stuff>`**，没有则可以不写。
> 
> 常见错误：认为固定墙或固定门不需要 material（❌ `QF_MiracleWall`、`QF_MiracleDoor` 都继承了 `<stuffCategories>`），或者根据 defName 或 label 判断。**唯一标准是 ThingDef XML 中是否存在或继承 `<stuffCategories>`**。

## 前缀约定速查

编写地图时看到不同前缀的 defName，分清来源：

| 前缀 | 来源 | 示例 | 说明 |
|------|------|------|------|
| 无前缀 | **Core** 原版 | `Wall`, `Steel`, `Concrete` | 原版核心内容，无需任何 DLC |
| `QF_` | **CQF** | `QF_MiracleWall`, `QF_MiracleDoor`, `QF_StrangeWall` | CQF 提供的特殊功能物件 |
| `QE_` | **CQF** | `QE_LootBox`, `QE_Flash`, `QE_Bookshelf` | CQF 的核心框架物件（交互、箱子、陷阱、入口出口等） |
| `CQF_` | **CQF** | `CQF_CryptosleepCasket`, `CQF_CustomDoor` | CQF 的复合物件 | 
| `US_` | **非常规生存 Mod** | `US_Map_GoldenParadise` | 其他 Mod 的自定义内容，非 CQF 通用 |

---

## 一、建筑 / 物品类

用在 `<thingDatas>` 的 `<def>` 字段中。

### 1.1 墙壁与门

表格中的“大小”来自 ThingDef 的 `<size>`；没有 `<size>` 的原版建筑默认为 `1x1`。`drawSize` 只影响贴图显示，不等于实际占格。

| 用途 | defName | 来源 | 大小 | 需要 `<stuff>` | 材质类别 | 备注 |
|------|---------|------|------|----------------|----------|------|
| 普通墙 | `Wall` | **Core** | `1x1` | 是 | Metallic/Woody/Stony | |
| 门 | `Door` | **Core** | `1x1` | 是 | Metallic/Woody/Stony | |
| 沙袋 | `Sandbags` | **Core** | `1x1` | 是 | Fabric/Leathery | 不是 Metallic/Woody/Stony |
| 路障 | `Barricade` | **Core** | `1x1` | 是 | Metallic/Woody/Stony | |
| 穴居墙 | `BurrowWall` | **Core** | `1x1` | 否 | — | 不可建造，天然虫墙 |
| 安全门 | `SecurityDoor` | **Core** | `1x1` | 否 | — | 不可建造，地图预设 |
| 远古堡垒墙 | `AncientFortifiedWall` | **Core** | `1x1` | 否 | — | 不可建造 |
| 轨道远古堡垒墙 | `OrbitalAncientFortifiedWall` | **Core** | `1x1` | 否 | — | 不可建造 |
| 远古防爆门 | `AncientBlastDoor` | **Core** | `1x1` | 否 | — | 不可建造 |
| 固定的墙 | `QF_MiracleWall` | **CQF** | `1x1` | 是 | Metallic/Woody/Stony | 无耐久、不可摧毁；CQFTool 中显示为“特殊建筑” |
| 固定的门 | `QF_MiracleDoor` | **CQF** | `1x1` | 是 | Metallic/Woody/Stony | `CustomDoor`；无法靠蛮力破坏、不可燃；CQFTool 中显示为 `CustomDoor` |
| 可交互墙 | `QF_StrangeWall` | **CQF** | `1x1` | 是 | Metallic/Woody/Stony | InteractableThing |
| 灰色墙 | `GrayWall` | **Anomaly** | `1x1` | 否 | — | |
| 灰色门 | `GrayDoor` | **Anomaly** | `1x1` | 否 | — | |
| 虚空金属墙 | `VoidmetalWall` | **Anomaly** | `1x1` | 否 | — | |

### 1.2 可放置建筑（装饰 / 实用）

| 用途 | defName | 来源 | 大小 | 需要 `<stuff>` | 材质类别 | 备注 |
|------|---------|------|------|----------------|----------|------|
| 柱子 | `Column` | **Core** | `1x1` | 是 | Metallic/Woody/Stony | |
| 床 | `Bed` | **Core** | `1x2` | 是 | Metallic/Woody/Stony | 游戏内名：单人床 |
| 睡袋 | `Bedroll` | **Core** | `1x2` | 是 | Fabric/Leathery | |
| 凳子 | `Stool` | **Core** | `1x1` | 是 | Metallic/Woody/Stony | |
| 餐椅 | `DiningChair` | **Core** | `1x1` | 是 | Metallic/Woody | 不支持 Stony |
| 桌子(1×2) | `Table1x2c` | **Core** | `1x2` | 是 | Metallic/Woody/Stony | |
| 桌子(2×2) | `Table2x2c` | **Core** | `2x2` | 是 | Metallic/Woody/Stony | |
| 桌子(2×4) | `Table2x4c` | **Core** | `2x4` | 是 | Metallic/Woody/Stony | |
| 屠宰桌 | `TableButcher` | **Core** | `3x1` | 是 | Metallic/Woody | 不支持 Stony |
| 物品架 | `Shelf` | **Core** | `2x1` | 是 | Metallic/Woody/Stony | |
| 小物品架 | `ShelfSmall` | **Core** | `1x1` | 是 | Metallic/Woody/Stony | |
| 落地灯 | `StandingLamp` | **Core** | `1x1` | 否 | — | 固定成本：Steel |
| 壁灯 | `WallLamp` | **Core** | `1x1` | 否 | — | 固定成本：Steel；墙挂附件 |
| 火把 | `TorchLamp` | **Core** | `1x1` | 否 | — | 固定成本/燃料：WoodLog |
| 篝火 | `Campfire` | **Core** | `1x1` | 否 | — | 固定成本/燃料：WoodLog |
| 坟墓 | `Grave` | **Core** | `1x2` | 否 | — | 不是 stuffable |
| 通讯台 | `CommsConsole` | **Core** | `3x2` | 否 | — | 固定成本：Steel + ComponentIndustrial |
| 轨道交易信标 | `OrbitalTradeBeacon` | **Core** | `1x1` | 否 | — | 固定成本：Steel + ComponentIndustrial |
| 棺材 | `Sarcophagus` | **Core** | `1x2` | 是 | Woody/Metallic/Stony | |
| 大型石碑 | `SteleLarge` | **Core** | `2x2` | 是 | Stony | 只支持石材砖块 |
| 宏伟石碑 | `SteleGrand` | **Core** | `3x3` | 是 | Stony | 只支持石材砖块 |
| 瓮 | `Urn` | **Core** | `1x1` | 是 | Metallic/Stony | 不支持 Woody |
| 迷你机枪塔 | `Turret_MiniTurret` | **Core** | `1x1` | 是 | Metallic | 需要 `<stuff>`，通常用 `Steel` |
| 自动迷你机枪塔 | `Turret_AutoMiniTurret` | **Core** | `1x1` | 否 | — | 远古/机械炮塔，不是 stuffable |
| 迫击炮 | `Turret_Mortar` | **Core** | `2x2` | 是 | Metallic | 需要 `<stuff>`，通常用 `Steel` |
| 自动充能爆破炮塔 | `Turret_AutoChargeBlaster` | **Core** | `2x2` | 否 | — | 远古/机械炮塔 |
| 自动地狱火炮塔 | `Turret_AutoInferno` | **Core** | `2x2` | 否 | — | 远古/机械炮塔 |
| 远古安保炮塔 | `Turret_AncientArmoredTurret` | **Core** | `1x1` | 否 | — | 不可建造 |
| 制造台 | `TableMachining` | **Core** | `3x1` | 否 | — | 原表 `MachiningTable` 不存在 |
| 燃料锻造台 | `FueledSmithy` | **Core** | `3x1` | 否 | — | 原表 `Smithy` 不存在 |
| 电力锻造台 | `ElectricSmithy` | **Core** | `3x1` | 否 | — | 原表 `Smithy` 不存在 |
| 石工台 | `TableStonecutter` | **Core** | `3x1` | 是 | Metallic/Woody | 原表 `StonecutterTable` 不存在 |
| 豪华双人床 | `RoyalBed` | **Royalty** | `2x2` | 是 | Metallic/Woody/Stony | |
| 远古灯 | `AncientLamp` | **Royalty** | `1x1` | 否 | — | 固定古代建筑 |
| 远古床 | `AncientBed` | **Royalty** | `1x2` | 否 | — | 固定古代建筑 |
| 幕帘 | `Drape` | **Ideology** | `1x1` | 是 | Woody/Soft/Stony | |
| 火盆 | `Brazier` | **Ideology** | `1x1` | 否 | — | 固定成本：Steel |
| 圣物箱 | `Reliquary` | **Ideology** | `1x1` | 是 | Metallic/Woody/Stony | |
| 讲台 | `Lectern` | **Ideology** | `1x1` | 是 | Woody/Stony | |
| 骨堆 | `Skullspike` | **Ideology** | `1x1` | 是 | Metallic/Woody/Stony | |
| 灵质舱 | `BiosculpterPod` | **Biotech** | `3x2` | 否 | — | 固定成本：Steel + ComponentIndustrial |
| 神经超充器 | `NeuralSupercharger` | **Biotech** | `1x3` | 否 | — | 固定成本：Steel + ComponentIndustrial |
| 机械孵化胶囊 | `MechCapsule` | **Biotech** | `2x3` | 否 | — | 固定建筑 |
| 机械残骸 | `ChunkMechanoidSlag` | **Biotech** | `1x1` | 否 | — | 废墟装饰/矿渣块，不是建筑 |
| 灵铁框架(大型) | `CerebrexCore` | **Anomaly** | `3x3` | 否 | — | 固定建筑 |
| 灵铁稳定器 | `CerebrexStabilizer` | **Anomaly** | `2x2` | 否 | — | 固定建筑 |
| 金属地狱裂隙 | `MetalHellFloorCracks` | **Anomaly** | `1x1` | 否 | — | 地形装饰 |
| 金属地狱标记 | `MetalHellFloorMarkings` | **Anomaly** | `1x1` | 否 | — | 地形装饰 |

### 1.3 容器 / 奖励箱类

| 用途 | defName | 来源 | 大小 | 需要 `<stuff>` | 材质类别 | 备注 |
|------|---------|------|------|----------------|----------|------|
| 远古低温休眠舱 | `AncientCryptosleepCasket` | **Core** | `1x2` | 否 | — | 含随机战利品 |
| 密封板条箱 | `SealedCrate` | **Core** | `1x1` | 否 | — | 可开启容器，不是 stuffable |
| 远古板条箱 | `AncientCrate` | **Core** | `1x1` | 否 | — | 装饰 |
| 远古大板条箱 | `AncientLargeCrate` | **Core** | `1x1` | 否 | — | 装饰 |
| 远古储物柜组 | `AncientLockerBank` | **Core** | `3x1` | 否 | — | 装饰 |
| 远古安保箱 | `AncientSecurityCrate` | **Core** | `2x2` | 否 | — | 可开启容器 |
| 远古食物容器 | `AncientHermeticCrate` | **Core** | `1x2` | 否 | — | 可开启容器 |
| 远古设备方块 | `AncientEquipmentBlocks` | **Core** | `4x2` | 否 | — | 装饰 |
| 远古机器 | `AncientMachine` | **Core** | `5x3` | 否 | — | 装饰 |
| 远古储物罐 | `AncientStorageCylinder` | **Core** | `1x1` | 否 | — | 装饰 |
| 远古终端 | `AncientTerminal` | **Core** | `1x1` | 否 | — | 可黑入终端 |
| 远古机柜 | `AncientSystemRack` | **Core** | `1x3` | 否 | — | 装饰 |
| 远古炉灶 | `AncientOven` | **Core** | `1x1` | 否 | — | 装饰 |
| 远古冰箱 | `AncientRefrigerator` | **Core** | `1x1` | 否 | — | 装饰 |
| 远古通讯台 | `AncientCommsConsole` | **Core** | `3x2` | 否 | — | 可黑入终端 |
| 远古大厨柜 | `AncientLargeContainer` | **Core** | `3x5` | 否 | — | 装饰，阻挡通行 |
| 远古小型炮艇残骸 | `AncientRustedCar` | **Core** | `2x4` | 否 | — | 装饰 |
| 远古安保终端 | `AncientEnemyTerminal` | **Core** | `1x1` | 否 | — | 可黑入终端 |
| 远古发电机 | `AncientGenerator` | **Core** | `2x2` | 否 | — | 装饰/古代建筑 |
| 飞螳卵囊 | `CocoonSpawner` | **Anomaly** | `1x1` | 否 | — | |
| 血肉心脏 | `FleshmassHeart` | **Anomaly** | `3x3` | 否 | — | |

---

## 二、掉落物 / 奖励类

### 2.1 基础资源

| 用途 | defName | 来源 | IsStuff | 材质类别 | 备注 |
|------|---------|------|---------|---------|------|
| 钢铁 | `Steel` | **Core** | ✅ | Metallic | 最常用建材 |
| 塑钢 | `Plasteel` | **Core** | ✅ | Metallic | 高级建材 |
| 黄金 | `Gold` | **Core** | ✅ | Metallic | 贵重/装饰 |
| 白银 | `Silver` | **Core** | ✅ | Metallic | 通货/装饰 |
| 铀 | `Uranium` | **Core** | ✅ | Metallic | 重型建筑 |
| 翡翠 | `Jade` | **Core** | ✅ | — | 装饰品 |
| 零部件 | `ComponentIndustrial` | **Core** | ✅ | Metallic | 工业零件 |
| 高级零部件 | `ComponentSpacer` | **Core** | ❌ | — | 零件，非 stuff |
| 布料 | `Cloth` | **Core** | ✅ | Soft | 织物 |
| 木材 | `WoodLog` | **Core** | ✅ | Woody | 基础建材 |
| 钢渣块 | `ChunkSlagSteel` | **Core** | ✅ | Metallic | 可冶炼 |
| 化合燃料 | `Chemfuel` | **Core** | ❌ | — | |
| 砂岩砖 | `BlocksSandstone` | **Core** | ✅ | Stony | 石质砖块 |
| 花岗岩砖 | `BlocksGranite` | **Core** | ✅ | Stony | 石质砖块 |
| 大理石砖 | `BlocksMarble` | **Core** | ✅ | Stony | 石质砖块 |
| 石灰岩砖 | `BlocksLimestone` | **Core** | ✅ | Stony | 石质砖块 |
| 板岩砖 | `BlocksSlate` | **Core** | ✅ | Stony | 石质砖块 |
| 垃圾袋 | `Wastepack` | **Biotech** | ✅ | — | |
| 玻璃钢 | `GravlitePanel` | **Biotech** | ✅ | — | |
| 生物铁 | `Bioferrite` | **Anomaly** | ✅ | Metallic | |
| 虚空金属块 | `ChunkVacstone` | **Anomaly** | ❌ | — | |
| 碎片 | `Shard` | **Anomaly** | ❌ | — | |
| 扭曲肉 | `Meat_Twisted` | **Anomaly** | ❌ | — | |
| 灰肉样本 | `GrayFleshSample` | **Anomaly** | ❌ | — | |
| 死亡粉尘雕像 | `GrayStatueDeadlifeDust` | **Anomaly** | ❌ | — | |
| 传送雕像 | `GrayStatueTeleporter` | **Anomaly** | ❌ | — | |
| 方尖碑碎片 | `MonolithFragment` | **Anomaly** | ❌ | — | |

### 2.2 武器

> ⚠️ 注意前缀：远程武器实际 defName 带 `Gun_` 前缀（如 `Gun_ChargeRifle`），弓带 `Bow_` 前缀（如 `Bow_Great`）。下表省略前缀以方便阅读，**写 `<thing>` 时必须补全**。
> 
> ✅ 示例：`<thing>Gun_ChargeRifle</thing>`
> 
> 近战武器已经直接写了完整 defName（`MeleeWeapon_` 前缀是 defName 的一部分，直接使用即可）。

| 用途 | 实际 defName（使用时请留意前缀） | 来源 |
|------|---------|------|
| 电荷步枪 | `ChargeRifle` → 完整：`Gun_ChargeRifle` | **Core** |
| 电荷标枪 | `ChargeLance` → 完整：`Gun_ChargeLance` | **Core** |
| 速射机枪 | `Minigun` → 完整：`Gun_Minigun` | **Core** |
| 突击步枪 | `AssaultRifle` → 完整：`Gun_AssaultRifle` | **Core** |
| 冲锋手枪 | `MachinePistol` → 完整：`Gun_MachinePistol` | **Core** |
| 重型冲锋枪 | `HeavySMG` → 完整：`Gun_HeavySMG` | **Core** |
| 泵动霰弹枪 | `PumpShotgun` → 完整：`Gun_PumpShotgun` | **Core** |
| 链式霰弹枪 | `ChainShotgun` → 完整：`Gun_ChainShotgun` | **Core** |
| 狙击步枪 | `SniperRifle` → 完整：`Gun_SniperRifle` | **Core** |
| 生存步枪 | `SurvivalRifle` → 完整：`Gun_SurvivalRifle` | **Core** |
| 左轮手枪 | `Revolver` → 完整：`Gun_Revolver` | **Core** |
| 自动手枪 | `Pistol` → 完整：`Gun_Autopistol` | **Core** |
| 栓动步枪 | `BoltActionRifle` → 完整：`Gun_BoltActionRifle` | **Core** |
| 轻机枪 | `LMG` → 完整：`Gun_LMG` | **Core** |
| 长矛 | `MeleeWeapon_Spear` | **Core** |
| 长剑 | `MeleeWeapon_LongSword` | **Core** |
| 短剑 | `MeleeWeapon_Gladius` | **Core** |
| 匕首 | `MeleeWeapon_Knife` | **Core** |
| 战锤 | `MeleeWeapon_WarHammer` | **Core** |
| 棍棒 | `MeleeWeapon_Club` | **Core** |
| 棍棒(木制) | `MeleeWeapon_ClubWood` | **Core** |
| 链锤 | `MeleeWeapon_Mace` | **Core** |
| 短矛 | `MeleeWeapon_Ikwa` | **Core** |
| 太刀 | `MeleeWeapon_Monoblades` | **Royalty** |
| 等离子剑 | `MeleeWeapon_PlasmaSword` | **Royalty** |
| 震击锤 | `MeleeWeapon_Hammer` | **Core** |
| 宙斯锤 | `MeleeWeapon_Zweihander` | **Royalty** |
| 巨弓 | `Bow_Great` | **Core** |
| 反曲弓 | `Bow_Recurve` | **Core** |
| 短弓 | `Bow_Short` | **Core** |
| 重标枪 | `Pila` | **Core** |

### 2.3 消耗品 / 特殊物品

| 用途 | defName | 来源 | 备注 |
|------|---------|------|------|
| 草药 | `MedicineHerbal` | **Core** | |
| 医药 | `MedicineIndustrial` | **Core** | |
| 闪耀世界医药 | `MedicineUltratech` | **Core** | |
| 高级芯片 | `AIPersonaCore` | **Core** | |
| 生存包 | `MealSurvivalPack` | **Core** | |
| 标准口粮 | `MealSimple` | **Core** | |
| 精制口粮 | `MealFine` | **Core** | |
| 干肉饼 | `Pemmican` | **Core** | |
| 啤酒 | `Beer` | **Core** | |
| 巧克力 | `Chocolate` | **Core** | |
| 虫胶 | `InsectJelly` | **Core** | |
| 干草 | `Hay` | **Core** | |
| 皮革(普通) | `Leather_Plain` | **Core** | |
| 夜袭者皮 | `Leather_Dread` | **Anomaly** | |
| 土豆 | `RawPotatoes` | **Core** | |
| 大米 | `RawRice` | **Core** | |
| 玉米 | `RawCorn` | **Core** | |
| 瘦肉 | `Meat_Large` | **Core** | |
| 神经训练器(射击) | `Neurotrainer_Shooting` | **Core** | 运行时生成，每技能一个 |
| 神经训练器(近战) | `Neurotrainer_Melee` | **Core** | 运行时生成，每技能一个 |
| 神经训练器(建造) | `Neurotrainer_Construction` | **Core** | 运行时生成 |
| 神经训练器(采矿) | `Neurotrainer_Mining` | **Core** | 运行时生成 |
| 神经训练器(烹饪) | `Neurotrainer_Cooking` | **Core** | 运行时生成 |
| 神经训练器(种植) | `Neurotrainer_Plants` | **Core** | 运行时生成 |
| 神经训练器(驯兽) | `Neurotrainer_Animals` | **Core** | 运行时生成 |
| 神经训练器(制作) | `Neurotrainer_Crafting` | **Core** | 运行时生成 |
| 神经训练器(艺术) | `Neurotrainer_Artistic` | **Core** | 运行时生成 |
| 神经训练器(医疗) | `Neurotrainer_Medicine` | **Core** | 运行时生成 |
| 神经训练器(社交) | `Neurotrainer_Social` | **Core** | 运行时生成 |
| 神经训练器(研究) | `Neurotrainer_Intellectual` | **Core** | 运行时生成 |
| 心灵训练器 | `Psytrainer_*` | **Royalty** | 运行时生成，每心灵能力一个 |
| 治愈者血清 | `MechSerumHealer` | **Royalty** | |

> ⚠️ `SkillNeurotrainer` 不存在！`SkillNeurotrainer` 是 `thingSetMakerTags` 里的标记，不是 ThingDef。正确的神经训练器 defName 格式是 `Neurotrainer_{SkillName}`（如 `Neurotrainer_Shooting`、`Neurotrainer_Melee`）。这些是运行时动态生成的，每个技能对应一个。

| 灵能放大器 | `PsychicAmplifier` | **Royalty** | |
| 灵能安抚器 | `PsychicEmanator` | **Royalty** | |
| 机械链接 | `Mechlink` | **Biotech** | |
| 基因包 | `Genepack` | **Biotech** | |
| 异种胚芽 | `Xenogerm` | **Biotech** | |
| 血原包 | `HemogenPack` | **Biotech** | |
| 婴儿食物 | `BabyFood` | **Biotech** | |

---

## 三、地形速查 (TerrainDef)

用在 `<terrainsRect>` 中：

```xml
<terrainsRect>
  <li>
    <key>Concrete</key>
    <value>
      <li>(2,2,44,44)</li>
    </value>
  </li>
</terrainsRect>
```

### 常用地形

| defName | 来源 | 视觉效果 | 适合场景 |
|---------|------|---------|---------|
| `Concrete` | **Core** | 灰色工业地面 | 室内、基地、走廊 |
| `AncientConcrete` | **Core** | 破损灰地面 | 废墟、远古遗迹 |
| `MetalTile` | **Core** | 金属网格地面（steel tile） | 科技设施、飞船 |
| `FineTileSandstone` | **Core** | 黄色精制石砖 | 普通室内 |
| `FineTileGranite` | **Core** | 灰色精制石砖 | |
| `FineTileMarble` | **Core** | 白色大理石 | 神殿、图书馆、高级房间 |
| `FineTileLimestone` | **Core** | 米色精制石砖 | |
| `FineTileSlate` | **Core** | 深色精制石砖 | |
| `TileSandstone` | **Core** | 黄色石砖 | 普通室内 |
| `TileGranite` | **Core** | 灰色石砖 | |
| `TileMarble` | **Core** | 白色石砖 | |
| `TileLimestone` | **Core** | 米色石砖 | |
| `TileSlate` | **Core** | 深色石砖 | |
| `FlagstoneSandstone` | **Core** | 粗糙石铺地 | 室外庭院、通道 |
| `FlagstoneGranite` | **Core** | 深色石铺地 | 室外、要塞 |
| `FlagstoneMarble` | **Core** | 白色石铺地 | 庭院 |
| `FlagstoneLimestone` | **Core** | 米色石铺地 | |
| `FlagstoneSlate` | **Core** | 深色石铺地 | |
| `WoodPlankFloor` | **Core** | 木地板 | 木屋 |
| `PavedTile` | **Core** | 方形石砖 | 室外广场 |
| `AncientTile` | **Core** | 远古瓷砖 | 废墟 |
| `Gravel` | **Core** | 碎石地面 | 道路、矿区 |
| `PackedDirt` | **Core** | 压实泥土 | 路径、营地 |
| `BrokenAsphalt` | **Core** | 碎裂柏油 | 废墟城市 |
| `Soil` | **Core** | 泥土地 | 农田、户外 |
| `SoilRich` | **Core** | 深色土壤 | 肥沃农田 |
| `Sand` | **Core** | 沙地 | 沙漠 |
| `Mud` | **Core** | 泥潭 | 沼泽 |
| `Marsh` | **Core** | 沼泽 | 沼泽 |
| `WaterShallow` | **Core** | 浅水 | 河流 |
| `WaterDeep` | **Core** | 深水 | 河流 |
| `Ice` | **Core** | 冰面 | 冰盖 |
| `Bridge` | **Core** | 木桥 | 跨水 |
| `HeavyBridge` | **Biotech** | 金属桥 | 跨水 |
| `GraySurface` | **Anomaly** | 灰色地表 | 异象区域 |
| `Voidmetal` | **Anomaly** | 虚空金属地面 | 异象建筑区域 |
| `Flesh` | **Anomaly** | 血肉地面 | 血肉迷宫 |
| `CooledLava` | **Anomaly** | 冷却岩浆 | 地狱场景 |
| `Substructure` | **Anomaly** | 下层结构 | 机械建筑 |

### ⚠️ CellRect 格式说明

`<terrainsRect>`、`<roofRects>`、`ThingData.<allRect>`、`ZoneCore` 等场景中使用的 CellRect，统一使用 RimWorld 原生格式：**`(minX,minZ,maxX,maxZ)`**。

```xml
<!-- ✅ 正确：(minX, minZ, maxX, maxZ) — 左上角坐标 + 右下角坐标 -->
<li>(2,3,10,8)</li>        <!-- 表示从 (2,3) 到 (10,8) 的矩形，宽 9 格、高 6 格 -->

<!-- ❌ 常见错误：写成 (x, z, width, height) -->
<!-- ❌ 常见错误：写成 (x, z, w, h) -->
```

| 概念 | 说明 | 示例 `(2,3,10,8)` |
|------|------|-------------------|
| `minX` | 左上角 x 坐标 | `2` |
| `minZ` | 左上角 z 坐标 | `3` |
| `maxX` | 右下角 x 坐标 | `10` |
| `maxZ` | 右下角 z 坐标 | `8` |
| Width | `maxX - minX + 1` | `9` |
| Height | `maxZ - minZ + 1` | `6` |

**一个 CellRect 覆盖的格子数** = `(maxX - minX + 1) * (maxZ - minZ + 1)`。

---

## 四、屋顶速查 (RoofDef)

用在 `<roofRects>` 中，以 `RoofDef.defName` 为 key：

```xml
<roofRects>
  <li>
    <key>RoofRockThick</key>
    <value>
      <li>(3,3,75,75)</li>
    </value>
  </li>
</roofRects>
```

### 所有 RoofDef

| defName | 来源 | 标签 | 说明 |
|---------|------|------|------|
| `RoofConstructed` | **Core** | constructed roof | 人造屋顶，可建造，可坍塌 |
| `RoofRockThin` | **Core** | rock roof (thin) | 薄岩顶，天然，`isThickRoof=false`，可被爆炸/武器打穿 |
| `RoofRockThick` | **Core** | overhead mountain | 厚岩顶（山体），天然，`isThickRoof=true`，不可被武器打穿 |
| `VoidmetalRoof` | **Anomaly** | void metal ceiling | 虚空金属天花板，天然，`isThickRoof=true`，`canCollapse=false` |

> ⚠️ **常见陷阱**：`RoofRock` 不存在！正确名称为 `RoofRockThick`（厚岩顶/山体屋顶）。编写 `<roofRects>` 时注意不要拼错。

---

## 五、CQF 自定义物件

所有物件均为 **CQF** 提供。表格中的“大小”来自 ThingDef 的 `<size>`；没有 `<size>` 的 ThingDef 默认为 `1x1`。`graphicData.drawSize` 只影响贴图显示大小，**不等于实际占格大小**。

> ⚠️ 生成地图时不要把所有 CQF 建筑都当作 `1x1`。例如 `QE_Cabinet` / `QE_Bookshelf` 是 `2x1`，`QE_APileOfCrate` 是 `4x1`，`CQF_CryptosleepCasket` / `QE_Sarcophagus` 是 `1x2`，`QE_LargeCage` 是 `2x2`。
>
> ⚠️ “需要 `<stuff>`”的唯一判断标准仍然是 ThingDef 或其父类是否有 `<stuffCategories>`。需要材质时，`<stuff>` 必须写具体 stuff defName，如 `Steel`、`WoodLog`、`BlocksMarble`、`Cloth`、`Leather_Plain`。

### 5.1 编辑器 / 工具类 (用在 `<customThings>`)

| defName | thingClass | 大小 | 需要 `<stuff>` | 材质类别 | 用途 |
|---------|-----------|------|----------------|----------|------|
| `QE_InteractableThing` | `InteractableThing` | `1x1` | 否 | — | 通用交互点：技能检测、条件分支、操作菜单 |
| `QE_Flash` | `InteractableThing` | `1x1` | 否 | — | 隐形触发器：透明不可见，带闪烁粒子 |
| `QE_LootBox` | `LootBox` | `1x1` | 否 | — | 通用战利品箱：按 LootData 概率抽奖 |
| `QE_CustomTrap` | `CustomTrap` | `1x1` | 否 | — | 通用陷阱：StepOn/Signal/Tick/Damage 触发 |
| `QE_TriggerTrap` | `CustomTrap_Dev` | `1x1` | 否 | — | 开发者陷阱，仅调试用 |
| `QE_CaptureNet` | `CustomTrap_Capture` | `1x1` | 是 | Fabric/Leathery | 捕获网：踩中后捕获 Pawn |
| `QE_StoneBurrow` | `CustomTrap` | `1x1` | 否 | — | 石头掩体陷阱 |
| `QE_PressurePlate` | `CustomTrap` | `1x1` | 是 | Metallic/Woody/Stony | 压力板：StepOn 触发 |
| `QE_StonePressurePlate` | `CustomTrap` | `1x1` | 是 | Stony | 石质压力板：StepOn 触发 |
| `QE_Spawner_Editor` | `Spawner` | `1x1` | 否 | — | Pawn 生成点：地图生成时刷出 Pawn |
| `QE_GenerationActionWorker` | `GenerationActionWorker` | `1x1` | 否 | — | 地图生成后执行动作链 |
| `QE_ZoneCore` | `ZoneCore` | `1x1` | 否 | — | 区域核心：ZoneCore 拼装系统 |
| `QE_CustomMapEntrance` | `CustomMapEntrance` | `1x1` | 否 | — | 子地图入口 |
| `QE_CustomMapEntrance_Chance` | `CustomMapEntrance_Chance` | `1x1` | 否 | — | 随机子地图入口 |
| `QE_CustomMapExit` | `CustomMapExit` | `1x1` | 否 | — | 子地图出口 |

### 5.2 预制建筑 (用在 `<customThings>`)

| defName | thingClass | 大小 | 需要 `<stuff>` | 材质类别 | 用途 |
|---------|-----------|------|----------------|----------|------|
| `QE_CellarDoor` | `CustomMapEntrance_Chance` | `1x1` | 是 | Metallic/Woody/Stony | 地下室门（入口，带开关动画） |
| `QE_Labber` | `CustomMapExit` | `1x1` | 是 | Metallic/Woody/Stony | 梯子（出口，通向上层） |
| `QE_LabberEntrance` | `CustomMapEntrance_Chance` | `1x1` | 是 | Metallic/Woody/Stony | 梯子（入口，通向地下） |
| `QE_LadderDown` | `CustomMapExit` | `1x1` | 是 | Metallic/Woody/Stony | 楼梯向下（出口） |
| `QE_Exit` | `CustomMapExit` | `1x1` | 否 | — | 通用出口（不可见图标） |
| `QE_SubMap_Burrow` | `CustomMapEntrance` | `1x1` | 否 | — | 洞穴入口 |
| `QE_SubMap_StoneBurrow` | `CustomMapEntrance` | `1x1` | 否 | — | 石洞入口 |
| `QE_Bookshelf` | `InteractableThing` | `2x1` | 是 | Metallic/Woody/Stony | 书架：线索/信息交互 |
| `QE_EmplyBookshelf` | `Building` | `2x1` | 是 | Metallic/Woody/Stony | 空书架：纯装饰 |
| `QE_Sign` | `InteractableThing` | `1x1` | 是 | Metallic/Woody/Stony | 标识牌：可设自定义文本 |
| `QE_DamagedSign` | `InteractableThing` | `1x1` | 是 | Metallic/Woody/Stony | 损坏的标识牌 |
| `QE_Tunnel` | `InteractableThing` | `1x1` | 否 | — | 隧道交互点 |
| `QE_WorkSpot` | `InteractableThing` | `1x1` | 否 | — | 工作点交互 |
| `QE_Cabinet` | `LootBox` | `2x1` | 是 | Metallic/Woody/Stony | 远古柜子（战利品箱） |
| `QE_TreasureChest` | `LootBox` | `1x1` | 是 | Metallic/Woody/Stony | 宝箱（带开启动画） |
| `QE_Crate` | `LootBox` | `1x1` | 是 | Metallic/Woody/Stony | 板条箱（战利品箱） |
| `QE_SomeCrate` | `LootBox` | `2x1` | 是 | Metallic/Woody/Stony | 一堆板条箱 |
| `QE_APileOfCrate` | `LootBox` | `4x1` | 是 | Metallic/Woody/Stony | 一大堆板条箱 |
| `QE_LootBox_HumanCorpses` | `LootBox` | `1x1` | 否 | — | 尸体堆（战利品箱） |
| `QE_LootBox_Corpses` | `LootBox` | `1x1` | 否 | — | 多具尸体（带开启动画） |
| `CQF_CryptosleepCasket` | `LootBox` | `1x2` | 否 | — | 远古低温舱（战利品箱） |
| `QE_Sarcophagus` | `LootBox` | `1x2` | 是 | Woody/Metallic/Stony | 棺材（战利品箱） |
| `QE_Cage` | `CustomContainer` | `1x1` | 是 | Metallic/Woody/Stony | 笼子（捕获容器） |
| `QE_LargeCage` | `CustomContainer` | `2x2` | 是 | Metallic/Woody/Stony | 大笼子（捕获大型 Pawn） |
| `QF_MiracleWall` | `Building` | `1x1` | 是 | Metallic/Woody/Stony | 固定的墙；无耐久、不可摧毁 |
| `QF_MiracleDoor` | `CustomDoor` | `1x1` | 是 | Metallic/Woody/Stony | 固定的门；无法靠蛮力破坏、不可燃 |
| `QF_StrangeWall` | `InteractableThing` | `1x1` | 是 | Metallic/Woody/Stony | 可交互墙 |
| `CQF_CustomDoor` | `CustomDoor` | `1x1` | 是 | Metallic/Woody/Stony | 自定义门（信号/条件开启） |

### 5.3 CQF 陷阱类

| defName | thingClass | 大小 | 需要 `<stuff>` | 材质类别 | 用途 |
|---------|-----------|------|----------------|----------|------|
| `QE_CaptureNet` | `CustomTrap_Capture` | `1x1` | 是 | Fabric/Leathery | 捕获网：踩中捕获 Pawn |
| `QE_StoneBurrow` | `CustomTrap` | `1x1` | 否 | — | 石头掩体陷阱 |
| `QE_PressurePlate` | `CustomTrap` | `1x1` | 是 | Metallic/Woody/Stony | 压力板：StepOn 触发 |
| `QE_StonePressurePlate` | `CustomTrap` | `1x1` | 是 | Stony | 石质压力板 |
| `QE_CustomTrap` | `CustomTrap` | `1x1` | 否 | — | 通用陷阱 |

---

## 六、物品规格写法 (CQFThingDefCount)

用在 `<things>` 列表中，给 `LootData` 指定掉落物。

```xml
<!-- 固定数量 -->
<li Class="QuestEditor_Library.CQFThingDefCount">
  <thing>Steel</thing>
  <count>50</count>
</li>

<!-- 范围数量（用波浪号 ~） -->
<li Class="QuestEditor_Library.CQFThingDefCount">
  <thing>ComponentIndustrial</thing>
  <count>5~15</count>
</li>

<!-- 带材质 -->
<li Class="QuestEditor_Library.CQFThingDefCount">
  <thing>BlocksMarble</thing>
  <count>10~20</count>
</li>
```

> ⚠️ `count` 的 IntRange 格式必须用 `min~max`（波浪号），不要用 `(min,max)` 括号格式，否则会报 IntRange 解析错误。

---

## 七、ThingCategoryDef 速查 (CQFThingCategoryCount)

用于 `LootData` 中的 `<categorys>` 列表，按物品分类掉落，不指定具体物品而是指定武器/服装等大类。

```xml
<li Class="QuestEditor_Library.CQFThingCategoryCount">
  <category>WeaponsRanged</category>
  <count>1~2</count>
</li>
```

| defName | 标签 | 说明 | 来源 |
|---------|------|------|------|
| `Weapons` | weapons | 所有武器（含近战和远程） | **Core** |
| `WeaponsRanged` | ranged weapons | 远程武器（枪械、弓等） | **Core** |
| `WeaponsMelee` | melee weapons | 近战武器（剑、锤、矛等） | **Core** |
| `WeaponsUnique` | unique weapons | 独特武器 | **Core** |
| `WeaponsMeleeBladelink` | persona weapons | 超凡武器（心灵武器） | **Royalty** |
| `Apparel` | apparel | 所有服装/装备 | **Core** |
| `Armor` | armor | 护甲类 | **Core** |
| `Medicine` | medicine | 医疗用品 | **Core** |
| `Drugs` | drugs | 药物/毒品类 | **Core** |
| `FoodMeals` | meals | 餐食类 | **Core** |
| `RawFood` | raw food | 生食材 | **Core** |
| `ResourcesRaw` | raw resources | 基础资源（钢铁、木材等） | **Core** |
| `Manufactured` | manufactured | 工业制成品 | **Core** |
| `BodyParts` | body parts | 仿生体部件 | **Core** |
| `Corpses` | corpses | 尸体 | **Core** |
| `Animals` | animals | 动物 | **Core** |
| `Buildings` | buildings | 建筑 | **Core** |
| `BuildingsArt` | art | 艺术品建筑 | **Core** |
| `BuildingsSecurity` | security | 安保建筑 | **Core** |
| `BuildingsMisc` | buildings | 杂项建筑 | **Core** |
| `Builders` | builders | 建造工具 | **Core** |
| `Misc` | misc | 杂项 | **Core** |

> ⚠️ `WeaponsBasic` 不存在！正确的远程武器分类是 `WeaponsRanged`，近战是 `WeaponsMelee`，所有武器是 `Weapons`。

---

## 八、技能名速查 (DialogCondition_Skill)

所有技能均为 **Core**（原版固有）。

| 写作名(skill 字段) | 对应 RimWorld 技能 | 用途 |
|-------------------|-------------------|------|
| `Construction` | 建造 | 陷阱拆除、建筑修复 |
| `Plants` | 种植 | 植物培育、农业知识 |
| `Intellectual` | 研究 | 终端破译、科技分析 |
| `Mining` | 采矿 | 挖掘、爆破 |
| `Shooting` | 射击 | 打靶、远程战斗 |
| `Melee` | 近战 | 格斗、武器掌握 |
| `Social` | 社交 | 演讲、谈判、说服 |
| `Animals` | 驯兽 | 动物驯服、畜牧 |
| `Cooking` | 烹饪 | 食物制作、生存烹饪 |
| `Medicine` | 医疗 | 医疗救治、制药 |
| `Artistic` | 艺术 | 艺术鉴赏、雕刻 |
| `Crafting` | 制作 | 锻造、制造、手工 |

---

## 九、地图编写骨架模板

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <QuestEditor_Library.CustomMapDataDef>
    <defName>SK_XXXXX</defName>
    <label>label_key</label>
    <description>desc_key</description>
    <size>(47, 1, 47)</size>
    <fogged>true</fogged>

    <!-- 地板（Core） -->
    <terrainsRect>
      <li>
        <key>Concrete</key>
        <value><li>(2,2,44,44)</li></value>
      </li>
    </terrainsRect>

    <!-- 建筑（Core） -->
    <thingDatas>
      <li>
        <def>Wall</def>
        <stuff>Steel</stuff>
        <allRect>
          <li>(2,2,2,44)</li>
          <li>(44,2,44,44)</li>
          <li>(2,44,44,44)</li>
          <li>(2,2,44,2)</li>
        </allRect>
      </li>
    </thingDatas>

    <!-- 自定义物件（CQF） -->
    <customThings>
      <li Class="QuestEditor_Library.CustomThingData_CustomMapEntrance">
        <def>QE_SubMap_Burrow</def>
        <position>(23,0,3)</position>
      </li>
      <li Class="QuestEditor_Library.CustomThingData_CustomMapExit">
        <def>QE_Exit</def>
        <position>(23,0,42)</position>
      </li>
    </customThings>
  </QuestEditor_Library.CustomMapDataDef>
</Defs>
```

---

## 九、快速校验清单

编写地图后逐项检查：

- [ ] `<terrainsRect>` 的 key — 确认 `TerrainDefOf` 中存在，标记了正确来源
- [ ] `<thingDatas>` 的 `<def>` — 确认 `ThingDefOf` 中存在，切勿拼接 defName
- [ ] `<thingDatas>` 和 `<customThings>` 的 `<stuff>` — 石质用砖块名（`BlocksMarble`），金属用基础名（`Steel`），木质用 `WoodLog`。有 `stuffCategories` 的 CQF 物件必须写 `<stuff>`
- [ ] `<customThings>` 的 `<def>` — 确认在 CQF XML 中存在
- [ ] `loot` 的 `<thing>` — 确认是物品而非建筑，远程武器记得加 `Gun_` 前缀
- [ ] `skill` 字段 — 使用 SkillDefOf 中的正确名
- [ ] 全篇无 `SteelWall` / `TileFine` / `ConcreteTile` / 用岩石本体当 stuff 等错误
