using UnityEngine;
using UnityEditor;
namespace BlockNight.Editor {
 [CustomEditor(typeof(EnemyDefinition))]
 public class EnemyDefinitionEditor:UnityEditor.Editor {
  public override void OnInspectorGUI(){serializedObject.Update();EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"),new GUIContent("敌人名称"));var r=serializedObject.FindProperty("rules");
   Field(r,"action","行为类型");Field(r,"direction","固定方向（格坐标）");Field(r,"spawnWarning","出生预警（世界秒）");Field(r,"attackInterval","攻击间隔（世界秒）");Field(r,"attackWarning","攻击预警（世界秒）");Field(r,"impactDuration","危险格持续（世界秒）");Field(r,"reverseAtEdge","边界反向");
   if(r.FindPropertyRelative("action").enumValueIndex==(int)EnemyAction.斜向冲击波){Field(r,"moveInterval","移动间隔（世界秒）");Field(r,"moveWarning","移动预警（世界秒）");Field(r,"waveRange","冲击波传播格数");Field(r,"waveCellWarning","每格传播预警（世界秒）");}
   EditorGUILayout.HelpBox("攻击与移动各自保留完整预警，已预警的动作不会被另一动作打断。预警冲突时动作顺延；斜波方向会规范为对角方向。",MessageType.Info);serializedObject.ApplyModifiedProperties();
  }
  static void Field(SerializedProperty r,string key,string label)=>EditorGUILayout.PropertyField(r.FindPropertyRelative(key),new GUIContent(label));
 }
}
