# 对话表现风格

现有对话默认保持滚动窗口。`DialogTreeDef.dialogStyle` 为空时继续使用原窗口绘制、历史记录和选项处理；默认导出的 XML 不新增风格字段。选择新风格不改变条件、结果动作、物资消耗和节点跳转。

## Galgame 风格

在对话编辑器的对话树设置中选择 `CQF_Galgame`，配置左右默认立绘。节点详情提供发言方、左右覆盖开关以及本节点的图片列表。新增界面文案按用户规则保留同名 Key，22 项待审核记录位于 `TextReview/DialogPresentation.csv` 与 `.xlsx`。

- 立绘来源：`Participant` 使用对话参与者的实际外观，`Image` 使用自定义纹理，`Hidden` 隐藏这一侧。默认左侧为 `Interviewee`，右侧为 `Interviewer`。
- 立绘可以配置图片路径、名称 Key、缩放、横纵偏移和镜像。名称 Key 为空时使用绑定参与者的名字。人物用原版 `PortraitsCache` 渲染，非人物对象显示物件图标。
- 节点 `speaker` 为 `Left`、`Right` 或 `None`。当前发言者正常亮度，另一侧压暗；旁白不显示发言者姓名，两侧保持正常亮度。
- 节点默认使用对话树立绘。`overrideLeft` 或 `overrideRight` 为 true 时使用该节点的对应配置；关闭覆盖保留已经填写的覆盖值。
- 节点图片 `placement=Background` 时按比例填满背景并裁切，`placement=Center` 时完整放在两侧立绘之间。多张中间图片纵向排列，多个背景按列表顺序叠加。两种用途可同时使用。
- `scale` 范围 0.1–3，横纵偏移范围 -1–1；偏移以所在图片区域的尺寸为单位，正值向右或向下。图片超出对应区域的部分裁切。
- 节点图片随节点切换。无图片的节点不继承上一节点的背景或话题图。
- 未建立 Galgame 节点配置时，原节点图片以中间图片显示。首次编辑该节点的 Galgame 配置时复制原图片路径和缩放，后续分别保存；切换回原风格不会改变原图片列表。
- 图片选择器沿用 `Textures/UI/CQF/DialogImage` 下的资源，也可在设置中填写其他有效纹理路径。路径不包含 `Textures/` 前缀和扩展名。

窗口显示当前节点文本，保留现有选项的禁用状态和执行入口。长文本和选项分别滚动；窄窗口改为上下排列。没有选项的终点提供关闭按钮，切换到终点时清除上一节点的选项。

预览可选择当前地图的两个 Pawn 查看实际外观。预览列出分支供手动选择，不计算条件、不消耗物资、不执行结果动作。背景、立绘和正文布局与运行时共用绘制实现。截图中的测试图片来自仓库已有资源，不代表新增立绘素材。

## XML 配置结构

树级字段为 `dialogStyle` 和 `presentations`；节点级字段为 `presentations`。二者都是风格配置列表，切换风格只改变 `dialogStyle`，保留未启用风格的数据。每个风格 ID 在同一层只能出现一次。

树级配置示例：

```xml
<dialogStyle>CQF_Galgame</dialogStyle>
<presentations>
  <li Class="QuestEditor_Library.CQFDialogPresentation_Galgame">
    <left>
      <source>Participant</source>
      <participant>Interviewee</participant>
    </left>
    <right>
      <source>Image</source>
      <participant>Interviewer</participant>
      <imagePath>UI/CQF/DialogImage/YourPortrait</imagePath>
      <mirror>true</mirror>
    </right>
  </li>
</presentations>
```

节点配置示例：

```xml
<presentations>
  <li Class="QuestEditor_Library.CQFDialogNodePresentation_Galgame">
    <speaker>Right</speaker>
    <images>
      <li>
        <imagePath>UI/CQF/DialogImage/YourBackground</imagePath>
        <placement>Background</placement>
      </li>
      <li>
        <imagePath>UI/CQF/DialogImage/YourTopic</imagePath>
        <placement>Center</placement>
        <scale>0.8</scale>
      </li>
    </images>
  </li>
</presentations>
```

示例纹理路径是占位符，须替换为 Mod 实际资源。示例为原有树和节点内的新增字段，不是独立 Def；定义本身的 `defName` 仍须带所属 Mod 前缀。

## 扩展接口

- 主库 `CQFDialogPresentation` 定义稳定的 `Id`、`LabelKey`、`CreateRenderer()`、`CreateNodePresentation(DialogNode)` 及配置检查。派生类的实例字段通过现有 `CQFSerialization` 保存，XML 使用 `Class` 标识实际类型。配置类需要公共无参构造函数，运行缓存不要放进配置字段。
- `CQFDialogNodePresentation` 定义对应的 `StyleId` 和节点专属字段。节点配置与树配置分开，公共文本、选项和结果仍归原对话模型。
- `CQFDialogRenderer` 定义窗口尺寸、边距、是否绘制原窗口背景，以及 `Draw(Rect, CQFDialogFrame)`。`Dispose()` 在运行窗口或预览关闭时调用，预览切换风格时也会释放旧绘制实例。
- `CQFDialogFrame` 提供当前节点、当前节点已经解析好的内容、选项、标题、两名参与者、关闭回调和 `IsPreview`。界面只调用提供的选项回调，不自行重新解析或执行 `DialogResult`。
- 可选编辑器程序集中的 `CQFDialogPresentationEditor` 按 `StyleId` 匹配树与节点编辑入口。扩展编辑器与运行数据分开依赖；主库不引用编辑器程序集。缺少编辑器实现会明确报错。
- `CQFDialogTreeWindow` 继续拥有会话状态，保留构造函数、公开字段、`GoToNode`、`CurrentNodeIndex`、`NodeChanged`、`Closed` 和 `IsClosed`。原演出调用无需改写。新增 `CurrentElements` 提供单个节点的内容，原 `elements` 继续保留历史。

没有新增 Harmony 补丁或旧存档迁移。新增风格的空配置、重复 ID、未知活动风格、无效枚举和非有限数值会给出诊断；缺失纹理在加载时报告并在画面显示缺失路径。

## 验证入口

- `Tests/DialogPresentation/Checks.csproj`：原有 XML、共享选项、风格数据往返、独立复制、撤销重做及无效配置。
- `Tests/DialogPresentationRuntime/RunChecks.ps1`：隔离原生游戏中的旧界面、新界面、节点事件、结果动作、预览无副作用、外部风格接入、关闭清理和截图。传入候选主库、编辑器路径及独立输出目录，使用 `-Launch` 执行。

本机 `rimworld-xml-dev` 技能未安装；本轮用 Rimsage 核对原版接口，并通过 `DirectXmlToObject` 实际加载导出的配置验证 XML，而非只做字符串检查。

## 本机验证记录（2026-10-29）

1.7.4982 主库与编辑器编译零错误，2330 项项目警告，新建表现层源码无编译警告。27 项数据检查、28 项原生实机检查以及 127 项既有演出检查通过。实机生成 8 张截图，已检查旧界面、双人物、中间图片、自定义图片、背景、窄窗口、预览及编辑配置；最终游戏 ERROR 为 0，stderr 为空。实机交互验证直接调用原选项回调，没有声称完成鼠标端到端测试。首轮测试夹具遗漏空 `ExposeData` 导致编译失败，修正后通过，失败证据保留。

原生验证目录：`C:/Users/24904/AppData/Local/Temp/CQF-Galgame-44eba92dad7148b3abe4f91514605fae/Runs/CQF_Cinematics_9ef7cac131494e93bd9bb6acb8d82656`。候选主库 SHA256 为 `24FE4BADABBAA679455BF6C3262B9A61929971B3EC84729608B6CF2A38037EAB`，编辑器为 `06E598FDF521749D636B9FA2F8757F63935EADF96F99EAFB5704B36265356E3B`。127 项演出检查使用独立编译输出，原始日志保留在同一验证根目录。

同一工作区的地图源码在本候选构建后继续更新，因此保留经过验证的对话源码和候选包，没有覆盖运行程序集。候选及日志、截图位于 `C:/Users/24904/AppData/Local/CQF/DeploymentPackages/CQF-DialogPresentation-1.7-20261029-44eba92d`；统一部署前应按最新完整源码重新编译，不直接覆盖为这个较早的候选。未修改版本号，未提交或推送 GitHub，未部署 1.6 文件。

## 统一部署等待记录（2026-10-29 12:34）

用户要求等地图修改结束后再统一编译部署。地图首轮完成后，已从当前完整工程重新构建主库和编辑器，零错误、2330 条既有警告。同一候选通过 27 项对话数据检查、28 项对话原生检查、55 项地图规则检查和 29 项地图原生检查；两个原生测试均无游戏 ERROR，stderr 为空，并复查了双人物与背景图片截图。

候选、源码与 XML 快照、日志及截图保留于 `C:/Users/24904/AppData/Local/CQF/DeploymentPackages/CQF-DialogMap-1.7-20261029-122604-f0116cbc`。主库 SHA256 为 `678F7B5DA8A472E21AC7FBDF6F349C743FF40D3FC18CF0CF0B69B06095A8FCFD`，编辑器为 `05DDD77C903AA98C2B5E0F0D01BFAB2D543015C018386C499740CB65FB9A1ADE`。

部署前的进程检查检测到另一任务的隔离游戏测试，因此在任何运行文件替换前停止。随后源码快照检查发现类别筛选正在修改四个共享源码文件；地图任务也已开始新一轮生态布局扩展。本候选已过期，本轮没有替换运行程序集。已在当前对话安排每 10 分钟检查上述任务，等待修改完成后重新构建与验证完整源码，再备份部署。包内 `DeployVerified.ps1` 仅作为部署流程参考，不可跳过检查直接发布旧候选。

2026-10-29 13:05 后续检查读取到用户在地图任务中明确选择“先保存成果，暂不部署”。据此取消本对话的自动部署检查，统一部署等待用户重新安排；候选与验证证据保留，本对话没有执行运行文件替换。

## 统一验证记录（2026-10-29）

用户随后明确要求“本次先只验证”。从实际工程复制独立快照，结合地图分支 `0da359f` 的最终资源与中文验证修正，统一构建主库和编辑器。生产源码与实际工程一致；最终地图的定义、中文资源迁移和测试差异只放入验证快照，没有替换运行目录。

同一候选通过对话数据 27 项、对话原生 28 项、地图规则 72 项、中文地图原生 52 项、类别筛选 24 项及演出回归 127 项，共 330 项检查。124 份 XML 解析通过；编译零错误、2330 条既有警告。两个原生运行的游戏 ERROR 为零、stderr 为空；复查了旧对话、双人物话题图和中文生态界面截图。类别筛选本轮只有逻辑检查，不宣称鼠标端到端验收。

完整验证包为 `C:/Users/24904/AppData/Local/CQF/DeploymentPackages/CQF-CombinedValidation-20261029-131352-47c436a9`，包含独立工程、最终地图资源差异清单、候选、测试日志、截图与 `Delivery.json`。主库 SHA256 为 `E09BCC1CD350E49070786FA1485B1C1DADD69B801B9E5EB36B3A2A2353695213`，编辑器为 `1A8C07BA5D4A1E512CDC10A205C23B6A7E52C4D4CB820A5C5A20EE6B3551B505`。验证前后原工程的 1275 个输入文件内容和文件集合均未变化；之后只补充本文记录。本轮没有部署，自动部署检查未重新启用。

## GitHub 保存范围（2026-10-29）

用户授权验证完成后更新 GitHub。对话功能独立保存至 `codex/dialog-presentation`，基于主分支 `b87e196`，包含表现层、编辑与预览入口、双语占位 Key、文本审核表、测试、说明及对应的 1.7 候选 DLL/PDB。地图、类别筛选和其他任务的未提交改动不纳入本对话提交，联合验证记录仍保留。

该独立分支重新构建零错误、2330 条既有警告，27 项对话数据、28 项原生对话和 127 项演出回归检查通过，共 182 项。原生游戏 ERROR 为零、stderr 为空，八张截图保存在上述验证包的 `GitHubRuntime`；构建和检查日志以 `GitHub` 开头。分支主库 SHA256 为 `C221A88A553A8FA35DE227898F450C25BB39352BE96E9A45FB58D076A238E7B9`，编辑器为 `3BBFB5E2C9D8C8F1C9E0649A9B653D38878F6BE0F412F9943757D9C71442DB25`。候选只复制到独立 Git 工作区用于保存，没有替换当前游戏目录；版本号与 1.6 运行文件保持原状。
