# CQF 任务书系统架构

## 1. 对象层级

```text
QuestBookDef（任务书定义）
└── QuestBookChapter（章节）
    └── QuestBookStep（步骤/节点）
        ├── QuestBookObjective（目标基类）
        │   ├── QuestBookObjective_Signal
        │   ├── QuestBookObjective_Resource
        │   ├── QuestBookObjective_Building
        │   └── QuestBookObjective_Research
        ├── CQFThingDefCount（步骤实际奖励）
        ├── QuestBookRewardInfo（奖励展示信息）
        ├── 步骤行为
        └── 后续步骤链接
```

章节和步骤是两层不同对象。章节负责组织步骤，节点画布显示章节内部的步骤；目标只存在于步骤内部。

定义和运行时状态分离：

```text
QuestBookDef
└── QuestBookInstance
    ├── QuestBookChapterState
    ├── QuestBookStepState
    └── QuestBookObjectiveProgress
```

## 2. QuestBookDef

`QuestBookDef` 继承 RimWorld 的 `Def`，是任务书的根定义。

| 字段 | 类型 | 含义 |
|---|---|---|
| `defName` | `string` | 任务书定义名称 |
| `label` | `string` | 任务书显示名称 |
| `description` | `string` | 任务书描述 |
| `questDef` | `QuestScriptDef` | 可选的原版 Quest 定义 |
| `questVisibility` | `QuestBookQuestVisibility` | 任务书与原版 Quest 的显示关系 |
| `completionAuthority` | `QuestBookCompletionAuthority` | Quest 或任务书的完成权 |
| `autoStart` | `bool` | 是否在新游戏或读档时自动创建实例 |
| `allowSkip` | `bool` | 是否允许跳过任务书 |
| `chapters` | `List<QuestBookChapter>` | 章节列表 |
| `onStartActions` | `List<CQFAction>` | 任务书开始行为 |
| `onCompleteActions` | `List<CQFAction>` | 任务书完成行为 |
| `onFailActions` | `List<CQFAction>` | 任务书失败行为 |

`QuestBookQuestVisibility` 包含 `QuestAndBook`、`BookOnly`、`QuestOnly`、`Internal`。`QuestBookCompletionAuthority` 包含 `Quest`、`QuestBook`、`Either`。

## 3. QuestBookChapter

| 字段 | 类型 | 含义 |
|---|---|---|
| `id` | `string` | 章节标识 |
| `labelKey` | `string` | 章节名称或翻译 Key |
| `descriptionKey` | `string` | 章节描述或翻译 Key |
| `steps` | `List<QuestBookStep>` | 本章节的步骤列表 |
| `onUnlockActions` | `List<CQFAction>` | 章节解锁行为 |
| `onCompleteActions` | `List<CQFAction>` | 章节完成行为 |

`Label` 和 `Description` 会把翻译 Key 转换为玩家可见文本。

## 4. QuestBookStep

`QuestBookStep` 是任务书节点，也是目标和奖励的容器。

| 字段 | 类型 | 含义 |
|---|---|---|
| `id` | `string` | 系统生成的步骤标识 |
| `labelKey` | `string` | 步骤名称或翻译 Key |
| `descriptionKey` | `string` | 步骤描述或翻译 Key |
| `completionMode` | `QuestBookCompletionMode` | 目标完成模式 |
| `objectives` | `List<QuestBookObjective>` | 步骤目标列表 |
| `rewards` | `List<CQFThingDefCount>` | 完成步骤时实际发放的奖励 |
| `rewardInfos` | `List<QuestBookRewardInfo>` | 仅用于展示的奖励信息 |
| `onActivateActions` | `List<CQFAction>` | 步骤激活行为 |
| `onCompleteActions` | `List<CQFAction>` | 步骤完成行为 |
| `onFailActions` | `List<CQFAction>` | 步骤失败行为 |
| `onSkipActions` | `List<CQFAction>` | 步骤跳过行为 |
| `nextStepIds` | `List<string>` | 后续步骤 ID 列表 |
| `position` | `Vector2` | 编辑器节点位置 |
| `iconPath` | `string` | 节点图标贴图路径 |
| `detailImagePaths` | `List<string>` | 详情页图片贴图路径列表 |

`QuestBookCompletionMode` 包含：

- `All`：全部必选目标完成。
- `Any`：任意一个必选目标完成。
- `Manual`：由外部行为手动完成。

`nextStepIds` 保存步骤链接关系。节点画布中的箭头对应这些 ID。

## 5. QuestBookObjective 基类

`QuestBookObjective` 是所有目标类型的基类。目标对象本身包含条件检查、信号处理、参数和编辑绘制，不再持有 `workerClass` 或 Worker 实例。

### 共通字段

| 字段 | 类型 | 含义 |
|---|---|---|
| `labelKey` | `string` | 目标名称或翻译 Key |
| `descriptionKey` | `string` | 目标描述或翻译 Key |
| `iconPath` | `string` | 目标图标贴图路径 |
| `iconManuallySelected` | `bool` | 是否锁定手动选择的目标图标；为 `false` 时选择事物目标可以自动同步图标 |
| `optional` | `bool` | 是否为可选目标 |

### 共通接口

| 成员 | 类型 | 含义 |
|---|---|---|
| `UsesSignal` | `bool` | 是否使用 CQF 信号 |
| `UsesThingTarget` | `bool` | 是否使用事物目标 |
| `UsesResearchTarget` | `bool` | 是否使用研究目标 |
| `UsesTargetCount` | `bool` | 是否使用目标数量 |
| `RequiresCheck` | `bool` | 是否需要运行时主动调用 `Check` 查询世界状态 |
| `GetThingTargets()` | `IEnumerable<ThingDef>` | 编辑器中的事物目标列表 |
| `Process(progress, signal)` | `bool` | 信号到达时处理进度 |
| `Check(progress)` | `bool` | 周期或手动检查进度；只有 `RequiresCheck=true` 的目标会被运行时调用 |
| `Draw(ref y, inRect, x)` | `void` | 绘制目标编辑内容，并在绘制完成后推进 `y` |

目标 XML 使用对象的 `Class` 属性保存具体子类类型，因此目标列表可以保存不同的目标子类。

## 6. 目标子类

### QuestBookObjective_TargetCount

数量扩展基类，字段为：

| 字段 | 类型 | 含义 |
|---|---|---|
| `targetCount` | `int` | 达成目标所需数量，最小值为 1 |

资源、建筑和信号目标继承这个扩展类。研究目标不继承它，因此没有目标数量参数。

### QuestBookObjective_ThingTarget

事物目标扩展基类，字段为：

| 字段 | 类型 | 含义 |
|---|---|---|
| `targetThingDef` | `ThingDef` | 被检查的资源或建筑定义 |

### QuestBookObjective_Signal

| 字段 | 类型 | 含义 |
|---|---|---|
| `signal` | `string` | 匹配的 CQF 信号标签 |
| `targetCount` | `int` | 匹配信号需要累计的次数 |

信号到达时，目标比较信号标签。标签完全相同，或信号标签以 `.` 分隔后缀匹配时，进度增加。

### QuestBookObjective_Resource

| 字段 | 类型 | 含义 |
|---|---|---|
| `targetThingDef` | `ThingDef` | 资源定义 |
| `targetCount` | `int` | 所需资源数量 |

资源数量来自原版 `Map.resourceCounter.GetCount(targetThingDef)`，检查范围是玩家殖民地地图。该目标的 `RequiresCheck` 为 `true`。

### QuestBookObjective_Building

| 字段 | 类型 | 含义 |
|---|---|---|
| `targetThingDef` | `ThingDef` | 建筑定义 |
| `targetCount` | `int` | 所需建筑数量 |

建筑数量来自玩家殖民地地图的 `listerBuildings.AllBuildingsColonistOfDef(targetThingDef)`。该目标的 `RequiresCheck` 为 `true`。

### QuestBookObjective_Research

| 字段 | 类型 | 含义 |
|---|---|---|
| `targetResearch` | `ResearchProjectDef` | 研究项目定义 |

研究目标通过 `targetResearch.IsFinished` 判断完成，不包含 `targetCount`。该目标的 `RequiresCheck` 为 `true`。

## 7. 目标编辑绘制

`QuestBookObjective.Draw(ref y, Rect inRect, float x)` 是目标编辑器的完整入口，基类在这里绘制名称、描述、图标、完成规则和通用布局，并调用 `DrawSpecial(ref y, Rect inRect, float x)`。目标子类只通过 `DrawSpecial` 绘制专属检测内容。事物选择器只存在于 `QuestBookObjective_ThingTarget`，研究选择器只存在于 `QuestBookObjective_Research`，其他目标不会显示目标选择控件。绘制完成后推进 `y`，详细窗口只负责提供滚动容器，不再自行拼接目标字段。

目标类型在步骤编辑器点击“添加目标”时通过 `Dialog_Select<Type>` 选择。选择后创建对应的 `QuestBookObjective_*` 实例并加入步骤，打开详细窗口后不再允许切换目标类型。

目标编辑窗口只提供滚动容器并调用目标的 `Draw`。窗口使用目标本次绘制结束后的 `y` 保存内容高度，不使用与目标类型无关的固定超大高度，因此不同目标类型的窗口内容区域会按实际绘制结果适配。目标选择事物后，仅当 `iconManuallySelected=false` 时才会把事物贴图同步到 `iconPath`；手动选择事物贴图或已加载图片会将该字段设为 `true`，清除图标会恢复自动同步状态。

## 8. 奖励和行为

步骤实际奖励是 `CQFThingDefCount` 列表，完成步骤时实际投递。奖励展示信息是 `QuestBookRewardInfo`，字段为 `labelKey`、`descriptionKey` 和 `iconPath`，只参与详情页显示。

步骤、章节和任务书保存 `CQFAction` 列表，对应激活、解锁、完成、失败和跳过等生命周期事件。

## 9. 运行时对象

### GameComponent_QuestBook

游戏组件保存全部任务书实例，处理自动激活、Quest 绑定、信号转发、目标检查、步骤/任务书完成、失败和定义热加载。

目标检查周期为 `GenDate.TicksPerDay`。信号目标的 `RequiresCheck` 默认为 `false`，只在信号到达时调用目标的 `Process`；资源、建筑和研究目标将 `RequiresCheck` 覆写为 `true`，每天调用目标的 `Check`。步骤详情窗口的手动检查仍使用同一套筛选逻辑，因此不会对纯 `Process` 目标调用 `Check`。

### Check 的完整流程

1. `GameComponent_QuestBook.GameComponentTick` 读取当前游戏 Tick。未到下一次检查时间时直接返回；到达后把下一次检查时间设为当前 Tick 加 `GenDate.TicksPerDay`。
2. 组件遍历所有状态为 `Active` 的 `QuestBookInstance`，调用实例的 `CheckObjectives()`。
3. 实例只遍历状态为 `Active` 的步骤，并对每个步骤调用 `CheckObjectives(stepId)`。
4. `CheckObjectives(stepId)` 先确认任务书、步骤状态和步骤定义都有效，然后遍历步骤目标。
5. 已经没有进度对象或已经完成的目标会跳过；`objective.RequiresCheck` 为 `false` 的目标也会跳过。
6. 对 `RequiresCheck` 为 `true` 的目标调用 `objective.Check(progress)`。目标子类在这里读取当前世界状态，更新 `progress.currentCount` 和 `progress.completed`。
7. 所有目标检查完后调用 `TryCompleteStep`，按照步骤的 `completionMode` 和非可选目标的完成状态判断是否完成步骤。
8. 如果步骤完成，更新步骤状态，发送完成信封，投递实际奖励，执行完成行为，激活后续步骤，并在满足条件时完成任务书。
9. 步骤详情窗口的“手动检查”按钮直接调用 `CheckObjectives(stepId)`，不会绕过上述流程。

### QuestBookInstance

| 字段 | 类型 | 含义 |
|---|---|---|
| `instanceId` | `string` | 实例标识 |
| `bookDef` | `QuestBookDef` | 对应任务书定义 |
| `boundQuest` | `Quest` | 绑定的原版 Quest，可为空 |
| `state` | `QuestBookState` | 任务书状态 |
| `chapters` | `List<QuestBookChapterState>` | 章节运行时状态 |
| `steps` | `List<QuestBookStepState>` | 步骤运行时状态 |
| `startedTick` | `int` | 开始时间 |
| `completedTick` | `int` | 完成时间 |

`QuestBookState` 包含 `Locked`、`Active`、`Completed`、`Failed`。

### 状态对象

`QuestBookChapterState` 保存 `chapterId` 和章节状态。

`QuestBookStepState` 保存 `chapterId`、`stepId`、步骤状态和目标进度列表。`QuestBookStepStatus` 包含 `Locked`、`Active`、`Completed`、`Failed`、`Skipped`。

`QuestBookObjectiveProgress` 保存 `currentCount` 和 `completed`，不复制目标名称、描述、图标或目标参数。

## 10. 运行流程

任务书实例来自 Quest 绑定或 `autoStart=true` 的自动创建。实例初始化时根据定义建立章节状态、步骤状态和目标进度，然后激活第一个步骤。

信号到达时，运行时遍历当前激活步骤的未完成目标并调用目标的 `Process`。每日检查时调用目标的 `Check`。进度更新后，步骤按照 `completionMode` 判断完成条件。

步骤完成包含：更新步骤状态、投递实际奖励、执行完成行为、激活 `nextStepIds` 中的后续步骤，以及判断章节和任务书是否完成。

## 11. 编辑器对象

| 编辑器对象 | 对应内容 |
|---|---|
| `QuestEditor_QuestBook` | 任务书定义和热加载 |
| `QuestBookChapterSidebar` | 章节列表和章节选择 |
| `QuestBookNodeCanvas` | 当前章节步骤节点、位置、视图和链接 |
| `Dialog_EditQuestBookChapter` | 章节属性 |
| `Dialog_EditQuestBookStep` | 步骤属性、目标、奖励和行为 |
| `Dialog_EditQuestBookObjective` | 提供滚动容器并调用目标自身的绘制接口 |
| `Dialog_EditQuestBookRewardInfo` | 奖励展示信息 |
| `Dialog_QuestBookStepInfo` | 运行时步骤详情 |

节点位置保存于 `QuestBookStep.position`，节点链接保存于 `QuestBookStep.nextStepIds`。图标和详情图片保存为贴图路径，加载时重新取得纹理。

## 12. 代码位置

- 定义对象：`.QuestEditor_Library/QuestEditor_Library/QuestBook/Def/`
- 目标子类：`.QuestEditor_Library/QuestEditor_Library/QuestBook/Objective/`
- 运行时：`.QuestEditor_Library/QuestEditor_Library/QuestBook/QuestBookRuntime.cs`、`Runtime/`
- 行为：`.QuestEditor_Library/QuestEditor_Library/QuestBook/Action/`
- 编辑器：`.QuestEditor_Library/QuestEditor_Library/QuestBook/Editor/`
- 任务书窗口：`.QuestEditor_Library/QuestEditor_Library/QuestBook/`
