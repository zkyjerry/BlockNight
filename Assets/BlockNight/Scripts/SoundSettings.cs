using UnityEngine;
namespace BlockNight {
 [CreateAssetMenu(menuName="Block Night/音乐音效配置")]
 public class SoundSettings:ScriptableObject {
  [System.Serializable] public class Track {
   [Tooltip("场景名，与 Build Settings 一致")] public string scene;
   public AudioClip clip;
   [Range(0,1)] public float volume=.7f;
   public bool loop=true;
  }
  [Header("每个场景的背景音乐")]
  public Track[] tracks;
  [Header("总音量")]
  [Range(0,1)] public float musicVolume=1f;
  [Range(0,1)] public float effectsVolume=1f;
  [Header("转场淡出淡入秒数；实际时长不超过遮盖/揭开动画")]
  [Min(0)] public float fadeSeconds=.8f;
  [Header("音效音量：0 开局 1 走格 2 击杀 3 连斩破盾 4 状态 5 时间折叠 6 死亡 7 节拍")]
  [Range(0,1)] public float[] cueVolumes={.7f,.7f,.55f,.7f,.7f,.7f,.7f,.35f};
  [Tooltip("场景没有配置 BGM 时，回落到内置合成节拍层")]
  public bool proceduralBeats=true;
  public Track TrackFor(string scene){
   if(tracks!=null)foreach(var t in tracks)if(t!=null&&t.scene==scene)return t;
   return null;
  }
  public float CueVolume(int id)=>(cueVolumes!=null&&id>=0&&id<cueVolumes.Length?cueVolumes[id]:.7f)*effectsVolume;
 }
}
