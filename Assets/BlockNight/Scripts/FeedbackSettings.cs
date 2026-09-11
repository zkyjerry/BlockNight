using UnityEngine;
namespace BlockNight {
 [CreateAssetMenu(menuName="Block Night/实验反馈配置")]
 public class FeedbackSettings:ScriptableObject {
  [Header("实验：选择强化后释放范围冲击波")]
  public bool upgradeShockwave=true;
  [Min(.1f)] public float shockwaveSeconds=.65f;
  [Tooltip("最大半径，以棋盘格为单位；圆形范围，包含边界。")][Min(.1f)] public float shockwaveRadius=4f;
  [Header("濒死反馈")]
  [Min(.2f)] public float dyingSeconds=2f;
  [Range(.2f,.9f)] public float dyingVignette=.62f;
  public Color dyingColor=new Color(.23f,.005f,.018f);
  [Min(1)] public float focusSize=4.6f;
  [Min(.05f)] public float focusSeconds=.25f, restoreSeconds=.35f;
  [Header("错过回溯后的玩家破碎")]
  [Tooltip("镜头归位并播放破碎粒子的时长，结束后才切到结算场景。")][Min(.2f)] public float shatterSeconds=1.7f;
  [Range(120,2000)] public int shatterParticles=760;
  [Header("技能就绪闪光")]
  [Range(.01f,.5f)] public float readyFlashAlpha=.14f;
  [Min(.1f)] public float readyFlashSeconds=.42f;
 }
}
