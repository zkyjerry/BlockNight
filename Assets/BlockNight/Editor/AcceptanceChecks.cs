using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace BlockNight.Editor {
public static class AcceptanceChecks {
 static int passed;static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL: "+label);passed++;}
 [MenuItem("Block Night/QA/Run Logic Checks")]
 public static void Run(){passed=0;var b=AssetDatabase.LoadAssetAtPath<Balance>("Assets/BlockNight/Data/Balance.asset");
 var m=new CombatModel(b){tutorial=true};m.Aim(Vector2.right);m.Step(1);Check(m.player==new Vector2(5,0)&&m.direction==Vector2.zero,"boundary stop");
 m=new CombatModel(b){tutorial=true};m.Spawn(0,new Vector2(2,0));m.foes[0].age=2;m.Aim(Vector2.right);m.Step(.3f);Check(m.kills==1&&m.chain==1,"swept kill and chain before wall");m.Step(1);Check(m.chain==0&&m.score==125,"wall settles once");m.Step(1);Check(m.score==125,"no repeat settlement");
 var normal=new CombatModel(b);var slow=new CombatModel(b);normal.Aim(Vector2.up);slow.Aim(Vector2.up);slow.Slow();normal.Step(.1f);slow.Step(.1f);Check(Vector2.Distance(normal.player,slow.player)<.0001f,"slow keeps player speed");Check(Mathf.Abs(slow.elapsed/normal.elapsed-.18f)<.0001f,"world slow ratio");
 m=new CombatModel(b){tutorial=true};m.Spawn(0,new Vector2(2,0));m.foes[0].age=2;m.Slow();int bursts=0;m.Killed+=(p,n,deferred)=>{if(!deferred)bursts++;};m.Aim(Vector2.right);m.Step(.3f);Check(m.foes[0].pending&&m.kills==1&&bursts==0,"deferred kill logic");m.Step(3);Check(!m.foes[0].active&&bursts==1,"one release burst");Check(!m.Slow(),"slow cooldown blocks repeat");
 m=new CombatModel(b);m.Spawn(1,Vector2.one);m.shots[0]=new Shot{active=true,pos=Vector2.one,vel=Vector2.right,life=3};m.score=777;m.chain=3;var snap=m.Capture();float random=m.NextRandom();m.player=Vector2.one*4;m.score=0;m.foes[0].active=false;m.shots[0].active=false;m.rewindCD=9;m.Restore(snap);Check(m.player==Vector2.zero&&m.score==777&&m.chain==3&&m.foes[0].active&&m.shots[0].active,"full snapshot restore");Check(m.NextRandom()==random&&m.rewindCD==9,"rng restored cooldown retained");Check(snap.foes[0].active,"snapshot deep copy");
 m=new CombatModel(b);m.shots[0]=new Shot{active=true,pos=new Vector2(-1,0),vel=new Vector2(20,0),life=3};m.Step(.1f);Check(m.hit,"one swept projectile is fatal");m=new CombatModel(b){grace=1};m.shots[0]=new Shot{active=true,pos=new Vector2(-1,0),vel=new Vector2(20,0),life=3};m.Step(.1f);Check(!m.hit,"rewind grace");
 m=new CombatModel(b);for(int n=0;n<200;n++){var options=m.Offers();Check(options.Distinct().Count()==3,"distinct offers");}m.draft=true;float t=m.elapsed;m.Step(10);Check(m.elapsed==t,"draft freezes simulation");m.Choose(0);Check(m.levels[0]==1&&!m.draft&&m.reward==30,"upgrade resumes and resets");
 m=new CombatModel(b);for(int i=0;i<9;i++)m.levels[i]=3;Check(m.Offers().All(x=>x==9),"maxed fallback");
 m=new CombatModel(b);for(int i=0;i<10000;i++){m.grace=1;if(i%17==0)m.Aim(new[]{Vector2.up,Vector2.right,Vector2.down,Vector2.left}[(i/17)%4]);if(i%701==0)m.Slow();m.Step(1f/60);if(m.draft)m.Choose(m.Offers()[0]);Check(!float.IsNaN(m.player.x)&&Mathf.Abs(m.player.x)<=5&&Mathf.Abs(m.player.y)<=5,"10k frame boundary");}
 Check(m.foes.Length==36&&m.shots.Length==80,"bounded pools");
 Directory.CreateDirectory("Docs/QA");File.WriteAllText("Docs/QA/logic-checks.txt","PASS — "+passed+" assertions\n10,000 simulated frames; deterministic movement, kill settlement, slow/deferred release, snapshot, random restore, fatal collision, grace, upgrade pool and freeze.\n"+DateTime.Now.ToString("s"));Debug.Log("BLOCK NIGHT QA: PASS "+passed+" assertions");
 }
 [MenuItem("Block Night/QA/Combat Fixture")]
 public static void Combat(){var d=UnityEngine.Object.FindObjectOfType<GameDirector>();d.Begin(false);d.model.elapsed=72;d.model.reward=12.8f;d.model.score=4250;d.model.chain=6;Vector2[] pos={new Vector2(-3,3),new Vector2(2,2),new Vector2(3,-2),new Vector2(-2,-3),new Vector2(-1,1),new Vector2(4,4)};for(int i=0;i<pos.Length;i++){d.model.Spawn(i%4,pos[i]);d.model.foes[i].age=3;d.model.foes[i].timer=i%2==0?.45f:1.5f;d.model.foes[i].aim=(d.model.player-pos[i]).normalized;}for(int i=0;i<10;i++)d.model.shots[i]=new Shot{active=true,pos=new Vector2(-4+i*.8f,-1.4f),vel=Vector2.down,life=5};d.model.grace=100;d.arena.Render(d.model);d.hud.Render(d,true);d.enabled=false;Canvas.ForceUpdateCanvases();}
 [MenuItem("Block Night/QA/Slow Fixture")]
 public static void Slow(){Combat();var d=UnityEngine.Object.FindObjectOfType<GameDirector>();d.model.Slow();d.model.foes[0].pending=true;d.model.foes[1].pending=true;d.arena.Render(d.model);d.arena.TimeEffect(true,false,false,1);d.hud.Render(d,true);}
 [MenuItem("Block Night/QA/Rewind Fixture")]
 public static void Rewind(){Combat();var d=UnityEngine.Object.FindObjectOfType<GameDirector>();d.mode=Mode.Rewinding;d.arena.TimeEffect(false,true,false,1);d.hud.Render(d,true);}
 [MenuItem("Block Night/QA/Draft Fixture")]
 public static void Draft(){Combat();var d=UnityEngine.Object.FindObjectOfType<GameDirector>();d.mode=Mode.Draft;d.hud.SetOffers(new[]{0,2,5},d.model.levels);d.hud.Render(d,true);}
 [MenuItem("Block Night/QA/Title Fixture")]
 public static void Title(){var d=UnityEngine.Object.FindObjectOfType<GameDirector>();d.Home();d.hud.Render(d,false);d.enabled=false;Canvas.ForceUpdateCanvases();}
}
}
