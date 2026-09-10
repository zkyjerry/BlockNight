using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
namespace BlockNight {
public class GameHUD:MonoBehaviour {
 public TMP_Text countdown,score,time,chain,slowLabel,rewindLabel,status,banner,tutorialText,deathScore;
 public Image progress,slowFill,rewindFill;public GameObject readouts;public GameObject titlePanel,pausePanel,draftPanel,deathPanel,tutorialPanel;
 public Button startButton,learnButton,resumeButton,retryButton,homeButton,skipButton;public Button[] cards;public TMP_Text[] cardTexts;
 GameDirector director;float bannerUntil;
 public static readonly string[] Names={"超驱","轻步","时井","快充","深忆","动量","复利","静域","残像","护盾","积分补给"};
 public static readonly string[] Descriptions={"冲刺速度 +12%\n更快穿越战场。","走格速度 +20%\n更快调整下一次斩击位置。","慢时间持续 +0.6 秒\n延长爆发前的悬念。","慢时间冷却 -1.5 秒\n更频繁地折叠时间。","回溯冷却 -2 秒\n让下一次机会更早到来。","击杀推进强化 +0.35 秒\n以战养战，加速成长。","击杀积分 +25%\n每一次斩击都更值钱。","慢速世界倍率 -4%\n让危险格进一步减速。","回溯后保护 +0.25 秒\n留出改写选择的空间。","冲刺时免疫一次任意方向伤害\n消耗后可再次获得，最多一个。","积分 +2,500\n所有系统已强化至满级。"};
 public void Bind(GameDirector d){director=d;if(startButton)startButton.onClick.AddListener(()=>d.Begin(false,true));if(learnButton)learnButton.onClick.AddListener(()=>d.Begin(true,true));resumeButton.onClick.AddListener(d.Pause);if(retryButton)retryButton.onClick.AddListener(()=>d.Begin(false,true));if(homeButton)homeButton.onClick.AddListener(d.Home);skipButton.onClick.AddListener(()=>d.Begin(false,true));for(int i=0;i<3;i++){int k=i;cards[i].onClick.AddListener(()=>d.Choose(k));}}
 public void SetOffers(int[] ids,int[] levels){for(int i=0;i<3;i++){int id=ids[i];cardTexts[i].text="0"+(i+1)+" / 强化\n\n"+Names[id]+"\n\n"+Descriptions[id]+"\n\n"+(id==9?"持有上限 1 个":id<10?"等级 "+(levels[id]+1)+" / "+CombatModel.MaxLevel(id):"补给");}}
 public void RewardPunch(){progress.transform.DOKill();progress.transform.localScale=Vector3.one;progress.transform.DOPunchScale(new Vector3(0,.28f,0),.18f,1);}
 public void Banner(string s){banner.text=s;bannerUntil=Time.unscaledTime+1.7f;}
 public void Render(GameDirector d,bool historyReady){var m=d.model;readouts.SetActive(d.mode!=Mode.Title);if(titlePanel)titlePanel.SetActive(d.mode==Mode.Title);pausePanel.SetActive(d.mode==Mode.Paused);draftPanel.SetActive(d.mode==Mode.Draft);if(deathPanel)deathPanel.SetActive(d.mode==Mode.Dead);tutorialPanel.SetActive(m.tutorial&&!d.lessons&&d.mode!=Mode.Title);
 countdown.text=Mathf.Max(0,m.reward).ToString("00.0")+"<size=22> 秒</size>";progress.fillAmount=1-m.reward/d.balance.upgradeSeconds;score.text=m.score.ToString("N0");time.text=((int)m.elapsed/60).ToString("00")+":"+((int)m.elapsed%60).ToString("00");chain.text=m.chain.ToString("00");
 slowLabel.text=m.slow>0?"生效中 "+m.slow.ToString("0.0")+"秒":m.slowCD>0?m.slowCD.ToString("0.0")+"秒":"就绪";rewindLabel.text=m.rewindCD>0?m.rewindCD.ToString("0.0")+"秒":m.dashing?"斩击锁定":historyReady?"就绪":"记录中";slowFill.fillAmount=m.slow>0?m.slow/(d.balance.slowSeconds+.6f*m.levels[2]):1-m.slowCD/d.balance.slowCooldown;rewindFill.fillAmount=1-m.rewindCD/d.balance.rewindCooldown;
 status.text=d.mode==Mode.Rewinding?"<<< 正在回溯":d.mode==Mode.Dying?"按 L / 改写命运":m.slow>0?"时间折叠 / 斩击蓄势待发":m.dashing?"斩击中 / WASD 可转向": m.PhaseName + " / WASD 转向·走格　J 斩击　K 折叠　L 回溯";banner.enabled=Time.unscaledTime<bannerUntil;
 if(m.levels[9]>0)status.text+=" / 盾 "+(m.shieldArmed?"展开":"就绪");
 if(deathScore)deathScore.text="最终积分  "+m.score.ToString("N0")+"\n\n"+m.kills+" 击杀 / 最佳连斩 "+m.bestChain+"\n\n存活时间 "+time.text;
 if(m.tutorial&&!d.lessons){string[] lines={"01 / 转向　按 D 朝右转身。\n朝向不同只转身，仍停在当前格子。","02 / 走格　再按一次 D，向右走一格。\n同方向按键才走格；普通移动不会杀敌。","03 / 斩击　待目标成形，按 W 朝上，再按 J。\n冲刺中可用 WASD 转向；撞到棋盘边缘才收刀。","04 / 折叠　按 K，观看慢时间斩杀演示。\n你保持全速；残影会在技能结束时一起爆发。","05 / 观察　等待残影集中释放。\n连斩 3、5、7 杀时，粒子与亮角逐档增强。","06 / 回溯　按 L，将整个战场倒带。\n回溯恢复格子和朝向；技能冷却不会恢复。","07 / 出发　按回车开始正式战斗。\nJ 斩击，K 折叠，L 回溯；一击即死，濒死可按 L 自救。"};tutorialText.text=lines[Mathf.Clamp(d.tutorialStep,0,6)];}
 }
}
}
