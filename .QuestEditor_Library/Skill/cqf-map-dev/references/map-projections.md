# 地图投影（RimWorld 1.7）

地图投影由独立的 `CQF.MapProjections.dll` 按需提供，入口为 `MapComponent_CQFMapProjections`，命名空间仍为 `QuestEditor_Library`。每个投影引用另一张真实地图，把整图或矩形区域绘制到宿主地图的指定矩形。人物、建筑和命令仍属于来源地图。

## 模块声明与打包

子 Mod 在自己的 `LoadFolders.xml` 中声明需求；仅使用 XML 动作也需要声明：

```xml
<loadFolders>
  <v1.7>
    <li>/</li>
    <li RequiresCQFModule="MapProjections">1.7/MapProjections</li>
  </v1.7>
</loadFolders>
```

需求目录必须实际存在。投影相关 Def、翻译和依赖投影的子 Mod DLL 放在该目录内；DLL 必须位于目录根部，例如 `1.7/MapProjections/MyMod.MapProjections.dll`，不能位于 `Assemblies` 或其他原版自动加载目录。依赖目录内的 DLL 不包含 `Mod` 派生入口；普通 Mod 入口保留在独立的普通程序集，并避免提前引用投影类型。在 About 中声明 CQF 依赖及加载顺序。

同时使用领域时，同一需求目录可写 `RequiresCQFModule="Domain,MapProjections"`；也可分别声明两个不同目录。名称区分大小写，列表以英文逗号分隔，同一列表不重复模块名，同一已选目录不重复声明。支持与原版 `IfModActive` 等条件组合，被原版条件或版本筛选掉的条目不触发请求。所有请求的框架模块先初始化，再按原 Mod 顺序及 LoadFolders 顺序加载子 Mod DLL，之后解析 Def。

CQF 自己的提供目录为 `<li IfCQFModule="MapProjections">1.7/Optional/MapProjections</li>`。没有任何已启用子 Mod 请求时，该目录及其资源不参加加载，投影程序集、GameComponent、MapComponent、开发者入口和 Harmony 补丁均不存在。仅请求投影不会加载 Domain；仅请求 Domain 不会加载投影。请求在启动时确定，修改声明或启用列表后须重启游戏，不支持运行中加载、卸载模块。未知名称、缺失目录或 DLL、错误 DLL 布局和类型加载失败会明确报错。

C# 子 Mod 编译时同时引用主库和 `1.7/Optional/MapProjections/CQF.MapProjections.dll`，不要把框架 DLL 复制到子 Mod。模块源码与工程为 `.QuestEditor_Library/CQF.MapProjections/` 和 `CQF.MapProjections.csproj`，原投影实现位于其 `MapProjections/` 子目录。中英文 Keyed 资源由 `Content/1.7/Languages/` 复制到可选目录，主库根语言目录不再携带投影文本。工程默认输出到 `1.7/Optional/MapProjections/`，仅支持 1.7；临时构建可指定 `CoreAssembly` 与 `OutputPath`，避免覆盖运行文件。

## 使用入口

- 模块启用后，配置入口统一为开发者命令菜单 `CQF → 地图投影`（英文为 `Map projections`）；不要求启用 CQF.Editor。普通地图界面不提供管理或调整窗口按钮，游玩时保留重置视图与返回宿主地图。
- 添加投影后，在来源选择窗口按地图名称搜索并点击缩略图。支持活动地图和休眠地图，未命名地图显示本地化的“地图 {编号}”。预览按需唤醒来源，窗口关闭或草稿结束后恢复原休眠状态；已经被启用投影使用的来源继续保持活动。
- 左侧面板可以选择整图或在缩略图上左键拖出来源选区。在宿主地图上左键拖出显示范围，放置后拖动内部移动、拖动四角缩放。“保持来源比例”约束放置与缩放，取消勾选可拉伸。右键或 Esc 先取消当前拖动，再次 Esc 退出编辑。
- 新建投影默认选择“游玩模式”。“调整窗口”只编辑来源、位置、尺寸和配置；“完成并游玩”提交后立即把操作焦点交给新视口，并释放名称输入框的键盘焦点。游玩时显示细边框，窗口移动和四角缩放仅在调整状态可用。
- 名称、来源、选区、显示模式、图层和启用状态都使用独立草稿；“完成”验证后一次性写入，“取消”保留原配置。编辑期间暂停游戏并关闭跨图输入；高级设置提供分辨率、刷新间隔及精确坐标。管理列表提供重命名、定位、启停和删除。
- `CQFMapProjectionMode.Display` 为纯显示。
- `CQFMapProjectionMode.Control` 为可控制。点击投影内的人物后，可使用原版检查面板、征召按钮、右键命令与目标选择器。右上角显示当前控制状态并提供“返回当前地图”；点击投影外的地图也可返回宿主操作，窗口内的按钮保留来源地图上下文。
- 游玩视口接入原版选择器和 Designator：可以点选人物、动物、建筑与物品，查看检查面板并使用原版 Gizmo、建造和划区。允许缩放时，滚轮围绕指针缩放小地图；中键拖动平移，指针位于视口内时也可使用地图移动和允许的缩放快捷键。此时宿主相机的滚轮、拖动、惯性和边缘滚屏被隔离。
- “允许缩放”关闭后，滚轮、缩放快捷键与 `ZoomView` 不改变当前比例，平移仍受来源范围约束。“格子 1:1”优先级更高：来源 20×10 格固定占宿主 20×10 格，锁定视角缩放与窗口拉伸；改变来源选区会同步改变占地。普通缩放锁定时，重置按钮仅重新居中。
- 独立视角以来源选区为边界，最小短边为 4 格或整个更小选区；整图来源可在全图内浏览。视角与窗口尺寸分开保存，移动或缩放来源视角不会移动窗口。原版跳转与选中来源物体只改变视口和原版选择，不切换实际宿主地图。
- 用户已授权生成自然文案，配置窗口、动作名称、参数提示和错误提示均提供自然中英文；“纯显示”“可以控制”保留用户原文。中英文文本及审核意见维护在 `.QuestEditor_Library/TextReview/MapProjections.csv` 与 `.xlsx`，新增生成文案仍标记为待审核。

## C# 接口

```csharp
MapComponent_CQFMapProjections component = host.GetComponent<MapComponent_CQFMapProjections>();
component.Add(new CQFMapProjection
{
    id = "MyMod_Observation",
    sourceMap = source,
    sourceRect = new CellRect(20, 12, 20, 10),
    destination = new CellRect(46, 22, 20, 10),
    mode = CQFMapProjectionMode.Control,
    background = false,
    resolution = 1024,
    refreshFrames = 1,
    allowZoom = false,
    matchCellSize = true
});
component.Remove("MyMod_Observation");
```

`sourceRect = CellRect.Empty` 表示整图。C# 的 `new CellRect(x, z, width, height)` 和配置窗口的四个输入框均使用起点与尺寸；XML 的 `CellRect` 字符串采用原版的最小、最大坐标，包含末格。来源和宿主必须是不同地图，矩形必须完整位于对应地图内。

`matchCellSize=true` 时，`destination` 的起点确定放置位置，实际宽高取自 `SourceBounds`。查询实际占地使用 `DestinationBounds`，绘制、点击、定位和边界验证都使用这个属性。无法完整放入宿主时明确拒绝配置，保留原投影，不自动缩小来源。该规则仅改变显示与输入比例，人物、寻路和弹道仍归来源地图；跨图移动需由子 Mod 接入地图出入口流程。

| 字段 | 含义 |
| --- | --- |
| `id` | 宿主内唯一的投影标识，内容作者使用自身 Mod 前缀 |
| `name` | 玩家可修改的显示名称；为空时根据来源地图名称生成本地化标题 |
| `sourceMap` | 真实来源地图引用 |
| `sourceRect` | 来源矩形；空矩形为整图 |
| `destination` | 宿主上的绘制矩形，可以缩放和改变长宽比 |
| `mode` | `Display` 或 `Control` |
| `background` | `true` 时位于宿主地形下；可通过 `QE_EtherealVoid` 等透明地形露出。现有 `QE_Null` 贴图 alpha 为 175–255，会遮挡画面，不能当作纯透明地形 |
| `enabled` | 是否启用；关闭时释放绘制资源和控制焦点 |
| `resolution` | 离屏图像长边像素数，范围 128–4096，默认 1024 |
| `refreshFrames` | 刷新间隔，范围 1–60 帧；正在控制的投影逐帧刷新 |
| `allowZoom` | 默认 `true`；关闭后禁止交互和 `ZoomView` 改变比例 |
| `matchCellSize` | 默认 `false`；启用后每个来源格占用一个宿主格，固定占地尺寸并锁定视角缩放 |

## CQF 动作

`CQFAction_SetMapProjection` 从现有目标系统解析宿主和来源。每个 Key 必须得到唯一地图。相同 `id` 更新现有投影；无效配置在替换前报错并保留原投影。`enabled=false` 可以关闭相同标识的投影。

```xml
<li Class="QuestEditor_Library.CQFAction_SetMapProjection">
  <id>MyMod_Observation</id>
  <hostKey>Position</hostKey>
  <sourceKey>MyMod_SourceMap</sourceKey>
  <sourceRect>(20,12,39,21)</sourceRect>
  <destination>(46,22,65,31)</destination>
  <mode>Control</mode>
  <background>false</background>
  <enabled>true</enabled>
  <resolution>1024</resolution>
  <refreshFrames>1</refreshFrames>
  <allowZoom>false</allowZoom>
  <matchCellSize>true</matchCellSize>
</li>
```

整图显示时省略 `sourceRect`。删除使用：

```xml
<li Class="QuestEditor_Library.CQFAction_RemoveMapProjection">
  <id>MyMod_Observation</id>
  <hostKey>Position</hostKey>
</li>
```

## 生命周期与输入

- 添加启用投影会通过现有地图管理器唤醒来源。活动宿主中的启用投影阻止来源休眠。关闭或删除最后一个投影后，来源可再次按原接口休眠；删除投影不会擅自让来源休眠。
- 主库通过 `ICQFMapLifetimeModule.KeepsMapActive(Game, Map)` 与 `BeforeMapsDisposed(Game)` 转发保活和退出释放，不引用投影类型；投影模块初始化时注册实现。Domain 仍通过原有 `ICQFMapModule` 接入同一个地图管理器，组合启用不替换领域运行对象。各释放步骤失败后仍继续其余资源，并汇总异常。
- 投影配置随宿主地图保存，来源使用原版地图交叉引用。读档重新创建相机、纹理和材质；控制焦点与选择状态是临时状态。
- `Copy()` 只复制配置，不共享相机或纹理；`Set()` 在验证成功后原位替换同标识投影，保持列表和绘制顺序。
- `ViewRect` 为当前浮点来源视区，`VisibleCells` 为对应剔除范围，`ViewZoom` 为相对来源选区的缩放比例。`ZoomView(factor, sourceAnchor)`、`PanView(delta)`、`CenterView(cell)` 和 `ResetView()` 操作视角，`ScreenRect(hostCamera)` 返回宿主屏幕上的交互范围。修改来源选区时重置视角。
- `CanZoom` 为考虑两个配置后的实际缩放权限；`DestinationBounds` 为实际宿主占地。`Copy()`、投影存档和 `CQFAction_SetMapProjection` 的 XML/存档均保存两项配置。`Set()` 和动作更新在验证成功后替换同标识对象，失败保留原对象。
- 编辑草稿与来源缩略图不保存。保存游戏前、切换宿主地图或退出游戏时结束草稿并释放预览；保存只包含已经完成的投影。关闭编辑器恢复进入前的游戏速度。
- 独立捕获绘制提交，按渲染队列与原提交顺序排序，避免来源网格进入宿主场景。来源的地形、建筑、人物、光照、天气、特效和选择覆盖层共同进入离屏图像。
- 输入通过有限作用域提供来源地图、视区和虚拟相机。鼠标位置、名称、框选及目标位置使用相同坐标变换。框选越界被裁到来源区域；区域外的普通指针不作为来源命令目标。
- 复用原版全局选择器、检查面板和菜单，不创建第二份 Game 或 UI 根。1.7 的窗口内容可以延后到 UIRootOnGUI 返回后执行，因此 Window.InnerWindowOnGUI 单独建立并释放来源上下文；否则 FloatMenuMap 在点击时会按宿主地图重新验证目标，导致装备和使用物品失效。
- 来源视区使用前置补丁直接返回，不写入原版宿主的同帧视区缓存。选择框和 Designator 拖拽使用当前可见来源范围，绘制与点击共用同一变换。
- Camera+ 3.4.9.0 的 FastUI 按帧缓存每格大小与鼠标位置，MarkerDecisionCache 按帧缓存人物显示决策。每次进入、离开或改变投影作用域时使这些缓存失效，避免小地图缩放污染宿主的生物图标和选择判断。没有启用 Camera+ 时不产生硬依赖；检测到其接口不匹配时记录明确错误。
- Camera+ 的 EdgeUIInsets.ObserveMarker 在离屏捕获期间跳过登记，避免来源地图的边缘人物标记参与整屏 UI 避让并挪动左下角悬停信息。宿主标记继续正常登记；该补丁仅在 Camera+ 类型存在时加载。
- 宿主移除、来源失效、关闭投影或退出游戏时释放资源。绘制和配置异常记录完整错误并关闭对应投影，不静默保留坏状态。
- 捕获来源时跳过其投影组件，当前不递归展开地图内的投影。地图间的寻路和弹道仍受原版地图边界约束。
- 当前实现与补丁仅编入 1.7；1.6 不注册本功能。默认不增加旧存档迁移。

## 验证入口

本功能当前有 17 个补丁类和 17 个目标方法或属性；模块被请求时使用 Harmony owner `CQF.MapProjections` 安装，不启用 Camera+ 时注册其中 16 个，模块未被请求时为零。UIRootOnGUI 和 InnerWindowOnGUI 各使用 Prefix 与 Finalizer，其他目标各使用一个 Prefix 或 Postfix。已删除 UI.UIToMapPosition 的重复转换补丁，原版通过虚拟相机完成转换；两个鼠标入口分别保留格子无效状态、浮点边界与拖拽裁切。原版 Vector3 转 IntVec3 会将 y 设为 0，无法仅靠公共浮点入口保留 IntVec3.Invalid 的 IsValid=false，因此格子入口不能直接合并。

| 范围 | 原版或 Mod 目标 | 数量 |
| --- | --- | --- |
| 来源地图 | Game.CurrentMap.get | 1 |
| 相机与视区 | Find.Camera.get、CameraDriver.CurrentRealPosition.get、CurrentViewRect.get、CameraDriverOnGUI、Update | 5 |
| 坐标与框选 | UI.MapToUIPosition、MouseCell、MouseMapPosition、DragBox.ScreenRect.get | 4 |
| GUI、延后窗口与绘制 | UIRoot_Play.UIRootOnGUI、Window.InnerWindowOnGUI、MapInterface.MapInterfaceUpdate | 3 |
| 跳转与选择 | CameraJumper.TryJumpInternal(IntVec3, Map, MovementMode)、TrySelectInternal(Thing) | 2 |
| 保存 | GameDataSaveLoader.SaveGame | 1 |
| Camera+ 可选兼容 | EdgeUIInsets.ObserveMarker | 1 |

`Tests/MapProjectionsRuntime/RunChecks.ps1` 编译独立候选，通过 RimDevTools 管理 `D:/RimWorld-Dev/RimWorld.exe`、独立存档 `D:/RimWorld-Dev/SaveData` 和隔离桌面。回执必须同时包含真实进程与离线参数、完整日志、原生截图和 `COMPLETE`。结果仅覆盖列出的原版与 CQF 路径，不能据此宣称已验证所有第三方 Mod 的绘制或命令。

加载验证使用 `-Modules None|Domain|MapProjections|Both`，每种模式采用不同 `-RunRoot`，避免残留夹具程序集交叉污染；可加 `-Language English|ChineseSimplified` 与 `-CameraPlus`。None / Domain 使用仅引用主库的 `Tests/ProjectionModulesRuntime` 夹具，实际检查未加载的程序集、组件、Patch、翻译和开发者入口；MapProjections / Both 使用完整游玩、菜单执行、缩放约束、休眠和存读档回归。`Tests/EditorModule` 的 `projections-*` 模式补充条件过滤、模块组合、错误声明及过早自动加载 DLL 布局检查。

## 子 Mod 适配

- 用宿主的 `MapComponent_CQFMapProjections.Add/Set/Remove` 或 CQF 动作管理投影，标识使用子 Mod 自身前缀。来源可使用地图系统已有的自定义地图与休眠地图。
- `ToSource`、`ToHost`、`ScreenRect` 与 `DestinationBounds` 提供相同坐标规则；使用实际占地属性，避免在 1:1 模式仍读取配置里的旧宽高。
- `CQFMapProjectionContext.Current` 暴露当前投影、宿主相机与来源绘制/输入状态；`IsRendering` 可用于识别离屏捕获。来源地图的 MapComponent 绘制会被捕获，依赖全局屏幕的特殊 UI 或缓存需按此状态适配。
- 原版菜单和检查窗口已覆盖同步与延后窗口回调。子 Mod 的异步、长事件和自行排队的操作应捕获人物、物品及来源地图引用；不要在稍后执行时仅依赖 `Find.CurrentMap`。确需原版地图查询上下文时，在实际执行的主线程回调内部使用 `CQFMapProjectionContext` 作用域，并在退出时释放；不要跨异步等待持有该作用域。
- 主要成本来自额外来源绘制、纹理和保持来源运行。增加同来源投影会增加绘制次数；不同来源还会增加活动地图模拟。增大 `refreshFrames` 可降低纯显示投影的刷新频率，控制焦点下仍逐帧刷新。分辨率主要影响离屏图像和图形资源成本，格子 1:1 本身不表示免渲染成本。

## 本机性能采样（2026-11-15）

以下为拆分模块前的同日采样，不能作为拆分前后性能增益对照。RimDevTools 的独立 1.7.4982 副本，启用 Camera+ 3.4.9.0，80×60 来源图，分别为 5 与 53 个来源生物；离屏分辨率 1024×768，暂停地图模拟。原版 Frame profiler 对每种情形预热 35 帧、导出 120 帧，检查无丢弃、溢出和不平衡标记。实际 Render 由测试夹具中的原版 profiler scope 包围，MapComponentDraw 与 UI 使用原版方法插桩，生产 DLL 无采样代码。

| 情形 | 5 个生物，每帧来源绘制 CPU 约值 | 53 个生物，每帧来源绘制 CPU 约值 |
| --- | --- | --- |
| 单个纯显示，每帧刷新 | 0.73 ms | 0.90 ms |
| 单个纯显示，每 6 帧刷新 | 0.16 ms | 0.18 ms |
| 单个游玩视口，每帧刷新 | 0.70 ms | 0.87 ms |
| 四个同来源纯显示，每帧刷新 | 2.22 ms | 2.94 ms |

上表使用有效 Render scope 的每次均值乘实际刷新率；每帧刷新组的有效 scope 为 119 次、四投影为 476 次，独立计数分别为 120 和 480 次，存在一帧采样边界差异。每 6 帧刷新组两种计数均为 20 次。耗时包含分析器开销，不代表 GPU 耗时、实际 FPS/TPS 或大型地图的性能保证，也没有做精简前后的性能增益对照。53 生物、四投影组的 Render scope 托管分配约 83 KB/采样帧，多个实时投影会增加分配压力。

证据与完整数值：`D:/RimWorld-Dev/TestWorkspaces/CQFProjectionAuthoring/Evidence-8d60c8e912564510ba6fbe03efc7c368/ProfilerSummary.json`；原始分组数据和原版窗口截图位于同目录 `Profiler`。
