# CQF 进阶交互与信号

编写 CQF 地图、任务和交互物时，先按本文检查动作目标、条件比较、信号连接和建筑占格。CQF 的部分配置在字段缺失时不会抛出明显错误，而是直接不执行动作，因此不要省略看似可以推断的字段。

## 位置与占格

- `position` 必须写成 RimWorld 的三坐标格式：`(x,0,z)`。不要写成 `(x,z)`。
- CQF 将建筑 `position` 作为建筑中心格，而不是左下角。
- 原版建筑占格应按 `GenAdj.OccupiedRect` 的中心算法计算。设旋转后的尺寸为 `width x height`：
  - `minX = center.x - (width - 1) / 2`
  - `minZ = center.z - (height - 1) / 2`
  - 偶数尺寸会按原版规则向一侧多占一格，不能用手工对称假设替代。
- 放置多格建筑前，检查完整 `CellRect` 是否越界、撞墙、撞门或与其他建筑重叠。
- 室内大房间和厚屋顶区域必须检查屋顶支撑距离。需要时使用原版 `Column`，或仅在确有设计理由时让自定义建筑设置 `holdsRoof`。

## 技能条件

`DialogCondition_Skill` 必须显式指定 `targetText`：

```xml
<li Class="QuestEditor_Library.DialogCondition_Skill">
  <skill>Construction</skill>
  <level>5</level>
  <needToBeGreater>true</needToBeGreater>
  <targetText>Trigger</targetText>
</li>
```

- `targetText` 写 `Trigger`，表示检查当前触发交互的殖民者。
- `needToBeGreater=true` 使用严格大于比较，即 `skill.Level > level`。
- 因此要求技能 6 级以上时，写 `<level>5</level>`；要求 7 级以上时写 `6`。
- 不要只看界面文案推断阈值，需同时核对比较运算符和 XML 数值。

## 动作目标

所有继承 `CQFAction_Target` 的动作都必须显式写 `targetsText`。该字段是列表；缺少它时会保留默认目标 `null`，筛选结果通常为空，动作会静默不执行。

```xml
<li Class="QuestEditor_Library.CQFAction_Explosion">
  <targetsText>
    <li>CustomThing</li>
  </targetsText>
  <damage>Bomb</damage>
  <amount>32</amount>
  <radius>3.2</radius>
</li>
```

常见目标：

- `Trigger`：当前触发交互的人或物。
- `CustomThing`：当前自定义物件或陷阱提供的目标。

每次使用目标动作都检查 `targetsText` 中的名称能否由当前交互、陷阱或任务上下文实际提供，不能依赖默认值。

## 库存条件与消耗

库存交付必须用固定数量，并同时写完整的目标字段：

```xml
<li Class="QuestEditor_Library.DialogCondition_Inventory">
  <targetText>Trigger</targetText>
  <requirations>
    <li Class="QuestEditor_Library.CQFThingDefCount">
      <thing>Kibble</thing>
      <count>30</count>
    </li>
  </requirations>
</li>

<li Class="QuestEditor_Library.CQFAction_ConsumeInInventory">
  <targetsText>
    <li>Trigger</li>
  </targetsText>
  <requirations>
    <li Class="QuestEditor_Library.CQFThingDefCount">
      <thing>Kibble</thing>
      <count>30</count>
    </li>
  </requirations>
</li>
```

- 字段名是 `requirations`，不要改成常规英文拼写。
- `DialogCondition_Inventory` 使用单数 `targetText`；交互者写 `Trigger`。
- `CQFAction_ConsumeInInventory` 继承 `CQFAction_Target`，必须使用列表字段 `targetsText`；交互者写 `Trigger`。
- 条件检查与实际消耗会分别调用一次 `count.RandomInRange`。如果写 `20~30`，两次可能抽到不同数量，造成条件通过后消耗失败或数量不一致。
- 因此同一交付流程中的检查和消耗必须写相同的固定值，例如都写 `<count>30</count>`。随机范围只用于奖励生成，不用于成对的库存检查与消耗。

## 布尔状态

使用 `CQFAction_SetBool` 写状态，使用 `DialogCondition_Bool` 读状态。两端的 Key 必须逐字一致。

```xml
<li Class="QuestEditor_Library.CQFAction_SetBool">
  <keyOfBool>HCM_CoolingRestored</keyOfBool>
  <valueOfBool>true</valueOfBool>
</li>
```

```xml
<li Class="QuestEditor_Library.DialogCondition_Bool">
  <failReason>HCM_CoolingOffline</failReason>
  <boolName>HCM_CoolingRestored</boolName>
</li>
```

- `DialogCondition_Bool` 只在对应值为 `true` 时通过，没有用于比较目标值的 `value` 字段。
- 需要检查 `false` 时，用 `DialogCondition_Reversal` 包裹 `DialogCondition_Bool`。
- 为互斥路线设计明确的初始状态和终态。
- 交互完成后如需防止重复执行，应设置单独的完成状态，并在交互条件中检查它。
- 检查所有状态读取都有至少一个可达的写入动作，避免永远无法满足的分支。

## 信号与延迟动作

- `CQFAction_SentSignal` 发送本地 CQF 信号。
- 陷阱或监听物的 `inSignal` 必须与发送值完全一致。
- `QuestNode_Signal` 监听任务信号时，会使用任务运行时自动添加的任务前缀。不要把运行时前缀硬编码进地图 XML。
- `CQFAction_DelayExecute` 的延迟单位是 Tick。RimWorld 通常为每秒 60 Tick。
- 多段延迟事件应使用不同信号名，便于验证每段链路并避免误触发。

### 信号触发爆炸示例

交互动作发送信号：

```xml
<li Class="QuestEditor_Library.CQFAction_SentSignal">
  <signal>HCM_ReactorBlast_1</signal>
</li>
```

延迟发送后续信号：

```xml
<li Class="QuestEditor_Library.CQFAction_DelayExecute">
  <delayTime>120</delayTime>
  <actions>
    <li Class="QuestEditor_Library.CQFAction_SentSignal">
      <signal>HCM_ReactorBlast_2</signal>
    </li>
  </actions>
</li>
```

`delayTime` 会在 `GameComponentTick` 中每 Tick 减一，因此 `120` 约为 2 秒。

地图陷阱监听信号，并明确以当前自定义陷阱为爆炸目标：

```xml
<li>
  <inSignal>HCM_ReactorBlast_1</inSignal>
  <actions>
    <li Class="QuestEditor_Library.CQFAction_Explosion">
      <targetsText>
        <li>CustomThing</li>
      </targetsText>
      <damage>Bomb</damage>
      <amount>32</amount>
      <radius>3.2</radius>
    </li>
  </actions>
</li>
```

具体类名和字段仍应以当前安装的 CQF 源码或已验证 XML 为准；不要凭记忆创造字段。

## 复用原版贴图

- 自定义 CQF 交互物可以复用原版 `texPath`，但应同时沿用原 ThingDef 的 `graphicClass`。
- `Graphic_Multi`、`Graphic_Random` 等图形类型不可随意互换，否则可能出现方向、随机子贴图或材质加载错误。
- 自定义建筑的 `<size>` 应与复用对象一致，除非已经确认贴图与占格可以接受不同尺寸。
- 修改尺寸后重新检查中心占格、墙体碰撞、交互格和屋顶支撑。

## 正式任务设计规则

### CQF 测试物件

- 不要在正式游戏中直接使用名称或描述明确为 `Custom ...`、`... for testing` 的 CQF 编辑器测试建筑。
- 正式可见物件应定义自己的 Mod ThingDef，底层使用 CQF 功能类或抽象基类，并复用合适的原版或 CQF 正式贴图。
- `QE_CustomTrap` 与 `QE_TriggerTrap` 是两个 ThingDef。`QE_CustomTrap` 使用 `CustomTrap`，是普通测试陷阱，正式地图禁止使用。
- `QE_TriggerTrap` 使用 `CustomTrap_Dev`，正常模式隐藏、开发者模式显示调试标记。它只允许承担隐藏献祭点或确有必要的不可见逻辑触发，不得作为可见建筑。

### LootBox 搜刮后状态

- 柜子、板条箱、低温舱等实体容器默认设置 `<destroyAfterOpening>false</destroyAfterOpening>`，搜刮后保留空容器，避免物件无缘无故消失。
- CQF 会保存 LootBox 的 `opened` 状态；保留容器不会允许重复生成战利品。
- 只有一次性包装、会被拆毁的密封装置或剧情明确要求消失的对象，才使用 `destroyAfterOpening=true`。
- `openWhenDestroyed` 单独控制容器被摧毁时是否掉出未领取战利品，不要将它与搜刮后销毁混为一谈。

### 剧情质量

- 不要只写“收到求救信号、进入遗迹、击败敌人、拿走奖励”的通用流程。
- 让入口信息、环境痕迹、可选日志和最终终端形成递进揭示；后续信息应改变玩家对前一段信息的理解。
- 最终选择必须同时具有剧情意义、玩法后果和奖励差异，避免仅用两个按钮选择不同数量的战利品。
- 将技能路线、危险升级、敌人、爆炸、开门和奖励与剧情因果连接，不能让机制像互不相关的功能演示。
- 高潮和最终威胁必须由前面的线索、空间和行为逐步铺垫。不要在结尾突然塞入一个与主题缺乏因果联系的敌人房间充当收尾。
- 控制文本数量，每段记录提供一个新事实或转折，不重复描述场景背景。

### 地图形状与探索路线

- 正式任务设施不要采用完整方形外壳、镜像房间和大面积规则棋盘布局。
- 使用错位模块、破损边界、局部坍塌、窄维护通道、偏心核心区和尺寸不同的房间构成有目的的不规则轮廓。
- 主路线应有转折、视线遮挡和至少一个可选探索分支；可以形成短回环，但不要制造无意义的迷宫。
- 不规则结构仍需服务于剧情和功能分区。不要为了避免方块感而随机撒墙、家具或废墟。
- 屋顶应按实际建筑模块分区铺设，不要用覆盖几乎整张地图的单个大矩形屋顶掩盖建筑轮廓。
- 关键交互必须从玩家预期接近的一侧可达。嵌墙设备、通风口和门控装置要按实际旋转检查 `interactionCellOffset`、交互格阻挡和失败提示，不能把必要提示藏在无法抵达的交互格后面。
- 锁门不能单独承担路线限制，因为普通墙和门可以被攻击或拆除。关键房间要么使用符合设定的不可破坏结构，要么把破墙、破门和绕行写成有反馈、有代价、能正确推进或结束任务的正式分支。

### 主要结构地形

- 世界任务可以继续使用随机世界地块，但主要建筑、关键房间、通道、门口和交互格必须在 `CustomMapDataDef` 中铺设明确的 `terrain`，不能依赖地块原生地形。
- 地形覆盖范围至少包含建筑实际占格、进出口、必要站立位和一圈合理的连接缓冲，避免湖泊或其他不可通行地形切断主体结构。
- 生成后要在不同世界地块实测，确认水域、泥地、沙地等原生地形不会覆盖主体地板，也不会让入口、出口或交互位失效。

## 完成前检查

1. 使用 UTF-8 解析所有 XML，确认无格式错误。
2. 检查每个 `position` 都是 `(x,0,z)`。
3. 检查所有多格建筑完整占格不越界、不重叠。
4. 检查墙、门、柱和屋顶支撑关系。
5. 检查每个显示 Key 在中英文 Keyed 中均存在。
6. 检查中文 DefInjected 覆盖需要本地化的 Def；英文 Def 直接保留原文。
7. 检查 `DialogCondition_Skill.targetText` 与严格比较阈值。
8. 检查每个目标动作的 `targetsText` 都能由当前上下文提供。
9. 检查布尔状态所有读写 Key 和路线可达性。
10. 检查发送信号、延迟信号、陷阱 `inSignal` 与任务监听信号完整连接。
11. 使用原版和 CQF 的实际 Def 定义核对 defName、Stuff 需求、尺寸与 `graphicClass`。
12. 检查正式地图不存在 `QE_CustomTrap`；只有确有需要的隐藏献祭点或不可见逻辑触发器可以使用 `QE_TriggerTrap`。
13. 检查实体 LootBox 搜刮后是否应该保留，避免无理由使用 `destroyAfterOpening=true`。
14. 检查剧情是否有递进揭示、选择后果与机制因果，最终威胁是否得到充分铺垫。
15. 检查设施轮廓、房间与路线是否过度方正或镜像对称。
16. 检查每个关键交互在预期方向和旋转下都有可达交互格与明确提示。
17. 检查锁门房间能否通过破坏墙体或门绕过；能绕过时必须有正式分支，不能绕过时使用合理的不可破坏结构。
18. 检查主要结构、通道、门口和交互格均有显式地形覆盖，并在水域较多的世界地块实测。
