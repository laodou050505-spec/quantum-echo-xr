# 量子回声资源清单

更新时间：2026-08-11

## 当前可运行版本已具备

- 四个完整实验舱场景，使用 Unity 基础几何体搭建，所有模型均是 Scene 中的真实 GameObject。
- PICO 官方左右手柄模型：`LeftControllerModel.prefab`、`RightControllerModel.prefab`。
- 可编辑材质：深色金属、地面、青色能量、琥珀机关、洋红回声、红色危险、玻璃。
- 量子核心、压力板、能量门、相位列车、追猎体、数据碎片和回声角色的占位模型。
- 科幻 HUD、长按进度条、关卡目标、回声计数、计时器和结算面板。
- 五条已接入的环境音乐/反馈音效。

## 已接入的素材库音频

这些素材来自全局 `ange-embed` 素材库。用户已明确授权素材库资源可直接用于本项目。v2 内部素材记录未返回作者和独立许可字段，来源 URL 与素材记录 ID 保留如下。

| Unity 文件 | 用途 | 素材记录 | 来源 |
| --- | --- | --- | --- |
| `space_ambient.mp3` | 太空实验舱环境音乐 | `9c92566b-5353-4c9d-b593-60bf1f6af682` | https://cdn-ange.holymolly.ai/1/7/17794b64-cbe1-4be7-a810-9aadbf9454f1.mp3 |
| `confirm.mp3` | 机关确认 | `276f9833-01de-42fe-8a08-a0b9e8c7e2a1` | https://cdn-ange.holymolly.ai/7/5/755a7229-4c64-4413-b772-87909e82cda3.mp3 |
| `error.mp3` | 时序错误/被捕获 | `218fd599-cb20-4c57-9658-71247e84e26c` | https://cdn-ange.holymolly.ai/a/7/a71ea856-975a-4e66-8d11-572aea136e47.mp3 |
| `unlock.mp3` | 门与关卡解锁 | `112249ff-cd7c-4420-beff-29c984ef2396` | https://cdn-ange.holymolly.ai/1/4/14ecb939-3b5f-46c9-bcf0-93e44f00a5df.mp3 |
| `interact.mp3` | 轻量交互 | `28bd60c3-b937-40de-a28a-6cb70b3582a3` | https://cdn-ange.holymolly.ai/e/7/e740a0bd-d952-4288-a451-57f8408aed1b.mp3 |

## 已生成的 UI 素材

- 文件：`Assets/QuantumEcho/UI/Generated/01_generated_image_url.png`
- 用途：无文字科幻 HUD 面板素材表，可在 Unity Sprite Editor 中切片后替换各个 HUD Panel 的 Image。
- Tripo 模型：`banana`（Nano Banana 系列）
- Tripo 任务：`990cd7c4-7764-4395-a1fd-ed84f35cc9cf`
- 状态：success
- 消耗：5 credits

### 2026-09-07 量子航迹仪视觉更新

| Unity 文件 | 用途 | Tripo 模型 | 任务 | 状态 / 消耗 |
| --- | --- | --- | --- | --- |
| `Assets/QuantumEcho/UI/Generated/CoverV2/01_generated_image_url.png` | 独立启动封面主视觉；左侧留白，右侧为角色、核心、门、碎片与回声路径 | `banana` (Nano Banana) | `29e9b9f7-1c3c-4083-9eea-77faf39452be` | success / 5 credits |
| `Assets/QuantumEcho/UI/Generated/Panels/01_generated_image_url.png` | 统一科幻面板母版，无文字，中央安全区用于 TMP | `banana` (Nano Banana) | `7ea900d5-e580-4d79-8886-abcbf6917750` | success / 5 credits |
| `Assets/Resources/QuantumEcho/QuantumPhasePanel.png` | 从面板母版裁切的 Unity 9-slice Sprite | 来自上述面板任务 | 同上 | 已接入 Runtime Resources |

封面和面板图片不含任何烘焙文字；中文、手柄键位、目标和状态均由 Unity TMP 排版，可编辑且不产生 AI 文字乱码。

### 2026-09-08 量子回声标题封面 V4

参考 `/Users/ld/Desktop/游戏封面/` 中的构图原则（全幅叙事主视觉、稳定留白、纵向菜单、明确当前选项），没有复用其中的商标、人物、文字或界面资产。

| Unity 文件 | 用途 | Tripo 模型 | 任务 | 状态 / 消耗 |
| --- | --- | --- | --- | --- |
| `Assets/QuantumEcho/UI/Generated/CoverV4/QuantumEchoCoverV4.png` | 旧版横向标题封面主视觉，保留作视觉迭代参考 | `banana` (Nano Banana) | `12ee3575-32e8-4235-b749-03e5d07989f8` | success / 5 credits |
| `Assets/QuantumEcho/UI/Generated/CoverV5/01_generated_image_url.png` | 当前英文正式封面；左侧 `QUANTUM ECHO / SPATIAL PUZZLE` 品牌区，右侧量子门、核心、玩家球体、回声路径与碎片 | `banana` (Nano Banana) | `34f8858e-fad7-47a6-8995-b6884389de2c` | success / 5 credits |

按钮文字仍由 TMP 渲染。START/EXIT 世界按钮具有暗色实体底、射线悬停离子青、扳机按下琥珀色收缩以及确认切换反馈；封面图不烘焙文字或按钮。

## 待制作或替换的正式模型

当前占位模型已足够完成玩法，但以下内容需要正式美术模型。模型最好提供 FBX 或 Unity Prefab；若只有 GLB，导入前需要转换为 FBX，或后续给项目加入 glTF 导入包。

| 优先级 | 模型 | 建议数量 | 尺寸/要求 | Scene 中的替换位置 |
| --- | --- | --- | --- | --- |
| P0 | 模块化科幻实验舱 | 1 套 | 墙、地面、顶板、门框可拆分；房间约 14.6m x 12.4m | 每关 `ENVIRONMENT - walls floor lights` |
| P0 | 量子核心 | 1 个 | 直径约 0.7m，有发光材质或可分离发光部件 | `QUANTUM CORE Q-XX - replace model here` |
| P0 | 能量门 | 1 套 | 左右门板必须分离，动画沿 X 轴开启 | `ENERGY GATE - panels animate locally` |
| P0 | 压力稳定器/枢纽 | 2 种 | 地面直径约 2.1m，踩下时有发光状态 | `A/B - ... PAD` |
| P1 | 相位列车 | 1 辆 | 长约 1.4m，占位版本为小型无人运输车 | `PHASE TRAIN - replace model here` |
| P1 | 追猎体无人机 | 1 个 | 悬浮球形或多面体，红色识别眼可单独发光 | `SENTINEL 1/2 - replace model here` |
| P1 | 数据碎片 | 1 个 | 蓝色晶体/全息存储片，约 0.5m 高 | `DATA FRAGMENT 1/2 - replace model here` |
| P1 | 回声角色 | 1 个 | 半透明全身轮廓，建议 1.7m 高，不需要复杂面部 | `RUNTIME TEMPLATES/Echo Avatar Template` |
| P2 | 舱内管线、终端、警示灯 | 1 套 | 只作环境装饰，不应挡住主要路线 | 各关 `ENVIRONMENT` |
| P2 | PICO XR UI 图标 | 1 套 | 扳机、摇杆、主/次按键、菜单键；透明 PNG | `HUD - editable sci-fi panels` |

## 素材库 3D 候选（尚未导入）

3D 库当前只返回 GLB、封面与包围盒，没有作者和许可字段，因此这里只保留候选，不直接混入第一版 Unity Scene。用户确认最终外观后再转换/导入。

| 类型 | GLB 候选 | 预览图 |
| --- | --- | --- |
| 量子核心 | https://cdn-hm.holymolly.ai/c/0/c0018476-b9d0-44fc-8371-171067bc3c5b.glb | https://cdn-hm.holymolly.ai/5/1/517828b5-90c3-4805-a946-36365e22d38d.webp |
| 压力稳定器 | https://cdn-hm.holymolly.ai/d/c/dcd9f291-c382-4e70-a748-7637a0c4b43b.glb | https://cdn-hm.holymolly.ai/2/2/22f7cf96-a9a3-48ac-9ff4-31da7dad21d5.webp |
| 相位列车 | https://cdn-hm.holymolly.ai/e/9/e93f1fab-a59d-43cc-b704-843a63cda3f2.glb | https://cdn-hm.holymolly.ai/b/4/b4dbadc1-46e6-436f-9ab3-de69e8915b9f.webp |
| 追猎体 | https://cdn-hm.holymolly.ai/b/6/b6cde371-0c55-4dab-a31c-48dd6aabe303.glb | https://cdn-hm.holymolly.ai/7/8/7873f46e-ab0b-471e-a061-dd03ef865b1a.webp |
| 数据碎片 | https://cdn-hm.holymolly.ai/2/b/2ba30163-d894-46bb-a74c-a9e4d0acd838.glb | https://cdn-hm.holymolly.ai/e/4/e47b955b-6240-4063-9282-af7d75474905.webp |


### 2026-09-08 V6 封面与舒适 UI 重做

- 按用户游戏封面参考重做，使用 Tripo Nano Banana Pro：`CoverV6Clean/01_generated_image_url.png` 为正式封面，`CoverV6` 保留迭代来源。定制英文品牌字与主视觉由生图承担；按钮保持 Unity 实时交互。
- `PanelV2/01_generated_image_url.png` 为统一不透明磨砂面板，导入 `Assets/Resources/QuantumEcho/QuantumPhasePanel.png` 并按九宫格使用。
- 参考只用于构图与工艺标准，不复用参考游戏商标、角色与资产。当前 ange-embed MCP 不可用，未从未审查的外部资产库导入素材。
- CoverV6: task `c8cf66aa-7b1d-424d-982f-20d0cd0e2701`, status `success`, credits `15.0`.
- CoverV6Clean: task `e7629381-ec65-459a-b8bd-cd780ac23125`, status `success`, credits `15.0`.
- PanelV2: task `98b46888-57ae-42b5-8292-a06e57134080`, status `success`, credits `15.0`.
