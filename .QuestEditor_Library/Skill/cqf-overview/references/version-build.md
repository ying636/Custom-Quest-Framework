# CQF 版本与构建

源码位于 `D:\Game\Steam\steamapps\common\RimWorld\Mods\CQF\.QuestEditor_Library`，与 Mod 包共用 Git 仓库。技能维护源放在其中的 `Skill`；更新时同时维护 `C:\Users\HaiLu\.codex\skills` 下对应安装副本，保留两处已有的不同内容。

当前 CQF 发布版本为 `3.6`，版本字段为 `About/About.xml` 的 `modVersion`。领域自 3.4 起拆为按需加载的独立模块，继续共用地图休眠系统的唯一 GComp。

## 引用与输出

本机构建与程序集检查使用游戏版本 `1.7.4981 rev136`。RimWorld 1.7 使用 .NET 10；先读取游戏 `Version.txt` 和 `RimWorld.runtimeconfig.json`，不要沿用旧 Unity Managed 目录。

主库与编辑器共用 `.QuestEditor_Library/RimWorld.props`，默认 `RimWorldVersion=1.7`：

- 主库：`net10.0`，输出 `1.7/Assemblies/net10.0/QuestEditor_Library.dll`。
- 编辑器：`net10.0`，输出 `1.7/Optional/Editor/CQF.Editor.dll`。
- 原版引用：游戏根目录的 `Assembly-CSharp.dll`、`UnityStubs.dll`、`Waylith.dll`，均不复制到 Mod。
- Harmony：默认引用 Steam 工坊 `2009463077/1.7/Assemblies/0Harmony.dll`；可用 `HarmonyPath` 指定其他安装位置。
- `RimWorldDir` 可覆盖默认游戏路径。
- `-p:RimWorldVersion=1.6` 保留 `net48` 和 `Krafs.Rimworld.Ref 1.6.4633` 构建入口。仅验证旧版构建时指定临时 `OutputPath`；发布跨版本功能时分别构建对应主库和可选模块，领域模块和后续跨版本调整同步更新 1.6 / 1.7 输出。

构建：

```powershell
dotnet build .QuestEditor_Library/CQF.Editor/CQF.Editor.csproj -c Release
```

编辑器仅由 `LoadFolders.xml` 中的 `IfCQFSetting="enableEditor"` 入口加载。不要把编辑器 DLL 放进自动扫描的主库 Assemblies 目录，也不要把游戏或 Harmony DLL 打包到 CQF。

## 可选领域模块

- 工程：`.QuestEditor_Library/CQF.Domain/CQF.Domain.csproj`，源码与主库完全分开；类型命名空间保持 `QuestEditor_Library`。
- 输出：`1.7/Optional/Domain/CQF.Domain.dll`，1.6 对应 `1.6/Optional/Domain/CQF.Domain.dll`；DLL 放在条件目录根部，不放 `Assemblies`。
- 主库的 `LoadFolders.xml` 用 `IfCQFModule="Domain"` 标记提供目录；子 Mod 用 `RequiresCQFModule="Domain"` 标记需求目录。两个属性由 `CQFModuleLoader` 实现，不是原版条件。
- 只读取当前启用 Mod 的原版已选目录，保留版本和 `IfModActive` 等条件；模块先加载，需求目录中的子 Mod DLL 后加载，再进入 Def 解析。
- 依赖领域的子 Mod DLL 同样放在需求目录根部。延后加载的 DLL 不得包含 `Mod` 派生入口类；独立的入口留在普通 Assemblies，且不能直接引用尚未加载的领域类型。
- 没有请求时不加载领域 DLL、不创建领域运行对象。领域模块不安装 Harmony 补丁。主库只保留 `ICQFMapModule` 生命周期接口；领域运行对象由唯一的 `GameComponent_MapManager` 持有，不增加 GComp。
- 寂屋邪祭输出为 `1.7/RSH.dll`，版本目录自身声明 RequiresCQFModule；可选玩家能力适配为 `1.7/PlayerAbilityFramework/RSH.PlayerAbilityFramework.dll`，1.6 对应同名版本目录。不要保留旧 Domain 或 Assemblies 中的重复 DLL。

构建领域模块会先构建主库：

```powershell
dotnet build .QuestEditor_Library/CQF.Domain/CQF.Domain.csproj -c Release
```

完整打包须包含同一版本的主库、领域模块和已更新引用的子 Mod。不要混用拆分前后的 DLL。字段、调用和加载验证见 [领域系统开发参考](domain-system.md)。

## 原生回调替代补丁

- 1.7：`MapComponent_CustomMapData` 订阅 `Map.events.ThingSpawned/ThingDespawned`，FinalizeInit 幂等绑定并在 MapRemoved/Dispose 解绑，同时取消活动地图尚未提交的休眠请求，不唤醒已休眠地图；离图事件触发时 Pawn 已不在地图，使用事件所属地图维护缓存。1.6 没有该接口，保留两个 Pawn 进出补丁，使用编译条件隔离。
- `LordJob_Custom.Notify_BuildingAdded` 给受管建筑附加 `Comp_CustomLordBuilding`，原版认领流程直接调用 `CompPreventClaimingBy`。CreateGraph 在读档重建时恢复组件；退出该 Lord 后限制立即失效。普通建筑不增加组件。
- CQF 及编辑器导出入口使用 `CQFSerialization.XElementFromObject`，递归处理 ISaveable、IntVec3、集合、Def 引用、Class、Unsaved 和 DefaultValue；不再修改全局 DirectXmlSaver。
- 领域保护使用 AbilityComp / ThingComp，详见 [领域系统开发参考](domain-system.md)。没有全局 Pawn.PreApplyDamage 补丁；准备和读档恢复均复用同一施术者组件。
- 建筑受伤、人物受伤和建筑销毁三个补丁本轮保留：ThingComp 的受伤回调位于原方法内部，不能覆盖建筑提前免疫返回后的现有通知；PostDestroy 又晚于离图和 Lord 清理。替换前须单独核对触发次数、伤害来源、地图和 Lord 引用。
- 1.7 主库有 18 个补丁目标，其中休眠为 3；领域为 0，编辑器为 1，模拟经营适配为 2。1.6 比 1.7 多保留两个 Pawn 事件区进出目标，不保留 Thing 休眠唤醒补丁。
- 休眠地图移出 Game.Maps 并由统一组件托管，Thing 原版状态为 Spawned=false、Map=null。没有对象/区域属性、Thing.DoTick、WorldObject.DoTick、地图更新、Tick 登记、自动移除、GameEnder、保存或 AddMap 补丁；只保留选图、取图、退出清理。
- CQF_ownedHibernatingMaps 使用独立字段深度保存组件托管地图，不复用旧 CQF_hibernatingMaps 引用列表；CQF_pendingHibernation 引用保存排队地图，不做旧格式迁移。单图保存时临时提供区域索引，Thing 仍未生成，finally 撤回。读档长事件结束时完成原版地图初始化及其延迟动作，再移出；普通唤醒不重复初始化。操作/销毁前显式激活，不通过原生物件生成/离图入口自动唤醒。

```powershell
dotnet run --project .QuestEditor_Library/Tests/EditorModule/Checks.csproj -c Release -- callbacks
dotnet run --project .QuestEditor_Library/Tests/EditorModule/Checks.csproj -c Release -- serialization
dotnet run --project .QuestEditor_Library/Tests/EditorModule/Checks.csproj -c Release -- hibernation
```

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

领域加载验证另见 `Tests/EditorModule/OptionalModuleChecks.cs`：独立进程运行 `modules-disabled`、`modules-filtered`、`modules-enabled`、`modules-adapter`、`modules-multiple`、`modules-invalid` 和 `modules-layout`；最后两项验证错误必须记录并抛出。领域规则验证入口为 `Tests/Domain/Checks.csproj`。

检查覆盖编辑器条件加载、类型解析、原版 XML Patch、运行时对编辑器的依赖、父对象地图生成参数和实际 Harmony 注册。1.7 的 Verse.Log 使用新日志后端；测试不能仅靠 `Debug.unityLogger.logHandler` 判断是否调用 `Log.Error`。

涉及原版 API 时通过 Rimsage 查询，并与当前本机程序集的编译/反射结果核对。涉及第三方适配时先确认前置 Mod 的版本与 API；缺少源码、DLL 或 1.7 前置版本时明确报告，不能把目录复制当作兼容验证。

## 可选适配项目

- 火种危机：构建 `.QuestEditor_Library/CQF.AllsparkCrisis/CQF.AllsparkCrisis.csproj`；输出 `1.7/Mod/AllsparkCrisis/Assemblies/CQFAndRS.dll`。默认引用火种危机 `1.7/Assemblies/AllsparkCrisis.dll`；`AllsparkCrisisPath` 可覆盖前置 DLL 路径。原适配仅有 DLL，当前源码依据其实际实现恢复。
- 模拟经营：构建 `.QuestEditor_Library/CQF.RimSimManagementFramework/CQF.RimSimManagementFramework.csproj`；输出 `1.7/Mod/RimSimManagementFramework/Assemblies/CQF_RSMF.dll`。`RimSimManagementPath` 可覆盖前置 DLL 路径。本机依赖仍在工坊 `3736621496/1.6/Assemblies`；两个顾客生成方法为 private，通过 Harmony TargetMethods 查找，并读取 out spawnedPawn，避免扫描整张地图收集新 pawn。
- Bill：本机未找到 BillDoorsPredefinedCharacter 的前置 DLL 或适配源码，1.7 未启用该加载入口，不把旧二进制复制视为适配完成。
- HCF：原 1.6 对应目录没有适配 DLL；1.7 删除空加载入口，不影响实际 HCF 在自身 Mod 中提供的联动项目。

已删除无对应 Def/运行类的 `CQF_DestroyMainSiteCountdown` 残留翻译，并删除错误短类型目录和重复英文 PawnMod DefInjected。自定义 Def 的中文翻译使用完整命名空间目录。

`Tests/GameRuntime` 的 `version17` 标记模式验证游戏内编辑器开关、编辑器地图、子地图和站点地图生成。使用独立 `-savedatafolder` 与临时测试 Mod，结束后归档测试 Mod；不要改用户实际配置或存档。原 AI HTTP 综合测试属于另一套测试，不能替代此版本适配验证。

休眠离线检查 32 项通过：使用真实原版 Tick 列表验证 Normal/Rare/Long 暂停与恢复，使用原版 Thing 序列化及真实 Scribe 验证组件托管的 Map 夹具深度保存与引用往返。Map.ExposeData/FinalizeLoading 使用精简夹具，不代表完整地图存读档通过。空地图休眠与唤醒不调用全局 Tick 移除，预热后 256 次宿主更新为 0 字节分配；框架子地图入口检查总地图容量。

当前领域发布验证：1.6 / 1.7 编译、52 项领域规则与组件生命周期检查及上述 7 种模块加载场景通过；空闲宿主在模块启用/禁用两种情况下，预热后 256 次更新均为 0 字节分配。游戏内集成测试在测试入口前因 `D3DSCache.dll`、异常码 `0xc0000409` 退出，未产生通过标记；不能据此宣称领域进出、存读档或视觉转场已通过游戏内验证。
