# CQF 领域系统开发参考

用于领域展开、领域内战斗、共享血量、外部载体、人员往返、地图池和转场扩展。以下接口依据当前本地源码核对；领域框架有 RimWorld 1.6 / 1.7 分支，具体构建环境见 [版本与构建](version-build.md)。

## 源码入口与职责

主库目录：`D:\Game\Steam\steamapps\common\RimWorld\Mods\CQF\.QuestEditor_Library\QuestEditor_Library\Domain`，类型均位于 `QuestEditor_Library` 命名空间。

| 源文件 / 类型 | 职责 |
| --- | --- |
| `DomainDef.cs` | 领域配置、类型与数值合法性检查 |
| `MapComponent_Domain.cs` | 单张地图的领域状态、参与者、共享血量、进出与存档 |
| `DomainWorker.cs` | 每张领域地图独立的玩法扩展实例 |
| `MapParent_Domain.cs` | 继承 `PocketMapParent`，阻止空领域地图被自动移除 |
| `DomainParticipant.cs` | 人物引用、原位置、原 Lord、征召状态和转场前眩晕结束 tick |
| `Building_Domain.cs` | 原地图上的外部载体、受伤限额和共享血量显示 |
| `Hediff_DomainProtection.cs` / `Comp_DomainProtection.cs` | 施术者伤害吸收、死亡通知和存读档后的保护组件恢复 |
| `DomainOpening.cs` | 子 Mod 展开物件的调度接口 |
| `Patch_DomainOpeningProtection.cs` | 施法、展开与入场期间的施术者保护 |
| `DomainTransition.cs` | 镜头切换、入场/返回锁定和转场资源释放 |
| `MapComponent_DomainTransitionLock.cs` / `DomainTransitionLifetime.cs` | 返回人员锁定，以及离开游戏时释放视觉资源 |

统一管理器位于同级 `MapManagement` 目录，仅有一个 `GameComponent_MapManager` 实例，通过 partial 按职责分文件：

| 源文件 | 职责 |
| --- | --- |
| `GameComponent_MapManager.cs` | 唯一构造函数、Instance、Update/OnGUI、存读档与释放入口 |
| `GameComponent_MapManager.Hibernation.cs` | 所有地图的休眠、激活、引用和索引恢复 |
| `GameComponent_MapManager.Domains.cs` | 领域地图池、预加载、获取、回收、Worker 安全更新与返回队列 |
| `GameComponent_MapManager.Transitions.cs` | 展开队列、当前转场与转场更新 |

统一 Update 先更新转场；没有长事件时依次处理预加载、Worker.ManagedUpdate、返回与展开，最后提交休眠请求。保存范围内不处理这些更新。新增地图管理需求优先接入现有管理器，不为同一套地图调度另建 GComp。

先读相关 CQF / 子 Mod 源码；原版类型和 API 用 Rimsage 核对。Rimsage 不索引 Mod 源码。

CQF 提供通用机制，不引用寂屋邪祭的程序集、血海 Def、Shader、音效或专有战斗规则。血海实现仅作为已存在的使用示例，不能当作所有领域的默认规则。

## DomainDef 配置

XML 根类型使用 `QuestEditor_Library.DomainDef`。字段大小写和类型以 `DomainDef.cs` 为准：

| 字段 | 默认值 / 要求 |
| --- | --- |
| `worldObject` | 必填 `WorldObjectDef`；`worldObjectClass` 继承 `MapParent_Domain` |
| `mapGenerator` | 必填 `MapGeneratorDef` |
| `workerClass` | 默认 `QuestEditor_Library.DomainWorker`；须为可实例化的 `DomainWorker` 子类，有公开无参构造函数 |
| `size` | `(60, 1, 60)`；x/z 为正，y 必须为 1 |
| `maximumHealth` | `1000`；有限正数 |
| `maximumAnchorDamagePerHit` | `float.MaxValue`；正数且不能为 NaN，约束外部载体单次受伤 |
| `anchor` | 可省略 `ThingDef`；`thingClass` 继承 `Building_Domain` |
| `protection` | 可省略 `HediffDef`；`hediffClass` 继承 `Hediff_DomainProtection` |
| `openingAbility` | 可省略 `AbilityDef`；执行该能力工作期间给予施术者展开保护 |
| `preload` | `true`；管理器在该 Def 尚无地图时预生成一张并休眠 |

新 Def 使用所属 Mod 的前缀。显示文本使用用户给定文本，没有文本时使用同名占位符；Def 原文为英语，中文放入 `Languages/ChineseSimplified (简体中文)/DefInjected/QuestEditor_Library.DomainDef`，不创建英文 DefInjected。DLL / UI 文本使用 Key 与中英 Keyed 翻译。

现有配置文件为 `D:\Game\Steam\steamapps\common\RimWorld\Mods\寂屋邪祭\Defs\DomainDefs\RSH_BloodSeaDomain.xml`，内容如下；这些引用属于寂屋邪祭：

```xml
<Defs>
  <QuestEditor_Library.DomainDef>
    <defName>RSH_BloodSeaDomain</defName>
    <label>boundless blood sea</label>
    <worldObject>RSH_BloodSeaDomain</worldObject>
    <mapGenerator>RSH_BloodSeaDomainGenerator</mapGenerator>
    <workerClass>RSH.DomainWorker_BloodSea</workerClass>
    <size>(60, 1, 60)</size>
    <maximumHealth>100000</maximumHealth>
    <maximumAnchorDamagePerHit>350</maximumAnchorDamagePerHit>
    <anchor>RSH_BloodSeaDomainBuilding</anchor>
    <protection>RSH_BloodSeaDomainProtection</protection>
    <openingAbility>RSH_BoundlessBloodSea</openingAbility>
  </QuestEditor_Library.DomainDef>
</Defs>
```

## 获取、进入与地图池

最小调用链：

```csharp
Map realm = GameComponent_MapManager.Instance!.Acquire(definition, caster);
realm.GetComponent<MapComponent_Domain>().Enter(caster, targets);
```

`Acquire` 先找同 Def 的 `Available` 地图，激活其休眠状态；无空闲地图时按需生成。它只获取地图，不转移人物，也不预留地图；获取后应在同一流程中进入。`DomainMaps` 是所有已登记领域地图，`HibernatingMaps` 是所有已休眠地图，两者不是同一份列表。领域地图池和休眠由同一管理器直接协调。

预加载在新建/加载游戏后初始化待办，新增或唤醒地图时检查尚待处理的定义，最后一张同 Def 领域地图移除后补充预加载。不会每帧扫描全部 `DomainDef`，也不保证始终留有一张空闲地图。预加载失败报告错误并在本局停止对该定义自动重试。队列为空时不创建快照；Worker 更新复用缓冲列表，仍保留对已登记地图调用 `ManagedUpdate` 的语义。

`Enter` 要求领域空闲，施术者存活、已生成、未倒地，且不在目标领域地图。调用方负责敌我、范围、能力条件与是否允许嵌套领域；框架不强制禁止从另一张领域地图进入。目标会按同地图、已生成、存活去重，倒地目标不会被框架排除。

顺序为：记录来源地图和施术者原点、激活并补满血量 → `Entering()` → 安装可选保护 Hediff → 记录并转移参与者 → `Entered(entrants)` → 在原点生成可选外部载体 → 默认镜头切换。进入位置尽量保留相对施术者的位置，安全落点由 `FindFreeCell` 选择。原 Lord 先移除，征召状态在转移后恢复。`Entered` 执行时外部载体尚未生成。

每张领域地图有一个 Worker，复用地图也复用 Worker；每次展开的专有状态应在 `Entering` / `Recycling` 重置。普通地图也有 `MapComponent_Domain`，未配置时 `Worker` / `GetWorker<T>()` 为 null，访问 `Definition` 会抛异常，不能仅凭组件存在判断领域。

| 状态属性 | 精确定义 |
| --- | --- |
| `Active` | 本次领域仍处于活动状态，包含等待退出阶段 |
| `Ready` | `active && !closing`，不代表入场转场已结束 |
| `Available` | 未激活、未关闭中且参与者记录为空 |
| `ArrivalLocked` | 正在入场锁定，期间不调用玩法 `Tick` |

## Worker 扩展与存档

| 回调 / 属性 | 时机与用途 |
| --- | --- |
| `Entering()` | 转移前初始化本次玩法状态 |
| `Entered(IReadOnlyList<Pawn> entrants)` | 转移后授予临时能力、创建领域内 Lord |
| `Tick()` | 已激活、未关闭、未暂停生命周期且入场解锁后推进规则 |
| `Update()` / `OnGUI()` | 来自 MapComponent 的对应调用，不自动按 Active 过滤，自行判断状态 |
| `ManagedUpdate()` | 管理器在没有正在进行或等待的长事件时遍历已登记地图调用，适合复活等结构变更；不自动按 Active 过滤 |
| `Draw()` | 当前正在查看且领域 Active 时绘制 |
| `BeforeReturn(bool visible)` | 返回人员前处理玩法状态、特殊载具和退出截图 |
| `PawnLeaving(Pawn pawn)` | 清理离开人物的专有状态；包括尸体中的 InnerPawn，不只原参与者 |
| `Returned(Map destination, IntVec3 focus, IReadOnlyList<Pawn> pawns)` | 人物、尸体、物品已转移，外部载体已移除；此时 Caster / Participants 尚可用，可清理能力或绑定返回转场 |
| `Recycling(Map destination)` | 地图残余 Thing / Lord 已清理，休眠前重置地形和实现状态 |
| `Collapsed(string reason)` | 共享血量耗尽或载体丢失等崩塌流程中的专有惩罚 |
| `CasterDied(Pawn pawn)` | 由关联保护 Hediff 的死亡通知转发；未配置保护时不能仅靠此回调捕获死亡 |
| `FinalizeInit()` | MapComponent 初始化阶段转发，可重建专有状态 |
| `ExposeData()` | 保存 / 载入专有状态；框架已创建 Worker 并绑定 Domain |
| `Dispose()` | 地图移除或更换配置时释放实现资源，不是每次退出回调 |
| `SuspendLifecycle` | 默认 false；为 true 时跳过领域 Tick 的自动退出检查和玩法 Tick，可供复活流程使用 |
| `CasterIsPresent` | 默认检查 `caster?.MapHeld == map`；可扩展特殊飞行载具等情形 |
| `HandlesEntryView` | 默认 false；转场接管入场镜头时覆盖 |

`DomainWorker` 实现 `IExposable`。框架存储定义、活动状态、血量、施术者、来源、外部载体和参与者，在 LoadingVars 阶段按 `workerClass` 重建实例并绑定 `Domain`，然后进入 `domainWorker` 节点调用 `ExposeData`。子类负责自己的计时、临时能力列表、复活标志等数据。默认不做旧存档或旧版血海字段迁移。

## 共享血量与保护

真实血量只有 `MapComponent_Domain.Health` 一份，配合 `MaximumHealth` / `HealthFraction`。通过 `DrainHealth` / `RestoreHealth` 更新，不在 Worker 或载体另存第二套血量。外部载体 `HitPoints` 是向上取整且至少为 1 的显示投影，不能反过来作为浮点血量来源。

保护 Hediff 为施术者安装 `Comp_DomainProtection`，`AbsorbDamage` 先消耗领域血量，超出部分继续作用于施术者；无关 Pawn 不受保护。退出时框架移除与本领域绑定的保护 Hediff，读档后恢复动态保护组件。

外部载体只对 `harmsHealth` 的正伤害按 `maximumAnchorDamagePerHit` 扣共享血量，并吸收该次普通建筑伤害流程，避免重复扣血。载体被 Destroy / DeSpawn 会崩塌；正常退出必须使用 `RemoveNormally()` 先解除关联，再销毁。`Collapse` 只执行一次，血量归零，移除载体，调用玩法惩罚并请求退出；本次领域崩塌后 `RestoreHealth` 不能使其复活。

## 正常返回、失败与回收

主动结束调用 `RequestReturn(reason)`；它设置关闭标志，随后由 MapComponentUpdate 排队，管理器在长事件结束后的更新点调用 `CompleteReturn()`。不要在受伤或人物遍历回调里直接执行批量跨地图返回。

未暂停生命周期且入场已解锁时，施术者缺失、死亡、倒地或不再存在于领域中也会请求正常返回；这些情形本身不等于 `Collapse`。特殊复活流程需要自行配合 `SuspendLifecycle`，恢复后解除暂停。

返回优先原地图，不可用时寻找其他玩家家园。原参与者尽量回原位置，额外生成的人物与物品回来源原点附近。框架转移地图上的 Pawn、Corpse 和 Item，并按条件恢复征召与原 Lord；特殊容器或飞行载具内的人物应由 `BeforeReturn` 先释放。`Returned` 的 pawns 参数是返回人物列表，尸体中的人物通过 `PawnLeaving` 清理。

完成返回后，管理器确认地图无人物，销毁残余 Thing、移除 Lord，调用 `Recycling` 并请求休眠，随后框架清空活动状态与参与者。地形重置属于子 Mod 的 `Recycling`，不能假设框架自动重建整张地图。

无可用返回地图时报告错误并保留领域和人员；批量返回异常时也保留剩余地图与参与者记录，结束相关转场并允许后续重试。已经转移的副作用不会整体回滚，返回回调应考虑重试时可能再次执行。入场异常会记录错误并尝试返回已经转移的人员，随后重新抛出原异常。

## 转场与展开物件

具体 `DomainTransition` 提供 `Duration`、`CoverProgress`、`RevealProgress`、`DrawEffect(float progress)`、`ReleaseVisuals()`。截图、材质、Shader 与动画属于子 Mod。使用管理器 `SetTransition(value)`，替换时会 Dispose 旧转场；当前游戏只有一个管理中的转场。

- `Bind(realm)` 关联领域并开始入场锁定。
- `BindReturn(destination, focus, pawns)` 关联返回地图，用 `MapComponent_DomainTransitionLock` 锁定返回人员。
- `Dispose()` 结束锁定并释放视觉资源；`DomainTransitionLifetime` 在游戏实例变化或退出游玩状态时也会释放。
- 锁定期间停止寻路、维持 90 tick 眩晕；结束时按先前记录的结束 tick 恢复原有剩余眩晕，并保留期间新增的更长眩晕。LoadedGame 会解除遗留入场与返回锁定。

1.7 的入场流程由管理器逐帧 `Update()` 推进：遮盖绘制完成一帧后切换地图，再等待 `LastTerrainRenderedFrame` 晚于切图帧，随后推进揭幕。**自定义地形绘制实现必须在确认实际有地形内容绘出后写入该帧号**；框架不会自动写入，遗漏会让入场一直等待地形就绪。`LastRenderedFrame` 是另一项可见性记录，不能代替地形就绪标记。

需要“原地图展开 → 遮盖 → 转移”的物件继承 `DomainOpening`，实现 `Definition`、`Caster`、`Active`、`CompleteOpening()`、`CompleteTransfer()`，展开完成后 `QueueOpening(this)`。基类在 SpawnSetup/DeSpawn 登记和移除展开物件，覆盖时须调用基类。管理器在有空闲领域时直接完成展开，否则排入生成地图的长事件。1.7 可先 `BindOpening(this)`，由转场在遮盖完成后调用 `CompleteTransfer()`，其中获取领域、进入并 `Bind(realm)`。

`Patch_DomainOpeningProtection` 在施术者执行已配置的 `openingAbility`、活动展开物件存在、同步进入和入场锁定期间将伤害置零，不消耗领域血量；死亡、倒地人物和其他参与者不适用。展开和入场结束后恢复通常的共享血量吸收。

`BindOpening` 与转场 `Update` 为 1.7 分支 API；1.6 不使用这套延后转移与地形帧等待机制。跨版本实现用已有 `RIMWORLD_1_7` 条件分支，不能无条件调用 1.7 方法。

## 已有实现与验证入口

子 Mod 参考目录：`D:\Game\Steam\steamapps\common\RimWorld\Mods\寂屋邪祭\.RSH\RSH`。

- `DomainWorker_BloodSea.cs`：进入、能力、Lord、复活、返回清理与地图重置。
- `Thing_BloodSeaDomainOpening.cs` / `BloodSeaDomainTransition.cs`：展开、截图、绑定和视觉资源。
- `SectionLayer_BloodSeaDomain.cs`：地形实际绘制后报告 `LastTerrainRenderedFrame`。
- `MapParent_BloodSeaDomain.cs`、`Building_BloodSeaDomain.cs`、`Hediff_BloodSeaDomainProtection.cs`：具体载体和保护子类。

以下命令从 CQF Mod 根目录执行，按改动选择验证：

```powershell
dotnet run --project .QuestEditor_Library/Tests/Domain/Checks.csproj -c Release
pwsh -File .QuestEditor_Library/Tests/DomainRuntime/RunChecks.ps1
```

`Tests/Domain` 使用真实游戏程序集验证 Worker 隔离、共享血量、伤害上限、保护溢出、崩塌只执行一次、展开保护及配置错误。`Tests/DomainRuntime` 依赖当前寂屋邪祭，检查统一管理器实例、领域进出、尸体与物品返回、复用休眠、崩塌、活动领域存读档，以及最后一张领域地图移除后的预加载；它会启动游戏，运行前要求没有 RimWorld 进程。运行脚本使用独立配置/存档目录并将临时测试 Mod 移入日志目录归档。该集成检查不能代替视觉转场的实际观察，且不能当作 1.6 运行验证。

地图休眠回归入口为 `pwsh -File .QuestEditor_Library/Tests/MapHibernation/RunChecks.ps1`，包含空闲管理器连续更新的分配检查，以及排队取消、Tick 停止/恢复、引用、索引和全部地图休眠的存读档检查。组件实例缓存切换见 `Tests/EditorModule/MapManagerInstanceChecks.cs`。
