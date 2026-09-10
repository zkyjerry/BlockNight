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
 public static void Run(){if(!EditorApplication.isPlaying)throw new Exception("Enter Play Mode first");game=UnityEngine.Object.FindObjectOfType<GameDirector>();game.enabled=false;game.routeScenes=false;
 try{
 var expectedFont=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/BlockNight/Fonts/FZXS12 TMP.asset");
 Assert(game.hud.GetComponentsInChildren<UnityEngine.UI.Text>(true).Length==0,"zero legacy Text components");
 foreach(var label in game.hud.GetComponentsInChildren<TMPro.TMP_Text>(true)) Assert(label.font==expectedFont,"every visible and hidden text uses user TMP font");
 Assert(game.hud.cardTexts[0]!=game.hud.cardTexts[1]&&game.hud.cardTexts[1]!=game.hud.cardTexts[2],"three independent TMP card bindings");
 game.Begin(false);game.model.grace=20;Frames(60);Assert(game.mode==Mode.Playing,"begin run");
 game.StartSlash();Assert(!game.Rewind(),"L cannot interrupt dash");game.Tick(.03f);game.MoveOrTurn(Vector2.right);Assert(game.model.queuedDirection==Vector2.right,"airborne steering input queued");Frames(60);
 Assert(game.ActivateSlow(),"K activates slow");Frames(60);Assert(game.Rewind(),"L rewinds from idle");Frames(60);Assert(game.mode==Mode.Playing&&game.model.rewindCD>0,"rewind completion");
 game.model.reward=.001f;Frames(1);Assert(game.mode==Mode.Draft,"draft opens");game.hud.cards[0].onClick.Invoke();Assert(game.mode==Mode.Playing,"card binding");game.Pause();float frozen=game.model.elapsed;Frames(60);Assert(game.mode==Mode.Paused&&game.model.elapsed==frozen,"pause freezes");game.Pause();game.model.hit=true;Frames(1);Assert(game.mode==Mode.Dying,"fatal grace");Frames(150);Assert(game.mode==Mode.Dead,"death");game.Begin(false);Assert(game.model.score==0&&game.mode==Mode.Playing,"retry");
 var board=GameObject.Find("BOARD — editable world sprites").transform;
 int tiles=0;foreach(Transform child in board)if(child.name.StartsWith("Tile "))tiles++;
 Assert(tiles==64,"exactly 64 tiles");
 var boardPosition=board.position;var cameraRest=game.arena.shakeCamera.localPosition;var uiPosition=game.hud.transform.localPosition;
 game.arena.ClearEffects();game.arena.Kill(game.model.player,7,false);
 Assert(DG.Tweening.DOTween.IsTweening(game.arena.shakeCamera),"DOTween targets camera");DG.Tweening.DOTween.Goto(game.arena.shakeCamera,.07f,false);
 Assert(Vector3.Distance(cameraRest,game.arena.shakeCamera.localPosition)>.001f,"camera actually moves");Assert(board.position==boardPosition&&game.hud.transform.localPosition==uiPosition,"board and UI transforms stay put");
 DG.Tweening.DOTween.Complete(game.arena.shakeCamera);Assert(Vector3.Distance(cameraRest,game.arena.shakeCamera.localPosition)<.0001f,"shake resets without drift");
 game.arena.ClearEffects();game.arena.Kill(game.model.player,7,false);for(int i=0;i<4;i++)game.arena.TimeEffect(false,false,false,.03f);
 game.arena.volume.profile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out var vignette);Assert(vignette.color.value.g>1&&vignette.intensity.value>.45f,"bright HDR vignette");Assert(game.arena.shards.particleCount>=280,"strong tier particle count");
 game.Home();Assert(game.mode==Mode.Title,"home");Finish("GRID V4 PASS — all TMP font references, independent card bindings, runtime flow, three-lesson tutorial covered by LessonChecks, dash steering and rewind lock, 64 scene tiles, camera tween/movement/reset, unchanged board/UI transforms, 280 particles and HDR bright vignette.");
 }catch(Exception e){Finish("FAIL — "+e.Message);Debug.LogException(e);}finally{game.routeScenes=true;game.enabled=true;}}
 static void Finish(string report){Directory.CreateDirectory("Docs/QA");File.WriteAllText("Docs/QA/runtime-checks.txt",report+"\n"+DateTime.Now.ToString("s"));Debug.Log("BLOCK NIGHT RUNTIME: "+report);}
 [MenuItem("Block Night/QA/Force Game Over")]
 public static void ForceDeath(){var d=UnityEngine.Object.FindObjectOfType<GameDirector>();d.enabled=true;d.routeScenes=true;d.model.score=12345;d.model.kills=42;d.model.bestChain=7;d.model.hit=true;}
 [MenuItem("Block Night/QA/Capture 1600x1000")]
 public static void CaptureLarge(){Capture(1600,1000);}
 [MenuItem("Block Night/QA/Capture 1280x720")]
 public static void CaptureWide(){Capture(1280,720);}
 static void Capture(int w,int h){var assembly=typeof(UnityEditor.Editor).Assembly;var sizesType=assembly.GetType("UnityEditor.GameViewSizes");var single=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);var sizes=single.GetProperty("instance").GetValue(null);var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{0});var sizeType=assembly.GetType("UnityEditor.GameViewSize");var kind=assembly.GetType("UnityEditor.GameViewSizeType");var fixedSize=Activator.CreateInstance(sizeType,new object[]{Enum.ToObject(kind,1),w,h,"BN QA "+w+"x"+h});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{fixedSize});int n=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,n-1);view.Repaint();var d=UnityEngine.Object.FindObjectOfType<GameDirector>();string state=d==null?UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLowerInvariant():d.model.tutorial?"tutorial":d.mode==Mode.Draft?"draft":d.mode==Mode.Rewinding?"rewind":d.mode==Mode.Title?"title":d.model.slow>0?"slow":"combat";int frames=0;EditorApplication.CallbackFunction capture=null;capture=()=>{if(++frames<15)return;EditorApplication.update-=capture;Directory.CreateDirectory("Docs/QA");ScreenCapture.CaptureScreenshot("Docs/QA/"+state+"-"+w+"x"+h+".png");Debug.Log("BN screenshot queued: "+state);};EditorApplication.update+=capture;}
}
}
