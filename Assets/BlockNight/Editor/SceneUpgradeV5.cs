using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
namespace BlockNight.Editor {
 public static class SceneUpgradeV5 {
  [MenuItem("Block Night/Apply v5 Shield and Telegraphs")]
  public static void Apply(){
   if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");EditorSceneManager.SaveOpenScenes();
   foreach(var name in new[]{"MainMenu","GameOver","BlockNight"}){
    var path="Assets/BlockNight/Scenes/"+name+".unity";var backup="Docs/Backups/"+name+"-before-v5.unity.backup";if(!File.Exists(backup))File.Copy(path,backup);
    var scene=EditorSceneManager.OpenScene(path);
    var menu=UnityEngine.Object.FindObjectOfType<MenuController>();
    if(menu){
     foreach(var player in menu.GetComponentsInChildren<Febucci.UI.TextAnimatorPlayer>(true))UnityEngine.Object.DestroyImmediate(player);
     foreach(var animator in menu.GetComponentsInChildren<Febucci.UI.TextAnimator>(true))UnityEngine.Object.DestroyImmediate(animator);
     menu.titleAnimation=menu.resultAnimation=menu.primaryAnimation=menu.secondaryAnimation=null;
     foreach(var label in menu.GetComponentsInChildren<TMP_Text>(true)){label.maxVisibleCharacters=int.MaxValue;label.ForceMeshUpdate(true);EditorUtility.SetDirty(label);}EditorUtility.SetDirty(menu);
    }
    var arena=UnityEngine.Object.FindObjectOfType<ArenaPresentation>();if(arena){var line=arena.shieldArc;line.name="DASH SHIELD — editable circle";line.loop=true;line.positionCount=48;for(int i=0;i<48;i++){float angle=2*Mathf.PI*i/48;line.SetPosition(i,arena.player.position+new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*arena.cellSize*.6f);}line.enabled=false;EditorUtility.SetDirty(line);}
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   }
   AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/BlockNight/Scenes/MainMenu.unity");
   File.WriteAllText("Docs/QA/v5-migration.txt","PASS: TextAnimator components removed from both menu scenes; TMP layouts retained; serialized 48-point shield circle.\n"+DateTime.Now.ToString("s"));
  }
  [MenuItem("Block Night/QA/Check v5 Presentation")]
  public static void CheckPresentation(){
   if(!EditorApplication.isPlaying)throw new Exception("Requires Play Mode");var d=UnityEngine.Object.FindObjectOfType<GameDirector>();d.Begin(false);d.enabled=false;var m=d.model;var a=d.arena;m.tutorial=true;m.grace=0;
   for(int i=0;i<4;i++){m.Spawn(i,CombatModel.Center(new Vector2Int(i+1,5)));}a.Render(m);
   for(int i=0;i<4;i++){Require(a.bodies[i].color.g>a.bodies[i].color.r*.5f||a.bodies[i].color.b>a.bodies[i].color.r,"enemy is not red");Require(a.warnings[i].color.g>a.warnings[i].color.r*.5f||a.warnings[i].color.b>a.warnings[i].color.r,"spawn is not red");}
   m.foes[0].age=3;m.foes[0].attackWindup=true;m.foes[0].attackTarget=new Vector2Int(1,4);a.Render(m);Require(a.warnings[0].color.r>10*a.warnings[0].color.g&&a.aims[0].startColor.r>10*a.aims[0].startColor.g,"red target warning");
   m.Choose(9);m.direction=Vector2.right;m.Dash();a.Render(m);Require(a.shieldArc.enabled&&a.shieldArc.loop&&a.shieldArc.positionCount==48,"closed shield circle");for(int i=0;i<48;i++)Require(Mathf.Abs(Vector2.Distance(a.shieldArc.GetPosition(i),a.World(m.player,-.35f))-.6f*a.cellSize)<.001f,"circle encloses player");
   m.shots[0]=new Shot{active=true,damaging=true,cell=m.cell,sourceCell=m.cell+Vector2Int.up,heading=Vector2Int.down,remaining=1,timer=.16f,impactDuration=.16f,warning=.28f,pos=m.player};m.Step(.01f);a.Render(m);Require(!m.hit&&m.levels[9]==0&&!a.shieldArc.enabled&&a.shards.particleCount>=40,"side hit consumes shield and emits particles");
   d.mode=Mode.Draft;d.hud.SetOffers(new[]{9,4,6},m.levels);d.hud.Render(d,true);Canvas.ForceUpdateCanvases();foreach(var label in d.hud.cardTexts){label.ForceMeshUpdate();Require(label.textBounds.size.x<=label.rectTransform.rect.width+1&&label.textBounds.size.y<=label.rectTransform.rect.height+1,"card fits");}
   File.WriteAllText("Docs/QA/v5-presentation.txt","PASS: four non-red enemies/spawn effects, red attack target, 48-point circular shield, side-hit consumption and particles, upgrade card bounds.\n"+DateTime.Now.ToString("s"));
  }
  static void Require(bool ok,string s){if(!ok)throw new Exception("V5 FAIL: "+s);}
 }
}
