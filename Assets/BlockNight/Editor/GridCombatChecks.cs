using System;
using System.Linq;
using UnityEngine;
namespace BlockNight.Editor {
 public static class GridCombatChecks {
  static int assertions;
  static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL: "+label);assertions++;}
  static void Place(CombatModel m,int x,int y,Vector2 dir){m.cell=m.target=new Vector2Int(x,y);m.player=CombatModel.Center(m.cell);m.direction=dir;m.moving=m.dashing=false;m.travel=0;}
  static void Frames(CombatModel m,int count){for(int i=0;i<count;i++)m.Step(.01f);}
  static Shot Hazard(Vector2Int cell,Vector2Int source)=>new Shot{active=true,damaging=true,cell=cell,sourceCell=source,heading=cell-source,remaining=4,timer=.16f,impactDuration=.16f,warning=.28f,pos=CombatModel.Center(cell)};
  public static string Run(){assertions=0;var balance=ScriptableObject.CreateInstance<Balance>();var schedule=ScriptableObject.CreateInstance<SpawnSchedule>();schedule.phases=new SpawnPhase[0];var definition=ScriptableObject.CreateInstance<EnemyDefinition>();
   try {
    var m=new CombatModel(balance,schedule){tutorial=true};Vector2 origin=m.player;m.MoveOrTurn(Vector2.right);Check(m.player==origin&&!m.moving,"turn in place");m.Spawn(0,CombatModel.Center(new Vector2Int(4,3)));m.foes[0].age=2;m.MoveOrTurn(Vector2.right);Frames(m,20);Check(m.cell==new Vector2Int(4,3)&&m.kills==0,"nonlethal one-cell walk");
    m=new CombatModel(balance,schedule){tutorial=true};Place(m,0,1,Vector2.right);m.Spawn(0,CombatModel.Center(new Vector2Int(1,1)));m.Spawn(1,CombatModel.Center(new Vector2Int(1,3)));for(int i=0;i<2;i++)m.foes[i].age=3;m.Dash();m.Step(.03f);m.MoveOrTurn(Vector2.up);Frames(m,90);Check(m.cell==new Vector2Int(1,7)&&m.bestChain==2,"turning slash remains one chain");
    definition.rules.attackInterval=1.1f;definition.rules.attackWarning=.4f;definition.rules.direction=Vector2Int.right;
    m=new CombatModel(balance,schedule,new[]{definition}){grace=20};m.Spawn(0,CombatModel.Center(new Vector2Int(1,3)));m.foes[0].age=3;
    Check(Mathf.Abs(m.foes[0].timer-1.1f)<.001f,"SO attack interval honored");Frames(m,72);Check(m.foes[0].attackWindup&&m.foes[0].attackTarget==new Vector2Int(2,3)&&m.foes[0].pos==CombatModel.Center(new Vector2Int(1,3)),"SO warning locks target before movement");Frames(m,45);Check(m.foes[0].pos==CombatModel.Center(new Vector2Int(2,3)),"ram advances exactly one tile");
    m=new CombatModel(balance,schedule);Place(m,2,3,Vector2.up);m.Spawn(0,CombatModel.Center(new Vector2Int(1,3)));m.foes[0].age=3;m.foes[0].timer=.01f;m.foes[0].attackWindup=true;m.foes[0].attackTarget=CombatModel.CellAt(m.foes[0].pos)+m.foes[0].heading;m.Step(.02f);Check(m.hit,"ram impact kills occupant");
    m=new CombatModel(balance,schedule){grace=20};m.Spawn(0,CombatModel.Center(new Vector2Int(1,3)));m.Spawn(0,CombatModel.Center(new Vector2Int(2,3)));m.foes[0].age=3;m.foes[0].timer=.01f;m.foes[0].attackWindup=true;m.foes[0].attackTarget=CombatModel.CellAt(m.foes[0].pos)+m.foes[0].heading;m.Step(.02f);Check(m.foes[0].pos==CombatModel.Center(new Vector2Int(1,3)),"ram respects enemy occupancy");
    m=new CombatModel(balance,schedule){grace=20};m.Spawn(1,CombatModel.Center(new Vector2Int(1,1)));m.foes[0].age=3;m.foes[0].timer=.01f;m.foes[0].attackWindup=true;m.foes[0].attackTarget=CombatModel.CellAt(m.foes[0].pos)+m.foes[0].heading;m.foes[0].hopTimer=100;m.Step(.02f);Check(m.shots[0].cell==new Vector2Int(2,2)&&m.shots[0].damaging,"wave activates first diagonal tile");Frames(m,15);Check(m.shots[0].cell==new Vector2Int(3,3)&&!m.shots[0].damaging,"next wave tile has separate warning");var frame=m.Capture();Frames(m,30);Check(m.shots[0].damaging,"wave warning resolves to impact");m.Restore(frame);Check(m.shots[0].timer==frame.shots[0].timer&&!m.shots[0].damaging,"rewind restores tile and warning phase");
    m=new CombatModel(balance,schedule){grace=20};m.Spawn(1,CombatModel.Center(new Vector2Int(1,1)));m.foes[0].age=3;m.foes[0].timer=100;m.foes[0].hopTimer=.01f;m.Step(.02f);Check(m.foes[0].moveWindup&&m.foes[0].pos==CombatModel.Center(new Vector2Int(1,1)),"overdue movement still receives full warning");Frames(m,62);Check(m.foes[0].pos==CombatModel.Center(new Vector2Int(2,2)),"wave enemy moves one diagonal cell");
    for(int side=0;side<3;side++){
     m=new CombatModel(balance,schedule);m.levels[9]=1;m.direction=Vector2.right;m.Dash();var saved=m.Capture();int blocked=0;m.ShieldBlocked+=p=>blocked++;
     var source=new Vector2Int(side==0?4:side==1?2:3,side==2?4:3);m.shots[0]=Hazard(m.cell,source);m.Step(.01f);
     Check(!m.hit&&blocked==1&&!m.shots[0].active,"shield absorbs front/side/rear impact");
     if(side==0){Check(m.levels[9]==0&&!m.shieldArmed,"one shield consumed without recharge");m.Restore(saved);Check(!m.shieldArmed&&m.levels[9]==0,"rewind cannot refund consumed shield");}
    }
    m=new CombatModel(balance,schedule);m.levels[9]=1;m.direction=Vector2.right;m.Dash();m.shots[0]=m.shots[1]=Hazard(m.cell,new Vector2Int(4,3));m.Step(.01f);Check(m.hit&&!m.shots[0].active&&m.shots[1].active,"shield blocks only one simultaneous impact");
    m=new CombatModel(balance,schedule);m.levels[9]=1;m.shots[0]=Hazard(m.cell,new Vector2Int(4,3));m.Step(.01f);Check(m.hit,"stationary player has no shield");
    m=new CombatModel(balance,schedule);m.Choose(9);Check(m.levels[9]==1&&m.Offers().All(i=>i!=9),"one-level shield excluded after acquisition");for(int i=0;i<10;i++)m.levels[i]=CombatModel.MaxLevel(i);Check(m.Offers().All(i=>i==10),"all-max fallback moved after shield");
    m=new CombatModel(balance,schedule);for(int i=0;i<9;i++)m.levels[i]=3;m.Choose(9);m.Choose(9);Check(m.levels[9]==1&&m.Offers().All(i=>i!=9),"shield cannot stack or appear while held");m.direction=Vector2.right;m.Dash();m.shots[0]=Hazard(m.cell,m.cell+Vector2Int.up);m.Step(.01f);Check(m.levels[9]==0&&m.Offers().Contains(9),"consumed shield returns to upgrade pool");Frames(m,900);Check(m.levels[9]==0,"shield never automatically recharges");m.Choose(9);Check(m.levels[9]==1,"new upgrade replenishes exactly one shield");
    m=new CombatModel(balance,schedule){grace=20};m.Spawn(1,CombatModel.Center(new Vector2Int(2,2)));m.foes[0].age=3;float before=m.foes[0].timer;m.Slow();m.Step(.05f);Check(Mathf.Abs(m.foes[0].timer-(before-.05f*.18f))<.0001f,"time fold slows enemy rules");
    schedule.phases=new[]{new SpawnPhase{spawnCount=4,spawnInterval=1,enemyTypes=new[]{EnemyKind.突进方卫,EnemyKind.斜波棱镜,EnemyKind.巡格猎手,EnemyKind.逆波脉冲}}};m=new CombatModel(balance,schedule);
    for(int i=0;i<10000;i++){
     m.grace=10;if(i%19==0)m.MoveOrTurn(new[]{Vector2.up,Vector2.right,Vector2.down,Vector2.left}[(i/19)%4]);if(i%71==0)m.Dash();m.Step(1f/60);if(m.draft)m.Choose(m.Offers()[0]);
     Check(CombatModel.InBounds(m.cell)&&CombatModel.InBounds(m.target),"player stays on board");var active=m.foes.Where(f=>f.active).ToArray();Check(active.Select(f=>f.pos).Distinct().Count()==active.Length,"no enemy overlap");foreach(var f in active)Check(f.pos==CombatModel.Center(CombatModel.CellAt(f.pos)),"enemy always at grid center");foreach(var attack in m.shots.Where(x=>x.active))Check(CombatModel.InBounds(attack.cell)&&attack.pos==CombatModel.Center(attack.cell)&&attack.vel==Vector2.zero,"all attacks stay on exact tiles; no flying bullets");
    }
    return "GRID V4 MODEL PASS: "+assertions+" assertions, 10,000 simulated frames. SO timing, ram, diagonal tile wave, shield omnidirectional/consumption/reoffer/rewind, grid occupancy and upgrades.";
   }finally{UnityEngine.Object.DestroyImmediate(balance);UnityEngine.Object.DestroyImmediate(schedule);UnityEngine.Object.DestroyImmediate(definition);}
  }
#if UNITY_EDITOR
  [UnityEditor.MenuItem("Block Night/QA/Shield v4 Fixture")]
  public static void ShieldFixture(){var d=UnityEngine.Object.FindObjectOfType<GameDirector>();d.Begin(false);d.enabled=false;d.model.levels[9]=1;d.model.direction=Vector2.right;d.model.Dash();d.model.Step(.025f);d.arena.Render(d.model);d.hud.Render(d,true);}
  [UnityEditor.MenuItem("Block Night/QA/Shield v4 Draft Fixture")]
  public static void ShieldDraft(){var d=UnityEngine.Object.FindObjectOfType<GameDirector>();d.Begin(false);d.enabled=false;d.mode=Mode.Draft;d.hud.SetOffers(new[]{9,4,6},d.model.levels);d.hud.Render(d,true);}
  [UnityEditor.MenuItem("Block Night/QA/Run Grid v4 Checks")]
  public static void Menu(){string report=Run();System.IO.File.WriteAllText("Docs/QA/grid-v4-unity-checks.txt",report);Debug.Log(report);}
#endif
 }
}
