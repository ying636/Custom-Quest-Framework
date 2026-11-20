# 场景光源（1.7）

`MapComponent_CQFSceneLighting` 为自定义场景提供点光、聚光、条形光、表面受光、简化软阴影，以及共享主光束参数的动态丁达尔散射。光照使用世界位置、表面朝向与材质法线计算；不把光照画进底色贴图，也不进行光线追踪。

## 接入

地图步骤持有 `List<CQFSceneLight>`，生成时调用：

```csharp
map.GetComponent<MapComponent_CQFSceneLighting>().Configure(lights);
```

`Configure` 复制配置，运行时修改 `Lights` 中对象的字段不会改动原始 XML 定义。每张地图最多配置 16 个光源；`Bind` 按接收范围选择至多 4 个有效光源。位置、方向、颜色、强度、开关等由普通字段控制，组件检查变化，不需每帧重复 Configure。

配置字段：

| 字段 | 含义 |
| --- | --- |
| `kind` | `Point`、`Spot`、`Strip` |
| `position` | 三维场景位置，Y 为离地高度 |
| `end` | 条形光另一端 |
| `direction` | 聚光朝向 |
| `color`、`intensity` | 颜色和强度 |
| `range` | 三维受光距离 |
| `innerAngle`、`outerAngle` | 聚光内外完整夹角，单位为度 |
| `softness` | 阴影过滤范围 |
| `castsShadows` | 接收此光源的遮挡 |
| `enabled` | 光源开关 |
| `glowRadius` | 原版地图照明半径；0 表示不登记原版光照，原版上限为 40 格 |

Poolcore 的 `CustomMapStep_CoreScene.lights`（原砼核）和 `CustomMapStep_ConcreteVista.lights`（天空之上）已提供 XML 配置和导出范例。其他地图可在自己的步骤中使用相同的数据类型，无需添加巨构地图名称判断。运行时光源使用 Deep 方式存档，阴影和材质在读档后重建，不保存 GPU 数据，不迁移旧存档。

## 动态主光束和散射

地图步骤可同时持有 `CQFSceneBeam beam`，调用 `Configure(lights, beam)`。不传主光束的旧入口仍然可用。配置被复制，运行时通过 `component.Beam` 调整；主光源索引从零开始，所选光源不再进入普通局部光源求和，避免同一主光计算两次。

| 字段 | 含义 |
| --- | --- |
| `lightIndex` | `lights` 中作为主光的索引，Poolcore 使用条形光 |
| `offset`、`slope`、`heightSlope` | 基础光束平面：`x - heightSlope*y - slope*z - offset = 0` |
| `innerWidth`、`outerWidth` | 平面两侧的光带内外宽度，外宽必须大于内宽 |
| `intensity`、`color` | 主光艺术强度与颜色，同时作用于表面、散射和原版补光 |
| `drift`、`sway`、`period` | 平移幅度、斜率摆动幅度、基础周期（游戏秒） |
| `skyResponse` | 原版天空亮度的影响比例，0 为不跟随，1 为完全跟随 |
| `sunPeakHour`、`sunHalfWidthHours` | 地图当地时间的直射峰值小时和窗口半宽。半宽为 0 时全天可见。窗口内平滑淡入淡出，窗口外关闭同源表面直射、散射和地面补光 |
| `density`、`dust` | 散射密度与稀疏浮尘强度，密度为 0 时跳过散射绘制 |
| `scatterHeight` | 简化散射平面高度，默认 6 格 |
| `enabled` | 主光束及对应原版补光开关 |
| `scatteringShader`、`compositeShader` | 调用方提供的离屏散射和世界合成 ShaderDef 引用 |

主光束的平面、来源位置、亮度、遮挡和时间由组件统一绑定。表面调用 `cqfSceneBeamSurface(position, geometryNormal, distant)`，再按自身原有艺术公式使用该强度；颜色来自 `_CQFBeamColor.rgb`。Poolcore 原砼核保留原斜光带和暖白亮面的明暗基调，天空之上使用较弱的同源主光。外围远景可用 `distant: true` 降低贡献；它不计算精细场景遮挡。

`PhaseTicks` 在 `MapComponentTick` 中前进并存档。暂停时光束与浮尘冻结；读档后根据实际经过的游戏 tick 继续，不使用墙上时钟。其他与主光关联的材质动画也应使用 `PhaseTicks / 60f`。主光仍会对天空亮度变化响应。

`MapDrawLayer_CQFSceneScattering` 按主光束配置自动启用，以单张地图四边形合成；不再另加原有静态光束网格。它通过当前地图解析组件，避免持有读档前的组件引用。每张地图的 `CQFSceneScattering` 懒创建 ARGB32 离屏纹理，宽 256、高按地图比例向上对齐至 8，144×96 地图为 256×176，约 176 KiB。双线性过滤与微量抖动用于减轻低透明度色阶。暂停且主光参数、天空亮度与遮挡未变时复用纹理；密度设为 0 后保留可复用资源，地图移除时释放。

散射片段使用同一光束遮罩和阴影缓存、低频密度变化及简化相函数，合成阶段增加稀疏浮尘。散射 alpha 上限为 0.12，含浮尘的合成 alpha 上限为 0.15。这是固定高度的氛围近似，不是多层体积积分，不能用于宣称真实三维体积光。

框架不引用 Poolcore 的 ShaderDef。子 Mod 需提供两个 ShaderDef：离屏 pass 使用适配 `Graphics.Blit` 单位 XZ 四边形的屏幕顶点着色器，配 `CQF_SceneScattering.frag.glsl`；合成 pass 使用地图世界顶点着色器，配 `CQF_SceneScatteringComposite.frag.glsl`，alpha 混合、关闭深度写入及测试，队列 3171。参考 Poolcore 的 `PC_SceneScattering`、`PC_SceneScatteringComposite` 和 `PC_Scattering.vert.glsl`。顶点文件必须保留引擎的顶点布局声明。导出 `CQFSceneBeam` 时使用其 `ISaveable.SaveToXElement`，以 Def 名保存两个引用；不能把 ShaderDef 展开为内部字段。

## 自定义表面

材质来自 `component.MaterialFor(template)`，避免地图之间共享可变参数。每次提交绘制前调用 `component.Bind(material, receiverBounds)`；背景只需环境受光时传入 `receiveLocal: false`。

片段着色器在自身的 `MaterialUniforms` 块内包含：

```glsl
#include "CQF_SceneLightingUniforms.glsl"
```

在块外包含：

```glsl
#include "CQF_SceneLighting.glsl"
```

调用 `cqfSceneIllumination(position, normal, geometryNormal)` 获得 RGB 入射光，乘以底色和材质遮蔽。`position` 必须是产生光源和遮挡时使用的同一三维场景坐标；`normal` 是带细节的单位法线；`geometryNormal` 是未扰动的表面法线，用于阴影接收平面修正。不能直接把为了 RimWorld 绘制顺序压缩的高度当作真实场景高度。

共享阴影采样器使用材质绑定 4，接入着色器须保留该位置。当前 Waylith 材质元数据不支持 uniform 数组，因此四组参数使用独立 vec4 字段；不能改成 SetVectorArray。整个材质 uniform 块不得超过 512 字节；共享参数按当前布局占 464 字节，调用方最多再使用 48 字节。Poolcore 的建筑材质正好达到 512 字节，不能继续追加独立参数。

## 遮挡和生命周期

通过 `SetOccluders(IReadOnlyList<Bounds>)` 提交场景的简化三维包围盒。重复提交相同内容不会使缓存失效。组件另接入原版建筑生成、移除和门开关事件，收集挡光建筑；未提供高度的原版建筑近似为 3.2 格高。

阴影缓存以 CPU 三角形光栅化生成六面深度图，每个面 64×64，使用 RFloat 存储可线性过滤的逆深度。四个光源时 GPU 深度数据约 384 KiB，CPU 光栅浮点缓冲与纹理 CPU 数据各约 384 KiB。上传使用公开的 `Texture2D.SetPixelData(float[], 0)`，保留原版坐标顺序，由引擎处理行翻转；不用逐像素分配临时数组的 `SetPixels(Color[])` 浮点路径。已核对 1.7.4982 和 1.7.5005 的此公开重载均转入内部 Span 上传，不复制为临时数组。禁止直接引用 `TexturePixelData`：1.7.5005 将其从 `Waylith.Rendering` 移到了 `Waylith.Graphics.Textures`，同为 1.7 也会产生类型加载异常。片段阶段使用五次过滤采样与接收平面深度修正。光源位置、范围、种类、投影开关或遮挡变化才重建；颜色、亮度、开关、聚光朝向及相机移动不重建深度图。条形光受光位置采用线段最近点，阴影使用中点近似。

动态主光移动引起的阴影重建上限为每秒 10 次，原版补光登记变化上限为每秒 5 次；原版补光按地图格及 Color32 量化，数值未改变则继续复用。配置直接修改、建筑和门的遮挡变化会立即刷新。主光束遮挡绑定缓存生成时的位置，保证两次重建之间的采样坐标一致。离屏散射只在实际绘制、相位或相关参数变化时更新，不会因同帧多个材质绑定重复执行。

`glowRadius` 将同源颜色、强度和开关登记到原版 RegisterCustomGlower，让人物、普通建筑及游戏光照查询得到对应照明。条形光沿线登记少量采样点。原版照明仍采用自身的二维传播，因此聚光锥和高处遮挡与自定义表面的三维结果存在近似差异。

主光条形灯按 `heightSlope` 把高处位置投到地面，与艺术光带对应。原版光照传播、天空照明和灯具的生命周期保持原有处理；仅注销本组件取得的自定义光源句柄，不改全局 `MatBases.LightOverlay` 或替换 GlowGrid。原版灯具和特殊光源可以同时存在。第三方全局光照或天气 Mod 的兼容性需要另测。

不新增 Harmony 补丁。普通地图不配置光源时不分配光源纹理或材质。地图移除时注销原版光源、地图事件并释放深度图和自有材质。地图内长期保留但暂不可见的光源缓存随该地图保留。

## 验证

Poolcore 的 `.Poolcore/Tests/RunChecks.ps1 -CoreScenes -SceneFinal -SceneLights -Launch -WaitForGame` 通过 RimDevTools 管理独立离线副本，检查六面深度、真实场景遮挡、光源移动和开关、地图内原版光照登记、相机稳定性、建筑遮挡变化、共享相位、暂停缓存、原版真实 tick、更新限频、原版火把共存、存读档与原版 Frame profiler 采样。测试代码不编入生产 DLL。

散射验证通过 `UnityStubs.Rendering.CameraTextureRenderer.Current.TryReadPixels` 直接回读 GPU RGBA8 纹理，检查有效像素、光带外透明和 alpha 上限；不能用本机 `Texture2D.ReadPixels` shim 的全零结果判断着色器输出。性能记录包含关闭、开启且暂停、开启且推进相位三种场景，各 120 个完整原版 profiler 帧。帧间隔受系统调度影响，不能据此宣称帧率提升；原版 CPU profiler 不给出完整 GPU 耗时。

2026-11-15 初始场景光源验证：仅保留原砼核与天空之上，删除巨构上。D:/RimWorld-Dev 独立离线副本完成 65 项检查、18 张原生 2560×1600 截图，控制台零 ERROR；原版地面照度随光源开关实际变化，午夜、地图切换和原生存读档通过。当时的测试包与生产 DLL 哈希一致。证据及性能原始采样位于 `D:/RimWorld-Dev/SaveData/TestRuns/Poolcore_f57c1cc941da483eae4ba903f3d5341c/SceneLightingValidation.json`。帧间隔受预热与系统调度影响，开关对照不用于宣称帧率提升或零 GPU 开销。首次材质 uniform 数组不受支持的失败日志保留在验证记录列出的首轮运行目录，最终实现使用独立参数并已复测。

2026-11-15 原砼核主光表现按用户要求恢复的历史阶段：`PC_BrutalistArchitecture` 保留原斜光带、暖白亮面与明暗基调，重新绘制 `PC_BrutalistLight` 的雾中光束；弱局部补光提供缓存遮挡与原版人物照明。该阶段 66 项检查、18 张原生截图通过，控制台零 ERROR，证据：`D:/RimWorld-Dev/SaveData/TestRuns/Poolcore_dec4249743164443b54f254722943f45/OriginalLightingValidation.json`。后续动态实现已用共享散射层替换旧光束网格，主光视觉基调继续保留。

2026-11-15 动态主光与丁达尔散射最终验证：离线 RimWorld 1.7.4982 rev1280 完成 135 项检查、26 张原生 2560×1600 截图，控制台零 ERROR。两张地图的 GPU 回读均确认光带内有有效散射、光带外透明；暂停缓存、原版实际 tick、10/5 Hz 更新预算、原版火把独立发光及共存、原生存读档均通过。读档后原版实际推进了 1 tick，两张地图的光照相位也准确增加 1。证据：`D:/RimWorld-Dev/SaveData/TestRuns/Poolcore_e7689165568444229acac905d2cc548d/DynamicLightingValidation.json`，目录保留两份受测 DLL、来源哈希、原生日志、像素数据、截图和原版 profiler 样本，失败轮次也保留。

本轮动态采样中，受测光照方法自身 CPU 时间合计约为原砼核 604 微秒/帧、天空之上 501 微秒/帧；临时分配分别约 4363、4222 字节/帧。暂停时约 5.5～6.9 微秒/帧、96 字节/帧。这不包含完整 GPU 成本，也不代表整帧耗时。浮点缓冲上传优化前的通过轮次为 `Poolcore_38d4ab3d0061461181987ecb88d84b1f`，其动态临时分配约 437/201 KB 每帧；优化后已完整复测。本次光照源码、资源和 CQF 模块与受测版本一致，Poolcore 后续由并行高崖地图工作重编；完整 DLL 的当前哈希差异及具体来源文件记录在验证 JSON 中，不能将受测快照与后续整个生产 DLL 混为同一版本。

2026-11-15 纹理上传类型修复：使用当前正式版 1.7.5005 的程序集，在独立 CLR JIT 校验中复现旧 DLL 的 `Waylith.Rendering.TexturePixelData` 加载异常；更换为公开浮点数组上传后，1.7.4982 与 1.7.5005 的相关方法均能完成 JIT。正式版未启动实机测试。RimDevTools 管理的 1.7.4982 离线副本完成 136 项场景光照回归，包含新增的上下不对称遮挡上传方向检查，完整日志零 ERROR；人物补光、GPU 散射回读及存读档恢复通过。编译零错误，保留 1983 条既有警告；首次误用私有 Span 重载的编译失败日志也保留。已部署主库与受测候选哈希一致。证据：`D:/RimWorld-Dev/SaveData/TestRuns/CQF_TextureUpload_20261115/Result.json`；实机目录：`Poolcore_c1c5bca210154e4daa2ea88abc7d41f7`。没有修改或替换游戏本体程序集。
