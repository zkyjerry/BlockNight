using System.Collections;
using UnityEngine;
namespace BlockNight {
public class SynthAudio:MonoBehaviour {
 public AudioSource music,effects;public SoundSettings settings;AudioClip[] cues;float beat;int step;float trackVolume=1,fade=1;bool beats=true;Coroutine fading;static bool muted;
 public static SynthAudio Current;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetMute(){muted=false;Current=null;}
 void Awake(){Current=this;if(!settings)settings=Resources.Load<SoundSettings>("SoundSettings");music.mute=effects.mute=muted;cues=new AudioClip[11];for(int i=0;i<cues.Length;i++)cues[i]=i==10?Blast():Tone(i);StartTrack();}
 void StartTrack(){beats=!settings||settings.proceduralBeats;trackVolume=settings?settings.musicVolume:1f;fade=1;
  var track=settings?settings.TrackFor(gameObject.scene.name):null;
  if(track!=null&&track.clip){beats=false;trackVolume=track.volume*settings.musicVolume;fade=SceneTransition.IsTransitioning?0:1;music.clip=track.clip;music.loop=track.loop;Apply();music.Play();}
  else Apply();}
 void Apply(){music.volume=trackVolume*fade;}
 /// <summary>转场调用：把当前场景 BGM 淡到 target（0 淡出 / 1 淡入），时长不超过 maxSeconds。</summary>
 public void FadeMusic(float target,float maxSeconds){if(fading!=null)StopCoroutine(fading);float seconds=Mathf.Min(settings?settings.fadeSeconds:.8f,Mathf.Max(0,maxSeconds));if(seconds<=0){fade=Mathf.Clamp01(target);Apply();return;}fading=StartCoroutine(FadeRoutine(Mathf.Clamp01(target),seconds));}
 IEnumerator FadeRoutine(float target,float seconds){float from=fade,elapsed=0;while(elapsed<seconds){elapsed+=Time.unscaledDeltaTime;fade=Mathf.Lerp(from,target,Mathf.Clamp01(elapsed/seconds));Apply();yield return null;}fade=target;Apply();fading=null;}
 AudioClip Tone(int kind){int rate=22050;float duration=kind==5?.85f:kind==6?.4f:kind==8?.075f:kind==9?.2f:.16f;int n=(int)(rate*duration);float[] data=new float[n];uint noise=13579;for(int i=0;i<n;i++){float t=i/(float)rate;noise=noise*1664525+1013904223;float hiss=((noise&65535)/32768f-1);float f=kind==5?Mathf.Lerp(1100,90,t/duration):kind==6?70:kind==7?65:kind==8?1480:kind==9?Mathf.Lerp(520,1240,t/duration):220+kind*110;float env=Mathf.Min(1,t/.005f)*Mathf.Pow(1-t/duration,2);data[i]=Mathf.Clamp((Mathf.Sin(2*Mathf.PI*f*t)*.55f+(kind==2?hiss*.45f:kind==9?hiss*.14f:0))*env*.35f,-1,1);}var clip=AudioClip.Create("BN_Synth_"+kind,n,1,rate,false);clip.SetData(data,0);return clip;}
 public void Cue(int id,int chain=1){effects.pitch=id==5?1:Mathf.Min(1.65f,1+chain*.025f);effects.PlayOneShot(cues[id],settings?settings.CueVolume(id):(id==2?.55f:.7f));}
 // 玩家破碎：低频下坠轰鸣 + 滤波噪声轰体 + 开头的碎裂脆响。
 AudioClip Blast(){int rate=22050;float duration=1.35f;int n=(int)(rate*duration);float[] data=new float[n];uint noise=24681;float rumble=0,debris=0;
  for(int i=0;i<n;i++){float t=i/(float)rate;noise=noise*1664525+1013904223;float white=((noise&65535)/32768f-1);
   rumble=Mathf.Lerp(rumble,white,.11f);debris=Mathf.Lerp(debris,white,.55f);
   float boom=Mathf.Sin(2*Mathf.PI*Mathf.Lerp(155,30,Mathf.Sqrt(t/duration))*t);
   float body=Mathf.Pow(1-t/duration,1.7f);
   data[i]=Mathf.Clamp(((boom*.8f+rumble*1.6f)*body+debris*Mathf.Exp(-t*24)*.75f)*.62f,-1,1);}
  var clip=AudioClip.Create("BN_Synth_10",n,1,rate,false);clip.SetData(data,0);return clip;}
 /// <summary>错过回溯、玩家破碎时的爆炸。</summary>
 public void Explode(){if(cues==null)return;effects.pitch=1;effects.PlayOneShot(cues[10],settings?settings.CueVolume(10):.9f);}
 /// <summary>菜单按钮：悬浮为轻点，点击为上扬确认音。</summary>
 public void Ui(bool click){if(cues==null)return;int id=click?9:8;effects.pitch=click?1f:Random.Range(.97f,1.06f);effects.PlayOneShot(cues[id],settings?settings.CueVolume(id):(click?.5f:.28f));}
 public void ToggleMute(){muted=!muted;music.mute=effects.mute=muted;}
 public void Tick(float elapsed,float rate,bool active,float dt){music.pitch=rate<1?.65f:1;if(!active||!beats)return;beat-=dt;if(beat<=0){beat=60/Mathf.Lerp(92,142,Mathf.Clamp01(elapsed/180))/2;step++;music.pitch*=new[]{1f,1.1892f,1.4983f,1.3348f}[step/4%4];music.PlayOneShot(cues[7],settings?settings.CueVolume(7):.35f);if(elapsed>25&&step%2==0)music.PlayOneShot(cues[1],.06f);if(elapsed>65&&step%4==0)music.PlayOneShot(cues[3],.09f);}}
 void OnDestroy(){if(Current==this)Current=null;if(cues==null)return;foreach(var c in cues)if(c)Destroy(c);}
}
}
