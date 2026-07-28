---
name: "cqf-map-dev"
description: "Detailed skill for building CQF maps, submaps, zone layouts, interactions, traps, loot, signals, and quest-linked map logic. Invoke when user wants AI to design or generate CQF maps."
---

# CQF Map Dev

用于 `CQF` 地图、子地图、区域拼装、地图交互、地图事件、地图任务联动的详细 Skill。

本 Mod 的地图 Skill 源文件位于：
`D:\Game\Steam\steamapps\common\RimWorld\Mods\CQF\.QuestEditor_Library\Skill\cqf-map-dev\SKILL.md`

本 Skill 的目标是让 AI 在处理地图需求时，不只是“知道类名”，而是能自己产出一套完整地图方案，包括：
- 地图结构
- 子地图与入口出口
- 区域拼装
- 交互对象
- 战利品箱
- 陷阱
- 门与容器
- 行为与条件
- 对话
- 信号链
- 任务联动
- 文本与翻译

## 调用时机

在以下情况调用本 Skill：
- 用户要制作 CQF 地图
- 用户要制作 CQF 子地图
- 用户要制作入口 / 出口 / 地图切换
- 用户要设计地图内交互、机关、战利品箱、陷阱、门禁
- 用户要设计地图推进流程
- 用户要做区域拼装或房间部件生成
- 用户要让 AI 直接产出一张 CQF 地图的完整实现方案

如果任务只是想了解 CQF 的总体结构、子 Mod 总览或框架边界，可先调用 `cqf-overview`。

## 核心目标

本 Skill 的最终目标不是“帮用户查源码”，而是让 AI 可以直接做出以下内容：
- 一张完整的 CQF 地图
- 一套可进入、可推进、可完成、可离开的地图流程
- 一组地图中的交互对象、陷阱、箱子、门禁与事件
- 一套由条件、动作、数据库、信号串起来的地图脚本

## 地图制作的五层模型

CQF 地图不是“铺砖 + 放东西”。
CQF 地图通常由五层共同组成：

### 1. 地图结构层

决定：
- 这是一张什么地图
- 是独立任务地图还是子地图
- 是固定布局还是模块拼装
- 如何进入和离开

### 2. 对象层

决定：
- 地图上放什么对象
- 哪些对象负责交互
- 哪些对象负责奖励
- 哪些对象负责惩罚
- 哪些对象负责推进

### 3. 条件层

决定：
- 某个交互能不能做
- 某个门是否可开
- 某个阶段是否完成
- 某个事件是否可以触发

### 4. 行为层

决定：
- 触发后会发生什么
- 生成什么
- 记录什么
- 发什么信号
- 谁被改变

### 5. 流程层

决定：
- 玩家如何从进入 -> 探索 -> 解锁 -> 获得奖励 -> 离开
- 任务节点和地图节点如何接起来

AI 做 CQF 地图时，必须同时考虑这五层。

## AI 默认工作流

收到地图需求后，默认按这个顺序思考：

1. 先判断地图类型
- 世界任务地图
- 现有地图中的子地图
- 固定布局地图
- 随机拼装地图
- 混合地图

2. 再判断流程类型
- 纯探索
- 对话推进
- 机关推进
- 多阶段战斗
- 钥匙门禁
- 奖励房
- Boss 房
- 护送 / 搜索 / 破坏目标

3. 再确定核心对象
- 入口
- 出口
- 关键交互点
- 关键门禁
- 箱子
- 陷阱
- 刷怪点
- 目标对象

4. 再设计状态流
- 哪些对象需要记到数据库
- 哪些阶段需要发信号
- 哪些条件限制推进
- 哪些对象只在某阶段激活

5. 最后补文本与本地化
- 交互文案
- 消息文本
- 任务描述
- 中英双语键

## 地图系统总览

CQF 地图体系的核心概念：

- `CustomMapDataDef`
- 自定义地图定义，描述地图内容与生成逻辑

- `CustomMapGenerationSet`
- 地图集合选择器，用于从候选地图中抽取或指定地图

- `ZoneCore`
- 区域拼装核心对象，用于地图部件连接、选择与限制生成

- `CustomMapEntrance`
- 子地图或特殊地图入口

- `CustomMapExit`
- 子地图或特殊地图出口

- `QuestNode_Root_CustomMap`
- 从任务侧生成 CQF 自定义地图

- `QuestNode_RandomCustomMap`
- 随机挑选自定义地图

- `CQFAction_GenerateSubMap`
- 在已有地图内部生成子地图

一句话理解：
- `CustomMapDataDef` 决定“地图是什么”
- `CustomMapGenerationSet` 决定“用哪张图”
- `ZoneCore` 决定“地图部件怎么拼”
- `Entrance / Exit` 决定“怎么进出”
- `QuestNode / CQFAction` 决定“何时生成”

## 什么时候用哪种地图生成方式

### 用 QuestNode 生成地图

适合：
- 任务触发一张独立地图
- 世界地图地点生成
- 任务站点 / 副本 / 目标区域

优先入口：
- `QuestNode_Root_CustomMap`
- `QuestNode_RandomCustomMap`

### 用 CQFAction 生成子地图

适合：
- 玩家在已有地图中进入地下室、密室、设施内部
- 某个机关激活后生成新区域
- 通过入口对象打开新地图层

优先入口：
- `CQFAction_GenerateSubMap`
- `CustomMapEntrance`
- `CustomMapExit`

### 用 ZoneCore 做区域拼装

适合：
- 模块化房间
- 随机走廊
- 部件式设施
- 依赖连接规则的地图

优先入口：
- `ZoneCore`
- `ZoneCondition`
- `generationKey`
- `coreTags`

## 地图结构层

### CustomMapDataDef

本质：
- 一张 CQF 地图的定义对象

负责：
- 地图生成内容
- 地图尺寸或生成配置
- 区域与对象布置
- 与入口出口、地图部件的关系

适合：
- 整张独立地图
- 子地图模板
- 特殊剧情地图
- Boss 房模板
- 地下设施模板

AI 设计地图时，先明确：
- 这张图是固定模板还是候选模板之一
- 这张图是否要支持随机拼装
- 这张图是否有专属入口出口
- 这张图是否需要多阶段事件

### 生成前步骤与任务绑定

`CustomMapDataDef` 同时提供两个 `CustomMapStep` 列表：

- `preCustomSteps`：在结构内容生成前运行。
- `customSteps`：在结构内容生成后运行。

两者都直接使用 `CustomMapStep`，不要为生成前步骤建立独立基类。编辑器通过列表字段的泛型类型枚举所有 `CustomMapStep` 子类，因此现有步骤可以按需要配置在任一时机。

`GenStep_CustomMap.SpawnCustomMap` 的关键顺序：

1. 创建共享的 `CustomSitePartParams`，写入调用方传入的 `quest`、当前 `mapData` 和 `isSubMap`。
2. 依次执行 `preCustomSteps`。
3. 从共享参数重新读取 `quest`，再计算 `questId`。
4. 使用更新后的任务生成建筑、CustomThing、Pawn、Lord、区域、GenerationAction 和任务标签。
5. 生成主体结束后执行 `customSteps`。

需要让地图或结构在生成时创建新任务，并让本次生成内容绑定该任务时，使用：

```xml
<preCustomSteps>
  <li Class="QuestEditor_Library.CustomMapStep_StartQuest">
    <quest>MyMod_QuestScriptDef</quest>
    <sendAvailableLetter>true</sendAvailableLetter>
  </li>
</preCustomSteps>
```

`CustomMapStep_StartQuest` 使用当前地图的任务事件点数生成任务，并将结果写入共享的 `CustomSitePartParams.quest`。后续内容由现有生成流程统一获得 `Quest{id}` 标签和任务引用，不要在步骤中逐个补标签。

注意：

- `CustomMapStep_StartQuest` 必须放在 `preCustomSteps` 中才能绑定随后生成的内容。
- 如果放在 `customSteps`，任务仍会创建，但已经生成的内容不会反向绑定。
- 这种“结构生成主动创建任务”的需求应配置在 `CustomMapDataDef`，不要把具体任务硬编码进开局剧本项。
- 新增生成步骤时优先继续继承 `CustomMapStep`，通过放入不同列表选择运行时机。

### CustomMapGenerationSet

本质：
- 地图选择器

负责：
- 选择具体要生成哪张 `CustomMapDataDef`

适合：
- 有多个候选地图时
- 同类地点存在多个变体时
- 同一个入口可能连向多个房间时

使用原则：
- 只有一张图时，也可以用它做统一入口
- 多变体地图优先用它管理，而不是把逻辑散落在对象上

### MainMap / MainSite

本质：
- 主要地图是可重复进入的长期世界站点系统。
- 它不再保存完整运行时 `Map`，也不再提供休眠地图或超时重生成机制。
- 地图本体遵循 RimWorld 原版生命周期：进入时生成，离开并满足原版移除条件后销毁。

核心职责：
- `MainMapDef`：只保存候选地图生成配置。
- `MainMapAndCondition`：保存候选项名称、`CustomMapGenerationSet set` 和选择条件。
- `MainSite`：长期世界对象，保存 `mainMapDef`、重要 Pawn 缓存、离开/生成 tick、进入次数等长期状态。
- `MainMapWorldComponent`：索引当前世界中的 `MainSite`，按 `MainMapDef` 查询主要地图站点。
- `MainPawnSpawnData`：生成或复用重要 Pawn，保证重要角色身份可跨地图重新生成。
- `GenStep_MainMap`：主要地图专用 GenStep，负责按 `MainMapDef.maps` 选择候选地图并调用 CQF 地图生成。
- `QuestNode_Root_MainMap`：任务侧创建 `MainSite`，主要地图必定可重复进入。

候选地图规则：
- `MainMapDef.maps` 从上到下依次检查。
- 第一个条件通过的候选项会被选中。
- 不使用 priority、weight、fallback 字段。
- 最后一项应当不设条件，作为兜底候选。
- `MainMapAndCondition` 不直接保存 `CustomMapDataDef map`，只通过 `CustomMapGenerationSet set` 选择地图。

持久化原则：
- 不要尝试保存完整 `Map` 到 `WorldComponent`。
- 不要从 `Game.Maps` 手动移除地图来模拟休眠。
- 不要维护 dormant map、dormant pawn、`regenerateAfterTicks`、`keepMapDormant` 或 `maxDormantTicks`。
- 需要长期保留的内容应保存为 `MainSite` 状态、任务数据库状态、全局数据库状态或重要 Pawn 缓存。
- 重要 NPC 使用 `MainPawnSpawnData.dataName` 作为缓存 key，并优先从 `MainSite.mainPawns` 复用。
- 普通地图内容不保证运行时持久化；重新进入时按地图生成流程重新生成，再由长期状态决定哪些内容应出现或跳过。

编辑器与文本规则：
- Tip 应说明主要地图保存的是长期状态，不保存运行时地图对象。
- MainMap 候选项名字仅用于编辑器可读性，不应当作运行时状态。
- 文本必须走 Key + 双语翻译，不要硬编码中文显示文本。

使用建议：
- 适合长期剧情地点、可重复进入站点、重要 NPC 驻留点。
- 如果要保存“门已打开、奖励已领取、Boss 已死亡”等状态，应设计稳定 key 写入 `MainSite` 或数据库，而不是依赖旧地图仍存在。
- 如果需要短期完全保留玩家改造过的地图，不应使用当前 MainMap 机制；RimWorld 原版没有可靠的轻量 Map 休眠 API。

### ZoneCore

本质：
- 地图区域拼装节点

负责：
- 控制地图部件之间的拼装
- 控制连接方向
- 控制生成条件
- 控制生成 key 和限制
- 控制保留生成物

适合：
- 随机房间系统
- 走廊拼接
- 特定功能房间插入
- 区域限制生成
- 多模块地图

AI 使用时要重点考虑：
- 这个 `ZoneCore` 是入口核心、连接核心，还是终点核心
- 是否需要 `generationKey`
- 是否需要 `coreTags`
- 是否需要 `reserveThing`
- 是否要限制旋转、翻转或重复生成
- 是否存在 `disgenerate / disdestroy` 之类的生成控制位

## 出入口层

### CustomMapEntrance

本质：
- 地图进入点

负责：
- 把玩家从当前地图引向 CQF 地图或子地图
- 与出口建立关联
- 有时也承担激活地图的功能

适合：
- 入口门
- 地洞
- 电梯
- 传送门
- 设施入口

AI 使用时要明确：
- 入口是预先存在，还是交互后才激活
- 入口是否生成时就绑定子地图
- 是否需要 `CQFAction_LinkEntranceAndExit`
- 是否需要写入数据库以供后续使用

### CustomMapExit

本质：
- 地图离开点

负责：
- 把玩家送回父地图或离开当前区域
- 作为流程完成后的返回点

适合：
- 离开门
- 撤离点
- 电梯返回口
- 地图终点出口

设计原则：
- 入口出口成对设计更稳
- 若地图是多阶段结构，出口通常不一开始开放
- 出口激活往往由交互、清场、信号或任务阶段决定

## 地图中的自定义物品 — XML 字段速查

所有自定义物件放在 `<customThings>` 中，每个物件必须指定 `Class`。
基础字段（所有 CustomThingData 共有）：
```xml
<li Class="QuestEditor_Library.CustomThingData_XXX">
  <def>ThingDefName</def>              <!-- ThingDef.defName，必填 -->
  <stuff>Steel</stuff>                 <!-- ⚠️ 有 stuffCategories 时必须写，否则报错 -->
  <position>(x,0,z)</position>         <!-- IntVec3，必填 -->
  <rotation>(0,0,0)</rotation>         <!-- Rot4，默认 North -->
  <customName>CustomNameKey</customName>       <!-- 可选：显示文本走翻译 key -->
  <customDescription>CustomDescKey</customDescription>  <!-- 可选：显示文本走翻译 key -->
  <color>(1,1,1,1)</color>            <!-- 可选 -->
  <comps>...</comps>                  <!-- 可选：ActionComp 列表 -->
</li>
```

> ⚠️ **stuff 是必填陷阱**：CQF 预制建筑大多有 `stuffCategories`（QE_Cabinet/QE_Bookshelf/QE_Crate/QE_TreasureChest/QE_PressurePlate/QE_Sarcophagus 等），**全部需要写 `<stuff>`**。不写会报 `MakeThing error: ... is madeFromStuff but stuff=null`。只有 `QE_Flash`、`QE_SubMap_Burrow`、`QE_CustomMapEntrance` 等没有 stuffCategories 的才不需要。

### 自定义事物默认传递目标速查

自定义事物触发条件或动作时，会自动构造一组目标 key。写 `targetText` / `targetsText` 前，先判断目标是否已经由当前对象默认传入。

| 自定义事物 / 触发点 | 默认传递目标 | 含义 | 常见用途 | 注意 |
|---------------------|--------------|------|----------|------|
| `InteractableThing` 的 `InteractionOperation.conditions` | `Trigger`、`CustomThing` | `Trigger` 是交互 Pawn；`CustomThing` 是当前可交互事物 | 检查技能、背包、状态、站位；记录或操作当前交互物 | 条件和结果动作使用同一类上下文 |
| `InteractableThing` 的 `InteractionResult.conditions/actions` | `Trigger`、`CustomThing` | `Trigger` 是交互 Pawn；`CustomThing` 是当前可交互事物 | 发消息、发信号、消耗物品、生成奖励、记录交互物 | 不要把 `interactionText` 或 `resultName` 当目标 key |
| `LootBox` 的 `Open` 组件动作 | `CustomThing` | 当前打开的战利品箱 | 打开后发信号、销毁、记录箱子 | 开箱生成的 loot 不是自动 `Inner` 目标；`Inner` 主要用于容器 |
| `CustomTrap` 的 `StepOn` | `CustomThing`、`Trigger` | `CustomThing` 是陷阱自身；`Trigger` 是踩中的 Pawn | 爆炸、伤害触发者、记录触发者、陷阱自毁 | 爆炸通常不写 `targetsText`；自毁用 `CustomThing` |
| `CustomTrap` 的 `Signal/Tick/Damaged` | `CustomThing` | 当前陷阱自身 | 定时机关、信号机关、受击触发机关 | 没有 Pawn 触发者时不要引用 `Trigger` |
| `CustomDoor.openingConditions` | `Trigger`、`CustomThing` | `Trigger` 是尝试开门的 Pawn；`CustomThing` 是门 | 门禁判断、钥匙检查、技能检查 | 只有条件阶段有 `Trigger` |
| `CustomDoor.openingActions` | `CustomThing` | 当前门 | 开门后发信号、记录门、触发刷怪 | 开门动作里默认没有 `Trigger`，需要 Pawn 时先在条件或外部逻辑记录 |
| `CustomContainer.openingConditions` | `CustomThing`、`Inner` | `CustomThing` 是容器；`Inner` 是容器内对象 | 检查容器是否有内容、检查被捕获 Pawn | `Inner` 可能为空，条件要考虑空容器 |
| `CustomContainer.openingActions` | `CustomThing`、`Inner` | `CustomThing` 是容器；`Inner` 是打开前取出的内部对象 | 释放对象、记录内部 Pawn、发信号 | `Inner` 是打开前缓存的内容，不是新生成目标 |
| `CompActionWorker` 的 `Spawn/Tick/Signal/Damaged/Open` | `CustomThing` | 组件所在的 parent Thing | 通用触发器、信号联动、定时动作 | 默认没有 `Trigger`；挂在箱子 Open 后也是组件 parent |
| `CompActionWorker` 的 `Destroy` | `CustomThing` | 被销毁对象原位置的 `TargetInfo` | 死亡/摧毁后在原位置生成、发信号 | 此时 `CustomThing` 更像位置目标，不再是可操作 Thing 实体 |
| `GenerationActionWorker` | `Position` | 地图生成时该 worker 的位置 | 地图生成后在指定位置执行动作 | 用于生成阶段；不是普通 `customThings` 交互对象 |
| `FinishRect` 的矩形内动作 | `Position` | 当前遍历到的矩形格子 | 批量地格处理、批量刷雾、批量生成 | `Position` 会随每个格子变化 |
| `DoActionForGroup` 子动作 | `Target` | 当前群组成员 | 批量处理 Pawn 或对象 | 子动作中优先用 `Target`，不要误用 `Trigger` |

默认判断规则：
- 当前对象默认已经传入的 key，不需要再 `RecordToDatabase`
- 跨阶段、延迟、信号后仍要用的对象，才记录到数据库
- 引用 `Trigger` 前先确认触发场景真的有 Pawn 触发者
- 引用 `Inner` 前先确认对象类型会传入内部对象
- 销毁当前自定义事物通常使用 `CustomThing`

### InteractableThing (CustomThingData_InteractableThing)

玩家主动点击交互的对象。

```xml
<li Class="QuestEditor_Library.CustomThingData_InteractableThing">
  <def>QE_Flash</def>                  <!-- InteractableThing 或子类 ThingDef -->
  <position>(15,0,8)</position>
  <customName>TerminalNameKey</customName>
  <customDescription>TerminalDescKey</customDescription>
  <operations>
    <li Class="QuestEditor_Library.InteractionOperation">
      <interactionText>InteractKey</interactionText>  <!-- 按钮文本 key -->
      <tickToOperate>200</tickToOperate>               <!-- int 耗时 -->
      <onlyGenerateSingleResult>false</onlyGenerateSingleResult>
      <requiredThings>...</requiredThings>
      <conditions>...</conditions>
      <results>
        <li Class="QuestEditor_Library.InteractionResult">
          <resultName>ResultName</resultName>
          <conditions>...</conditions>
          <actions>
            <li Class="QuestEditor_Library.CQFAction_SentSignal">
              <signal>MySignal</signal>
              <addQuestPrefix>true</addQuestPrefix>
            </li>
          </actions>
        </li>
      </results>
    </li>
  </operations>
  <operationDefs>...</operationDefs>    <!-- 可选：复用 InteractionDataDef -->
</li>
```

默认传递目标：
- `Trigger`：交互者 Pawn
- `CustomThing`：当前可交互事物

使用要点：
- `InteractionOperation.conditions` 和 `InteractionResult.conditions/actions` 都使用这两个默认目标
- 技能、背包、Hediff、Trait 等 Pawn 条件通常检查 `Trigger`
- 记录、销毁或替换当前交互物通常使用 `CustomThing`

### LootBox (CustomThingData_LootBox)

打开时抽奖的奖励箱。

```xml
<li Class="QuestEditor_Library.CustomThingData_LootBox">
  <def>QE_Cabinet</def>                <!-- LootBox 子类 ThingDef -->
  <stuff>Steel</stuff>                 <!-- ⚠️ 必须写 -->
  <position>(23,0,11)</position>
  <lootBoxName>Reward01</lootBoxName>   <!-- 名字，用于自动信号 -->
  <tickToOpen>120</tickToOpen>
  <openReport>Opening</openReport>
  <destroyAfterOpening>true</destroyAfterOpening>
  <openWhenDestroyed>true</openWhenDestroyed>
  <lootDef>LootDataDefName</lootDef>    <!-- 可选：复用模板 -->
  <loots>
    <li Class="QuestEditor_Library.LootData">
      <dataName>Pool1</dataName>
      <chance>0.6</chance>
      <message>LootFoundMessageKey</message>
      <things>
        <li Class="QuestEditor_Library.CQFThingDefCount">
          <thing>Steel</thing>
          <count>40~60</count>
        </li>
      </things>
      <categorys>...</categorys>
      <specialThingDatas>...</specialThingDatas>
      <pawnDatas>...</pawnDatas>
    </li>
  </loots>
</li>
```

自动信号：打开时发出 `Quest{id}.{lootBoxName}`

默认传递目标：
- `CustomThing`：当前打开的战利品箱

使用要点：
- `Open` 类型的 `CompActionWorker` 动作会收到 `CustomThing`
- 开箱生成的战利品不会自动作为 `Inner` 传入
- 需要后续引用箱子时，可记录 `CustomThing`
- 需要引用开箱者 Pawn 时，不要假设默认有 `Trigger`

### CustomTrap (CustomThingData_CustomTrap)

被动触发机关。

```xml
<li Class="QuestEditor_Library.CustomThingData_CustomTrap">
  <def>QE_PressurePlate</def>
  <stuff>Steel</stuff>                 <!-- ⚠️ 有 stuffCategories 时必须写 -->
  <position>(28,0,22)</position>
  <trapName>MyTrap</trapName>
  <trapComps>
    <li>
      <mode>StepOn</mode>              <!-- StepOn/Signal/Tick/Damaged -->
      <inSignal>SomeSignal</inSignal>
      <signalIsOnlyValidInPart>false</signalIsOnlyValidInPart>
      <tick>60000</tick>
      <triggerWhenDamaged>false</triggerWhenDamaged>
      <actions>
        <li Class="QuestEditor_Library.CQFAction_Explosion">
          <radius>2.9</radius>
          <amount>30</amount>
          <damage>Bomb</damage>
        </li>
      </actions>
    </li>
  </trapComps>
</li>
```

默认传递目标：
- `CustomThing`：当前陷阱自身
- `Trigger`：踩中陷阱的 Pawn，仅 `StepOn` 且存在 Pawn 时传入

使用要点：
- `StepOn` 陷阱可用 `Trigger` 伤害、记录或检查触发 Pawn
- `Signal`、`Tick`、`Damaged` 触发通常只有 `CustomThing`，不要直接引用 `Trigger`
- 陷阱自毁使用 `CustomThing`

#### 陷阱是否触发后销毁

使用 `CustomTrap` 时必须先判断它是不是一次性机关，不要默认所有陷阱都保留，也不要默认所有陷阱都销毁。

通常规则：
- 压力板、一次性地雷、机关爆点：触发后通常销毁自身，避免重复爆炸或重复刷消息
- 可重复警报、周期性毒气、常驻区域效果：通常不销毁
- 已解除的压力板或机关如果仍会触发提示，可在提示后销毁自身，避免重复提示

动作顺序建议：
- `Message -> Explosion -> Destory`
- 爆炸动作通常不写 `targetsText`，让它使用陷阱触发时的当前上下文
- 销毁自身时，`CQFAction_Destory` 必须明确指向 `CustomThing`

```xml
<li Class="QuestEditor_Library.CQFAction_Message">
  <message>TrapTriggered</message>
  <type>ThreatSmall</type>
</li>
<li Class="QuestEditor_Library.CQFAction_Explosion">
  <radius>2.5</radius>
  <amount>28</amount>
  <damage>Bomb</damage>
</li>
<li Class="QuestEditor_Library.CQFAction_Destory">
  <targetsText>
    <li>CustomThing</li>
  </targetsText>
</li>
```

### Spawner (CustomThingData)

Pawn 生成点。数据存在 `CustomMapDataDef.pawns` 字典中，不在物件 XML 里。
```xml
<li Class="QuestEditor_Library.CustomThingData">
  <def>QE_Spawner_Editor</def>
  <position>(15,0,10)</position>
</li>
```

### CustomMapEntrance (CustomThingData_CustomMapEntrance)

子地图入口。

```xml
<li Class="QuestEditor_Library.CustomThingData_CustomMapEntrance">
  <def>QE_SubMap_Burrow</def>          <!-- CustomMapEntrance 子类 -->
  <position>(23,0,3)</position>
  <exitName>MyExit</exitName>           <!-- 与出口关联 -->
  <opended>true</opended>              <!-- 初始是否开启 -->
  <data>SK_Ruins</data>                <!-- 直接指定地图 -->
  <tagWithChance>                      <!-- 按标签随机选 -->
    <li><tag>Ruins</tag><chance>1</chance></li>
  </tagWithChance>
  <mapDefWithChance>                   <!-- 按定义随机选 -->
    <li><def>SK_Ruins</def><chance>1</chance></li>
  </mapDefWithChance>
</li>
```

- `QE_CustomMapEntrance` 用 `<data>` 直接指定
- `QE_CustomMapEntrance_Chance` 用 `<tagWithChance>`/`<mapDefWithChance>` 随机选

默认传递目标：
- 入口自身没有专属 `openingActions`；若给入口挂 `CompActionWorker`，按组件规则传入 `CustomThing`
- 通过 `CQFAction_ActivateCustomMap`、`CQFAction_SwtichEntranceStatus` 等动作操作入口时，通常需要在动作的 `targetsText` 中指定入口 key

使用要点：
- 入口要跨对象联动时，先把入口记录到数据库，再由信号或动作引用
- `LinkEntranceAndExit` 使用 `entranceText` / `exitText` 指向已记录的入口与出口

### CustomMapExit (CustomThingData_CustomMapExit)

子地图出口。

```xml
<li Class="QuestEditor_Library.CustomThingData_CustomMapExit">
  <def>QE_Exit</def>
  <position>(23,0,42)</position>
  <exitName>MyExit</exitName>           <!-- 与入口关联 -->
  <comps>
    <li Class="QuestEditor_Library.ActionComp">
      <compName>EnableOnSignal</compName>
      <mode>Signal</mode>
      <signal>CompleteSignal</signal>
    </li>
  </comps>
</li>
```

出口默认开放，挂 `<comps>+Signal` 可让出口初始关闭、信号激活。

默认传递目标：
- 出口自身没有专属 `openingActions`；若给出口挂 `CompActionWorker`，按组件规则传入 `CustomThing`
- 被其他动作操作时，通常需要先记录出口 key，再通过 `targetsText` 或专用字段引用

使用要点：
- 出口和入口联动时优先记录 `Entrance` / `Exit` 两个稳定 key
- 出口激活一般用信号或组件，不要假设出口移动 Pawn 时会给动作链传入 `Trigger`

### CustomDoor (CustomThingData_CustomDoor)

可设开门条件和动作的门。

```xml
<li Class="QuestEditor_Library.CustomThingData_CustomDoor">
  <def>CQF_CustomDoor</def>
  <stuff>Steel</stuff>
  <position>(23,0,17)</position>
  <openingConditions>...</openingConditions>
  <openingActions>...</openingActions>
</li>
```

默认传递目标：
- `openingConditions`：`Trigger`、`CustomThing`
- `openingActions`：`CustomThing`

使用要点：
- 开门条件里 `Trigger` 是尝试开门的 Pawn，可用于钥匙、技能、身份检查
- 开门动作里默认只有 `CustomThing`，也就是门自身
- 如果开门后动作需要知道是谁开的门，需要提前用其他流程记录 Pawn，不能在 `openingActions` 里直接假设有 `Trigger`

### CustomContainer (CustomThingData_CustomContainer)

容纳 Pawn 或物品的容器。

```xml
<li Class="QuestEditor_Library.CustomThingData_CustomContainer">
  <def>QE_Cage</def>
  <stuff>Steel</stuff>
  <position>(23,0,17)</position>
  <tickToOpen>150</tickToOpen>
  <openingConditions>...</openingConditions>
  <openingActions>...</openingActions>
  <innerThings>                        <!-- 内部物品 LootData -->
    <li Class="QuestEditor_Library.LootData">
      <dataName>Inside</dataName>
      <chance>1</chance>
      <things>...</things>
    </li>
  </innerThings>
</li>
```

默认传递目标：
- `CustomThing`：当前容器
- `Inner`：容器内对象，打开时会先缓存打开前的 `ContainedThing`

使用要点：
- `openingConditions` 和 `openingActions` 都可使用 `CustomThing`、`Inner`
- `Inner` 适合用于检查、释放、记录被关押 Pawn 或内部物品
- `Inner` 可能为空，空容器逻辑要用条件保护
- `openingActions` 里的 `Inner` 是打开前缓存的内部对象，不是打开后新生成的对象

### ZoneCore (CustomThingData_ZoneCore)

区域拼装核心。数据在 `CustomMapDataDef.zoneCores` 中。
```xml
<li Class="QuestEditor_Library.CustomThingData">
  <def>QE_ZoneCore</def>
  <position>(23,0,23)</position>
</li>
```

默认传递目标：
- `ZoneCore` 本身主要服务于地图拼装筛选，不是常规交互动作执行器
- 若在区域生成、生成后执行或组件动作里需要引用某个核心，通常应显式记录数据库 key

使用要点：
- 不要假设 `ZoneCore` 会像交互物一样提供 `Trigger`
- 需要跨区域联动时，优先用 `generationKey`、信号和数据库记录管理

### CompActionWorker — 通用事件组件

挂在物件 `<comps>` 下，不是独立条目。

```xml
<li Class="QuestEditor_Library.ActionComp">
  <compName>MyComp</compName>
  <mode>Signal</mode>                  <!-- Signal/Tick/Damaged/Destroy/Spawn/MapGeneration/Open -->
  <signal>SomeSignal</signal>
  <signalIsOnlyValidInPart>false</signalIsOnlyValidInPart>
  <tick>2500</tick>
  <actions>
    <li Class="QuestEditor_Library.CQFAction_Message">
      <message>ActionTriggeredMessageKey</message>
    </li>
  </actions>
</li>
```

默认传递目标：
- `Spawn`、`Tick`、`Signal`、`Damaged`、`Open`：`CustomThing` 是组件所在的 parent Thing
- `Destroy`：`CustomThing` 是被销毁对象原位置的 `TargetInfo`

使用要点：
- 组件动作默认没有 `Trigger`
- `Destroy` 触发时对象可能已经不可作为实体操作，应把 `CustomThing` 当原位置使用
- 想在组件动作中操作其他对象，应先用数据库记录目标 key
- `Open` 触发常见于战利品箱打开后，默认目标仍是箱子本体

### ThingData — 普通物品的批量存储

普通物品（不是 CQF 自定义物件）存放在 `CustomMapDataDef.thingDatas` 中。

ThingData 有三种位置字段，分别对应不同场景：

| 字段 | 用途 | 手动编写 |
|------|------|----------|
| `position` | 单物品坐标。编辑器保存时，如果只有一个物品则用此字段 | ✅ 默认用这个 |
| `allRect` | 批量合并坐标（CellRect 列表）。编辑器保存时，如果发现多个完全相同的物品（def/stuff/颜色/品质等一致），会合并成一个 ThingData + allRect | ⚠️ 非必要不使用，优先用 position 逐个写 |
| `allPositions` | **内部临时缓存**。在编辑器收集过程中暂存重复物品的位置，保存前转成 allRect 后清空。**不进 XML** | ❌ 永远不要手动写 |

```xml
<li Class="QuestEditor_Library.ThingData">
  <def>Steel</def>                      <!-- ThingDef.defName -->
  <stuff>Steel</stuff>                 <!-- 有 stuff 时可选 -->
  <position>(0,0,0)</position>         <!-- 单物品时使用 -->
  <count>50</count>                    <!-- 堆叠数 -->
  <hitPoint>100</hitPoint>
  <quality>Normal</quality>
  <color>(1,1,1,1)</color>
</li>
```

**保存时的合并流程**：
1. 编辑器从地图收集物品时，同类物品（`Equals_Def` 完全匹配）的第一个创建 ThingData 并设置 `position`，后续相同物品的坐标追加到 `allPositions`
2. 收集完毕后，如果有 `allPositions`（即有重复物品），则将 `position` 也并入 `allPositions`，然后整体转为 `allRect`，再将 `allPositions` 清空，`position` 置零
3. 最终写入 XML 时：有 `allRect` 则写 `allRect`，没有则写 `position`

**生成时的读取顺序**（`GenStep_CustomMap.SpawnThings`）：
1. 有 `allPositions` → 用它（兼容旧数据或手动写入的脏数据）
2. 没有 `allPositions`，有 `allRect` → 展开所有格子
3. 两者都没有 → 用 `position`（单物品回退）

---

## 地图中的行为系统

地图里常用的动作，应按用途来理解，而不是按类名死记。

### 基础认知

- `CQFAction` 负责执行副作用
- `CQFAction_Target` 先根据 `targetsText` 取目标，再对每个目标执行动作
- 大多数地图动作都依赖正确的目标 key 和数据库记录

常见目标 key：
- `CustomThing`
- `Trigger`
- `Position`
- `Inner`
- `Target`

### CQFAction_Sequence

作用：
- 顺序执行一串动作

关键参数：
- `actions`

适合：
- 标准事件链
- 固定推进流程

典型组合：
- `Message -> RecordToDatabase -> SentSignal`

### CQFAction_Random

作用：
- 从动作列表里随机执行一个

关键参数：
- `actions`

适合：
- 随机房间奖励
- 随机陷阱结果
- 随机剧情分支

### CQFAction_Condition

作用：
- 条件全满足时才执行动作列表

关键参数：
- `conditions`
- `actions`

适合：
- 动作层分支
- 开门前再做一次安全校验
- 根据当前状态决定是否生成下一阶段

### CQFAction_Chance

作用：
- 按概率执行一个动作

关键参数：
- `action`
- `chance`

适合：
- 稀有掉落
- 机关额外效果
- 偶发刷怪

### CQFAction_SentSignal

作用：
- 发送一个信号

关键参数：
- `signal`
- 发送的信号名
- `signalIsOnlyValidInPart`
- 是否只在当前地图部件内有效
- `addQuestPrefix`
- 是否自动加 `Quest{id}.` 前缀

适合：
- 推进地图阶段
- 打开门
- 激活出口
- 通知任务节点

典型组合：
- `InteractableThing -> SentSignal`
- `LootBox(Open) -> SentSignal`
- `Trap -> SentSignal`

常见坑：
- 发了信号，但没人监听
- 忘了 `addQuestPrefix`，导致任务侧收不到

### CQFAction_Message

作用：
- 给玩家显示消息

关键参数：
- `message`
- 消息文本 key 或文本
- `type`
- 消息类型

适合：
- 机关提示
- 成功反馈
- 失败提示
- 剧情说明

注意：
- 消息文本应走翻译，不要硬编码中文显示文本

### CQFAction_RecordToDatabase

作用：
- 把目标写入数据库

关键参数：
- `targetsText`
- 要记录哪些目标 key
- `recordKey`
- 存入数据库时使用的 key
- `recordToTemporaryBase`
- 是否写入临时数据库
- `recordToQuestBase`
- 是否写入任务数据库
- `recordToGlobalBase`
- 是否写入全局数据库

适合：
- 记录入口
- 记录出口
- 记录钥匙
- 记录 Boss
- 记录目标点
- 记录关键交互对象

典型组合：
- `GetThingToRecord -> RecordToDatabase`
- `GetCellToRecord -> RecordToDatabase`
- `RecordToDatabase -> DatabaseExists`

常见坑：
- 忘了选数据库类型
- 后续引用的是 `recordKey`，不是原始 `targetsText`

### CQFAction_GetThingToRecord

作用：
- 从给定目标位置上取到 Thing，再按记录逻辑写数据库

继承自：
- `CQFAction_RecordToDatabase`

额外逻辑：
- 会把当前位置的对象取出来，而不是直接记录原目标

适合：
- 记住当前位置上的门
- 记住生成点上的对象
- 记住交互后新出现的实体

### CQFAction_GetCellToRecord

作用：
- 把给定目标转成位置，再按记录逻辑写数据库

继承自：
- `CQFAction_RecordToDatabase`

适合：
- 记住站位点
- 记住触发点
- 记住刷怪位置

### CQFAction_Spawn

作用：
- 用 `LootData` 在目标位置生成内容

关键参数：
- `targetsText`
- 在哪些目标位置生成
- `datas`
- 生成模板列表

适合：
- 奖励生成
- 战利品生成
- 场景刷物

### CQFAction_SpawnCustomThing

作用：
- 在目标位置生成 CQF 自定义物件

适合：
- 动态刷门
- 动态刷箱子
- 动态刷陷阱
- 动态刷入口出口

使用原则：
- 需要 CQF 特殊对象时优先用它
- 需要普通资源或掉落时优先用 `Spawn`

### CQFAction_GenerateSubMap

作用：
- 在目标所在地图上生成子地图

关键参数：
- `targetsText`
- 从哪些目标处取地图上下文
- `pos`
- 子地图起始位置
- `set`
- `CustomMapGenerationSet`

适合：
- 地下室
- 隐藏房
- 设施内部
- 电梯下层

典型组合：
- `InteractableThing -> GenerateSubMap`
- `Entrance -> GenerateSubMap -> LinkEntranceAndExit`

### CQFAction_OpenLootBox

作用：
- 强制打开目标箱子

关键参数：
- `targetsText`

适合：
- 连锁开箱
- 打开主箱后同步开副箱
- 信号触发奖励

### CQFAction_SetDuty

作用：
- 设置目标 Pawn 的 Duty

关键参数：
- `targetsText`
- `duty`

适合：
- 进入房间后让敌人驻守
- 激活警报后切换巡逻
- 对话后改变 NPC 行为

### CQFAction_Hediff

作用：
- 给目标 Pawn 添加或设置 Hediff

关键参数：
- `targetsText`
- `hediff`
- `severity`
- `bodyPart`
- `customLabel`

适合：
- 毒气房
- 辐射区
- 中毒陷阱
- 剧情感染

### CQFAction_RecordStartCell

作用：
- 记录矩形区域起点

关键参数：
- `targetsText`
- `recordKey`

适合：
- 准备做范围刷地板
- 准备做矩形区域机关

### CQFAction_FinishRect

作用：
- 结合前面记录的起点，形成矩形区域，对矩形内所有格执行动作

关键参数：
- `targetsText`
- `recordKey`
- `actions`

适合：
- 整个房间刷雾
- 整个房间刷火
- 整个房间批量生成内容
- 批量处理区域地格

### 地图里高频行为默认优先级

设计地图时，优先考虑这些动作：
- `RecordToDatabase`
- `SentSignal`
- `Sequence`
- `Condition`
- `Spawn`
- `SpawnCustomThing`
- `GenerateSubMap`
- `OpenLootBox`
- `RecordStartCell`
- `FinishRect`

## 地图中的条件系统

地图里最常用的条件，不是所有条件都一样重要。AI 设计地图时，优先从以下类型思考。

### 基础认知

- `DialogCondition` 只负责判定，不直接做事
- `DialogCondition_Target` 依赖 `targetText` 指向某个目标
- `DialogCondition_Target_Pawn` 先要求目标必须是 Pawn

### DialogCondition_Bool

作用：
- 检查全局或任务布尔值

关键参数：
- `boolName`

适合：
- 地图阶段门
- 是否已解锁
- 是否已读日志
- 是否已激活控制台

### DialogCondition_DatabaseExists

作用：
- 检查数据库里是否已存在某个 key

关键参数：
- `targetKey`
- 数据库里的 key
- `needSpawned`
- 是否要求对象仍然已生成
- `checkGlobalDatabase`
- 是否检查全局数据库
- `checkTemporaryDatabase`
- 是否检查临时数据库
- `checkQuestDatabase`
- 是否检查任务数据库

适合：
- 检查钥匙是否已记录
- 检查出口是否已生成
- 检查 Boss 是否已记住
- 检查某阶段对象是否存在

典型组合：
- `RecordToDatabase -> DatabaseExists`

### DialogCondition_GroupExists

作用：
- 检查目标是否属于某个任务 group

关键参数：
- `targetText`
- `targetKey`
- `needSpawned`

适合：
- 多目标任务
- 多波敌人
- 房间组判断

### DialogCondition_QuestState

作用：
- 检查某个任务当前状态

关键参数：
- `quest`
- `state`

适合：
- 任务地图阶段门
- 特定任务成功后开放区域

### DialogCondition_ThingInPosition

作用：
- 检查目标对象是否位于某个位置

关键参数：
- `targetText`
- 被检查的目标
- `positionName`
- 位置 key

适合：
- 站位机关
- 把物体放到指定格
- 运输类谜题

### DialogCondition_Skill

作用：
- 固定技能门槛

关键参数：
- `targetText`
- `skill`
- `level`
- `needToBeGreater`

适合：
- 开锁
- 挖掘
- 分析终端
- 拆除设备

### DialogCondition_SkillCheck

作用：
- 概率型技能检定

关键参数：
- `targetText`
- `skill`
- `checkModifier`

适合：
- 破解
- 说服
- 临时维修
- 风险交互

### DialogCondition_Hediff

作用：
- 检查 Pawn 是否带某个 Hediff，及其严重度

关键参数：
- `targetText`
- `hediff`
- `severity`
- `needToBeGreater`

适合：
- 感染区
- 中毒区
- 状态门

### DialogCondition_Trait

作用：
- 检查 Pawn Trait

关键参数：
- `targetText`
- `trait`
- `degree`
- `needToBeGreater`
- `accurate`

适合：
- 角色专属解法
- 剧情特殊分支

### DialogCondition_Inventory

作用：
- 检查 Pawn 背包物品

关键参数：
- `targetText`
- `requirations`

适合：
- 门禁卡
- 研究样本
- 燃料电池
- 祭品类交互

### DialogCondition_ContainerIsFull

作用：
- 检查容器里是否有内容

关键参数：
- `targetText`

适合：
- 检查容器是否已装填
- 检查储物仓 / 俘虏仓状态

### DialogCondition_CapturedPawn

作用：
- 检查捕获型对象中是否有 Pawn

关键参数：
- `targetText`

适合：
- 捕获机关后续流程
- 救援 / 处决 / 释放类剧情

### DialogCondition_And

作用：
- 多个条件都满足时通过

关键参数：
- `condition`

适合：
- 高级门禁
- 多前置要求

### DialogCondition_Or

作用：
- 任一条件满足时通过

关键参数：
- `condition`

适合：
- 多解法机关
- 任一钥匙可开门

### DialogCondition_Reversal

作用：
- 反转一个条件

关键参数：
- `condition`

适合：
- “未完成时才允许”
- “未拥有时才显示”

### ⚠️ 条件字段名陷阱 — 写错直接报错

条件类 XML 的节点**必须与 C# 字段名完全一致**，差一个字母或多一个 `s` 就会报 `doesn't correspond to any field`。

| 所在类 | C# 字段名 | XML 节点名 | 常见错误 |
|--------|-----------|------------|----------|
| `DialogCondition_And` | `condition` | `<condition>` | ❌ 写成 `<conditions>`（多 s） |
| `DialogCondition_Or` | `condition` | `<condition>` | ❌ 写成 `<conditions>`（多 s） |
| `DialogCondition_Reversal` | `condition` | `<condition>` | ✅ 容易写对 |
| `InteractionOperation` | `conditions` | `<conditions>` | ✅ 本就带 s |
| `InteractionResult` | `conditions` | `<conditions>` | ✅ 本就带 s |
| `CustomDoor` | `openingConditions` | `<openingConditions>` | ❌ 简写成 `<conditions>` |
| `CustomContainer` | `openingConditions` | `<openingConditions>` | ❌ 简写成 `<conditions>` |

**核心规则**：XML 节点名 = C# 字段名。写 XML 时先看一眼源码的字段名，不要凭语义猜测单复数。

正确写法对比：
```xml
<!-- ✅ DialogCondition_And 内嵌条件用 <condition>（单数，字段名就是 condition） -->
<li Class="QuestEditor_Library.DialogCondition_And">
  <condition>
    <li Class="QuestEditor_Library.DialogCondition_Bool">
      <boolName>DoorUnlock</boolName>
    </li>
    <li Class="QuestEditor_Library.DialogCondition_DatabaseExists">
      <targetKey>KeyCard</targetKey>
      <checkQuestDatabase>true</checkQuestDatabase>
    </li>
  </condition>
</li>

<!-- ✅ InteractionOperation 的条件列表用 <conditions>（复数，字段名就是 conditions） -->
<li Class="QuestEditor_Library.InteractionOperation">
  <interactionText>OpenDoor</interactionText>
  <conditions>
    <li Class="QuestEditor_Library.DialogCondition_Bool">
      <boolName>DoorUnlock</boolName>
    </li>
  </conditions>
  ...
</li>
```

### 地图里高频条件默认优先级

设计地图时，优先考虑这些条件：
- `Bool`
- `DatabaseExists`
- `Inventory`
- `Skill`
- `SkillCheck`
- `ThingInPosition`
- `And`
- `Or`
- `Reversal`

## 地图中的对话与交互

CQF 地图中的对话，不应该被理解成单独的“文本系统”。
它本质上是地图推进系统的一部分。

地图中的对话常见用途：
- 开始任务说明
- 给予线索
- 判断是否允许继续深入
- 给玩家选择分支
- 决定门是否解锁
- 决定是否刷敌人
- 决定奖励或惩罚

地图对话的典型结构：
- 玩家接触 NPC 或交互对象
- 通过 `DialogCondition` 检查资格
- 通过 `InteractionResult` 或 `CQFAction` 执行结果
- 记录关键对象
- 发信号推进阶段

设计原则：
- 地图对话应服务于地图流程
- 不要把对话写成孤立文本
- 每个关键对话都要明确：
- 前置条件
- 命中结果
- 后续动作
- 是否需要数据库记录
- 是否需要信号

## 地图中的数据库与信号

地图制作中，这一层极其关键。

### 为什么要记录数据库

因为地图流程经常需要记住：
- 入口对象
- 出口对象
- 门禁对象
- 钥匙对象
- Boss
- 机关位置
- 目标位置
- 已触发事件
- 已生成房间

如果不记录，后续动作经常拿不到对象。

### 三类数据库怎么选

- `temporary database`
- 用于当前交互、当前开箱、当前生成流程
- 不适合长期状态

- `quest database`
- 用于当前任务地图流程
- 最适合地图阶段推进

- `global database`
- 用于跨任务长期保存
- 只在确实需要全局状态时使用

### 信号怎么用

信号适合做：
- 开门
- 开启下一波
- 激活出口
- 地图阶段推进
- 对象联动
- 任务通知

设计建议：
- 每个阶段只设置少量清晰信号
- 先想清楚谁发信号
- 再想清楚谁接收信号
- 需要跨阶段引用的对象先记录数据库

## 地图制作配方

### 配方 1：废墟探索图

组成：
- 入口
- 多个 `InteractableThing`
- 少量 `LootBox`
- 若干 `CustomTrap`
- 一个最终奖励点
- 若干消息与信号

流程：
1. 进入地图
2. 调查线索点
3. 触发若干机关或陷阱
4. 解锁最终房间
5. 开箱获得奖励
6. 离开

### 配方 2：钥匙门禁图

组成：
- 入口
- 锁门
- 钥匙卡交互点或掉落点
- 条件检查
- 门解锁动作
- 出口

流程：
1. 玩家找到钥匙卡
2. 记录到数据库或背包
3. 门的交互使用 `Inventory` 或 `DatabaseExists` 检查
4. 满足后发信号开门
5. 进入后续区域

### 配方 3：对话推进图

组成：
- NPC 或终端交互
- 多个条件分支
- 若干结果动作
- 阶段布尔值
- 出口控制

流程：
1. 通过对话得知目标
2. 满足条件后开放下一阶段
3. 某次对话结果激活门、刷怪或奖励
4. 最后解锁出口

### 配方 4：陷阱机关图

组成：
- `CustomTrap`
- 站位条件
- 雾区或污染区
- 警告消息
- 奖惩点

流程：
1. 玩家进入区域
2. 踩中或触发陷阱
3. 执行动作链
4. 产生爆炸、毒气、刷怪或封门
5. 玩家找到解除机关的方法

### 配方 5：子地图设施图

组成：
- `CustomMapEntrance`
- `CustomMapGenerationSet`
- `CQFAction_GenerateSubMap`
- `CustomMapExit`
- 内部交互对象
- 结尾奖励

流程：
1. 玩家从大地图进入设施
2. 生成设施子地图
3. 在子地图内完成目标
4. 激活出口
5. 返回父地图

### 配方 6：模块化设施图

组成：
- `ZoneCore`
- 多个房间模块
- 若干随机事件房
- 若干连接模块
- 终点核心区

流程：
1. 用 `ZoneCore` 控制模块拼装
2. 用 `generationKey` 控制唯一房间
3. 用 `coreTags` 控制房间类别
4. 把奖励房、战斗房、剧情房混合插入
5. 终点房触发结算

## AI 生成地图时的默认决策规则

当用户只说“做一张 CQF 地图”而没说细节时，默认：

1. 先提出方案，再动手
2. 默认做一张：
- 有入口
- 有出口
- 有至少一个交互点
- 有至少一个奖励点
- 有至少一个推进条件
- 有至少一个阶段信号

3. 如果地图需要玩法，优先加入：
- `InteractableThing`
- `LootBox`
- `CustomTrap`

4. 如果地图需要推进，优先加入：
- `RecordToDatabase`
- `SentSignal`
- `DatabaseExists`
- `Bool`

5. 如果地图需要重复利用，优先把：
- 掉落抽到 `LootDataDef`
- 交互抽到 `InteractionDataDef`
- 地图候选抽到 `CustomMapGenerationSet`

6. 默认补双语文本，不硬编码中文显示文本

## 常见错误

- 只做地图结构，不做流程
- 只放对象，不接条件和动作
- 不记录关键对象，后续动作无法引用
- 发了信号，但没有对象监听
- 做了出口，但没有激活条件
- 交互完成后没有推进下一阶段
- 把临时对象写进错误数据库
- 把被动机关写成交互对象
- 把主动交互写成陷阱
- 忘记文本双语与 Key 规则

### `stuff=null` 错误（MadeFromStuff 对象必须传材质）

有 `stuffCategories` 的 ThingDef 是 MadeFromStuff 类型，在 `customThings` 中必须指定 `<stuff>`，否则报 `MakeThing error: XXX is madeFromStuff but stuff=null`。

**怎么判断一个 def 是否需要 `<stuff>`**：XML 定义里有 `<stuffCategories>` 标签的就是 MadeFromStuff。常见需要 `<stuff>` 的 CQF 物件：

| defName | 建议材质 |
|---------|---------|
| `QE_Bookshelf` | WoodLog / Steel / Marble |
| `QE_Cabinet` | WoodLog / Steel / Marble |
| `QE_TreasureChest` | WoodLog / Steel / Marble |
| `QE_Crate` / `QE_SomeCrate` | WoodLog / Steel |
| `QE_CellarDoor` | Steel / WoodLog / Marble |
| `QE_Labber` / `QE_LadderDown` | Steel |
| `QE_Sarcophagus` | Marble |

不需要 `<stuff>` 的物件：`QE_Flash`、`QE_Exit`、`QE_SubMap_Burrow`、`QE_PressurePlate`、`CQF_CryptosleepCasket`、`QE_LootBox_Corpses` 等（无 `stuffCategories`）。

```
× 错误：<li Class="QuestEditor_Library.CustomThingData_LootBox">
          <def>QE_Bookshelf</def>
          <position>(23,0,11)</position>
          <!-- 缺少 stuff，报错 -->
        </li>

✓ 正确：<li Class="QuestEditor_Library.CustomThingData_LootBox">
          <def>QE_Bookshelf</def>
          <stuff>WoodLog</stuff>
          <position>(23,0,11)</position>
        </li>
```

### ThingData XML 标签名实战反例

**在 `thingDatas` 中，`<allPositions>` 不是 `<positions>`**

ThingData 的 C# 字段名叫 `allPositions`（`List<IntVec3>`），所以手写 XML 时标签必须用 `<allPositions>`。用 `<positions>` 会无法反序列化。

```
× 错误：<li>
          <def>StandingLamp</def>
          <stuff>Steel</stuff>
          <positions>                       <!-- 字段名不对 -->
            <li>(38, 0, 8)</li>
          </positions>
        </li>

✓ 正确：<li>
          <def>StandingLamp</def>
          <stuff>Steel</stuff>
          <allPositions>                    <!-- 匹配字段名 allPositions -->
            <li>(38, 0, 8)</li>
          </allPositions>
        </li>
```

原理：`ThingData` 的字段定义是 `public List<IntVec3> allPositions`，`SaveToXElement` 写的是 `"allRect"` 和 `"position"`。
- 有 `allRect` 时用 `<allRect>` + `<li>(minX,minZ,maxX,maxZ)</li>`
- 有 `allPositions` 时用 `<allPositions>` + `<li>(x,0,z)</li>`
- 单个位置用 `<position>(x,0,z)</position>`
- 这三个是互斥的：`allRect` > `allPositions` > `position` 优先级（见 `GenStep_CustomMap.cs:361-368`）

### ThingData 的 rotation 格式

ThingData 的 `rotation` 在 `SaveToXElement` 中写为 `rotation.AsInt`（整数 0~3），对应：
- `0` = North
- `1` = East  
- `2` = South
- `3` = West

默认是 North（0），不需要写。只有非默认方向才写 `<rotation>1</rotation>`。**不要写成 `(0,0,0)` 这种 IntVec3 格式**。

```
× 错误：<li>
          <def>AncientTerminal</def>
          <position>(55, 0, 8)</position>
          <rotation>(0, 0, 0)</rotation>    <!-- IntVec3 格式，Rot4 不认 -->
        </li>

✓ 正确：<li>
          <def>AncientTerminal</def>
          <position>(55, 0, 8)</position>
          <!-- 默认 North，不写 rotation -->
        </li>

✓ 正确：<li>
          <def>AncientTerminal</def>
          <position>(55, 0, 8)</position>
          <rotation>1</rotation>            <!-- East，整数 0-3 -->
        </li>
```

### XML 注释过多问题

XML 中的注释不会被解析器忽略（RimWorld 的 XML 解析在反序列化阶段会跳过注释，但大量注释会让文件难以维护）。

推荐规则：
- 每个大段只用一行短注释标注即可（如 `<!-- 入口大厅装饰柱 -->`）
- 不要用大段 `=====` 框框注释
- 注释内容用中文，简洁清晰

### LootDataDef 字段名：`loots` 不是 `datas`

`LootDataDef` 的 C# 字段定义是 `public List<LootData> loots`，所以 XML 标签必须用 `<loots>`。用 `<datas>` 会报错 "doesn't correspond to any field in type LootDataDef"。

```
× 错误：<QuestEditor_Library.LootDataDef>
          <defName>MyLoot</defName>
          <datas>                               <!-- 字段名不对 -->
            <li Class="QuestEditor_Library.LootData">...</li>
          </datas>
        </QuestEditor_Library.LootDataDef>

✓ 正确：<QuestEditor_Library.LootDataDef>
          <defName>MyLoot</defName>
          <loots>                               <!-- 匹配字段名 loots -->
            <li Class="QuestEditor_Library.LootData">...</li>
          </loots>
        </QuestEditor_Library.LootDataDef>
```

原理：写任何 CQF Def 时，**先查源码字段名**。字段名就是 XML 标签名。对应关系：
- `DataDefs.cs:12`: `public List<LootData> loots` → `<loots>`
- `DataDefs.cs:16`: `public List<InteractionOperation> interactions` → `<interactions>`

### DialogCondition_Reversal 的单对象格式

`DialogCondition_Reversal` 的 `condition` 字段是单对象（`public DialogCondition condition`），不是列表。它用 `Scribe_Deep.Look` 序列化，XML 中必须把 `Class` 属性直接写在 `<condition>` 元素上，**不能**用 `<li>` 包装。

```
× 错误：<li Class="QuestEditor_Library.DialogCondition_Reversal">
          <condition>
            <li Class="QuestEditor_Library.DialogCondition_Bool">    <!-- 单对象不能有 li 包装 -->
              <boolName>MyBool</boolName>
            </li>
          </condition>
        </li>

✓ 正确：<li Class="QuestEditor_Library.DialogCondition_Reversal">
          <condition Class="QuestEditor_Library.DialogCondition_Bool">  <!-- 直接在 condition 上加 Class -->
            <boolName>MyBool</boolName>
          </condition>
        </li>
```

通用原则：CQF 中所有"单个子对象"的字段（不是列表），都用 `Class` 属性直接写在子节点上：
 - `DialogCondition_Reversal.condition` → `<condition Class="...">`
 - 所有含有单个嵌套对象的字段同理，不要想当然套 `<li>` 列表格式

### DialogCondition_And / DialogCondition_Or 的字段名

`DialogCondition_And` 和 `DialogCondition_Or` 的字段名是 `condition`（单数），不是 `conditions`（复数）。字段定义是 `public List<DialogCondition> condition`（虽然是列表但名称为单数）。

```
× 错误：<li Class="QuestEditor_Library.DialogCondition_And">
          <conditions>                          <!-- 字段名是 condition 不是 conditions -->
            <li Class="QuestEditor_Library.DialogCondition_DatabaseExists">...</li>
          </conditions>
        </li>

✓ 正确：<li Class="QuestEditor_Library.DialogCondition_And">
          <condition>                           <!-- 匹配字段名 condition -->
            <li Class="QuestEditor_Library.DialogCondition_DatabaseExists">...</li>
          </condition>
        </li>
```

注意区别（容易混淆的两对"条件列表"字段）：
- `InteractionOperation.conditions` → `<conditions>`（复数 ✅）
- `InteractionResult.conditions` → `<conditions>`（复数 ✅）
- `DialogCondition_And.condition` → `<condition>`（单数！⚠️）
- `DialogCondition_Or.condition` → `<condition>`（单数！⚠️）

原理：永远以 C# 字段名为准，不要凭感觉加 s。

### CQFAction 字段名实战反例

**`CQFAction_Explosion` 没有 `damageDef` 字段**
```
× 错误：<li Class="QuestEditor_Library.CQFAction_Explosion">
          <radius>2.9</radius>
          <damageDef>Bomb</damageDef>    <!-- damageDef 不存在 -->
        </li>

✓ 正确：<li Class="QuestEditor_Library.CQFAction_Explosion">
          <radius>2.9</radius>
          <amount>30</amount>
          <damage>Bomb</damage>          <!-- 字段名是 damage，不是 damageDef -->
        </li>
```
原理：写 CQFAction 子类 XML 时，**不要靠猜字段名**，必须查源码中 `SaveToXElement` 或 `ExposeData` 的确切写法。
`CQFAction_Explosion` 的三个可写字段是 `damage`（DamageDef）、`amount`（int）、`radius`（float）。

## CQFAction API 速查

所有 CQFAction 的 XML 写法。`<li Class="QuestEditor_Library.CQFAction_XXX">` 是固定开头。

> 字段说明：`[NoTranslate]` = 字符串（不翻译），`Def` = defName 引用，粗体 = 必填

### 流程控制类

**CQFAction_Sequence** — 顺序执行子动作列表
```xml
<li Class="QuestEditor_Library.CQFAction_Sequence">
  <actions>...</actions>             <!-- List<CQFAction> 子动作 -->
</li>
```

**CQFAction_Random** — 随机执行一个子动作
```xml
<li Class="QuestEditor_Library.CQFAction_Random">
  <actions>...</actions>
</li>
```

**CQFAction_Condition** — 条件满足后才执行子动作
```xml
<li Class="QuestEditor_Library.CQFAction_Condition">
  <actions>...</actions>             <!-- 条件满足时执行 -->
  <conditions>...</conditions>       <!-- List<DialogCondition> 条件列表 -->
</li>
```

**CQFAction_Chance** — 按概率执行一个动作
```xml
<li Class="QuestEditor_Library.CQFAction_Chance">
  <action>...</action>               <!-- CQFAction 子动作 -->
  <chance>0.5</chance>               <!-- float 概率 0~1 -->
</li>
```

**CQFAction_Loop** — 循环执行子动作
```xml
<li Class="QuestEditor_Library.CQFAction_Loop">
  <loopCount>3</loopCount>           <!-- int 循环次数 -->
  <actions>...</actions>
</li>
```

**CQFAction_DelayExecute** — 延迟后执行
```xml
<li Class="QuestEditor_Library.CQFAction_DelayExecute">
  <delayTime>2500</delayTime>        <!-- int 延迟 tick -->
  <actions>...</actions>
</li>
```

### 信号 / 状态类

**CQFAction_SentSignal** — 发送信号
```xml
<li Class="QuestEditor_Library.CQFAction_SentSignal">
  <signal>MyEvent</signal>           <!-- [NoTranslate] 信号名 -->
  <addQuestPrefix>true</addQuestPrefix>  <!-- bool 自动加 Quest{id}. 前缀 -->
  <signalIsOnlyValidInPart>false</signalIsOnlyValidInPart>  <!-- bool 仅当前地图部件 -->
</li>
```

**CQFAction_SetBool** — 设置任务布尔值
```xml
<li Class="QuestEditor_Library.CQFAction_SetBool">
  <keyOfBool>BossDefeated</keyOfBool>  <!-- [NoTranslate] 键名 -->
  <valueOfBool>true</valueOfBool>       <!-- bool 值 -->
</li>
```

**CQFAction_SetGlobalBool** — 设置全局布尔值（跨任务）
```xml
<li Class="QuestEditor_Library.CQFAction_SetGlobalBool">
  <keyOfBool>GlobalEvent</keyOfBool>
  <valueOfBool>true</valueOfBool>
</li>
```

**CQFAction_SetRelation** — 设置两个 Pawn 之间的亲缘关系
```xml
<li Class="QuestEditor_Library.CQFAction_SetRelation">
  <relation>Parent</relation>          <!-- PawnRelationDef.defName -->
  <targetA>Trigger</targetA>           <!-- [NoTranslate] 目标 key -->
  <targetB>CustomThing</targetB>
</li>
```

**CQFAction_AddQuestTag** — 给目标添加任务标签（使其能收发信号）
```xml
<li Class="QuestEditor_Library.CQFAction_AddQuestTag">
  <tag>QuestKey</tag>                 <!-- [NoTranslate] -->
</li>
```

### 消息 / 对话类

**CQFAction_Message** — 显示消息
```xml
<li Class="QuestEditor_Library.CQFAction_Message">
  <message>Some text here</message>    <!-- 消息文本（支持翻译 key） -->
  <type>PositiveEvent</type>           <!-- MessageTypeDef.defName -->
</li>
```
type 可选：PositiveEvent / NegativeEvent / NeutralEvent / ThreatBig / ThreatSmall / TaskCompletion / RejectInput / CautionInput / SilentInput

**CQFAction_StartDialog** — 启动对话
```xml
<li Class="QuestEditor_Library.CQFAction_StartDialog">
  <dialog>MyDialog</dialog>            <!-- DialogManagerDef.defName -->
  <interviewerText>Trigger</interviewerText>
  <intervieeText>CustomThing</intervieeText>
</li>
```

**CQFAction_EndGame** — 结束游戏并显示消息
```xml
<li Class="QuestEditor_Library.CQFAction_EndGame">
  <message>Game over text</message>
</li>
```

**CQFAction_GainMood** — 给目标添加心情
```xml
<li Class="QuestEditor_Library.CQFAction_GainMood">
  <thought>ThoughtDefName</thought>    <!-- ThoughtDef.defName -->
  <stage>0</stage>                     <!-- int 阶段索引 -->
</li>
```

**CQFAction_GainExperience** — 给目标添加技能经验
```xml
<li Class="QuestEditor_Library.CQFAction_GainExperience">
  <skill>Crafting</skill>              <!-- SkillDef.defName，留空=随机 -->
  <experienceRange>500~1000</experienceRange>  <!-- FloatRange -->
</li>
```

### 数据库记录类

**CQFAction_RecordToDatabase** — 记录目标到数据库
```xml
<li Class="QuestEditor_Library.CQFAction_RecordToDatabase">
  <recordKey>MyKey</recordKey>         <!-- [NoTranslate] 存储 key -->
  <recordToQuestBase>true</recordToQuestBase>     <!-- bool 任务数据库 -->
  <recordToTemporaryBase>false</recordToTemporaryBase>
  <recordToGlobalBase>false</recordToGlobalBase>
</li>
```

**CQFAction_GetThingToRecord** — 获取目标位置上的 Thing 再记录
```xml
<li Class="QuestEditor_Library.CQFAction_GetThingToRecord">
  <recordKey>DoorKey</recordKey>
  <recordToQuestBase>true</recordToQuestBase>
</li>
```

**CQFAction_GetCellToRecord** — 记录位置到数据库
```xml
<li Class="QuestEditor_Library.CQFAction_GetCellToRecord">
  <recordKey>EntrancePos</recordKey>
  <recordToQuestBase>true</recordToQuestBase>
</li>
```

**CQFAction_RecordStartCell** — 记录矩形起始格
```xml
<li Class="QuestEditor_Library.CQFAction_RecordStartCell">
  <recordKey>RoomStart</recordKey>
</li>
```

**CQFAction_FinishRect** — 配合 RecordStartCell 形成矩形区域，批量执行动作
```xml
<li Class="QuestEditor_Library.CQFAction_FinishRect">
  <recordKey>RoomStart</recordKey>
  <actions>...</actions>
</li>
```

**CQFAction_RecordToGroup** — 记录目标到群组
```xml
<li Class="QuestEditor_Library.CQFAction_RecordToGroup">
  <recordKey>EnemyGroup</recordKey>
</li>
```

**CQFAction_DoActionForGroup** — 对群组内所有目标执行动作
```xml
<li Class="QuestEditor_Library.CQFAction_DoActionForGroup">
  <recordKey>EnemyGroup</recordKey>
  <actions>...</actions>
</li>
```

### 生成 / 刷出类

**CQFAction_Spawn** — 在目标位置刷出掉落物
```xml
<li Class="QuestEditor_Library.CQFAction_Spawn">
  <datas>
    <li Class="QuestEditor_Library.LootData">
      <dataName>Reward</dataName>
      <chance>1</chance>
      <things>
        <li Class="QuestEditor_Library.CQFThingDefCount">
          <thing>Steel</thing>
          <count>50</count>
        </li>
      </things>
    </li>
  </datas>
</li>
```

**CQFAction_SpawnCustomThing** — 在目标位置生成 CQF 自定义物件
```xml
<li Class="QuestEditor_Library.CQFAction_SpawnCustomThing">
  <data>...</data>                     <!-- CustomThingData 完整定义 -->
  <key>RecordKey</key>                 <!-- [NoTranslate] 可选：记录 key -->
</li>
```

**CQFAction_SpawnAndAddToInventory** — 生成并加入目标背包
```xml
<li Class="QuestEditor_Library.CQFAction_SpawnAndAddToInventory">
  <datas>...</datas>
</li>
```

**CQFAction_SpawnAndAddToContainer** — 生成并加入容器
```xml
<li Class="QuestEditor_Library.CQFAction_SpawnAndAddToContainer">
  <datas>...</datas>
</li>
```

**CQFAction_ReleaseFromContainer** — 从容器释放 Pawn
```xml
<li Class="QuestEditor_Library.CQFAction_ReleaseFromContainer">
  <!-- 无额外参数，基于 targetsText 获取目标容器 -->
</li>
```

### 地图 / 子地图类

**CQFAction_GenerateSubMap** — 生成子地图
```xml
<li Class="QuestEditor_Library.CQFAction_GenerateSubMap">
  <set>...</set>                       <!-- CustomMapGenerationSet -->
  <pos>(10,0,10)</pos>                 <!-- IntVec3 子地图起始位置 -->
</li>
```

**CQFAction_LinkEntranceAndExit** — 将入口对象和出口对象关联
```xml
<li Class="QuestEditor_Library.CQFAction_LinkEntranceAndExit">
  <entranceText>Entrance</entranceText>
  <exitText>Exit</exitText>
</li>
```

**CQFAction_ActivateCustomMap** — 激活一个 CustomMapEntrance
```xml
<li Class="QuestEditor_Library.CQFAction_ActivateCustomMap">
  <!-- 基于 targetsText 获取入口对象 -->
</li>
```

**CQFAction_SwtichEntranceStatus** — 开关入口状态
```xml
<li Class="QuestEditor_Library.CQFAction_SwtichEntranceStatus">
  <!-- 基于 targetsText 获取入口对象，切换开启/关闭 -->
</li>
```

### Pawn 状态操作类

**CQFAction_Hediff** — 给目标添加 Hediff
```xml
<li Class="QuestEditor_Library.CQFAction_Hediff">
  <hediff>CQF_CustomHediff</hediff>    <!-- HediffDef.defName -->
  <severity>1.0</severity>             <!-- float 严重度 -->
  <bodyPart>Head</bodyPart>            <!-- BodyPartDef.defName，可选 -->
  <customLabel>Custom label</customLabel>  <!-- 可选 -->
</li>
```

**CQFAction_SetCustomHediff** — 设置自定义 Hediff（含组件和颜色）
```xml
<li Class="QuestEditor_Library.CQFAction_SetCustomHediff">
  <hediff>CQF_CustomHediff</hediff>
  <label>Custom Label</label>
  <desc>Custom description</desc>
  <color>(1,0,0,1)</color>
  <comps>...</comps>
</li>
```

**CQFAction_Trait** — 给目标添加特性
```xml
<li Class="QuestEditor_Library.CQFAction_Trait">
  <trait>Kind</trait>                  <!-- TraitDef.defName -->
  <degree>0</degree>                   <!-- int 程度 -->
</li>
```

**CQFAction_RemoveTrait** — 移除特性
```xml
<li Class="QuestEditor_Library.CQFAction_RemoveTrait">
  <trait>Kind</trait>
  <degree>0</degree>
</li>
```

**CQFAction_UpgradeTrait** — 升级特性
```xml
<li Class="QuestEditor_Library.CQFAction_UpgradeTrait">
  <trait>Kind</trait>
  <initDegree>0</initDegree>
  <message>Upgrade message</message>
  <initMessage>Init message</initMessage>
</li>
```

**CQFAction_Ability** — 给目标添加能力
```xml
<li Class="QuestEditor_Library.CQFAction_Ability">
  <ability>AbilityDefName</ability>
</li>
```

**CQFAction_SetDuty** — 设置目标 Duty
```xml
<li Class="QuestEditor_Library.CQFAction_SetDuty">
  <duty>QE_Duty_Guard</duty>           <!-- DutyDef.defName -->
</li>
```

**CQFAction_SetXenotype** — 设置异种基因
```xml
<li Class="QuestEditor_Library.CQFAction_SetXenotype">
  <xenotype>XenotypeDefName</xenotype>
</li>
```

**CQFAction_StartMentalState** — 让目标进入精神状态
```xml
<li Class="QuestEditor_Library.CQFAction_StartMentalState">
  <state>MentalStateDefName</state>
  <stateTargetText>Trigger</stateTargetText>  <!-- 可选，社交战斗时目标 -->
</li>
```

**CQFAction_Faction** — 设置目标派系
```xml
<li Class="QuestEditor_Library.CQFAction_Faction">
  <faction>FactionDefName</faction>
</li>
```

### 传送 / 移动类

**CQFAction_Skip** — 把目标传送到指定位置
```xml
<li Class="QuestEditor_Library.CQFAction_Skip">
  <skipedTargetText>Trigger</skipedTargetText>
  <targetLocationText>CustomThing</targetLocationText>
</li>
```

**CQFAction_SkipToPlayerMap** — 把目标传送到玩家地图
```xml
<li Class="QuestEditor_Library.CQFAction_SkipToPlayerMap">
  <skipedTargetText>Trigger</skipedTargetText>
</li>
```

### 伤害 / 环境类

**CQFAction_Explosion** — 在目标位置产生爆炸
```xml
<li Class="QuestEditor_Library.CQFAction_Explosion">
  <radius>2.9</radius>                 <!-- float 爆炸半径 -->
  <amount>30</amount>                  <!-- int 伤害值 -->
  <damage>Bomb</damage>                <!-- DamageDef.defName -->
</li>
```

**CQFAction_Lightning** — 在目标位置召唤闪电
```xml
<li Class="QuestEditor_Library.CQFAction_Lightning">
  <!-- 无额外参数 -->
</li>
```

**CQFAction_TakeDamage** — 直接对目标造成伤害
```xml
<li Class="QuestEditor_Library.CQFAction_TakeDamage">
  <damage>Bullet</damage>              <!-- DamageDef.defName -->
  <amount>15</amount>                  <!-- float 伤害值 -->
</li>
```

**CQFAction_Destory** — 销毁目标
```xml
<li Class="QuestEditor_Library.CQFAction_Destory">
  <targetsText>
    <li>CustomThing</li>              <!-- 销毁当前陷阱 / 交互物自身 -->
  </targetsText>
</li>
```
陷阱自销毁时使用 `CustomThing`，不要写 `Trigger`。

**CQFAction_Fog** — 给目标位置添加迷雾
```xml
<li Class="QuestEditor_Library.CQFAction_Fog">
</li>
```

**CQFAction_FloodUnfog** — 清除迷雾
```xml
<li Class="QuestEditor_Library.CQFAction_FloodUnfog">
</li>
```

**CQFAction_Pollute** — 污染目标区域
```xml
<li Class="QuestEditor_Library.CQFAction_Pollute">
</li>
```

### 事件 / 任务类

**CQFAction_Incident** — 触发一个事件
```xml
<li Class="QuestEditor_Library.CQFAction_Incident">
  <incident>IncidentDefName</incident>
</li>
```

**CQFAction_Quest** — 发放一个任务
```xml
<li Class="QuestEditor_Library.CQFAction_Quest">
  <quest>QuestScriptDefName</quest>
</li>
```

**CQFAction_SetGameCondition** — 添加地图环境状态
```xml
<li Class="QuestEditor_Library.CQFAction_SetGameCondition">
  <condition>GameConditionDefName</condition>
  <duration>60000</duration>           <!-- IntRange tick -->
  <permanent>false</permanent>         <!-- bool 是否永久 -->
</li>
```

**CQFAction_SetGameConditionWithActions** — 添加地图环境状态（带动作触发）
```xml
<li Class="QuestEditor_Library.CQFAction_SetGameConditionWithActions">
  <condition>GameConditionDefName</condition>
  <duration>60000</duration>
  <actions>...</actions>
  <useTick>true</useTick>
  <tick>2500</tick>
</li>
```

**CQFAction_ChangeGoodwillOfFaction** — 改变派系好感
```xml
<li Class="QuestEditor_Library.CQFAction_ChangeGoodwillOfFaction">
  <fixedFaction>FactionDefName</fixedFaction>  <!-- 可选，指定派系 -->
  <isIncrease>true</isIncrease>                 <!-- bool 增加/减少 -->
  <value>30</value>                              <!-- int 数值 -->
  <sendLetter>true</sendLetter>                  <!-- bool 发信件 -->
</li>
```

### 容器 / 物品操作类

**CQFAction_OpenLootBox** — 强制打开战利品箱
```xml
<li Class="QuestEditor_Library.CQFAction_OpenLootBox">
  <!-- 基于 targetsText 获取目标箱子 -->
</li>
```

**CQFAction_ConsumeInInventory** — 消耗目标背包中的物品
```xml
<li Class="QuestEditor_Library.CQFAction_ConsumeInInventory">
  <requirations>
    <li Class="QuestEditor_Library.CQFThingDefCount">
      <thing>Steel</thing>
      <count>5~10</count>
    </li>
  </requirations>
</li>
```

**CQFAction_Replace** — 替换目标物件
```xml
<li Class="QuestEditor_Library.CQFAction_Replace">
  <!-- 自定义替换逻辑 -->
</li>
```

**CQFAction_AddThingActionTrigger** — 给目标附加被动触发动作
```xml
<li Class="QuestEditor_Library.CQFAction_AddThingActionTrigger">
  <key>TriggerKey</key>
  <mode>Damaged</mode>                 <!-- ActionTriggerMode -->
  <actions>...</actions>
</li>
```

**CQFAction_PostGenerationExecute** — 地图生成后执行动作
```xml
<li Class="QuestEditor_Library.CQFAction_PostGenerationExecute">
  <actions>...</actions>
</li>
```

### 视觉特效类

**CQFAction_DoEffect** — 执行特效
```xml
<li Class="QuestEditor_Library.CQFAction_DoEffect">
</li>
```

**CQFAction_MakeMoteStatic** — 创建静态粒子
```xml
<li Class="QuestEditor_Library.CQFAction_MakeMoteStatic">
</li>
```

**CQFAction_ThrowMote** — 抛射粒子效果
```xml
<li Class="QuestEditor_Library.CQFAction_ThrowMote">
</li>
```

### Lord / 集群类

**CQFAction_Lord_Visit** — 让 Lord 执行访问殖民地行为
```xml
<li Class="QuestEditor_Library.CQFAction_Lord_Visit">
  <lordName>MyLord</lordName>
  <faction>FactionDefName</faction>
  <durationTicks>50000</durationTicks>
</li>
```

---

## 目标 key 速查 (targetsText)

CQFAction_Target 的子类通过 `targetsText` 指定目标来源。常用 key：

| key | 来源 |
|-----|------|
| `Trigger` | 交互者 Pawn |
| `CustomThing` | 当前交互对象 |
| `Position` | 当前位置（GenerationActionWorker 等场景） |
| `Inner` | 容器内部 |
| `Target` | 群组动作的当前目标 |
| `null` | 空目标 |

> 所有的 action 字段名和 XML 结构均来自 `D:\Code\Git\QuestEditor_Library\QuestEditor_Library\CQFUtility.cs` 中对应类的 `SaveToXElement()` 方法。

## DialogCondition API 速查

所有 DialogCondition 的 XML 写法。用于交互条件、`CQFAction_Condition` 等场景。

**DialogCondition_Bool** — 检查任务或全局布尔值
```xml
<li Class="QuestEditor_Library.DialogCondition_Bool">
  <boolName>MyBoolKey</boolName>       <!-- [NoTranslate] 键名 -->
  <failReason>Not completed</failReason>
</li>
```

**DialogCondition_DatabaseExists** — 检查数据库 key 是否存在
```xml
<li Class="QuestEditor_Library.DialogCondition_DatabaseExists">
  <targetKey>MyRecordedKey</targetKey>  <!-- [NoTranslate] 数据库 key -->
  <checkQuestDatabase>true</checkQuestDatabase>      <!-- 检查任务数据库 -->
  <checkTemporaryDatabase>false</checkTemporaryDatabase>
  <checkGlobalDatabase>false</checkGlobalDatabase>
  <needSpawned>true</needSpawned>                    <!-- 要求目标仍存活/生成 -->
</li>
```

**DialogCondition_Skill** — 固定技能门槛
```xml
<li Class="QuestEditor_Library.DialogCondition_Skill">
  <skill>Crafting</skill>              <!-- SkillDef.defName -->
  <level>4</level>                     <!-- int 等级 -->
  <needToBeGreater>true</needToBeGreater>  <!-- bool >= 还是 <= -->
  <failReason>Need better skill</failReason>
</li>
```

**DialogCondition_SkillCheck** — 概率型技能检定
```xml
<li Class="QuestEditor_Library.DialogCondition_SkillCheck">
  <skill>Medicine</skill>
  <checkModifier>0.5</checkModifier>   <!-- float 难度修正 -->
  <failReason>Check failed</failReason>
</li>
```

**DialogCondition_Inventory** — 检查背包物品
```xml
<li Class="QuestEditor_Library.DialogCondition_Inventory">
  <requirations>
    <li Class="QuestEditor_Library.CQFThingDefCount">
      <thing>Steel</thing>
      <count>5~10</count>
    </li>
  </requirations>
  <failReason>Missing items</failReason>
</li>
```

**DialogCondition_ThingInPosition** — 检查目标是否在指定位置
```xml
<li Class="QuestEditor_Library.DialogCondition_ThingInPosition">
  <targetText>Trigger</targetText>     <!-- 被检查的目标 key -->
  <positionName>CustomThing</positionName>  <!-- 位置 key -->
  <failReason>Not in position</failReason>
</li>
```

**DialogCondition_Hediff** — 检查 Pawn 是否带某 Hediff
```xml
<li Class="QuestEditor_Library.DialogCondition_Hediff">
  <targetText>Trigger</targetText>
  <hediff>HediffDefName</hediff>
  <severity>0.5</severity>             <!-- float 最低严重度 -->
  <needToBeGreater>true</needToBeGreater>
  <failReason>Missing hediff</failReason>
</li>
```

**DialogCondition_Trait** — 检查 Pawn 特性
```xml
<li Class="QuestEditor_Library.DialogCondition_Trait">
  <targetText>Trigger</targetText>
  <trait>Kind</trait>                  <!-- TraitDef.defName -->
  <degree>0</degree>                   <!-- int 程度 -->
  <needToBeGreater>true</needToBeGreater>
  <accurate>false</accurate>           <!-- bool 精确匹配 -->
  <failReason>Wrong trait</failReason>
</li>
```

**DialogCondition_QuestState** — 检查任务状态
```xml
<li Class="QuestEditor_Library.DialogCondition_QuestState">
  <quest>QuestScriptDefName</quest>    <!-- QuestScriptDef.defName -->
  <state>Ongoing</state>               <!-- 可选：Ongoing/Ended/Success/Fail -->
  <failReason>Quest not active</failReason>
</li>
```

**DialogCondition_GroupExists** — 检查群组是否存在
```xml
<li Class="QuestEditor_Library.DialogCondition_GroupExists">
  <targetText>Trigger</targetText>
  <targetKey>GroupKey</targetKey>      <!-- 群组 key -->
  <needSpawned>true</needSpawned>
  <failReason>Group not found</failReason>
</li>
```

**DialogCondition_ContainerIsFull** — 检查容器是否有内容
```xml
<li Class="QuestEditor_Library.DialogCondition_ContainerIsFull">
  <targetText>CustomThing</targetText>
  <failReason>Container is empty</failReason>
</li>
```

**DialogCondition_CapturedPawn** — 检查捕获型容器中是否有 Pawn
```xml
<li Class="QuestEditor_Library.DialogCondition_CapturedPawn">
  <targetText>CustomThing</targetText>
  <failReason>No captured pawn</failReason>
</li>
```

**DialogCondition_Chance** — 概率判定
```xml
<li Class="QuestEditor_Library.DialogCondition_Chance">
  <chance>0.5</chance>                 <!-- float 概率 0~1 -->
  <failReason>Bad luck</failReason>
</li>
```

**DialogCondition_And** — 多个条件全部满足
```xml
<li Class="QuestEditor_Library.DialogCondition_And">
  <condition>
    <li Class="QuestEditor_Library.DialogCondition_Bool">
      <boolName>CondA</boolName>
    </li>
    <li Class="QuestEditor_Library.DialogCondition_Bool">
      <boolName>CondB</boolName>
    </li>
  </condition>
</li>
```

**DialogCondition_Or** — 任一条件满足
```xml
<li Class="QuestEditor_Library.DialogCondition_Or">
  <condition>...</condition>
</li>
```

**DialogCondition_Reversal** — 反转条件结果
```xml
<li Class="QuestEditor_Library.DialogCondition_Reversal">
  <condition>...</condition>
</li>
```

---

## 源码第一跳索引

以后遇到地图问题，优先先查这些稳定入口：
- `QuestEditor_Library/Def/CustomMapDataDef.cs`
- `QuestEditor_Library/CustomMapGenerationSet.cs`
- `QuestEditor_Library/CustomThing/SpecialThing/ZoneCore.cs`
- `QuestEditor_Library/CustomThing/SpecialThing/InteractableThing.cs`
- `QuestEditor_Library/CustomThing/SpecialThing/LootBox.cs`
- `QuestEditor_Library/CustomThing/SpecialThing/CustomTrap.cs`
- `QuestEditor_Library/CustomThing/SpecialThing/CompActionWorker.cs`
- `QuestEditor_Library/QuestNode/QuestNode_Root_CustomMap.cs`
- `QuestEditor_Library/QuestNode/QuestNode_RandomCustomMap.cs`
- `QuestEditor_Library/QuestNode/QuestNode_DoCQFActions.cs`
- `QuestEditor_Library/CQFUtility.cs`

## 工作方式

处理地图任务时，按以下顺序：

1. 先说明方案
- 先说地图类型
- 先说流程结构
- 先说准备读哪些文件
- 先说准备改哪些 Def / 文本 / 数据

2. 先定地图骨架
- 入口
- 出口
- 核心房间
- 关键对象
- 关键阶段

3. 再定对象玩法
- 交互点
- 门禁
- 箱子
- 陷阱
- 刷怪点

4. 再定条件和动作
- 每个关键点都要明确：
- 用什么条件
- 触发什么动作
- 是否发信号
- 是否记数据库

5. 最后补文本和翻译

## 文本规则

必须遵守：
- 不要硬编码中文显示文本
- 必须做双语
- UI 显示文本优先走 `Key + 翻译`
- Def 文本优先走翻译体系
- 地图交互文本、消息文本、结果文本都要考虑双语

## 目标

本 Skill 的目标是：
- 让 AI 可以独立设计 CQF 地图
- 让 AI 可以把地图、交互、陷阱、战利品箱、条件、行为、对话、信号接成完整流程
- 让 AI 不用每次重查源码才能生产地图
