# CQF 实体系统参考

## 范围

这份参考覆盖 CQF 实体/NPC 系统：

- `ComplexPawnDef` 与实体编辑器。
- `PawnModDef` 模块。
- `PawnSpawnData` 与 `PawnSpawnData_ComplexPawn`。
- Lord、自定义 LordJob、Duty 与 DutyMap。
- pawn 相关 UI、翻译、action 和 condition。

## ComplexPawnDef

`ComplexPawnDef` 是用于自定义 NPC 的 `Def`。它由实体编辑器编辑，并从 `Quests/Pawn` 热加载。

当前核心字段：

- `defName`
- `label`
- `modDatas`

模块自己的状态放在 `modDatas` 内的 `PawnModData_*` 条目里，不再作为平铺字段直接保存在 `ComplexPawnDef` 上。`Unique` 和 `KindDef` 是基于 `PawnModData_Basic` 的便捷属性。

默认约定：

- 新实体默认男性，除非编辑器或用户明确修改性别。
- `skinColor == null` 表示使用基因/原版默认肤色逻辑。
- 显示优先使用 `label`，序列化和交叉引用使用 `defName`。
- 生成时或地图上下文数据不要放进 `ComplexPawnDef`，例如 Lord 选择应放在实体生成数据里。

## PawnModDef 与 PawnModWorker

模块注册在 `1.6/Defs/QuestEditor_Library.PawnModDef/PawnMods.xml`。

模块数据模式：

- 在 `PawnEdit/PawnMod/PawnEditData.cs` 中添加 `PawnModData_*` 类。
- 重写 `ModDef` 属性，在代码里返回所属模块 Def，通常使用 `this.NamedModDef("CQF_PawnMod_*")`。
- 不要把 `modDef` 字段序列化到 XML；数据类通过属性识别自己的模块。
- 通过 `pawnDef.DataFor<PawnModData_X>()` 读取和修改模块状态。

Worker 生命周期：

1. `CanAddFor(ComplexPawnDef pawnDef)`
   根据当前 pawn kind、race 或 DLC 判断模块是否可用。例如只适用于人类的模块应对动物返回 false。

2. `Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)`
   绘制编辑器 UI，并修改对应 `PawnModData_*`。UI 使用 `CQF_PawnEditor_*` key。

3. `ModifyGenerationRequest(ComplexPawnDef pawnDef, ref PawnGenerationRequest request)`
   在 RimWorld 创建 pawn 之前强制生成参数。适合处理性别、年龄、强制 xenotype、名字约束等。

4. `ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)`
   将模块数据应用到预览 pawn 和生成 pawn。必须能容忍部分 tracker 为空或尚未初始化。不要在这里注册地图运行时状态。

5. `LoadData(ComplexPawnDef pawnDef, XmlNode node)`
   将模块自己的 XML 读取或迁移到对应 `PawnModData_*`。Def 引用保存和读取时使用 `defName`。

6. `OnPawnSpawned(ComplexPawnDef pawnDef, Pawn pawn, Quest quest)`
   pawn 已经实际生成到地图之后执行运行时绑定。对话、行为触发器、任务 tag 或其他地图/任务状态放这里。

## 现有模块

当前实体编辑器模块包括：

- 基础：`defName`、`label`、pawn kind、faction。
- 名字/身体：名字字段、手动随机化 def 名、生成时随机名字、name maker、年龄、性别。
- 外观：发型、发色、肤色覆盖/默认、头型、身体类型。
- 基因：xenotype/基因模板与自定义基因。
- 背景故事：childhood/adulthood。
- 特性。
- 技能：接近原版的等级和热情绘制。
- 能力。
- 服装：按服装层显示，避免重复层级条目。
- 武器。
- Hediff：支持选择具体身体部位，并保存足够信息区分重复部位。
- 对话：生成后绑定 `DialogManagerDef`。
- 行为触发：生成后注册 `ThingActionTrigger`。

新增模块时，优先复制最接近的现有模块风格，不要另起一套编辑器框架。

## 预览与刷新

`QuestEditor_PawnDataEditor` 拥有预览 pawn。

规则：

- `GetPreviewKey()` 只包含必须重新生成 pawn 的变化，目前主要是 `kindDef`。
- `GetPreviewApplyKey()` 包含可以应用到现有预览 pawn 的字段。
- 模块应用后刷新 pawn graphics 和 `PortraitsCache`。
- 不要在每次参数变化时重新生成 pawn，否则会破坏稳定预览并随机掉无关状态。

## 服装与武器

服装和武器模块应遵循：

- 使用类似 `ThingData` 的数据结构，包含 `def` 与可选 `stuff`。
- 尽量按服装层、身体、race 校验可用性。
- 服装按 `ApparelLayerDef.LastLayer` 或等价层级分组显示。
- 同一层服装应替换，而不是允许重复造成混乱。
- 应用到预览 pawn 的 apparel/equipment tracker，并刷新图形。

材质选择使用接近原版 designator 的二次选择流程：先选 ThingDef，如果 `MadeFromStuff` 再选允许的 stuff。

## Hediff

Hediff 数据不能只依赖 `BodyPartDef`，因为 pawn 身体可能存在重复部位，例如左右手臂。

保存时需要足够信息来解析具体部位：

- `part`：`BodyPartDef`
- `partLabel` 或 custom label
- `partIndex` 或稳定的 fallback 位置
- `severity`

应用时解析具体 `BodyPartRecord`；如果当前 race/body 上找不到对应部位，要优雅降级。

## 对话与行为触发

对话模块：

- 保存 `DialogManagerDef`。
- 生成后通过 `GameComponent_Editor.AddDialog(Thing, DialogManagerDef)` 绑定。
- 不要绑定到预览 pawn。

行为触发模块：

- 保存 `PawnActionTriggerData`/`ThingActionTrigger` 数据。
- 生成后绑定。
- 确认选择的 `ActionTriggerMode` 有实际通知路径。
- pawn 受伤触发依赖 damage 通知 patch 转发到 CQF map/component 触发逻辑。

## PawnSpawnData

基础 `PawnSpawnData` 处理通用生成配置：

- `kind`
- `faction`
- `count`
- `spawnType`
- `generationChance`
- inventory
- dialog manager
- Lord 开关、`lordDataName`、`duty`、route、rotation

派生类型：

- `PawnSpawnData_Faction`：使用 faction group maker 与 points。
- `PawnSpawnData_Group`：组生成配置。
- `PawnSpawnData_Random`：从候选数据中随机选择。
- `PawnSpawnData_ComplexPawn`：生成选定的 `ComplexPawnDef`。

`PawnSpawnData_ComplexPawn` 应：

- 显示实体时优先使用 `label`，没有则使用 `defName`。
- 保存选中实体时使用 `defName`。
- 当复杂实体要加入已有/已生成自定义 Lord 时，保存 `lordDataName`。
- 调用 `ComplexPawnDef.GetPawn()`。
- 执行 `ActionAfterGeneration`。
- 如果外部没有传入 Lord，则通过 `MapComponent_CustomMapData.TryGetLord` 解析 `lordDataName`，再把生成 pawn 加入该 Lord。
- 地图生成完成后调用 `ComplexPawnDef.NotifyPawnSpawned`。

## LordJob_Custom

简单 CQF Lord AI：

- `LordJob_Custom` 拥有 pawn 到 duty、route、defend focus 的映射。
- `LordToil_Custom.UpdateAllDuties()` 为每个注册 pawn 创建 `PawnDuty`。
- `pawnDutyDatas` 将 pawn 映射到 `DutyDef`。
- `pawnRouteDatas` 将 pawn 映射到巡逻 route。
- `defendDatas` 将 pawn 映射到防守点。

把 pawn 加入该 Lord 时，如果生成数据选择了 duty，也要填充对应 duty 字典。

## DutyMap / 复杂 Duty

复杂 duty 图 AI：

- `DutyMapDef`：包含节点和转移的 Def。
- `DutyMapNode`：一个 duty 状态。
- `DutyMapTransition`：有向边。
- `CustomDutyTrigger`：转移触发器基类。
- `LordJob_ComplexCustom`：运行 `DutyMapDef`。
- `LordToil_ComplexCustom`：将当前节点的 `PawnDuty` 应用到所属 pawn。
- `QuestEditor_DutyMap`：编辑器 UI。

节点字段：

- `nodeId`
- `duty`
- `focusTarget`、`focusSecondTarget`、`focusThirdTarget`
- `radius`
- `wanderRadius`
- `locomotion`
- `maxDanger`
- `overrideFacing`
- `tag`
- enter/exit action 列表

Target key 通过 CQF target 数据库解析。内部 target key 保持不翻译。

## Pawn 相关 CQFAction 与 Condition

常见 pawn 相关 action：

- `CQFAction_Hediff`
- `CQFAction_SetCustomHediff`
- `CQFAction_Trait`
- `CQFAction_RemoveTrait`
- `CQFAction_UpgradeTrait`
- `CQFAction_Ability`
- `CQFAction_SetDuty`
- `CQFAction_SetXenotype`
- `CQFAction_GainMood`
- `CQFAction_GainExperience`
- `CQFAction_StartMentalState`
- `CQFAction_SetRelation`
- `CQFAction_Faction`

常见 pawn 相关 condition：

- `DialogCondition_Skill`
- `DialogCondition_SkillCheck`
- `DialogCondition_Hediff`
- `DialogCondition_Trait`
- `DialogCondition_Age`
- `DialogCondition_PrisonerOrSlave`
- `DialogCondition_Thought`

涉及 action/condition 分类和 target key 行为时，应结合对应 action/condition skill。

## 翻译位置

使用这些位置：

- 实体编辑器 Keyed UI：`Languages/*/Keyed/PawnEditor.xml`
- PawnModDef 名称和描述：`Languages/*/DefInjected/PawnModDef/PawnMods.xml`，以及存在时的 `Languages/*/DefInjected/QuestEditor_Library.PawnModDef/PawnMods.xml`
- 实体生成数据 UI：`Languages/*/Keyed/PawnData.xml`
- Lord UI：`Languages/*/Keyed/Lord.xml`
- Duty 编辑器通用 UI：`Languages/*/Keyed/Key.xml`
- DutyDef 名称和描述：`Languages/*/DefInjected/DutyDef/*.xml`

除非代码库已经明确使用某个原版/通用 key，否则不要给新的编辑器专用文本复用原版 key。

## 验证清单

完成 pawn 系统改动前检查：

- 使用 `dotnet build .QuestEditor_Library\QuestEditor_Library.sln` 构建。
- UI 文本有英文和简体中文翻译。
- XML 翻译文件按 UTF-8 读取时可解析。
- 新 Def 使用稳定 `defName`，显示使用 `label`。
- 预览只有在 `PawnKindDef` 变化时才重建。
- 地图/运行时绑定没有发生在预览 pawn 上。
- 生成 pawn 能正确加入所选 Lord/Duty 行为。
