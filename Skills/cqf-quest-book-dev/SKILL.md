---
name: cqf-quest-book-dev
description: Develop, extend, debug, and review the CQF Quest Book system in the CQF RimWorld mod, including definitions, runtime progress, objectives, rewards, editor UI, hot reload, and task-book integration.
metadata:
  short-description: CQF任务书系统开发与维护
---

# CQF任务书开发

用于处理 CQF 任务书系统的代码、Def、编辑器和运行时行为。仅在任务书、章节、步骤、目标、奖励、任务书入口或任务书热加载相关请求中使用；普通对话、地图、Pawn 或通用 CQF 行为请求不应仅因引用 CQF 而触发本技能。

## 工作方式

- 先阅读 `.QuestEditor_Library/QuestEditor_Library/QuestBook/` 下的现有实现，再决定扩展点；不要把所有对象塞进一个文件。
- 查看源码时优先使用 Rimsage；修改前确认实际类名、字段、Def XML 节点和 RimWorld API。
- 任务书数据层、运行时层、编辑器层、奖励层和入口层保持分离。对象关系与生命周期见 [references/architecture.md](references/architecture.md)。
- 用户可见文本使用翻译 Key；英语直接写英文 Keyed 文本，中文放在 `Languages/ChineseSimplified (简体中文)/Keyed/`。
- 目标检查默认按现有运行时机制执行：信号即时处理，资源、建筑、研究等周期性条件按游戏日检查；不要改成每 Tick 全量扫描。
- 物品和建筑显示优先复用 `ThingDef` / `CQFThingData` 的原版图标和绘制方法，过滤无图标对象与 Mote。
- 任务书节点编辑器与运行时任务书都使用节点画布、章节分组、箭头链接和节点图标；视图平移与节点拖拽必须保持为两套交互。
- 完成状态使用 RimWorld 原版对号贴图或 `Widgets.CheckboxDraw`，未完成使用方框，不手绘红色 `x` 或 `DrawLine`。
- 奖励展示信息可以多条保存；详情页面默认奖励行显示图标和名称，描述通过悬停 Tip 展示，并为奖励行提供悬停高光。
- 任务书入口应根据存档中是否存在有效任务书实例显示；完成或失败的任务书仍属于可查看实例。
- 修改后编译 `.QuestEditor_Library/QuestEditor_Library/QuestEditor_Library.csproj`，确认 0 个错误；同时检查 `1.6/Assemblies/net48/QuestEditor_Library.dll` 是否更新。

## 参考资料

- 需要理解对象、状态、信号、目标检查、奖励发放和热加载时，阅读 [references/architecture.md](references/architecture.md)。
