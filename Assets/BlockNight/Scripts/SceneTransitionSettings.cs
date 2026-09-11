using UnityEngine;
namespace BlockNight {
 [CreateAssetMenu(menuName="Block Night/转场配置")]
 public class SceneTransitionSettings:ScriptableObject {
  [System.Serializable] public class Entry {
   [Tooltip("目标场景名，与 Build Settings 一致")] public string scene;
   [Tooltip("进入该场景时 SceneShift 材质的 _MainColor")] public Color color=Color.white;
  }
  [Header("每个目标场景的转场颜色")]
  public Entry[] scenes;
  [Tooltip("场景未在上表中时使用的颜色")] public Color fallback=new Color(.163f,.547f,.484f,0);
  [Header("遮盖与揭开时长（秒，不受时间缩放影响）")]
  [Min(.05f)] public float coverSeconds=1f;
  [Min(.05f)] public float revealSeconds=2f;
  public Color ColorFor(string scene){
   if(scenes!=null)foreach(var e in scenes)if(e!=null&&e.scene==scene)return e.color;
   return fallback;
  }
 }
}
