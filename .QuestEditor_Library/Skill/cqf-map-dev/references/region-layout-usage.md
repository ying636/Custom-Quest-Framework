# 区域标记与布局生成

适用于 RimWorld 1.7、CQF 主程序集与可选编辑器模块。布局算法属于主程序集，绘制和预览界面属于 CQF.Editor。没有启用编辑器时仍可通过地图模板的 `customSteps` 执行布局。

## 主城外围聚落

1. 开启开发者模式和上帝模式，在建筑菜单的 CQF 编辑工具分类选择“区域标记”。
2. 新增区域，框选或涂抹主城。设置唯一 Key（例如 `CQF_MainCity`）及可选 tags（例如 `MainCity`），应用属性，开启区域保护。
3. 用同样方式标记需要保留的入口。区域可以重叠，所有保护区域取并集。区域标记不改变原版允许区域或事件触发区域。
4. 若要生成道路，在主城边界添加连接点，朝向指向区域外侧，连接点所在格应可通行。聚落部件也需要入口：区域连接点、带方向的区域核心，或 `enterSpots` 配合 `enterDirection`。
5. 打开“布局生成”，选择“村落与卫星聚落”，引用 `CQF_MainCity`，设定距离、数量、间距、方向及随机种子。增加部件规则，在规则的候选池中填写部件 tag 或指定部件。
6. 预览显示部件占地、道路及保护范围；右键选择生成。预览不修改地图。执行前会重新规划，地图变化导致结果改变时需重新预览。
7. 生成结果自动登记为 `outputKey/运行序号/Part_序号` 区域。设置 `outputTags` 后，后续步骤可按 tag 找到这些聚落继续生成外围内容。

用户已允许生成正式中英文文案，区域和布局界面使用自然文本。对应 CSV 与 Excel 位于 `.QuestEditor_Library/TextReview/MapLayouts.*`；生成文案的来源按审核规范保留为“生成占位”，此标记不在游戏界面显示，审核状态仍为“待审核”。

## 生态底图与兴趣点

1. 先标记并保护主城、入口等需要保留的内容，然后选择“生态底图与兴趣点”。
2. 打开“生态底图设置”，选择基础生态。可使用原版或已加载 Mod 的生态资源，或添加自定义地形、植物及权重；设置植被密度、水域比例、山石比例和斑块尺度。植被密度为 `-1` 时采用所选生态的值。
3. 增加兴趣点部件规则，在候选池填写已有部件 tag。每条规则可选择岸边、山石边缘、林中空地、开阔地或任意适合陆地，并设置距环境特征的距离、允许地形和入口可达性。
4. 预览会先显示规划后的水域、山石和树木，再显示符合条件的部件占地。兴趣点依据这份新底图选址。执行前地图内容有变化时，旧预览会被拒绝。
5. 执行后，结果区域同时带有 `outputTags` 和该规则的 `role`，供后续脚本查询。关闭“重新生成生态底图”即可保留当前生态，仅寻找新兴趣点。

生态底图处理整张地图的可修改格，包括地图边缘；`allowedRegion`、`edgeMargin` 和主城距离约束限制兴趣点与道路的布置范围，不裁剪整图生态。保护区域、禁止生成或摧毁的格、已有人工对象占用格以及有屋顶的格保留。水域和山石比例以其余可修改格为基数；自定义地形权重用于连续斑块采样，植物权重还受土壤肥力筛选。

岸边与山石边缘距离从部件完整占地到最近特征格计算；部件不能压在水或山石上。林中空地要求占地内树木较少、周边有树；开阔地要求占地内树木稀少。可达性从参考区域的连接点检查；无可用连接点时从地图边缘检查。也可以显式关闭可达性条件以布置孤立遗迹。

基础生态改变当前地图的生态身份，并随存档保存；不会改写世界地块或共享的 BiomeDef。自定义地形和植物列表用于本次生成，后续自然生长仍由游戏及基础生态负责。此功能读取生态的地形、野生植物与山石资源，不直接重放第三方的整图 GenStep 或特殊地形生成器；缺少可用地形、水体或山石时会明确报错，需要补充自定义配置。

以下示例沿用已有 `CQF_SettlementPart` tag：

```xml
<li Class="QuestEditor_Library.CustomMapStep_Layout">
  <layout>CQF_Layout_Ecology</layout>
  <relation>Anywhere</relation>
  <seed>517</seed>
  <outputKey>CQF_EcologyPoints</outputKey>
  <outputTags><li>PointOfInterest</li></outputTags>
  <ecology>
    <biome>TemperateForest</biome>
    <waterTerrain>WaterShallow</waterTerrain>
    <waterCoverage>0.08</waterCoverage>
    <rockCoverage>0.12</rockCoverage>
    <patchSize>24</patchSize>
  </ecology>
  <parts>
    <li>
      <role>ShoreSettlement</role>
      <count>2~3</count>
      <pool><tags><li><tag>CQF_SettlementPart</tag><weight>1</weight></li></tags></pool>
      <pointOfInterest>
        <habitat>Shore</habitat>
        <featureDistance>1~8</featureDistance>
        <requireReachable>true</requireReachable>
      </pointOfInterest>
    </li>
  </parts>
</li>
```

## 两种 tag 的区别

| 数据 | 用途 |
| --- | --- |
| 区域 `tags` | 指出当前地图上的语义范围，例如主城、入口、聚落 |
| `CustomMapDataDef.tags` | 从已有部件库抽取建筑、营地等模板 |
| 规则 `pool.tags` | 加权候选来源，复用原有候选池结构 |
| `requiredTags` / `anyTags` / `excludedTags` | 分别要求全部命中、至少一个命中、全部排除 |

规则只抽取 `isPart=true` 的定义。显式部件优先采用显式权重，不与 tag 候选重复计算。原有 `CustomMapGenerationSet.GetMap` 行为保持不变。只填过滤条件不会自动扫描全部定义，仍需配置候选池。

## 脚本配置示例

以下 `CQF_SettlementPart` 是需要作者为现有部件设置的示例 tag，不是新建的美术或建筑资产。将步骤加入原有地图的 `customSteps`，使底图和区域先建立。`preCustomSteps` 不能执行该步骤。

```xml
<customSteps>
  <li Class="QuestEditor_Library.CustomMapStep_Layout">
    <layout>CQF_Layout_Village</layout>
    <referenceKey>CQF_MainCity</referenceKey>
    <relation>Around</relation>
    <distance>12~35</distance>
    <spacing>6</spacing>
    <seed>517</seed>
    <connectRoads>true</connectRoads>
    <roadTerrain>Concrete</roadTerrain>
    <roadWidth>3</roadWidth>
    <outputKey>CQF_Settlements</outputKey>
    <outputTags><li>Settlement</li></outputTags>
    <parts>
      <li>
        <role>Content</role>
        <count>3~5</count>
        <pool>
          <tags><li><tag>CQF_SettlementPart</tag><weight>1</weight></li></tags>
        </pool>
      </li>
    </parts>
  </li>
</customSteps>
```

也可使用 `referenceTags` 查找区域。默认必须唯一；`eachReference=true` 时按稳定顺序对每个匹配区域执行。实例内 Key 优先解析为本实例的区域，重复放置部件不会覆盖另一个实例的同名区域。区域 ID 和原有物体 `targetKeys` 是两套标识；原有物体目标仍需全地图唯一，重复部件携带同一目标 Key 会在预检时报错。

`Around` 的距离从区域实际边界到部件完整占地计算，支持不规则区域。`Inside` 要求完整占地在区域内；`Outside` 要求与区域完全不相交；`Anywhere` 不限制相对位置。`allowedRegion` 可进一步限制允许生成范围。`directions` 可限定外围方位，未填写表示不限。保护约束始终生效，因此要在区域内部生成时应按意图关闭该区域的保护。

## 布局定义

| defName | 已实现行为 | 部件要求 |
| --- | --- | --- |
| CQF_Layout_City | 网格街区；启用道路时铺设街道并连接部件入口 | 每个部件完整占地不超过 `gridSize-roadWidth-2` |
| CQF_Layout_Village | 围绕参考区域分散角度放置，也可无参考散布 | 连接道路时需可用入口 |
| CQF_Layout_Scatter | 按随机种子散布，保留间距和既有占用 | 适合荒野探索点、营地 |
| CQF_Layout_Clusters | 先选择相互分离的群落中心，再优先在中心附近放置 | `clusterCount` 控制中心数量；使用规则 tags 选择部件 |
| CQF_Layout_Ruins | 城市网格上按 `vacancy` 留出空地 | 完整与残破部件由作者分别配置 tag、数量和权重 |
| CQF_Layout_Fortified | 填满矩形外圈的墙、四角与南侧门，内部放置普通内容 | 必须各有一条非可选 `Wall`、`Corner`、`Gate` 规则；外围模块完整占地等于 `gridSize × gridSize`，基础朝向 North |
| CQF_Layout_Ecology | 先规划整图生态，再按环境条件抽取和布置兴趣点 | 配置基础生态与候选池；可达性检查需部件有可用入口或开放边缘 |

七个内置 Def 提供布局算法，不携带硬编码部件库。步骤的 `parts` 非空时使用步骤规则，否则使用布局 Def 的 `parts`。`optional=true` 才允许数量不足时跳过，并记录警告；必须项失败会阻止执行。布局会复用部件 `generationLimit`，规则也可配置 `maxPerDefinition`。

## 保存与保护边界

- 区域随当前格式存档保存，也随地图、部件导出。导出区域的一部分会明确拒绝，需扩大选区或调整标记；复制、旋转、偏移同步处理连接点方向和保护标记。
- 新布局避让既有建筑、物品、角色等占用，允许清除新部件占地内的植物和污物。保护区内的建筑、物品、地形、屋顶保持原状；多格建筑只要与保护区相交就整体保留。
- 原有禁止生成与禁止摧毁标记继续被尊重。新增保护仅约束布局生成，不阻止玩家正常游戏操作。
- 部件经原有 `GenerateByCore` 生成，嵌套 `ZoneCore` 继承本部件允许范围和保护范围，不能越界扩展。不能满足范围的核心会明确报错，不会突破保护区强行生成。
- 为保证范围可约束，作为候选的部件不能携带任意 `preCustomSteps`、`customSteps` 或 `generationActions`。原有未使用布局的地图不受此限制。普通原版对象、地形、屋顶及 CQF 既有部件生成仍走原有通道；外部 Mod 自定义生成回调不属于可验证的隔离环境。
- 执行前检查布局、道路和目标 Key；执行后检查实际入口的格子连通性。规划通过不代表任意第三方运行逻辑一定成功；运行期异常会记录并停止，已放置内容不会自动回滚，因此反复试验应使用编辑地图或另存副本。
- 道路当前采用指定地形，不包含道路模板拼装；生态布局增加允许地形及环境条件，其他布局沿用可通行底图检查。围合布局保证模块占地闭合，模块内部的实际围墙形状由部件作者负责。
- 不增加旧存档迁移，不更改现有任务、人物、事件区域的数据格式。

## 代码与验证入口

数据、规划与七种算法分别位于 `QuestEditor_Library/MapLayouts/`，步骤位于 `GenStep/CustomMapStep/CustomMapStep_Layout.cs`，绘制和预览位于 `CQF.Editor/Designator/` 与 `CQF.Editor/Window/`。

- `Tests/MapLayouts/Checks.csproj`：区域引用、复制旋转、XML、真实边界、候选过滤、保护、数量、种子、七种布局、生态分布、兴趣点环境与可达性、道路、目标冲突和预览一致性。
- `Tests/MapLayoutsRuntime/RunChecks.ps1`：独立游戏副本加载指定候选程序集，实际放置部件与道路、检查保留对象、保存并载入、检查原有地图生成入口，并输出截图；不会加载玩家原存档。
- 支持范围针对 1.7；验证结果需分别记录规则测试、构建、游戏运行结果，不用编译通过代替游戏内验证。

## 本机部署

`Tests/MapLayouts/Deploy.ps1` 在游戏关闭后从所在工程的当前源码重新构建，依次运行规则与独立游戏检查，再备份并替换主库及编辑器 DLL/PDB。日志与备份保存在本机 `AppData/Local/CQF/DeploymentPackages/RegionLayouts-*`。检测到运行中的游戏、检查失败或构建期间源文件变化时停止；复制失败时恢复备份。脚本不关闭玩家的游戏，不修改版本号或发布主分支。
