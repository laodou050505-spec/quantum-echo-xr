# Product

<!-- impeccable:product-schema 1 -->

## Platform

android

## Users

PICO XR 玩家；在空间游戏中操作第三人称角色，通过左摇杆、手柄射线与按键解决时序解密。

## Product Purpose

《量子回声》是四关深空科幻解密游戏。玩家记录自己的路径与交互，将其封存为可重放的“回声”，与过去的自己合作开门、同步双稳定器、调度列车并引开追猎体。

## Positioning

核心机制是“将自己完成的空间路径封存为可重放的协作者”，玩家以空间与时间的交叉解决机关。

## Operating Context

游戏在低环境光的 PICO XR 视野中使用。核心玩法对象位于真实世界空间；标题与任务提示需要在舒适距离内清晰可读，不得遮挡机关和角色。

## Capabilities and Constraints

- 官方 Unity 6000.5.5f1 与 PICO Integration SDK 6.0.0。
- Android ARM64、IL2CPP、Activity、PXR Loader、Multiview、Input System-only。
- 保留桌面操作回退，但所有玩家可见键位仅显示 PICO 手柄。
- 开始页是独立、不透明的启动阶段；按开始后才进入世界空间玩法。
- 无真实头显时不宣称舒适度或实体手柄已验证。

## Brand Commitments

- 游戏名称：量子回声 / Quantum Echo。
- 主题：深空量子实验、时间回声、路径记录、科学仪器感。
- 主色为深海军蓝与离子青，回声紫标记过去路径，琺珀色只标记当前可操作目标。
- 封面英文品牌字允许由 Tripo Nano Banana 直接生成；交互按钮、操作提示和游戏内文字使用 Unity TMP 保持清晰与可编辑，UI 面板母版不烘焙动态文字。

## Evidence on Hand

- 实际桌面游戏画面：`Logs/VisualEvidence/Gameplay-V6-opaque-20260908.png`。
- Tripo Nano Banana 封面主视觉：`Assets/QuantumEcho/UI/Generated/CoverV6Clean/01_generated_image_url.png`。
- Tripo Nano Banana 面板母版：`Assets/QuantumEcho/UI/Generated/PanelV2/01_generated_image_url.png`。

## Product Principles

- 一眼看懂当前目标、手柄键位和下一步。
- 主角、机关和出口的视觉层级高于装饰。
- 交互用颜色角色和反馈教会玩家，不依赖密集说明。
- 视觉克制、字号足够、对比稳定，在 XR 中优先舒适与可读。

## 2026-09-08 舒适 UI

- 常驻面板保留当前目标、简短说明和当前动作键位；完整手柄说明通过【左摇杆按下】展开/收起。
- 桌面实拍用于验证遮挡与文字排版，PICO 头显实体键位与持续游玩舒适度待实机确认。


## 2026-09-08 追加：碰撞与顶部任务面板

按用户 15:03 截图反馈，任务面板改为顶部居中、7% 顶边距、按视口宽度适配；适度放大并重新排列关卡、帮助入口、目标、说明与动作。实体模型补齐按本地网格尺寸拟合的盒碰撞体，角色碰撞尺寸匹配可见模型，补充移动物体接触静止角色的分离处理与追猎体扫掠。封面保持已交付的 V6Clean。验证及安装证据以本轮碰撞/HUD 日志为准。


2026-09-08 V8 修复：用户报告 PICO 面板出画、水晶无法交互。XR 面板改用双眼共同可见区域与上方角度限制；水晶改为按实体表面距离判定并排除玩家自身的射线阻挡，保留外部墙体遮挡。新增 57 项交互/XR 检查通过，包含四关各三个方向从可站立边界持续吸取至完成。此前“流程 PASS”主要是场景引用检查，不应作为此次交互回归已覆盖的证据；以新版 ReachAndXRQA 为准。
