using UnityEngine;
namespace BlockNight {
[CreateAssetMenu(menuName="Block Night/Balance")]
public class Balance:ScriptableObject {
 public float boardHalf=5,playerSpeed=13,slowRate=.18f,slowSeconds=2.6f,slowCooldown=11,rewindSeconds=3,rewindCooldown=16,upgradeSeconds=30,killSeconds=1.2f,spawnWarning=1.2f;
}
}
