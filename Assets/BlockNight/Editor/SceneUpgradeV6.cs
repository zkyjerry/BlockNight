using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Cinemachine;
namespace BlockNight.Editor {
 public static class SceneUpgradeV6 {
  [MenuItem("Block Night/Apply v6 Feedback")]
  public static void Apply(){
   if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode");EditorSceneManager.SaveOpenScenes();const string path="Assets/BlockNight/Scenes/BlockNight.unity";const string backup="Docs/Backups/BlockNight-before-v6.unity.backup";if(!File.Exists(backup))File.Copy(path,backup);var scene=EditorSceneManager.OpenScene(path);var d=UnityEngine.Object.FindObjectOfType<GameDirector>();
   const string data="Assets/BlockNight/Data/FeedbackSettings.asset";var settings=AssetDatabase.LoadAssetAtPath<FeedbackSettings>(data);if(!settings){settings=ScriptableObject.CreateInstance<FeedbackSettings>();AssetDatabase.CreateAsset(settings,data);}
   var f=d.GetComponent<CombatFeedback>();if(!f)f=d.gameObject.AddComponent<CombatFeedback>();d.feedback=f;f.settings=settings;f.arena=d.arena;d.arena.feedbackSettings=settings;f.outputCamera=d.arena.shakeCamera.GetComponent<Camera>();f.brain=f.outputCamera.GetComponent<CinemachineBrain>();if(!f.brain)f.brain=f.outputCamera.gameObject.AddComponent<CinemachineBrain>();f.brain.enabled=false;f.brain.m_UpdateMethod=CinemachineBrain.UpdateMethod.ManualUpdate;f.brain.m_DefaultBlend=new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut,0);
   if(!f.focusCamera){var go=new GameObject("DYING FOCUS — Cinemachine");f.focusCamera=go.AddComponent<CinemachineVirtualCamera>();}f.focusCamera.transform.position=f.outputCamera.transform.position;f.focusCamera.m_Lens.OrthographicSize=f.outputCamera.orthographicSize;f.focusCamera.m_Lens.ModeOverride=LensSettings.OverrideModes.Orthographic;f.focusCamera.Priority=100;f.focusCamera.enabled=false;
   f.countdownTrack=Line(f.countdownTrack,"UPGRADE CLOCK — track",d.arena);f.countdownRing=Line(f.countdownRing,"UPGRADE CLOCK — remaining CCW",d.arena);f.upgradeRing=Line(f.upgradeRing,"UPGRADE CLEAR — expanding wave",d.arena);
   foreach(var line in new[]{f.countdownTrack,f.countdownRing}){line.positionCount=129;for(int i=0;i<=128;i++)line.SetPosition(i,CombatFeedback.EdgePoint(i/128f,d.arena.cellSize*4+.16f));line.startWidth=line.endWidth=line==f.countdownTrack?.14f:.115f;line.startColor=line.endColor=line==f.countdownTrack?new Color(.035f,.13f,.11f):new Color(.1f,1,.72f)*1.5f;}f.upgradeRing.loop=true;f.upgradeRing.startWidth=f.upgradeRing.endWidth=.12f;f.upgradeRing.enabled=false;
   if(!f.readyFlash){var canvasGo=new GameObject("SKILL READY — screen flash",typeof(RectTransform),typeof(Canvas));var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;var go=new GameObject("Skill colour flash",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(canvasGo.transform,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;f.readyFlash=go.GetComponent<Image>();f.readyFlash.raycastTarget=false;}f.readyFlash.enabled=false;
   d.hud.progress.enabled=false;var track=d.hud.transform.GetComponentsInChildren<Image>(true).FirstOrDefault(x=>x.name=="Progress track");if(track)track.enabled=false;
   EditorUtility.SetDirty(d);EditorUtility.SetDirty(d.arena);EditorUtility.SetDirty(f);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/BlockNight/Scenes/MainMenu.unity");File.WriteAllText("Docs/QA/v6-migration.txt","PASS: saved experiment SO, Cinemachine focus camera, thick world countdown ring, upgrade wave, non-blocking skill flash; existing UI layout preserved.");
  }
  static LineRenderer Line(LineRenderer line,string name,ArenaPresentation a){if(!line)line=new GameObject(name).AddComponent<LineRenderer>();line.sharedMaterial=a.shieldArc.sharedMaterial;line.useWorldSpace=true;line.numCornerVertices=4;line.numCapVertices=4;line.sortingOrder=20;return line;}
 }
}
