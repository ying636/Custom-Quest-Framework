# 领域系统

领域系统位于可选的 `CQF.Domain` 模块中，支持 RimWorld 1.6 和 1.7。框架管理领域生命周期、共享血量、外部载体、参与者转移、地图池和转场调度。领域的具体战斗规则与表现通过 `DomainWorker` 和 `DomainTransition` 扩展。

主库保留唯一的 `GameComponent_MapManager` 管理通用地图休眠，通过 `ICQFMapModule` 将生命周期转发给普通对象 `CQFDomainRuntime`。领域 API 使用 `CQFDomainRuntime.Instance`，不会创建额外 GComp。主库不包含领域 Def、MapComponent 或领域补丁。

CQF 的 LoadFolders 条目使用 `<li IfCQFModule="Domain">1.7/Optional/Domain</li>`；子 Mod 使用 `<li RequiresCQFModule="Domain">1.7</li>` 声明需求。这两个属性由 CQF 处理，保留原版的版本和 IfModActive 条件。没有启用子 Mod 请求时不加载领域 DLL。

领域保护通过原生 AbilityComp / ThingComp 回调实现，模块不安装 Harmony 补丁。领域能力通过 DomainDef.openingAbility 自动配置组件；若展开物件在 SpawnSetup 后才设置 Caster，初始化时须再次调用 MapComponent_Domain.RegisterOpening。

领域和依赖它的子 Mod DLL 均放在声明目录根部，不放 Assemblies；加载顺序为主库、领域、子 Mod DLL、Def 解析。延后加载的 DLL 不得包含 Mod 派生入口类。多个请求共享一个模块，每个游戏独立创建运行对象。1.6 使用对应版本路径。

更新顺序为转场、无长事件时的预加载/Worker 安全更新/返回/展开，宿主最后提交休眠请求。预加载按需安排，不每帧扫描全部领域定义；ManagedUpdate 只处理未休眠地图，仍不按 Active 过滤。默认不迁移拆分前旧存档。

## 定义

`DomainDef` 使用完整类型 `QuestEditor_Library.DomainDef`。现有血海领域在寂屋邪祭中提供，配置如下：

```xml
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
```

`worldObject` 的类必须继承 `MapParent_Domain`，`anchor` 的类必须继承 `Building_Domain`，`protection` 的类必须继承 `Hediff_DomainProtection`。后两项可以省略。`workerClass` 必须有公开无参构造函数。`preload` 默认开启，每种领域定义可预生成一个空闲地图；更多并发领域按需生成。

`openingAbility` 是可选的原版 `AbilityDef` 引用。配置后，施术者执行该能力工作期间由框架提供展开无敌，包括展开物件生成前的施法阶段。通过玩家能力或自定义调用展开时，可直接使用下述 `DomainOpening` 接口。

显示名使用英文原文与 `DefInjected/QuestEditor_Library.DomainDef` 中的中文翻译。CQF 主库不引用血海的程序集、定义和图形资源。

## 生命周期与扩展方法

通过 `CQFDomainRuntime.Instance.Acquire(definition, caster)` 取得领域地图，再调用该地图的 `MapComponent_Domain.Enter(caster, targets)` 进入。目标筛选由调用方完成；框架过滤重复、死亡和不在施术者地图上的目标。

每张领域地图对应一个 `MapComponent_Domain` 和一个独立的 `DomainWorker`。普通地图也有通用组件，尚未配置领域时 `Worker` 为 `null`；使用 `GetWorker<T>()` 查询具体实现时需处理返回空值的情况。

| Worker 扩展方法 | 调用时机与职责 |
| --- | --- |
| `Entering` | 初始化本次领域的专有状态，人员尚未转移 |
| `Entered` | 人员转移完成，可授予能力或设置战斗 Lord |
| `Tick` | 领域运行中且入场锁定已解除时推进具体规则 |
| `ManagedUpdate` | 长事件结束后的安全更新点，可执行复活等结构变更 |
| `BeforeReturn` | 人员返回前处理专有状态、载具和退出效果 |
| `PawnLeaving` | 清理返回人物及尸体人物的专有状态 |
| `Returned` | 人员和物品已返回，可清理临时能力和绑定退出转场 |
| `Recycling` | 空图中的物件和 Lord 已清理，可重置地形及实现状态 |
| `Collapsed` | 血量归零或外部载体丢失时执行领域专有惩罚 |
| `CasterDied` | 施术者死亡通知，可实现领域内复活规则 |
| `ExposeData` | 保存、载入专有状态；框架已绑定 `Domain` 上下文 |
| `Dispose` | 释放领域实现持有的图形资源 |

`SuspendLifecycle` 可以在专有复活流程期间阻止自动退出判断。`CasterIsPresent` 可扩展对特殊移动载具的识别。`HandlesEntryView` 表示由实现的转场接管进入时的镜头切换。

`RequestReturn` 发起正常退出，`Collapse` 发起崩塌退出。原地图不可用时框架尝试返回其他玩家家园；没有目标地图时保留领域和人员并报告错误。部分转移失败时保留参与者记录，允许后续重试。默认不提供旧版血海领域存档的迁移。

## 血量和外部载体

`Health`、`MaximumHealth`、`HealthFraction` 属于 `MapComponent_Domain`。`DrainHealth` 和 `RestoreHealth` 更新共享血量，并同步外部载体的整数生命值。归零只触发一次崩塌；崩塌后的领域无法通过恢复血量重新激活。

`Hediff_DomainProtection` 为施术者安装共享伤害吸收组件，优先消耗领域血量，超出部分继续作用于施术者。它只保护关联领域的施术者，退出时由框架移除。外部载体直接消耗同一份领域血量，使用定义中的单次受伤上限，避免再经普通建筑受伤流程重复扣血。

## 转场

`Comp_DomainProtection` 通过原生伤害回调保护领域施术者：执行已配置的展开能力、活动展开物件存在、同步进入领域以及入场锁定期间，伤害置零并跳过后续处理，不消耗领域血量。展开和入场结束后恢复通常的领域血量吸收；其他参与者、普通能力和已失效展开不会获得该无敌。死亡或倒地人物不适用展开保护。

`DomainTransition` 管理地图切换、等待领域地形完成绘制、入场和返回期间的人物锁定、过渡结束及资源清理。具体实现提供 `Duration`、`CoverProgress`、`RevealProgress`、`DrawEffect` 和 `ReleaseVisuals`。截图采集、材质和动画细节留在子 Mod。

使用 `CQFDomainRuntime.Instance.SetTransition` 管理当前转场。展开中的视觉对象可继承 `DomainOpening`，通过 `QueueOpening` 调度完成操作。返回锁定使用 `MapComponent_DomainTransitionLock`，结束时恢复人物原有的剩余眩晕时间。

`DomainOpening` 必须提供 `Definition`、`Caster`、`Active` 和两个完成方法。基类在 `SpawnSetup` 自动登记展开物件，在 `DeSpawn` 自动移除登记；覆盖这两个生命周期方法时须调用基类，以便所有领域共用展开保护，读档后也会重新登记。

## 验证

`dotnet run --project .QuestEditor_Library/Tests/Domain/Checks.csproj -c Release` 使用实际游戏程序集检查领域血量、伤害吸收、外部载体同步及配置错误。

`.QuestEditor_Library/Tests/DomainRuntime/RunChecks.ps1` 使用独立配置与存档目录运行血海领域集成检查，覆盖进出、尸体与物品返回、领域复用、崩塌和存档往返。临时测试 Mod 在运行结束后移入本次日志目录归档，不修改玩家配置与存档。

领域空地图休眠时移出 Game.Maps，由统一组件托管；Thing 按原版未生成处理，唤醒时复用原图并恢复索引与 Tick 登记。框架不补丁 Thing.Map/Spawned 或地图更新入口。CQF_ownedHibernatingMaps 深度保存休眠地图，单图保存范围内临时提供区域索引，Thing 保持未生成；不迁移旧格式。
