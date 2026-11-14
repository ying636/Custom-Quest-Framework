# CQF 运行性能

原版 API 用 Rimsage 核对，CQF 实现从本地源码读取。1.7 实测使用原版 Frame profiler，由 `rimworld-source-dev` Skill 的 RimDevTools 流程管理独立离线副本。不要把未启用模块的成本、活跃地图模拟、离屏绘制和主库轮询的成本混作同一结论。投影历史采样见 [地图投影](../../cqf-map-dev/references/map-projections.md)。

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
