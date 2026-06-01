---
name: "cqf-action-condition-dev"
description: "Detailed skill for CQF actions, dialog conditions, targets, signals, and databases. Invoke when tasks involve CQF runtime logic, action chains, condition gates, or reusable behavior design."
---

# CQF Action And Condition Dev

用于 `CQF / QuestEditor_Library` 的通用运行时 Skill。

本 Skill 专门处理这些整个 CQF 系统都会反复用到的内容：
- `CQFAction`
- `DialogCondition`
- `targets / target key`
- `quest database / global database / temporary database`
- `signal`
- 动作链、条件链、典型组合
- XML 参数与常见坑

它不专属于地图，也不专属于对话。

一句话理解：
- `cqf-map-dev` 负责地图结构与地图对象
- `cqf-dialog-dev` 负责正式对话树
- `cqf-action-condition-dev` 负责“这些系统共同使用的运行时逻辑”

## 调用时机

在以下情况调用本 Skill：
- 用户要写或改 `CQFAction_*`
- 用户要写或改 `DialogCondition_*`
- 用户要设计一串运行时行为链
- 用户要决定某个逻辑该用动作还是条件
- 用户要处理 target key、数据库、信号、布尔值、群组
- 用户要做可复用的通用逻辑，而不是单张地图结构
- 用户要查询某个动作 / 条件的参数、作用、适用场景

如果任务重点是：
- 地图结构、区域拼装、入口出口、地图对象：优先用 `cqf-map-dev`
- 正式多轮对话树、对话管理器、节点分支：优先用 `cqf-dialog-dev`

## 运行时总模型

CQF 运行时逻辑通常由这五层组成：

1. 目标层
- 决定动作和条件到底对谁生效

2. 条件层
- 判断当前能不能继续

3. 行为层
- 真的执行副作用

4. 数据层
- 记录对象、位置、群组、布尔值

5. 信号层
- 通知下一个对象或任务阶段继续执行

默认思考顺序：
1. 目标是谁
2. 需要检查什么条件
3. 条件通过后执行什么动作
4. 是否需要记录数据库
5. 是否需要发送信号

## Action 和 Condition 的边界

### CQFAction

本质：
- 执行副作用

负责：
- 生成
- 改状态
- 发信号
- 发任务
- 记数据库
- 传送
- 开箱
- 改派系
- 加 Hediff / Trait / Ability

一句话理解：
- `CQFAction = 做事`

### DialogCondition

本质：
- 纯判定

负责：
- 判断是否满足前置
- 返回失败原因
- 不直接改变世界状态

一句话理解：
- `DialogCondition = 决定能不能做`

### 默认判断规则

如果你在问：
- “要不要执行” -> 优先想 `DialogCondition`
- “执行后发生什么” -> 优先想 `CQFAction`
- “满足条件后再执行动作” -> `DialogCondition + CQFAction_Condition`
- “选项或交互是否可点” -> 优先想 `DialogCondition`

## 目标系统

### CQFAction_Target

很多动作都继承自 `CQFAction_Target`。

它的核心不是具体效果，而是：
- 先根据 `targetsText` 解析出一组目标
- 再对这些目标执行 `RealWork`

这意味着：
- 大多数动作真正的第一步不是“爆炸 / 传送 / 生成”
- 而是“先找到目标”

### 常用 target key

- `Trigger`
- 当前触发者，常见于交互者、踩陷阱者、事件触发 Pawn

- `CustomThing`
- 当前交互对象、陷阱、门、地图对象本体

- `Position`
- 当前位置，常见于生成、地图处理、区域动作

- `Inner`
- 容器 / 箱子 / 内部生成对象

- `Target`
- 群组动作或通用循环中的当前目标

- `Interviewee`
- 正式对话中的被对话对象

- `Interviewer`
- 正式对话中的发起者

### 行为 / 条件动作链中的目标使用规则

CQF 的目标来源分三类：

1. 上下文目标
- 由当前触发场景自动传入
- 交互对象通常提供 `Trigger` 和 `CustomThing`
- 陷阱通常提供 `Trigger`、`CustomThing` 或触发位置相关上下文
- 群组循环通常提供当前成员 `Target`

2. 显式目标
- 由字段直接指定
- `targetText` 指向单个目标 key
- `targetsText` 指向目标 key 列表
- 适合明确要操作某个 Pawn、Thing、Cell 或群组成员时使用

3. 数据库目标
- 由之前的 `RecordToDatabase`、`GetThingToRecord`、`GetCellToRecord`、`RecordToGroup` 等动作写入
- 后续动作或条件再通过 key 读取
- 适合跨阶段、跨对象、延迟执行或信号联动

默认规则：
- 能直接使用当前上下文时，不要额外写 `targetsText`
- 需要指定“谁被检查 / 谁被操作”时，才写 `targetText` 或 `targetsText`
- 目标要跨阶段复用时，先记录数据库，再用数据库 key
- 不要把显示文本、翻译 key、消息 key 当成目标 key
- 不要假设某个 key 天然存在；使用前要确认它来自上下文或数据库

### targetText 与 targetsText

- `targetText`：单目标字段，常见于 `DialogCondition_Target` 类条件，例如技能、特性、站位、容器状态检查
- `targetsText`：多目标字段，常见于 `CQFAction_Target` 类动作，例如记录、生成、伤害、销毁、开箱

写法判断：
- 条件检查一个对象：通常用 `targetText`
- 动作处理一组对象或位置：通常用 `targetsText`
- 动作链只依赖当前触发上下文：通常不写目标字段

### CQFAction_Condition 中的目标

`CQFAction_Condition` 不会创建新目标。

它的 `conditions` 和 `actions` 使用同一份当前 `targets`：
- 条件里用 `Trigger`，动作里仍可用同一个 `Trigger`
- 条件里用 `CustomThing`，动作里仍可用同一个 `CustomThing`
- 如果条件或动作需要额外目标，必须提前记录数据库或由上游动作传入

注意：
- `CQFAction_Condition` 只有“满足时执行”
- 它没有 `else`
- 失败分支要用另一个互斥的 `CQFAction_Condition`，通常配合 `DialogCondition_Reversal`

### 自定义事物默认目标速查

常见 CQF 自定义事物会在触发条件或动作时自动传入目标 key。写动作链时先看默认目标，不要重复记录或引用不存在的 key。

| 来源 | 默认目标 | 说明 |
|------|----------|------|
| `InteractableThing` 条件与结果动作 | `Trigger`、`CustomThing` | `Trigger` 是交互 Pawn；`CustomThing` 是当前交互物 |
| `LootBox` 的 `Open` 组件动作 | `CustomThing` | 当前打开的战利品箱；开箱 loot 不会自动变成 `Inner` |
| `CustomTrap.StepOn` | `CustomThing`、`Trigger` | `CustomThing` 是陷阱；`Trigger` 是踩中的 Pawn |
| `CustomTrap.Signal/Tick/Damaged` | `CustomThing` | 没有 Pawn 触发者时不要引用 `Trigger` |
| `CustomDoor.openingConditions` | `Trigger`、`CustomThing` | `Trigger` 是尝试开门的 Pawn；`CustomThing` 是门 |
| `CustomDoor.openingActions` | `CustomThing` | 开门动作默认没有 `Trigger` |
| `CustomContainer.openingConditions/actions` | `CustomThing`、`Inner` | `Inner` 是容器打开前缓存的内部对象，可能为空 |
| `CompActionWorker.Spawn/Tick/Signal/Damaged/Open` | `CustomThing` | 组件所在的 parent Thing |
| `CompActionWorker.Destroy` | `CustomThing` | 被销毁对象原位置的 `TargetInfo`，不一定还能当实体操作 |
| `GenerationActionWorker` | `Position` | 地图生成时 worker 所在位置 |
| `FinishRect` 矩形内动作 | `Position` | 当前遍历到的格子 |
| `DoActionForGroup` 子动作 | `Target` | 当前群组成员 |

判断规则：
- 当前触发上下文已经提供的目标，不需要先记录数据库
- 延迟执行、信号联动、跨阶段复用时，才优先记录数据库
- `Trigger` 只在确实有 Pawn 触发者的场景可用
- `Inner` 只在容器类场景可靠，且要考虑为空
- 当前自定义事物自毁通常用 `CustomThing`

### 目标解析顺序

CQF 目标通常按以下顺序解析：
1. 当前传入的 `targets`
2. `quest database`
3. `temporary database`
4. `global database`

因此：
- 写动作链时，不要假设 key 天然存在
- 缺 key 时，应先 `RecordToDatabase`

## 数据库系统

### quest database

适合：
- 当前任务流程内的对象、位置、布尔值、群组

常用场景：
- 记录入口
- 记录出口
- 记录 Boss
- 记录关键交互物
- 记录任务阶段标记

### temporary database

适合：
- 当前交互
- 当前开箱
- 当前生成流程

常用场景：
- 某次交互链里的临时目标
- 某次地图生成中的中间对象

### global database

适合：
- 跨任务长期状态
- 全局剧情开关
- 全局唯一对象引用

### 常见误用

- 长期状态写进 `temporary database`
- 交互临时目标写进 `global database`
- 记录对象后忘了统一 `recordKey`
- 后续动作引用了错误 key

## 信号系统

### signal 的作用

信号是 CQF 的事件总线。

适合：
- 开门
- 激活出口
- 切换阶段
- 刷下一波
- 通知任务节点
- 对象之间联动

### 命名建议

信号名尽量体现：
- 阶段
- 发送者
- 结果

例如：
- `Stage1Completed`
- `BossRoomUnlocked`
- `TerminalActivated`
- `ExitEnabled`

### Quest 前缀

很多动作支持：
- `addQuestPrefix`

作用：
- 自动把信号变成 `Quest{id}.SignalName`

适合：
- 要让任务节点接收信号时

### 信号自查

每次设计信号时都要确认：
- 谁发信号
- 谁监听信号
- 是否要加 Quest 前缀
- 是否只在当前地图部件有效

## 参数阅读规则

在 CQF 里，真正决定 XML 可写字段的不是类名直觉，而是源码里的：
- `SaveToXElement()`
- `ExposeData()`

因此写 XML 时遵守：
- 不要猜字段名
- 不要把翻译文本字段和内部 key 混用
- `targetsText`、`recordKey`、`signal` 通常是内部标识，不是显示文本
- `targetText` 指向单个目标 key
- `targetsText` 指向目标 key 列表

参数理解的统一方式：
- `输入目标`
- 这个动作 / 条件会从哪里取目标
- `关键参数`
- XML 里最核心的字段
- `参数含义`
- 字段到底控制什么
- `典型组合`
- 和哪些动作 / 条件最常连用
- `常见坑`
- 最容易配错的地方

## CQFAction 分类

### 流程控制类

- `CQFAction_Sequence`
- `CQFAction_Random`
- `CQFAction_Condition`
- `CQFAction_Chance`
- `CQFAction_Loop`
- `CQFAction_DelayExecute`

### 信号 / 状态类

- `CQFAction_SentSignal`
- `CQFAction_SetBool`
- `CQFAction_SetGlobalBool`
- `CQFAction_AddQuestTag`

### 数据记录类

- `CQFAction_RecordToDatabase`
- `CQFAction_GetThingToRecord`
- `CQFAction_GetCellToRecord`
- `CQFAction_RecordToGroup`
- `CQFAction_RecordStartCell`
- `CQFAction_FinishRect`

### 生成 / 刷出类

- `CQFAction_Spawn`
- `CQFAction_SpawnCustomThing`
- `CQFAction_SpawnAndAddToInventory`
- `CQFAction_SpawnAndAddToContainer`
- `CQFAction_ReleaseFromContainer`

### 任务 / 地图 / 事件类

- `CQFAction_Quest`
- `CQFAction_Incident`
- `CQFAction_GenerateSubMap`
- `CQFAction_LinkEntranceAndExit`
- `CQFAction_ActivateCustomMap`
- `CQFAction_SwtichEntranceStatus`

### Pawn / 关系 / 派系类

- `CQFAction_Hediff`
- `CQFAction_SetCustomHediff`
- `CQFAction_Trait`
- `CQFAction_RemoveTrait`
- `CQFAction_UpgradeTrait`
- `CQFAction_Ability`
- `CQFAction_SetDuty`
- `CQFAction_SetXenotype`
- `CQFAction_StartMentalState`
- `CQFAction_Faction`
- `CQFAction_SetRelation`
- `CQFAction_ChangeGoodwillOfFaction`

### 传送 / 位置类

- `CQFAction_Skip`
- `CQFAction_SkipToPlayerMap`

### 消息 / 对话类

- `CQFAction_Message`
- `CQFAction_StartDialog`

### 伤害 / 环境 / 销毁类

- `CQFAction_Explosion`
- `CQFAction_Lightning`
- `CQFAction_TakeDamage`
- `CQFAction_Destory`
- `CQFAction_Fog`
- `CQFAction_FloodUnfog`
- `CQFAction_Pollute`

## 高频 CQFAction 参数详解

### CQFAction_Sequence

作用：
- 顺序执行一串动作

输入目标：
- 不额外取目标
- 直接把当前 `targets` 原样传给所有子动作

关键参数：
- `actions`（`List<CQFAction>`）：子动作列表，按顺序执行

适合：
- 固定流程
- 标准事件链
- 开门后连续做三四件事

典型组合：
- `Message -> RecordToDatabase -> SentSignal`
- `SetBool -> Message -> Spawn`

常见坑：
- 以为它会自动做条件判断；实际上它只负责顺序执行

### CQFAction_Random

作用：
- 从动作列表里随机选一个执行

输入目标：
- 继承当前 `targets`

关键参数：
- `actions`（`List<CQFAction>`）：候选动作列表，随机挑一个执行

适合：
- 随机奖励
- 随机事件
- 随机支线效果

常见坑：
- 想做加权概率时不要只用它；需要加权时通常用多个 `Chance` 或外部概率控制

### CQFAction_Condition

作用：
- 条件全部满足时才执行动作列表

输入目标：
- 条件和动作都使用当前 `targets`
- 它不会创建新目标，也不会自动记录目标

关键参数：
- `conditions`（`List<DialogCondition>`）：需要全部通过的条件列表
- `actions`（`List<CQFAction>`）：条件通过后执行的动作列表

适合：
- 运行时分支
- 条件通过才生成内容
- 条件通过才发信号

典型组合：
- `DatabaseExists -> SpawnCustomThing`
- `Bool -> SentSignal`
- `SkillCheck -> Message / Spawn / SentSignal`

常见坑：
- 把“失败后要执行什么”也写进这里；它只处理“满足时执行”
- 以为条件里的目标会自动变成动作目标；实际上条件和动作只是共享当前传入的 `targets`
- 需要失败分支时，应另写一个互斥的 `CQFAction_Condition`

### CQFAction_Chance

作用：
- 按概率执行一个动作

输入目标：
- 当前 `targets`

关键参数：
- `action`（`CQFAction`）：被概率包裹的单个动作
- `chance`（`float`）：概率值，通常为 `0~1`

适合：
- 稀有掉落
- 机关额外效果
- 偶发奖励 / 惩罚

常见坑：
- `chance` 不是百分比文本，`0.5` 才是 50%

### CQFAction_Loop

作用：
- 循环执行动作列表

输入目标：
- 当前 `targets`

关键参数：
- `loopCount`（`int`）：循环次数
- `actions`（`List<CQFAction>`）：每次循环都执行的动作列表

适合：
- 连续生成
- 连续判定
- 固定次数刷波次

常见坑：
- 循环里如果动作自身会累积副作用，要确认是否真的需要重复多次

### CQFAction_DelayExecute

作用：
- 延迟一段时间后执行动作列表

输入目标：
- 当前 `targets`

关键参数：
- `delayTime`（`int`）：延迟 tick 数
- `actions`（`List<CQFAction>`）：延迟后执行的动作列表

适合：
- 延时爆炸
- 延时刷怪
- 延时提示

常见坑：
- 延迟后目标可能已不存在，因此依赖对象时应先记录数据库

### CQFAction_SentSignal

作用：
- 发送一个信号

输入目标：
- 不直接消费目标，但影响谁来接这个信号

关键参数：
- `signal`（`string`）：信号名，内部标识，不翻译
- `signalIsOnlyValidInPart`（`bool`）：是否只在当前地图部件内视为有效
- `addQuestPrefix`（`bool`）：是否自动拼上 `Quest{id}.`

适合：
- 开门
- 激活出口
- 推进阶段
- 通知任务节点

典型组合：
- `InteractableThing -> SentSignal`
- `LootBox(Open) -> SentSignal`
- `Trap -> SentSignal`

常见坑：
- 发了信号，但没人监听
- 忘了 `addQuestPrefix`
- 把显示文本拿来当稳定信号名

### CQFAction_SetBool

作用：
- 设置任务布尔值

输入目标：
- 不依赖目标

关键参数：
- `keyOfBool`（`string`）：当前任务数据库里的布尔值 key
- `valueOfBool`（`bool`）：设置成 `true` 或 `false`

适合：
- 阶段锁
- 一次性事件标记

常见坑：
- 想跨任务持久化时不要用它，要用 `SetGlobalBool`

### CQFAction_SetGlobalBool

作用：
- 设置全局布尔值

输入目标：
- 不依赖目标

关键参数：
- `keyOfBool`（`string`）：全局布尔值 key
- `valueOfBool`（`bool`）：设置成 `true` 或 `false`

适合：
- 全局剧情状态
- 跨任务解锁

常见坑：
- 本地图临时流程不要滥用全局布尔值

### CQFAction_Message

作用：
- 给玩家显示消息

输入目标：
- 会利用当前 `targets` 做命名参数替换

关键参数：
- `message`（`string`）：消息 key 或文本
- `type`（`MessageTypeDef` / `Def`）：消息类型

适合：
- 反馈
- 警告
- 成功 / 失败提示

典型组合：
- `Condition -> Message`
- `Spawn -> Message`

常见坑：
- 文本硬编码中文
- 忘记它会基于目标做格式化，导致 key 写错

### CQFAction_RecordToDatabase

作用：
- 把目标写入数据库

输入目标：
- 从 `targetsText` 取目标

关键参数：
- `targetsText`（`List<string>`）：要记录哪些目标 key，可以是一个或多个
- `recordKey`（`string`）：存到数据库中的新名字
- `recordToQuestBase`（`bool`）：是否写入任务数据库
- `recordToTemporaryBase`（`bool`）：是否写入临时数据库
- `recordToGlobalBase`（`bool`）：是否写入全局数据库

适合：
- 记录入口
- 记录出口
- 记录钥匙
- 记录 Boss
- 记录关键位置

典型组合：
- `RecordToDatabase -> DatabaseExists`
- `RecordToDatabase -> SentSignal`

常见坑：
- 后续读取时要用 `recordKey`，不是原 `targetsText`
- 三种数据库别乱选

### CQFAction_GetThingToRecord

作用：
- 从目标位置上取 Thing，再按记录逻辑写数据库

输入目标：
- 从 `targetsText` 取一个位置或可转成位置的目标

关键参数：
- `targetsText`（`List<string>`）：要取对象的位置 key
- `recordKey`（`string`）：记录到数据库的 key
- `recordToQuestBase`（`bool`）：是否写入任务数据库
- `recordToTemporaryBase`（`bool`）：是否写入临时数据库
- `recordToGlobalBase`（`bool`）：是否写入全局数据库

适合：
- 记住当前位置上的门
- 记住某位置新刷出的对象

常见坑：
- 如果目标位置上没有目标 Thing，就不会得到预期结果

### CQFAction_GetCellToRecord

作用：
- 把目标转换成位置后记录

输入目标：
- 从 `targetsText` 取目标，再转成 Cell

关键参数：
- `targetsText`（`List<string>`）：要转换成位置的目标 key
- `recordKey`（`string`）：记录到数据库的 key
- `recordToQuestBase`（`bool`）：是否写入任务数据库
- `recordToTemporaryBase`（`bool`）：是否写入临时数据库
- `recordToGlobalBase`（`bool`）：是否写入全局数据库

适合：
- 记住入口格
- 记住触发点
- 记住刷怪点

### CQFAction_RecordToGroup

作用：
- 记录目标到群组

输入目标：
- 从 `targetsText` 取目标

关键参数：
- `targetsText`（`List<string>`）：要加入群组的目标 key
- `recordKey`（`string`）：群组名

适合：
- 批量敌人
- 批量对象
- 批量出口 / 陷阱

### CQFAction_RecordStartCell

作用：
- 记录矩形区域起点

输入目标：
- 从 `targetsText` 取位置

关键参数：
- `targetsText`（`List<string>`）：起点来源 key
- `recordKey`（`string`）：起点存储 key

适合：
- 先记区域起点，再交给 `FinishRect`

### CQFAction_FinishRect

作用：
- 结合之前记录的起点，对矩形区域执行动作

输入目标：
- `targetsText` 负责当前终点
- `recordKey` 负责起点

关键参数：
- `targetsText`（`List<string>`）：当前终点来源 key
- `recordKey`（`string`）：之前用 `RecordStartCell` 存的起点 key
- `actions`（`List<CQFAction>`）：对整个矩形区域内格子执行的动作

适合：
- 整房间刷雾
- 整房间刷火
- 批量地格处理

常见坑：
- 忘记先记录起点

### CQFAction_Spawn

作用：
- 用 `LootData` 在目标位置生成内容

输入目标：
- 从 `targetsText` 取位置 / 地图上下文

关键参数：
- `targetsText`（`List<string>`）：生成位置来源 key
- `datas`（`List<LootData>`）：生成模板列表，每项可继续定义物品、Pawn、类别、specialThing

适合：
- 掉落
- 奖励
- 场景物生成

常见坑：
- 这不是生成 CQF 特殊对象的首选；特殊对象优先用 `SpawnCustomThing`

### CQFAction_SpawnCustomThing

作用：
- 在目标位置生成 CQF 自定义物件

输入目标：
- 从 `targetsText` 取位置

关键参数：
- `data`（`CustomThingData`）：一整段自定义物件定义
- `key`（`string`）：可选，生成后记录到数据库的 key

适合：
- 刷门
- 刷箱子
- 刷陷阱
- 刷入口出口

常见坑：
- 普通物品掉落不要滥用它
- `data` 本身内部字段也必须完整，尤其 `def / position / stuff`

### CQFAction_GenerateSubMap

作用：
- 生成子地图

输入目标：
- 从 `targetsText` 取地图上下文

关键参数：
- `targetsText`（`List<string>`）：地图上下文来源 key
- `pos`（`IntVec3`）：子地图生成锚点
- `set`（`CustomMapGenerationSet`）：子地图候选集合

适合：
- 地下室
- 内部设施
- 隐藏房间

典型组合：
- `InteractableThing -> GenerateSubMap`
- `GenerateSubMap -> LinkEntranceAndExit`

常见坑：
- 只生成地图，不补入口出口和后续流程

### CQFAction_LinkEntranceAndExit

作用：
- 把入口和出口绑定到一起

输入目标：
- 从当前 targets 或数据库取入口 / 出口

关键参数：
- `entranceText`（`string`）：指向入口对象的 key
- `exitText`（`string`）：指向出口对象的 key

适合：
- 子地图入口出口配对

常见坑：
- 两边 key 记录不一致

### CQFAction_OpenLootBox

作用：
- 强制打开战利品箱

输入目标：
- 从 `targetsText` 取目标箱子

关键参数：
- `targetsText`（`List<string>`）：目标箱子 key 列表

适合：
- 连锁开箱
- 信号触发奖励

### CQFAction_SetDuty

作用：
- 设置目标 Pawn 的 Duty

输入目标：
- 从 `targetsText` 取 Pawn

关键参数：
- `targetsText`（`List<string>`）：目标 Pawn key 列表
- `duty`（`DutyDef` / `Def`）：要设置的 Duty

适合：
- 驻守
- 巡逻
- 切换行为模式

### CQFAction_Hediff

作用：
- 给目标 Pawn 添加或设置 Hediff

输入目标：
- 从 `targetsText` 取 Pawn

关键参数：
- `targetsText`（`List<string>`）：目标 Pawn key 列表
- `hediff`（`HediffDef` / `Def`）：Hediff 定义
- `severity`（`float`）：严重度
- `bodyPart`（`BodyPartDef` / `Def`）：指定身体部位，可选
- `customLabel`（`string`）：自定义显示名，可选

适合：
- 中毒
- 感染
- 剧情状态

### CQFAction_StartDialog

作用：
- 强制启动正式对话

输入目标：
- 用 `interviewerText / intervieeText` 分别找两个 Thing

关键参数：
- `dialog`（`DialogManagerDef` / `Def`）：对话管理器
- `interviewerText`（`string`）：发起者 key
- `intervieeText`（`string`）：被对话者 key

适合：
- 剧情弹出对话
- 事件强制转入对话树

常见坑：
- 两个目标 key 反了
- 没提前保证对应对象已存在

### CQFAction_Skip

作用：
- 把目标 Thing 传送到另一个目标位置

输入目标：
- `skipedTargetText`
- 被传送对象
- `targetLocationText`
- 目标位置

关键参数：
- `skipedTargetText`（`string`）：被传送对象 key
- `targetLocationText`（`string`）：目标位置 key

适合：
- 传送机关
- 拉人进房间

### CQFAction_SkipToPlayerMap

作用：
- 把目标传回玩家地图

输入目标：
- `skipedTargetText`

关键参数：
- `skipedTargetText`（`string`）：被传送对象 key

适合：
- 副本结算回传
- 从子地图送回主基地

### CQFAction_Explosion

作用：
- 在目标位置产生爆炸

输入目标：
- 优先使用当前动作链上下文
- 在 `CustomTrap.trapComps.actions` 中通常不要显式写 `targetsText`
- 陷阱触发时，上下文已经提供陷阱对象、触发者或触发位置相关目标

关键参数：
- `radius`（`float`）：爆炸半径
- `amount`（`int`）：伤害值
- `damage`（`DamageDef` / `Def`）：伤害类型

推荐写法：
```xml
<li Class="QuestEditor_Library.CQFAction_Explosion">
  <radius>2.5</radius>
  <amount>28</amount>
  <damage>Bomb</damage>
</li>
```

常见坑：
- 在陷阱里额外写不存在的 `targetsText` key，导致爆炸位置取错
- 把字段写成 `damageDef`；正确字段是 `damage`
- 把爆炸放在销毁自身之后，导致后续目标上下文不稳定

### CQFAction_Destory

作用：
- 销毁目标对象

输入目标：
- 从 `targetsText` 取要销毁的目标
- 陷阱、交互物、门、箱子等对象自己的动作链中，销毁自身通常使用 `CustomThing`

关键参数：
- `targetsText`（`List<string>`）：要销毁的目标 key

推荐写法：
```xml
<li Class="QuestEditor_Library.CQFAction_Destory">
  <targetsText>
    <li>CustomThing</li>
  </targetsText>
</li>
```

适合：
- 一次性陷阱触发后销毁自身
- 交互物完成使命后移除
- 已解除机关提示后移除，避免重复触发

常见坑：
- 类名拼写是 `Destory`，不是 `Destroy`
- 销毁陷阱自身时写成 `Trigger`，结果销毁触发 Pawn
- 不写 `targetsText`，导致目标不明确
- 把销毁动作放在爆炸、消息、信号等依赖上下文的动作前面

## DialogCondition 分类

### 状态类

- `DialogCondition_Bool`
- `DialogCondition_QuestState`
- `DialogCondition_QuestIsGenerated`

### 数据库 / 群组类

- `DialogCondition_DatabaseExists`
- `DialogCondition_GroupExists`

### Pawn 属性类

- `DialogCondition_Skill`
- `DialogCondition_SkillCheck`
- `DialogCondition_Hediff`
- `DialogCondition_Trait`
- `DialogCondition_Age`
- `DialogCondition_PrisonerOrSlave`
- `DialogCondition_Thought`

### 物品 / 容器 / 位置类

- `DialogCondition_Inventory`
- `DialogCondition_ThingInPosition`
- `DialogCondition_ContainerIsFull`
- `DialogCondition_CapturedPawn`

### 逻辑容器类

- `DialogCondition_And`
- `DialogCondition_Or`
- `DialogCondition_Reversal`

### 概率类

- `DialogCondition_Chance`

## 高频 DialogCondition 参数详解

### DialogCondition_Bool

作用：
- 检查布尔值是否成立

输入目标：
- 不依赖目标

关键参数：
- `boolName`（`string`）：要检查的布尔值 key
- `failReason`（`string`）：失败时显示文本

适合：
- 阶段锁
- 一次性事件门

常见坑：
- 不区分任务 bool 和全局 bool 的来源语义

### DialogCondition_DatabaseExists

作用：
- 检查数据库里是否存在某个 key

输入目标：
- 不直接用 `targetText`
- 直接按 `targetKey` 去指定数据库查

关键参数：
- `targetKey`（`string`）：数据库 key
- `needSpawned`（`bool`）：若记录的是对象，是否要求它仍存在 / 已生成
- `checkQuestDatabase`（`bool`）：查任务数据库
- `checkTemporaryDatabase`（`bool`）：查临时数据库
- `checkGlobalDatabase`（`bool`）：查全局数据库
- `failReason`（`string`）：失败时显示文本

适合：
- 检查对象是否已记录
- 检查关键位置是否已准备好

常见坑：
- 数据明明写进 quest database，却只查 temporary

### DialogCondition_GroupExists

作用：
- 检查目标或群组是否存在

输入目标：
- `targetText`

关键参数：
- `targetText`（`string`）：当前检查目标 key
- `targetKey`（`string`）：group 的 key
- `needSpawned`（`bool`）：是否要求成员仍然存在
- `failReason`（`string`）：失败时显示文本

适合：
- 多目标流程
- 批量敌人流程

### DialogCondition_QuestState

作用：
- 检查某个任务状态

关键参数：
- `quest`（`QuestScriptDef` / `Def`）：任务定义
- `state`（`string` / `enum-like`）：任务状态
- `failReason`（`string`）：失败时显示文本

适合：
- 任务阶段门

### DialogCondition_Skill

作用：
- 固定技能门槛

输入目标：
- `targetText`

关键参数：
- `targetText`（`string`）：要检查哪个 Pawn
- `skill`（`SkillDef` / `Def`）：技能定义
- `level`（`int`）：门槛值
- `needToBeGreater`（`bool`）：`>=` 还是 `<=`
- `failReason`（`string`）：失败时显示文本

适合：
- 开锁
- 挖掘
- 分析

### DialogCondition_SkillCheck

作用：
- 概率型技能检定

输入目标：
- `targetText`

关键参数：
- `targetText`（`string`）：要检查哪个 Pawn
- `skill`（`SkillDef` / `Def`）：技能定义
- `checkModifier`（`float`）：检定修正值，影响成功率
- `failReason`（`string`）：失败时显示文本

适合：
- 破解
- 说服
- 风险交互

常见坑：
- 把它当固定门槛；它本质是检定

### DialogCondition_Hediff

作用：
- 检查目标是否有某个 Hediff 及其严重度

输入目标：
- `targetText`

关键参数：
- `targetText`（`string`）：目标 Pawn key
- `hediff`（`HediffDef` / `Def`）：Hediff 定义
- `severity`（`float`）：严重度门槛
- `needToBeGreater`（`bool`）：是否要求大于等于该严重度
- `failReason`（`string`）：失败时显示文本

适合：
- 状态门
- 感染 / 中毒分支

### DialogCondition_Trait

作用：
- 检查 Trait 及程度

输入目标：
- `targetText`

关键参数：
- `targetText`（`string`）：目标 Pawn key
- `trait`（`TraitDef` / `Def`）：Trait 定义
- `degree`（`int`）：Trait 程度
- `needToBeGreater`（`bool`）：是否按大于等于判定
- `accurate`（`bool`）：是否严格按 degree 精确匹配
- `failReason`（`string`）：失败时显示文本

适合：
- 特性分支
- 人物专属选项

### DialogCondition_Inventory

作用：
- 检查目标背包物品

输入目标：
- `targetText`

关键参数：
- `targetText`（`string`）：目标 Pawn key
- `requirations`（`List<CQFThingDefCount>`）：需要的物品列表
- `failReason`（`string`）：失败时显示文本

适合：
- 交付
- 门禁卡
- 祭品

常见坑：
- 只检查有无，不处理消耗；消耗要另用 `ConsumeInInventory`

### DialogCondition_ThingInPosition

作用：
- 检查目标是否位于某位置

输入目标：
- `targetText`
- `positionName`

关键参数：
- `targetText`（`string`）：被检查对象
- `positionName`（`string`）：目标位置 key
- `failReason`（`string`）：失败时显示文本

适合：
- 站位机关
- 放置谜题
- 运输逻辑

### DialogCondition_ContainerIsFull

作用：
- 检查容器是否有内容

输入目标：
- `targetText`

关键参数：
- `targetText`（`string`）：目标容器 key
- `failReason`（`string`）：失败时显示文本

适合：
- 容器状态判断

### DialogCondition_CapturedPawn

作用：
- 检查捕获容器中是否有 Pawn

输入目标：
- `targetText`

关键参数：
- `targetText`（`string`）：目标容器 key
- `failReason`（`string`）：失败时显示文本

适合：
- 捕获后续流程

### DialogCondition_Chance

作用：
- 概率判定

关键参数：
- `chance`（`float`）：概率值，通常为 `0~1`
- `failReason`（`string`）：失败时显示文本

适合：
- 结果随机分支

### DialogCondition_And

作用：
- 所有子条件都满足才通过

关键参数：
- `conditions`（`List<DialogCondition>`）：子条件列表

适合：
- 高级门禁
- 多前置要求

### DialogCondition_Or

作用：
- 任一条件满足即通过

关键参数：
- `conditions`（`List<DialogCondition>`）：子条件列表

适合：
- 多解法
- 备用入口

### DialogCondition_Reversal

作用：
- 反转一个条件

关键参数：
- `condition`（`DialogCondition`）：要被反转的单个条件

适合：
- “未完成时才允许”
- “没有某物时才显示”

## 典型组合

### 组合 1：记录后再判断

链路：
- `CQFAction_RecordToDatabase`
- `DialogCondition_DatabaseExists`

适合：
- 先记住入口 / 钥匙 / Boss / 位置
- 后续再判定是否存在

### 组合 2：条件通过后发信号

链路：
- `DialogCondition_*`
- `CQFAction_SentSignal`

适合：
- 开门
- 激活出口
- 推阶段

### 组合 3：交付物品换奖励

链路：
- `DialogCondition_Inventory`
- `CQFAction_ConsumeInInventory`
- `CQFAction_Spawn` 或 `CQFAction_GainMood`

适合：
- 提交材料
- 交换物资
- 祭品

### 组合 4：技能检定决定后果

链路：
- `DialogCondition_SkillCheck`
- `CQFAction_Condition`
- 成功 / 失败动作链

适合：
- 破解
- 侦查
- 修理

### 组合 5：阶段布尔值控制流程

链路：
- `CQFAction_SetBool`
- `DialogCondition_Bool`

适合：
- 剧情阶段
- 一次性事件
- 任务推进

### 组合 6：群组批量处理

链路：
- `CQFAction_RecordToGroup`
- `CQFAction_DoActionForGroup`

适合：
- 一批敌人
- 一批对象
- 一批出口 / 陷阱

## 设计运行时逻辑时的默认顺序

1. 先定目标
- 谁是 `Trigger`
- 谁是 `CustomThing`
- 是否需要额外 key

2. 再定条件
- 这个逻辑是否应该先判定

3. 再定动作
- 成功后做什么
- 失败后做什么

4. 再定数据库
- 哪些对象或位置需要跨阶段保留

5. 再定信号
- 是否要通知下一步

## 常见坑

- 把 `Condition` 当成 `Action`
- 把 `Action` 当成显示条件
- 没记录对象就直接引用数据库 key
- `recordKey` 命名混乱，后续动作接不上
- 发了信号但没人监听
- 忘记 `addQuestPrefix`
- `targetsText` 写了不存在的 key
- 长期状态写进临时数据库
- 需要通用逻辑时却把它硬绑进地图对象
- 靠猜字段名写 XML，而不查真实字段

## 命名建议

### recordKey

建议使用稳定、语义化命名：
- `MainEntrance`
- `BossPawn`
- `KeyTerminal`
- `ExitCell`
- `RewardChest`

避免：
- `aaa`
- `test`
- `key1`
- 中文和英文随意混搭

### signal

建议按“阶段 / 对象 / 结果”命名：
- `Stage1Started`
- `BossRoomUnlocked`
- `TerminalActivated`
- `ExitEnabled`

## 工作方式

处理 CQF 行为 / 条件任务时，按以下顺序：

1. 先说明方案
- 先说准备读哪些类或 Def
- 先说逻辑应该落在动作、条件、数据库还是信号

2. 先判断归属
- 通用运行时逻辑
- 地图专属逻辑
- 对话专属逻辑

3. 优先复用现有类
- 能组合解决，就不要先加新类

4. 输出时写清：
- 输入目标
- 条件
- 动作
- 数据库
- 信号

## 文本规则

必须遵守：
- 不要硬编码中文显示文本
- 必须做双语
- 消息、提示、结果文本都应走翻译 key
- `signal`、`recordKey`、`targetsText` 这类内部标识不应当作显示文本

## 目标

本 Skill 的目标是：
- 让 AI 理解 CQF 的通用运行时体系
- 让 AI 能独立设计可复用的动作链和条件链
- 让地图 Skill 和对话 Skill 不再重复承载整套运行时说明
- 让 AI 不必每次重查源码才能处理 CQF 行为与条件
