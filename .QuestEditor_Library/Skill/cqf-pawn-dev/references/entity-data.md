# CQF 实体数据系统

## 目录

- [源码入口](#源码入口)
- [数据模型](#数据模型)
- [加载与保存](#加载与保存)
- [生成生命周期](#生成生命周期)
- [PawnMod 模块](#pawnmod-模块)
- [实体编辑器与预览](#实体编辑器与预览)
- [PawnSpawnData](#pawnspawndata)
- [Lord 绑定](#lord-绑定)
- [扩展流程](#扩展流程)
- [常见陷阱](#常见陷阱)

## 源码入口

核心文件：

- `PawnEdit/ComplexPawnDef.cs`
- `PawnEdit/QuestEditor_PawnDataEditor.cs`
- `PawnEdit/PawnSpawnData_ComplexPawn.cs`
- `PawnEdit/PawnMod/PawnModDef.cs`
- `PawnEdit/PawnMod/PawnModWorker.cs`
- `PawnEdit/PawnMod/PawnEditData.cs`
- `PawnEdit/PawnMod/PawnModWorker_*.cs`
- `PawnData/PawnSpawnData.cs`
- `PawnData/PawnSpawnData_Faction.cs`
- `PawnData/PawnSpawnData_Group.cs`
- `PawnData/PawnSpawnData_Random.cs`
- `LordData.cs`
- `CQFQuestDefBootstrap.cs`

Def 注册：

- `1.6/Defs/QuestEditor_Library.PawnModDef/PawnMods.xml`

## 数据模型

### ComplexPawnDef

`ComplexPawnDef` 是可复用 NPC 模板，核心字段只有：

- `defName`
- `label`
- `List<PawnModData> modDatas`

派生属性：

- `Unique` 来自 `PawnModData_Basic.unique`。
- `KindDef` 来自 `PawnModData_Basic.kindDef`。

实体本身的数据全部进入模块；生成次数、概率、地图位置和 Lord 名称属于 `PawnSpawnData`。

推荐 XML：

```xml
<QuestEditor_Library.ComplexPawnDef>
  <defName>CQF_TestNpc</defName>
  <label>test NPC</label>
  <modDatas>
    <li Class="QuestEditor_Library.PawnModData_Basic">
      <kindDef>Colonist</kindDef>
    </li>
    <li Class="QuestEditor_Library.PawnModData_NameAndBody">
      <firstName>Alex</firstName>
      <bioAge>30</bioAge>
      <chrAge>30</chrAge>
      <gender>Male</gender>
    </li>
  </modDatas>
</QuestEditor_Library.ComplexPawnDef>
```

`PawnModData.ModDef` 是代码属性，通过 `NamedModDef` 解析，不保存 `<modDef>`。

## 加载与保存

实体编辑器保存到：

```text
Quests/Pawn/<defName>.xml
```

`CQFQuestDefBootstrap` 启动时扫描 `Quests/Pawn`，读取 `QuestEditor_Library.ComplexPawnDef` 并加入 `DefDatabase<ComplexPawnDef>`。

加载流程：

1. 克隆 XML 节点。
2. 只保留 `defName`、`label`、`modDatas`。
3. 通过 `DirectXmlToObject` 读取新结构。
4. 仅当原 XML 没有 `modDatas` 时，依次调用 Worker 的 `LoadData` 迁移旧平铺字段。
5. 解析交叉引用并执行 `ConfigErrors`。

因此：

- 新 XML 只写 `modDatas`。
- 一旦存在 `modDatas`，旧平铺字段不会再合并进入模块。
- 旧格式兼容只放在 `LoadData`，不要继续生成旧格式。

热加载会从 DefDatabase 移除同名 Def 再加入当前对象。已经持有旧 Def 引用的运行时对象不会自动全部换成新引用；验证时应重新生成相关实体或重新绑定职责。

## 生成生命周期

### ComplexPawnDef.GetPawn

当 `Unique == true` 且 `GameComponent_Editor.pawns` 已缓存同名 pawn 时，直接返回同一个 Pawn；否则调用 `Spawn()`。

`Unique` 表示复用同一个 Pawn 实例，不是“每次复制相同模板”。当前缓存读取没有自动排除死亡或销毁对象，使用唯一实体时要检查完整生命周期。

### ComplexPawnDef.CreatePawn

顺序：

1. `KindDef` 为空时记录错误并返回 null。
2. 使用 `KindDef` 和基础模块 faction 创建 `PawnGenerationRequest`。
3. 按 PawnModDef.order 遍历可用模块，调用 `ModifyGenerationRequest`。
4. `PawnGenerator.GeneratePawn(request)`。
5. 按相同顺序调用 `ApplyToPawn(pawn, false)`。
6. 唯一实体按 `defName` 缓存。

预览使用同一流程，但调用 `CreatePawn(false)`，不写唯一实体缓存。

### NotifyPawnSpawned

`NotifyPawnSpawned` 在 pawn 实际放置到地图后遍历模块并调用 `OnPawnSpawned`。

放置后处理：

- 对话注册。
- Pawn action trigger 注册。
- DutyMap 与复杂 Lord 绑定。
- 任何依赖 `pawn.Map`、`pawn.Spawned` 或 Quest 的状态。

不要把这些操作只放在 `ApplyToPawn`。预览也会调用 `ApplyToPawn`，并且普通生成 pawn 在实际放置前就会经过该方法。

## PawnMod 模块

### 三件套

每个模块由以下结构组成：

- `PawnModDef`：注册、显示名、描述、order、workerClass。
- `PawnModWorker_X`：编辑和应用逻辑。
- `PawnModData_X`：实体持久化数据。

`ComplexPawnDef.AvailableMods()` 会：

1. 遍历全部 PawnModDef。
2. 调用 `Worker.CanAddFor(this)`。
3. 按 `order` 排序。

`DataFor<T>()` 和 `DataFor(PawnModDef)` 在缺少数据时会创建并加入 `modDatas`。Worker 不应假设读取数据是纯操作。

### Worker 生命周期

| 方法 | 使用阶段 | 适合内容 |
| --- | --- | --- |
| `CanAddFor` | 模块筛选 | race、Humanlike、DLC 可用性 |
| `CreateData` | 创建模块数据 | 返回对应 PawnModData |
| `Draw` | 实体编辑器 | 编辑模块字段 |
| `ModifyGenerationRequest` | PawnGenerator 前 | 性别、年龄、xenotype、名字约束 |
| `ApplyToPawn` | 预览和生成 | 外观、装备、技能、trait、hediff、ability |
| `LoadData` | 旧 XML 迁移 | 从旧平铺节点读取模块字段 |
| `GetPreviewApplyKeyParts` | 预览刷新判断 | 未直接体现在 SaveToXElement 的预览依赖 |
| `OnPawnSpawned` | 放置到地图后 | 对话、触发器、DutyMap、Quest 绑定 |

`PawnModDef.Worker` 通过 `workerClass` 创建一次并缓存。不要把某个实体的正式状态存进 Worker 字段。

### 当前模块

| Def | Data | 主要职责 |
| --- | --- | --- |
| `CQF_PawnMod_Basic` | `PawnModData_Basic` | kind、faction、unique |
| `CQF_PawnMod_NameAndBody` | `PawnModData_NameAndBody` | 名字、name maker、年龄、性别 |
| `CQF_PawnMod_Appearance` | `PawnModData_Appearance` | hair、head、body type、颜色 |
| `CQF_PawnMod_Genes` | `PawnModData_Genes` | xenotype、自定义基因 |
| `CQF_PawnMod_Backstory` | `PawnModData_Backstory` | childhood、adulthood |
| `CQF_PawnMod_Traits` | `PawnModData_Traits` | trait、degree、chance |
| `CQF_PawnMod_Skills` | `PawnModData_Skills` | skill level、passion |
| `CQF_PawnMod_Abilities` | `PawnModData_Abilities` | AbilityDef 列表 |
| `CQF_PawnMod_Apparel` | `PawnModData_Apparel` | 服装和 stuff |
| `CQF_PawnMod_Weapon` | `PawnModData_Weapon` | 武器和 stuff |
| `CQF_PawnMod_Hediff` | `PawnModData_Hediff` | hediff、严重度、具体身体部位 |
| `CQF_PawnMod_Dialog` | `PawnModData_Dialog` | 生成后对话绑定 |
| `CQF_PawnMod_ActionTrigger` | `PawnModData_ActionTrigger` | 生成后 pawn 事件动作 |
| `CQF_PawnMod_DutyMap` | `PawnModData_DutyMap` | 生成后职责图和起始节点 |

### 模块数据约束

- `PawnModData_NameAndBody.gender` 默认 `Male`，生物与实际年龄默认 14。
- `PawnModData_Appearance.skinColor == null` 表示保留基因或原版肤色。
- Hediff 必须保存足够信息定位重复身体部位：`part`、`partLabel`、`partIndex`。
- 服装和武器使用 `ThingData`，MadeFromStuff 对象同时保存 stuff。
- DutyMap 模块只保存 `dutyMap` 和 `dutyMapStartNodeId`，不保存 Lord 名称。

## 实体编辑器与预览

`QuestEditor_PawnDataEditor` 使用三栏结构：左侧预览、中间当前模块、右侧模块列表。

预览机制：

- `GetPreviewKey()` 当前只返回 `KindDef.defName`。
- 只有 PawnKindDef 变化才销毁并重建预览 Pawn。
- 其他模块变化通过序列化 `modDatas` 和 `GetPreviewApplyKeyParts` 生成 apply key。
- apply key 变化时对现有预览调用 `ApplyModsToPawn(..., true)`，随后刷新 graphics 和 `PortraitsCache`。

新增模块时：

- 能直接修改现有 Pawn 的状态放进 `ApplyToPawn`。
- 如果某字段必须重新生成 Pawn 才生效，需要谨慎扩展 preview key 机制。
- 运行时注册必须检查 `preview` 或改放 `OnPawnSpawned`。
- `KindDef == null` 时编辑器只显示基础模块。

## PawnSpawnData

### 基类职责

`PawnSpawnData` 保存地图生成上下文：

- `dataName`
- `generationChance`
- `count`
- `spawnType`、`timeToSpawn`
- `faction`
- `kind`、`extraKinds`
- `enableLord`、`lordDataName`、`duty`、`routeName`、`rotation`
- `way`
- `dialogManager`
- `hediffs`
- `actions`
- inventory thing/category 数据

普通生成流程：

1. 检查概率和位置。
2. 解析 faction。
3. 必要时寻找或创建 `LordJob_Custom`。
4. 按 kind 和 count 生成 Pawn。
5. 调用 `ActionAfterGeneration`。
6. 加入 Lord 并写入简单 Duty 数据。
7. 如果 Lord 是 `LordJob_ComplexCustom`，应用 Lord 默认 DutyMap。
8. 通过 `ArrivingWay.SpawnPnaw` 放置 Pawn。
9. 按 dataName 写入 Quest group。

`ActionAfterGeneration` 在放置前执行 action、hediff、dialog、questTags 和 inventory 逻辑。

### 派生类型

- `PawnSpawnData_Faction`：按 faction group maker 和 points 生成。
- `PawnSpawnData_Group`：组合多个生成数据。
- `PawnSpawnData_Random`：从候选生成数据中随机选择。
- `PawnSpawnData_ComplexPawn`：通过 `ComplexPawnDef` 生成自定义 NPC。

### PawnSpawnData_ComplexPawn

推荐 XML：

```xml
<li Class="QuestEditor_Library.PawnSpawnData_ComplexPawn">
  <dataName>GuardCaptain</dataName>
  <count>1</count>
  <spawnType>MapGeneration</spawnType>
  <pawnDef>CQF_TestNpc</pawnDef>
  <lordDataName>StationGuards</lordDataName>
</li>
```

当前行为：

1. 检查 `pawnDef`、概率、map 和位置。
2. 当 `setLord == true`、外部未传 Lord 且 `lordDataName` 非空时，从 `MapComponent_CustomMapData` 查找 Lord。
3. 按继承的 `count` 调用 `pawnDef.GetPawn()`。
4. 已经 Spawned 的唯一 pawn只写入返回结果，不重复放置，也不再次调用 `NotifyPawnSpawned`。
5. 新 pawn 调用 `ActionAfterGeneration`，加入已解析 Lord。
6. 统一通过 `SpawnPnaw` 放置。
7. 对新放置 pawn 调用 `pawnDef.NotifyPawnSpawned`。
8. 按 dataName 写入 Quest group。

重要差异：

- 它不依赖 `enableLord` 决定是否解析 `lordDataName`。
- 它继承并序列化基类的 count、chance、spawnType 等字段，但当前 `Draw` 只绘制名称、ComplexPawnDef 和 Lord 名称。
- `CanSaveToMap` 当前只检查 `pawnDef != null`，不会额外验证 count。
- 外部传入的 Lord 优先于 `lordDataName`。

## Lord 绑定

实体和复杂职责有三种常见绑定入口：

1. `PawnSpawnData_ComplexPawn.lordDataName`
   把新实体加入地图数据中已有的 Lord。
2. `LordData.LordJobData`
   当 LordJob 类型为 `LordJob_ComplexCustom` 时，保存 `dutyMap` 和 `dutyMapStartNodeId` 作为整个 Lord 的默认配置。
3. `PawnModData_DutyMap`
   某个 ComplexPawnDef 自带职责图，在 `OnPawnSpawned` 中为该 pawn 绑定。

普通 `PawnSpawnData.MakePawns` 把 pawn 加入 `LordJob_ComplexCustom` 后，会显式调用 `ApplyDefaultDutyMap`。`PawnSpawnData_ComplexPawn.Spawn` 当前只调用 `lord.AddPawn`，不会主动应用复杂 Lord 的默认 DutyMap。

因此：

- 普通 PawnSpawnData 可以继承复杂 Lord 的默认图。
- ComplexPawnDef 若要稳定获得职责图，应配置 `PawnModData_DutyMap`，或在生成后显式调用设置职责图的 action/API。
- 仅给 `PawnSpawnData_ComplexPawn` 设置 `lordDataName`，不能假设它会继承 Lord 默认图。
- 如果其他调用方先应用 Lord 默认图，PawnMod 的 DutyMap 在 `NotifyPawnSpawned` 阶段仍可能覆盖它；设计多入口配置时必须明确最终来源。

`GameComponent_ComplexDuty.SetDutyMap` 会调用 `LordJob_ComplexCustom.EnsureForPawn`。如果 pawn 已属于其他 LordJob，该 Lord 会被直接切换到 `LordJob_ComplexCustom`，从而影响 Lord 内其他 pawn。不要在不确认群组影响时给已有普通 Lord 中的单个 pawn设置 DutyMap。

## 扩展流程

### 新增实体模块

1. 在 `PawnEdit/PawnMod` 下新增独立 Worker 文件。
2. 在数据文件中新增 `PawnModData_X`，或按代码组织要求拆成独立文件。
3. 注册 PawnModDef。
4. 实现旧数据迁移和新 XML 保存。
5. 明确生成前、应用和生成后三个阶段。
6. Keyed UI 补英文和简体中文；Def 的英文名称与描述直接写在 Def，简体中文使用 DefInjected。
7. 验证人类、动物、缺失 tracker 和预览场景。

### 新增 PawnSpawnData 派生类

1. 独立文件继承 `PawnSpawnData`。
2. 重写 `Draw`、`CanSaveToMap`、`Spawn`、`SaveToXElement` 和必要的 `ExposeData`。
3. 保存时写 `Class`；Def 引用写 `defName`。
4. 明确是否复用基类 `enableLord` 语义。
5. 不要静默返回无法诊断的失败；关键配置缺失时记录上下文。

## 常见陷阱

- 把 Lord 或生成数量放入 `ComplexPawnDef`。
- 在 Worker 字段中保存实体正式数据。
- `ApplyToPawn` 注册地图组件状态，导致预览污染运行时。
- 认为唯一实体会生成多个相同副本。
- 认为 `ActionAfterGeneration` 时 pawn 已经 Spawned。
- 认为 `PawnSpawnData_ComplexPawn` 加入复杂 Lord 后会自动应用 Lord 默认 DutyMap。
- 只保存 `BodyPartDef`，无法区分左右或重复身体部位。
- 新格式同时写平铺字段和 `modDatas`，造成迁移语义不清。
- 给一个已有普通 Lord 的 pawn 设置 DutyMap，却没有评估整个 LordJob 被替换的影响。
- 修改 UI 文本但只补一种语言。
