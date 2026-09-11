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
  public static void Presentation(){SceneUpgradeV5.CheckPresentation();}
  [MenuItem("Block Night/QA/Check Static Menu")]
  public static void MenuAnimation(){
   Check(EditorApplication.isPlaying,"requires Play Mode");var menu=UnityEngine.Object.FindObjectOfType<MenuController>();Check(menu,"requires menu scene");var labels=menu.GetComponentsInChildren<TMP_Text>(true);
   foreach(var label in labels){Check(!label.GetComponent<Febucci.UI.TextAnimator>()&&!label.GetComponent<Febucci.UI.TextAnimatorPlayer>(),"no text animation components");Check(label.font&&label.font.name=="FZXS12 TMP","user font");Check(label.maxVisibleCharacters>=label.text.Length,"all text visible");}
   if(menu.isGameOver)Check(menu.resultText.text.Contains(SceneFlow.Score.ToString("N0")),"correct results");Check(menu.primaryButton.interactable&&menu.secondaryButton.interactable,"buttons usable");if(menu.quitButton)Check(menu.quitButton.interactable,"quit usable");
   File.WriteAllText("Docs/QA/"+menu.gameObject.scene.name.ToLower()+"-static-v5.txt","PASS: "+labels.Length+" static TMP labels, no TextAnimator, correct font, complete text, usable buttons and result binding.");
  }
 }
}
