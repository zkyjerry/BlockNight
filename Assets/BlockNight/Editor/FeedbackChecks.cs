using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
namespace BlockNight.Editor {
 public static class FeedbackChecks {
  static void Check(bool value,string label){if(!value)throw new Exception("V6 FAIL: "+label);}
  static GameDirector Game(){var d=UnityEngine.Object.FindObjectOfType<GameDirector>();if(!EditorApplication.isPlaying||!d)throw new Exception("Requires combat Play Mode");d.enabled=false;d.routeScenes=false;return d;}
  [MenuItem("Block Night/QA/Run v6 Feedback Checks")]
  public static void Run(){var d=Game();d.Begin(false);var f=d.feedback;var m=d.model;
   Check(f&&f.settings&&f.focusCamera&&f.brain&&f.countdownRing&&f.readyFlash,"serialized references");
   m.tutorial=true;for(int i=0;i<6;i++)m.Spawn(i%4,CombatModel.Center(new Vector2Int(i+1,6)));m.reward=30;m.StartUpgradeWave(.65f);for(int i=0;i<70;i++)m.Step(.01f);
   Check(m.kills==6&&m.score==975&&Mathf.Abs(m.reward-22.8f)<.001f&&!m.foes.Any(x=>x.active),"wave exactly six kills, 975 points, 7.2 seconds");int score=m.score;for(int i=0;i<70;i++)m.Step(.01f);Check(m.score==score,"no duplicate rewards");
   d.Begin(false);m=d.model;m.tutorial=true;m.Spawn(0,CombatModel.Center(new Vector2Int(7,7)));m.StartUpgradeWave(.65f);m.Step(.1f);var saved=m.Capture();m.Step(.2f);m.Restore(saved);Check(m.upgradeWaveRadius==saved.upgradeWaveRadius&&m.upgradeWaveActive,"wave included in rewind snapshot");
   bool old=f.settings.upgradeShockwave;try{foreach(bool enabled in new[]{false,true}){f.settings.upgradeShockwave=enabled;d.Begin(false);d.model.grace=10;d.model.reward=0;d.Tick(.01f);Check(d.mode==Mode.Draft,"draft opens for switch check");d.Choose(0);Check(d.model.upgradeWaveActive==enabled,"SO switch controls wave");}}finally{f.settings.upgradeShockwave=old;}
   d.Begin(false);m=d.model;m.tutorial=true;m.Spawn(0,CombatModel.Center(new Vector2Int(2,5)));m.foes[0].age=3;m.foes[0].attackWindup=true;m.foes[0].attackTarget=new Vector2Int(3,5);m.foes[0].timer=m.RulesFor(0).attackWarning;d.arena.Render(m);float start=Vector3.Distance(d.arena.aims[0].GetPosition(0),d.arena.aims[0].GetPosition(1));m.foes[0].timer=0;d.arena.Render(m);float end=Vector3.Distance(d.arena.aims[0].GetPosition(0),d.arena.aims[0].GetPosition(1));Check(end>start*4.9f,"warning grows fivefold");
   m.reward=m.settings.upgradeSeconds;f.Render(m,0);Check(f.countdownRing.positionCount==129&&f.RemainingFraction==1,"full border");m.reward=m.settings.upgradeSeconds*.5f;f.Render(m,0);Check(f.countdownRing.positionCount==65&&f.countdownRing.GetPosition(0).y<0,"half countdown disappears counterclockwise");m.reward=0;f.Render(m,0);Check(!f.countdownRing.enabled,"zero hides border");
   d.Begin(false);m=d.model;m.tutorial=true;m.slowCD=.02f;m.rewindCD=.03f;int k=f.slowReadyCount,l=f.rewindReadyCount;d.Tick(.04f);Check(f.slowReadyCount==k+1&&f.rewindReadyCount==l+1,"each ready edge fires");for(int i=0;i<30;i++)d.Tick(.02f);Check(f.slowReadyCount==k+1&&f.rewindReadyCount==l+1&&!f.readyFlash.enabled,"no repeated ready flash");
   d.arena.ClearEffects();d.arena.ShieldBreak(m.player);Check(d.arena.shards.particleCount>=220&&d.arena.shockwaves.Count(x=>x.enabled)>=2,"strong shield break");
   d.Begin(false);d.model.tutorial=true;for(int i=0;i<40;i++)d.Tick(.025f);d.model.player=CombatModel.Center(new Vector2Int(1,1));d.model.cell=d.model.target=new Vector2Int(1,1);d.model.hit=true;d.Tick(.02f);for(int i=0;i<10;i++)d.Tick(.02f);Check(d.mode==Mode.Dying&&d.danger>1,"two-second rescue window");d.arena.volume.profile.TryGet<Vignette>(out var v);Check(v.intensity.value>.6f&&v.color.value.r>v.color.value.g*10,"deep red pressure vignette");
   double deadline=EditorApplication.timeSinceStartup+.6;EditorApplication.CallbackFunction stage=null;stage=()=>{if(EditorApplication.timeSinceStartup<deadline)return;EditorApplication.update-=stage;try{
    Check(Mathf.Abs(f.outputCamera.orthographicSize-f.settings.focusSize)<.05f&&Vector2.Distance(f.outputCamera.transform.position,d.arena.World(d.model.player))<.05f,"Cinemachine output focuses player");Check(d.Rewind(),"rescue rewind accepted");for(int i=0;i<40;i++)d.Tick(.025f);deadline=EditorApplication.timeSinceStartup+.6;EditorApplication.CallbackFunction restored=null;restored=()=>{if(EditorApplication.timeSinceStartup<deadline)return;EditorApplication.update-=restored;try{Check(!f.brain.enabled&&Mathf.Abs(f.outputCamera.orthographicSize-7.7f)<.05f&&((Vector2)f.outputCamera.transform.position).magnitude<.05f,"camera restored and DOTween control returned");Check(v.intensity.value<.3f,"vignette restored after rewind");File.WriteAllText("Docs/QA/v6-feedback-checks.txt","PASS: wave switch/scoring/timer/no duplicates/snapshot; growing warning; CCW border; one-shot skill ready; 220 shield shards/two rings; two-second danger; actual Cinemachine position and size; rewind restoration.\n"+DateTime.Now.ToString("s"));d.Begin(false);}catch(Exception e){Fail(e);}};EditorApplication.update+=restored;
   }catch(Exception e){Fail(e);}};EditorApplication.update+=stage;
  }
  static void Fail(Exception e){File.WriteAllText("Docs/QA/v6-feedback-checks.txt",e.ToString());Debug.LogException(e);}
  [MenuItem("Block Night/QA/v6 Combat Fixture")]
  public static void Combat(){V4RuntimeChecks.CombatFixture();var d=Game();d.model.foes[0].timer=.32f;d.model.foes[1].timer=.08f;d.arena.Render(d.model);d.feedback.Render(d.model,0);}
  [MenuItem("Block Night/QA/v6 Dying Fixture")]
  public static void Dying(){Combat();var d=Game();d.model.player=CombatModel.Center(new Vector2Int(1,2));d.model.hit=true;d.mode=Mode.Dying;d.danger=2;d.arena.Render(d.model);d.feedback.Focus(true,d.model.player);for(int i=0;i<20;i++)d.arena.TimeEffect(false,false,true,.03f);d.hud.Render(d,true);}
  [MenuItem("Block Night/QA/v6 Shield Break Fixture")]
  public static void ShieldBreak(){Combat();var d=Game();d.arena.ClearEffects();d.arena.ShieldBreak(d.model.player);d.arena.shards.Pause();d.arena.shards.Simulate(.1f,true,false);d.arena.shards.Pause();d.arena.TimeEffect(false,false,false,.04f);}
  [MenuItem("Block Night/QA/v6 Upgrade Wave Fixture")]
  public static void Wave(){Combat();var d=Game();d.model.StartUpgradeWave(d.feedback.settings.shockwaveSeconds,d.feedback.settings.shockwaveRadius);d.model.grace=1;d.model.Step(.23f);d.arena.Render(d.model);d.feedback.Render(d.model,0);d.hud.Render(d,true);}
  [MenuItem("Block Night/QA/v6 Skill Ready Fixture")]
  public static void Ready(){Combat();var d=Game();d.feedback.Ready(true);d.feedback.Render(d.model,.21f);}
 }
}
