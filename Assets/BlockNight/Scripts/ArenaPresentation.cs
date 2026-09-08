using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Cinemachine;
using DG.Tweening;
namespace BlockNight {
public class ArenaPresentation:MonoBehaviour {
 public Transform player;public TrailRenderer trail;public SpriteRenderer playerSprite;
 public SpriteRenderer[] bodies, warnings, bullets; public LineRenderer[] aims; public Sprite[] shapes;
 public ParticleSystem shards;public Volume volume;public CinemachineImpulseSource impulse;public Light2D playerLight;
 readonly Color[] colors={new Color(1,.22f,.32f),new Color(.68f,.35f,1),new Color(1,.64f,.16f),new Color(1,.25f,.69f)};
 Bloom bloom;ChromaticAberration chroma;ColorAdjustments grade;Vignette vignette;float kick;
 void Awake(){volume.profile.TryGet(out bloom);volume.profile.TryGet(out chroma);volume.profile.TryGet(out grade);volume.profile.TryGet(out vignette);}
 public void ClearEffects(){trail.Clear();shards.Clear();}
 public void Kill(Vector2 p,int n,bool deferred){if(deferred)return;if(!shards.isPlaying)shards.Play();int count=Mathf.Min(72,12+6*n);var e=new ParticleSystem.EmitParams{position=new Vector3(p.x,p.y,-.3f),startColor=Color.Lerp(Color.cyan,Color.white,.3f),startSize=.065f+.007f*Mathf.Min(n,8)};shards.Emit(e,count);kick=Mathf.Min(1,.15f+n*.07f);impulse.GenerateImpulseWithVelocity(Random.insideUnitCircle*Mathf.Min(.22f,.04f+n*.012f));player.DOKill();player.DOScale(.42f,.06f).SetLoops(2,LoopType.Yoyo).OnComplete(()=>player.localScale=Vector3.one*.34f);}
 public void Render(CombatModel m){player.position=new Vector3(m.player.x,m.player.y,-.2f);playerSprite.color=m.grace>0?Color.white:new Color(.25f,1,.89f);if(playerLight)playerLight.transform.position=player.position;trail.emitting=m.direction!=Vector2.zero;
 for(int i=0;i<bodies.Length;i++){var f=m.foes[i];bodies[i].enabled=f.active;warnings[i].enabled=f.active&&!f.pending;aims[i].enabled=false;if(!f.active)continue;
 bodies[i].transform.position=new Vector3(f.pos.x,f.pos.y,-.1f);bodies[i].sprite=shapes[f.type];bodies[i].transform.localScale=Vector3.one*(f.type==3?.42f:.48f);bodies[i].transform.rotation=Quaternion.Euler(0,0,f.pending?35:0);
 Color c=colors[f.type];bool forming=f.age<m.settings.spawnWarning;bodies[i].color=f.pending?new Color(.7f,.8f,1,.38f):forming?new Color(c.r,c.g,c.b,.18f):c*1.7f;
 warnings[i].transform.position=new Vector3(f.pos.x,f.pos.y,0);float size=forming?Mathf.Lerp(1.1f,.6f,f.age/m.settings.spawnWarning):f.timer<.7f?Mathf.Lerp(1.3f,.7f,f.timer/.7f):.64f;warnings[i].transform.localScale=Vector3.one*size;warnings[i].color=new Color(c.r,c.g,c.b,forming?.75f:f.timer<.7f?.9f:.18f);
 if(!forming&&!f.pending&&f.timer<.7f){var l=aims[i];l.enabled=true;l.startColor=l.endColor=new Color(c.r,c.g,c.b,.5f);Vector3 p=new Vector3(f.pos.x,f.pos.y,-.05f);if(f.type==1){l.positionCount=5;l.SetPositions(new[]{p+Vector3.left*1.6f,p+Vector3.right*1.6f,p,p+Vector3.up*1.6f,p+Vector3.down*1.6f});}else {l.positionCount=2;l.SetPosition(0,p);l.SetPosition(1,p+(Vector3)f.aim*2.8f);}}
 }
 for(int i=0;i<bullets.Length;i++){var s=m.shots[i];bullets[i].enabled=s.active;if(s.active){bullets[i].transform.position=new Vector3(s.pos.x,s.pos.y,-.2f);bullets[i].color=new Color(1,.44f,.28f)*2;}}
 }
 public void TimeEffect(bool slow,bool rewind,bool death,float dt){kick=Mathf.MoveTowards(kick,0,dt*3);bloom.intensity.value=1.1f+kick*2+(slow?.5f:0);chroma.intensity.value=Mathf.Lerp(chroma.intensity.value,rewind?.65f:slow?.2f:kick*.2f,dt*12);grade.colorFilter.value=Color.Lerp(grade.colorFilter.value,rewind?new Color(.68f,.43f,1):slow?new Color(.43f,.85f,1):Color.white,dt*8);vignette.intensity.value=death?.5f:rewind?.4f:.25f;}
}
}
