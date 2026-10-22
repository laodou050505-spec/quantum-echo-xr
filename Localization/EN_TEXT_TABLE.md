# QUANTUM ECHO – 英文版文本翻译表

生成日期：2026-10-22  
英文工程：`/Users/ld/Documents/Codex/EnglishEditions/QuantumEcho-EN/`  
原版工程：`/Users/ld/Documents/Codex/2026-08-11/unity-pico-xr-pico/解密1/`

---

## 术语表

| 中文术语 | 英文术语 | 备注 |
|---|---|---|
| 回声 | Echo | 核心游戏机制 |
| 回路 | Loop | 每次录制周期 |
| 保持器 | Holder | 第一关踩住门开启的平台 |
| 稳定器 | Stabilizer | 第二关 A/B 区同步圆盘 |
| 相位列车 | Phase Train | 第三关列车谜题 |
| 追猎体 | Hunter | 第四关追踪机器人 |
| 诱饵/诱饵信标 | Decoy / Decoy Zone | 引开追猎体的回声目标区 |
| 数据回收 | Data Recovery | 整体最后一关目标 |
| 封存回声 | Seal Echo | 按握把/A/X储存当前回路 |
| 撤销 | Undo | 移除最后一个回声 |
| 重置 | Reset | 重置整局实验 |
| 核心 | Core | 每关末尾目标物体 |
| 力场 | Field | 关卡入口能量门 |
| 发车枢纽 | Launch Junction | 第三关发车触发点 |
| 换轨枢纽 | Switch Junction | 第三关换轨触发点 |
| 失落数据 | Lost Data | 第四关蓝色碎片目标 |
| 量子核心 | Quantum Core | 核心同义词（完整形式） |
| 实验舱 | Lab | 内部关卡编辑标签 |
| 回声序列 | Echoes | HUD 回声计数 |
| 残响区 | Decoy Zone | 诱饵区域 |

---

## 完整文本翻译

| key | 原中文 | 英文 | 来源文件:上下文 | 使用场景 | 状态 |
|---|---|---|---|---|---|
| level_title_0 | 让回声替你守住门 | Let Your Echo Hold the Door | QuantumEchoSceneBuilder.cs:364 / scene title field | 第一关标题（场景序列化 + 编辑器） | 已实施 |
| level_title_1 | 一个人，三个时间切片 | One Self, Three Time Slices | QuantumEchoSceneBuilder.cs:365 / scene title field | 第二关标题 | 已实施 |
| level_title_2 | 给量子列车编一段节拍 | Set the Rhythm for the Phase Train | QuantumEchoSceneBuilder.cs:366 / scene title field | 第三关标题 | 已实施 |
| level_title_3 | 别让追猎体认出你 | Don't Let the Hunters Find You | QuantumEchoSceneBuilder.cs:367 / scene title field | 第四关标题 | 已实施 |
| obj_t0_title | 踩住保持器 | Stand on the Holder | QuantumEchoGameManager.cs:719 | 第一关阶段1目标 | 已实施 |
| obj_t0_detail | 走进蓝色圆盘，门会保持开启；站上去后封存为回声。 | Step onto the blue disc to keep the door open. Then seal it as an Echo. | QuantumEchoGameManager.cs:719 | 第一关阶段1细节 | 已实施 |
| obj_t1_title | 让回声接手 | Let Your Echo Take Over | QuantumEchoGameManager.cs:720 | 第一关阶段2目标 | 已实施 |
| obj_t1_detail | 新回路开始后，回声会走回保持器。等门打开后，自己穿过去。 | When the new loop starts, your Echo returns to the Holder. Wait for the door, then walk through. | QuantumEchoGameManager.cs:720 | 第一关阶段2细节 | 已实施 |
| obj_t2_title | 回收量子核心 | Recover the Quantum Core | QuantumEchoGameManager.cs:721 | 第一关阶段3目标 | 已实施 |
| obj_t2_detail | 穿过已开启的教学力场，靠近并持续吸取核心。 | Walk through the open Tutorial Field and hold to extract the Core. | QuantumEchoGameManager.cs:721 | 第一关阶段3细节 | 已实施 |
| obj_p0_title | 记录左侧稳定器 | Sync Stabilizer A | QuantumEchoGameManager.cs:725 | 第二关阶段1目标 | 已实施 |
| obj_p0_detail | 进入 A 区同步，然后封存当前回路。 | Enter zone A to sync, then seal this loop. | QuantumEchoGameManager.cs:725 | 第二关阶段1细节 | 已实施 |
| obj_p1_title | 记录右侧稳定器 | Sync Stabilizer B | QuantumEchoGameManager.cs:726 | 第二关阶段2目标 | 已实施 |
| obj_p1_detail | 让回声同步 A 区；你在 B 区同步，再封存一次。 | Let your Echo sync zone A while you sync zone B, then seal. | QuantumEchoGameManager.cs:726 | 第二关阶段2细节 | 已实施 |
| obj_p2_title | 取回量子核心 | Recover the Quantum Core | QuantumEchoGameManager.cs:727 | 第二关阶段3目标 | 已实施 |
| obj_p2_detail | 等待两个回声解除力场，靠近并持续吸取核心。 | Wait for both Echoes to lower the field, then hold to extract the Core. | QuantumEchoGameManager.cs:727 | 第二关阶段3细节 | 已实施 |
| obj_tr0_title | 录制发车节拍 | Record the Departure Beat | QuantumEchoGameManager.cs:731 | 第三关阶段1目标 | 已实施 |
| obj_tr0_detail | 到 A 枢纽同步后封存，它会让列车从始发站出发。 | Sync at junction A and seal — this sends the Phase Train from the start. | QuantumEchoGameManager.cs:731 | 第三关阶段1细节 | 已实施 |
| obj_tr1_title | 录制换轨节拍 | Record the Switch Beat | QuantumEchoGameManager.cs:732 | 第三关阶段2目标 | 已实施 |
| obj_tr1_detail | 到 B 枢纽同步后再封存；这条路径要比第一条更晚抵达。 | Sync at junction B and seal. This path must arrive later than the first. | QuantumEchoGameManager.cs:732 | 第三关阶段2细节 | 已实施 |
| obj_tr2_title | 等待列车靠站 | Wait for the Train to Dock | QuantumEchoGameManager.cs:733 | 第三关阶段3目标 | 已实施 |
| obj_tr2_detail | 回声会让列车先发车、再换轨。停靠后靠近并持续吸取核心。 | Your Echoes launch and redirect the train. Once docked, hold to extract the Core. | QuantumEchoGameManager.cs:733 | 第三关阶段3细节 | 已实施 |
| obj_l0_title | 录制左侧诱饵 | Record Left Decoy | QuantumEchoGameManager.cs:737 | 第四关阶段1目标 | 已实施 |
| obj_l0_detail | 跑向左上方紫色残响区并封存，第一台追猎体会认错目标。 | Run to the upper-left Decoy Zone and seal — the first Hunter will follow your Echo. | QuantumEchoGameManager.cs:737 | 第四关阶段1细节 | 已实施 |
| obj_l1_title | 录制右侧诱饵 | Record Right Decoy | QuantumEchoGameManager.cs:738 | 第四关阶段2目标 | 已实施 |
| obj_l1_detail | 下一回路跑向右上方残响区并封存，两台追猎体将分别被带走。 | In the next loop, run to the upper-right Decoy Zone and seal — both Hunters will be drawn away. | QuantumEchoGameManager.cs:738 | 第四关阶段2细节 | 已实施 |
| obj_l2_title | 回收门前数据 | Recover the Lost Data | QuantumEchoGameManager.cs:739 | 第四关阶段3目标 | 已实施 |
| obj_l2_detail | 两台追猎体被引走后，持续吸取门前两枚蓝色碎片。 | With both Hunters lured away, hold to extract the two blue fragments by the gate. | QuantumEchoGameManager.cs:739 | 第四关阶段3细节 | 已实施 |
| result_title_0 | 回声接力完成 | Echo Relay Complete | QuantumEchoGameManager.cs:755 | 第一关通关标题 | 已实施 |
| result_title_1 | 核心回收完成 | Core Recovery Complete | QuantumEchoGameManager.cs:756 | 第二关通关标题 | 已实施 |
| result_title_2 | 相位列车调度完成 | Phase Train Dispatch Complete | QuantumEchoGameManager.cs:757 | 第三关通关标题 | 已实施 |
| result_title_3 | 失落数据已回收 | Lost Data Recovered | QuantumEchoGameManager.cs:758 | 第四关通关标题 | 已实施 |
| hud_loop | 当前回路  {loop:00} | Loop  {loop:00} | QuantumHUD.cs:120,127 | HUD 回路计数 | 已实施 |
| hud_echo_seq | 回声序列  {count} / {maximum} | Echoes  {count} / {maximum} | QuantumHUD.cs:148 | HUD 回声序列 | 已实施 |
| hud_result_stats | 使用回声  {echoes:00}    总用时  mm:ss | Echoes  {echoes:00}    Time  mm:ss | QuantumHUD.cs:217 | 结算统计 | 已实施 |
| hud_result_retry | 按【左菜单键】重试最后一关 | Press [Menu] to retry the final level | QuantumHUD.cs:217 | 最终关结算 | 已实施 |
| hud_result_loading | 正在载入下一关… | Loading next level… | QuantumHUD.cs:217 | 非最终关结算 | 已实施 |
| hud_prompt_key | 【握把 / A / X】 | [Grip / A / X] | QuantumHUD.cs:67 + scene | 默认交互键提示 | 已实施 |
| hud_prompt_commit | 封存当前动作并开始下一回路 | Seal this action and start the next loop | QuantumHUD.cs:75 + scene | 默认提示文字 | 已实施 |
| hud_progress_label | 【右扳机·持续按住】回收 | [R-Trigger · Hold] Extract | QuantumHUD.cs:81 + scene | 进度条标签 | 已实施 |
| hud_obj_index | 目标 {index} | Objective {index} | QuantumComfortObjectiveHUD.cs:232 | 左上面板目标索引 | 已实施 |
| comfort_countdown | 回声结束  0.0s | Echo ends in 0.0s | QuantumComfortObjectiveHUD.cs:86 | 左上面板倒计时初始值 | 已实施 |
| comfort_countdown_tmpl | 回声结束  {remaining:0.0}s | Echo ends in {remaining:0.0}s | QuantumComfortObjectiveHUD.cs:240 | 左上面板倒计时模板 | 已实施 |
| comfort_help_hint | 【左摇杆按下】操作说明 | [Press L-Stick] Controls | QuantumComfortObjectiveHUD.cs:89 | 左上面板帮助提示 | 已实施 |
| comfort_help_collapse | 【左摇杆按下】收起操作说明 | [Press L-Stick] Hide Controls | QuantumComfortObjectiveHUD.cs:129 | 展开状态帮助提示 | 已实施 |
| comfort_controls | 【左摇杆】移动\n【右扳机】交互 / 持续吸取\n【握把 / A / X】封存回声\n【B / Y】撤销   【左菜单键】重置 | [L-Stick] Move\n[R-Trigger] Interact / Hold to Extract\n[Grip / A / X] Seal Echo\n[B / Y] Undo   [Menu] Reset | QuantumComfortObjectiveHUD.cs:91 | 展开后操控说明 | 已实施 |
| transition_complete | 关卡完成 · {title} | Level Complete · {title} | QuantumLevelTransitionHUD.cs:65 | 关卡完成提示 | 已实施 |
| transition_entering | 正在进入第 {n} 关  ·  {s:0.0} 秒 | Entering Level {n}  ·  {s:0.0}s | QuantumLevelTransitionHUD.cs:66 | 进入下一关提示 | 已实施 |
| toast_level_done | 本关已完成，正在进入下一关… | Level complete — loading next level… | QuantumEchoGameManager.cs:102 | Toast | 已实施 |
| toast_all_done | 回声任务已全部完成 | All Echo missions complete! | QuantumEchoGameManager.cs:102 | Toast | 已实施 |
| toast_loop_fail | 回路失稳：当前动作未封存 | Loop unstable — action not sealed | QuantumEchoGameManager.cs:144 | Toast | 已实施 |
| toast_loop_start | 回路已启动：按左上角面板中的手柄键位操作 | Loop started — see the top-left panel for controls | QuantumEchoGameManager.cs:164 | Toast | 已实施 |
| toast_reset | 实验已重置 | Experiment reset | QuantumEchoGameManager.cs:177 | Toast | 已实施 |
| toast_echo_short | 当前回路太短，尚未形成稳定回声 | Loop too short — no stable Echo formed | QuantumEchoGameManager.cs:272 | Toast | 已实施 |
| toast_echo_full | 回声槽已满，请撤销一个回声 | Echo slots full — undo an Echo first | QuantumEchoGameManager.cs:278 | Toast | 已实施 |
| toast_echo_committed | 回声 {n} 已封存 | Echo {n} sealed | QuantumEchoGameManager.cs:288 | Toast | 已实施 |
| toast_echo_none | 没有可以撤销的回声 | No Echo to undo | QuantumEchoGameManager.cs:316 | Toast | 已实施 |
| toast_echo_undone | 最后一个回声已撤销 | Last Echo undone | QuantumEchoGameManager.cs:326 | Toast | 已实施 |
| toast_switch_early | 换轨过早：让第二段路线更晚抵达，撤销后重录 | Switch too early — the second path must arrive later; undo and re-record | QuantumEchoGameManager.cs:445 | Toast | 已实施 |
| toast_switch_hit | 换轨节拍命中：列车驶向核心泊位 | Switch beat on time — the Phase Train is heading to the Core dock | QuantumEchoGameManager.cs:451 | Toast | 已实施 |
| toast_train_docked | 列车已停靠核心泊位 | Phase Train docked at the core | QuantumEchoGameManager.cs:466 | Toast | 已实施 |
| toast_hunter_lured | 追猎体 {n} 已锁定回声诱饵 | Hunter {n} locked onto the Echo Decoy | QuantumEchoGameManager.cs:489 | Toast | 已实施 |
| toast_data_recovered | 失落数据已回收 | Lost Data recovered | QuantumEchoGameManager.cs:519 | Toast | 已实施 |
| toast_locked | 本体被追猎体锁定：先录制诱饵路线 | You are locked by a Hunter — record Decoy routes first | QuantumEchoGameManager.cs:532 | Toast | 已实施 |
| progress_extracting_core | 【右扳机·持续按住】正在回收量子核心 | [R-Trigger · Hold] Extracting Core… | QuantumEchoGameManager.cs:604 | 进度标签 | 已实施 |
| progress_extract_core | 【右扳机·持续按住】回收量子核心 | [R-Trigger · Hold] Extract Core | QuantumEchoGameManager.cs:605 | 进度标签 | 已实施 |
| progress_extract_data | 【右扳机·持续按住】提取失落数据 | [R-Trigger · Hold] Extract Lost Data | QuantumEchoGameManager.cs:512 | 进度标签 | 已实施 |
| timer_tutorial | 教学回路长度 | Tutorial Loop | QuantumEchoGameManager.cs:640 | 计时器标签 | 已实施 |
| timer_train | 本段节拍长度 | Beat Length | QuantumEchoGameManager.cs:641 | 计时器标签 | 已实施 |
| timer_lure | 诱饵回路长度 | Decoy Loop | QuantumEchoGameManager.cs:642 | 计时器标签 | 已实施 |
| timer_default | 回路稳定度 | Loop Stability | QuantumEchoGameManager.cs:643 | 计时器标签 | 已实施 |
| prompt_extracting_core | 持续按住，核心回收 {%}% | Hold — Core {%}% extracted | QuantumEchoGameManager.cs:668 | 提示 | 已实施 |
| prompt_extract_core | 回收量子核心 | Extract the Quantum Core | QuantumEchoGameManager.cs:668 | 提示 | 已实施 |
| prompt_extract_data_pct | 提取失落数据 {%}% | Extracting Lost Data {%}% | QuantumEchoGameManager.cs:680 | 提示 | 已实施 |
| prompt_decoy_routes | 跑入左右紫色残响区，分别封存两条诱饵路线 | Run to both Decoy Residue zones and commit two routes | QuantumEchoGameManager.cs:685 | 提示 | 已实施 |
| prompt_wait_hunters | 保持距离，等待回声引走追猎体 | Keep distance — let the Echoes lure the Hunters away | QuantumEchoGameManager.cs:686 | 提示 | 已实施 |
| prompt_extract_fragments | 靠近门前碎片并持续吸取 | Approach the gate fragments and hold to extract | QuantumEchoGameManager.cs:687 | 提示 | 已实施 |
| prompt_sync_core | 穿过已开启的收容门并同步核心 | Walk through the open gate and sync the Core | QuantumEchoGameManager.cs:688 | 提示 | 已实施 |
| prompt_holder_active | 保持器已踩住：封存这段站位 | Holder activated — commit this position | QuantumEchoGameManager.cs:696 | 提示 | 已实施 |
| prompt_sync_zone | {X} 区同步{中} | Syncing zone {X} | QuantumEchoGameManager.cs:697 | 提示（参数化） | 已实施 |
| prompt_wait_train | 移至安全观察位，等待列车靠站 | Move to a safe spot and wait for the train to dock | QuantumEchoGameManager.cs:702 | 提示 | 已实施 |
| prompt_walk_holder | 走上蓝色保持器 | Step onto the blue Holder | QuantumEchoGameManager.cs:706 | 提示 | 已实施 |
| prompt_walk_stab_a | 前往 A 区稳定器 | Head to Stabilizer A | QuantumEchoGameManager.cs:706 | 提示 | 已实施 |
| prompt_walk_gate | 穿过门，靠近量子核心 | Walk through the gate to the Quantum Core | QuantumEchoGameManager.cs:708 | 提示 | 已实施 |
| prompt_commit_next | 封存当前动作，开始下一回路 | Commit this action and start the next loop | QuantumEchoGameManager.cs:710 | 提示 | 已实施 |
| world_holder | 保持器 // 持续踩住 | Holder // Stay on It | QuantumEchoSceneBuilder.cs:413 + scene | 世界标签 | 已实施 |
| world_stab_a | A // 稳定器 | A // Stabilizer | QuantumEchoSceneBuilder.cs + scene | 世界标签 | 已实施 |
| world_stab_b | B // 稳定器 | B // Stabilizer | QuantumEchoSceneBuilder.cs + scene | 世界标签 | 已实施 |
| world_launch_jct | A // 发车枢纽 | A // Launch Junction | QuantumEchoSceneBuilder.cs + scene | 世界标签 | 已实施 |
| world_switch_jct | B // 换轨枢纽 | B // Switch Junction | QuantumEchoSceneBuilder.cs + scene | 世界标签 | 已实施 |
| world_gate_0 | 教学力场 | Tutorial Field | QuantumEchoSceneBuilder.cs:424 + scene | 关卡0力场标签 | 已实施 |
| world_gate_1 | 双相位力场 | Dual-Phase Field | QuantumEchoSceneBuilder.cs:424 + scene | 关卡1力场标签 | 已实施 |
| world_gate_2 | 量子列车泊位 | Phase Train Dock | QuantumEchoSceneBuilder.cs:424 + scene | 关卡2停靠区标签 | 已实施 |
| world_gate_3 | 数据收容区 | Data Containment Zone | QuantumEchoSceneBuilder.cs:424 + scene | 关卡3门标签 | 已实施 |
| world_train_standby | 相位列车 // 待命 | Phase Train // Standby | QuantumEchoSceneBuilder.cs:549 + scene | 列车世界标签 | 已实施 |
| world_decoy_residue | 诱饵残响 {n} | Decoy Residue {n} | QuantumEchoSceneBuilder.cs:563 + scene | 诱饵区域标签 | 已实施 |
| world_lost_data | 失落数据 {n} // 长按提取 | Lost Data {n} // Hold to Extract | QuantumEchoSceneBuilder.cs:582 + scene | 碎片世界标签 | 已实施 |
| world_hunter_matrix | 追猎矩阵 // 录制两条反向诱饵路线 | Hunter Matrix // Record two opposing Decoy routes | QuantumEchoSceneBuilder.cs:600 + scene | 追猎体区域标签 | 已实施 |
| world_core_label | Q-{n} // 核心 | Q-{n} // Core | QuantumEchoSceneBuilder.cs:429 + scene | 核心世界标签 | 已实施 |
| world_lab_label | 实验舱 {n} // 场景模型可直接编辑 | Lab {n} // Edit scene models here | QuantumEchoSceneBuilder.cs:469 + scene | 编辑器内部标签（玩家可见） | 已实施 |
| brand_text | 量子回声\nQ-RELAY / PICO XR | QUANTUM ECHO\nQ-RELAY / PICO XR | QuantumEchoSceneBuilder.cs:700 + scene | 品牌面板（运行时隐藏，但场景中存在） | 已实施 |
| title_menu_hint | 【右扳机】选择并确认 | [Right Trigger] Select & Confirm | QuantumEchoTitleSceneBuilder.cs + scene | 标题场景操控提示 | 已实施 |
| product_name | 量子回声 | QUANTUM ECHO | ProjectSettings/ProjectSettings.asset | 应用名 | 已实施 |
| bundle_id | com.quantumecho.decrypt1 | com.quantumecho.decrypt1.en | ProjectSettings/ProjectSettings.asset | 包名 | 已实施 |

---

## 字体

| 原字体 | 英文字体 | 理由 | 许可证 | 来源 |
|---|---|---|---|---|
| NotoSansSC-Variable.ttf (几何无衬线，科技风) | Noto Sans (Latin, Variable) | NotoSansSC 的 Latin 子集与 Noto Sans 同一设计家族，几何清洁，适合科技 VR 风格；Dynamic TMP 模式直接使用现有 .ttf | SIL OFL 1.1 | Google Fonts / 工程已有 |

注意：NotoSansSC-Variable.ttf 本身包含高质量 Latin glyphs（与 Noto Sans 共享设计），英文版直接复用该字体。Dynamic TMP 资产在运行时只加载实际使用的字符，不会加载 CJK 字符集。

---

## CJK 扫描范围说明

- 扫描包含：所有 .cs 脚本字符串字面量、scene YAML m_text 字段、ProjectSettings
- 排除：代码注释（中文注释不影响玩家画面）、log 文件、`.meta` 里的 `m_Name`（开发内部名，不显示给玩家）
- 世界标签（Label - XXX）的 m_Name 是开发标识符，但其 m_text 字段已翻译

---

## 动态模板参数对应关系

| 中文模板 | 英文模板 | 参数 |
|---|---|---|
| `当前回路  {loop:00}` | `Loop  {loop:00}` | loop = int |
| `回声序列  {count} / {maximum}` | `Echoes  {count} / {maximum}` | count, maximum = int |
| `使用回声  {echoes:00}    总用时  {min:02}:{sec:02}` | `Echoes  {echoes:00}    Time  {min:02}:{sec:02}` | echoes, min, sec = int |
| `回声 {n} 已封存` | `Echo {n} committed` | n = int |
| `追猎体 {index+1} 已锁定回声诱饵` | `Hunter {index+1} locked onto the Echo Decoy` | index+1 = int |
| `持续按住，核心回收 {pct}%` | `Hold — Core {pct}% extracted` | pct = int |
| `提取失落数据 {pct}%` | `Extracting Lost Data {pct}%` | pct = int |
| `{A/B} 区同步{中/}` | `Syncing zone {A/B}` / `Sync zone {A/B}` | A/B = string |
| `回声结束  {remaining:0.0}s` | `Echo End  {remaining:0.0}s` | remaining = float |
| `关卡完成 · {title}` | `Level Complete · {title}` | title = string |
| `正在进入第 {n} 关  ·  {s:0.0} 秒` | `Entering Level {n}  ·  {s:0.0}s` | n = int, s = float |
| `{level}  ·  目标 {index}` | `{level}  ·  Obj. {index}` | level = string, index = string |
