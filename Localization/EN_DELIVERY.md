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
| 构建方法 | `QuantumEchoENBuilder.BuildENAndroid`（新建脚本，跳过 PXR SDK 依赖验证） |
| 目标路径 | `Builds/EN/QuantumEcho-EN-PICO.apk` |
| Unity | 6000.5.5f1 batchmode (`-nographics -quit`) |
| 状态 | **构建中**（后台进行，日志：`Builds/EN/build.log`） |
| APK SHA-256 | *构建完成后补充* |
| APK 大小 | *构建完成后补充* |

---

## 8. 截图（视觉 QA）

目录：`VisualQA/EN-20261022/`

| 文件名 | 内容 | 状态 |
|---|---|---|
| cover.png | 封面主视觉（英文封面原图） | 现有文件 |
| *(编辑器截图待补充)* | 教程 HUD、帮助展开/收起、结算、诱饵关 | 待 Unity 进程完成后 |

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
| 诱饵残响 | Decoy Residue | "残响"字面为 reverb/echo residue，采用"Residue"作为游戏术语标记区域物理残留 |
| 换轨节拍 | Switch Beat | "节拍"=beat，与第三关铁路节拍机制对应 |
| 发车枢纽 | Launch Junction | "枢纽"=junction（转辙器/扳道叉），英文铁路术语 |
| 保持器已踩住 | Holder activated | 直译"已踩住"比较生硬，采用"activated"更符合游戏反馈惯例 |
| 封存当前动作并开始下一回路 | Commit this action and start the next loop | 两个完整句子，保留"and"连接以匹配原文两段指令结构 |
