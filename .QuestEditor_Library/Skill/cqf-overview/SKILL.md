---
name: "cqf-overview"
description: "Explains CQF systems, responsibilities, and extension entry points. Invoke when tasks involve CQF submods, runtime logic, quests, custom things, or deciding which CQF subsystem to use."
---

# CQF Overview

用于 `CQF / QuestEditor_Library` 的总体认知 Skill。

本 Skill 的目标不是代替源码，而是让 AI 在处理 CQF 需求时先知道：
- `CQF` 能做什么
- 各系统分别负责什么
- 某个需求应该落在哪一层
- 什么时候该继续调用地图 Skill
- 什么时候该扩展 `CQFAction`、`DialogCondition`、`QuestNode` 或自定义物件

## 调用时机

在以下情况调用本 Skill：
- 用户要做 `CQF` 子 Mod
- 用户要扩展 `CQF` 本体
- 用户要判断某个需求该用哪套 CQF 系统
- 用户要做交互、战利品箱、陷阱、任务联动、信号逻辑、数据库记录
- 用户不确定应该使用 `CQFAction`、`DialogCondition`、`QuestNode`、`CustomThing` 还是地图系统
- 用户要快速了解 CQF 的结构与能力

如果任务明确是“做 CQF 地图 / 子地图 / 区域拼装 / 地图入口出口 / 地图流程设计”，优先继续调用 `cqf-map-dev`。

## 仓库与信息源

CQF 源码仓库：
- `D:\Code\Git\QuestEditor_Library`

CQF Mod 包：
- `D:\Game\Steam\steamapps\common\RimWorld\Mods\CQF`

默认策略：
- 需要理解框架能力、扩展入口、运行模型时，优先看源码仓库
- 需要理解已发布 Def、翻译、版本目录和资源落点时，优先看 Mod 包
- 已有稳定入口时，不要每次全仓重扫

## CQF 是什么

CQF 不是单一玩法 Mod，而是一套用于制作以下内容的框架：
- 自定义地图与子地图
- 地图入口、出口、区域拼装
- 交互对象、陷阱、门、容器、战利品箱
- 条件判断与行为执行
- 对话与交互结果
- 任务节点与任务联动
- 数据库存档与信号驱动流程
- 编辑器式内容制作

理解 CQF 的关键点：
- 它不是只靠单个 Def 生效
- 它的大部分玩法来自“对象 + 条件 + 动作 + 信号 + 数据库”的组合
- 它本质上是一套领域脚本系统

## CQF 的主要系统

### 1. 行为系统

核心对象：
- `CQFAction`
- `CQFAction_Target`

作用：
- 执行副作用
- 改变世界状态
- 生成对象
- 发信号
- 发任务
- 改 Pawn 状态
- 记录目标
- 推进地图与剧情流程

一句话理解：
- `CQFAction = 做事`

适用场景：
- 交互结果
- 陷阱触发
- 开箱结果
- 地图事件
- 任务收到信号后的执行链
- 地图生成后的补动作

### 2. 条件系统

核心对象：
- `DialogCondition`
- `DialogCondition_Target`
- `DialogCondition_Target_Pawn`

作用：
- 判断当前是否满足条件
- 返回失败原因
- 不直接修改世界状态

一句话理解：
- `DialogCondition = 判断能不能做`

适用场景：
- 某个交互选项是否可见或可执行
- 某个结果分支是否命中
- 某个事件是否能触发
- 某个任务阶段是否达成
- 某个对象或目标是否存在

### 3. 自定义物件系统

核心对象：
- `InteractableThing`
- `LootBox`
- `CustomTrap`
- `CompActionWorker`
- `CustomDoor`
- `CustomContainer`
- `Spawner`
- `CustomMapEntrance`
- `CustomMapExit`
- `ZoneCore`

作用：
- 承载地图上的可交互玩法
- 作为事件触发器
- 作为奖励、机关、门禁、刷怪、进入子地图的载体

一句话理解：
- `CustomThing = 地图上的玩法节点`

### 4. 任务系统

核心对象：
- `QuestNode_*`
- `QuestNode_DoCQFActions`
- `QuestNode_Root_CustomMap`
- `QuestNode_RandomCustomMap`

作用：
- 把 CQF 内容接进任务链
- 根据任务状态驱动地图、交互和事件
- 在信号到达时执行行为

一句话理解：
- `QuestNode = 任务流程中的 CQF 入口`

### 5. 数据库系统

主要概念：
- `quest database`
- `global database`
- `temporary database`

作用：
- 保存目标对象
- 保存状态标记
- 保存群组
- 让后续动作和条件继续引用前面产生的对象和状态

一句话理解：
- `database = CQF 流程共享上下文`

### 6. 信号系统

主要概念：
- `signal`
- `Quest` 前缀
- 对象 `questTags`
- `ActionTriggerMode.Signal`

作用：
- 串联对象与对象
- 串联任务与对象
- 串联地图阶段
- 驱动延后触发与异步流程

一句话理解：
- `signal = CQF 的事件总线`

### 7. 地图系统

主要概念：
- `CustomMapDataDef`
- `CustomMapGenerationSet`
- `ZoneCore`
- `CustomMapEntrance`
- `CustomMapExit`
- `MapParent_Custom`

作用：
- 生成自定义地图
- 生成子地图
- 处理区域拼装
- 处理入口出口和地图切换

一句话理解：
- `map system = CQF 的地图生产体系`

地图相关任务应继续调用 `cqf-map-dev`。

## CQF 里最重要的三组认知

### 1. Action 和 Condition 的关系

- `Condition` 决定是否允许
- `Action` 负责真的执行
- 一个完整流程通常是：
- 条件判断 -> 结果分支 -> 行为执行 -> 发信号 / 记录数据 -> 后续对象继续响应

### 2. CustomThing 和 QuestNode 的关系

- `CustomThing` 处理地图上“具体对象怎么玩”
- `QuestNode` 处理任务流程里“何时生成、何时触发、何时推进”
- 一个做地图玩法，一个做任务流程调度

### 3. Database 和 Signal 的关系

- `Database` 负责“记住谁是谁”
- `Signal` 负责“通知下一步开始”
- 如果不记录对象，后续动作经常拿不到目标
- 如果不发信号，后续流程经常不会继续

## 常见需求怎么归类

- 需要“玩家主动点击并出现多个选项”时，优先用 `InteractableThing`
- 需要“打开或破坏后给奖励”时，优先用 `LootBox`
- 需要“踩上去 / 受伤 / 定时 / 信号驱动触发”时，优先用 `CustomTrap`
- 需要“给一个普通对象挂被动触发逻辑”时，优先用 `CompActionWorker`
- 需要“任务里控制何时生成或何时执行一串行为”时，优先用 `QuestNode_*`
- 需要“地图生产与地图切换”时，优先用 `cqf-map-dev`
- 需要“新增执行效果”时，优先扩展 `CQFAction_*`
- 需要“新增判定方式”时，优先扩展 `DialogCondition_*`

## 常见 CQF 子 Mod 组成

一个完整的 CQF 子 Mod 通常至少包含以下几层中的若干层：
- Def
- 翻译
- 任务入口
- 自定义对象
- 条件与动作
- 地图或子地图
- 数据库与信号联动

不要把 CQF 理解成“只改一个 XML 就结束”的体系。

## 文本与本地化规则

必须遵守：
- 不要硬编码中文显示文本
- 必须做双语
- DLL / UI 文本优先采用 `Key + 翻译`
- Def 文本要走翻译体系
- UI 类文本优先视为 `Keyed`
- Def 的 `label / description` 优先走 `DefInjected`

## 工作方式

处理 CQF 任务时，按以下顺序工作：

1. 先说明方案
- 先说准备读哪些文件
- 先说准备从哪个系统切入
- 先说预计要改哪些文件

2. 先分类需求
- 总体逻辑
- 地图逻辑
- 对话逻辑
- 交互逻辑
- 奖励逻辑
- 任务逻辑
- 框架扩展

3. 优先复用现有 CQF 模型
- 能用现有 `CQFAction` 组合解决，就不要先加新类
- 能用现有 `DialogCondition` 组合解决，就不要先加新条件
- 能用现有 `CustomThing` 结构解决，就不要先发明新对象类型

4. 地图任务继续调用 `cqf-map-dev`

## 禁忌

- 不要把 `temporary database` 当长期数据库
- 不要没记录目标就直接引用 key
- 不要把 `Condition` 当 `Action`
- 不要把 `Action` 当显示条件
- 不要把交互对象和任务节点混为一谈
- 不要在地图需求里只改一个对象而忽略整条流程
- 不要硬编码中文显示文本

## 目标

本 Skill 的目标是：
- 让 AI 快速理解 CQF 整体能力
- 让 AI 知道某个需求应落在哪套系统
- 让 AI 在真正做地图前，先完成正确的系统归类
- 让 AI 不必每次重新通读 CQF 全仓
