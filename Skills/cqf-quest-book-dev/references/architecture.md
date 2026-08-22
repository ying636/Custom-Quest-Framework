# CQF任务书系统架构

## 代码目录

- `QuestBook/Def/`：任务书、章节、步骤、目标、奖励展示信息和枚举。
- `QuestBook/Runtime/`：章节、步骤、目标进度状态。
- `QuestBook/QuestBookRuntime.cs`：游戏组件、任务书实例、信号接收、周期检查、激活、完成、失败和存档。
- `QuestBook/Objective/`：目标 Worker。现有资源、建筑、研究、信号目标分别独立实现。
- `QuestBook/Reward/`：奖励投递，将 `CQFThingData` 转成殖民地空投奖励。
- `QuestBook/Action/`：启动、完成、失败、打开任务书和发放奖励等 CQF 行为。
- `QuestBook/Quest/`：任务与任务书绑定的 QuestNode / QuestPart。
- `QuestBook/Editor/`：任务书编辑器、章节栏、节点画布、步骤编辑和目标/奖励二级窗口。
- `QuestBook/Dialog_QuestBookStepInfo.cs`：运行时节点详情窗口。
- `MainTabWindow_QuestBook.cs` 与 `MainButtonWorker_QuestBook.cs`：任务书入口和可见性。

## 数据对象

`QuestBookDef` 是任务书定义，包含 `QuestBookChapter` 列表、任务绑定配置、自动激活、任务书可见性、完成权限和任务书级行为。

`QuestBookChapter` 是独立层级，包含章节名称、描述、步骤列表、解锁/完成行为。步骤不能放在任务书的扁平列表中。

`QuestBookStep` 属于章节，包含 id、名称、描述、图标、完成模式、目标列表、步骤奖励、奖励展示信息、后续步骤链接和步骤行为。

`QuestBookObjective` 属于步骤，包含名称、描述、Worker 类型、目标对象、数量、可选标记和自定义图标。研究目标不保存目标数量。

`QuestBookRewardInfo` 是可选的多条奖励展示信息，包含图标、名称和描述；它不代替真正的 `CQFThingData` 奖励。

`QuestBookInstance` 是存档中的运行时任务书实例，保存定义引用、状态、章节状态、步骤状态、绑定 Quest 和时间戳。

## 运行机制

1. 游戏启动或读档时，`GameComponent_QuestBook` 根据 `autoStart` 创建任务书实例；非自动任务书由 CQF 行为或任务绑定激活。
2. 实例初始化章节和步骤状态，并激活第一步或由链接指定的步骤。
3. CQF 信号只处理当前激活步骤的信号型目标；目标 Worker 更新自己的进度。
4. 资源、建筑、研究等目标在周期检查中读取原版 Def/地图/殖民地数据，不进行每 Tick 全量扫描。
5. 步骤按 `completionMode` 判断目标是否完成；完成后发放步骤奖励、执行完成行为并激活后续链接步骤。
6. 任务书状态由实例维护；完成或失败实例仍可在任务书窗口查看。
7. 热加载替换任务书 Def 后，通过 id 保留已有章节、步骤和目标进度，新增内容创建新状态，删除内容移除对应状态。

## 编辑器约束

- 章节栏只管理章节；节点画布只显示当前章节的步骤。
- 右键节点画布使用 FloatMenu 添加步骤；新步骤在鼠标位置创建。
- 节点可以拖拽，视图可以用鼠标拖拽或 WASD 平移，两者不能混淆。
- 链接操作选择目标步骤，链接线使用箭头贴图并跟随鼠标预览。
- 步骤、目标、奖励展示信息使用二级窗口编辑；列表行支持悬停高光和 Tip。
- 图标选择使用 `Dialog_Select` / 已加载图片选择器；无图标、占位图和 Mote 不可选。
- 编辑器所有按钮遵循当前界面约定；图标清除、增删等操作优先使用原版按钮控件，不自绘图片按钮背景。

## 奖励规则

- 步骤实际奖励与奖励展示信息在详情页合并到同一个“奖励”区域，不再分小标题。
- 实际奖励支持 `CQFThingData` 的所有子类；投递优先复用原版任务完成空投机制。
- 奖励展示信息的名称直接显示，描述通过悬停 Tip 显示，并与实际奖励使用一致的行背景、图标尺寸和高光。
