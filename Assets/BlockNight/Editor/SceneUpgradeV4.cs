using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Febucci.UI;
namespace BlockNight.Editor {
 public static class SceneUpgradeV4 {
  const string Root="Assets/BlockNight/Scenes/";
  [MenuItem("Block Night/Apply v4 Grid Combat")]
  public static void Apply(){
   if(EditorApplication.isPlaying)throw new InvalidOperationException("请先退出 Play Mode。");
   EditorSceneManager.SaveOpenScenes();Directory.CreateDirectory("Docs/Backups");
   foreach(string name in new[]{"MainMenu","BlockNight","GameOver"}){string backup="Docs/Backups/"+name+"-before-v4.unity.backup";if(!File.Exists(backup))File.Copy(Root+name+".unity",backup);}
   var scene=EditorSceneManager.OpenScene(Root+"BlockNight.unity");var director=UnityEngine.Object.FindObjectOfType<GameDirector>();
   const string folder="Assets/BlockNight/Data/Enemies";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/BlockNight/Data","Enemies");
   var names=new[]{"突进方卫","斜波棱镜","巡格猎手","逆波脉冲"};var defs=new EnemyDefinition[4];
   for(int i=0;i<4;i++){string path=folder+"/"+names[i]+".asset";defs[i]=AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);if(defs[i])continue;
    var def=ScriptableObject.CreateInstance<EnemyDefinition>();def.displayName=names[i];def.rules=new EnemyRules();
    if(i==1){def.rules.action=EnemyAction.斜向冲击波;def.rules.direction=new Vector2Int(1,1);def.rules.attackInterval=2.8f;}
    if(i==2){def.rules.direction=Vector2Int.up;def.rules.attackInterval=2;def.rules.attackWarning=.55f;}
    if(i==3){def.rules.action=EnemyAction.斜向冲击波;def.rules.direction=new Vector2Int(-1,1);def.rules.attackInterval=3.2f;def.rules.moveInterval=4;def.rules.waveRange=5;}
    def.rules.Clamp();AssetDatabase.CreateAsset(def,path);defs[i]=def;
   }
   director.enemyDefinitions=defs;var arena=director.arena;
   if(!arena.shieldArc){var go=new GameObject("DASH SHIELD — editable arc");var line=go.AddComponent<LineRenderer>();line.sharedMaterial=arena.aims[0].sharedMaterial;line.useWorldSpace=true;line.positionCount=21;line.startWidth=line.endWidth=.065f;line.numCapVertices=6;line.numCornerVertices=4;line.startColor=line.endColor=new Color(.35f,1,1)*2;line.sortingOrder=15;line.enabled=false;arena.shieldArc=line;}
   foreach(var slot in arena.bullets){slot.sprite=arena.shapes[0];slot.transform.localScale=Vector3.one*arena.cellSize*.88f;slot.enabled=false;EditorUtility.SetDirty(slot);}
   EditorUtility.SetDirty(director);EditorUtility.SetDirty(arena);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   foreach(string name in new[]{"MainMenu","GameOver"}){
    scene=EditorSceneManager.OpenScene(Root+name+".unity");var menu=UnityEngine.Object.FindObjectOfType<MenuController>();var texts=menu.GetComponentsInChildren<TMP_Text>(true);
    var geometry=texts.ToDictionary(t=>t.GetInstanceID(),t=>RectKey(t.rectTransform));
    var title=texts.OrderByDescending(t=>t.fontSize).First();menu.titleAnimation=Attach(title,.055f,true);
    menu.primaryAnimation=Attach(menu.primaryButton.GetComponentInChildren<TMP_Text>(true),.02f,false);menu.secondaryAnimation=Attach(menu.secondaryButton.GetComponentInChildren<TMP_Text>(true),.02f,false);
    if(menu.resultText)menu.resultAnimation=Attach(menu.resultText,.012f,true);
    foreach(var t in texts)if(geometry[t.GetInstanceID()]!=RectKey(t.rectTransform))throw new Exception("TextAnimator changed layout: "+t.name);
    EditorUtility.SetDirty(menu);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   }
   AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(Root+"MainMenu.unity");
   File.WriteAllText("Docs/QA/v4-scene-migration.txt","4 enemy definition SO assets; existing grid impact pool reused; 1 serialized shield arc; 7 TMP labels equipped with TextAnimator; menu text layouts unchanged.\n"+DateTime.Now.ToString("s"));
   Debug.Log("V4 scene migration complete. Run Block Night/QA/Run Grid v4 Checks, then validate in Play Mode.");
  }
  static string RectKey(RectTransform t)=>t.anchorMin+"|"+t.anchorMax+"|"+t.pivot+"|"+t.anchoredPosition+"|"+t.sizeDelta+"|"+t.localScale+"|"+t.localRotation;
  static TextAnimatorPlayer Attach(TMP_Text label,float interval,bool typewriter){
   var animator=label.GetComponent<TextAnimator>();if(!animator)animator=label.gameObject.AddComponent<TextAnimator>();animator.timeScale=TextAnimator.TimeScale.Unscaled;
   var player=label.GetComponent<TextAnimatorPlayer>();if(!player)player=label.gameObject.AddComponent<TextAnimatorPlayer>();player.useTypeWriter=typewriter;player.waitForNormalChars=interval;player.waitLong=.08f;player.waitMiddle=.04f;
   var serialized=new SerializedObject(player);serialized.FindProperty("startTypewriterMode").intValue=2;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(animator);EditorUtility.SetDirty(player);return player;
  }
 }
}
