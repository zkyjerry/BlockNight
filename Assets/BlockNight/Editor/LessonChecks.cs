using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace BlockNight.Editor {
 public static class LessonChecks {
  static GameDirector d;static void Check(bool ok,string s){if(!ok)throw new Exception("LESSON FAIL: "+s);}
  static void Frames(int count){for(int i=0;i<count;i++)d.Tick(.02f);}
  static void Dialogue(){int guard=0;while(d.lessons.DialogueWaiting&&guard++<10)d.lessons.AdvanceDialogue();Check(guard<10,"dialogue finite");}
  static void Until(LessonStage stage,int limit=250){for(int i=0;i<limit&&d.lessons.stage!=stage;i++)d.Tick(.02f);Check(d.lessons.stage==stage,"reached "+stage+" actual "+d.lessons.stage);}
  static void Get(){d=UnityEngine.Object.FindObjectOfType<GameDirector>();if(!EditorApplication.isPlaying||!d)throw new Exception("Requires combat Play Mode");d.enabled=false;d.routeScenes=false;}
  [MenuItem("Block Night/QA/Run Three Lesson Checks")]
  public static void Run(){Get();try{
   d.Begin(true);var f=d.lessons;var origin=d.model.player;d.MoveOrTurn(Vector2.right);Check(d.model.player==origin&&d.model.direction==Vector2.up,"dialogue blocks actions");Check(f.textPlayer&&f.dialogueText.GetComponent<Febucci.UI.TextAnimator>(),"TextAnimator dialogue");Dialogue();
   d.MoveOrTurn(Vector2.right);d.Tick(.02f);Check(f.stage==LessonStage.Walk&&!d.model.moving,"turn lesson");Dialogue();d.MoveOrTurn(Vector2.right);Frames(20);Check(f.stage==LessonStage.Slash&&d.model.cell.x==4,"walk lesson");Frames(80);d.MoveOrTurn(Vector2.up);Check(d.StartSlash(),"manual slash starts");Until(LessonStage.ObserveNormal);Check(d.model.kills==3,"three introductory kills");Dialogue();Until(LessonStage.SlowKey);
   float before=d.model.foes[0].timer;d.Tick(.05f);float normal=before-d.model.foes[0].timer;Check(normal>.049f,"normal warning progresses visibly");Check(d.ActivateSlow()&&!d.model.dashing,"K does not automatically slash");before=d.model.foes[0].timer;d.Tick(.05f);float slow=before-d.model.foes[0].timer;Check(Mathf.Abs(slow/normal-.18f)<.001f,"warning growth slows to 18 percent");Check(d.StartSlash(),"player manually slashes in slow time");Until(LessonStage.Release);Check(d.model.foes[0].pending,"kill animation deferred");Until(LessonStage.Impact);Dialogue();Until(LessonStage.RewindKey);Check(d.model.hit&&d.mode==Mode.Dying,"real enemy impact enters frozen death");float deathWindow=d.danger;Frames(300);Check(d.mode==Mode.Dying&&d.danger==deathWindow,"reading dialogue cannot time out death");Dialogue();Frames(300);Check(d.mode==Mode.Dying&&d.danger==deathWindow,"awaiting L cannot time out");Check(d.Rewind(),"rewind from real lethal hit");Until(LessonStage.RewindKill);Check(!d.model.hit&&d.model.foes[0].active,"rewind restored living attacker and player");Check(d.StartSlash(),"player changes choice by slashing");Until(LessonStage.Complete);Check(d.model.kills>3&&!d.model.hit,"attacker killed after rewind");Dialogue();Check(d.mode==Mode.Countdown&&!d.model.tutorial,"completion enters formal intro");
   float elapsed=d.model.elapsed,reward=d.model.reward;Check(f.countdownPanel.GetComponent<Image>().color.a==1,"opaque black mask");for(int index=0;index<4;index++){Check(f.CountdownIndex==index&&f.countdownText.text==new[]{"3","2","1","活下去！"}[index],"countdown text order");d.MoveOrTurn(Vector2.right);Check(!d.model.moving&&!d.StartSlash(),"intro blocks movement and attack");f.TickCountdown(.8f);}
   Check(d.mode==Mode.Playing&&!f.countdownPanel.activeSelf&&d.model.elapsed==elapsed&&d.model.reward==reward,"intro ends without advancing combat");
   File.WriteAllText("Docs/QA/tutorial-v7-checks.txt","PASS: turn/walk/manual slash; real warning at 1x then 0.18x; manual K then J and deferred release; real lethal ram; unlimited reading/L wait; rewind then attacker kill; ordered opaque 3/2/1/survive intro; no combat during intro.\n"+DateTime.Now.ToString("s"));d.Begin(true);
  }catch(Exception e){File.WriteAllText("Docs/QA/tutorial-v7-checks.txt",e.ToString());Debug.LogException(e);}}
  [MenuItem("Block Night/QA/Check Tutorial Any Key")]
  public static void AnyKey(){Get();var previous=UnityEngine.InputSystem.Keyboard.current;var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("LessonQAKeyboard");try{
   keyboard.MakeCurrent();UnityEngine.InputSystem.LowLevel.InputState.Change(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());GameInput.Poll();d.Begin(true);
   UnityEngine.InputSystem.LowLevel.InputState.Change(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Q));GameInput.Poll();Check(GameInput.AnyDown,"Q detected outside combat key map");d.Tick(.02f);Check(d.lessons.dialogueText.text.Contains("朝向相同时"),"Q advances to next dialogue");GameInput.Poll();d.Tick(.02f);Check(d.lessons.DialogueWaiting,"holding Q does not skip more lines");
   UnityEngine.InputSystem.LowLevel.InputState.Change(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());GameInput.Poll();UnityEngine.InputSystem.LowLevel.InputState.Change(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.D));GameInput.Poll();d.Tick(.02f);Check(!d.lessons.DialogueWaiting&&d.model.direction==Vector2.up&&!d.model.moving,"dialogue D consumed without turning");
   File.WriteAllText("Docs/QA/tutorial-any-key.txt","PASS: Input System keyboard Q advances dialogue; held key does not repeat; D closes dialogue without triggering gameplay.");
  }finally{UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);if(previous!=null)previous.MakeCurrent();GameInput.Poll();d.Begin(true);}}
  [MenuItem("Block Night/QA/Tutorial Dialogue Fixture")]
  public static void DialogFixture(){Get();d.Begin(true);}
  [MenuItem("Block Night/QA/Tutorial Slow Fixture")]
  public static void SlowFixture(){Get();d.Begin(true);Dialogue();d.MoveOrTurn(Vector2.right);d.Tick(.02f);Dialogue();d.MoveOrTurn(Vector2.right);Frames(100);d.MoveOrTurn(Vector2.up);d.StartSlash();Until(LessonStage.ObserveNormal);Dialogue();Until(LessonStage.SlowKey);d.ActivateSlow();Frames(15);}
  [MenuItem("Block Night/QA/Tutorial Death Fixture")]
  public static void DeathFixture(){SlowFixture();d.StartSlash();Until(LessonStage.Release);Until(LessonStage.Impact);Dialogue();Until(LessonStage.RewindKey);}
  [MenuItem("Block Night/QA/Run Intro Fixture")]
  public static void IntroFixture(){Get();d.Begin(false,true);d.lessons.TickCountdown(2.4f);}
 }
}
