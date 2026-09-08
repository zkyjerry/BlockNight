# BLOCK NIGHT · 方块战士

一个五分钟的 2D 时间战斗 Demo。以一冲到底的路线选择、慢时蓄积斩杀和完整场景回溯为核心。

## 开始

Unity 2022.3.62f3c1，URP 2D。打开 `Assets/Scenes/BlockNight.unity`，按 Play，然后 Enter。DOTween 与 Input System 使用项目现有依赖。

| 操作 | 功能 |
|---|---|
| WASD / 方向键 | 冲刺到对应边界，击杀路径敌人 |
| Shift + WASD | 沿边挪一格，选择新的贯穿路线 |
| Space | 慢时，结束时统一释放斩杀 |
| Q | 回溯；死亡后也可用 |
| 1 / 2 / 3 或鼠标 | 三选一强化 |
| Esc / M / Enter | 暂停 / 静音 / 开始或重开 |

## 可反复查阅的文档

- [设计与数值](Docs/GameDesign.md)：完整玩法、技能、敌人、强化、压力曲线与视听语言。
- [验收与迭代](Docs/Acceptance.md)：标准、自动测试、运行证据，以及仍需真人试玩的项目。
- [程序架构](Docs/Architecture.md)：职责边界、时间语义、扩展和调试。
- [规则测试结果](Docs/Evidence/model-tests.txt)。

## 编辑器菜单

`Block Night/Run Acceptance Tests` 运行规则回归；`Block Night/QA` 提供可复现展示场景与截图；`Block Night/Build macOS Demo` 导出到 `Builds/BlockNight.app`。QA 场景含保护条件，不能作为正常通关成绩。

原有 `SampleScene` 保留；Demo 使用独立的 `BlockNight` 场景。所有美术形状与音轨由代码生成，不含外部下载素材。
