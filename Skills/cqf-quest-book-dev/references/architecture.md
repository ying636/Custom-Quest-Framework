# CQF任务书系统架构

## 一、对象层级

```text
QuestBookDef（任务书定义）
└── QuestBookChapter（章节）
    └── QuestBookStep（步骤/节点）
        ├── QuestBookObjective（目标）
        │   └── QuestBookObjectiveWorker（目标检查器）
        ├── CQFThingData / CQFThingDefCount（实际奖励）
        ├── QuestBookRewardInfo（奖励展示信息）
        ├── nextStepIds（后续步骤链接）
        └── onActivate / onComplete / onFail / onSkip 行为
```

任务书定义和存档进度分开：

```text
QuestBookDef
└── QuestBookInstance（运行时实例）
    ├── QuestBookChapterState
    ├── QuestBookStepState
    └── QuestBookObjectiveProgress
```

定义对象决定“任务书是什么”，实例对象决定“玩家现在进行到哪里”。同一个 Def 可以因为不同 Quest 或自动激活产生不同实例。

## 二、定义对象

### QuestBookDef

任务书的根对象，负责任务书名称、描述、章节列表、自动激活、原版 Quest 绑定、可见性、完成权限和任务书级行为。

### QuestBookChapter

章节是组织层级，不是步骤，包含章节名称、描述、步骤列表以及章节解锁/完成行为。编辑器左侧章节栏只操作章节，节点画布只显示当前章节的步骤。

### QuestBookStep

步骤是任务书中实际显示的节点，包含 id、名称、描述、图标、完成模式、目标列表、实际奖励、奖励展示信息、后续步骤 id 和步骤行为。步骤链接保存步骤 id，便于热加载和存档后重新解析。

### QuestBookObjective

目标属于步骤，包含名称、描述、Worker 类型、资源/建筑目标、研究目标、数量、可选标记和自定义图标。研究目标不保存目标数量。

### QuestBookObjectiveWorker

Worker 把目标定义转换成运行时检查逻辑：

- `QuestBookObjectiveWorker_Signal`：接收 CQF 信号后更新进度。
- `QuestBookObjectiveWorker_Resource`：检查殖民地拥有的指定资源数量。
- `QuestBookObjectiveWorker_Building`：检查地图上指定建筑的建造/拥有数量。
- `QuestBookObjectiveWorker_Research`：检查指定研究是否完成。

新增目标类型应新增 Worker，不要把不同目标类型塞入一个巨型条件类。

### 奖励对象

步骤有两种不同含义的奖励数据：

1. **实际奖励**：`CQFThingData` 或 `CQFThingDefCount`，用于完成步骤时实际发放物品、建筑等内容。
2. **奖励展示信息**：`QuestBookRewardInfo`，只保存玩家看到的图标、名称和描述，不参与发放。

详情页将两者合并显示在同一个“奖励”区域。奖励展示信息显示图标和名称，描述通过鼠标悬停 Tip 查看；实际奖励显示图标和数量。

## 三、运行时对象和状态

### GameComponent_QuestBook

游戏组件是任务书系统的总管理器，负责保存全部实例、新游戏/读档后的自动激活、根据 Quest 创建实例、转发 CQF 信号、周期检查目标、执行任务书完成/失败处理和 Def 热加载刷新。

### QuestBookInstance

运行时实例保存实例 id、`QuestBookDef`、绑定 Quest（可为空）、任务书状态、章节状态列表、步骤状态列表以及开始/完成时间。

### 状态对象

- `QuestBookChapterState`：章节 id 和章节状态。
- `QuestBookStepState`：章节 id、步骤 id、步骤状态和目标进度列表。
- `QuestBookObjectiveProgress`：当前数量和是否完成。

状态对象只保存进度，不复制名称、描述、图标等定义数据；绘制时将定义和状态合并读取。

## 四、运行流程

### 创建任务书

1. 游戏开始或读档时扫描 `QuestBookDef`。
2. `autoStart=true` 的任务书自动创建实例。
3. `autoStart=false` 的任务书由 CQF 行为或 Quest 绑定创建实例。
4. 实例根据章节和步骤定义创建状态，并激活第一个可激活步骤。

### 检查目标

- 信号目标在 CQF 信号到达时处理，只处理当前激活步骤中未完成的信号目标。
- 资源、建筑、研究目标由运行时周期检查，不进行每 Tick 全量扫描。
- 步骤详情窗口可以手动触发当前步骤的条件检查。
- Worker 更新 `QuestBookObjectiveProgress`，再由步骤完成模式判断步骤是否完成。

### 完成步骤

1. 根据完成模式收集必选目标的完成状态。
2. 条件满足后将步骤状态改为完成。
3. 发放步骤实际奖励。
4. 执行步骤完成行为。
5. 根据 `nextStepIds` 激活后续步骤。
6. 如果章节和任务书完成条件满足，继续处理章节/任务书完成行为。

失败或跳过只改变状态并执行对应行为，不删除实例；完成、失败和跳过的任务书仍可查看。

## 五、Quest 接入

任务书通过 QuestNode / QuestPart 接入原版任务系统：

- QuestNode 在任务生成流程中创建或绑定任务书。
- QuestPart 保存任务书实例与 Quest 的绑定关系。
- Quest 状态变化可以驱动任务书完成或失败。
- 任务书内部状态变化可以触发 CQF 行为或影响绑定 Quest。

任务书不要求所有步骤都出现在原版 Quest 任务列表中，可以作为更直观的可视化进度界面。

## 六、奖励发放

步骤完成奖励和 CQF 行为发放奖励都经过奖励投递逻辑：接受所有 `CQFThingData` 子类，解析目标殖民地或地图，并使用原版任务完成机制风格创建空投/奖励投递。发放失败时记录错误，不静默丢失奖励。步骤奖励没有额外数量偏移，数量由 `CQFThingData` 自身定义。

## 七、编辑器对应关系

- `QuestEditor_QuestBook`：任务书根设置、Def 加载/保存和热加载。
- `QuestBookChapterSidebar`：章节列表、展开/收起、章节选择。
- `QuestBookNodeCanvas`：当前章节步骤节点、节点位置、节点拖拽、节点链接和视图平移。
- `Dialog_EditQuestBookChapter`：章节二级编辑窗口。
- `Dialog_EditQuestBookStep`：步骤二级编辑窗口。
- `Dialog_EditQuestBookObjective`：目标二级编辑窗口。
- `Dialog_EditQuestBookRewardInfo`：奖励展示信息二级编辑窗口。
- `Dialog_QuestBookStepInfo`：运行时节点详情窗口。

节点画布右键打开 FloatMenu 添加步骤，新步骤在鼠标位置创建。拖拽节点只移动节点；拖拽空白区域、WASD 和滚轮只移动视图。链接操作选择目标步骤，箭头线实时延伸到鼠标位置，完成后保存目标步骤 id。

## 八、入口和热加载

任务书入口只要存档中存在有效任务书实例就显示，进行中、完成和失败状态都属于可查看实例；没有实例时隐藏入口。

热加载通过章节 id、步骤 id 保留已有状态；新增内容创建锁定状态，删除内容移除对应状态，目标数量变化时同步调整目标进度列表，节点位置和链接来自新定义。

## 九、代码定位

- 定义：`.QuestEditor_Library/QuestEditor_Library/QuestBook/Def/`
- 运行时：`.QuestEditor_Library/QuestEditor_Library/QuestBook/QuestBookRuntime.cs`、`Runtime/`
- 目标：`.QuestEditor_Library/QuestEditor_Library/QuestBook/Objective/`
- 奖励：`.QuestEditor_Library/QuestEditor_Library/QuestBook/Reward/`
- 行为：`.QuestEditor_Library/QuestEditor_Library/QuestBook/Action/`
- Quest 接入：`.QuestEditor_Library/QuestEditor_Library/QuestBook/Quest/`
- 编辑器：`.QuestEditor_Library/QuestEditor_Library/QuestBook/Editor/`
- 入口和详情：`Dialog_QuestBookStepInfo.cs`、`MainTabWindow_QuestBook.cs`、`MainButtonWorker_QuestBook.cs`
