using System;
using System.Collections.Generic;
using UnityEngine;
namespace BlockNight {
[Serializable] public struct Foe { public bool active, pending; public int type, killChain; public Vector2 pos, aim; public float age, timer; }
[Serializable] public struct Shot { public bool active; public Vector2 pos, vel; public float life; }
[Serializable] public class Frame {
 public Vector2 player, direction; public Foe[] foes; public Shot[] shots; public float elapsed, spawn, reward; public int score, kills, chain, bestChain; public uint rng;
}
public class CombatModel {
 public const int Capacity=36, Bullets=80;
 public Vector2 player, direction; public Foe[] foes=new Foe[Capacity]; public Shot[] shots=new Shot[Bullets];
 public float elapsed,spawn=1.5f,reward=30,slow,slowCD,rewindCD,grace; public int score,kills,chain,bestChain; public uint rng=7193;
 public bool hit, draft, tutorial; public int[] levels=new int[9]; public Balance settings;
 public event Action<Vector2,int,bool> Killed; public event Action<int> ChainEnded;
 public CombatModel(Balance config) { settings=config; reward=config.upgradeSeconds; }
 public float Speed=>settings.playerSpeed*(1+.12f*levels[0]);
 public float WorldRate=>slow>0?Mathf.Max(.06f,settings.slowRate-.04f*levels[7]):1;
 public float NextRandom(){rng^=rng<<13;rng^=rng>>17;rng^=rng<<5;return (rng&0xffffff)/16777216f;}
 public void Aim(Vector2 d){direction=d;}
 public bool Slow(){if(slow>0||slowCD>0)return false;slow=settings.slowSeconds+.6f*levels[2];return true;}
 public static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b){var v=b-a;return Vector2.Distance(p,a+v*Mathf.Clamp01(Vector2.Dot(p-a,v)/Mathf.Max(.000001f,v.sqrMagnitude)));}
 public void Spawn(int type,Vector2 pos){for(int i=0;i<foes.Length;i++)if(!foes[i].active){foes[i]=new Foe{active=true,type=type,pos=pos,timer=2.4f,aim=Vector2.down};return;}}
 void Fire(Vector2 p,Vector2 d){for(int i=0;i<shots.Length;i++)if(!shots[i].active){shots[i]=new Shot{active=true,pos=p,vel=d*Mathf.Min(4.2f,2+elapsed/120),life=8};return;}}
 public void Release(){for(int i=0;i<foes.Length;i++)if(foes[i].pending){Killed?.Invoke(foes[i].pos,Mathf.Max(1,foes[i].killChain),false);foes[i].pending=false;foes[i].active=false;}}
 public void Step(float dt){
 if(hit||draft)return;
 grace=Mathf.Max(0,grace-dt);slowCD=Mathf.Max(0,slowCD-dt);rewindCD=Mathf.Max(0,rewindCD-dt);
 float rate=WorldRate;
 if(slow>0){slow-=dt;if(slow<=0){slow=0;slowCD=Mathf.Max(4,settings.slowCooldown-1.5f*levels[3]);Release();}}
 float wd=dt*rate;
 if(!tutorial){elapsed+=wd;reward-=dt;spawn-=wd;if(spawn<=0){spawn=Mathf.Max(.65f,2.2f-elapsed/100)*(elapsed%45>38?1.8f:1);var p=new Vector2(NextRandom()*9-4.5f,NextRandom()*9-4.5f);if(Vector2.Distance(p,player)>1.7f)Spawn(Mathf.Min((int)(NextRandom()*(1+Mathf.Min(3,(int)(elapsed/20)))),3),p);}}
 Vector2 old=player;player+=direction*Speed*dt;player=new Vector2(Mathf.Clamp(player.x,-settings.boardHalf,settings.boardHalf),Mathf.Clamp(player.y,-settings.boardHalf,settings.boardHalf));
 for(int i=0;i<foes.Length;i++){
 var f=foes[i];if(!f.active||f.pending)continue;
 f.age+=wd;
 if(direction!=Vector2.zero&&f.age>=settings.spawnWarning&&SegmentDistance(f.pos,old,player)<.42f+.08f*levels[1]){
 chain++;kills++;score+=Mathf.RoundToInt(100*(1+.25f*Mathf.Min(chain-1,12))*(1+.25f*levels[6]));reward-=settings.killSeconds+.35f*levels[5];
 f.killChain=chain;f.pending=slow>0;f.active=f.pending;foes[i]=f;Killed?.Invoke(f.pos,chain,f.pending);continue;
 }
 if(f.age>=settings.spawnWarning&&!tutorial){f.timer-=wd;
 if(f.timer>.7f)f.aim=(player-f.pos).normalized;
 if(f.type==2&&f.timer>.7f)f.pos=Vector2.MoveTowards(f.pos,player,.42f*wd);
 if(f.timer<=0){int count=f.type==1?4:f.type==3?8:1;for(int j=0;j<count;j++){var d=count==1?f.aim:new Vector2(Mathf.Cos(j*Mathf.PI*2/count),Mathf.Sin(j*Mathf.PI*2/count));Fire(f.pos,d);}f.timer=Mathf.Max(1.5f,3.2f-elapsed/160);}
 }foes[i]=f;
 }
 if(direction!=Vector2.zero&&((Mathf.Abs(player.x)>=settings.boardHalf&&direction.x!=0)||(Mathf.Abs(player.y)>=settings.boardHalf&&direction.y!=0))){direction=Vector2.zero;if(chain>0){score+=25*chain*chain;reward-=Mathf.Min(3,.2f*chain);bestChain=Mathf.Max(bestChain,chain);ChainEnded?.Invoke(chain);}chain=0;}
 for(int i=0;i<shots.Length;i++){var s=shots[i];if(!s.active)continue;Vector2 before=s.pos;s.pos+=s.vel*wd;s.life-=wd;if(s.life<=0||Mathf.Abs(s.pos.x)>5.7f||Mathf.Abs(s.pos.y)>5.7f)s.active=false;
 // Relative swept test accounts for both fast player and projectile motion.
 if(s.active&&grace<=0&&SegmentDistance(Vector2.zero,before-old,s.pos-player)<.23f)hit=true;shots[i]=s;}
 if(reward<=0&&!tutorial&&!hit){reward=0;draft=true;}
 }
 public Frame Capture()=>new Frame{player=player,direction=direction,foes=(Foe[])foes.Clone(),shots=(Shot[])shots.Clone(),elapsed=elapsed,spawn=spawn,reward=reward,score=score,kills=kills,chain=chain,bestChain=bestChain,rng=rng};
 public void Restore(Frame f){player=f.player;direction=f.direction;Array.Copy(f.foes,foes,Capacity);Array.Copy(f.shots,shots,Bullets);elapsed=f.elapsed;spawn=f.spawn;reward=f.reward;score=f.score;kills=f.kills;chain=f.chain;bestChain=f.bestChain;rng=f.rng;hit=false;draft=false;}
 public int[] Offers(){var bag=new List<int>();for(int i=0;i<9;i++)if(levels[i]<3)bag.Add(i);var result=new int[3];for(int i=0;i<3;i++){if(bag.Count==0){result[i]=9;continue;}int n=(int)(NextRandom()*bag.Count);result[i]=bag[n];bag.RemoveAt(n);}return result;}
 public void Choose(int id){if(id<9)levels[id]++;else score+=2500;reward=settings.upgradeSeconds;draft=false;}
}
}
