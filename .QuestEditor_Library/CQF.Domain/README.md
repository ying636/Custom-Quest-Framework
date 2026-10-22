# 领域系统

领域系统位于可选的 `CQF.Domain` 模块中，支持 RimWorld 1.6 和 1.7。框架管理领域生命周期、共享血量、外部载体、参与者转移、地图池和转场调度。领域的具体战斗规则与表现通过 `DomainWorker` 和 `DomainTransition` 扩展。

主库保留唯一的 `GameComponent_MapManager` 管理通用地图休眠，通过 `ICQFMapModule` 将生命周期转发给普通对象 `CQFDomainRuntime`。领域 API 使用 `CQFDomainRuntime.Instance`，不会创建额外 GComp。主库不包含领域 Def、MapComponent 或领域补丁。

领域对抗按当前血量分配固定地图的总面积，通过主分界线推进争夺；已取消自动在腹地生成侵蚀孔洞。主动占领地持续暴露在敌方主领地内 10 游戏秒会失守，保护可阻止侵蚀。一方取消或崩塌后人物仍滞留到最后一方结束。无 surface 的纯地形建筑领域也使用同一机制，见 [领域对抗](../Skill/cqf-overview/references/domain-contest.md)。本次 1.6 运行包保持原有功能；血海专有适配由独立的寂屋邪祭仓库提供。

1.7 领域内反展开保留准备阶段，闭合时播放新领域自己的全屏特效；完全遮盖后才建立对抗，确认双方地形绘制后揭幕，不在已有领域里再次升起地面罩子。原地图展开及外部载体周围的融合演出沿用原流程。实际对抗复用当前地图和预生成模板，人物保持原位。

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

`worldObject` 的类必须继承 `MapParent_Domain`，`anchor` 的类必须继承 `Building_Domain`，`protection` 的类必须继承 `Hediff_DomainProtection`。后两项可以省略。`workerClass` 必须有公开无参构造函数。`preload` 默认开启，每种领域定义预生成两张地图，并保存地形与建筑模板；更多并发领域按需生成。

`openingAbility` 是可选的原版 `AbilityDef` 引用。配置后，施术者执行该能力工作期间由框架提供展开无敌，包括展开物件生成前的施法阶段。通过玩家能力或自定义调用展开时，可直接使用下述 `DomainOpening` 接口。

显示名使用英文原文与 `DefInjected/QuestEditor_Library.DomainDef` 中的中文翻译。CQF 主库不引用血海的程序集、定义和图形资源。

## 生命周期与扩展方法

通过 `CQFDomainRuntime.Instance.Acquire(definition, caster)` 取得领域地图，再调用该地图的 `MapComponent_Domain.Enter(caster, targets)` 进入。目标筛选由调用方完成；框架过滤重复、死亡和不在施术者地图上的目标。

每张领域地图对应一个 `MapComponent_Domain` 和一个独立的 `DomainWorker`。普通地图也有通用组件，尚未配置领域时 `Worker` 为 `null`；使用 `GetWorker<T>()` 查询具体实现时需处理返回空值的情况。

对抗中的第二实例另有独立 Worker。`DomainAt`、`FindDomainFor`、`HasActiveDomain`、`HasOtherActiveDomain` 和 `GetActiveWorker<T>` 从任一方调用都查询共同战场；`OwnsCell`、`GetWorker<T>` 仍针对调用实例。人物所属领域应先通过 `FindDomainFor` 确定，再读取其 Worker。

| Worker 扩展方法 | 调用时机与职责 |
| --- | --- |
| `Entering` | 初始化本次领域的专有状态，人员尚未转移 |
| `Entered` | 人员转移完成，可授予能力或设置战斗 Lord |
| `Tick` | 领域运行中且入场锁定已解除时推进具体规则 |
| `ManagedUpdate` | 长事件结束后的安全更新点，可执行复活等结构变更 |
| `PrepareReturn` | 返回前反复询问；可见退出须等自身转场全屏覆盖后返回 true，等待期间不转移人员 |
| `BeforeReturn` | 人员返回前处理专有状态、载具和退出效果 |
| `PawnLeaving` | 清理返回人物及尸体人物的专有状态 |
| `Dissolved` | 本方领域结束，撤销临时能力及领域规则；人物可能仍被困在对方领域 |
| `Returned` | 人员和物品实际返回，处理来源地图规则并绑定退出转场 |
| `Recycling` | 空图中的物件和 Lord 已清理，可重置地形及实现状态 |
| `Collapsed` | 血量归零或外部载体丢失时执行领域专有惩罚 |
| `CasterDied` | 施术者死亡通知，可实现领域内复活规则 |
| `ExposeData` | 保存、载入专有状态；框架已绑定 `Domain` 上下文 |
| `Dispose` | 释放领域实现持有的图形资源 |

`SuspendLifecycle` 可以在专有复活流程期间阻止自动退出判断。`CasterIsPresent` 可扩展对特殊移动载具的识别。`HandlesEntryView` 表示由实现的转场接管进入时的镜头切换。

`RequestReturn` 发起正常退出，`Collapse` 发起崩塌退出。原地图不可用时框架尝试返回其他玩家家园；没有目标地图时保留领域和人员并报告错误。部分转移失败时保留参与者记录，允许后续重试。默认不提供旧版血海领域存档的迁移。

对抗中只要另一方仍活动，主动取消与崩塌都只解除本方领域，人物及尸体留在共同地图，记录为 Trapped。最后一个领域结束时，框架在退出遮盖完成后，先按各方自己的来源记录送回滞留人员，再返回最后一方并回收地图。不要通过 Returned 撤销必须立即失效的能力；这些清理应放在 Dissolved。

`DomainDef.surface` 可选。基础领域启用 `DomainSurfaceDef.screenBorderWidth`，使用同一特征材质绘制屏幕四周；默认宽度为零，无特效领域不会自动增加边缘。自定义屏幕 Shader 若要启用，需实现 `_SurfaceReveal.w=2` 的边缘模式。

## 血量和外部载体

`Health`、`MaximumHealth`、`HealthFraction` 属于 `MapComponent_Domain`。`DrainHealth` 和 `RestoreHealth` 更新共享血量，并同步外部载体的整数生命值。归零只触发一次崩塌；崩塌后的领域无法通过恢复血量重新激活。

对抗双方入场解锁后，每游戏秒默认各消耗自身最大血量的 0.5%，由 DomainDef.contestHealthLossPerSecond 配置。共同战场每游戏 tick 只结算一次双方消耗；一方关闭、入场锁定或规则暂停时双方均不消耗。自然消耗不触发受击反冲，不受减伤影响。领域施术者在本方真实领地内承受伤害时，先获得 30% 减伤，再消耗领域血量，溢出伤害继续作用于人物；其他参与者和外部载体没有这项主场减伤。

技能可选择对抗中的任何合法格子。框架仅提供 OwnsCell / DomainAt，不统一修改技能强度。血海通过自身 DefModExtension_BloodSeaDomainRules.enemyTerritorySkillFactor 配置敌方领地强度，当前为 0.7：潮汐按命中位置削弱伤害及击退，触手按目标位置缩短控制时间并按每次伤害位置削弱伤害，穿行按目的地增加耗时。

领域技能使用 Domain.ProtectTerritory(cells, durationTicks, source) 保护当前己方格子，返回可主动解除的编号；可绑定来源建筑。保护优先于主线和占领，计入己方面积，其他区域补偿；保护面积超过血量配额时成为本方最小面积。到期、来源被毁或本方领域结束时失效。它是真实格子保护，地形、建筑、归属与基础屏幕边缘使用相同结果。

运行中生成的建筑可调用 Domain.RegisterStructure(building, parkOnTerritoryLoss) 登记，必须已生成在本方完整占地内。默认失地收起原对象，设为 false 可留在失地处；效果是否停用或允许夺取由建筑自身规则处理。两种建筑都随本次领域保存和清理。回血、特殊建筑和守区技能的具体消耗、冷却、名称与表现由子 Mod 提供，框架不自动授予新技能。

对抗双方的领域主在敌方实际领地连续停留 180 游戏 tick（3 秒）后，以当前位置为中心夺取原版半径 2 格范围，并在确实夺到敌方格子时恢复 300 领域血量，上限仍为最大血量。在敌区移动不打断计时，回到己方领地、倒地、死亡、离图、入场锁定或对抗结束会清零；暂停及同 tick 的重复分区刷新不推进计时。转化后脚下成为己方领地，原地不重复回血。

占领区计入己方总面积并补偿主线；超过血量配额时形成最小据点面积。每格持续位于敌方主分界一侧时累计暴露，默认 600 tick 后失守，由 DomainDef.contestClaimErosionTicks 配置；己方主线推进到该格或保护生效会重置计时。入场锁和规则暂停不累计暴露，失守本身不回血，暴露进度随占领格保存。保护区优先，完全被保护的占领尝试不回血。双方同时占领的重叠格归距离更近的一方，等距保留原归属。地形、建筑、技能、主场减伤及屏幕边缘读取统一实际归属。公共接触带在占领格和保护格内裁掉，避免装饰材质覆盖已确定的领地。

`Hediff_DomainProtection` 为施术者安装共享伤害吸收组件，优先消耗领域血量，超出部分继续作用于施术者。它只保护关联领域的施术者，退出时由框架移除。外部载体直接消耗同一份领域血量，使用定义中的单次受伤上限，避免再经普通建筑受伤流程重复扣血。

## 转场

`Comp_DomainProtection` 通过原生伤害回调保护领域施术者：执行已配置的展开能力、活动展开物件存在、同步进入领域以及入场锁定期间，伤害置零并跳过后续处理，不消耗领域血量。展开和入场结束后恢复通常的领域血量吸收；其他参与者、普通能力和已失效展开不会获得该无敌。死亡或倒地人物不适用展开保护。

`DomainTransition` 管理地图切换、等待领域地形完成绘制、入场和返回期间的人物锁定、过渡结束及资源清理。具体实现提供 `Duration`、`CoverProgress`、`RevealProgress`、`DrawEffect` 和 `ReleaseVisuals`。基础截图、材质和动画由 DomainMaterialTransition / DomainOpeningVisuals 提供，特殊表现仍可由子 Mod 扩展。

使用 `CQFDomainRuntime.Instance.SetTransition` 管理当前转场。展开中的视觉对象可继承 `DomainOpening`，通过 `QueueOpening` 调度完成操作。返回锁定使用 `MapComponent_DomainTransitionLock`，结束时恢复人物原有的剩余眩晕时间。

1.7 基础与血海退出均由 `PrepareReturn` 等待 `ReturnCovered`；只在覆盖帧完成后切回来源地图，再由 `Returned` 绑定同一转场揭幕。不可见退出直接返回。血海返回材质在 0.42 的覆盖点到 0.46 的揭幕点之间保持不透明，不改变原有入场展开效果。

`DomainOpening` 必须提供 `Definition`、`Caster`、`Active` 和两个完成方法。基类在 `SpawnSetup` 自动登记展开物件，在 `DeSpawn` 自动移除登记；覆盖这两个生命周期方法时须调用基类，以便所有领域共用展开保护，读档后也会重新登记。

## 基础展开与换皮

`DomainDef.visuals` 可引用 `DomainVisualDef`，中性预设为 `CQF_DomainVisuals`。配置贴图、噪声、颜色、流动、扭曲、半径和时间即可换皮。普通领域使用 `DomainWorker_Visual`，通过 `DomainOpening_Basic.Start(definition, caster)` 展开。展开阶段默认 6 游戏秒，周围生物可以移动和反展开；闭合阶段默认 2 游戏秒，此时锁定并捕获真实范围内的全部存活生物，持续补收闭合期间新进入者。openingTicks / closingTicks 分别配置两个阶段，原材质进度按 foldStart 分段映射。基础 Shader 保证切图期间全遮盖，1.7 的通用 Worker 验证原版地形实际提交后揭幕。

`DomainOpeningVisuals` 和 `DomainMaterialTransition` 支持传入自定义 Material 模板，框架复制并释放自己的实例。通用资源在 `Content/1.6` 和 `Content/1.7`，构建时与模块 DLL 一起复制到输出；发布必须同时部署 Defs 和 Shaders/AssetBundles。无 Domain 请求时不加载这些资源，不新增补丁或 GComp。

详细字段、材质参数和构建方法见 [基础领域视觉](../Skill/cqf-overview/references/domain-visuals.md)。寂屋邪祭保留原有展开视觉，并已接入通用平面与领域对抗；框架不引用血海程序集或专有资源。

测试能力 `CQF_TestDomainExpansion`（展开基础领域）可在开发者调试动作的 CQF 分类中选择人物授予。第一次施放使用测试地图并播放展开，目标由展开范围确定；领域内再次施放返回。1.7 默认预加载，1.6 测试定义不预加载。

## 验证

`dotnet run --project .QuestEditor_Library/Tests/Domain/Checks.csproj -c Release` 使用实际游戏程序集检查领域血量、伤害吸收、外部载体同步及配置错误。

`.QuestEditor_Library/Tests/DomainRuntime/RunChecks.ps1` 使用独立配置与存档目录运行血海领域集成检查，覆盖进出、尸体与物品返回、领域复用、崩塌和存档往返。临时测试 Mod 在运行结束后移入本次日志目录归档，不修改玩家配置与存档。

领域空地图休眠时移出 Game.Maps，由统一组件托管；Thing 按原版未生成处理，唤醒时复用原图并恢复索引与 Tick 登记。框架不补丁 Thing.Map/Spawned 或地图更新入口。CQF_ownedHibernatingMaps 深度保存休眠地图，单图保存范围内临时提供区域索引，Thing 保持未生成；3.7 已删除临时旧格式迁移，只读写当前格式；未迁移旧档须先用带兼容修复的 3.6 加载并重新保存。
