using UnityEngine;
using TMPro;
using Febucci.UI;
using UnityEngine.UI;

namespace BlockNight
{
    public class MenuController : MonoBehaviour
    {
        public bool isGameOver;
        public Button primaryButton, secondaryButton;
        public TMP_Text resultText;
        public SynthAudio audioBus;
        public TextAnimatorPlayer titleAnimation, resultAnimation, primaryAnimation, secondaryAnimation;
        void Awake()
        {
            primaryButton.onClick.AddListener(() => SceneFlow.StartRun(false));
            if (isGameOver)
            {
                secondaryButton.onClick.AddListener(SceneFlow.ToMenu);
                resultText.text = "最终积分  " + SceneFlow.Score.ToString("N0") + "\n\n" + SceneFlow.Kills + " 击杀 / 最佳连斩 " + SceneFlow.BestChain + "\n\n存活时间 " + ((int)SceneFlow.Elapsed / 60).ToString("00") + ":" + ((int)SceneFlow.Elapsed % 60).ToString("00");
            }
            else secondaryButton.onClick.AddListener(() => SceneFlow.StartRun(true));
        }
        void Update()
        {
            GameInput.Poll();
            if (GameInput.Down(KeyCode.Return)) SceneFlow.StartRun(false);
            if (GameInput.Down(KeyCode.M)) audioBus.ToggleMute();
            audioBus.Tick(0, 1, !isGameOver, Time.unscaledDeltaTime);
        }
        void Start() {
            if(isGameOver)audioBus.Cue(6);
            if(titleAnimation){string plain=titleAnimation.textAnimator.tmproText.text;titleAnimation.ShowText("{fade}"+(isGameOver?"<wave a=0.05 f=0.35>":"<wave a=0.1 f=0.3>")+plain+"</wave>{/fade}");}
            if(resultAnimation)resultAnimation.ShowText("{fade}"+resultText.text+"{/fade}");
            if(primaryAnimation)primaryAnimation.ShowText("{fade}"+primaryAnimation.textAnimator.tmproText.text+"{/fade}");
            if(secondaryAnimation)secondaryAnimation.ShowText("{fade}"+secondaryAnimation.textAnimator.tmproText.text+"{/fade}");
        }
    }
}
