# CQF 职责与职责图系统

## 目录

- [源码入口](#源码入口)
- [总体架构](#总体架构)
- [DutyDef 行为树](#dutydef-行为树)
- [DutyMapDef 状态图](#dutymapdef-状态图)
- [运行时对象](#运行时对象)
- [职责图绑定入口](#职责图绑定入口)
- [节点切换](#节点切换)
- [触发器](#触发器)
- [目标解析](#目标解析)
- [运行时数据库](#运行时数据库)
- [现有 CQF JobGiver](#现有-cqf-jobgiver)
- [扩展流程](#扩展流程)
- [常见陷阱](#常见陷阱)

## 源码入口

职责定义和编辑器：

- `ComplexDuty/QuestEditor_DutyDefEditor.cs`
- `Extension/ModExtension_CustomDuty.cs`
- `1.6/Defs/Duty/Duty.xml`
- `1.6/Defs/Duty/LordDuty.xml`

职责图定义和编辑器：

- `ComplexDuty/DutyMapDef.cs`
- `ComplexDuty/QuestEditor_DutyMap.cs`
- `ComplexDuty/Dialog_EditDutyMapNode.cs`
- `ComplexDuty/Dialog_EditDutyMapTransition.cs`
- `ComplexDuty/CustomDutyTrigger.cs`

运行时：

- `ComplexDuty/CustomDutyMap.cs`
- `ComplexDuty/GameComponent_ComplexDuty.cs`
- `ComplexDuty/LordJob_ComplexCustom.cs`
- `ComplexDuty/LordToil_ComplexCustom.cs`
- `LordData.cs`

行为节点：

- `Job/JobGiver/JobGiver_*.cs`
- `Job/JobDriver/JobDriver_*.cs`

入口 action 与事件：

- `ActionAndCondition/Actions/CQFActions_Target.cs`
- `Patch/Patch_BuildingDamaged.cs`
- `PawnEdit/PawnMod/PawnModWorker_DutyMap.cs`

## 总体架构

职责系统分成三层：

```text
DutyMapNode
  -> MakeDuty()
  -> PawnDuty(def = DutyDef)
  -> DutyDef.thinkNode / constantThinkNode
  -> ThinkNode_JobGiver
  -> Job / JobDriver
```

- `DutyDef` 是行为树定义。
- `DutyMapDef` 是职责状态图定义。
- `CustomDutyMap` 是每个 pawn 的状态图运行时实例。
- `LordJob_ComplexCustom` 驱动状态图并将当前节点应用为 PawnDuty。
- `LordToil_ComplexCustom.UpdateAllDuties` 只负责让 Lord 内 pawn 重新应用当前节点。

## DutyDef 行为树

`DutyDef` 通过两个根节点控制行为：

- `thinkNode`：主要发 Job 的行为树。
- `constantThinkNode`：持续检查行为。

常见结构：

```xml
<DutyDef ParentName="CustomDutyBase">
  <defName>CQF_Duty_CustomGuard</defName>
  <label>custom guard</label>
  <description>Guards a configured target.</description>
  <alwaysShowWeapon>true</alwaysShowWeapon>
  <thinkNode Class="ThinkNode_Priority">
    <subNodes>
      <li Class="JobGiver_AIFightEnemies">
        <targetAcquireRadius>30</targetAcquireRadius>
        <targetKeepRadius>45</targetKeepRadius>
      </li>
      <li Class="QuestEditor_Library.JobGiver_GotoTarget">
        <targetKey>GuardTarget</targetKey>
      </li>
    </subNodes>
  </thinkNode>
  <modExtensions>
    <li Class="QuestEditor_Library.ModExtension_CustomDuty" />
  </modExtensions>
</DutyDef>
```

`ThinkNode_Priority` 按 subNodes 顺序尝试；前面的节点发出 Job 后不会继续检查后面的节点。战斗通常放在移动、等待或巡逻之前。

### DutyDef 编辑器

编辑器保存到：

```text
Quests/Duty/<defName>.xml
```

保存前会递归设置 `parent`，调用 `ResolveSubnodesAndRecur` 和 `ResolveReferences`，然后使用 `DirectXmlSaver` 序列化根节点。

可选 ThinkNode 类型来自全部非抽象子类，并要求：

- 有无参构造函数。
- 可被当前运行环境加载。

编辑器通过反射绘制字段，排除：

- `[Unsaved]` 字段。
- readonly 字段。
- `subNodes`。
- 编译器 backing field。

当前直接支持字符串、布尔、整数、浮点、枚举、nullable、Def、DutyDef、IntRange、FloatRange、IntVec2/3、Vector2/3、Rot4、泛型 IList 等。无法处理的字段会明确显示 unsupported，而不是自动正确编辑。

新增 ThinkNode 字段时：

- 使用可序列化的公开或私有实例字段。
- 避免只有复杂构造函数的嵌套类型。
- 为字段添加 `CQF_DutyField_<fieldName>` 和必要的 `CQF_DutyFieldTip_<fieldName>`。
- 为节点添加 `CQF_DutyNode_<TypeName>` 和 `_Tip`。
- 不要让编辑器看似可配但运行时没有使用该字段。

## DutyMapDef 状态图

`DutyMapDef` 字段：

- `startNodeId`
- `nextNodeIndex`
- `List<DutyMapNode> nodes`
- `List<DutyMapTransition> transitions`

`StartNode` 优先寻找 `startNodeId`，找不到时回退到第一个节点。开发时仍应保证 `startNodeId` 有效，不要依赖回退掩盖错误。

`CreateNode` 使用 `Node<nextNodeIndex>`，递增索引，并在首次创建时设置起始节点。

### DutyMapNode

节点字段：

- `nodeId`
- `editorPosition`
- `duty`
- `focusTarget`、`focusSecondTarget`、`focusThirdTarget`
- `radius`
- `wanderRadius`
- `locomotion`
- `maxDanger`
- `overrideFacing`
- `tag`
- `enterActions`
- `exitActions`

`MakeDuty` 使用节点 `duty`；为空时回退 `DutyDefOf.Defend`。它创建新的 PawnDuty，并写入上述运行参数。

### DutyMapTransition

转移字段：

- `fromNodeId`
- `toNodeId`
- `List<CustomDutyTrigger> triggers`
- `List<DialogCondition> conditions`

`CanTransition` 规则：

- triggers 为空时 trigger 部分通过。
- triggers 不为空时全部 `Triggered == true` 才通过。
- conditions 为空时 condition 部分通过。
- conditions 不为空时任意一个失败都会阻止转移。

condition 上下文会复制调用方 targets，并在缺少 `Target` 时加入当前 pawn。它不会自动把 CustomDutyMap.Targets 全部注入 condition 字典。

### XML 示例

```xml
<QuestEditor_Library.DutyMapDef>
  <defName>CQF_DutyMap_Guard</defName>
  <label>guard duty map</label>
  <startNodeId>Node1</startNodeId>
  <nextNodeIndex>3</nextNodeIndex>
  <nodes>
    <li>
      <nodeId>Node1</nodeId>
      <editorPosition>(80, 120)</editorPosition>
      <duty>QE_Duty_Waiter</duty>
      <focusTarget>GuardPoint</focusTarget>
    </li>
    <li>
      <nodeId>Node2</nodeId>
      <editorPosition>(280, 120)</editorPosition>
      <duty>QE_Duty_Guard</duty>
    </li>
  </nodes>
  <transitions>
    <li>
      <fromNodeId>Node1</fromNodeId>
      <toNodeId>Node2</toNodeId>
      <triggers>
        <li Class="QuestEditor_Library.CustomDutyTrigger_Damaged" />
      </triggers>
    </li>
  </transitions>
</QuestEditor_Library.DutyMapDef>
```

保持 nodeId 唯一且稳定。删除或重命名节点时同步修正 `startNodeId`、所有 transition 端点和外部 `dutyMapStartNodeId`。

## 运行时对象

### GameComponent_ComplexDuty

GameComponent 保存：

```text
Dictionary<Pawn, CustomDutyMap>
```

`GetRuntime(pawn)` 在不存在时创建空运行时，并回写 pawn 引用。它不是纯查询；不要用它判断“是否已经绑定职责图”。应继续检查 `runtime.dutyMap`。

存档：

- Pawn 使用 Reference。
- CustomDutyMap 使用 Deep。
- 保存前移除 null、死亡、销毁或无 dutyMap 的条目。
- PostLoadInit 后重新设置 Pawn，并重新注册 signal receiver。

`Remove(pawn)` 会注销 signal receiver 并删除运行时；新增 Pawn 生命周期清理路径时调用它。

### CustomDutyMap

持久化字段：

- `dutyMap`
- `currentNodeId`
- `lastTransitionTick`
- `nextTickTransitionTick`
- `lastDamageTick`
- `lastSignal`
- `lastSignalTick`
- strings、ints、floats、bools、targets 数据库

`CurrentNode` 会先按 currentNodeId 查找，失败时回退 DutyMap.StartNode。不要依赖该回退长期容忍无效 currentNodeId。

### LordJob_ComplexCustom

职责：

- StateGraph 中只创建一个 `LordToil_ComplexCustom`。
- 每 tick 遍历 owned pawns，但只在 `nextTickTransitionTick <= 当前 tick` 时检查定时转移。
- 应用当前节点 PawnDuty。
- 执行强制跳转或经 transition 验证的跳转。
- 维护默认 DutyMap 和默认起始节点。

`EnsureForPawn` 行为：

- Pawn 无 Map 时返回 null。
- Pawn 已属于 `LordJob_ComplexCustom` 时复用。
- Pawn 已属于其他 LordJob 时，直接把整个 Lord 的 Job 替换为 `LordJob_ComplexCustom`。
- Pawn 无 Lord 时新建复杂 Lord 并加入 Pawn。

给已有 Lord 中单个 pawn绑定职责图前，必须评估 LordJob 替换对同组 pawn 的影响。

## 职责图绑定入口

### LordData 默认图

`LordJobData.CreateJob` 在选择 `LordJob_ComplexCustom` 时写入：

- `defaultDutyMap`
- `defaultStartNodeId`

普通 PawnSpawnData 把 pawn 加入复杂 Lord 后会调用 `ApplyDefaultDutyMap`。

### ComplexPawnDef DutyMap 模块

`PawnModWorker_DutyMap`：

- `ApplyToPawn` 在非预览生成阶段预先设置职责图；此时 pawn 可能还没有 Map，复杂 Lord 不一定已经建立。
- `OnPawnSpawned` 再次设置职责图，并在指定起始节点有效时调用 `ChangeNode`。
- 该模块可能覆盖 Lord 默认图。

指定的 `dutyMapStartNodeId` 与 Def 的 startNode 不同时，当前实现先通过 `SetDutyMap` 落到 startNode，再调用 `ChangeNode`。这会执行 startNode 的 exitActions 和指定节点的 enterActions；不要把 startNode exitActions 设计成只有“真正进入过该节点”才允许执行的逻辑。

### CQFAction_Pawn_SetDutyMap

字段：

- `dutyMap`
- `useStartNode`，默认 true。

对 targets 中所有 Pawn 调用 `SetDutyMap`。

### CQFAction_Pawn_RunDutyMapTransition

字段：

- 可选 `dutyMap`
- `toNodeId`

行为：

1. 如果指定 dutyMap，先使用起始节点重设职责图。
2. 读取当前节点。
3. 调用 `TryChangeByTransition(current, toNodeId)`。

它不是强制跳转；必须存在对应有向边且 trigger、condition 全部满足。旧字段 `nodeId` 会兼容迁移到 `toNodeId`。

## 节点切换

### SetDutyMap

顺序：

1. `EnsureForPawn`。
2. 设置 runtime.dutyMap。
3. 注册 signal receiver。
4. 根据 `useStartNode` 和当前节点有效性选择 start node。
5. 更新 `lastTransitionTick`。
6. 应用当前节点 PawnDuty。
7. 立即尝试一次 tick transition，用于安排下一次检查。

它不会执行起始节点 enterActions。

它也不会清空 strings、ints、floats、bools 或 targets。切换到另一个 DutyMap 时运行时数据库会继续保留，除非调用方显式 `ClearDatabase` 或移除对应 key。

### ChangeNode

顺序：

1. 读取旧节点。
2. 直接写入 `currentNodeId` 和 `lastTransitionTick`。
3. 读取新节点。
4. 生成 targets，确保包含 `Target = pawn`。
5. 执行旧节点 exitActions。
6. 执行新节点 enterActions。
7. 应用新 PawnDuty。
8. 刷新下一次 tick transition 时间。

当前 `ChangeNode` 没有先验证 nodeId 是否存在。调用强制跳转 API 前必须自行验证目标节点，并在无效时记录错误。

### Transition 选择

- `TransitionsFrom` 保留 Def 中列表顺序。
- 自动检查遇到第一个满足条件的转移后立即切换并返回。
- 多条出边的顺序就是优先级。
- `TryChangeByTransition` 只取 from/to 相同的第一条边。

## 触发器

### TickInterval

- 条件：当前 tick 与 lastTransitionTick 的差值达到 intervalTicks。
- `RefreshTickTransition` 扫描当前节点所有出边，取最小正 interval。
- 到期后只检查包含 TickInterval 的转移。
- 若检查过但未切换，会重新安排下一次检查。

### Damaged

- Harmony patch 接入 `Pawn.PostApplyDamage`。
- 写入 `lastDamageTick = 当前 tick`。
- 只检查包含 `CustomDutyTrigger_Damaged` 的转移。
- Triggered 仅在同一 tick 返回 true。

### Signal

- CustomDutyMap 注册到 `Find.SignalManager`。
- 收到非空 signal 后记录 tag 和 tick。
- 只检查包含 `CustomDutyTrigger_Signal` 的转移。
- `addQuestPrefix` 会把配置 signal 解析为 `Quest<id>.<signal>`。
- signal 为空时接受任意同 tick signal。

### LordPawnCountBelow

- 条件：当前 Lord 的 ownedPawns.Count 小于 count。
- 当前没有独立事件入口主动检查这种 trigger。
- 单独使用时不会自动轮询；通常与 TickInterval 组合，或由外部代码显式调用 `TryRunTriggeredTransition`。

### 组合语义

所有 trigger 是 AND。例如 Damaged 与 TickInterval 同时存在时，必须在受伤同一 tick 又满足间隔，通常不是想要的 OR 行为。需要 OR 时建立多条相同目标的转移，并按优先级排列。

## 目标解析

### DutyMapNode focus

`ResolveTarget` 顺序：

1. key 为空：Invalid。
2. key 为 `Pawn`：当前 pawn。
3. Quest database。
4. Global database。
5. Temporary database。

它不读取 CustomDutyMap runtime targets。

### JobGiver_TargetBase

字段：

- `targetKey`
- `useRuntimeDatabase`
- `useQuestDatabase`
- `useTemporaryDatabase`
- `useGlobalDatabase`

默认顺序：

1. Runtime。
2. Quest。
3. Temporary。
4. Global。

`JobGiver_Wait`、`JobGiver_GotoTarget`、`JobGiver_RepairTarget` 等复用该逻辑。四个数据库可能出现相同 key；文档和 XML 中应明确期望来源。

## 运行时数据库

`CustomDutyMap` 为每个 Pawn 保存独立数据：

```csharp
CustomDutyMap runtime = GameComponent_ComplexDuty.Instance.GetRuntime(pawn);
runtime.SetString("Phase", "Alert");
runtime.SetValue("PatrolRouteIndex", 0);
runtime.SetFloat("SearchRadius", 12f);
runtime.SetBool("HasSeenEnemy", true);
runtime.RecordTarget("RepairTarget", target);
```

读取方法：

- `GetString`
- `GetValue`
- `GetFloat`
- `GetBool`
- `GetTarget`
- `TargetExists`
- `HasKey`

修改方法：

- `SetString`
- `SetValue`
- `SetFloat`
- `SetBool`
- `RecordTarget`
- `RemoveKey`
- `ClearDatabase`

规则：

- key 为空时 setter 不写入。
- 同名 key 可以同时存在于不同类型字典；`HasKey` 会检查全部类型。
- `RemoveKey` 会删除该名称在全部类型中的数据。
- Target 使用 `TargetWithKey` 深度保存。
- key 是内部标识，使用稳定英文并加 `[NoTranslate]`。
- 未经允许不要创建通用 Utility；在拥有 Pawn 的调用点直接取得 runtime 并操作。

## 现有 CQF JobGiver

### 旧简单 Lord 行为

- `JobGiver_Patrol`：读取 `LordJob_Custom` 的 route 数据。
- `JobGiver_Wait`：优先按 targetKey 读取数据库，失败时兼容旧防守点。
- `JobGiver_ExitSubMapAndExitMap`：先找子地图出口，再找地图边缘。
- `JobGiver_MoveToNewLevel`：处理多层级 Lord 移动请求。

### 职责图巡逻

`JobGiver_PatrolMove`：

- 通过 `routeKey` 从 `MapComponent_CustomMapData.route` 读取路线。
- 通过 runtime int `routeIndexKey` 读取当前位置索引。
- 不可达时推进索引。
- 到达目标点时不发 Job，交给观察节点。

`JobGiver_PatrolObserve`：

- 先尝试敌人战斗 Job。
- Pawn 位于当前路线点时创建 `CQF_DutyMapLookAround`。
- JobDriver 左右观察完成后推进同一个 routeIndexKey。

组合建议：

```xml
<thinkNode Class="ThinkNode_Priority">
  <subNodes>
    <li Class="QuestEditor_Library.JobGiver_PatrolObserve">
      <routeKey>OuterRoute</routeKey>
      <routeIndexKey>OuterRouteIndex</routeIndexKey>
    </li>
    <li Class="QuestEditor_Library.JobGiver_PatrolMove">
      <routeKey>OuterRoute</routeKey>
      <routeIndexKey>OuterRouteIndex</routeIndexKey>
    </li>
  </subNodes>
</thinkNode>
```

观察节点必须在移动节点之前，否则到达点后的行为可能不符合预期。

## 扩展流程

### 新增 JobGiver

1. 在 `Job/JobGiver` 新建独立类文件。
2. 继承最接近的 ThinkNode 或 `ThinkNode_JobGiver`。
3. 只在 `TryGiveJob` 决定和构造 Job；复杂执行拆到 JobDriver。
4. 公开可序列化配置字段，并为编辑器补字段翻译。
5. 如需目标，优先继承 `JobGiver_TargetBase`。
6. 为节点名称和提示补 `CQF_DutyNode_<TypeName>` 英文和简体中文 Key。
7. 用 DutyDef 组合，不要在 DutyMapNode 中硬编码。

### 新增 trigger

1. 新建 `CustomDutyTrigger_X`，实现 `Triggered`。
2. 实现 `Draw`、`SaveToXElement`、`ExposeData`。
3. 添加类型名称、提示和字段双语翻译。
4. 建立真实通知入口，并写入运行时事件状态。
5. 通知入口调用 `TryRunTriggeredTransition` 并传入精确 trigger type。
6. 验证存档加载后事件接收仍有效。

### 新增运行时数据使用者

1. 明确 key 的所有者和类型。
2. 在拥有 Pawn 的局部逻辑直接取得 runtime。
3. 写入与读取使用同一 key 常量或稳定字段。
4. 处理 Pawn 为空、Component 为空、runtime.dutyMap 为空。
5. 不要静默忽略必要数据缺失；记录 pawn、map、dutyMap、nodeId 和 key。

## 常见陷阱

- 把 DutyDef 当成状态图，或把 DutyMapNode 当成 JobGiver。
- 以为 `SetDutyMap` 会执行 start node enterActions。
- 以为 `ChangeNode` 会校验节点存在或校验 transition。
- 以为多个 trigger 是 OR。
- 单独使用 `LordPawnCountBelow`，却没有 tick 或显式检查入口。
- 认为所有 target key 都按同一种数据库顺序解析。
- 调用 `GetRuntime` 判断是否绑定，忽略它会自动创建空运行时。
- 给一个普通 Lord 中的 pawn设置 DutyMap，意外替换整个 LordJob。
- 删除节点后没有修正 transition、startNodeId 或外部 start node 引用。
- 新增 ThinkNode 复杂字段，但 DutyDef 编辑器无法编辑该类型。
- 新增 trigger 类却没有接入事件通知。
- 只修改中文或只修改英文翻译。
