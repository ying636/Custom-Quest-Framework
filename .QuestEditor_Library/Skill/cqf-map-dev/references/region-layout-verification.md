# 区域布局验证记录

日期：2026-10-29。方案先保存至 `b87e196`，实现分支为 `codex/region-layouts`，未发布新版本号或合并主分支。该分支只包含区域布局改动，不包含工作区另行进行的领域、休眠和对话演出修改。

## 最终检查

| 检查 | 结果 |
| --- | --- |
| CQF 主库与编辑器 Release 构建 | 零错误；主库 1983、编辑器 347 条既有警告 |
| `Tests/MapLayouts` | 55 项通过 |
| 独立游戏 RimWorld 1.7.4982 | 29 项通过，无游戏 ERROR，已正常结束 |
| 双语资源 | 74 个 Key 对齐，6 个布局 Def 标签另有中文 DefInjected |
| 文本审核表 | 80 行，CSV 与 Excel；均保持同名占位待审核 |
| XML、部署脚本语法、Git 空白检查 | 通过 |

规则测试覆盖区域唯一引用、tag、多实例、偏移旋转、XML、真实边界、候选权重、方向、保护、种子、数量上限、预览变更、目标 Key 冲突、多位置墙体阻塞入口、六种布局和群落中心不足。

原生游戏验证调用实际绘制工具与生成入口：先在地图上标记并保护主城，再生成三个带道路的聚落，使用原版寻路检查主城到三个门的可达性。验证保留墙的身份和耐久、物品身份和数量、地形、屋顶不变；地图导出含区域；原生保存及重新载入保留几何、连接点、tags 和实例序号。另验证无区域的旧模板继续生成、地图模板先登记区域再运行布局、布局失败向调用方传递异常并清理生成状态。

界面检查使用五张新截图；绘制由测试驱动调用生产 Designator 方法，未将其宣称为鼠标点击或 CU 测试。先前截图发现数值缓冲未初始化，已修正并复查显示。失败场景初版使用空模板，触发原有 `CustomMapDataDef.GetRect` 的 `Nullable object must have a value`；补齐测试底图后最终通过，保留失败日志。

## 可追溯构建

仅包含本次改动的最终原生验证目录：

`C:/Users/24904/AppData/Local/Temp/CQF-RegionLayouts/Runtime/CQF_Cinematics_e4fb165cceb84d0bb9c18efd5ba9a52c`

| 文件 | SHA256 |
| --- | --- |
| QuestEditor_Library.dll | `3A46CCF8A2F681065C7A71E2A119EACCCCC4E6254B79CACD9E7C2C253E0A314F` |
| CQF.Editor.dll | `31EDF1587837CFB79016958854A654868176C0BE6AC2A7E581004A1D6364C5BA` |

该目录包含 `SaveData/MapLayoutChecks.txt`、截图和游戏日志。规则日志位于 `C:/Users/24904/AppData/Local/Temp/CQF-RegionLayouts-clean-checks-final.log`。仓库中的候选 DLL/PDB 与该次验证对应。

本机部署脚本会针对运行时的整个当前工程重新构建和验证，保留其他并行修改；因此其哈希可能与独立分支不同。部署清单、检查日志、旧 DLL/PDB 备份位于 `AppData/Local/CQF/DeploymentPackages/RegionLayouts-*`。

当前限制及使用配置见 [区域布局用法](region-layout-usage.md)。本轮未验证或部署 1.6，不做旧存档迁移；道路模板拼装仍未实现。
