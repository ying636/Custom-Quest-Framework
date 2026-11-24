# CQF 外部 Agent 接入

适用于 RimWorld 1.7 的 CQF 编辑器模块。CQF 提供 stdio MCP Host，读取和编辑在当前 RimWorld 进程内执行。Hermes 使用自己配置的模型。

1. 在 CQF 设置启用编辑器，重启游戏后打开 **AI 设置 → 外部 Agent（MCP）**。
2. 默认可以读取。需要改动游戏或编辑器数据时，启动前勾选 **允许编辑**。开始后点击管道名复制。桥接窗口须保持打开。关闭后重新启动会生成新的管道名。
3. 在与 RimWorld 使用同一 Windows 用户的 Hermes Agent 中，将下面的条目合并进 `config.yaml`。替换 Host 的绝对路径和游戏内显示的管道名。Host 需要 .NET 10 Runtime。

```yaml
mcp_servers:
  cqf:
    command: 'D:\Game\Steam\steamapps\common\RimWorld\Mods\CQF\MCP\CQF.McpHost.exe'
    args:
      - '--pipe'
      - 'CQF-替换为游戏中复制的管道名'
```

4. 重新加载 Hermes 的 MCP 连接，调用 `cqf_session_get` 检查连通性、游戏加载状态和编辑权限。用 `cqf_tools_find` 查找 CQF 工具，先列出并选定目标，再按需查字段和工具。
5. 调用 `cqf_tool_call` 时，为每次操作提供唯一的 `request_id`。同一桥接会话中，遇到连接中断时使用原请求 ID 和相同参数重试。CQF 返回已有回执，避免重复应用写入。可撤销的编辑可在桥接窗口撤销。需要保存到磁盘的编辑仍按 CQF 编辑器和目标工具的保存规则处理。

制作新地图时，用 `cqf_scene_assets` 按已加载的 Mod 和类别查候选，选中后才请求 `detail=placement`。将独立地图定义写入草稿后，用 `cqf_scene_preview` 生成实机预览和截图，以 `cqf_scene_checks` 验证地图上的路线和关键物件。视觉确认后再导出。参数与本地批处理命令见 [CQF 实机素材搭景 Skill](../.QuestEditor_Library/Skill/cqf-live-scene/SKILL.md)。

独立构建 Host：

```powershell
dotnet publish .QuestEditor_Library/CQF.McpHost/CQF.McpHost.csproj -c Release -o MCP
```

同机 stdio 是本版提供的连接方式。远程 Hermes 及 HTTP MCP 需要独立的传输部署。
