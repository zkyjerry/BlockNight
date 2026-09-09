using System;
using UnityEngine;

namespace BlockNight
{
    public enum EnemyKind { 突进方卫 = 0, 斜波棱镜 = 1, 巡格猎手 = 2, 逆波脉冲 = 3 }

    [Serializable]
    public class SpawnPhase
    {
        public string phaseName = "新阶段";
        [Min(1), Tooltip("阶段持续的世界时间（秒）；慢时间会延缓世界时间。")]
        public float duration = 30;
        [Min(0), Tooltip("每一批刷新的数量；0 表示休整阶段。")]
        public int spawnCount = 2;
        [Min(.2f), Tooltip("两批刷新之间的世界时间（秒）。")]
        public float spawnInterval = 3;
        [Tooltip("从列表中随机选种类；重复条目可增加该种类权重。")]
        public EnemyKind[] enemyTypes = { EnemyKind.突进方卫 };
    }

    [CreateAssetMenu(menuName = "Block Night/阶段刷新配置", fileName = "SpawnSchedule")]
    public class SpawnSchedule : ScriptableObject
    {
        public SpawnPhase[] phases = { new SpawnPhase() };
        [Tooltip("所有阶段结束后持续重复最后阶段；关闭则不再刷新。")]
        public bool repeatLastPhase = true;
        public static readonly SpawnPhase Fallback = new SpawnPhase();

        public SpawnPhase At(float elapsed, out int index)
        {
            index = -1;
            if (phases == null || phases.Length == 0) return null;
            float end = 0;
            for (int i = 0; i < phases.Length; i++)
            {
                end += Mathf.Max(1, phases[i].duration);
                if (elapsed < end) { index = i; return phases[i]; }
            }
            if (!repeatLastPhase) return null;
            index = phases.Length - 1;
            return phases[index];
        }

        void OnValidate()
        {
            if (phases == null) return;
            foreach (var phase in phases)
            {
                phase.duration = Mathf.Max(1, phase.duration);
                phase.spawnInterval = Mathf.Max(.2f, phase.spawnInterval);
                phase.spawnCount = Mathf.Clamp(phase.spawnCount, 0, CombatModel.Capacity);
            }
        }
    }
}
