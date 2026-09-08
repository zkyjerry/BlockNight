using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
namespace BlockNight {
public class GameHUD:MonoBehaviour {
 public Text countdown,score,time,chain,slowLabel,rewindLabel,status,banner,tutorialText,deathScore;
 public Image progress,slowFill,rewindFill;public GameObject readouts;public GameObject titlePanel,pausePanel,draftPanel,deathPanel,tutorialPanel;
 public Button startButton,learnButton,resumeButton,retryButton,homeButton,skipButton;public Button[] cards;public Text[] cardTexts;
 GameDirector director;float bannerUntil;
 public static readonly string[] Names={"超驱","宽刃","时井","快充","深忆","动量","复利","静域","残像","积分补给"};
 public static readonly string[] Descriptions={"冲刺速度 +12%\n更快穿越战场。","斩击半径 +0.08\n每次转向，收割更多。","慢时间持续 +0.6 秒\n延长爆发前的悬念。","慢时间冷却 -1.5 秒\n更频繁地折叠时间。","回溯冷却 -2 秒\n让下一次机会更早到来。","击杀推进强化 +0.35 秒\n以战养战，加速成长。","击杀积分 +25%\n每一次斩击都更值钱。","慢速世界倍率 -4%\n让弹幕进一步静止。","回溯后保护 +0.25 秒\n留出改写选择的空间。","积分 +2,500\n所有系统已强化至满级。"};
 public void Bind(GameDirector d){director=d;startButton.onClick.AddListener(()=>d.Begin(false));learnButton.onClick.AddListener(()=>d.Begin(true));resumeButton.onClick.AddListener(d.Pause);retryButton.onClick.AddListener(()=>d.Begin(false));homeButton.onClick.AddListener(d.Home);skipButton.onClick.AddListener(()=>d.Begin(false));for(int i=0;i<3;i++){int k=i;cards[i].onClick.AddListener(()=>d.Choose(k));}}
 public void SetOffers(int[] ids,int[] levels){for(int i=0;i<3;i++){int id=ids[i];cardTexts[i].text="0"+(i+1)+" / 强化\n\n"+Names[id]+"\n\n"+Descriptions[id]+"\n\n"+(id<9?"等级 "+(levels[id]+1)+" / 3":"补给");}}
 public void RewardPunch(){progress.transform.DOKill();progress.transform.localScale=Vector3.one;progress.transform.DOPunchScale(new Vector3(0,.28f,0),.18f,1);}
 public void Banner(string s){banner.text=s;bannerUntil=Time.unscaledTime+1.7f;}
 public void Render(GameDirector d,bool historyReady){var m=d.model;readouts.SetActive(d.mode!=Mode.Title);titlePanel.SetActive(d.mode==Mode.Title);pausePanel.SetActive(d.mode==Mode.Paused);draftPanel.SetActive(d.mode==Mode.Draft);deathPanel.SetActive(d.mode==Mode.Dead);tutorialPanel.SetActive(m.tutorial&&d.mode!=Mode.Title);
 countdown.text=m.reward.ToString("00.0")+"<size=22> 秒</size>";progress.fillAmount=1-m.reward/d.balance.upgradeSeconds;score.text=m.score.ToString("N0");time.text=((int)m.elapsed/60).ToString("00")+":"+((int)m.elapsed%60).ToString("00");chain.text=m.chain.ToString("00");
 slowLabel.text=m.slow>0?"生效中 "+m.slow.ToString("0.0")+"秒":m.slowCD>0?m.slowCD.ToString("0.0")+"秒":"就绪";rewindLabel.text=m.rewindCD>0?m.rewindCD.ToString("0.0")+"秒":historyReady?"就绪":"记录中";slowFill.fillAmount=m.slow>0?m.slow/(d.balance.slowSeconds+.6f*m.levels[2]):1-m.slowCD/d.balance.slowCooldown;rewindFill.fillAmount=1-m.rewindCD/d.balance.rewindCooldown;
 status.text=d.mode==Mode.Rewinding?"<<< 正在回溯":d.mode==Mode.Dying?"按 Q / 改写命运":m.slow>0?"时间折叠 / 斩击蓄势待发":m.elapsed%45>38?"喘息阶段 / 调整战斗节奏":"WASD 移动与转向 / ESC 暂停";banner.enabled=Time.unscaledTime<bannerUntil;
 deathScore.text="最终积分  "+m.score.ToString("N0")+"\n\n"+m.kills+" 击杀 / 最佳连斩 "+m.bestChain+"\n\n存活时间 "+time.text;
 if(m.tutorial){string[] lines={"01 / 移动　按 W 向目标冲刺。\nWASD 可随时转向；撞到边缘才会结束斩击。","02 / 斩杀　穿过红色方块即可将其击杀。\n冲刺途中可以转向，只有敌人的攻击才会伤到你。","03 / 慢时间　按空格。\n你保持全速，世界放慢；斩杀特效在技能结束时爆发。","04 / 观察释放　刚刚斩杀的敌人留下了残影。\n等待慢时间结束，所有斩击将集中爆发。","05 / 回溯　移动一段距离后按 Q。\n整个战场会倒带，技能冷却不会随之恢复。","06 / 出发　一击即死，濒死时按 Q 自救。\n击杀可加速获得强化。按回车开始正式战斗。"};tutorialText.text=lines[Mathf.Clamp(d.tutorialStep,0,5)];}
 }
}
}
