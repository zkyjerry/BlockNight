using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Febucci.UI;
namespace BlockNight.Editor {
 public static class LessonSceneSetup {
  [MenuItem("Block Night/Apply Three Lesson Tutorial")]
  public static void Apply(){
   if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");EditorSceneManager.SaveOpenScenes();const string path="Assets/BlockNight/Scenes/BlockNight.unity";const string backup="Docs/Backups/BlockNight-before-lessons-v7.unity.backup";if(!File.Exists(backup))File.Copy(path,backup);var scene=EditorSceneManager.OpenScene(path);var d=UnityEngine.Object.FindObjectOfType<GameDirector>();
   if(d.lessons)throw new Exception("Tutorial already installed; edit its serialized objects directly.");
   var flow=d.gameObject.AddComponent<LessonFlow>();d.lessons=flow;flow.director=d;
   var font=d.hud.countdown.font;
   var canvasGo=new GameObject("LESSONS AND INTRO — editable overlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=200;var scaler=canvasGo.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);scaler.matchWidthOrHeight=.5f;
   var dialog=Image("Tutorial dialogue",canvasGo.transform,new Color(.012f,.025f,.045f,.9f));Rect(dialog.rectTransform,new Vector2(.05f,0),new Vector2(.95f,0),new Vector2(0,22),new Vector2(0,205));dialog.rectTransform.pivot=new Vector2(.5f,0);flow.dialoguePanel=dialog.gameObject;
   var accent=Image("Dialogue accent",dialog.transform,new Color(.12f,1,.8f));Rect(accent.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(0,-2),new Vector2(0,3));
   flow.dialogueText=Text("Tutorial text — animated TMP",dialog.transform,font,30,TextAlignmentOptions.TopLeft);Rect(flow.dialogueText.rectTransform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);flow.dialogueText.margin=new Vector4(32,26,32,48);flow.dialogueText.text="第一课 / 移动与斩击";
   flow.dialogueHint=Text("Next line hint",dialog.transform,font,18,TextAlignmentOptions.Left);Rect(flow.dialogueHint.rectTransform,Vector2.zero,new Vector2(1,0),new Vector2(0,24),new Vector2(0,30));flow.dialogueHint.margin=new Vector4(32,0,32,0);flow.dialogueHint.color=new Color(.35f,1,.82f);flow.dialogueHint.text="任意键 / 下一条";
   var animator=flow.dialogueText.gameObject.AddComponent<TextAnimator>();animator.timeScale=TextAnimator.TimeScale.Unscaled;flow.textPlayer=flow.dialogueText.gameObject.AddComponent<TextAnimatorPlayer>();flow.textPlayer.useTypeWriter=true;flow.textPlayer.waitForNormalChars=.025f;flow.textPlayer.waitLong=.12f;flow.textPlayer.waitMiddle=.05f;var serialized=new SerializedObject(flow.textPlayer);serialized.FindProperty("startTypewriterMode").intValue=2;serialized.ApplyModifiedPropertiesWithoutUndo();
   var mask=Image("Run countdown — opaque black mask",canvasGo.transform,Color.black);Rect(mask.rectTransform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);mask.raycastTarget=true;flow.countdownPanel=mask.gameObject;flow.countdownText=Text("Run countdown text",mask.transform,font,240,TextAlignmentOptions.Center);Rect(flow.countdownText.rectTransform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);flow.countdownText.text="3";
   flow.slowEnemy=Enemy("Tutorial slow warning",new Vector2Int(1,0),2,2.5f);flow.rewindEnemy=Enemy("Tutorial rewind attacker",new Vector2Int(0,-1),1.6f,2.5f);
   dialog.gameObject.SetActive(false);mask.gameObject.SetActive(false);EditorUtility.SetDirty(d);EditorUtility.SetDirty(flow);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   string script=File.ReadAllText("Assets/BlockNight/Scripts/LessonFlow.cs");string missing;font.TryAddCharacters(new string(script.Where(c=>c>=' ').Distinct().ToArray()),out missing,true);foreach(var texture in font.atlasTextures){if(!AssetDatabase.Contains(texture))AssetDatabase.AddObjectToAsset(texture,font);EditorUtility.SetDirty(texture);}EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();
   EditorSceneManager.OpenScene("Assets/BlockNight/Scenes/MainMenu.unity");File.WriteAllText("Docs/QA/tutorial-v7-migration.txt","Added independent dialogue / opaque intro overlay and two tutorial enemy SOs; existing UI untouched. Missing new font chars: "+missing);
  }
  static EnemyDefinition Enemy(string name,Vector2Int dir,float warning,float interval){string path="Assets/BlockNight/Data/Enemies/"+name+".asset";var def=AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);if(!def){def=ScriptableObject.CreateInstance<EnemyDefinition>();def.displayName=name;def.rules=new EnemyRules{direction=dir,attackWarning=warning,attackInterval=interval};AssetDatabase.CreateAsset(def,path);}return def;}
  static Image Image(string name,Transform parent,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
  static TMP_Text Text(string name,Transform parent,TMP_FontAsset font,float size,TextAlignmentOptions align){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TextMeshProUGUI>();text.font=font;text.fontSharedMaterial=font.material;text.fontSize=size;text.color=Color.white;text.alignment=align;text.enableWordWrapping=true;text.raycastTarget=false;return text;}
  static void Rect(RectTransform r,Vector2 min,Vector2 max,Vector2 pos,Vector2 size){r.anchorMin=min;r.anchorMax=max;r.anchoredPosition=pos;r.sizeDelta=size;}
 }
}
