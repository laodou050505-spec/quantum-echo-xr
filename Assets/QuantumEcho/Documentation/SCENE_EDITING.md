# Scene 编辑说明

主场景：`Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity`

## 场景排布

四个关卡都保存在同一个 Scene，编辑状态默认全部可见：

- `LEVEL_00_Tutorial - EDITABLE`：X = 0
- `LEVEL_01_Paired - EDITABLE`：X = 22
- `LEVEL_02_Train - EDITABLE`：X = 44
- `LEVEL_03_Lure - EDITABLE`：X = 66

运行游戏后，`QuantumEchoGameManager` 只激活当前关卡；这不会删除或修改其他关卡对象。

## 修改方式

1. 在 Hierarchy 展开 `QUANTUM ECHO - EDITABLE SCENE/LEVELS - move and replace models here`。
2. 选中任意 `LEVEL_XX_... - EDITABLE`，按 `F` 聚焦该房间。
3. 环境模型位于 `ENVIRONMENT - walls floor lights`。
4. 机关和玩法模型位于 `PUZZLE OBJECTS - move these freely`。
5. 文本提示位于 `WORLD LABELS`，可以整体隐藏。
6. 替换模型时保留最外层玩法对象及其脚本引用，只替换其子模型最稳妥。

## 不应随意删除的对象

- 每关根对象上的 `QuantumLevelScene` 组件。
- `PLAYER SPAWN`。
- 能量门的 `Gate Left Panel` 与 `Gate Right Panel` Transform。
- 核心和碎片根对象上的 `QuantumSceneTarget` 组件。
- `GAMEPLAY - Quantum Echo Manager`、`PLAYER RIG`、`HUD`、`EventSystem`。

若必须更换这些根对象，请同步更新 `QuantumLevelScene` 或 `QuantumEchoGameManager` Inspector 中的引用。

