using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
namespace BlockNight {
public class ButtonFeel:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerClickHandler {
        const float scale = 1.235f;
        const float rotation = 1.5f;
        const float duration = .16f;
    public void OnPointerEnter(PointerEventData e){
            if(SynthAudio.Current)SynthAudio.Current.Ui(false);
            transform.DOKill();transform.DOScale(scale,duration).SetUpdate(true);transform.DOShakeRotation(duration, new Vector3(0, 0, rotation), 10, 90).SetUpdate(true);
            }
    public void OnPointerExit(PointerEventData e){
        transform.DOKill();transform.DOScale(1,duration).SetUpdate(true); transform.DOShakeRotation(duration, new Vector3(0, 0, rotation), 10, 90).SetUpdate(true).OnComplete(() => { transform.localRotation = Quaternion.identity; }); 
        }
    public void OnPointerClick(PointerEventData e){
        if(SynthAudio.Current)SynthAudio.Current.Ui(true);
        transform.DOKill();transform.DOPunchScale(Vector3.one * -.06f, scale * 2, 1).SetUpdate(true);
        }
    void OnDisable(){
        transform.DOKill();transform.localScale=Vector3.one;transform.localRotation = Quaternion.identity;
        }
    }
}
