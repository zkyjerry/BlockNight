using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
namespace BlockNight {
public class ButtonFeel:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerClickHandler {
 public void OnPointerEnter(PointerEventData e){transform.DOKill();transform.DOScale(1.035f,.13f).SetUpdate(true);}
 public void OnPointerExit(PointerEventData e){transform.DOKill();transform.DOScale(1,.13f).SetUpdate(true);}
 public void OnPointerClick(PointerEventData e){transform.DOKill();transform.DOPunchScale(Vector3.one*-.06f,.18f,1).SetUpdate(true);}
 void OnDisable(){transform.DOKill();transform.localScale=Vector3.one;}
}
}
