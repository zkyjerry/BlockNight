# 方块战士架构 · v6

当前规则见 [游戏设计](游戏设计.md)，编辑入口见 [使用与微调](使用与微调.md)。旧版架构存于 `Backups/legacy-docs/Architecture.md`。

| 模块 | 职责与边界 |
|---|---|
| CombatModel / Frame / Foe / Shot | 独立战斗数据、逐格移动、排队转向、逐格撞击与斜向格子冲击波、一次性护盾、积分与强化、确定性随机、快照捕获及恢复；不依赖场景对象 |
| Balance / SpawnSchedule / SpawnPhase / EnemyDefinition | 基础数值、分阶段刷怪及每类敌人属性 SO；中文 Inspector |
| GameDirector | 输入到模型调用、状态机、20 Hz 历史、七步教学、串联表现和 UI |
| ArenaPresentation | 将模型映射为场景 Sprite、LineRenderer、Particle System、Light2D；驱动 DOTween 摄像机及 Volume，不能修改战斗奖励 |
| GameHUD | TMP 动态数字、教程与卡片、按钮绑定；不计算规则 |
| SceneFlow / MenuController | 三场景路由与本局成绩传递；主菜单/失败页独立，重开新建模型，不保留旧历史 |
| SynthAudio / ButtonFeel | 合成音效与节奏、跨场景静音；按钮悬浮和点击反馈 |
| Editor 下迁移及 QA 工具 | 场景持久化、旧版本迁移、逻辑/运行检查、固定截图；不参与发行版运行 |

敌人 36、格子攻击 80、棋盘 64 格均在场景中预置。逻辑位置采用格坐标，表现统一乘 1.375。移动状态包括当前格、目标格、插值、朝向及排队方向，确保转弯和倒带不破坏网格。

快照复制实体和格子攻击数组、游戏时间、刷怪计时与阶段索引、奖励、积分、连斩及随机种子。技能 CD、强化层数独立于快照；选牌清空历史。历史默认最多 61 帧，每 0.05 秒一帧。恢复期间重新渲染世界，清除前向粒子和相机位移。

正常战斗由 DOTween 震动；濒死与恢复阶段由 CombatFeedback 驱动专用 Cinemachine 虚拟相机，通过 Brain.ManualUpdate 输出，恢复结束后停用 Brain，避免双重写入。所有文字统一 TMP_Text / TextMeshProUGUI。UI 保持用户布局；不使用运行时场景生成器。

护盾持有量使用 levels[9] 的 0/1 表示，消耗即归零；仅冲刺阶段激活、全方向判定，无自动重充。持有量不进入回溯快照，避免恢复已经消耗的盾。圆环 LineRenderer 在场景中预置，共 48 个闭合顶点。

菜单已停用 TextAnimator：场景移除了组件，MenuController 不再调用动画 API；保留插件与历史迁移工具供人工参考。历史 Apply v4 不应再次运行，否则会重挂动画。

CombatFeedback 管理镜头接管、技能就绪闪光、世界计时环和冲击波终点粒子；FeedbackSettings SO 提供实验开关及视听数值。CombatModel 扩散阶段使用释放时捕获的半径和时长，调用统一 KillEnemy 路径计分；UpgradeWaveEnded 事件仅负责表现，表现层不决定命中。波状态纳入 Frame，粒子在回溯开始清除。原有 42 个战斗 UI RectTransform 保留，仅新增闪光 Canvas 与 Image 两个矩形。
