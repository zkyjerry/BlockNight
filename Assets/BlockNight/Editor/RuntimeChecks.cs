using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace BlockNight.Editor {
public static class RuntimeChecks {
 static GameDirector game;
 static void Assert(bool test,string label){if(!test)throw new Exception(label);}
 static void Frames(int n){for(int i=0;i<n;i++)game.Tick(1f/60);}
 [MenuItem("Block Night/QA/Run Runtime Checks")]
 public static void Run(){if(!EditorApplication.isPlaying)throw new Exception("Enter Play Mode first");game=UnityEngine.Object.FindObjectOfType<GameDirector>();game.enabled=false;
 try{game.Begin(false);game.model.grace=20;Frames(60);Assert(game.mode==Mode.Playing,"begin run");game.model.Aim(Vector2.up);Assert(game.model.Slow(),"activate slow");Frames(60);Assert(game.Rewind(),"rewind available with history");Frames(60);Assert(game.mode==Mode.Playing&&game.model.rewindCD>0,"rewind completes and spends cooldown");game.model.reward=.001f;Frames(1);Assert(game.mode==Mode.Draft,"draft opens");game.hud.cards[0].onClick.Invoke();Assert(game.mode==Mode.Playing,"card resumes");game.Pause();float frozen=game.model.elapsed;Frames(60);Assert(game.mode==Mode.Paused&&game.model.elapsed==frozen,"pause freezes");game.Pause();game.model.hit=true;Frames(1);Assert(game.mode==Mode.Dying,"fatal hit grace window");Frames(50);Assert(game.mode==Mode.Dead,"death ends run");game.hud.retryButton.onClick.Invoke();Assert(game.mode==Mode.Playing&&game.model.score==0,"retry reset");game.Begin(true);Assert(game.model.tutorial&&game.tutorialStep==0,"tutorial start");game.hud.skipButton.onClick.Invoke();Assert(!game.model.tutorial,"skip tutorial");game.Home();Assert(game.mode==Mode.Title,"return home");Finish("PASS — 292 driven Play Mode frames: slow, full rewind, draft/card, pause/resume, fatal hit, death, retry, tutorial/skip, title.");}
 catch(Exception e){Finish("FAIL — "+e.Message);Debug.LogException(e);}finally{game.enabled=true;}}
 static void Finish(string report){Directory.CreateDirectory("Docs/QA");File.WriteAllText("Docs/QA/runtime-checks.txt",report+"\n"+DateTime.Now.ToString("s"));Debug.Log("BLOCK NIGHT RUNTIME: "+report);}
 [MenuItem("Block Night/QA/Capture 1600x1000")]
 public static void CaptureLarge(){Capture(1600,1000);}
 [MenuItem("Block Night/QA/Capture 1280x720")]
 public static void CaptureWide(){Capture(1280,720);}
 static void Capture(int w,int h){var assembly=typeof(UnityEditor.Editor).Assembly;var sizesType=assembly.GetType("UnityEditor.GameViewSizes");var single=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);var sizes=single.GetProperty("instance").GetValue(null);var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{0});var sizeType=assembly.GetType("UnityEditor.GameViewSize");var kind=assembly.GetType("UnityEditor.GameViewSizeType");var fixedSize=Activator.CreateInstance(sizeType,new object[]{Enum.ToObject(kind,1),w,h,"BN QA "+w+"x"+h});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{fixedSize});int n=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,n-1);view.Repaint();var d=UnityEngine.Object.FindObjectOfType<GameDirector>();string state=d.model.tutorial?"tutorial":d.mode==Mode.Draft?"draft":d.mode==Mode.Rewinding?"rewind":d.mode==Mode.Title?"title":d.model.slow>0?"slow":"combat";int frames=0;EditorApplication.CallbackFunction capture=null;capture=()=>{if(++frames<15)return;EditorApplication.update-=capture;Directory.CreateDirectory("Docs/QA");ScreenCapture.CaptureScreenshot("Docs/QA/"+state+"-"+w+"x"+h+".png");Debug.Log("BN screenshot queued: "+state);};EditorApplication.update+=capture;}
}
}
