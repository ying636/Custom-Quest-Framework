# CQF 运行性能

原版 API 用 Rimsage 核对，CQF 实现从本地源码读取。1.7 实测使用原版 Frame profiler，由 `rimworld-source-dev` Skill 的 RimDevTools 流程管理独立离线副本。不要把未启用模块的成本、活跃地图模拟、离屏绘制和主库轮询的成本混作同一结论。投影历史采样见 [地图投影](../../cqf-map-dev/references/map-projections.md)。

## 地图投影可见区与像素密度（5.4）

世界投影只捕获与宿主屏幕相交的来源范围，来源剔除使用该捕获相机的实际视区；输入仍使用完整来源视区，保持点选和命令坐标。自动分辨率默认开启，包含 UI 缩放的物理像素密度，每边最多 4096，按 64 像素档位分配。关闭 `autoResolution` 使用原有固定分辨率。来源缩略图仍捕获完整选区。子 Mod 直接消费 `Texture` 时须同时查询 `TextureBounds`，不能默认它包含完整 `ViewRect`。

控制模式不再强制逐帧刷新，改为遵守 `refreshFrames`；视角、屏幕裁剪、分辨率或控制焦点变化立即刷新。图像刷新与原版输入事件分开，降低绘制频率不降低命令处理频率。来源地图仍保持活动，不把渲染节流当作休眠或模拟减负。

原版 `CameraTextureBaker.RecordAndSubmit` 同步等待 GPU，当前仍走该公开接口，未引入异步 Vulkan 渲染或新补丁。高清大窗口比原有 1024 像素纹理占用更多显存；小窗口按实际像素分配，手动分辨率仍可用于控制预算。不要只凭分辨率增加推断整帧性能，也不要把固定像素或格子 1:1 当作相同概念。

原生验证入口为 `Tests/MapProjectionsRuntime/RunChecks.ps1 -ProjectionQuality -Modules MapProjections -CameraPlus`，`-ProjectionAssemblyOverride` 可指定旧投影 DLL。专用夹具位于 `Tests/ProjectionQualityRuntime`，使用同一 `CQF_ProjectionQuality.rws` 存档；三轮帧率采样每组至少 600 帧、至少 3 秒，原版 Frame profiler 另采 120 帧。`-ProjectionPaired` 在同一进程交替旧绘制路径和新路径，六轮、每组至少 600 帧及 2 秒，每轮反转顺序。旧路径源码来自本次父提交，仅存在测试程序集，不随生产模块加载；旧 DLL 独立基线仍保留。

原生截图同时保留直接查看来源地图与投影画面，比较保持原比例裁切。所有测试暂停地图模拟并关闭帧率上限，结论只表示渲染吞吐，不表示自然运行 TPS 或任意用户存档的帧率。跨进程样本还须核对关闭投影时的对照帧率；外部负载明显变化时，不能只挑选增益最大的轮次。

2026-11-17 的独立 1.7.4982 rev1280 副本，2560×1600、UI 缩放 1.5、Camera+，同一 250×250 来源图、6561 棵树和 40 名小人，存档 SHA256 为 `E8AEAEAE250227FEE8AFF5C2DD1BCABFC505EED3542723551964DCA1BB0AE4B2`。六轮同进程配对，每轮反转路径顺序，各组实际帧率中位数如下；不是固定调用耗时换算出来的 FPS。

| 场景 | 旧绘制路径 FPS | 优化路径 FPS |
| --- | ---: | ---: |
| 近景纯显示，每帧刷新 | 277.1 | 498.8 |
| 近景游玩，配置每 3 帧刷新 | 281.0 | 596.6 |
| 远景显示（FullDisplay） | 270.1 | 341.0 |

游玩组旧路径仍实际逐帧刷新，新路径每帧平均约 1/3 次来源绘制；该组收益同时包含裁剪和刷新间隔生效。近景来源捕获范围从 250×250 缩为约 115.09×71.94，纹理从旧版 1024×1024 改为 2624×1664，匹配本轮屏幕像素；原比例原生截图确认树木与人物边缘更清楚，离屏二次采样仍不能称为与直接查看来源逐像素相同。

跨进程最后一轮关闭投影组从约 511 降至 352 FPS，表明外部负载发生变化，因此不以该轮或先前最高增益作主结论。保留全部原始轮次。主要证据为 `D:/RimWorld-Dev/TestWorkspaces/CQFProjectionQuality/PairedComparison.json`，配对实机为 `Paired/Evidence-6fb50d39fa604d87a040de262ed54490`，独立旧 DLL 基线与最终接口验证分别见 `BaselineLong`、`Final`。清晰度原图与比较为 `ClarityComparison.png` 和各 Evidence 的 Reference / Projected。源码复制的旧绘制器只编入夹具；生产 DLL 没有测试入口。

中文 Camera+ 与领域组合 120 项交互回归通过，涵盖原版菜单实际装备、食用和使用物品、框选、建造、视角、休眠、释放与完整存读档。最终像素与配对夹具分别通过裁剪范围公开查询、输入范围隔离、手动分辨率及立即刷新检查。配置、目录链接与分析器文件由原有事务流程恢复，保留全部控制台日志和失败的夹具编译、环境占用回执。

### 离屏提交列表复用（2026-11-17）

在上述可见区优化基础上，原版 `CameraTextureBaker.Render` 每次刷新仍分配 `List<FinalDrawItem>` 和引用数组。投影现在复用实例列表，通过同一后端的公开 `RenderItems` 入口提交，结束或异常时归还全部池对象。转换前按原版顺序刷新纹理及待上传网格，保留相同的材质属性区间、实例数据、渲染队列、相机与同步提交时机。零命令或全部命令被拒绝时继续使用原 `Render` 清除画面，避免残留旧图。

不新增主机程序集硬引用或 Harmony 补丁。首次使用或后端实例改变时绑定公开方法；第三方后端缺少相同入口时明确警告并使用原 `ICameraTextureRenderer.Render`，绑定异常记录完整错误后回退。每个投影独立拥有列表，释放时清空列表和后端引用；没有跨地图共享可修改提交数据，也没有声称消除了同步 GPU 等待。

复测使用 `RunChecks.ps1 -ProjectionPool -Modules MapProjections -CameraPlus`，同一进程六轮交替标准 `Render` 与列表复用 `RenderItems`，每组至少 600 帧及 2 秒、交替先后顺序。两路使用相同绘制命令、相机、纹理和刷新率，游玩组两路都每三帧刷新。提交方法入口、出口读取主线程分配字节；该计数仅包含提交阶段，不是完整地图绘制或整帧分配。

| 提交阶段，每次刷新 | 标准接口 | 复用接口 |
| --- | ---: | ---: |
| 近景纯显示 | 2576 B | 40 B |
| 近景游玩 | 2584 B | 40 B |
| 远景 | 6432 B | 40 B |

同轮 FPS 配对比值中位数约为近景纯显示 +2.2%、游玩 +0.7%、远景 +8.1%；游玩组视为基本持平。各组六个独立 FPS 中位数分别约 464→483、543→526、305→327；统计量不同，不能把独立中位数之比当作配对比值。仍存在系统负载和同步等待波动，主要已验证收益是持续分配减少，不能承诺稳定提高自然运行 FPS。

对同一批实际捕获命令，两个提交接口的 RGBA 读回逐字节一致；零命令清屏、无预备入口的替代后端、注入提交异常后池对象归还、切回原后端继续显示均通过。专用夹具的额外补丁与故障后端仅在测试 DLL 内。证据为 `D:/RimWorld-Dev/TestWorkspaces/CQFProjectionPool/Comparison.json` 与 `Native/Evidence-f1793a7594ba4c06a4abfa7bb62c07c7`。该测试暂停模拟、关闭帧率上限，版本仍为独立 1.7.4982 rev1280；生产模块与夹具零警告零错误，原版分析器和完整日志保留。

## 多方领域网格和纹理（5.4）

`DomainMultiTerritory.IsBoundary` 曾为每个格子重复计算四角、每个领域的评分，所有领域的网格重建又重复遍历全图。5.4 在求解完成的 `RefreshOwners` 中缓存每个角点归属，并保存格子中心前两名评分与归属；边界判断读取角点，`Distance` 在精确格子中心复用最佳对手，任意其他坐标仍走原计算。保留评分顺序、平分时的选择和全部梯度计算。构建及 PostLoadInit 会重新生成缓存，保护/占领判断仍读取实时覆盖，不增加存档字段或 Harmony 补丁。

缓存每张 180×180 战场约额外占 0.50 MiB，属于分区实例，不是全局缓存；完成下一次求解后统一刷新。不要缓存未完成求解的分数或把中心结果用于任意浮点坐标。

`DomainTerritoryField` 的持久像素缓冲改为 `Color32[]`，仍使用 RGBA32，上传走 `SetPixels32`。本机 1.7 的 `SetPixels(Color[])` 会分配同尺寸 Waylith 浮点数组再转换，`SetPixels32` 可避免该拷贝；两条路径的量化与行翻转已通过原版源码和实机字节对照核对。缓冲本身由每像素 16 B 降为 4 B；`Apply` 等步骤仍有分配，不能称为零分配上传。

本机原版 Frame profiler 的一次固定工作量顺序对照，2/4/8 方分别使用 90×90、128×128、180×180 分区，游戏暂停，各组预热 30 帧、采样 120 帧。完整重建与强制纹理刷新代表实际需要重建时的工作量，稳定状态不会逐帧强制重建。

| 路径 | 旧版 ms/次 | 优化版 ms/次 | 旧版分配 B/次 | 优化版分配 B/次 |
| --- | ---: | ---: | ---: | ---: |
| 四方完整网格 | 8.65 | 1.87 | 0 | 0 |
| 八方完整网格 | 57.98 | 5.48 | 0 | 0 |
| 四方全部纹理 | 6.54 | 4.62 | 1,053,637 | 4,752 |
| 八方全部纹理 | 31.59 | 18.08 | 4,157,428 | 9,816 |

二方网格和纹理 CPU 没有明显改善，纹理分配从约 261,816 B 降为 2,568 B。主要求解算法没有改动，不把本轮求解耗时波动归为算法收益。这些是独立 CPU 工作量，不是整局 FPS/TPS、GPU 或任意第三方 Mod 的性能保证。

原始对照为 `D:/RimWorld-Dev/TestWorkspaces/CQF54Performance/PerformanceComparison.json`，基线 `Baseline/Evidence-aaefadefd76542379582a9d116fd84a2`，优化版 `Candidate/Evidence-062d5536feb7442b8a580a0aff1ed061`。每种情形的完整归属、边界和顶点/三角索引哈希一致；优化版还逐位比较中心距离，并用原版浮点上传路径逐字节比较 RGBA32 纹理。5521 项连通性检查及多方、曲面、守区、侵蚀、归属、释放回归通过。八方实机证据为 `Gameplay/Evidence-aecc9b7d36904f9d83664bf5833d1557`，覆盖弱方、守区、回血、完整重载、首方崩塌和精确返回；该夹具临时关闭展开/返回动画，不能代替动画验证。发布检出按用户确认的全部已完成本地功能成套重建与测试。

复测入口为 `Tests/MapProjectionsRuntime/RunChecks.ps1 -Modules Domain -DomainPerformance`，同一存档为 `CQF_DomainPerformance.rws`；`-DomainGameplay` 切换为八方生命周期回归，不重复完整性能采样。两种入口均使用 RimDevTools 和现有配置恢复流程。保留并归档各轮 Evidence、分析器配置及本轮存档。夹具首次误用内部 `InstanceAt` 导致编译失败，已改为公开 `Instances` 并重跑。

5.4 最终完整发布包另通过 235 项原生回归，包含地图布局/生态、对话、八方领域、中文 Camera+ 与投影组合，以及完全不请求可选模块的加载检查。实际发布运行文件与成功测试候选一致，证据与各模块边界见 [版本与构建](version-build.md)。

## 主库定时生成与陷阱 Tick

`MapComponent_CustomMapData.MapComponentTick` 和 `CustomTrap.Tick` 的高频捕获委托已改为具体 List 的 `foreach`，减少逐次临时分配。没有新增 Patch、轮询缓存或修改触发间隔。

地图生成计时保留 `time > timeToSpawn`、重置时机、随机数量、来源坐标和每次生成时的 Lord / Quest 查询。空生成列表和缺失列表仍支持。陷阱保留哈希触发、零间隔停用、模式筛选和原动作顺序；每个动作仍新建目标字典并当场解析 Quest，前一动作修改目标或任务状态不污染下一动作。不要为省分配而在动作之间共享可修改的目标字典。回调直接修改正在遍历的配置列表仍明确抛出集合修改异常。

原版 `GenCollection.RemoveAll(Dictionary<TKey,TValue>, Predicate<...>)` 只在确有删除项时租用并归还 `SimplePool<List<TKey>>`；没有证据时不要改成另一套常驻缓存。主库执行请求、事件区域和地图管理已有空闲检查或复用缓冲，先测量再扩大改动。

## 固定工作量复测

夹具位于 `.QuestEditor_Library/Tests/CorePerformanceRuntime`，由现有运行脚本的 `-CorePerformance -Modules None` 分支加载。该分支不请求可选模块，并自动启用分析器配置的备份恢复，等同于同时传入 `-Profile`。

```powershell
& .QuestEditor_Library/Tests/MapProjectionsRuntime/RunChecks.ps1 `
  -RunRoot D:/RimWorld-Dev/TestWorkspaces/CQFCorePerformance/Baseline `
  -Modules None -CorePerformance -Profile -Language English `
  -CoreAssemblyOverride D:/RimWorld-Dev/TestWorkspaces/CQFCorePerformance/BaselineRuntime/QuestEditor_Library.dll
```

保留旧 DLL/PDB 后，用另一个 RunRoot 和新 `CoreAssemblyOverride` 执行优化版本。夹具首次创建 `D:/RimWorld-Dev/SaveData/Saves/CQF_CorePerformance.rws`，后续两轮均完整加载同一文件；运行前检查同名存档是否属于本轮，结束后归档。比较 `Scene.json` 中存档 SHA256、地图尺寸、对象数量及各版本主库 SHA256，不能只比较文件名。脚本会构建新主库候选，提供旧 DLL override 才能测旧版本。

游戏暂停，使用原版地图组件和 100 个实际生成的石质陷阱，每个采样帧直接调用被测入口 1000 次。每种情形预热 30 帧、采集 120 帧，另外确认全部计时器和动作实际执行次数。外层原生 scope 提供 CPU 数据，主线程 `GC.GetAllocatedBytesForCurrentThread` 提供独立分配计数；方法插桩只限定两个被测方法。要求 scope 有效、无丢弃事件或不平衡结束、完整日志无游戏 ERROR，并检查原版分析器截图。未生成来源人物的计时路径使用明确的测试生成回调，不能将其耗时称为人物生成耗时。

## 2026-11-15 本机对照

独立副本 1.7.4982 rev1280；同一 250×250 存档 SHA256 为 `3D8A7A91B5F2F33C74BF94C0AF5962D000F2C93138AC9E847AC5415A2DB63F08`。一组顺序对照，每种情形 120000 次被测入口调用；下表 CPU 数据包含分析器开销。

| 路径 | 旧版 µs/次 | 优化版 µs/次 | 旧版分配 B/次 | 优化版分配 B/次 |
| --- | ---: | ---: | ---: | ---: |
| 空地图生成列表 | 0.300 | 0.108 | 64 | 0 |
| 100 条尚未到期的生成计划 | 3.061 | 0.645 | 64 | 0 |
| 闲置陷阱 | 0.349 | 0.139 | 64 | 0 |
| 未到期的定时陷阱 | 0.437 | 0.245 | 64.0008 | 0.0008 |
| 每次执行一个动作的陷阱 | 1.446 | 1.044 | 416 | 288 |

未到期陷阱的两版计数均额外出现 96 字节，摊到 120000 次为 0.0008 B/次，不能将此称为严格零分配。执行动作时仍保留目标字典及任务解析所需分配。上述为固定调用测试，不是自然运行 FPS / TPS、GPU 性能或整个模组的提速比例。

完整原始数据和对照：`D:/RimWorld-Dev/TestWorkspaces/CQFCorePerformance/PerformanceComparison.json`。旧版证据为 `Baseline/Evidence-9eacddac30ae48158c318490094fd06c`，优化版为 `Candidate/Evidence-8ce33cdc1b8346d2b7845eb2d588b762`；两轮各 28 项检查、1 张原生分析器截图，均 COMPLETE 且日志无游戏 ERROR。保留缺少 ExposeData / 命名空间的夹具编译失败和漏传陷阱材质的失败运行；不将这些轮次计入通过或性能结论。
