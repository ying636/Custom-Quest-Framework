# CQF 1.7 版本与构建

源码位于 `D:\Game\Steam\steamapps\common\RimWorld\Mods\CQF\.QuestEditor_Library`，与 Mod 包共用 Git 仓库。技能维护源放在其中的 `Skill`；更新时同时维护 `C:\Users\HaiLu\.codex\skills` 下对应安装副本，保留两处已有的不同内容。

## 引用与输出

本机已验证版本为 `1.7.4976 rev1043`。RimWorld 1.7 使用 .NET 10；先读取游戏 `Version.txt` 和 `RimWorld.runtimeconfig.json`，不要沿用旧 Unity Managed 目录。

主库与编辑器共用 `.QuestEditor_Library/RimWorld.props`，默认 `RimWorldVersion=1.7`：

- 主库：`net10.0`，输出 `1.7/Assemblies/net10.0/QuestEditor_Library.dll`。
- 编辑器：`net10.0`，输出 `1.7/Optional/Editor/CQF.Editor.dll`。
- 原版引用：游戏根目录的 `Assembly-CSharp.dll`、`UnityStubs.dll`、`Waylith.dll`，均不复制到 Mod。
- Harmony：默认引用 Steam 工坊 `2009463077/1.7/Assemblies/0Harmony.dll`；可用 `HarmonyPath` 指定其他安装位置。
- `RimWorldDir` 可覆盖默认游戏路径。
- `-p:RimWorldVersion=1.6` 保留 `net48` 和 `Krafs.Rimworld.Ref 1.6.4633` 构建入口。改源码时不要覆盖已发布的旧版 DLL；验证旧版构建时指定临时 `OutputPath`。

构建：

```powershell
dotnet build .QuestEditor_Library/CQF.Editor/CQF.Editor.csproj -c Release
```

编辑器仅由 `LoadFolders.xml` 中的 `IfCQFSetting="enableEditor"` 入口加载。不要把编辑器 DLL 放进自动扫描的主库 Assemblies 目录，也不要把游戏或 Harmony DLL 打包到 CQF。

## 旧系统清理后的接口

- 已删除 `CustomSitePartParams` 和 `Patch_SitePartParams`。站点地图从 `CustomSite.mapDef/quest` 读取；子地图从 `MapParent_Custom.mapDataDef/quest` 读取；编辑器地图从 `EditorMapObject.mapDef` 读取。
- `CustomMapStep.Generate(Map, CustomMapDataDef, CustomMapGenerationContext)` 使用临时上下文的 `Quest` 传递生成前步骤创建的任务。上下文不参与存档，不继承原版站点参数。
- `ComplexPawnDef` 只读取 `modDatas`；已删除 `LoadModData`、Worker `LoadData` 及平铺字段过滤/迁移。
- `CQFAction_Pawn_RunDutyMapTransition` 只保存和读取 `toNodeId`，不迁移旧 `nodeId`。
- 简单 Lord、巡逻/等待路线、PawnSpawnData 与 `ThingData.allPositions` 仍有当前生成和编辑器调用，不得仅凭名称或历史描述当作存档兼容壳删除。

## 验证

```powershell
dotnet run --project .QuestEditor_Library/Tests/EditorModule/Checks.csproj -c Release
dotnet run --project .QuestEditor_Library/Tests/EditorModule/Checks.csproj -c Release -- enabled
dotnet run --project .QuestEditor_Library/Tests/EditorModule/Checks.csproj -c Release -- patches
```

检查覆盖编辑器条件加载、类型解析、原版 XML Patch、运行时对编辑器的依赖、父对象地图生成参数和实际 Harmony 注册。1.7 的 Verse.Log 使用新日志后端；测试不能仅靠 `Debug.unityLogger.logHandler` 判断是否调用 `Log.Error`。

涉及原版 API 时通过 Rimsage 查询，并与当前本机程序集的编译/反射结果核对。涉及第三方适配时先确认前置 Mod 的版本与 API；缺少源码、DLL 或 1.7 前置版本时明确报告，不能把目录复制当作兼容验证。

## 可选适配项目

- 火种危机：构建 `.QuestEditor_Library/CQF.AllsparkCrisis/CQF.AllsparkCrisis.csproj`；输出 `1.7/Mod/AllsparkCrisis/Assemblies/CQFAndRS.dll`。默认引用火种危机 `1.7/Assemblies/AllsparkCrisis.dll`；`AllsparkCrisisPath` 可覆盖前置 DLL 路径。原适配仅有 DLL，当前源码依据其实际实现恢复。
- 模拟经营：构建 `.QuestEditor_Library/CQF.RimSimManagementFramework/CQF.RimSimManagementFramework.csproj`；输出 `1.7/Mod/RimSimManagementFramework/Assemblies/CQF_RSMF.dll`。`RimSimManagementPath` 可覆盖前置 DLL 路径。本机依赖仍在工坊 `3736621496/1.6/Assemblies`；两个顾客生成方法为 private，通过 Harmony TargetMethods 查找，并读取 out spawnedPawn，避免扫描整张地图收集新 pawn。
- Bill：本机未找到 BillDoorsPredefinedCharacter 的前置 DLL 或适配源码，1.7 未启用该加载入口，不把旧二进制复制视为适配完成。
- HCF：原 1.6 对应目录没有适配 DLL；1.7 删除空加载入口，不影响实际 HCF 在自身 Mod 中提供的联动项目。

已删除无对应 Def/运行类的 `CQF_DestroyMainSiteCountdown` 残留翻译，并删除错误短类型目录和重复英文 PawnMod DefInjected。自定义 Def 的中文翻译使用完整命名空间目录。

`Tests/GameRuntime` 的 `version17` 标记模式验证游戏内编辑器开关、编辑器地图、子地图和站点地图生成。使用独立 `-savedatafolder` 与临时测试 Mod，结束后归档测试 Mod；不要改用户实际配置或存档。原 AI HTTP 综合测试属于另一套测试，不能替代此版本适配验证。