---
name: cqf-pawn-dev
description: "CQF 实体/NPC 开发 skill。用于 QuestEditor_Library 的 ComplexPawnDef、实体编辑器、PawnModDef/PawnModWorker、PawnModData、实体生成数据、PawnSpawnData_ComplexPawn、Lord/LordJob、Duty/DutyMap、pawn 相关 CQFAction/DialogCondition、翻译和 UI 扩展。"
---

# CQF 实体开发

本 skill 是 CQF 实体系统的扩展文档。处理实体编辑器、实体生成数据、PawnMod 模块、Lord、Duty、DutyMap 或 pawn 相关运行时逻辑时，只按本文的分层和规则判断归属并设计实现；不要把它当成源码索引。

## 总体分层

实体系统分为四层：

- `ComplexPawnDef`：定义自定义 NPC/实体本身。
- `PawnModDef + PawnModWorker + PawnModData_*`：定义可扩展编辑模块，以及模块如何修改预览 pawn、生成请求、生成 pawn、保存数据。
- `PawnSpawnData_ComplexPawn`：定义地图或 action 里“生成哪个复杂实体、生成多少、何时生成、加入哪个 Lord”等生成上下文。
- Lord/Duty/DutyMap：定义生成后 pawn 的集群 AI 与职责行为。

关键原则：

- `ComplexPawnDef` 只保存实体定义，不保存地图生成上下文。
- 模块自己的数据保存到 `ComplexPawnDef.modDatas` 里的 `PawnModData_*`，不要直接在 `ComplexPawnDef` 上堆字段。
- `PawnSpawnData_ComplexPawn` 负责生成时信息，例如 `lordDataName`；Lord 不属于 PawnMod，也不属于 `ComplexPawnDef`。
- 可热加载实体 Def 保存到 `Quests/Pawn`。
- UI 文本使用 key + 双语翻译，不硬编码中文。

## ComplexPawnDef

`ComplexPawnDef` 是可编辑 NPC/实体 Def。不要重新引入 `ComplexPawnData` 作为主模型，也不要增加 `CharacterSpawnDef` 中间层。

`ComplexPawnDef` 的核心字段应保持精简：

- `defName`
- `label`
- `modDatas`

便捷属性可以从模块数据派生，例如：

- `Unique` 来自 `PawnModData_Basic.unique`
- `KindDef` 来自 `PawnModData_Basic.kindDef`

保存和显示约定：

- 显示优先用 `label`，没有则用 `defName`。
- XML 交叉引用保存 `defName`。
- 新建实体默认男性，除非用户修改。
- 旧版平铺字段需要兼容迁移到 `PawnModData_*`。

禁止事项：

- 不要把名字、外观、服装、武器、技能、基因等模块字段直接加回 `ComplexPawnDef`。
- 不要把 Lord、spawn count、spawn chance、spawn type、route、duty 这类生成上下文放进 `ComplexPawnDef`。
- 不要在 `ComplexPawnDef` 里处理地图运行时绑定。

## PawnMod 数据模式

每个实体编辑器模块由三部分组成：

- `PawnModDef`：模块 Def，包含 label、description、order、workerClass 等。
- `PawnModWorker` 子类：模块行为。
- `PawnModData_*` 子类：模块自己的可保存数据。

`PawnModData_*` 规则：

- 每个模块添加一个对应数据类，例如 `PawnModData_Skills`、`PawnModData_Apparel`。
- `PawnModData.ModDef` 是代码属性，返回所属 `PawnModDef`。
- 常用写法是 `this.NamedModDef("CQF_PawnMod_*")`。
- 不要把 `<modDef>` 写入 XML。
- 模块数据通过 `pawnDef.DataFor<PawnModData_X>()` 读取和修改。

XML 保存形态应类似：

```xml
<modDatas>
  <li Class="QuestEditor_Library.PawnModData_Basic">
    <kindDef>Colonist</kindDef>
  </li>
</modDatas>
```

## PawnModWorker 职责

`PawnModWorker` 的职责必须拆开：

- `CanAddFor(ComplexPawnDef)`：根据当前 `PawnKindDef`、race 或 DLC 判断模块是否可用。
- `CreateData()`：返回当前模块对应的 `PawnModData_*`。
- `Draw(...)`：绘制 UI 并编辑模块数据。
- `ModifyGenerationRequest(...)`：在 `PawnGenerator.GeneratePawn` 前修改生成请求。
- `ApplyToPawn(...)`：修改预览 pawn 和生成 pawn。
- `LoadData(...)`：把 XML 读取或迁移到对应 `PawnModData_*`。
- `GetPreviewApplyKeyParts(...)`：声明哪些数据变化可应用到现有预览 pawn。
- `OnPawnSpawned(...)`：pawn 已进入地图后绑定运行时状态。

适用边界：

- 性别、年龄、xenotype、名字生成约束等生成前参数放在 `ModifyGenerationRequest`。
- 发型、发色、肤色、身体类型、服装、武器、hediff、ability、trait、skill 等能直接作用于 pawn 的内容放在 `ApplyToPawn`。
- 对话绑定、行为触发器注册、任务 tag、地图组件注册等只属于运行时的内容放在 `OnPawnSpawned`。
- 不要在预览 pawn 上注册地图运行时状态。

## 预览刷新规则

实体编辑器左侧预览 pawn 应稳定，不应每改一个参数就重新生成。

规则：

- 只有 `PawnKindDef` 变化时才重建预览 pawn。
- 普通参数变化时，对现有预览 pawn 应用模块。
- 应用后刷新 pawn graphics 和 `PortraitsCache`。
- `GetPreviewKey()` 只放必须重新生成 pawn 的字段。
- `GetPreviewApplyKey()` 或 `GetPreviewApplyKeyParts(...)` 放可以直接应用的字段。

常见要求：

- 服装和武器改动要显示在立绘上。
- 发型、身体、肤色、基因、hediff、skill、trait、ability 改动要尽量即时显示。
- 人形肤色默认由基因优先；只有用户明确设置自定义肤色时才覆盖。
- 非人形动物的随机颜色/外观通常来自 `PawnKindDef.alternateGraphics`，由 `PawnGraphicUtils.TryGetAlternate` 按 `pawn.thingIDNumber` 稳定随机选择。
- 不要把动物颜色当作 `CompColorable` 或通用 `DrawColor` 处理；`CompColorable` 是物品染色路径。

## 现有模块约定

基础模块：

- 保存 `defName`、`label`、`kindDef`、faction、unique 等基础信息。
- `kindDef` 变化会触发预览 pawn 重建。

名字/身体模块：

- 保存 first/nick/last name、name maker、年龄、性别、生成时随机名字等。
- “随机化名字”是手动随机化 def 当前名字。
- “生成时随机名字”是 checkbox，表示生成 pawn 时是否重新随机名字。
- name maker 选择应显示当前选择，尽量筛选适合当前 pawn 的 name maker。

外观模块：

- 保存发型、发色、肤色、头型、身体类型。
- 发型显示使用 `label`。
- 身体类型显示使用 defName 翻译，无法翻译时显示原文。
- 肤色为空表示使用基因/默认。
- 动物颜色不是通用 `DrawColor` 或 `CompColorable`；原版动物随机外观通常由 `PawnKindDef.alternateGraphics` 的贴图/颜色变体限定，后续应作为外观模块扩展处理。

基因模块：

- 保存 xenotype/基因模板，以及自定义基因列表。
- 基因模板和自定义基因属于基因模块数据，不要加到 `ComplexPawnDef`。

背景故事模块：

- 保存 childhood/adulthood。
- 只对可用 backstory 的 pawn 显示。

特性模块：

- 保存 trait 和 degree。
- 应用时先处理重复和冲突，避免同一 trait 重复添加。

技能模块：

- 使用接近原版的等级与热情绘制。
- 等级优先用拖动方式修改，而不是只填数字。
- 热情可点击区域要有小方块背景或明确可点击状态。

能力模块：

- 保存 `AbilityDef` 列表。
- 生成和预览时添加缺失能力，避免重复。

服装模块：

- 使用 `ThingData` 风格数据，包含 apparel def 和可选 stuff。
- UI 按服装层显示，点击选择对应服装，避免同一层重复堆叠。
- 同层服装应替换，而不是追加重复项。
- 选择 `MadeFromStuff` 服装时，先选 def，再二次选择允许的 stuff。
- 应用到预览 pawn 的 apparel tracker，并刷新图形。

武器模块：

- 使用 `ThingData` 风格数据，包含 weapon def 和可选 stuff。
- 选择 `MadeFromStuff` 武器时，先选 def，再二次选择允许的 stuff。
- 应用到 equipment tracker，并刷新图形。

Hediff 模块：

- 保存 hediff、severity、具体身体部位信息。
- 不要只保存 `BodyPartDef`，因为身体可能有左右重复部位。
- 保存 `part`、`partLabel` 或 `partIndex` 等足够解析具体 `BodyPartRecord` 的信息。
- 当前 race/body 找不到对应部位时要优雅降级。

对话模块：

- 保存 `DialogManagerDef`。
- 生成后通过 `GameComponent_Editor.AddDialog(Thing, DialogManagerDef)` 绑定。
- 不绑定预览 pawn。

行为触发模块：

- 保存 `PawnActionTriggerData` 或 `ThingActionTrigger` 数据。
- 每个行为触发者独立成框显示。
- 添加和删除触发行为使用图标按钮。
- 生成后注册触发器，不注册到预览 pawn。
- 选择的 `ActionTriggerMode` 必须有实际通知路径。

DutyMap 模块：

- 保存实体自身生成后需要绑定的复杂 duty map 数据。
- 只保存实体定义级别的 duty map 信息。
- 不保存 spawn-time 的 Lord 名称；Lord 名称属于 `PawnSpawnData_ComplexPawn`。

## PawnSpawnData_ComplexPawn

地图或 action 需要生成 `ComplexPawnDef` 时使用 `PawnSpawnData_ComplexPawn`。

职责：

- 选择一个 `ComplexPawnDef`。
- 控制生成数量、生成概率、生成时机等继承自 `PawnSpawnData` 的通用信息。
- 保存 `lordDataName` 字符串，用于生成时加入已有/已生成自定义 Lord。
- 调用 `ComplexPawnDef.GetPawn()` 生成 pawn。
- 执行 `ActionAfterGeneration`。
- 将 pawn 加入传入的 Lord，或用 `lordDataName` 在当前地图的 `MapComponent_CustomMapData` 中查找 Lord 后加入。
- 地图生成后调用 `ComplexPawnDef.NotifyPawnSpawned`。

Lord 规则：

- Lord 不做 PawnMod。
- Lord 不写进 `ComplexPawnDef`。
- `PawnSpawnData_ComplexPawn` 只需要保存 `lordDataName` 这种生成数据字符串。
- 如果外部已经传入 Lord，优先使用外部 Lord。
- 如果外部没有传入 Lord 且 `lordDataName` 不为空，使用 `MapComponent_CustomMapData.TryGetLord(lordDataName, out lord)`。

显示和保存：

- 复杂实体按钮显示 `label`，没有则 `defName`。
- 保存实体引用用 `<pawnDef>defName</pawnDef>`。
- 保存 Lord 用 `<lordDataName>name</lordDataName>`。

## 普通 PawnSpawnData 区分

普通 `PawnSpawnData` 仍负责原版 PawnKind 生成数据：

- `kind`
- `faction`
- `count`
- `spawnType`
- `generationChance`
- inventory
- dialog manager
- Lord enablement
- `lordDataName`
- `duty`
- route
- rotation

`PawnSpawnData_ComplexPawn` 可以复用这些通用字段，但复杂实体自己的定义由 `ComplexPawnDef` 和模块决定。

## Lord 与 Duty

简单自定义 Lord 使用：

- `LordJob_Custom`
- `LordToil_Custom`
- `pawnDutyDatas`
- `pawnRouteDatas`
- `defendDatas`

职责：

- `pawnDutyDatas`：pawn 到 `DutyDef` 的映射。
- `pawnRouteDatas`：pawn 到巡逻 route 的映射。
- `defendDatas`：pawn 到防守点的映射。
- `LordToil_Custom.UpdateAllDuties()` 根据这些映射创建 `PawnDuty`。

复杂 Duty 图使用：

- `DutyMapDef`
- `DutyMapNode`
- `DutyMapTransition`
- `CustomDutyTrigger`
- `LordJob_ComplexCustom`
- `LordToil_ComplexCustom`

DutyMap 节点常见字段：

- `nodeId`
- `duty`
- `focusTarget`
- `focusSecondTarget`
- `focusThirdTarget`
- `radius`
- `wanderRadius`
- `locomotion`
- `maxDanger`
- `overrideFacing`
- `tag`
- enter/exit action 列表

规则：

- 生成 pawn 并加入 Lord 时，要保证 duty 路径适配该 LordJob。
- target key 是内部标识，不翻译。
- Duty 编辑器文本补全时走 Keyed/DefInjected，不硬编码中文。

## Pawn 相关 CQFAction 与 Condition

常见 pawn action：

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

常见 pawn condition：

- `DialogCondition_Skill`
- `DialogCondition_SkillCheck`
- `DialogCondition_Hediff`
- `DialogCondition_Trait`
- `DialogCondition_Age`
- `DialogCondition_PrisonerOrSlave`
- `DialogCondition_Thought`

使用规则：

- action 是副作用，condition 是纯判定。
- target key 不要翻译。
- 需要跨阶段引用 pawn 时，先记录到数据库或 group。

## UI 规则

实体编辑器 UI 规则：

- 左侧显示当前目标 pawn 立绘。
- 模块列表在右侧；点击模块后绘制该模块内容。
- 不使用“文字 + 按钮”的冗余样式；需要时使用无背景文本按钮风格。
- 控件范围不能超出窗口，也不能出现文本重叠。
- 删除、添加等短命令优先使用图标按钮。
- 模块 UI 应按内容分组，避免一坨文本挤在一起。
- 文本不能硬编码中文，必须走翻译 key。

显示名规则：

- `PawnSpawnData` 处显示当前选择项的显示名。
- `ComplexPawnDef` 显示优先 `label`。
- 发型使用 `label`。
- body type 使用 defName 翻译，无法翻译时显示原文。

## 翻译规则

文本位置：

- 实体编辑器 Keyed UI：`Languages/*/Keyed/PawnEditor.xml`
- 实体生成数据 UI：`Languages/*/Keyed/PawnData.xml`
- Lord UI：`Languages/*/Keyed/Lord.xml`
- Duty 编辑器通用 UI：`Languages/*/Keyed/Key.xml`
- PawnModDef 名称和描述：`Languages/*/DefInjected/PawnModDef/PawnMods.xml`
- 存在 `Languages/*/DefInjected/QuestEditor_Library.PawnModDef/PawnMods.xml` 时同步维护。
- DutyDef 名称和描述：`Languages/*/DefInjected/DutyDef/*.xml`

规则：

- DLL/UI 文本采用 Key + 翻译。
- 不使用原版已有 key 表示新的 CQF 编辑器含义。
- 实体编辑器的 key 翻译单独放在独立实体编辑器翻译文件。
- 中文和英文都要补。
- PowerShell 读写中文翻译 XML 时显式使用 UTF-8。

## 目录与文件组织

组织规则：

- 实体编辑器相关类放在 `PawnEdit`。
- PawnMod 相关类放在 `PawnEdit/PawnMod` 子文件夹。
- 不要把所有类塞到一个文件。
- 实体相关 skill 作为备份放在源码文件夹的 `Skill/cqf-pawn-dev` 下。
- `Quests/Pawn` 保存热加载实体数据。

命名规则：

- Def 名称稳定，XML 引用使用 `defName`。
- UI 显示使用 `label` 或翻译文本。
- 内部 key、target key、signal、record key 不翻译。

## 旧数据兼容

从旧结构迁移到 `modDatas` 时：

- 保留 `defName`、`label`、`modDatas`。
- 旧平铺字段由对应 `PawnModWorker.LoadData` 迁移到 `PawnModData_*`。
- 不要把迁移兼容当成新 XML 结构继续使用。
- 新保存格式应只保存模块数据。

## 验证清单

完成实体系统改动前检查：

- 构建：`dotnet build .QuestEditor_Library\QuestEditor_Library.sln -v:minimal`。
- UI 文本有英文和简体中文翻译。
- XML 翻译文件用 UTF-8 读取时可解析。
- 新 Def 使用稳定 `defName`，显示使用 `label`。
- 模块数据保存在 `PawnModData_*`，没有新增模块字段到 `ComplexPawnDef`。
- `PawnModData.ModDef` 没有写入 XML。
- 预览只有 `PawnKindDef` 变化时才重建。
- 地图/运行时绑定没有发生在预览 pawn 上。
- 服装、武器、外观、基因等模块能正确更新左侧立绘。
- `PawnSpawnData_ComplexPawn.lordDataName` 能在生成时加入对应 Lord。
- Lord 没有被做成 PawnMod。
