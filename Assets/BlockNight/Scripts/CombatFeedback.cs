using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
namespace BlockNight {
 // Scene-authored feedback objects; model owns all damage and rewards.
 [DefaultExecutionOrder(1000)]
 public class CombatFeedback:MonoBehaviour {
  public FeedbackSettings settings;
  public ArenaPresentation arena;
  public CinemachineBrain brain;
  public CinemachineVirtualCamera focusCamera;
  public Camera outputCamera;
  public LineRenderer countdownTrack,countdownRing,upgradeRing;
  public Image readyFlash;
  public ParticleSystem waveDissolve;
  public int waveDissolveCount;
  Vector3 rest,from,target;float restSize,fromSize,targetSize,cameraClock,cameraDuration;
  bool initialized,cameraActive,focus;
  float flashClock,pulse;Color flashColor;
  public int slowReadyCount,rewindReadyCount;
  public float RemainingFraction {get;private set;}=1;
  void Awake(){Initialize();}
  void Initialize(){if(initialized)return;rest=outputCamera.transform.position;restSize=outputCamera.orthographicSize;initialized=true;}
  public void ResetEffects(){Initialize();focus=false;cameraActive=false;brain.enabled=false;focusCamera.enabled=false;outputCamera.transform.position=rest;outputCamera.orthographicSize=restSize;readyFlash.enabled=false;flashClock=0;pulse=0;upgradeRing.enabled=false;ClearWaveParticles();}
  public void Focus(bool dying,Vector2 player){
   Initialize();if(dying==focus)return;focus=dying;arena.StopShake();
   from=outputCamera.transform.position;fromSize=outputCamera.orthographicSize;target=dying?arena.World(player,rest.z):rest;targetSize=dying?settings.focusSize:restSize;
   cameraClock=0;cameraDuration=dying?settings.focusSeconds:settings.restoreSeconds;cameraActive=true;
   focusCamera.transform.position=from;focusCamera.m_Lens.OrthographicSize=fromSize;focusCamera.enabled=true;brain.enabled=true;
  }
  void LateUpdate(){if(!cameraActive)return;cameraClock+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(cameraClock/Mathf.Max(.01f,cameraDuration));float eased=t*t*(3-2*t);focusCamera.transform.position=Vector3.Lerp(from,target,eased);focusCamera.m_Lens.OrthographicSize=Mathf.Lerp(fromSize,targetSize,eased);brain.ManualUpdate();if(t>=1&&!focus){brain.enabled=false;focusCamera.enabled=false;cameraActive=false;outputCamera.transform.position=rest;outputCamera.orthographicSize=restSize;}}
  public void ClearWaveParticles(){if(waveDissolve)waveDissolve.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
  public void WaveDissolve(Vector2 origin,float radius){
   if(!waveDissolve)return;waveDissolveCount++;upgradeRing.enabled=false;waveDissolve.Play();
   int count=Mathf.Clamp(Mathf.CeilToInt(radius*64),128,768);
   for(int i=0;i<count;i++){float angle=2*Mathf.PI*(i+Random.value*.6f)/count;var outward=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));var tangent=new Vector2(-outward.y,outward.x);waveDissolve.Emit(new ParticleSystem.EmitParams{position=arena.World(origin+outward*radius,-.4f),velocity=outward*Random.Range(.5f,1.8f)+tangent*Random.Range(-.5f,.5f),startColor=Color.Lerp(new Color(.25f,1,.65f),Color.white,Random.value)*2,startSize=Random.Range(.055f,.13f),startLifetime=Random.Range(.35f,.7f)},1);}
  }
  public void Ready(bool rewind){if(rewind)rewindReadyCount++;else slowReadyCount++;flashColor=rewind?new Color(.64f,.22f,1):new Color(.05f,.95f,.82f);flashClock=settings.readyFlashSeconds;readyFlash.enabled=true;}
  public void RewardPulse(){pulse=1;}
  // Counterclockwise from top centre: left, bottom, right, top.
  public static Vector3 EdgePoint(float fraction,float half){float d=Mathf.Clamp01(fraction)*8*half;if(d<=half)return new Vector3(-d,half,-.45f);d-=half;if(d<=2*half)return new Vector3(-half,half-d,-.45f);d-=2*half;if(d<=2*half)return new Vector3(-half+d,-half,-.45f);d-=2*half;if(d<=2*half)return new Vector3(half,-half+d,-.45f);d-=2*half;return new Vector3(half-d,half,-.45f);}
  public void Render(CombatModel m,float dt){
   pulse=Mathf.Max(0,pulse-dt*3);RemainingFraction=Mathf.Clamp01(m.reward/m.settings.upgradeSeconds);
   float elapsed=1-RemainingFraction;int segments=Mathf.Max(1,Mathf.CeilToInt(RemainingFraction*128));countdownRing.enabled=RemainingFraction>0;countdownRing.positionCount=segments+1;
   for(int i=0;i<=segments;i++)countdownRing.SetPosition(i,EdgePoint(Mathf.Lerp(elapsed,1,(float)i/segments),arena.cellSize*4+.16f));
   countdownRing.startWidth=countdownRing.endWidth=.115f+pulse*.075f;countdownRing.startColor=countdownRing.endColor=Color.Lerp(new Color(.1f,1,.72f)*1.5f,Color.white*2,pulse);
   upgradeRing.enabled=m.upgradeWaveActive;if(upgradeRing.enabled){upgradeRing.positionCount=96;upgradeRing.loop=true;for(int i=0;i<96;i++){float angle=2*Mathf.PI*i/96;upgradeRing.SetPosition(i,arena.World(m.upgradeWaveOrigin+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*m.upgradeWaveRadius,-.4f));}upgradeRing.startColor=upgradeRing.endColor=new Color(.6f,1,.75f,Mathf.Lerp(1,.4f,Mathf.Clamp01(m.upgradeWaveRadius/Mathf.Max(.1f,m.upgradeWaveMaxRadius))))*2;}
   if(flashClock>0){flashClock=Mathf.Max(0,flashClock-dt);float u=1-flashClock/settings.readyFlashSeconds;float alpha=Mathf.Sin(Mathf.PI*u)*settings.readyFlashAlpha;readyFlash.color=new Color(flashColor.r,flashColor.g,flashColor.b,alpha);readyFlash.enabled=flashClock>0;}
  }
  void OnDisable(){if(initialized&&outputCamera){brain.enabled=false;focusCamera.enabled=false;outputCamera.transform.position=rest;outputCamera.orthographicSize=restSize;}}
 }
}
