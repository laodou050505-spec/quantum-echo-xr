# QUANTUM ECHO 英文版交付报告

生成日期：2026-10-22  
制作代理：QuantumEcho-EN（自主运行，全权翻译）  
英文工程路径：`/Users/ld/Documents/Codex/EnglishEditions/QuantumEcho-EN/`

---

## 1. 工程来源

| 项目 | 内容 |
|---|---|
| 克隆来源 | `/Users/ld/Documents/Codex/2026-08-11/unity-pico-xr-pico/解密1/` |
| 选源理由 | 解密1 有 git 历史（基线 commit a9cd9bf5）；APK SHA-256 与交接文档 §7 一致（9caa6451…）；BuildStaging/QuantumEchoV16ASCII 内容相同但无 git 记录，选有历史的版本作权威来源 |
| 克隆方式 | `cp -cR`（APFS copy-on-write，不复制 Library/Temp/Logs/obj/Builds/build/.git） |
| 英文工程 git | 独立 init，基线 commit：d929908（713 文件，2026-10-22） |

---

## 2. 改动清单

### 2.1 C# 运行时脚本（6 个文件）

| 文件 | 修改内容 |
|---|---|
| `Assets/QuantumEcho/Scripts/Runtime/QuantumComfortObjectiveHUD.cs` | 5 处：倒计时初值/模板、帮助按键提示、操控说明全文、目标索引模板 |
| `Assets/QuantumEcho/Scripts/Runtime/QuantumHUD.cs` | 8 处：提示键、提示文字、进度标签、回路计数×2、回声序列、括号格式化方法、结算统计+重试 |
| `Assets/QuantumEcho/Scripts/Runtime/QuantumLevelTransitionHUD.cs` | 2 处：关卡完成标题、进入下一关提示 |
| `Assets/QuantumEcho/Scripts/Runtime/QuantumEchoGameManager.cs` | 54 处：全部 toast、定时器标签、控制器 key 常量、各关目标元组、结算标题、进度/提示标签 |
| `Assets/QuantumEcho/Scripts/Editor/QuantumEchoSceneBuilder.cs` | 33 处：productName、包名、关卡标题、世界标签、HUD 默认值、验证列表 |
| `Assets/QuantumEcho/Scripts/Editor/QuantumEchoTitleSceneBuilder.cs` | 1 处：标题场景菜单提示文字 |

### 2.2 编辑器工具脚本（修正字符串比对以匹配英文标签）

| 文件 | 修改内容 |
|---|---|
| `Assets/QuantumEcho/Scripts/Editor/QuantumEchoScenePolisher.cs` | `StartsWith("失落数据")` → `StartsWith("Lost Data")`；品牌文本 |
| `Assets/QuantumEcho/Scripts/Editor/QuantumEchoScienceFictionModelReplacer.cs` | `StartsWith("失落数据")` → `StartsWith("Lost Data")` |
| `Assets/QuantumEcho/Scripts/Editor/QuantumEchoCollisionQA.cs` | 5 处 QA 测试字符串翻译为英文 |

### 2.3 场景 YAML

| 文件 | 修改内容 |
|---|---|
| `Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity` | 35 处 `m_text` 字段 + 4 处 `title` 字段 + 23 处 `m_Name` 字段（世界标签对象名）|
| `Assets/QuantumEcho/Scenes/QuantumEcho_Title.unity` | 1 处 `m_text` 字段（菜单提示） |

### 2.4 项目设置

| 文件 | 修改内容 |
|---|---|
| `ProjectSettings/ProjectSettings.asset` | `productName: "QUANTUM ECHO"`、`Android: com.quantumecho.decrypt1.en`、Metro 字段（不影响 Android，但清 CJK 扫描） |

### 2.5 图像资源

| 文件 | 说明 |
|---|---|
| `Assets/QuantumEcho/UI/Generated/CoverV6Clean/01_generated_image_url.png` | **无需重做**：原版封面已完全英文（"QUANTUM ECHO" + "REWRITE THE MOMENT"），banana_pro 生成（task_id: e7629381-ec65-459a-b8bd-cd780ac23125） |
| `Assets/AppIcon/AppIcon.png` | **新建**：原版 AppIcon.png 已缺失（仅有 .meta）；从英文封面图裁剪中心 1:1 区域，resize 到 1024×1024，使用 Pillow；无需 Tripo 调用 |

---

## 3. 字体与许可

| 原字体 | 英文字体 | 选型理由 | 许可证 | OFL 文件 |
|---|---|---|---|---|
| `NotoSansSC-Variable.ttf` | 同文件（NotoSansSC 含高质量 Latin subset） | NotoSansSC 与 Noto Sans Latin 同一设计家族，几何无衬线，与原版科技风格完全一致；英文版不加载 CJK 字符集（Dynamic TMP 模式按需加载） | SIL OFL 1.1 | `Assets/QuantumEcho/Fonts/OFL.txt` |

**判断理由**：规范要求"同风格英文字体"，NotoSansSC 的 Latin glyphs 与 Noto Sans 共享相同设计 token（Weight/Width/Optical Size），无需另下 Exo 2/Orbitron，避免额外字体导入和 TMP 资产重建。

---

## 4. Tripo 调用清单

| task_id | 模型 | 用途 | 状态 |
|---|---|---|---|
| e7629381-ec65-459a-b8bd-cd780ac23125 | banana_pro | 封面主视觉（原版已生成，本次复用） | 复用，无新调用 |

**本次英文版制作 Tripo 新调用次数：0**  
原因：封面已完全英文，AppIcon 从封面裁剪，无带字图片需要重生成。

---

## 5. CJK 扫描结果

扫描范围：`.cs` / `.unity` / `.prefab` / `.asset` / `.shader`（排除 Library/Temp/Logs/obj/Builds/build）  
排除注释行（`//`、`*`、`/*` 开头）

| 结果 | 数量 | 说明 |
|---|---|---|
| 玩家可见 CJK 字符 | **0** | 全部替换完成 |
| BOM 误报（排除） | 4 行 | TMP 标准 shader 文件首行 UTF-8 BOM（U+FEFF），非 CJK 字符，不影响玩家画面 |
| 代码注释中文（排除） | 保留 | 注释不渲染到屏幕；规范允许注释保留中文 |
| PRODUCT.md / DESIGN.md 中文（排除） | 大量 | 内部开发文档，非玩家可见资产 |

---

## 6. 特别设计决策说明

| 议题 | 决策 | 理由 |
|---|---|---|
| 最后一关 Decoy/Hunter 无碰撞 | **不补回** | 交接文档 §7 明确"有意设计，不要恢复 Collider" |
| 封面 4.8×2.7 尺寸和 20% UV 羽化 | 保留原始资产，未修改 | 封面已是英文，无需重生成 |
| 任务面板左上锚点、只向下展开 | 保留原始实现 | QuantumComfortObjectiveHUD.SetHelpExpanded 逻辑未变，只替换文字 |
| NotoSansSC 复用不另下字体 | 保留 | Latin 子集完全满足英文排版，OFL 许可允许商用 |
| m_Name 对象名（世界标签）翻译 | **翻译了** | 虽属内部名称但会出现在运行时调试面板；翻译后不影响功能且 CJK 扫描更干净 |
| 编辑器脚本字符串比对 | 同步翻译为英文 | ScenePolisher 和 ModelReplacer 用 `StartsWith("失落数据")` 匹配标签，翻译后必须同步否则运行时逻辑失效 |

---

## 7. APK 构建

| 项目 | 内容 |
|---|---|
| 构建方法 | `QuantumEchoENBuilder.CaptureAndBuild`（截图 + 构建合并入口） |
| 目标路径 | `Builds/EN/QuantumEcho-EN-PICO.apk` |
| Unity | 6000.5.5f1 batchmode (含图形上下文，支持 RenderTexture 截图) |
| 状态 | **已完成**（日志：`Builds/EN/build4.log`） |
| APK SHA-256 | `5865f1e9dda745a84f97f5ef7d4b33141d35a69c8ddd72486d286dda1dab5c8b` |
| APK 大小 | 76,890,063 字节（73.3 MB） |

### 返工内容（总控核查后修订，2026-10-22）

| 类别 | 修改 |
|---|---|
| 封存动词 | "commit" → "seal" 全游戏统一（Echo committed/sealed、Commit/Seal Echo、Commit/Seal this action、Holder commit → seal、Decoy commit → seal） |
| 保持器提示 | "Activate the Holder" → "Stand on the Holder" |
| 手柄提示 | "[L-Stick] Controls/Hide Controls" → "[Press L-Stick] Controls/Hide Controls" |
| 目标索引 | "Obj. {n}" → "Objective {n}" |
| 倒计时 | "Echo End {x}s" → "Echo ends in {x}s" |
| 诱饵区标签 | "Decoy Residue" → "Decoy Zone"（世界标签、代码、YAML 全部同步） |
| Hunter toast | "You are locked by a Hunter..." → "A Hunter has locked onto you..." |
| 换轨 toast | "Switch beat hit — Phase Train heading to core dock" → "Switch beat on time — the Phase Train is heading to the Core dock" |
| 核心吸取提示 | "Hold — Core {pct}% extracted" → "Extracting Core… {pct}%" |
| Lab 世界标签 | "Lab {n} // Edit scene models here" → "Lab {n}"（移除开发者注释） |
| [Left Menu] | 全游戏改为 [Menu] |
| Hunter Matrix 标签 | "Record two opposing Decoy routes" → "Record two opposing Decoy Zone routes" |
| Scene YAML 同步 | QuantumEcho_Main.unity 中 m_text 同步更新（Lab 标签、Commit→Seal、Hunter Matrix） |

---

## 8. 截图（视觉 QA）

目录：`VisualQA/EN-20261022/`

截图由 `QuantumEchoScreenshots.CaptureAll` 通过 `RenderTexture` 在 Editor batchmode 中渲染生成，无需 XR 头显。

| 文件名 | 内容 | 状态 |
|---|---|---|
| 01_cover_QUANTUM_ECHO.png | 英文封面主视觉 | 现有 |
| 00_title_scene.png | 标题场景（EXIT / START GAME / [Right Trigger] Select & Confirm） | 已生成 |
| 01_main_default.png | 主场景默认视角（第1关、全量 HUD 元素） | 已生成 |
| 02_level0_tutorial_overview.png | 第1关全景（教程 Holder 区域） | 已生成 |
| 03_level1_paired_overview.png | 第2关全景（Stabilizer A/B 区域） | 已生成 |
| 04_level2_train_overview.png | 第3关全景（Phase Train 区域） | 已生成 |
| 05_level3_lure_overview.png | 第4关全景（Decoy Zone 区域） | 已生成 |

**TMP 文本核查（来自截图日志，编辑器 batchmode）：**
- 标题场景：`[Right Trigger] Select & Confirm` ✓，`START GAME` ✓，`EXIT` ✓
- 主场景关键标签：`Decoy Zone 1/2` ✓，`Holder // Stay on It` ✓，`A // Launch Junction` ✓，`B // Switch Junction` ✓，`A // Stabilizer` ✓，`B // Stabilizer` ✓，`Phase Train // Standby` ✓，`Hunter Matrix // Record two opposing Decoy Zone routes` ✓（返工后）
- HUD 状态：`Loop  01` ✓，`Echoes  0 / 4` ✓，`Tutorial Loop` ✓，`Q-RELAY  /  QUANTUM ECHO` ✓
- 通关标题（场景默认）：`Core Recovery Complete` ✓
- 目标标题（场景默认）：`Stand on the Holder` ✓（返工后）
- Lab 标签（返工后）：`Lab 00~03`（无开发者注释）✓

**注意**：截图由 Editor 静态渲染（非 Play 模式），HUD 运行时覆盖值（如 Seal this action、Objective {n}、Echo ends in）由代码在 `Awake()/Start()` 动态设置，不体现在此静态截图中；代码逻辑已验证。

---

## 9. 未验证边界

1. **实体 PICO 头显**：所有字体大小、行距、HUD 位置仅在 macOS 桌面模式验证，实机舒适度待头显测试
2. **模拟器安装**：构建完成后未安装至模拟器（等待 PICO 模拟器锁）
3. **AppIcon 圆角裁剪**：PICO 系统对图标做圆角裁剪，未确认构图在圆形遮罩内效果（需实机查看）
4. **TMP Dynamic Atlas 英文字符集**：未用 Unity 强制生成 SDF Atlas，仅依赖运行时动态加载；首次运行可能有微小闪烁（原版同此行为）
5. **APK 签名**：沿用原工程 Keystore 配置，未单独验证签名证书链

---

## 10. 翻译把握度说明

以下翻译做了主观判断，如有疑义请确认：

| 原文 | 英文 | 备注 |
|---|---|---|
| 诱饵残响 | Decoy Zone | 改为 Decoy Zone，与关卡术语统一（区域性概念，"残响"=余波，英文 Zone 更直白） |
| 换轨节拍 | Switch Beat | "节拍"=beat，与第三关铁路节拍机制对应 |
| 发车枢纽 | Launch Junction | "枢纽"=junction（转辙器/扳道叉），英文铁路术语 |
| 踩住保持器 | Stand on the Holder | 改为"Stand on"，比"Activate"更直接描述物理动作 |
| 封存当前动作并开始下一回路 | Seal this action and start the next loop | "封存"→"Seal"统一全游戏术语，比 Commit 更符合时间回声游戏的密封/固化含义 |
