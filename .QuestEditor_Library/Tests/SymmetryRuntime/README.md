# CQF 对称模式实机检查

在 RimWorld 1.7 的独立离线副本运行，不使用个人存档。先按 cqf-overview 的版本与构建说明生成主库、编辑器，再构建本目录的 Checks.csproj。测试 DLL 仅用于临时测试 Mod，不放入交付 Mod 的程序集目录。

临时测试 Mod 的启动入口应在游戏完成 Mod 初始化后加载 CQF.SymmetryChecks.dll，再调用 CQF.SymmetryChecks.SymmetryChecksStartup.Start。这样不会让测试程序集提前加载可选编辑器。所需内容为 Harmony、Core、Ideology、Biotech、CQF（编辑器设置开启）和 RimDevTools 的 LogSyncMod。

夹具自动建立 81×81 编辑地图，覆盖格子中心／格缝轴与 1–4 格尺寸、四种朝向的 384 组往返；检查直接放置、屋顶和地形、相对属性刷去重、移动旋转、阻挡与缺失目标、复杂交互和文本、独立列表和目标注册，以及设置的 Scribe 读写。最后使用原生 SaveGame／LoadGame 重新加载完整游戏并检查地图设置。

界面检查输出 Symmetry-English.png 和 Symmetry-Chinese.png。SymmetryEvidence.json 的 success 必须为 true，同时核对本次进程加载的 DLL 路径、版本及时间，不能只把 RimDevTools manual 模式正常退出当作功能通过。

性能检查采用原生 FrameProfiler 的外层 scope，暂时把正常对称更新回调移到夹具内，每帧恰好执行一次，结束时恢复。每组预热 10 帧后收集 120 个完成帧，分别为关闭空闲、显示双轴、隐藏轴线、普通刷 100 格、双轴刷 100 格和普通刷相同的 400 个实际格子。输出包含原始帧号、路径、精确累计调用数、每帧均值、分配及丢弃统计。游戏处于暂停编辑状态，不能将这些结果换算为活动存档的 FPS／TPS。

本次实现的范围是 CQF 直接编辑工具与配置窗口。原版建造指令、其他 Mod 的专用编辑窗口、路线编辑和蓝图整体生成不属于该夹具或功能的覆盖范围。

测试结束后关闭进程，从测试配置移除临时 packageId，并把临时测试 Mod 移出副本的 Mods 目录。保留证据、配置和可复用离线副本；执行 RimDevTools test cleanup --status 确认没有测试锁或待恢复事务。
