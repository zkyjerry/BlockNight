# 架构与维护

## 运行

打开 `Assets/Scenes/BlockNight.unity`，进入 Play，Enter 开始。`Block Night/Create Demo Scene` 可重新创建专用场景；保留原 SampleScene。所有游戏代码位于 `Assets/BlockNight`。场景只保存组合根，几何、UI、音频与后处理由运行时生成，不需要外部美术。

## 模块边界

- `ArenaModel`：纯 C# 状态机与固定步长模拟。拥有 `World`、敌人/弹幕数据、技能成本、强化与历史。依赖 Unity 数学类型，不依赖场景对象；规则可以脱离 Play Mode 测试。
- `Balance`：Inspector 可调基础数值，序列化于组合根。强化元数据由 `Upgrade[]` 定义。
- `BlockNightGame`：组合根、输入、60Hz 累积器、事件分发与局部 hit-stop。每帧最多补四步，暂停不会补算过去时间。
- `ArenaView`：将模型状态投影到几何 Sprite。敌人按 ID 同步，弹幕复用。DOTween 管理一次性碎片、残像和环；回溯清除前向临时效果。
- `ArenaHUD`：Canvas、菜单、三选一、技能状态和操作反馈。通过回调操作模型，无战斗判断。
- `ArenaAudio`：初始化生成并缓存音轨/音效；运行时负责混音，不在音频线程分配对象。
- `Editor/BlockNightTools`：场景创建、可复现 QA 场景及验收测试；UnityEditor 引用不进入运行时代码。

## 时间约定

模拟固定步长 1/60 秒。战斗时间控制局时、技能与成长倒计时；世界时间=战斗时间×慢时倍率。暂停/选牌/死亡冻结；回溯按照未缩放帧时间播放快照。命中冻结仅停模拟，视听 Tween 继续。不得在其他模块随意写 `Time.timeScale`。

快照是深复制；敌人列表/弹幕列表与元素均不得共享引用。历史只保存可撤销状态，技能冷却/强化不在其中。修改 `World` 新增字段时，检查 `Copy()` 与回溯测试；引用类型必须显式复制。回溯终点截断未来历史，选牌提交后清空历史。

## 扩展

新增敌人：扩展 EnemyKind、模拟行为、视觉外形与读招提示，然后加入确定性测试。新增强化：更新元数据/等级数组/选择池/实际数值应用/设计表，验证满级和不足三项的情况。迁移大规模内容时再引入 ScriptableObject 目录，避免 Demo 阶段产生并行配置源。

当前上限 48 敌人、105 快照；状态历史和 ToArray/LINQ 式操作会产生小量 GC，正式移动端版本应在 Profiler 证据支持下改成环形缓冲/对象池。不要据此宣称零 GC 或移动端性能已通过。

## 调试与证据

菜单 `Block Night/Run Acceptance Tests` 运行不依赖场景的规则测试，将结果写入 `Docs/Evidence/model-tests.txt`。Play 中的 `Block Night/QA/Slow Showcase` 创建三连斩慢时场景；`QA/Draft` 展示选牌；`QA/Rewind` 触发真实回溯；`QA/State` 输出当前模型 JSON。这些入口仅在 Editor 内可用。

通过 unity-cli 执行菜单与 Play Mode、截图、Console 读取。验收测试不能代替主观手感试玩。禁止通过 QA 的保护时间或手动改分数来声称正常玩家能够通关。

最终输入使用项目已有 Input System。技能按键用上一帧按下集合检测边沿，兼容物理设备和 unity-cli 的虚拟设备；方向输入采用保持按下。程序允许后台渲染便于调试，但失焦会切换至暂停状态，避免用户切走窗口时死亡。
