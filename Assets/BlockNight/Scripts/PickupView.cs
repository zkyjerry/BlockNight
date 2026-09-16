using UnityEngine;
namespace BlockNight {
 public enum PickupKind { 时间暂停=0, 时间回溯=1 }
 [System.Serializable] public struct Pickup {
  public bool active;
  public PickupKind kind;
  public Vector2Int cell;
 }
 public class PickupView:MonoBehaviour {
  public PickupKind kind;
  public SpriteRenderer body;
 }
}
