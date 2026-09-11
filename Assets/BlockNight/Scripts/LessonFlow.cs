using System;
using UnityEngine;
using TMPro;
using Febucci.UI;
using DG.Tweening;
namespace BlockNight {
 public enum LessonStage { Turn,Walk,Slash,ObserveNormal,SlowKey,SlowAttack,Release,Impact,RewindKey,RewindKill,Complete }
 public class LessonFlow:MonoBehaviour {
  public GameDirector director;
  public GameObject dialoguePanel,countdownPanel;
  public TMP_Text dialogueText,dialogueHint,countdownText;
  public TextAnimatorPlayer textPlayer;
  public EnemyDefinition slowEnemy,rewindEnemy;
  public LessonStage stage;
  public bool DialogueWaiting {get;private set;}
  public int CountdownIndex {get;private set;}
  public bool HoldDeath=>director.model.tutorial&&stage==LessonStage.RewindKey;
  public bool CanMove=>!DialogueWaiting&&(stage==LessonStage.Turn||stage==LessonStage.Walk||stage==LessonStage.Slash||stage==LessonStage.SlowAttack||stage==LessonStage.RewindKill);
  public bool CanSlash=>!DialogueWaiting&&(stage==LessonStage.Slash||stage==LessonStage.SlowAttack||stage==LessonStage.RewindKill);
  public bool CanSlow=>!DialogueWaiting&&stage==LessonStage.SlowKey;
  public bool CanRewind=>!DialogueWaiting&&stage==LessonStage.RewindKey;
  string[] dialogue;int line;Action afterDialogue;float stageTime,countdownClock;int startingKills;bool sawRewinding;
  static readonly string[] CountdownLines={"3","2","1","活下去！"};
  public void ResetFlow(){DialogueWaiting=false;afterDialogue=null;dialoguePanel.SetActive(false);countdownPanel.SetActive(false);countdownText.transform.DOKill();sawRewinding=false;}
  public void StartCountdown(){ResetFlow();director.mode=Mode.Countdown;CountdownIndex=-1;countdownClock=0;countdownPanel.SetActive(true);ShowCount(0);}
  void ShowCount(int index){CountdownIndex=index;countdownText.text=CountdownLines[index];countdownText.fontSize=index==3?126:240;countdownText.transform.DOKill();countdownText.transform.localScale=Vector3.one*1.25f;countdownText.transform.DOScale(1,.35f).SetEase(Ease.OutCubic).SetUpdate(true);director.audioBus.Cue(index==3?3:1,index==3?5:1);}
  public void TickCountdown(float dt){countdownClock+=dt;int index=Mathf.FloorToInt(countdownClock/.8f);if(index>=4){countdownPanel.SetActive(false);director.mode=Mode.Playing;return;}if(index!=CountdownIndex)ShowCount(index);}
  public void StartLesson(){ResetFlow();stage=LessonStage.Turn;director.tutorialStep=0;Say(new[]{"第一课 / 移动与斩击\nWASD 控制朝向。朝向不同，只在当前格转身。","朝向相同时，再按一次就走一格。\n先按 D 朝右转身。"});}
  void Say(string[] lines,Action after=null){dialogue=lines;line=0;afterDialogue=after;DialogueWaiting=true;ShowLine(lines[0],"任意键 / 下一条");}
  void ShowLine(string text,string hint){dialoguePanel.SetActive(true);dialogueHint.text=hint;textPlayer.ShowText("{fade}"+text+"{/fade}");}
  void Prompt(string text){DialogueWaiting=false;ShowLine(text,"按提示完成操作");}
  public void AdvanceDialogue(){if(!DialogueWaiting)return;if(++line<dialogue.Length){ShowLine(dialogue[line],"任意键 / 下一条");return;}DialogueWaiting=false;dialogueHint.text="按提示完成操作";var next=afterDialogue;afterDialogue=null;next?.Invoke();}
  public bool BeforeTick(float dt){if(!director.model.tutorial)return false;if(DialogueWaiting){if(GameInput.AnyDown)AdvanceDialogue();return true;}return false;}
  public void AfterTick(float dt){
   var m=director.model;if(!m.tutorial)return;director.tutorialStep=(int)stage;
   if(stage==LessonStage.Turn&&m.direction==Vector2.right){stage=LessonStage.Walk;Say(new[]{"很好，已经朝右。\n再按一次 D，向右走一格。"});}
   else if(stage==LessonStage.Walk&&!m.moving&&m.cell.x==4){stage=LessonStage.Slash;m.Spawn(0,CombatModel.Center(new Vector2Int(4,4)));m.Spawn(0,CombatModel.Center(new Vector2Int(5,4)));m.Spawn(0,CombatModel.Center(new Vector2Int(6,4)));Prompt("朝上按 W，再按 J 冲刺。\n接近第一个敌人时按 D：抵达它所在格后会转向右边，完成三连斩。");}
   else if(stage==LessonStage.Slash&&m.kills>=3&&!m.dashing){Say(new[]{"一次冲刺可以贯穿多个敌人。\n接下来观察敌人的攻击预警。"},SetupSlow);stage=LessonStage.ObserveNormal;}
   else if(stage==LessonStage.ObserveNormal&&!DialogueWaiting){stageTime+=dt;if(stageTime>=.7f){stage=LessonStage.SlowKey;Prompt("第二课 / 时间折叠\n红框正在正常速度变大。现在按 K，比较它的速度。");}}
   else if(stage==LessonStage.SlowKey){if(m.foes[0].timer<.3f){m.foes[0].timer=slowEnemy.rules.attackWarning;m.foes[0].attackWindup=true;}}
   else if(stage==LessonStage.SlowAttack){if(m.kills>startingKills){stage=LessonStage.Release;Prompt("预警变慢了，你的斩击仍然很快。\n等待时间恢复，看被击杀的敌人一起碎裂。");}else if(m.slow<=0&&!m.dashing){Say(new[]{"折叠结束了，再试一次。\n按 K 后，亲自按 J 消灭正上方的敌人。"},SetupSlow);stage=LessonStage.ObserveNormal;}}
   else if(stage==LessonStage.Release&&m.slow<=0&&!m.dashing){stage=LessonStage.Impact;Say(new[]{"第三课 / 时间回溯\n接下来暂时不操作，观察敌人攻击命中的瞬间。"},SetupImpact);}
   else if((stage==LessonStage.Impact||stage==LessonStage.RewindKill)&&director.mode==Mode.Dying){stage=LessonStage.RewindKey;m.rewindCD=0;Say(new[]{"你被击中了。\n世界停在受击瞬间，红色边缘和镜头聚焦提示危险。","按 L 回溯到受击之前。\n这一次，你可以做出不同的选择。"},()=>Prompt("按 L / 改写这次死亡\n回到攻击发生之前。"));}
   else if(stage==LessonStage.RewindKey){if(director.mode==Mode.Rewinding)sawRewinding=true;if(sawRewinding&&director.mode==Mode.Playing){sawRewinding=false;stage=LessonStage.RewindKill;startingKills=m.kills;Prompt("你回到了被击中之前。\n现在按 J，消灭正上方的敌人，改写结果！");}}
   else if(stage==LessonStage.RewindKill&&m.kills>startingKills&&!m.dashing){stage=LessonStage.Complete;Say(new[]{"这次你活下来了。\n回溯让你重新选择，而不只是重看过去。","教学完成。\nWASD 转向与走格，J 斩击，K 折叠，L 回溯。"},()=>director.Begin(false,true));}
  }
  void Prepare(Vector2Int playerCell,EnemyDefinition definition,Vector2Int foeCell){var m=director.model;Array.Clear(m.foes,0,m.foes.Length);Array.Clear(m.shots,0,m.shots.Length);m.hit=false;m.draft=false;m.moving=m.dashing=false;m.player=CombatModel.Center(playerCell);m.cell=m.target=playerCell;m.travel=0;m.direction=Vector2.up;m.queuedDirection=Vector2.zero;m.slow=m.slowCD=m.rewindCD=m.grace=0;m.tutorialEnemyActions=true;m.enemyDefinitions=(EnemyDefinition[])director.enemyDefinitions.Clone();m.enemyDefinitions[0]=definition;m.Spawn(0,CombatModel.Center(foeCell));m.foes[0].age=definition.rules.spawnWarning;m.foes[0].timer=definition.rules.attackInterval;director.ClearHistory();director.arena.ClearEffects();director.feedback.ResetEffects();director.mode=Mode.Playing;startingKills=m.kills;stageTime=0;}
  void SetupSlow(){Prepare(new Vector2Int(3,1),slowEnemy,new Vector2Int(3,4));var m=director.model;m.foes[0].attackWindup=true;m.foes[0].attackTarget=new Vector2Int(4,4);m.foes[0].timer=slowEnemy.rules.attackWarning;stage=LessonStage.ObserveNormal;Prompt("先观察红色预警框变大的速度。\n它填满时，敌人会撞向右边的格子。");}
  void SetupImpact(){Prepare(new Vector2Int(3,3),rewindEnemy,new Vector2Int(3,4));stage=LessonStage.Impact;Prompt("先不操作，观察这次攻击。\n这次受击不会结束教学。");}
  public void SlowActivated(){stage=LessonStage.SlowAttack;Prompt("红框增长变慢了。你的速度没有变。\n现在按 J，冲刺消灭正上方的敌人。");}
 }
}
