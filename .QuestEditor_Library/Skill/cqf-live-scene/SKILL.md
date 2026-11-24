---
name: cqf-live-scene
description: Build a CQF RimWorld scene from assets actually loaded by the game. Use for Mod-based set dressing, map geometry and prop placement, finding exact Defs via CQF MCP, previewing in the game, checking routes and exporting the scene. Use the relevant CQF development skill for framework changes or quest scripting.
---

# CQF 实机素材搭景

通过 CQF 的 MCP 桥接查询目标整合包实际加载的资源。根据用户想搭的场景和指定的 Mod 查一小批候选，无需维护固定 Def 清单。

## 连游戏

- 沿用用户授权的游戏实例。不要打开或操作用户的存档来代替隔离测试。无 CU 时，可以连接已启用的桥接，或在用户同意后用独立离线副本验证。
- 在目标 CQF 安装内找 `MCP/CONNECT.md` 和 `MCP/CQF.McpHost.exe`。Host 通过 stdio 启动，参数为 `--pipe <管道名>`。使用现有 MCP 客户端，或用本 Skill 的 `scripts/cqf_stdio.py --host <Host路径> [--pipe <管道名>] status` 连线。脚本只在发现唯一 CQF 管道时自动选择。
- `cqf_session_get` 成功表示已经与游戏往返。核对 `gameLoaded` 和 `editingAllowed`。查素材走只读权限。创建和保存场景需要目标实例允许编辑。未启用桥接时，文件可用于查找安装和版本，实机加载结论要等桥接或隔离副本核实。

## 看实际可用的素材

1. `cqf_list_mods` 确认 Mod 的精确 `packageId`。用 `cqf_scene_assets` 按 `mod`、`kind`（`building`、`item`、`plant`、`terrain`）和 `search` 查候选。默认 `brief`，每页最多 20 项，按 `total` 和 `offset` 分页。需要放置时，用精确 Def 名和 `detail=placement` 查材质、原生占地、旋转、互动格与通行性。
2. `cqf_query_resources` 补查普通分类以外的素材，例如地牢光源，或用 `images` 查贴图。CQF 特殊物件用 `cqf_list_cqf_things`、`cqf_find_types` 和 `cqf_get_schema` 确定地图数据类。行为有疑点时查对应 Mod 源码，Rimsage 用于原版。

## 搭景与交付

- 新场景先确定一个能在全图辨认的主体，从入口画主路线，再划房间、次路线和留白。用同一批材质贯穿整个场景，重点区给不同的轮廓、地面或灯光。道具按空间用途成组摆放，预留门口和互动格。大块地面用区域操作，局部用脚本计算坐标和占格，模型无需逐格写入。
- 新场景用独立 `CustomMapDataDef`。把地图操作存为 `<changes>` XML。另写 `<checks>`，用 `<route x="X" z="Z" toX="X" toZ="Z"/>`、`<thing x="X" z="Z" def="DefName"/>`、`<terrain x="X" z="Z" def="DefName"/>` 验主路线和关键摆放，每批最多 24 项。路线用原版寻路，允许通门。预览通过原生生成，定义里的生成步骤会执行。
- 用 `cqf_scene_pipeline.py preview` 建草稿、分批写入、检查并截取全景和近景。打开两张实机截图，检查主路线、主体和道具分组。调整后用 `inspect` 重新生成现有草稿并验图。视觉检查通过后用独立的 `export` 命令保存。如果要交付可重用的地图，保留操作稿、检查稿、观察点和 XML，并在隔离实例重载导出文件验一次。
- 修改已运行地图时，用 `cqf_read_map_overview` 和精读工具查位置。批量地形、路径和蓝图用 `cqf_stage_map_draft`，查看 `cqf_map_draft_status`，让用户在草稿窗口确认应用。

```powershell
python scripts/cqf_scene_pipeline.py --host <Host路径> --pipe <管道名> --name CQF_Scene preview --changes changes.xml --checks checks.xml --views views.json --output preview
python scripts/cqf_scene_pipeline.py --host <Host路径> --pipe <管道名> --name CQF_Scene export --output export
```

`views.json` 用 `{"overview":[42,38,44],"detail":[60,53,23]}` 这样的格式填写 `[x,z,zoom]`。方形大地图的全景可从中心与约六成边长的 `zoom` 开始，核对界面有没有挡住边缘。`inspect` 参数与 `preview` 相同，可省去 `--changes`。截图路径、定义校验和实机检查结果在 `--output` 目录下。连线或分批写入中断时，先读回草稿确认已执行的部分，再继续编辑。

脚本可用 `find <名称或功能>` 查当前工具，或 `call <CQF工具名> --arguments-file <UTF-8 JSON>` 调用。工具参数的值用字符串。连接中断后，只有在同一管道会话且确认参数完全一样时才用原 `--request-id` 重试写操作。
