# 全景背景外围光照

CQF 使用 `MapDrawLayer_CQFPanoramaLighting` 统一补齐 CameraVisible 全景超出原版外围光照后的明暗与冷暖变化。子 Mod 配置现有背景数据即可，不需要额外补光层或新增编辑器选项。

## 作用条件

绘制要求同时满足：

- `DebugViewSettings.drawLightingOverlay` 开启。
- `MapComponent_CustomMapData.background.Enabled` 为真。
- `background.DrawOnCameraVisibleArea` 为真。

`Enabled` 根据 `texPath` 是否为空判断，`UI/Null` 也属于启用。补光不读取背景纹理，不检查纹理可见性、透明度或地图 defName。因此实际全景由子 Mod 的其他绘制层提供时同样生效。普通地图、空背景与 `drawScope=Map` 不新增绘制。

## 原版边界与补绘分区

`Patch_MapBackgroundClippers` 对上述 CameraVisible 背景关闭原版地图外围遮挡。原版 `MapDrawLayer_ExteriorLightingOverlay` 使用 `MatBases.LightOverlay` 和 `MatBases.ShadowMask`，其外围几何只覆盖地图外 200 格。地图本身及原版外围几何共同覆盖：

```text
x = -200 .. map.Size.x + 200
z = -200 .. map.Size.z + 200
```

`MapEdgeClipDrawer` 的固定 500 格遮罩与此补光边界无关。补光层不修改遮罩、地图边缘行为或原版光照网格。

每帧从实际相机位置、`orthographicSize` 与 `aspect` 计算可见矩形。先检查该矩形是否完全位于原版覆盖范围内，满足时立即返回。超出时最多分成四块：左、右使用可见矩形的完整纵向范围；下、上只使用可见矩形与原版覆盖范围相交的横向范围。这样角落归属于左或右区域，上下区域不会再次覆盖角落；相机完全位于原版范围外时也不会产生反向矩形。

每块正面积区域分别提交 LightOverlay 和 ShadowMask。所有补绘使用 `AltitudeLayer.LightingOverlay` 高度和 `GravshipExclude` 渲染层，与原版一致。每帧新增绘制提交为 0、2、4、6 或 8 次，取决于非空分区数量；这只是提交次数上限，不能单凭几何数量推断性能开销。

## 缓存与生命周期

每张地图只缓存两个带零顶点颜色的单位四边形，材质直接复用 `MatBases`。相机移动与缩放只改变绘制矩阵，不重建网格，也不创建临时数组、集合、材质或迭代器。再次调用 `Regenerate` 保留既有网格。`RefreshSubMeshBounds` 保持单位网格的本地包围盒，避免原版按地图尺寸设置的包围盒在缩放矩阵下造成错误裁剪。

补光开关每帧读取现有背景配置和原版光照开关；首次启用时可延迟建立网格。切换背景模式不依赖地图网格脏标记。网格由原版 `MapDrawLayer.Dispose` 清理，读档后由新绘制层重建。没有新增存档字段、XML 字段、翻译键或旧档迁移。

## 子 Mod 接入

Poolcore 已移除 `MapDrawLayer_PanoramaLighting`，其场景测试改用 CQF 的 `MapDrawLayer_CQFPanoramaLighting`。其他子 Mod 若存在等价临时补光层，也应移除或停用，避免重复叠加两种原版材质。不要让子 Mod 通过恢复地图边缘遮挡处理此问题。

## 验证入口

专用夹具位于 `.QuestEditor_Library/Tests/PanoramaLightingRuntime`，不参与生产主库编译。`RunChecks.ps1` 使用 RimDevTools 管理 `D:/RimWorld-Dev/RimWorld.exe`、`D:/RimWorld-Dev/SaveData`、离线 AppId 与隔离桌面。测试使用独立的 CQF、Poolcore 包镜像，退出后恢复开发目录链接、Prefs 与性能分析器配置。

夹具覆盖 Star、Poolcore 池核与砼核、普通地图，检查四个时段、200 格四边四角、超远缩放、模式及光照开关、复制与 XML、原生存读档。测试专门锁定实际相机变换并记录位置、大小、宽高比、裁剪距离和屏幕尺寸，避免原版相机限位掩盖边界问题。

几何验证在运行时观察真实 `Graphics.DrawMesh` 调用，检查原版区域零重叠、补绘区域互不重叠、面积覆盖、两种材质、高度和渲染层。同镜头对照只切换新补光层的提交，冻结时间，保留原生截图。性能采样移除绘制观察器，通过原版 Frame profiler 测量实际地图绘制与补光入口，并比较移除该层的基线；记录耗时、分配和实际帧间隔。

Poolcore 的屏幕雾效会读取周围像素，因此外围颜色修正会通过雾效影响紧邻边界的少量原版区域像素。夹具同时保留正常雾效与临时关闭雾效的对照：无雾画面用于核对补光层本身没有改变原版区域；正常画面统计时单独避开雾效的相邻采样范围。不能将这种后处理传播误认为补光网格重叠，也不能将带提示文字变化的界面区域纳入地图像素一致性比较。

2026 年 11 月 12 日，1.7.4982 独立离线副本完成 1443 项检查，原生日志零 ERROR，138 张原生截图、69 组同镜头对照及原版性能采样通过。新增提交为范围内 0 次、一边 2 次、一角 4 次、完整外围 8 次；实测补光入口平均约 0.87–3.51 µs/帧、托管分配 0，不能据此宣称 GPU 或总帧时间零开销。测试条件、像素比较范围、异常帧及证据见 [外围补光实机验证](../../../Tests/PanoramaLightingRuntime/README.md)。
