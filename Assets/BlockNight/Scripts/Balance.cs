using UnityEngine;
namespace BlockNight
{
    [CreateAssetMenu(menuName = "Block Night/Balance")]
    public class Balance : ScriptableObject
    {
        [Header("8×8 格子；速度以格/秒计")]
        public float boardHalf = 3.5f, playerSpeed = 13, walkSpeed = 7;
        [Header("时间技能")]
        public float slowRate = .18f, slowSeconds = 2.6f, slowCooldown = 11, rewindSeconds = 3, rewindCooldown = 16;
        [Header("时间道具")]
        [Range(1,3)] public int maxCharges = 3;
        [Min(.5f)] public float itemInterval = 4.5f;
        [Range(0,1)] public float itemChance = .35f;
        [Range(0,1)] public float rewindPickupShare = .5f;
        [Header("成长与刷新预警")]
        public float upgradeSeconds = 30, killSeconds = 1.2f, spawnWarning = 1.2f;
    }
}
