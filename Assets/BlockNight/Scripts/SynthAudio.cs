using UnityEngine;
namespace BlockNight {
public class SynthAudio:MonoBehaviour {
 public AudioSource music,effects;AudioClip[] cues;float beat;int step;static bool muted;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetMute(){muted=false;}
 void Awake(){music.mute=effects.mute=muted;cues=new AudioClip[8];for(int i=0;i<cues.Length;i++)cues[i]=Tone(i);}
 AudioClip Tone(int kind){int rate=22050;float duration=kind==5?.85f:kind==6?.4f:.16f;int n=(int)(rate*duration);float[] data=new float[n];uint noise=13579;for(int i=0;i<n;i++){float t=i/(float)rate;noise=noise*1664525+1013904223;float hiss=((noise&65535)/32768f-1);float f=kind==5?Mathf.Lerp(1100,90,t/duration):kind==6?70:kind==7?65:220+kind*110;float env=Mathf.Min(1,t/.005f)*Mathf.Pow(1-t/duration,2);data[i]=Mathf.Clamp((Mathf.Sin(2*Mathf.PI*f*t)*.55f+(kind==2?hiss*.45f:0))*env*.35f,-1,1);}var clip=AudioClip.Create("BN_Synth_"+kind,n,1,rate,false);clip.SetData(data,0);return clip;}
 public void Cue(int id,int chain=1){effects.pitch=id==5?1:Mathf.Min(1.65f,1+chain*.025f);effects.PlayOneShot(cues[id],id==2?.55f:.7f);}
 public void ToggleMute(){muted=!muted;music.mute=effects.mute=muted;}
 public void Tick(float elapsed,float rate,bool active,float dt){music.pitch=rate<1?.65f:1;if(!active)return;beat-=dt;if(beat<=0){beat=60/Mathf.Lerp(92,142,Mathf.Clamp01(elapsed/180))/2;step++;music.pitch*=new[]{1f,1.1892f,1.4983f,1.3348f}[step/4%4];music.PlayOneShot(cues[7],.35f);if(elapsed>25&&step%2==0)music.PlayOneShot(cues[1],.06f);if(elapsed>65&&step%4==0)music.PlayOneShot(cues[3],.09f);}}
 void OnDestroy(){if(cues==null)return;foreach(var c in cues)if(c)Destroy(c);}
}
}
