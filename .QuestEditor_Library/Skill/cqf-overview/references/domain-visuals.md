# CQF 基础领域视觉

CQF 可选 Domain 模块提供基础地面扩散、幕墙升起/闭合、进入与返回转场。寂屋邪祭当前仍使用自己的血海视觉，本次不修改，按用户要求留到下次更新再接入；不得把已有血海 Shader 或规则当作框架依赖。

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
DomainOpening_Basic.Start(definition, caster, targets);
```

`Start` 不自动选择敌我或半径内人物；调用方传入目标列表，框架加入施术者，筛除重复、死亡和不在原地图的人物。展开时停止寻路并暂时眩晕，取消/完成后恢复原有剩余眩晕，不清除新增的更长眩晕；展开物件支持保存/加载。此入口复用统一展开队列、地图池、跨图转移和保护机制，不新增 GameComponent 或 Harmony 补丁。

## 外观和时间参数

| 字段 | 默认值 / 用途 |
| --- | --- |
| `texturePath` / `noiseTexturePath` | 可省略，使用白色表面和中性灰噪声；贴图使用重复 UV，应能平铺 |
| `color` / `secondaryColor` / `edgeColor` | 主色、流动次色、边缘色；默认中性灰阶，不绑定血海风格 |
| `flowSpeed` | `(0.1,-0.2)`，纹理流动速度 |
| `textureScale` / `distortion` / `edgeWidth` | `1` / `0.15` / `0.12`，纹理缩放、扭曲、边缘宽度 |
| `radius` / `curtainHeight` | `12` / `4.5`，展开半径和幕墙高度，不决定目标筛选范围 |
| `openingTicks` | `180`，展开使用游戏 tick，随暂停和游戏速度变化 |
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

1.7 的 `DomainWorker_Visual.Draw` 在转场期间检查视野中心原版 SectionLayer_Terrain 的已提交网格，确认实际绘制后记录 LastTerrainRenderedFrame；不每帧扫描所有地图或全部地形。特殊地形若完全替代原版地形层，需要自己的 Draw / SectionLayer 提供实际地形帧标记，不能只重写 Draw 而丢失就绪通知。

## 游戏内测试能力

Domain 模块内提供 `CQF_TestDomainExpansion`，中文名称使用用户给定的“展开基础领域”。开发者模式打开调试动作，进入 CQF 分类的 `CQF_TestDomainExpansion`，选择人物即可授予；未征召也会显示能力。

第一次施放先在长事件中生成 60×60 的测试地图，然后播放原地图展开、幕墙和转场；只转移施术者，不带其他人物。地图为 Soil / PavedTile 网格，使用独立的 CQF_TestDomainBiome（无野生动植物、禁用天空光照与阴影），测试领域引用 `CQF_DomainVisuals`，`preload=false`，不会在游戏启动时自动生成。进入后再点同一能力请求返回，可观察返回转场和地图回收。测试期间保持正常速度且不要暂停，以便展开 tick 推进。

定义位于 `Content/<版本>/Defs/CQF_TestDomain.xml`；授予入口为 `DebugActions_TestDomain`，施放组件为 `AbilityComp_TestDomainExpansion`，地形步骤为 `GenStep_TestDomain`。测试入口仍属于可选 Domain 模块，没有请求时不加载，测试能力不自动授予人物；寂屋邪祭维持自己的原实现。

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

打包脚本回读两个 Shader，检查 UnityFS、Shader 名称和 pass，再通过 GPU 像素读回检查进入/返回遮盖及换皮表面。验证成功后将 cqf_domain_win 放回 Content/1.6/AssetBundles 再构建模块。1.7 的 visuals 检查使用游戏自带编译器编译四个阶段并检查接口、材质布局、配置和打包结构；它不等于完整游戏内视觉验收。测试指定 `CQF_CHECKS_PROVIDER_ROOT` 时应指向待发布包根目录，默认读取源码 Content 与模组目录。
