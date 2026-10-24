# CQF 基础领域视觉

CQF 可选 Domain 模块提供基础地面扩散、幕墙升起/闭合、进入与返回转场，以及独立的领域通用平面。通用平面表示领域的基础视觉特征，不能据此替换领域原有展开动画。寂屋邪祭保留自己的血海展开，血海平面和地形共用材质算法；CQF 不依赖血海程序集。

领域内反展开使用全屏转场替代再次升起罩子：保留 360 tick 准备时间，闭合开始即播放新领域自身的进入特效，遮盖完成后才交接对抗领地。基础领域复用 DomainMaterialTransition，血海复用 BloodSeaDomainTransition；没有修改这两种材质算法。双方入场锁由同一转场管理，等待地形帧就绪后揭幕。原地图的地面扩散、罩子、闭合与双方接触融合流程保持原入口。

自定义 DomainOpening 派生类应使用 IsCounterOpening 区分领域内外演出，以 ReadyToTransfer 判断转场准备完成，完成转移后通过 BindEntryTransition 绑定对应领域实例。领域内跳过世界绘制、人物折叠载具和 SupportsFusion；CounterOpeningValid 在宿主不再接受挑战时使当前展开失效。框架保存此模式；默认不迁移旧展开存档。

## 通用领域平面

`DomainDef.surface` 引用独立的 `DomainSurfaceDef`，与 `DomainDef.visuals` 分别配置。基础领域引用 `CQF_BasicDomainSurface`，使用灰白流淌；血海引用 `RSH_BloodSeaDomainSurface`，复用 `RSH_BloodMarshTerrain` 的算法。通用平面不能代替领域罩子；单独展开沿用原有动画和材质算法，融合只增加启用时生效的交叠侵蚀分支。

surface 可以完全省略，纯地形与建筑领域不需要虚构一份材质才能参加融合或对抗。公共绘制器只实例化已配置的平面；缺失一方平面时，屏幕转场增加中性遮盖以保证切图屏障，不会在领域地图创建整图颜色面。融合资格由展开器与 Worker 的能力决定。`contestColor` 只为公共接触带提供该领域颜色。

`DomainSurfaceDef.screenBorderWidth` 默认 0，可选范围 [0,0.3]，按屏幕高度控制四周宽度；`screenBorderOpacity` 默认 0.75，范围 [0,1]。基础灰白平面启用宽度 0.13。MapComponent_Domain 绘制 DomainScreenBorder，复用特征材质算法但采用独立屏幕坐标流动，中央保持透明；对抗时按当前镜头映射的实际分区裁剪。本方边缘在退出全屏遮盖后的领域解散阶段释放，读档后按需重建。原有展开与返回材质不变。

对抗中可见取消与崩塌复用本方原有返回转场：PrepareReturn 等待 ReturnCovered，随后在全屏遮盖下交接真实领地，直接将同一转场 BindReturn 到共同地图。人物保持原位，不调用表示跨图返回的 Returned。揭幕检测同时接受活动领域的 LastTerrainRenderedFrame 和原版地形提交；DomainWorker_Visual 在返回转场期间也上报特殊地形帧，避免基础或血海接管后因没有原版地形层而卡住遮盖。退出期间切换观察地图仍释放该转场，无特效 Worker 保持自身默认退出行为。

屏幕 Shader 使用 `_SurfaceReveal.w=2` 表示边缘模式，x 为宽高比，y 为宽度；`_SurfaceCircle.x/y` 为争夺边界世界 x 与本方方向，方向 0 表示整张屏幕边缘。常规揭幕只处理 0.5 < Reveal.w < 1.5。自定义领域若要启用屏幕边缘，须在自己的屏幕入口实现这一模式；不启用时无需增加 Shader 工作。

`DomainSurfaceDef.CreateMaterial(bool screen)` 创建调用方拥有的材质；`shader` 提供世界材质，`screenShader` 可指定同算法的屏幕入口。`Configure(MaterialPropertyBlock, time)` 和 `Configure(Material, time)` 分别提供世界与屏幕参数扩展，血海在自己的 Def 子类中设置活动、填充和时间参数。

`DomainSurfaceVisuals.Draw(mesh, matrix, time, circle, rival, opacity, clash)` 绘制任意网格；`DrawScreen(rect, view, circle, rival, reveal, time, opacity)` 用于屏幕展示。每个绘制器拥有自己的实例，禁止修改原版缓存材质。屏幕材质首次绘制时才创建；Dispose 与游戏结束清理都释放已创建资源。

平面 Shader 使用 `_SurfaceState=(时间,屏幕模式,透明度,启用)`、`_SurfaceCircle=(中心x,中心z,半径,圆形裁切)`、`_SurfaceRival=(对手中心x,中心z,半径,叠加层)`、`_SurfaceView=(原点x,原点z,宽,高)` 和 `_SurfaceReveal=(中心x,中心z,揭幕半径,启用)`。叠加层大于 1.5 时，在重叠区应用动态侵蚀遮罩，底层保留完整材质，避免双方同时透明造成孔洞。

血海展开底盘使用 RSH_BloodSeaDomainGround，复用原展开顶点和片元算法，仅启用深度检测并改用队列 2900；绘制高度位于倒地人物下方，展开与外部载体各自持有并释放材质。幕墙维持原材质与队列，使展开阶段自由行动的人物仍能显示。

基础世界与屏幕入口共用 `CQF_DomainSurfaceCommon.glsl`；血海地形和屏幕入口共用 `RSH_BloodMarshSurfaceCommon.glsl`，材质算法不复制两套。普通血海地形未启用 SurfaceState 时保持原地形行为。

1.7 的普通 GUI 材质管线提供颜色与 UV，不保证使用自定义世界顶点 Shader。屏幕片元入口必须由 UV 重建平面坐标，不能读取世界绘制的 `vWorld`。屏幕顶点入口另保留原生 GUI Frame 的 binding 1 和矩阵偏移，供原生阶段管线使用；世界入口维持自己的视图布局。

同步展开后的 `DomainFusionTransition` 调用双方通用平面叠加、侵蚀，并共用全遮盖与揭幕屏障。对抗区域使用各自的领域平面；已经通过专有地形绘制该特征的 Worker 设置 `RendersSurfaceTerrain=true`，避免再叠加一次全图材质。血海已声明该标记。自动侵蚀孔洞已取消，公共网格沿主线和实际固定格裁剪双方区域，无 surface 时直接展示对应真实地形。

对抗图的 DomainContestOverlay 使用 CQF_DomainContestFront。真实主分割线两侧各 1.6 格内，已配置的双方平面以互补波动遮罩侵蚀；有专用地形材质时也保留这段局部补绘。推进方向与目标差值形成压力，扣血产生衰减的反冲与波纹。`_FrontState=(边界,时间,压力,反冲)`、`_FrontShape=(半宽,首方方向,目标比例,冲击年龄)`。surface 使用 `_SurfaceClash.w=2` 与相同波形，原地图融合仍用模式 1。公共边缘保持半透明，实际建筑和人物继续按原深度显示；规则查询由同一主线、主动占领和保护归属决定。新增 surface 应实现模式 2 的局部遮罩；未提供 surface 时，地形网格和公共接触带仍能显示双方特征。

基础屏幕边缘额外接收 `_TerritoryState=(主线x,本方方向,预留,启用)`，按镜头映射位置裁剪真实领地。DomainSurfaceVisuals.SetTerritory 只用于边缘实例；原有 DrawScreen 方法签名保持，未使用该协议的材质不接收这些参数。原展开、融合和切图遮盖不使用此领地模式。

固定领地通过 _TerritoryProtectionState=(地图宽,地图高,本方是否首方,启用) 与点采样的 _TerritoryProtection 纹理传给基础屏幕边缘；纹理 r 为首方标记、g 为是否存在保护或占领覆盖。纹理按分区版本更新并随边缘实例释放，无固定格时不创建纹理。地形和 surface 网格在固定整格使用实际所有者，占领失守后同步恢复主线裁剪。

主动占领同样使用此整格纹理协议，g 现在表示是否有固定归属，r 为固定格的首方标记；OverrideAt 已先合成保护优先级。无保护、仅有占领时仍生成对应遮罩。公共直线与孔洞接触带也按实际固定格裁剪，保持任何领域的自定义通用平面都不会覆盖已占领地形，无需给每个领域 Shader 添加占领分支。

在已有领域外部建筑周围展开时，`DomainOpening_Fusion` 直接把该建筑与新展开配对；建筑保持完整半径，新罩子按原动画生长，接触后共用平面侵蚀和罩子裁切。接触至少一秒且展开完成后播放双方特征的全屏融合转场，在全遮盖下直接进入原有领域地图并开始对抗，读取新领域模板，不先创建第二张活动地图再合并。取消新展开会释放建筑配对；旧建筑先消失则解除配对，新展开恢复自己的流程。

大小领域的外部罩子分别保留自己的真实半径。DomainDef.OpeningRadius 优先读取 visuals.radius，无 visuals 时读取 openingRadius（默认 12）；血海的预览、捕获、底盘、幕墙、选区、配对距离与外部载体统一使用该值，不再固定为 12。地面与幕墙仍使用原材质，人物和内部地图尺寸不随之缩放。完全包围时，domainContactFront 平滑改为围绕小领域的闭合接触环，保留小领域内部，避免半径差使小罩子整块消失；普通相交沿用原接触场。罩子、双方通用平面与接触带共用此算法，血海维护同算法的独立 Shader include，普通未融合展开保持原效果。

原地图的接触动画由 `DomainFusionVisuals` 统一绘制两种平面及公共接缝，使用 `_SurfaceClash` 控制侵蚀、接触波纹和压力偏移。`DomainOpening_Fusion` 必须使用 RealtimeOnly，不能只播放屏幕转场。两侧罩子持续绘制，通过 `DomainFusionState` 提供同一世界坐标场的互补遮罩，只侵蚀交叠区域；未配对时参数为零，原展开效果保持。对抗后两个外部建筑调用同一 DomainContest 的绘制器，保留罩子并继续侵蚀。禁止通过淡出整个罩子替代融合。

基础领域 1.7 使用 `CQF_BasicDomainTerrain`，不再生成 Soil / PavedTile 网格。`DomainWorker_Basic` 和 `SectionLayer_BasicDomainTerrain` 以通用灰白平面材质绘制真实特殊地形，零肥力、可行走。原版地形层不重复绘制，接缝邻格交给 SectionLayer_DomainContest。就绪检测通过 `TerrainLayerType` 检查实际提交的专用地形层，不能等待被禁用的原版层。未提供地形名称与描述，中英文均保留同名占位符。

## 接入

领域 `DomainDef` 新增可选 `visuals`，引用 `DomainVisualDef`。未设置时现有领域行为不变。最小预设为 `CQF_DomainVisuals`，通用展开物件 Def 为 `CQF_DomainOpening`，所有资源都在请求 Domain 的可选目录内加载。

```xml
<QuestEditor_Library.DomainVisualDef>
  <defName>MYMOD_DomainVisuals</defName>
  <texturePath>MYMOD/Domain/Surface</texturePath>
  <noiseTexturePath>MYMOD/Domain/Noise</noiseTexturePath>
  <color>(0.06,0.06,0.06,1)</color>
  <secondaryColor>(0.3,0.3,0.3,1)</secondaryColor>
  <edgeColor>(1,1,1,1)</edgeColor>
</QuestEditor_Library.DomainVisualDef>
```

示例贴图路径和 MYMOD 前缀由子 Mod 提供，不是 CQF 已有资源。只用中性预设可直接在自己的 DomainDef 中设置 `<visuals>CQF_DomainVisuals</visuals>`。普通领域用 `<workerClass>QuestEditor_Library.DomainWorker_Visual</workerClass>`，具有玩法的 Worker 可继承它，并在覆盖 BeforeReturn / Returned / Draw 时调用基类，以保留通用转场。

```csharp
DomainOpening_Basic.Start(definition, caster);
```

`Start` 分为展开与闭合两阶段：默认展开 360 tick（6 游戏秒），仅约束施术者，周围生物保持在图、可以移动与反展开；闭合 120 tick（2 游戏秒），才收集半径内所有存活 Pawn 并锁定，血海此时才创建卷入载具。闭合期间继续收集新进入范围的生物；闭合前已离开者不进入名单。Progress 在展开阶段映射到 [0, foldStart]，闭合阶段映射到 [foldStart, 1]，沿用原地面、幕墙及折叠材质。涉及当前地图殖民者且未暂停时，开始展开将游戏速度降至正常。

双方展开配对后等待两侧就绪才共同闭合；检测同时识别尚在能力预热中的反展开，避免先完成的一侧提前锁人。闭合名册在释放血海载具后交给统一转移入口，交叠对象只登记一次；血海仅为敌对参与者创建进攻 Lord。锁定记录由 DomainOpening 统一保存与恢复，重叠锁使用原始眩晕结束 tick 并等待其他展开锁释放，不提前清掉更长外部眩晕；取消与完成均释放临时锁。Start 保留可选 targets 调用参数，但实际捕获统一以闭合时真实范围为准，不新增 GameComponent 或 Harmony 补丁。

## 外观和时间参数

| 字段 | 默认值 / 用途 |
| --- | --- |
| `texturePath` / `noiseTexturePath` | 可省略，使用白色表面和中性灰噪声；贴图使用重复 UV，应能平铺 |
| `color` / `secondaryColor` / `edgeColor` | 主色、流动次色、边缘色；默认中性灰阶，不绑定血海风格 |
| `flowSpeed` | `(0.1,-0.2)`，纹理流动速度 |
| `textureScale` / `distortion` / `edgeWidth` | `1` / `0.15` / `0.12`，纹理缩放、扭曲、边缘宽度 |
| `radius` / `curtainHeight` | `12` / `4.5`，展开与捕获半径、幕墙高度 |
| `openingTicks` / `closingTicks` | `360` / `120`，分别控制展开与闭合，随暂停和游戏速度变化 |
| `expansionEnd` | `0.55`，地面扩散完成进度 |
| `curtainStart` / `curtainEnd` | `0.48` / `0.72`，幕墙升起区间 |
| `foldStart` / `foldEnd` | `0.7` / `0.94`，幕墙闭合区间 |
| `entryDuration` / `returnDuration` | `2` / `1.8` 秒，转场使用真实时间 |
| `coverProgress` / `revealProgress` / `revealEnd` | `0.42` / `0.46` / `0.95`，全遮盖、揭幕开始、揭幕完成，必须严格递增 |
| `openingShader` / `transitionShader` | 1.7 ShaderDef 名，默认 `CQF_DomainOpening` / `CQF_DomainTransition` |
| `openingUnityShader` / `transitionUnityShader` | 1.6 Shader 名，默认 `CQF/DomainOpening` / `CQF/DomainTransition` |

换皮通常只需更换贴图、颜色和材质参数。需要复用 C# 绘制而完全替换材质时，可创建 `DomainOpeningVisuals(visuals, template)` 或 `DomainMaterialTransition(visuals, origin, returning, template)`；传入的模板会复制，框架只销毁自己的实例。自行使用展开绘制器时须 Dispose；统一运行对象还会在游戏释放时清理已登记绘制器。

自定义材质遵守参数约定：`_Progress`、`_EffectTime`、`_Radius`、`_Diameter`、`_Mode` 由展开绘制器提供；`_Mode=0` 是地面，`1` 是幕墙，幕墙 UV.x 为弧度，UV.y 为高度比例。通用预设提供 `_Color`、`_SecondaryColor`、`_EdgeColor`、`_Flow=(速度x,速度y,缩放,扭曲)`、`_Stages=(扩散完成,幕墙开始,幕墙完成,闭合开始)`、`_FoldEnd`、`_Height`、`_EdgeWidth`。

转场的 `_MainTex` 保留给原地图截图，换皮表面使用 `_SurfaceTex`；`_NoiseTex` 为噪声。转场还提供 `_SourceUV`、`_SourceSaturation`、`_Origin`、`_Destination`、`_Returning`、`_CoverProgress`、`_RevealProgress`、`_RevealEnd`。自定义转场 Shader 必须在 coverProgress 到 revealProgress 之间完全不透明；基础 Shader 会强制保证该区间的遮盖，不让表面透明度造成切图漏帧。

1.7 的 `DomainWorker_Visual.Update` 在转场期间检查视野中心原版 SectionLayer_Terrain 的已提交网格，确认实际绘制后记录 LastTerrainRenderedFrame；不每帧扫描所有地图或全部地形。原版 MapComponentDraw 在本帧 MapUpdate 提交地形前调用，不能在该回调检查待提交命令；MapComponentUpdate 位于地图绘制提交之后。切图后等待地形就绪超过 5 秒会记录错误并释放转场及入场锁，避免永久遮屏。特殊地形若完全替代原版地形层，需要自己的 Update / SectionLayer 提供实际地形帧标记，覆盖 Update 时须保留基类就绪通知。

## 游戏内测试能力

Domain 模块内提供 `CQF_TestDomainExpansion`，中文名称使用用户给定的“展开基础领域”。开发者模式打开调试动作，进入 CQF 分类的 `CQF_TestDomainExpansion`，使用原版 `ToolMapForPawns` 在地图上点选人物授予 Ability，再由人物使用能力展开；未征召也会显示能力。授予入口不缓存人物菜单、不强制选择人物，避免调试菜单持有已销毁人物引用。

1.7 在新游戏或读档后由 GameComponent_MapManager 驱动 CQFDomainRuntime，为每个 preload 领域提前保留两张地图并休眠，另缓存原始地形与建筑蓝图；基础地图为 60×60。基础测试领域 `CQF_TestDomain` 在 1.6 / 1.7 均设置 `preload=false`，开局不预生成，首次实际使用时由 Acquire 生成，后续施放唤醒复用，再播放原地图展开、幕墙和转场；基础展开同时转移范围内所有存活生物，包括动物、机械体和倒地人物。使用独立 CQF_TestDomainBiome（无野生动植物、禁用天空光照与阴影）和 CQF_BasicDomainTerrain，引用 `CQF_DomainVisuals`；1.6 保留原 Soil / PavedTile 网格。进入后再点同一能力请求返回；1.7 基础视觉通过 `PrepareReturn` 在领域内开始转场，实际绘出全屏遮盖后才允许人物与镜头返回，目的地图地形提交并经过完整帧后才揭幕。原有 Worker 默认仍调用 `BeforeReturn` 并立即返回，不改血海专有视觉。退出后的地图走统一 `Recycle → Hibernate`，再次施放走 `Acquire → Activate`，复用同一个 Map；`preload=false` 只关闭首次预生成，不关闭退出休眠。测试期间保持正常速度且不要暂停，以便展开 tick 推进。

定义位于 `Content/<版本>/Defs/CQF_TestDomain.xml`；授予入口为 `DebugActions_TestDomain`，施放组件为 `AbilityComp_TestDomainExpansion`，地形步骤为 `GenStep_TestDomain`。测试入口仍属于可选 Domain 模块，没有请求时不加载，测试能力不自动授予人物；寂屋邪祭维持自己的原实现。

2026-10-24 关闭基础测试领域预生成后的验证：RimDevTools 原生截图服务与框架运行接口在 1.7.4985 独立副本通过 9 项实机检查，开局测试领域地图与模板均为 0，首次 Acquire 只生成 1 张，后续获取复用且不补充预生成；其他启用预加载的领域仍保留 2 张地图。90 项既有领域视觉、Shader 与 XML 检查通过，1.6 / 1.7 的源码及运行 XML 均确认关闭预生成。证据位于 `C:/Users/24904/AppData/Local/Temp/CQF-Preload-c3d75ea4753a485ca066865a25ef2209/PreloadPolicyEvidence.json`，保留 2 张 RimDevTools 新截图；未使用 CU，没有进行 1.6 实机验证。

针对偶发问题，同日补充 6 次独立随机世界启动，每次进行 20 轮按需新建、回收休眠、唤醒复用及原版地图销毁，共 120 次新建、120 次复用、726 项断言。132 次子地图生成返回观察均存在领域组件，六轮游戏 ERROR 为 0、stderr 为空，保留 12 张 RimDevTools 截图；汇总为同一证据目录的 `StressSummary.json`。这些检查保持测试领域 `preload=false`，没有重放旧配置的自动预加载条件；原反馈的空引用仍未复现，不能将关闭预生成或本轮通过表述为已查明并修复其根因。

口袋地图通常没有有效的世界格子。测试地图不能直接使用 TemperateForest 等含可食用野生植物的生态：原版 Map.FinalizeInit → MapPlantGrowthRateCalculator.BuildFor 会模拟植物的日温度，直接索引世界格子温度缓存并越界。测试生态保留空 wildPlants / wildAnimals 和零密度，使用 Clear 天气；仅把 plantDensity 设为 0 不能阻止生长率计算，它依据 AllWildPlants 列表执行。不要用温度系统补丁掩盖测试配置问题。

## 资源与构建

源码资源在 `.QuestEditor_Library/CQF.Domain/Content/<版本>`，项目构建会把对应版本资源与 DLL 一起复制到输出目录。发布必须同时部署 DLL、Defs 和 Shaders/AssetBundles；只替换 DLL 会缺 Shader，只有新 Def 而没有新 DLL 则无法解析类型。

- 1.7：`Optional/Domain/Defs/CQF_DomainShaders.xml` 与 `Optional/Domain/Shaders/*.glsl`，由原版 ShaderDefRegistry 读取，不加载 Unity AssetBundle。
- 1.6：`Optional/Domain/AssetBundles/cqf_domain_win`，复用已加载包，不重复 LoadFromFile。Unity 源码和打包脚本在 `.QuestEditor_Library/ShaderSource/Domain`。
- Unity 构建使用实际项目对应的编辑器版本；项目 manifest 必须启用 `com.unity.modules.assetbundle` 和 `com.unity.modules.imageconversion`。缺失前者会产生不可回读的包，不能当作版本错误绕过。

```powershell
pwsh -File .QuestEditor_Library/ShaderSource/Domain/BuildBundle.ps1 -UnityEditor <Unity.exe> -ProjectPath <已有Unity项目> -OutputPath <临时输出目录>
dotnet run --project .QuestEditor_Library/Tests/Domain/Checks.csproj -c Release -- visuals
```

打包脚本回读两个 Shader，检查 UnityFS、Shader 名称和 pass，再通过 GPU 像素读回检查进入/返回遮盖及换皮表面。验证成功后将 cqf_domain_win 放回 Content/1.6/AssetBundles 再构建模块。1.7 的 visuals 检查使用游戏自带编译器编译包含接触带在内的十一组材质、二十二个阶段，并检查接口、材质布局、GUI 坐标、特殊地形、边缘配置和打包结构；当前 90 项。它不等于完整游戏内视觉验收，必须另检查原地图接触、罩子交叠、原地建筑和战场截图。测试指定 `CQF_CHECKS_PROVIDER_ROOT` 时应指向待发布包根目录，默认读取源码 Content 与模组目录。

1.7 的地面使用独立 CQF_DomainGround ShaderDef（groundShader），开启深度检测、不写深度、队列 2900；底盘高度为 LayingPawn 下方 0.01，覆盖 LowPlant / Building 层的草木，保留倒地和站立人物的深度遮挡。植物仅由展开底盘遮蔽，不销毁、不离图；展开进度与径向边界继续由 Shader 控制，展开取消或载体移除后恢复原显示。幕墙仍使用 openingShader 的 3601 / 3603 队列。更换展开 Shader 时应同时提供适配的 groundShader；自定义模板材质由调用方设置地面深度状态。基础领域配置 CQF_TestDomainBuilding，使用 Building_DomainVisual，在原施放位置保留可选中、可攻击的外部载体，复用通用共享血量、崩塌和返回移除流程。未提供名称和说明时，中英均保留同名占位符。

DomainHealthBar 复用血海的屏幕常驻血条样式，显示在人物栏下方，不使用 Gizmo，也不依赖选中施术者或外部载体。DomainWorker_Visual.OnGUI 与血海 BloodSeaDomainBar 共用绘制入口，实时读取各自 MapComponent_Domain 的 Health / MaximumHealth，保留领域名称与施术者名称。1.7 基础领域与外部建筑的最大血量均为 100000，与血海相同。对抗时主领域与挑战领域上下排列；一方退出后剩余血条回到首行。只在当前领域地图显示，遵守原版截图隐藏 UI 选项，结束后隐藏，并恢复绘制前的 GUI 状态。实际 UI 与植物遮蔽仍需游戏内查看。
