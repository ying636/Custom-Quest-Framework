# CQF 版本与构建

源码位于 `D:\Game\Steam\steamapps\common\RimWorld\Mods\CQF\.QuestEditor_Library`，与 Mod 包共用 Git 仓库。技能维护源放在其中的 `Skill`；更新时同时维护 `C:\Users\HaiLu\.codex\skills` 下对应安装副本，保留两处已有的不同内容。

当前 CQF 发布版本为 `4.2`，版本字段为 `About/About.xml` 的 `modVersion`。领域自 3.4 起拆为按需加载的独立模块，继续共用地图休眠系统的唯一 GComp。

## 4.2 HCF 联动合并（2026-10-20）

配套 HCF 3.16，将加入巢群、设置群组模式、原有双语文本及检查迁至 HCF 的 `Mods/CQF` 工程，与任务书目标和完成信号共用 `HiveCreatureFramework.CQF.dll`。CQF 删除双版本 `Mod/HCF` 入口、旧 `HCFWithCQF.dll` 及独立项目；主库 DLL 不变。保留行为类型全名和配置字段，不新增旧存档迁移。两个 Mod 须同时更新，避免旧联动副本残留。

联动在 HCF 最新上游 `c86ad09` 基础上完成 1.6 / 1.7 编译，均零警告、零错误。1.7 离线 41 项检查通过，实际原生启动的仅 HCF、仅 CQF、双开组合共 30 项检查通过；最终成功运行无 ERROR。1.6 仅编译验证，未验证完整 Lord 调度或编辑器视觉。迁移文本逐项保持原文，所有加载目录存在。复跑入口与验证清单由 HCF 仓库维护。

## 4.1 HCF 行为兼容（2026-10-20）

恢复 CQF 自有的加入巢群和设置群组模式行为，补齐中英文 Keyed 文本、1.6 / 1.7 条件加载和发布 DLL。兼容代码针对已发布 HCF 3.15 构建，双版本编译零警告、零错误，1.7 离线检查 32 项通过；执行端使用探针记录调用，尚未验证完整游戏中的 Lord 调度或编辑器视觉效果。不恢复旧存档字段或旧模式枚举。

## 4.0 发布范围（2026-10-20）

大版本从 3 升到 4，小版本归零。本次发布当前 CQF 仓库已完成的 1.7 领域系统：反展开与重叠展开进入共同战场、领域通用平面与融合表现、按血量比例推进的固定地图分区、不规则真实侵蚀、保护与建筑登记、领域主连续入侵 3 秒占领半径 2 格并恢复 300 血量，以及取消后的滞留和最终返回。包含双方归属查询一致性及合并建筑朝向/回滚修复，源码、运行 DLL、Defs、Shaders、接入文档和回归检查成套提交。

采用此前已验证的 1.7 运行文件，不因版本号修改重新生成二进制。最新验证为 369 项逻辑与原生 Shader 检查、22 项针对性实机检查、92 项滞留回归及五次完整重载。实机在 1.7.4982 独立副本完成；主动注入的合并失败属于预期回滚检查。1.6 仅通过临时源码编译，本次保留其原运行包，不宣称新增领域功能或第三方兼容性已在 1.6 验证。placingDraggableDimensions 反馈尚未取得原始日志，未据此改动 XML 或标记修复。

血海专有适配与退出材质在独立寂屋邪祭仓库维护，不包含在本次 CQF 提交内；验证使用本机已配套的寂屋邪祭构建。接入当前血海对抗时须同时使用对应适配版本。领域实例拆分与完整转移事务仍属于后续设计，不是 4.0 已完成内容。不新增旧存档迁移。

## 历史开发记录

下文保留各次开发当时的版本、发布状态与检查结果；已实现的 CQF 改动本次纳入 4.0，当前发布范围以上节为准。

3.8 修复 1.7 基础领域地形就绪检测时机、退出全屏遮盖后再返回、底盘深度与绘制层级；启用预生成休眠地图，并配置可攻击的外部领域建筑和施术者共享伤害 Hediff。基础领域最大血量明确为 1000，能力提示及外部建筑显示当前/最大血量。领域与地图休眠的开发者选项使用英文；本次更新 1.7 主库及 Domain DLL/Defs，1.6 包保持原版本行为。55 项领域检查、32 项视觉/配置/Shader 检查通过，休眠调度与保存检查通过；实际游戏动画和选中范围仍需重启游戏验证。

下次更新的领域对抗方向已获用户确认，3.8 不实现：同一对抗地图持有两个领域实例，各自保存施术者、Worker、血量、原地图载体和返回记录。使用当前绝对血量计算面积配额，甲方比例为 H甲/(H甲+H乙)，两者均为零时共同结束，禁止除零。用连通分区推进边界，只更新边界附近的地形和领域效果，不随占地变化搬动人物。规则与视觉依据所属分区计算；一方归零仅结束该实例并返回其参与者，胜方继续维持，全部结束后回收休眠。现有 MapComponent_Domain 的单 caster/worker/participants 结构及整图清理 Recycle 不能直接承担双领域生命周期，须先拆出实例状态和共同战场管理；仍由唯一 GameComponent_MapManager 托管地图。首版触发条件、连通分区算法、边界速度和第三领域加入规则尚待下次实现时确定，不把本段当作已完成的功能。

本机未发布的领域对抗开发见 [领域对抗](domain-contest.md)：两个入口、独立实例及共同战场，完整地图内按当前血量比例平滑移动贯穿地图的直线；血海对抗期间不随血量减少内部填充。基础领域和外部建筑最大血量已与血海对齐为 100000，开发者入口为地图点选授予 Ability。CQF 与寂屋邪祭 1.7 须使用本次成套构建，不能只换其中一个 DLL；3.8 发布版本字段暂不变。

最新本机调整：基础领域增加可配置的灰白屏幕边缘，复用通用平面算法，不替换展开动画。对抗中主动取消与崩塌都保留人物和尸体，保存为 Trapped；撤销本方规则、保护、载体、布局和边缘，剩余方立即接管全图。最后一个领域结束时按双方各自来源记录返回。Dissolved 处理领域规则结束，Returned 仅处理实际返回。当前领域 56 项、对抗 83 项、视觉 90 项通过。

2026-10-19 已按用户确认加入实际侵蚀孔洞：1.7 强势方占比超过 70% 后在弱势方腹地逐渐形成三处侵占区，90% 时达到最大强度；孔洞计入总面积，解析求解补偿主分界线。地形、建筑与血海技能均使用同一归属；回血闭洞，失效后清除侵蚀并由剩余领域接管。通用曲线网格同时支持特殊材质、普通地形和普通 surface 绘制器，保留原有展开动画；基础屏幕边缘按实际孔洞裁剪。新增 RSH 1.7 地形补丁，将隐藏血海领域地形的 edgeType 覆盖为 Hard，避免原版邻格渐变使用 BadGraphic 造成红色格状边。无需重建 RSH DLL，但必须同时部署该补丁。

本轮侵蚀规则 43 项、原对抗 83 项、普通领域 56 项、原生 Shader/配置 90 项通过；七种侵蚀场景 169 项、原融合 70 项及混合滞留/完整重载 92 项实机回归完成。侵蚀场景包含一次完整重载与双方建筑/技能归属检查，滞留回归包含五次完整重载。已检查新的孔洞实机图，修复首轮发现的血海红色格边。60×60 分区更新采样约 0.03–0.16 毫秒，稳定和移动预热后均为零托管分配，数据不含渲染开销。独立 Shader 检查曾因进程目录未提供下游编译器加载路径失败，切换到原生运行目录后完成 90 项；这不是游戏内 Shader 编译失败。1.6 只在临时目录编译，实际侵蚀初始化仅启用 1.7；未更新 1.6 包，未调整版本字段或推送 GitHub。

本次混合领域滞留 92 项、纯基础滞留 88 项、融合 70+55 项、建筑布局 20 项和混合入口/合并 20 项实机检查通过；滞留检查包含五次完整重载、双方同时崩塌和施术者尸体返回。另有一次补测停在原版启动构造阶段，未进入世界或领域生成；重建 1.7 测试输出后重跑产生 COMPLETE，暂未复现，不能将该次停滞归于领域预生成。完整重建报告 1981 条警告，主要来自主库既有可空注解，另含测试夹具提示；无编译错误，未扩展到全库警告清理。

开局耗时检查使用当前开发配置的独立副本与临时测试 Mod。一次分段采样：启动构造阶段到达 22.17 秒，世界生成 9.734 秒，主地图生成 10.856 秒；四张 60×60 领域地图（含绘制与模板准备）共 224 毫秒。另一轮同配置四图为 305 毫秒，启用/关闭预生成的单次总时间分别约 44.87 / 43.55 秒；样本存在波动，不能把总差值全部归于预生成。该开销不足以解释几十秒等待，保留预生成。它只在新建/载入游戏后的管理器更新执行，不在进入主菜单前执行。测试进程并发固定为 1，数据不代表任意现有存档的加载时间。启动分段通过临时测试 Mod 的 Harmony 采样，正式领域模块仍无 Harmony 补丁。

2026-10-19 进一步检查改用用户实际 26 个启用 Mod 的独立副本、正常 32 逻辑处理器、固定世界种子与起始格 `71307,0`。未限制并发的详细采样：世界生成 4.512 秒、主地图 4.084 秒，其中原版植物生成 2.159 秒；四张领域地图完整准备约 0.2 秒。资源阶段 VMech 贴图读取 2390.809 毫秒，CQF 构造约 199 毫秒，语言、音效引用解析、VEF 初始化、图集绘制也有显著开销。不能将先前单处理器采样的绝对时间视为正常游戏性能。

使用原生 1.7 `-BakeModAssetPacks=<mod目录>` 验证 VMech 的资源预处理，保留全部原 PNG、音频和尺寸；202 张贴图与 3 个音频烘焙成功，32 个重复纹理载荷去重。预处理后详细采样的 VMech 贴图读取为 3.858 毫秒，该阶段缩短不能直接当作完整启动收益。关闭详细日志的两组顺序对照，未预处理启动为 31.79166 / 31.64560 秒，预处理后为 31.00559 / 30.28070 秒，平均约缩短 1.08 秒；仅代表本机这组固定配置和种子。两组对照均完成四图预生成且无 ERROR，202 张贴图的实际加载尺寸逐一核对，三组主要人物图与原 PNG 同屏检查，三个打包音频解压后逐字节哈希一致。现存第三方/原版非二次幂贴图及静态初始化警告仍在，未以关闭警告掩盖。诊断夹具新增临时编译目录、正常并发、固定种子、生成步骤计时、详细日志和资源画面检查；只在测试进程安装采样补丁，正式领域逻辑和预生成策略不变。

该资源优化已应用本机 VMech：根目录与 `1.7` 各生成 `Assets.zip`、`.fingerprint`、`.roles`、`.stamps`，两个资源包与通过检查的副本逐字节一致，205 个原始图片/音频文件哈希保持不变；fingerprint 含目录身份，不要求不同路径间一致。VMech 的 `.VMech/Tools/Build-AssetPacks.ps1` 使用独立保存目录调用原生烘焙，运行游戏时拒绝重建，失败保留日志并报错。构建与资源工具均不改用户配置和存档。本次未覆盖正式 CQF/RSH DLL，未变更版本字段或推送 GitHub。诊断夹具曾因继承方法的 Harmony 采样目标和截图异步写出等待不足失败，已分别修复方法声明定位和有界等待后复测完成，不能把这两次夹具失败记为游戏功能失败。

本轮本机 1.7 更新加入同步展开直接融合、独立 `DomainSurfaceDef` 通用平面与世界/屏幕绘制接口。基础灰白流淌、血海地形及融合侵蚀共用各自材质算法。原地图由同一绘制器管理接触波纹和侵蚀；保留罩子，只裁切交叠部分，外部建筑延续同一效果。基础领域新增真实特殊地形 CQF_BasicDomainTerrain 和专用地形层。每个 preload 领域准备两张休眠地图并缓存原始地形与建筑蓝图，反展开不再临时唤醒模板，融合只唤醒共同战场；缓存随游戏深度保存。当前 56 项领域、83 项对抗、90 项视觉/Shader 检查通过；七种融合实机场景 70 项通过，另增加无特效领域五种融合配对 55 项。建筑布局检查覆盖真实保存重载、原对象/血量/品质、阻挡等待、摧毁不复活、容器人物释放、退出复原及精确合并；基础双领域 18 项、基础/血海 20 项完整游戏保存重载检查完成。surface 已为可选项，对抗分界增加局部材质侵蚀、推进压力与受伤反馈，真实归属仍为直线。宿主地图组件密封，第二方显式构造并深度保存，修复挑战方漏存。保留固定网格、Section 合并刷新、相同地形跳过、屏幕材质延迟创建和等待退出零分配。独立副本为 1.7.4982 rev1280；1.6 仅临时构建检查，未更新包。后续架构建议见 [领域系统设计](domain-design.md)。

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

基础领域视觉资源位于 `CQF.Domain/Content/<版本>`，构建自动复制 Defs、GLSL 或 AssetBundle 到领域模块输出目录；发布必须与对应 DLL 同时部署。参数、换皮入口和 Shader 构建见 [基础领域视觉](domain-visuals.md)。寂屋邪祭本次不接入新视觉，留到下次更新。

完整打包须包含同一版本的主库、领域模块和已更新引用的子 Mod。不要混用拆分前后的 DLL。字段、调用和加载验证见 [领域系统开发参考](domain-system.md)。

## 原生回调替代补丁

- 1.7：`MapComponent_CustomMapData` 订阅 `Map.events.ThingSpawned/ThingDespawned`，FinalizeInit 幂等绑定并在 MapRemoved/Dispose 解绑，同时取消活动地图尚未提交的休眠请求，不唤醒已休眠地图；离图事件触发时 Pawn 已不在地图，使用事件所属地图维护缓存。1.6 没有该接口，保留两个 Pawn 进出补丁，使用编译条件隔离。
- `LordJob_Custom.Notify_BuildingAdded` 给受管建筑附加 `Comp_CustomLordBuilding`，原版认领流程直接调用 `CompPreventClaimingBy`。CreateGraph 在读档重建时恢复组件；退出该 Lord 后限制立即失效。普通建筑不增加组件。
- CQF 及编辑器导出入口使用 `CQFSerialization.XElementFromObject`，递归处理 ISaveable、IntVec3、集合、Def 引用、Class、Unsaved 和 DefaultValue；不再修改全局 DirectXmlSaver。
- 领域保护使用 AbilityComp / ThingComp，详见 [领域系统开发参考](domain-system.md)。没有全局 Pawn.PreApplyDamage 补丁；准备和读档恢复均复用同一施术者组件。
- 建筑受伤、人物受伤和建筑销毁三个补丁本轮保留：ThingComp 的受伤回调位于原方法内部，不能覆盖建筑提前免疫返回后的现有通知；PostDestroy 又晚于离图和 Lord 清理。替换前须单独核对触发次数、伤害来源、地图和 Lord 引用。
- 1.7 主库有 18 个补丁目标，其中休眠为 3；领域为 0，编辑器为 1，模拟经营适配为 2。1.6 比 1.7 多保留两个 Pawn 事件区进出目标，不保留 Thing 休眠唤醒补丁。
- 休眠地图移出 Game.Maps 并由统一组件托管，Thing 原版状态为 Spawned=false、Map=null。没有对象/区域属性、Thing.DoTick、WorldObject.DoTick、地图更新、Tick 登记、自动移除、GameEnder、保存或 AddMap 补丁；只保留选图、取图、退出清理。
- CQF_ownedHibernatingMaps 使用独立字段深度保存组件托管地图，不复用旧字段；CQF_pendingHibernation 引用保存排队地图。3.7 只读取当前格式，已删除 3.6 的旧字段迁移。缺失或冲突身份仍在改动地图列表前明确报错。单图保存时临时提供区域索引，Thing 仍未生成，finally 撤回。读档长事件结束时完成原版地图初始化及其延迟动作，再移出；普通唤醒不重复初始化。操作/销毁前显式激活，不通过原生物件生成/离图入口自动唤醒。

```powershell
dotnet run --project .QuestEditor_Library/Tests/EditorModule/Checks.csproj -c Release -- callbacks
dotnet run --project .QuestEditor_Library/Tests/EditorModule/Checks.csproj -c Release -- serialization
dotnet run --project .QuestEditor_Library/Tests/EditorModule/Checks.csproj -c Release -- hibernation
```

## 3.7 更新与存档边界

本次加入可换皮的基础展开与转场 Shader、通用展开物件、视觉 Worker 和测试能力，并修复测试口袋地图使用温带森林导致原版植物生长率计算访问无效世界格子的问题。测试生态使用独立的 CQF_TestDomainBiome，不新增补丁或 GComp；寂屋邪祭视觉本次未接入。

按用户预先要求，3.7 已删除 3.6 临时休眠兼容：旧 CQF_hibernatingMaps / CQF_mapPauses 读取、旧记录识别与归一化、迁移方法及临时字段均已移除。保留 CQF_ownedHibernatingMaps / CQF_pendingHibernation、当前格式深度读写、身份校验和原版初始化调度。测试改为 HibernationPersistenceChecks，覆盖当前格式再次保存/读取、跨地图引用、区域更新标记和损坏数据拒绝；原生集成夹具不再构造旧格式。

已用带兼容修复的 3.6 成功加载并重新保存的文件可继续使用当前格式。未重存的旧档及旧备份仍须先用该 3.6 版本转换；仅看到版本号 3.6 或经过一段时间不代表已转换。不得重新引入本次删除的迁移壳。

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
- HCF：自 CQF 4.2 / HCF 3.16 起，所有双向联动统一由 HCF 的 `.HiveCreatureFramework/HiveCreatureFramework/Mods/CQF` 工程维护，输出双版本 `Mods/CQF/Assemblies/HiveCreatureFramework.CQF.dll`，由 HCF 的 `IfModActive="HaiLuan.CustomQuestFramework"` 条件加载。包括 `CQFAction_JoinHiveGroup`、`CQFAction_SetHiveGroupMode`、五种任务书目标与完成信号。两框架主库均无对方的程序集引用，无新增 Harmony 补丁。配置、构建和检查见该 HCF 工程的 README.md；CQF 不再提供 HCF 联动项目、DLL 或重复翻译。

已删除无对应 Def/运行类的 `CQF_DestroyMainSiteCountdown` 残留翻译，并删除错误短类型目录和重复英文 PawnMod DefInjected。自定义 Def 的中文翻译使用完整命名空间目录。

`Tests/GameRuntime` 的 `version17` 标记模式验证游戏内编辑器开关、编辑器地图、子地图和站点地图生成。使用独立 `-savedatafolder` 与临时测试 Mod，结束后归档测试 Mod；不要改用户实际配置或存档。原 AI HTTP 综合测试属于另一套测试，不能替代此版本适配验证。

3.7 当前格式休眠检查 56 项通过：使用真实原版 Tick 列表验证 Normal/Rare/Long 暂停与恢复，使用原版 Thing 序列化及真实 Scribe 验证组件托管的 Map 夹具深度保存、再次存读档、跨地图引用、原区域更新开关和损坏数据拒绝。Map.ExposeData/FinalizeLoading 使用精简夹具；旧格式迁移用例及 CQF_CHECKS_LEGACY_SAVE 入口已删除。空地图休眠与唤醒不调用全局 Tick 移除，预热后 256 次宿主更新为 0 字节分配；框架子地图入口检查总地图容量。

当前领域发布验证：1.6 / 1.7 编译、52 项领域规则与组件生命周期、29 项 Shader/测试能力/生态检查及上述 7 种模块加载场景通过；两个 Unity Shader 已回读，10 次 GPU 像素检查验证全遮盖、端点透明、贴图换皮及径向裁切。空闲宿主在模块启用/禁用两种情况下，预热后 256 次更新均为 0 字节分配。此前游戏内集成测试在入口前因 `D3DSCache.dll`、异常码 `0xc0000409` 退出；最新隔离框架测试在启动加载阶段 180 秒超时，仍未产生 COMPLETE。临时框架与测试 Mod 已归档；这些隔离运行未修改原存档。基础视觉测试包后来按用户确认安装至本机 1.7，完整动画仍需实际游玩验证；不能据此宣称完整旧存档、领域进出或视觉转场已通过游戏内验证。

## 本机领域领地战更新（尚未发布）

CQF.Domain 与 RSH 的 1.7 本机运行文件同步更新：双方每秒自然损耗最大血量 0.5%，主场施术者先减伤 30%，不规则真实侵蚀孔洞，来源建筑或计时绑定的领地保护，以及运行中技能建筑登记。血海技能在敌区保留 70% 的对应参数强度，由 RSH 自己实现；未添加未指定的新技能或文案。

规则与原生 Shader 检查共 319 项通过（56 基础、83 对抗、43 侵蚀、47 守区、90 视觉）。本次实机检查包括 31 项战斗规则并两次完整重载、175 项七种侵蚀组合并一次完整重载、70 项展开融合回归、92 项滞留回归并五次完整重载；各运行均 COMPLETE，日志无游戏 ERROR。额外确认来源建筑刚被摧毁时保存不会留下失效引用。检查图片包含不规则真实地形孔洞和保住建筑的守区孤岛。

最终只部署 1.7 的 CQF.Domain DLL/PDB、相关通用 Shader 和 RSH DLL/PDB；RSH DomainDef 同步自己的敌区倍率配置。主库运行 DLL、1.6 运行文件与版本号保持；1.6 领域源码仅在临时目录编译确认。没有旧存档迁移。本轮未提交或推送 GitHub。

## 本机主动占领更新（尚未发布）

领域主在敌区连续 3 游戏秒后，转化当前位置半径 2 格并在成功夺格时恢复 300 领域血量。敌区移动保持计时，回己方清零；占领区保存到被夺回或本方结束，保护优先，原地不重复回血。双方同时触发按距离分配重叠格，等距保留原归属。自然消耗、技能倍率与主场减伤继续读取同一实际领地。

新增 42 项占领逻辑检查，现有 319 项基础/对抗/侵蚀/守区/Shader 检查全部通过。实机占领 18 项通过，两次完整重载覆盖未满 3 秒的计时及已占领区域；300 回血、双方夺回、建筑损伤保存、保护阻挡、双方同时回血和退出休眠均已确认。既有战斗规则 31 项及七种侵蚀组合 175 项本轮回归通过，三次实机运行均 COMPLETE、无游戏 ERROR。最初的占领测试因读档后游戏恢复运行，导致断言前已发生占领回血；测试恢复点显式暂停后精确通过，未为测试误差改变玩法参数。

公共接触带新增按固定格裁剪的复用网格，避免保护格和占领格被另一方材质覆盖；不改原领域展开动画或各领域 Shader。1.6 领域源码仅在临时目录编译通过。本次只更新本机 1.7 CQF.Domain DLL/PDB，保留旧运行文件备份；RSH DLL、主库、版本号及 GitHub 均未变更。当前格式保存占领与计时，未增加旧版迁移。

## 本机领域审查修复（2026-10-20，尚未发布）

修复双方实例的共同战场查询不一致；合并和失败回滚保存物件朝向，完整占地检查与实际生成一致，并确认生成成功；血海可见退出等待自身转场完整覆盖后才迁移人物和镜头，原返回材质延后揭幕，不修改入场展开。同步更新接入文档和针对性检查。实例拆分及接管实例后的完整事务回滚尚未实施。

369 项逻辑与原生 Shader 检查通过（56 基础、91 对抗、43 侵蚀、47 守区、42 占领、90 视觉）。真实独立游戏副本中的 22 项修复检查通过：横向床合并与异常回滚保留朝向、占地、血量和相邻墙；双方查询一致；对抗幸存血海、独立血海及不可见返回均完成回收。已检查覆盖与返回完成的四张截图。另有 92 项滞留回归及五次完整重载通过。修复检查主动注入一次 CQF_EXPECTED_MERGE_ROLLBACK_CHECK 错误验证回滚，此预期错误有日志；成功运行没有其他游戏 ERROR，滞留回归没有游戏 ERROR。首次修复检查因测试过早访问领域管理器失败，调整到地图加载后重跑通过。

已备份并部署测试副本完全一致的 1.7 CQF.Domain DLL/PDB、RSH DLL/PDB；RSH 返回 Shader 与测试文件一致。备份位于 C:/Users/24904/AppData/Local/Temp/CQF_DomainReviewDeployment_0e27dd19fc2f4003bd86617b91347ccf。1.6 领域源码临时编译通过，但未更新 1.6 运行文件。主库、版本号、GitHub 保持不变，无旧存档迁移。
