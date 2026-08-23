---
name: cqf-quest-book-dev
description: CQF 任务书系统的架构、对象、字段参数、运行时状态、目标子类、奖励和编辑器参考。
metadata:
  short-description: CQF任务书系统参考
---

# CQF 任务书系统

这是一份 CQF 任务书系统参考资料，内容包括任务书对象层级、定义字段、运行时实例、目标检查、步骤奖励、Quest 绑定、编辑器结构以及目标对象的完整绘制接口。

完整对象和参数说明位于 [references/architecture.md](references/architecture.md)。

任务书的基本关系是：

```text
QuestBookDef
└── QuestBookChapter
    └── QuestBookStep
        ├── QuestBookObjective
        │   └── QuestBookObjective_* 子类
        ├── 实际奖励
        ├── 奖励展示信息
        ├── 步骤行为
        └── 后续步骤链接
```

定义描述任务书内容，运行时实例和状态对象描述玩家当前进度。章节是组织层级，步骤是节点，目标属于步骤内部。目标的检查、参数和编辑绘制由 `QuestBookObjective` 及其具体子类负责，不再通过独立 Worker 委托。基类 `Draw` 负责完整的通用编辑界面，目标子类通过 `DrawSpecial` 扩展专属内容；目标类型在步骤编辑器添加目标时确定，目标详细窗口只编辑已创建目标的内容。
