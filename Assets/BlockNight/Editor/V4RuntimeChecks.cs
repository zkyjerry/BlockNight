using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using TMPro;
namespace BlockNight.Editor {
 public static class V4RuntimeChecks {
  static void Check(bool ok,string message){if(!ok)throw new Exception("V4 RUNTIME FAIL: "+message);}
  [MenuItem("Block Night/QA/Grid v4 Combat Fixture")]
  public static void CombatFixture(){AcceptanceChecks.Combat();var d=UnityEngine.Object.FindObjectOfType<GameDirector>();
   d.model.shots[0]=new Shot{active=true,damaging=true,cell=new Vector2Int(6,6),sourceCell=new Vector2Int(5,5),heading=new Vector2Int(1,1),remaining=2,timer=.16f,warning=.28f,impactDuration=.16f,pos=CombatModel.Center(new Vector2Int(6,6))};
   d.model.shots[1]=new Shot{active=true,cell=new Vector2Int(1,2),sourceCell=new Vector2Int(2,1),heading=new Vector2Int(-1,1),remaining=2,timer=.28f,warning=.28f,impactDuration=.16f,pos=CombatModel.Center(new Vector2Int(1,2))};d.arena.Render(d.model);d.hud.Render(d,true);
  }
  [MenuItem("Block Night/QA/Export Console Warnings")]
  public static void ExportWarnings(){var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;var assembly=typeof(UnityEditor.Editor).Assembly;var type=assembly.GetType("UnityEditor.LogEntries");var property=type.GetProperty("consoleFlags",flags);int original=(int)property.GetValue(null);var output=new System.Text.StringBuilder();
   try{property.SetValue(null,(original&~(128|256|512))|256);type.GetMethod("StartGettingEntries",flags).Invoke(null,null);try{int count=(int)type.GetMethod("GetCount",flags).Invoke(null,null);var entryType=assembly.GetType("UnityEditor.LogEntry");var entry=Activator.CreateInstance(entryType);var method=type.GetMethod("GetEntryInternal",flags);var field=entryType.GetField("message",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);for(int i=0;i<count;i++){method.Invoke(null,new[]{(object)i,entry});output.AppendLine((string)field.GetValue(entry));}}finally{type.GetMethod("EndGettingEntries",flags).Invoke(null,null);}}finally{property.SetValue(null,original);}File.WriteAllText("Docs/QA/console-warnings.txt",output.ToString());
  }
  [MenuItem("Block Night/QA/Check v4 Presentation")]
  public static void Presentation(){
   Check(EditorApplication.isPlaying,"requires Play Mode");var d=UnityEngine.Object.FindObjectOfType<GameDirector>();d.enabled=false;d.Begin(false);d.model.tutorial=true;d.model.grace=0;var m=d.model;var a=d.arena;
   Check(d.enemyDefinitions.Length==4&&d.enemyDefinitions.All(x=>x&&AssetDatabase.GetAssetPath(x).StartsWith("Assets/BlockNight/Data/Enemies/")),"four serialized enemy SO references");
   m.Spawn(0,CombatModel.Center(new Vector2Int(1,3)));m.foes[0].age=3;m.foes[0].attackWindup=true;m.foes[0].attackTarget=new Vector2Int(2,3);a.Render(m);
   Check(a.aims[0].enabled&&a.aims[0].positionCount==5&&a.aims[0].GetPosition(0)==a.aims[0].GetPosition(4),"warning outlines one closed tile");
   Check(a.warnings[0].transform.position==a.World(CombatModel.Center(new Vector2Int(2,3)),0),"warning matches committed target cell");
   m.shots[0]=new Shot{active=true,cell=new Vector2Int(3,4),sourceCell=new Vector2Int(2,3),heading=new Vector2Int(1,1),remaining=4,warning=.28f,timer=.28f,impactDuration=.16f,pos=CombatModel.Center(new Vector2Int(3,4))};a.Render(m);float warningAlpha=a.bullets[0].color.a;
   Check(a.bullets[0].sprite==a.shapes[0]&&a.bullets[0].transform.position==a.World(CombatModel.Center(m.shots[0].cell),-.15f),"wave is a square on the exact grid center");m.shots[0].damaging=true;a.Render(m);Check(a.bullets[0].color.a>warningAlpha,"impact visibly stronger than warning");
   d.Begin(false);m=d.model;m.tutorial=true;m.levels[9]=1;m.direction=Vector2.right;m.Dash();a.Render(m);
   Check(a.shieldArc&&a.shieldArc.enabled&&a.shieldArc.positionCount==21,"serialized shield arc shown during slash");
   for(int i=0;i<21;i++)Check(Vector2.Dot((Vector2)(a.shieldArc.GetPosition(i)-a.World(m.player,-.35f)),m.direction)>=a.cellSize*.299f,"arc entirely in front half-space");
   Check(!AssetDatabase.GetAssetPath(a.shieldArc).Contains("Prefab"),"shield is a scene object");
   m.shots[0]=new Shot{active=true,damaging=true,cell=m.cell,sourceCell=m.cell+Vector2Int.right,heading=new Vector2Int(-1,0),remaining=4,timer=.16f,impactDuration=.16f,warning=.28f,pos=m.player};m.Step(.01f);a.Render(m);
   Check(!m.hit&&m.shieldCD>7.9f&&!a.shieldArc.enabled&&a.shards.particleCount>=40,"front hit consumes shield and emits break particles");
   d.mode=Mode.Draft;d.hud.SetOffers(new[]{9,4,6},m.levels);d.hud.Render(d,true);Canvas.ForceUpdateCanvases();
   foreach(var label in d.hud.cardTexts){label.ForceMeshUpdate();Check(label.textBounds.size.x<=label.rectTransform.rect.width+1&&label.textBounds.size.y<=label.rectTransform.rect.height+1,"shield card text fits its existing rectangle");}
   File.WriteAllText("Docs/QA/v4-runtime-presentation.txt","PASS: 4 serialized enemy SOs; target-tile warning; square grid wave warning/impact; 21-point front shield; one-hit break particles; shield card text fits.\n"+DateTime.Now.ToString("s"));Debug.Log("V4 presentation checks PASS");
  }
  [MenuItem("Block Night/QA/Check Menu TextAnimator")]
  public static void MenuAnimation(){
   Check(EditorApplication.isPlaying,"requires Play Mode");var menu=UnityEngine.Object.FindObjectOfType<MenuController>();Check(menu,"requires menu scene");
   var labels=menu.GetComponentsInChildren<TMP_Text>(true);var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/BlockNight/Fonts/FZXS12 TMP.asset");
   foreach(var label in labels){Check(label.font==font,"font preserved");Check(label.GetComponent<Febucci.UI.TextAnimator>()&&label.GetComponent<Febucci.UI.TextAnimatorPlayer>(),"each menu text has TextAnimator");}
   var title=menu.titleAnimation.textAnimator.tmproText;var before=title.mesh.vertices;var rect=title.rectTransform.anchoredPosition;double deadline=EditorApplication.timeSinceStartup+.4;string scene=menu.gameObject.scene.name;
   EditorApplication.CallbackFunction update=null;update=()=>{if(EditorApplication.timeSinceStartup<deadline)return;EditorApplication.update-=update;
    try{Check(menu&&title,"menu survived animation check");var after=title.mesh.vertices;Check(before.Length==after.Length&&before.Where((v,i)=>Vector3.Distance(v,after[i])>.001f).Any(),"TextAnimator actually changes glyph vertices over time");Check(title.rectTransform.anchoredPosition==rect,"text animation preserves layout");Check(menu.titleAnimation.textAnimator.allLettersShown,"title reveal completes");Check(menu.primaryButton.interactable&&menu.secondaryButton.interactable,"buttons remain usable");
     if(menu.isGameOver)Check(menu.resultText.text.Contains(SceneFlow.Score.ToString("N0")),"result text preserves run score");
     File.WriteAllText("Docs/QA/"+scene.ToLower()+"-textanimator.txt","PASS: "+labels.Length+" TMP labels, user font, real animated title vertices, completed reveal, unchanged text rectangle, enabled buttons"+(menu.isGameOver?", correct result score":"")+".\n"+DateTime.Now.ToString("s"));Debug.Log(scene+" TextAnimator checks PASS");
    }catch(Exception e){Debug.LogException(e);File.WriteAllText("Docs/QA/"+scene.ToLower()+"-textanimator.txt","FAIL: "+e.Message);}
   };EditorApplication.update+=update;
  }
 }
}
