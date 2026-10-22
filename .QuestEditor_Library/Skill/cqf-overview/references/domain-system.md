# CQF 领域系统开发参考

双领域对抗扩展见 [领域对抗](domain-contest.md)。下文的单领域流程仍适用于普通展开；共同战场中的第二实例必须通过 FindDomainFor / GetInstances 获取，不能假设单图 GetWorker 就是当前人物的领域。

用于领域展开、领域内战斗、共享血量、外部载体、人员往返、地图池和转场扩展。以下接口依据当前本地源码核对；领域框架有 RimWorld 1.6 / 1.7 分支，具体构建环境见 [版本与构建](version-build.md)。

## 源码入口与职责

当前领地玩法的公共接口见 [领域对抗](domain-contest.md)：RestoreHealth 供技能回血；ProtectTerritory / RemoveTerritoryProtection 管理计时或来源绑定的真实守区；RegisterStructure 将技能建筑登记到本次领域布局。自然对抗消耗与主场 30% 减伤由框架处理，敌区技能倍率由子 Mod 自己处理。三个机制均复用同一个领域血量与实际 OwnsCell 查询，不新增 GComp、地图或领域配对分支。

领域模块源码目录：`D:\Game\Steam\steamapps\common\RimWorld\Mods\CQF\.QuestEditor_Library\CQF.Domain`，类型均位于 `QuestEditor_Library` 命名空间。

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
| `DomainMapTemplate.cs` / `DomainStructureBlueprint.cs` | 预生成地形与初始建筑布局，提供特殊建筑初始化扩展 |
| `DomainLayout.cs` / `DomainStructure.cs` | 实例建筑的领地显隐、原对象托管、合并与深度保存 |
| `AbilityCompProperties_DomainProtection.cs` / `AbilityComp_DomainProtection.cs` | 领域能力初始化与读档后的原生伤害组件准备 |
| `DomainTransition.cs` | 镜头切换、入场/返回锁定和转场资源释放 |
| `MapComponent_DomainTransitionLock.cs` / `DomainTransitionLifetime.cs` | 返回人员锁定，以及离开游戏时释放视觉资源 |

领域工程为 `.QuestEditor_Library/CQF.Domain/CQF.Domain.csproj`，输出到版本目录的 `Optional/Domain/CQF.Domain.dll`。主库不引用这个 DLL，也不保留领域 Def、MapComponent 或领域补丁类型。

| 源文件 | 职责 |
| --- | --- |
| 主库 `MapManagement/GameComponent_MapManager.cs` | 唯一 GComp、游戏实例缓存、通用地图休眠、可选模块生命周期转发 |
| 主库 `MapManagement/GameComponent_MapManager.Hibernation.cs` | 移出并托管休眠地图、切换索引与 Tick 登记、深度保存休眠地图 |
| 主库 `Modules/CQFModuleLoader.cs` | 解析需求、按顺序加载框架模块及子 Mod DLL、登记工厂 |
| 领域 `CQFDomainModule.cs` | 注册领域运行对象工厂，不安装 Harmony 补丁 |
| 领域 `CQFDomainRuntime.cs` | 按游戏创建的普通运行对象、领域存档和释放入口 |
| 领域 `CQFDomainRuntime.Domains.cs` | 领域地图池、预加载、获取、回收、Worker 安全更新与返回队列 |
| 领域 `CQFDomainRuntime.Transitions.cs` | 展开队列、当前转场与转场更新 |

`CQFDomainRuntime.Instance` 从当前游戏的 `GameComponent_MapManager.Module` 获取领域运行对象；模块未启用时抛出可诊断异常，不自动创建全局替代实例。领域模块不新增 GameComponent。partial 不能跨程序集使用，领域 API 不再挂在主库 GComp 上。

统一 Update 在保存范围之外调用可选模块，模块先更新转场；没有长事件时依次处理预加载、Worker.ManagedUpdate、返回与展开，宿主最后提交休眠请求。主库存读档、地图添加、OnGUI 和释放也通过 `ICQFMapModule` 转发。模块状态保存于 `cqfMapModule` 节点，默认不迁移拆分前的旧存档。

领域池中的休眠地图移出原版 Game.Maps，由统一组件持有和深度保存。Thing 按原版未生成处理，Spawned=false、Map=null，不补出物件或 MapParent 的地图引用；唤醒后复用原图并恢复索引与 Tick 登记。回收要求无人物，空图休眠/唤醒不扫描全局 Tick 表。CQF_ownedHibernatingMaps 保存托管 Map，CQF_pendingHibernation 引用保存排队地图；单图保存范围内临时提供原版区域索引，Thing 仍未生成。3.7 已删除临时旧格式迁移，只读写当前字段；未转换的旧档须先经带兼容修复的 3.6 重存。边界见地图 Skill 的独立地图休眠段落。

## LoadFolders 声明与打包

CQF 自己提供可选目录：

```xml
<li IfCQFModule="Domain">1.7/Optional/Domain</li>
```

子 Mod 在自己的当前版本条目中声明：

```xml
<li RequiresCQFModule="Domain">1.7</li>
```

从 CQF 3.4 起，领域必须通过上述声明启用。需求在游戏启动时解析；变更启用的子 Mod 或 LoadFolders 后须重新启动游戏，不能在运行中切换模块。

需求条目可与原版 `IfModActive` 等条件组合。加载器只处理当前启用 Mod 在原版版本/依赖条件筛选后保留的目录；被排除的目录和未启用 Mod 不会触发需求。多个请求共用一份领域模块，不需要在 CQF 硬编码子 Mod packageId，也不依赖玩家设置。

加载顺序为主库 → 领域 DLL → 各子 Mod 需求目录根部的 DLL → Def 解析。子 Mod 之间按原有加载顺序处理，同一子 Mod 按 LoadFolders 声明顺序处理需求目录。先声明主领域 DLL 目录，再声明依赖它的适配目录。需要自己在 About 中声明 CQF 依赖和正确的前后加载关系。

**依赖领域的 DLL 必须放在需求目录根部，不能放 `Assemblies` 子目录或普通自动加载目录。** 原版会在 CQF Mod 构造函数之前检查普通程序集的类型，此时继承 `DomainWorker` 等类型会因缺少模块而失败。条件 DLL 不包含 `Mod` 派生入口类；需要 Mod 入口时，放在独立普通 DLL，入口不能提前引用领域类型。

`IfCQFModule` / `RequiresCQFModule` 是 CQF 的自定义属性，不是原版功能。提供方限于 CQF，子 Mod 使用需求属性。未知模块、缺少模块 DLL、错误目录和类型加载失败会记录包含子 Mod packageId 与路径的错误并抛出。没有任何请求时领域 DLL 不加载、领域 MapComponent 不注册、领域状态不创建。领域模块本身不安装 Harmony 补丁。1.6 将上述路径中的版本改为 1.6，并构建匹配的 DLL。

寂屋邪祭已将主 DLL 合入版本目录，使用 `1.7/RSH.dll` 和 `1.7/PlayerAbilityFramework/RSH.PlayerAbilityFramework.dll`；版本目录自身声明 RequiresCQFModule，后者保留玩家能力框架的 IfModActive 条件。1.6 使用对应版本目录。不能残留旧 Domain 或 Assemblies 中的重复文件。

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
| `preload` | `true`；管理器为该 Def 预生成两张地图并休眠，同时缓存原始地形与建筑蓝图，供同类展开和领域对抗复用 |
| `visuals` | 可省略 `DomainVisualDef`；通用展开、材质和时间参数，见 [基础领域视觉](domain-visuals.md) |
| `surface` | 可省略 `DomainSurfaceDef`；领域特征材质，省略时使用真实地形与建筑，不影响参加对抗 |
| `contestColor` | 默认 `(0.72, 0.76, 0.80, 1)`；公共接触带的领域颜色，不用于整图染色 |

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
Map realm = CQFDomainRuntime.Instance.Acquire(definition, caster);
realm.GetComponent<MapComponent_Domain>().Enter(caster, targets);
```

`Acquire` 先找同 Def 且没有人物的 `Available` 地图，激活其休眠状态；无空闲地图时按需生成。它只获取地图，不转移人物，也不预留地图；获取后应在同一流程中进入。`DomainMaps` 是所有已登记领域地图，`HibernatingMaps` 是所有已休眠地图，两者不是同一份列表。领域运行对象通过其宿主直接调用通用休眠与激活。

反展开只查询 `GetMapTemplate` 的地形与建筑缓存，不再 Acquire / Activate 临时模板地图；同步展开也只唤醒宿主地图。缓存由同一运行对象深度保存，不能从已被对抗改写的活动地图重新采样原始模板。

`DomainMapTemplate` 在原始地图生成后捕获地形及除外部载体之外的 Building。`DomainLayout` 在普通进入时接管原有模板建筑，反展开时从蓝图创建独立建筑；对抗只显示完整占地都归本方的建筑。退让时通过 `DomainStructure` 的 ThingOwner 托管原建筑，夺回时恢复原对象，保留本次使用中的组件状态、损伤及品质。目标占地被人物、物品或建筑占用时等待，之后定期重试，不强行覆盖。已摧毁建筑在本次使用中不重建；返回后清理布局，下次使用从原始蓝图恢复。

归属查询包含主分界线、主动占领和保护区域，自动腹地孔洞已取消。地形切换、完整建筑占地判定、技能和持续效果均通过 DomainAt / OwnsCell 获取结果，不能仅比较 x 与 BoundaryX。FirstFraction 表示总面积份额，BoundaryX 按固定归属格补偿，不一定等于份额乘地图宽度。主动占领格连续位于敌方主领地 600 tick 后失守，己方主线到达或保护生效会重置暴露计时。新增领域无需实现类型配对，普通地形与自定义材质共用领地几何。

默认蓝图记录 Def、Stuff、坐标、旋转、血量、阵营、品质、颜色和风格，不克隆任意组件、库存或屋顶。需要额外初始数据的特殊建筑，覆盖 `DomainWorker.CaptureStructure(Building)`，返回独立的 `DomainStructureBlueprint` 子类，在 `Create` / `ExposeData` 中处理自身数据；不要加入领域类型配对判断。运行中的建筑收起保留整个原对象，与初始蓝图捕获范围不同。收起及返回清理前先释放建筑容器中的人物/尸体；原版生成中的 Casket 使用自身 EjectContents，其他持有容器使用原版 TryDrop，失败会报错并保留状态。

预加载在新建/加载游戏后初始化待办，新增或唤醒地图时检查尚待处理的定义，最后一张同 Def 领域地图移除后补充预加载。不会每帧扫描全部 `DomainDef`，也不保证始终留有一张空闲地图。预加载失败报告错误并在本局停止对该定义自动重试。队列为空时不创建快照；Worker 更新只从原版活动地图挑选已登记领域并复用缓冲列表，不逐帧遍历休眠池。预加载来源排除休眠和待休眠地图，新增或唤醒地图后重新检查。

`Enter` 要求领域空闲，施术者存活、已生成、未倒地，且不在目标领域地图。调用方负责敌我、范围、能力条件与是否允许嵌套领域；框架不强制禁止从另一张领域地图进入。目标会按同地图、已生成、存活去重，倒地目标不会被框架排除。

顺序为：记录来源地图和施术者原点、激活并补满血量 → 建立并显示本次布局 → `Entering()` → 安装可选保护 Hediff → 记录并转移参与者 → `Entered(entrants)` → 在原点生成可选外部载体 → 默认镜头切换。进入位置尽量保留相对施术者的位置，安全落点由 `FindFreeCell` 选择。原 Lord 先移除，征召状态在转移后恢复。`Entered` 执行时外部载体尚未生成。

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
| `ManagedUpdate()` | 管理器在没有长事件时对未休眠地图调用，适合复活等结构变更；按休眠过滤，但不按 Active 过滤 |
| `Draw()` | 当前正在查看且领域 Active 时绘制 |
| `PrepareReturn(bool visible)` | 反复询问退出是否就绪；默认调用 BeforeReturn 后立即允许，带可见转场的实现需等待自身 ReturnCovered，不能每次重建转场 |
| `BeforeReturn(bool visible)` | 返回人员前处理玩法状态、特殊载具和退出截图 |
| `PawnLeaving(Pawn pawn)` | 清理离开人物的专有状态；包括尸体中的 InnerPawn，不只原参与者 |
| `Dissolved()` | 本方领域结束时撤销规则和临时能力；对抗中人物仍会滞留，不能等实际返回才清理 |
| `Returned(Map destination, IntVec3 focus, IReadOnlyList<Pawn> pawns)` | 人物、尸体、物品实际转移；此时 Caster / Participants 尚可用，用于来源地图规则和返回转场，必须立即失效的能力放在 Dissolved 清理 |
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

对抗中另一方仍活动时，RequestReturn 与 Collapse 均先经过 PrepareReturn。1.7 基础与血海复用自身退出转场，实际绘出全屏遮盖后才进入 Trapped：结束入场锁定、释放特殊载具与布局容器、调用 Dissolved、撤销本方保护/载体/布局及屏幕边缘，人物和尸体保持原地图、原位置。保留施术者、参与者及来源记录并深度保存，不执行表示实际返回的 Returned。剩余方在遮盖下立即接管整张地图的真实地形和建筑，暂停时也完成接管；框架将同一转场直接 BindReturn 到共同战场，等待接管后的地形实际绘出，再揭幕并解除临时锁定。最后一方退出时先验证双方返程地图，完整遮盖后分别按各自记录返回，再统一回收。不可见退出仍直接处理，不创建全屏等待。Trapped 实例不能成为空闲地图，也不能接纳第三方。

## 正常返回、失败与回收

主动结束调用 `RequestReturn(reason)`；它设置关闭标志，随后由 MapComponentUpdate 排队，管理器在长事件结束后的更新点调用 `CompleteReturn()`。不要在受伤或人物遍历回调里直接执行批量跨地图返回。

待返回队列复用处理缓存。等待遮盖的 `ReturnToSource` 与真正迁移人员的私有 `FinishReturn` 分开，使后续 LINQ 闭包不会在等待阶段提前分配；预热后连续 256 次等待重试新增分配为 0。实际跨图操作仍只在遮盖屏障通过后执行。

1.7 基础与血海 Worker 均等待自身退出转场的 ReturnCovered。血海保留本次转场引用，Returned 仅绑定同一转场，随后清空引用；返回材质在覆盖点至揭幕点保持不透明。不可见退出不创建等待屏障。对抗中的幸存血海和独立血海使用同一流程。

未暂停生命周期且入场已解锁时，施术者缺失、死亡、倒地或不再存在于领域中也会请求正常返回；这些情形本身不等于 `Collapse`。特殊复活流程需要自行配合 `SuspendLifecycle`，恢复后解除暂停。

返回优先原地图，不可用时寻找其他玩家家园。原参与者尽量回原位置，额外生成的人物与物品回来源原点附近。框架转移地图上的 Pawn、Corpse 和 Item，并按条件恢复征召与原 Lord；特殊容器或飞行载具内的人物应由 `BeforeReturn` 先释放。`Returned` 的 pawns 参数是返回人物列表，尸体中的人物通过 `PawnLeaving` 清理。

完成返回后，管理器确认地图无人物，销毁残余 Thing、移除 Lord，调用 `Recycling` 并请求休眠，随后框架清空活动状态与参与者。地形重置属于子 Mod 的 `Recycling`，不能假设框架自动重建整张地图。

无可用返回地图时报告错误并保留领域和人员；批量返回异常时也保留剩余地图与参与者记录，结束相关转场并允许后续重试。已经转移的副作用不会整体回滚，返回回调应考虑重试时可能再次执行。入场异常会记录错误并尝试返回已经转移的人员，随后重新抛出原异常。

## 转场与展开物件

具体 `DomainTransition` 提供 `Duration`、`CoverProgress`、`RevealProgress`、`DrawEffect(float progress)`、`ReleaseVisuals()`。基础截图、材质、Shader 与动画可使用 CQF 提供的通用实现，见 [基础领域视觉](domain-visuals.md)；特殊效果仍由子 Mod 扩展。使用 `CQFDomainRuntime.Instance.SetTransition(value)`，替换时会 Dispose 旧转场；当前游戏只有一个管理中的转场。

- `Bind(realm)` 关联领域并开始入场锁定。
- `BindReturn(destination, focus, pawns)` 关联返回地图，用 `MapComponent_DomainTransitionLock` 锁定返回人员。
- `Dispose()` 结束锁定并释放视觉资源；`DomainTransitionLifetime` 在游戏实例变化或退出游玩状态时也会释放。
- 锁定期间停止寻路、维持 90 tick 眩晕；结束时按先前记录的结束 tick 恢复原有剩余眩晕，并保留期间新增的更长眩晕。LoadedGame 会解除遗留入场与返回锁定。

1.7 的入场流程由管理器逐帧 `Update()` 推进：遮盖绘制完成一帧后切换地图，再等待 `LastTerrainRenderedFrame` 晚于切图帧，随后推进揭幕。**自定义地形绘制实现必须在确认实际有地形内容绘出后写入该帧号**；框架不会自动写入，遗漏会让入场一直等待地形就绪。`LastRenderedFrame` 是另一项可见性记录，不能代替地形就绪标记。

需要“原地图展开 → 遮盖 → 转移”的物件继承 `DomainOpening`，实现 `Definition`、`Caster`、`Active`、`CompleteOpening()`、`CompleteTransfer()`，展开完成后 `QueueOpening(this)`。基类在 SpawnSetup/DeSpawn 登记和移除展开物件，覆盖时须调用基类。管理器在有空闲领域时直接完成展开，否则排入生成地图的长事件。1.7 可先 `BindOpening(this)`，由转场在遮盖完成后调用 `CompleteTransfer()`，其中获取领域、进入并 `Bind(realm)`。

`DomainDef.ResolveReferences()` 为 `openingAbility` 自动加入一份 `AbilityCompProperties_DomainProtection`。能力初始化及 PostLoadInit 通过 `AbilityComp_DomainProtection` 为施术者准备唯一的 `Comp_DomainProtection`；该 ThingComp 在原版 `PostPreApplyDamage` 中检查当前施法、活动展开物件、同步进入和入场锁定，免伤时不消耗领域血量。死亡、倒地人物和其他参与者不获得这层免伤。检查不遍历 DomainDef，也不增加逐帧轮询。

`Hediff_DomainProtection` 与能力复用同一个伤害组件，并绑定领域血量吸收。移除领域 Hediff 只解除血量绑定，保留后续施法所需的组件；中断施法、展开注销及入场解锁后，状态判断立即失效。领域进入和 FinalizeInit 也会准备组件，允许未配置能力或保护 Hediff 的领域获得入场保护。

`RegisterOpening(opening)` 同时准备已知施术者的组件。若子类在 SpawnSetup 之后才设置 Caster，初始化时须再次调用 `Map.GetComponent<MapComponent_Domain>().RegisterOpening(this)`；注册是幂等的，子类加载和清理仍须调用基类。寂屋邪祭的 `Thing_BloodSeaDomainOpening.Initialize` 已接入。领域框架不再含 `Patch_DomainOpeningProtection`，也不调用 Harmony.PatchAll。

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

`Tests/Domain` 使用真实游戏程序集验证 Worker 隔离、共享血量、伤害上限、保护溢出、崩塌只执行一次、展开保护、原生伤害回调、读档组件重建、重复初始化及配置错误。`Tests/DomainRuntime` 依赖当前寂屋邪祭，检查统一管理器实例、领域进出、尸体与物品返回、复用休眠、崩塌、活动领域存读档，以及最后一张领域地图移除后的预加载；它会启动游戏，运行前要求没有 RimWorld 进程。运行脚本使用独立配置/存档目录并将临时测试 Mod 移入日志目录归档。该集成检查不能代替视觉转场的实际观察，且不能当作 1.6 运行验证。

地图休眠回归入口为 `pwsh -File .QuestEditor_Library/Tests/MapHibernation/RunChecks.ps1`，包含空闲管理器连续更新的分配检查，以及排队取消、Tick 停止/恢复、引用、索引和全部地图休眠的存读档检查。组件实例缓存切换见 `Tests/EditorModule/MapManagerInstanceChecks.cs`。模块边界与加载顺序见 `Tests/EditorModule/OptionalModuleChecks.cs`，独立进程运行 modules-disabled、modules-filtered、modules-enabled、modules-adapter、modules-multiple、modules-invalid 和 modules-layout 模式。
