using System;
using UnityEngine;
namespace BlockNight {
 public enum EnemyAction { 逐格撞击, 斜向冲击波 }
 [Serializable] public class EnemyRules {
  public EnemyAction action;
  public Vector2Int direction=Vector2Int.right;
  [Min(.2f)] public float spawnWarning=1.2f;
  [Min(.3f)] public float attackInterval=2.4f;
  [Min(.1f)] public float attackWarning=.65f;
  [Min(.3f)] public float moveInterval=3.2f;
  [Min(.1f)] public float moveWarning=.6f;
  [Range(1,8)] public int waveRange=4;
  [Min(.1f)] public float waveCellWarning=.28f;
  [Min(.05f)] public float impactDuration=.16f;
  public bool reverseAtEdge=true;
  public Vector2Int Heading { get {var d=direction;if(action==EnemyAction.斜向冲击波)return new Vector2Int(d.x<0?-1:1,d.y<0?-1:1);if(d==Vector2Int.zero)return Vector2Int.right;return Mathf.Abs(d.x)>=Mathf.Abs(d.y)?new Vector2Int(d.x<0?-1:1,0):new Vector2Int(0,d.y<0?-1:1); } }
  public void Clamp(){spawnWarning=Mathf.Max(.2f,spawnWarning);attackWarning=Mathf.Max(.1f,attackWarning);attackInterval=Mathf.Max(attackWarning+.1f,attackInterval);moveWarning=Mathf.Max(.1f,moveWarning);moveInterval=Mathf.Max(moveWarning+.1f,moveInterval);waveRange=Mathf.Clamp(waveRange,1,8);waveCellWarning=Mathf.Max(.1f,waveCellWarning);impactDuration=Mathf.Max(.05f,impactDuration);direction=Heading;}
 }
 [CreateAssetMenu(menuName="Block Night/敌人属性",fileName="EnemyDefinition")]
 public class EnemyDefinition:ScriptableObject {
  public string displayName="新敌人";
  public EnemyRules rules=new EnemyRules();
  void OnValidate(){if(rules==null)rules=new EnemyRules();rules.Clamp();}
 }
}
