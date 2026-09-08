using System.Collections.Generic;
using UnityEngine;
namespace BlockNight {
public enum Mode { Title, Playing, Paused, Draft, Dying, Dead, Rewinding }
public class GameDirector:MonoBehaviour {
 public Balance balance;public ArenaPresentation arena;public GameHUD hud;public SynthAudio audioBus;
 public CombatModel model;public Mode mode;public int tutorialStep=-1;public float danger;
 readonly List<Frame> history=new List<Frame>();float sample,rewindClock;int rewindFrom;int[] offers;bool returnTutorial;
 public void Awake(){Application.runInBackground=true;NewModel();mode=Mode.Title;hud.Bind(this);arena.Render(model);}
 void NewModel(){model=new CombatModel(balance);model.rng=(uint)System.Environment.TickCount|1u;model.Killed+=(p,n,deferred)=>{arena.Kill(p,n,deferred);if(!deferred)audioBus.Cue(2,n);hud.RewardPunch();};model.ChainEnded+=n=>{hud.Banner("连斩 / "+n.ToString("00"));audioBus.Cue(3,n);};history.Clear();sample=0;}
 public void Begin(bool tutorial){NewModel();tutorialStep=tutorial?0:-1;model.tutorial=tutorial;mode=Mode.Playing;arena.ClearEffects();if(tutorial){model.Spawn(0,new Vector2(0,3));}audioBus.Cue(0);}
 public void Home(){NewModel();mode=Mode.Title;tutorialStep=-1;arena.ClearEffects();}
 public void Pause(){if(mode==Mode.Playing){mode=Mode.Paused;}else if(mode==Mode.Paused)mode=Mode.Playing;}
 public void Choose(int index){if(mode!=Mode.Draft)return;model.Choose(offers[index]);history.Clear();sample=0;mode=Mode.Playing;audioBus.Cue(4);}
 public bool Rewind(){if(model.rewindCD>0||history.Count<11)return false;returnTutorial=model.tutorial;rewindFrom=history.Count-1;rewindClock=0;mode=Mode.Rewinding;if(model.slow>0)model.slowCD=Mathf.Max(model.slowCD,balance.slowCooldown-1.5f*model.levels[3]);model.slow=0;audioBus.Cue(5);arena.ClearEffects();return true;}
 void Update(){Tick(Mathf.Min(Time.unscaledDeltaTime,.05f));}
 public void Tick(float dt){if(GameInput.Down(KeyCode.M))audioBus.ToggleMute();if(GameInput.Down(KeyCode.Escape))Pause();
 if(mode==Mode.Rewinding){rewindClock+=dt;int idx=Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(rewindFrom,0,rewindClock/.85f)),0,rewindFrom);model.Restore(history[idx]);if(rewindClock>=.85f){history.Clear();model.Release();model.grace=.55f+.25f*model.levels[8];model.rewindCD=Mathf.Max(6,balance.rewindCooldown-2*model.levels[4]);mode=Mode.Playing;if(returnTutorial&&tutorialStep==4)tutorialStep=5;}}
 else if(mode==Mode.Dying){danger-=dt;if(GameInput.Down(KeyCode.Q)&&Rewind()){}else if(danger<=0){mode=Mode.Dead;audioBus.Cue(6);}}
 else if(mode==Mode.Playing){
 if(GameInput.Down(KeyCode.Q)&&Rewind()){}else{
 Vector2 d=Vector2.zero;if(GameInput.Down(KeyCode.W)||GameInput.Down(KeyCode.UpArrow))d=Vector2.up;if(GameInput.Down(KeyCode.S)||GameInput.Down(KeyCode.DownArrow))d=Vector2.down;if(GameInput.Down(KeyCode.A)||GameInput.Down(KeyCode.LeftArrow))d=Vector2.left;if(GameInput.Down(KeyCode.D)||GameInput.Down(KeyCode.RightArrow))d=Vector2.right;
 if(d!=Vector2.zero){model.Aim(d);audioBus.Cue(1);if(tutorialStep==0)tutorialStep=1;}
 if(GameInput.Down(KeyCode.Space)&&model.Slow()){audioBus.Cue(4);if(tutorialStep==2){tutorialStep=3;model.player=new Vector2(0,-4);model.Aim(Vector2.up);for(int j=0;j<3;j++)model.Spawn(0,new Vector2(0,-2+j*2));for(int j=0;j<model.foes.Length;j++)if(model.foes[j].active)model.foes[j].age=balance.spawnWarning;}}
 model.Step(dt);sample+=dt;if(sample>=.05f){sample-=.05f;history.Add(model.Capture());if(history.Count>Mathf.CeilToInt(balance.rewindSeconds/.05f)+1)history.RemoveAt(0);}
 if(tutorialStep==1&&model.kills>0)tutorialStep=2;
 if(tutorialStep==3&&model.slow<=0)tutorialStep=4;
 if(tutorialStep==5&&GameInput.Down(KeyCode.Return))Begin(false);
 if(model.hit){mode=Mode.Dying;danger=.6f;audioBus.Cue(6);}else if(model.draft){offers=model.Offers();hud.SetOffers(offers,model.levels);mode=Mode.Draft;audioBus.Cue(4);}
 }}else if(mode==Mode.Draft){if(GameInput.Down(KeyCode.Alpha1))Choose(0);if(GameInput.Down(KeyCode.Alpha2))Choose(1);if(GameInput.Down(KeyCode.Alpha3))Choose(2);}
 arena.Render(model);arena.TimeEffect(model.slow>0,mode==Mode.Rewinding,mode==Mode.Dying,dt);hud.Render(this,history.Count>=11);audioBus.Tick(model.elapsed,model.WorldRate,mode==Mode.Playing,dt);
 }
}
}
