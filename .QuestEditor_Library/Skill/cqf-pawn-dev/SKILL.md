---
name: cqf-pawn-dev
description: "开发、扩展、审查或调试 CQF 的实体/NPC 与职责系统。用于 QuestEditor_Library 的 ComplexPawnDef、实体编辑器、PawnModDef/PawnModWorker/PawnModData、PawnSpawnData 及其派生类型、LordData、LordJob_Custom、DutyDef/ThinkNode、DutyMapDef、CustomDutyMap、LordJob_ComplexCustom、职责图触发器、运行时数据库、pawn 相关 CQFAction、翻译和 UI。"
---

# CQF NPC 与职责开发

处理 CQF 自身实现时，先读取本仓库源码；只有需要确认 RimWorld 原版类型或行为时才查看外部源码。先判断需求属于实体定义、生成上下文、Lord、DutyDef 行为树还是 DutyMap 状态图，再沿用对应层的现有结构。

## 按需读取参考

- 处理 `ComplexPawnDef`、PawnMod、实体编辑器、预览 pawn、`PawnSpawnData`、生成流程或 Lord 绑定时，读取 [references/entity-data.md](references/entity-data.md)。
- 处理 `DutyDef`、ThinkNode、JobGiver、`DutyMapDef`、节点切换、触发器、运行时数据库或复杂 Lord 时，读取 [references/duty-map.md](references/duty-map.md)。
- 同时修改实体生成和职责图绑定时，两份参考都读取。

## 系统分层

按以下边界放置数据和逻辑：

1. `ComplexPawnDef`
   保存可复用 NPC 定义，只保留 `defName`、`label` 和 `modDatas`。
2. `PawnModDef + PawnModWorker + PawnModData_*`
   保存并应用名字、外观、基因、技能、装备、对话、职责图等实体模块。
3. `PawnSpawnData` 及派生类
   保存地图或 action 中的生成上下文，例如数量、概率、到达方式、生成后 action 和 Lord 选择。
4. `LordData / LordJob`
   管理一组 pawn 的集群 AI 所有权和默认职责配置。
5. `DutyDef`
   通过 `thinkNode` 和 `constantThinkNode` 决定当前职责怎样发放 Job。
6. `DutyMapDef + CustomDutyMap`
   管理单个 pawn 的职责状态、节点转移和持久化运行时数据。

不要把这些层合并：

- 不要把生成数量、生成概率、地图位置、`lordDataName` 放进 `ComplexPawnDef`。
- 不要把实体模块字段重新平铺到 `ComplexPawnDef`。
- 不要把 Lord 做成 PawnMod。
- 不要把 `DutyDef` 行为树和 `DutyMapDef` 状态图当成同一种对象。
- 不要用静态字段保存某个 pawn 的职责图运行时状态。

## 实体改动流程

1. 确认数据是实体定义还是生成上下文。
2. 实体定义字段放进对应 `PawnModData_*`；没有合适模块时再新增模块。
3. 为新模块分别创建数据类和 Worker 文件，不要把所有类放进一个文件。
4. 在 `PawnModWorker` 的正确阶段处理逻辑：
   - `ModifyGenerationRequest`：生成前约束。
   - `ApplyToPawn`：可作用于预览和生成 pawn 的实体状态。
   - `OnPawnSpawned`：只在进入地图后注册的运行时状态。
5. 兼容旧平铺 XML 时实现 `LoadData`，但新保存格式只写 `modDatas`。
6. UI 文本使用 Keyed；模块名称和描述维护 PawnModDef 翻译。
7. 验证预览不会因为普通字段变化反复重建。

## 新增 PawnMod

新增模块时至少完成：

- 新建 `PawnModData_X`，重写 `ModDef` 和 `SaveToXElement`。
- 新建 `PawnModWorker_X`，重写 `CreateData`。
- 按需要实现 `CanAddFor`、`Draw`、`ModifyGenerationRequest`、`ApplyToPawn`、`LoadData`、`GetPreviewApplyKeyParts`、`OnPawnSpawned`。
- 在 `1.6/Defs/QuestEditor_Library.PawnModDef/PawnMods.xml` 注册 `PawnModDef` 和稳定的 `order`。
- 英文模块名称和描述直接写在 Def；简体中文使用 DefInjected；UI Key 同时维护英文和简体中文。

`PawnModDef.Worker` 是每个 Def 缓存的 Worker 实例。Worker 字段只能保存编辑器缓冲或无实体归属的状态；实体数据必须放入 `PawnModData_*`。

## 生成阶段规则

- `ComplexPawnDef.CreatePawn` 的顺序是：构造 `PawnGenerationRequest`、依次修改请求、生成 pawn、依次应用模块。
- `NotifyPawnSpawned` 是独立阶段；对话、触发器、DutyMap 等地图运行时绑定放在这里。
- 预览调用 `ApplyToPawn(..., true)`，不得注册 Lord、信号接收器、地图组件记录或任务状态。
- `PawnSpawnData.ActionAfterGeneration` 发生在实际放置 pawn 前；需要 `pawn.Spawned` 或地图位置的逻辑不能放在这里。
- 不要静默吞掉生成错误。对无效 Def、缺失节点或无法解析的必要引用记录包含上下文的 `Log.Error`。

## DutyDef 与 DutyMap

始终区分职责行为和职责状态：

- `DutyDef` 决定 pawn 在当前职责下尝试哪些 Job。
- `DutyMapNode` 只选择一个 `DutyDef`，并组装 `PawnDuty` 的 focus、半径、移动方式等参数。
- `DutyMapTransition` 决定何时从一个节点切换到另一个节点。
- `CustomDutyMap` 保存每个 pawn 的当前节点、触发器时间戳和运行时数据库。
- `LordJob_ComplexCustom` 轮询定时转移、执行切换并把节点转换成 `PawnDuty`。

新增职责行为时，优先新增 `ThinkNode_JobGiver` 或复用现有 ThinkNode，再由 `DutyDef` 组合优先级。不要把发 Job 的逻辑直接写进 `DutyMapNode`。

## 职责图切换规则

维护以下语义：

- 同一转移内的所有 trigger 使用 AND；所有 condition 也必须通过。
- 从当前节点按 `transitions` 顺序选择第一个满足条件的转移。
- 节点切换顺序是：旧节点 exit actions、新节点 enter actions、应用新 `PawnDuty`、刷新下一次定时检查。
- `SetDutyMap` 直接设置当前节点并应用 Duty，不执行起始节点 enter actions。
- `SetNode`/`ChangeNode` 是强制跳转；`TryChangeByTransition` 才会验证有向边、trigger 和 condition。
- 只有拥有实际通知入口的 trigger 才能新增；不得只定义 `Triggered` 而不接入 damage、signal、tick 或其他事件路径。

## 运行时数据库

`CustomDutyMap` 是每个 pawn 独立的持久化数据库，可保存：

- `string`
- `int`
- `float`
- `bool`
- `TargetInfo`

通过 `GameComponent_ComplexDuty.Instance.GetRuntime(pawn)` 取得运行时对象并直接调用其方法。未经用户允许不要新增通用 Utility 包装；局部逻辑直接内联。内部 key 使用稳定英文标识并加 `[NoTranslate]`，不要翻译。

注意目标解析不是统一顺序：

- `DutyMapNode` 的 focus key：`Pawn`、Quest、Global、Temporary。
- `JobGiver_TargetBase`：Runtime、Quest、Temporary、Global，并受四个布尔开关控制。

不要假设节点 focus 会自动读取职责图运行时目标。

## 文件与翻译

遵守现有目录：

- 实体定义和编辑器：`PawnEdit`。
- PawnMod：`PawnEdit/PawnMod`，数据类与 Worker 按现有拆分方式组织。
- 生成数据：`PawnData`。
- 职责图：`ComplexDuty`。
- 自定义 JobGiver/JobDriver：`Job/JobGiver`、`Job/JobDriver`。
- 热加载实体：`Quests/Pawn`。
- 热加载 DutyDef 和 DutyMapDef：`Quests/Duty`。

翻译位置：

- 实体编辑器：`Languages/*/Keyed/PawnEditor.xml`。
- 实体生成数据：`Languages/*/Keyed/PawnData.xml`。
- Lord：`Languages/*/Keyed/Lord.xml`。
- Duty/DutyMap 编辑器：`Languages/*/Keyed/CQF_DutyMap.xml`。
- PawnModDef：英文直接使用 Def 中的原文；简体中文放在 `Languages/ChineseSimplified (简体中文)/DefInjected/PawnModDef/PawnMods.xml`，存在兼容路径时同步维护。
- DutyDef：英文直接使用 Def 中的原文；简体中文放在 `Languages/ChineseSimplified (简体中文)/DefInjected/DutyDef/*.xml`。

DLL 文本使用 Key + 双语翻译。不要硬编码中文。内部 key、signal、nodeId、route key、target key 和 record key 不翻译。使用 PowerShell 读取中文 XML 时显式指定 UTF-8。

## 验证

完成修改后按风险执行：

- 构建：`dotnet build .QuestEditor_Library/QuestEditor_Library.sln -v:minimal`。
- 用 UTF-8 解析改过的 XML 和翻译文件。
- 检查 `defName`、nodeId、transition 端点和 startNodeId 均有效且稳定。
- 检查新 PawnMod 数据只存在于对应 `PawnModData_*`。
- 检查预览路径没有注册地图运行时状态。
- 检查生成 pawn 能加入预期 Lord，并在生成后绑定 DutyMap。
- 检查每种 trigger 都有真实通知路径，切换后 Duty 与 enter/exit actions 顺序正确。
- 检查存档加载后职责图 signal receiver 会重新注册。

## 地图 NPC 生活与敌对切换规则

- `Defend` Duty 只会在职责 focus 的有限半径内运行基础需求逻辑。需要 NPC 正常回屋睡觉时，必须在其 focus 半径内放置可达、未占用且地形有效的原版床位；不能只检查地图上“某处有床”。
- 中立 NPC 通过 `CQFAction_Faction` + `CQFAction_SetDuty` 转为敌对时，先验证对话能取回正确 Quest、`PawnSpawnData.dataName` 已写入 QuestData group、目标 `FactionDef` 有运行时 faction 实例、阵营切换后的 pawn 不再受旧 Lord 阻拦，并确认新 Duty 实际产生战斗 Job。
- 关键对话目标优先直接使用 `Interviewee` 执行阵营和职责切换，其他成员再用 `CQFAction_DoActionForGroup` 批量处理，避免单体行为完全依赖分组记录。
- 敌对分支必须提供即时消息或回应节点，并在实机观察 faction、Lord、`mindState.duty` 和首个 Job；静态 XML 可解析不等于 NPC 已经进入战斗状态。
