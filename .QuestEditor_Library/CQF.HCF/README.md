# CQF 的 HCF 行为兼容

CQF 4.1 在启用 `HaiLuan.HiveCreatureFramework` 时，通过 `1.6/Mod/HCF` 或 `1.7/Mod/HCF` 加载 `HCFWithCQF.dll`。主库不引用 HCF，不增加 Harmony 补丁。

本工程提供两个 CQF 行为；HCF 自身的 `Mods/CQF` 工程继续负责生产、升级、融合和改造的任务书目标与信号，两者功能独立。

## 加入巢群

类型：`HCFWithCQF.Actions.CQFAction_JoinHiveGroup`。

- `targetsText`：待入组单位的 CQF 目标密匙列表，默认 `Pawn`。沿用 CQF 的目标与数据库群组解析。
- `hiveKey`：目标巢穴或节点的 CQF 密匙，默认 `Hive`。
- `groupIndex`：默认 `-1`，依内部 ID 顺序为每个单位选择首个能接纳它的群组；`0` 及以上指定一个群组。

支持巢穴 `CompHiveGroup`、节点 ThingComp 和节点 HediffComp。单位与巢穴或节点必须已生成在同一地图。入组调用 HCF 的 `CanAccept` 和 `AcceptUnit`，保留派系、标签、容量和节点归属限制。已经在选定群组中的单位不重复加入。拒绝入组时记录具体原因。

```xml
<li Class="HCFWithCQF.Actions.CQFAction_JoinHiveGroup">
  <targetsText><li>Pawn</li></targetsText>
  <hiveKey>Hive</hiveKey>
  <groupIndex>-1</groupIndex>
</li>
```

## 设置集群模式

类型：`HCFWithCQF.Actions.CQFAction_SetHiveGroupMode`。

- `targetsText`：巢穴、节点或已入组单位的密匙列表，默认 `Hive`。
- `mode`：HCF 的 `GroupWorkModeDef`。编辑器通过实际已加载的 Def 列表选择，不使用旧枚举。
- `groupIndex`：默认 `-1`，操作目标拥有的全部群组；`0` 及以上按内部 ID 顺序指定一个群组。普通单位操作其当前所属群组。

同一次行为中的重复群组只处理一次。先验证模式 Worker 的支持条件与目标回退，再调用 `SetMode(mode, false)`，由 HCF 更新职责及群组命令。无效目标、越界序号和不支持的模式均有诊断日志。

## 构建与验证

```powershell
dotnet build .QuestEditor_Library/CQF.HCF/CQF.HCF.csproj -c Release -p:RimWorldVersion=1.6
dotnet build .QuestEditor_Library/CQF.HCF/CQF.HCF.csproj -c Release -p:RimWorldVersion=1.7
dotnet run --project .QuestEditor_Library/Tests/HCF/Checks.csproj -c Release
```

`HiveCreatureFrameworkPath` 和 `CustomQuestFrameworkPath` 可覆盖对应版本的前置 DLL 路径。工程只输出兼容 DLL 与 PDB，不复制前置程序集。测试工程仅用于 1.7 离线验证，不进入发布目录。

4.1 使用 GitHub 已发布的 HCF 3.15（`2999b64`）双版本 DLL 构建，1.6 / 1.7 均为零警告、零错误。1.7 离线检查共 32 项，覆盖实际 HCF 入组条件、自动与指定群组、重复入组、跨地图/未生成目标拒绝、ThingComp/HediffComp 节点、模式选择、重复目标、XML 导出、编辑器复制及类型发现。入组和模式执行端使用探针群组记录调用；未验证完整游戏中的 Lord 调度或编辑器视觉效果。没有旧存档迁移。
