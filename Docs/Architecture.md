# 方块战士架构 · v7

当前规则见 [游戏设计](游戏设计.md)，编辑入口见 [使用与微调](使用与微调.md)。旧版架构存于 `Backups/legacy-docs/Architecture.md`。

| 模块 | 职责与边界 |
|---|---|
| CombatModel / Frame / Foe / Shot / Pickup | 独立战斗数据、逐格移动、敌人与道具刷新、时间技能缓存、一次性护盾、积分与强化、确定性随机、快照捕获及恢复；不依赖场景对象 |
| Balance / SpawnSchedule / SpawnPhase / EnemyDefinition | 基础数值、道具概率、分阶段刷怪及每类敌人属性 SO；中文 Inspector |
| GameDirector | 输入到模型调用、状态机、20 Hz 历史、七步教学、串联表现和 UI |
| ArenaPresentation / EnemyView / PickupView | 根据模型 Instantiate/销毁敌人与道具 Prefab，更新玩家 Prefab、Sprite、LineRenderer、Particle System、Light2D；驱动 DOTween 摄像机及 Volume，不能修改战斗奖励 |
| GameHUD | TMP 动态数字、教程与卡片、按钮绑定；不计算规则 |
| SceneFlow / MenuController | 三场景路由与本局成绩传递；主菜单/失败页独立，重开新建模型，不保留旧历史 |
| SynthAudio / ButtonFeel | 合成音效与节奏、跨场景静音；按钮悬浮和点击反馈 |
| Editor 下迁移及 QA 工具 | 场景持久化、旧版本迁移、逻辑/运行检查、固定截图；不参与发行版运行 |

棋盘 64 格、格子攻击 80 个槽及玩家 Prefab 实例保存在场景中。四类敌人与两类道具分别位于 `Prefabs/Enemies`、`Prefabs/Items`，运行时由 `ArenaPresentation` 按模型槽位 Instantiate；旧 36 敌人表现池仅为迁移遗留且已停用。逻辑位置采用格坐标，表现统一乘 1.375。

快照复制敌人、道具、格子攻击和技能缓存数组，以及游戏时间、刷新计时、阶段索引、奖励、积分、连斩及随机种子。回溯完成后消耗一枚回溯缓存；强化层数独立于快照，选牌清空历史。历史每 0.05 秒一帧，基础记录 3 秒，强化可延长。恢复期间重新渲染 Prefab 实例，并清除前向粒子和相机位移。

正常战斗由 DOTween 震动；濒死与恢复阶段由 CombatFeedback 驱动专用 Cinemachine 虚拟相机，通过 Brain.ManualUpdate 输出，恢复结束后停用 Brain，避免双重写入。所有文字统一 TMP_Text / TextMeshProUGUI。UI 保持用户布局；不使用运行时场景生成器。

护盾持有量使用 levels[9] 的 0/1 表示，消耗即归零；仅冲刺阶段激活、全方向判定，无自动重充。持有量不进入回溯快照，避免恢复已经消耗的盾。圆环 LineRenderer 在场景中预置，共 48 个闭合顶点。

菜单已停用 TextAnimator：场景移除了组件，MenuController 不再调用动画 API；保留插件与历史迁移工具供人工参考。历史 Apply v4 不应再次运行，否则会重挂动画。

CombatFeedback 管理镜头接管、拾取闪光、世界计时环和冲击波终点粒子；FeedbackSettings SO 提供实验开关及视听数值。拾取事件只负责音效和闪光，库存及消耗规则仍在模型层。三枚缓存按拾取顺序显示在玩家 Prefab 子节点 `Charge mark 0..2`，青色表示暂停、紫色表示回溯；HUD 的旧技能 CD Bar 已停用。
