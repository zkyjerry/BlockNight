using UnityEngine;
using UnityEditor;
namespace BlockNight.Editor {
[CustomEditor(typeof(SpawnSchedule))]
public class SpawnScheduleEditor:UnityEditor.Editor {
 public override void OnInspectorGUI(){serializedObject.Update();
 EditorGUILayout.HelpBox("按列表顺序执行；进入阶段立即刷新一批。时间单位为世界秒，时间折叠会延缓阶段进度。数量为 0 可安排休整。",MessageType.Info);
 var phases=serializedObject.FindProperty("phases");
 int count=Mathf.Clamp(EditorGUILayout.IntField("阶段数量",phases.arraySize),0,100);if(count!=phases.arraySize)phases.arraySize=count;
 for(int i=0;i<phases.arraySize;i++){var p=phases.GetArrayElementAtIndex(i);EditorGUILayout.BeginVertical(EditorStyles.helpBox);p.isExpanded=EditorGUILayout.Foldout(p.isExpanded,(i+1)+" / "+p.FindPropertyRelative("phaseName").stringValue,true);
 if(p.isExpanded){EditorGUILayout.PropertyField(p.FindPropertyRelative("phaseName"),new GUIContent("阶段名称"));EditorGUILayout.PropertyField(p.FindPropertyRelative("duration"),new GUIContent("持续时间（秒）"));EditorGUILayout.PropertyField(p.FindPropertyRelative("spawnCount"),new GUIContent("每批敌人数"));EditorGUILayout.PropertyField(p.FindPropertyRelative("spawnInterval"),new GUIContent("刷新间隔（秒）"));EditorGUILayout.PropertyField(p.FindPropertyRelative("enemyTypes"),new GUIContent("允许敌人种类"),true);}
 EditorGUILayout.EndVertical();}
 EditorGUILayout.PropertyField(serializedObject.FindProperty("repeatLastPhase"),new GUIContent("结束后持续最后阶段"));serializedObject.ApplyModifiedProperties();
 }
}
}
