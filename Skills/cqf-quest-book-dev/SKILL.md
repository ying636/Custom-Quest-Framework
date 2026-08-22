---
name: cqf-quest-book-dev
description: Explain the CQF Quest Book system and architecture, including object hierarchy, runtime state flow, objective checks, rewards, task binding, editor structure, and hot reload.
metadata:
  short-description: CQF任务书系统与架构说明
---

# CQF任务书系统

这是 CQF 任务书系统的架构说明技能。用于回答“系统有哪些对象、对象如何嵌套、运行时如何运作、编辑器如何对应数据、目标如何完成、奖励如何发放”等问题，也用于基于现有架构扩展任务书功能。

## 阅读顺序

1. 先阅读 [references/architecture.md](references/architecture.md)，了解完整对象层级和生命周期。
2. 需要修改代码时，再根据架构中的“代码对应关系”定位 `.QuestEditor_Library/QuestEditor_Library/QuestBook/` 文件。
3. 涉及目标检查、奖励投递、任务绑定或热加载时，只阅读参考文档中对应章节，不要重新发明一套任务书模型。

## 核心结论

- 任务书由章节组成，章节由步骤组成，步骤内部包含目标和奖励；章节与步骤不是同一层级。
- 定义对象描述任务书内容，实例对象保存存档中的进度；编辑器编辑定义，运行时读取实例和定义共同绘制界面。
- 目标由独立 Worker 检查，信号目标即时响应，资源、建筑、研究目标按周期检查。
- 步骤完成时先确认完成模式，再发放实际奖励、执行步骤行为并激活后续步骤。
- 奖励展示信息只负责给玩家看的图标、名称和 Tip 描述；实际奖励由 `CQFThingData` 数据发放。
- 任务书可以绑定原版 Quest，也可以由 `autoStart` 自动创建实例；完成和失败的实例仍然可以查看。
