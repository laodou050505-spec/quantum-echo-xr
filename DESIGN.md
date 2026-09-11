---
name: Quantum Echo
description: 深空时间回声中的电影式入口与克制的实验仪器读数
colors:
  menu-pearl: "rgb(83% 92% 94%)"
  type-ink: "rgb(2% 4.5% 6%)"
  menu-dark: "rgb(3.5% 6% 7.8% / 93%)"
  type-pearl: "rgb(85% 92% 95%)"
  type-white: "#ffffff"
  focus-cyan: "rgb(20% 100% 95%)"
  press-amber: "rgb(100% 68% 22%)"
typography:
  menu-label:
    fontFamily: "Noto Sans SC"
    fontSize: "29px"
    fontWeight: 700
    letterSpacing: "4px"
  objective:
    fontFamily: "Noto Sans SC"
    fontSize: "36px"
    fontWeight: 700
  body:
    fontFamily: "Noto Sans SC"
    fontSize: "28px"
    fontWeight: 400
  support:
    fontFamily: "Noto Sans SC"
    fontSize: "24px"
    fontWeight: 400
rounded:
  square: "0px"
components:
  button-start:
    backgroundColor: "{colors.menu-pearl}"
    textColor: "{colors.type-ink}"
    typography: "{typography.menu-label}"
    rounded: "{rounded.square}"
    width: "448px"
    height: "62px"
  button-exit:
    backgroundColor: "{colors.menu-dark}"
    textColor: "{colors.type-pearl}"
    typography: "{typography.menu-label}"
    rounded: "{rounded.square}"
    width: "448px"
    height: "62px"
  button-focused:
    backgroundColor: "{colors.focus-cyan}"
    textColor: "{colors.type-ink}"
    typography: "{typography.menu-label}"
  button-pressed:
    backgroundColor: "{colors.press-amber}"
    textColor: "{colors.type-ink}"
    typography: "{typography.menu-label}"
---

# Design System: Quantum Echo

## Overview

**Creative North Star: "深空时间回声"**

入口以深空建筑、时间残影无人机和大幅品牌字建立电影式游戏气氛；进入玩法后，辅助读数位于视野顶部中央，角色、机关与路径成为主角。保留《量子回声》的深海军蓝、离子青、回声紫和实验仪器语义；金属和相位痕迹来自实际图像与空间场景，而非通用玻璃卡片。

本次记录以 V6 源码和桌面实拍为准，更新旧 V5 文档。视觉权威来自用户提供的游戏参考：concept-seed 已调用但未返回输出，随后明确采用用户参考作为回退；没有另行批准的独立设计稿。位置、距离和视角是当前实现与设计意图，不代表已通过真实 PICO 头显的舒适度或实体键位验证。

**Key Characteristics:**
- 大幅定制品牌字与真实可交互菜单分工。
- 青色表达聚焦与相位，紫色表达过去的回声，琥珀表达即时动作反馈。
- 文字保持 TMP 实时渲染，哑光母版只承载表面。
- 辅助 HUD 默认收起完整操作表，展开时只向下增长。

## Colors

整体以墨色深海军蓝和珍珠白形成低光对比，青色照亮相位与焦点，紫色在图像和世界中承载时间残影。

### Primary

- **Focus Cyan**：实时菜单聚焦填充。场景光照与生成图中的青色属于各自材质，不强制替换为菜单值。

### Secondary

- **Press Amber**：菜单按下瞬间的反馈；辅助 HUD 的短反馈也采用暖色，但源码使用独立亮度。开始按钮静态为珍珠白，不能沿用“开始默认琥珀”的旧描述。
- **Echo Violet**：过去路径和重复无人机的图像语义；本次未从位图反推虚构的规范色值。

### Neutral

- **Menu Pearl / Type Ink**：主要动作底色与深色文字。
- **Menu Dark / Type Pearl**：次要动作的暗面与浅色文字。
- **Type White**：目标标题、完整操作表和过关正文。

前置 token 精确保留 Unity 的归一化 RGB 数值；封面及面板图像保留自己的连续色阶，不再将旧文档的六个近似十六进制值冒充统一源码 token。

## Typography

**Display Font:** 主视觉内生成的双行定制 QUANTUM ECHO 字形；不是可复用字体，也没有可声明的字体栈。

**Body / Label Font:** `Assets/QuantumEcho/Fonts/NotoSansSC-Dynamic.asset` 的 Noto Sans SC，通过 TMP 渲染中英文。

前置 `px` 是便于 DESIGN.md 工具展示的 Unity UI 设计单位映射，不是头显屏幕像素，也不保证实际角字号。菜单标签、目标标题、正文和提示构成紧凑层级；菜单使用粗体与加宽字距，目标用粗体，说明用常规字重。当前辅助 HUD 另有 24 单位关卡行与展开提示、26 单位完整操作表；过关标题为 36 单位。这些为具体组件参数，不扩张为全局字号阶梯。未从源码推断统一行高。

**The Live Action Rule.** 可交互按钮、目标、键位和状态必须保持实时文字；定制英文品牌字可保留在封面主视觉中。

## Layout

标题是独立、不透明的启动场景。正式主视觉为 `Assets/QuantumEcho/UI/Generated/CoverV6Clean/01_generated_image_url.png`：左上大幅双行品牌字，左下实时操作，右侧时间无人机、紫色残影、青色光口和阶梯建筑。作者画布为 1600 × 900，比例缩放为 0.003，背景约 4.8 × 2.7 米，展示距离参数为 3.8 米。两个菜单动作中心分别在画布 (-476, -116) 与 (-476, -200)，按钮尺寸由 token 规定；按钮内部文字框宽 392，左右各留 28 单位。操作提示位于 (-476, -287)。这是当前标题构图，不是所有屏幕的通用网格。

桌面版辅助 HUD 距摄像机 2.1 米，顶部居中，顶边固定在视口高度 93% 处，预留 7% 顶部边距。画布宽 780、高 320 UI 单位，默认缩放 0.0015；窄视口会进一步缩小，左右至少留 10% 空间。正文宽 712，左右各 34；标题 36、正文与动作 28、关卡和帮助入口 24 单位。关卡与帮助入口同处第一行，目标、说明、动作分别占据独立文字区域。展开时高度为 500，向下增加操作表，顶边不动。位置根据相机投影实时更新；已在 16:9、4:3、1:1 与 44°、60°、90° FOV 的组合检查边距。上述几何验证不代表真实头显舒适度验收。

短暂过关通知相对摄像机位于 (0, 0.20, 2.20) 米，缩放 0.00105，画布 660 × 210；它只反馈完成与倒计时，不承载主要操作。完整任务终端与机关保留世界空间。

**The Stable Header Rule.** 展开操作说明时保持目标标题与面板顶边原位，只向下增加内容。

## Elevation & Depth

标题按钮为平面矩形，层级由填充明暗、焦点线和微小缩放表达；建筑体量与无人机深度由主视觉承担。玩法中实体金属、相位光和空间遮挡构成深度，面板使用哑光纹理。没有可复用的 CSS 阴影或玻璃模糊 token。

辅助 HUD Canvas 排序为 24；专用背板材质队列为 4100，文字队列为 4110，均在世界全息标记的 4000 之后。过关通知 Canvas 排序为 30；不要把 Canvas 顺序与材质队列混为一谈，也不要推断它使用辅助 HUD 的专用材质。完整世界终端保持深度感知，不继承辅助面板的覆盖规则。

**The Readable Auxiliary Rule.** 顶部辅助 HUD 的背板先于自身文字绘制，并覆盖世界全息标记；完整世界终端继续参与世界深度遮挡。

## Shapes

菜单采用清晰矩形，无立体按钮厚块。窄焦点底线只随焦点出现。面板边缘、细线和角部来自 `Assets/QuantumEcho/UI/Generated/PanelV2/01_generated_image_url.png`，运行时使用 `Assets/Resources/QuantumEcho/QuantumPhasePanel.png` 的切片 Sprite。`QuantumGeneratedPanelSkin` 使用 Sliced、pixelsPerUnitMultiplier 6，按容器尺寸拉伸，不保持原图比例；不要把贴图角部反推为圆角 token。

## Components

### Title actions

START GAME 是浅色主动作，EXIT 是暗色次动作，宽高相同。右手射线聚焦切换为青色，出现宽 448、厚 2 单位底线，并放大到 1.015 倍；扳机按下显示琥珀色、缩至 0.965 倍，反馈保持 0.12 秒。该倍率取自标题构建器对组件默认值的实际覆盖。切换场景或退出是动作结果，源码没有独立的持久“已确认”视觉样式。

### Objective readout

常驻内容依次为关卡/目标索引、当前目标、说明、当前动作、操作说明入口。短反馈临时替换动作行，最短显示 1 秒后恢复，避免两条消息叠加。面板完全不透明，文字不接收射线；主要游戏交互仍在世界中发生。

### Expandable controls

【左摇杆按下】展开/收起；桌面 Tab 是回退入口。操作表保持实际映射：【左摇杆】移动；【右扳机】交互 / 持续吸取；【握把 / A / X】封存回声；【B / Y】撤销；【左菜单键】重置。帮助开关不占用 A、X 或握把。玩家可见提示继续显示 PICO 键位。

### Generated panel skin

同一无文字母版服务辅助读数与过关通知。默认不透明；过关使用 0.98 opacity。动态文字保持 TMP，背景不烘焙标题、状态或操作提示。

### Completion notice

居中显示完成标题与下一关倒计时，短暂出现且不可交互；标题为青色、正文为白色，不扩展为常驻仪表盘。

桌面证据位于 `Logs/VisualEvidence/Title-V6-final-20260908.png`、`Gameplay-V6-final-20260908.png` 和 `Help-V6-final-20260908.png`。这组三张图记录当前构图，但早于最后的辅助 HUD 4100/4110 绘制顺序修复，不能作为该修复已验证的证据。源依据为 `QuantumEchoTitleSceneBuilder.cs`、`QuantumComfortObjectiveHUD.cs`、`QuantumTitleMenuButton.cs`、`QuantumGeneratedPanelSkin.cs` 与 `QuantumLevelTransitionHUD.cs`。

## Do's and Don'ts

### Do:
- **Do** 使用正式 CoverV6Clean 主视觉与 PanelV2 衍生母版，保留动态图文分离。
- **Do** 保持开始与退出同宽，以明暗和状态区分层级。
- **Do** 默认显示当前目标、简短说明、当前动作与展开入口。
- **Do** 用桌面证据说明排版，用真实头显另行验证舒适度和操作。

### Don't:
- **Don’t** 将旧 V5 小标题、常驻完整操作表或 8% 统一内边距继续当成当前规则。
- **Don’t** 把角色和机关改成辅助 HUD 内的平面操作对象。
- **Don’t** 用世界全息的穿透效果覆盖辅助 HUD 正文。
- **Don’t** 从一幅封面推广出所有界面都必须拥有巨型标题或阶梯构图的要求。


## 2026-09-08 碰撞与顶部 HUD 修订

实体导入模型使用按网格本地边界拟合的轻量盒碰撞体，随模型父级变换同步；地板、门、设备、列车与远景实体均覆盖。角色控制器半径 0.96 米、高 1.92 米，覆盖可见外壳。回声模板、射线、全息标记和 UI 维持原有非实体语义。移动实体与静止角色重叠时做最小分离修正；追猎体移动使用盒体扫掠。


## 2026-09-08 V8：XR 可见范围与实体交互修复

桌面仍使用顶部 7% 边距；Android/PICO 改按左右眼非对称投影的共同可见范围布局，同时限定在相机前方 2.1m、顶边不高于 0.8m 的上方视野内。布局在 LateUpdate 和渲染前刷新，双眼投影尚未就绪时使用保守的相机局部范围；完整帮助也必须留在共同可见范围中。不要再用 V7 的单目视口 93% 顶边规则描述 XR。

水晶和碎片交互使用目标实体表面的最近点，并保留外部实体的射线遮挡。角色自己的实体碰撞体与查询 trigger 不拦截交互射线。四关水晶在真实碰撞边缘约 1.81–1.86m 的中心距离处，均已执行持续吸取与完成判定测试。

## 2026-09-08 V9：左上目标面板

辅助目标面板由顶部居中改为左上安全区锚定。桌面左、上边距分别为 6% 与 7%；PICO/XR 按左右眼共同可见画面的真实左上边缘定位，使用 NDC ±0.88 的可见范围并保留 0.06m 左、上内缩，不再限制在中央舒适框。收起与展开状态共用同一左上锚点，帮助内容只向下增长。

PICO 模拟器镜像会放大双眼边缘偏移，因此实际 XR 锚点还需与相机局部校准框 `x=-1.55..1.55`、`y=-1.05..1.5` 相交。该框用于防止面板越过模拟器画面左上边缘，不改变桌面布局。

面板宽度由 780 提升到 840 设计单位，最大世界缩放由 0.0015 提升到 0.00175；标题、正文、当前操作与控制表同步放大并增加垂直间距。面板仍保持完全不透明、动态图文分离以及 4100/4110 的背板与文字绘制顺序。

标题封面的世界位置与 4.8×2.7 比例保持不变。封面材质改用 `QuantumEcho/CoverFeather`，四边以 4% UV 宽度渐隐到后方深色遮挡板，使矩形边缘柔和融入黑色背景，但不移动或裁切主视觉与菜单。

根据 2026-09-08 19:05 的两张 PICO 模拟器对比图，XR 标题展示距离由 3.8m 调整为 3.05m。封面、菜单和提示围绕原中心一起等比放大约 25%，对应目标截图的占屏比例；桌面标题相机和封面世界尺寸不变。羽化宽度由 4% 提升到 5.5%。

最后一关的 `Echo Lure Beacon` 与 `Hunter Sentinel` 是非实体玩法对象。它们不拥有 Collider，不参与角色阻挡；诱饵由回声路线进入残响区域判定，追猎体抓取由平面距离判定。批量模型碰撞安装必须排除这两类完整层级。封面羽化后续提升到 7%。
