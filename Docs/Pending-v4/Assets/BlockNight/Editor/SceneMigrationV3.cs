using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BlockNight.Editor
{
    public static class SceneMigrationV3
    {
        const string Root = "Assets/BlockNight/Scenes/";
        [Serializable] class Layout { public string path; public Vector2 anchorMin, anchorMax, pivot, position, size; public Vector3 scale; public Quaternion rotation; }
        static Dictionary<string, string> Layouts(GameObject canvas)
        {
            var map = new Dictionary<string, string>();
            foreach (var r in canvas.GetComponentsInChildren<RectTransform>(true))
            {
                string path = r.name;
                for (Transform t = r.parent; t != null; t = t.parent) path = t.name + "/" + path;
                map[path] = JsonUtility.ToJson(new Layout { path = path, anchorMin = r.anchorMin, anchorMax = r.anchorMax, pivot = r.pivot, position = r.anchoredPosition, size = r.sizeDelta, scale = r.localScale, rotation = r.localRotation });
            }
            return map;
        }

        [MenuItem("Block Night/Apply v3 Scenes and Phases")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("先退出运行模式。");
            var director = UnityEngine.Object.FindObjectOfType<GameDirector>();
            if (!director || !director.hud.titlePanel || !director.hud.deathPanel) throw new InvalidOperationException("需要尚未拆分的 BlockNight 场景；已拆分时请直接编辑对应场景。");
            var source = director.gameObject.scene;
            EditorSceneManager.SaveScene(source);
            var before = Layouts(director.hud.gameObject);
            var after = new Dictionary<string, string>();
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/BlockNight/Fonts/FZXIANGSU12.TTF");
            if (!font) throw new InvalidOperationException("用户提供的字体尚未导入。");
            foreach (var label in director.hud.GetComponentsInChildren<Text>(true))
            {
                label.font = font;
                if (label.name == "Chain hint") label.text = "冲刺转弯，延续连斩。\n3 / 5 / 7 杀，反馈升级。";
                EditorUtility.SetDirty(label);
            }
            var schedule = AssetDatabase.LoadAssetAtPath<SpawnSchedule>("Assets/BlockNight/Data/SpawnSchedule.asset");
            if (!schedule)
            {
                schedule = ScriptableObject.CreateInstance<SpawnSchedule>();
                schedule.phases = new[]
                {
                    Phase("入阵",25,2,3,EnemyKind.突进方卫),
                    Phase("交锋",35,3,3,EnemyKind.突进方卫,EnemyKind.斜波棱镜),
                    Phase("游猎",40,3,2.7f,EnemyKind.斜波棱镜,EnemyKind.巡格猎手),
                    Phase("脉冲",45,4,2.4f,EnemyKind.突进方卫,EnemyKind.斜波棱镜,EnemyKind.巡格猎手,EnemyKind.逆波脉冲),
                    Phase("压力",35,5,2.2f,EnemyKind.突进方卫,EnemyKind.斜波棱镜,EnemyKind.巡格猎手,EnemyKind.逆波脉冲),
                    Phase("喘息",8,0,2,EnemyKind.突进方卫),
                    Phase("极限",60,5,1.8f,EnemyKind.突进方卫,EnemyKind.斜波棱镜,EnemyKind.巡格猎手,EnemyKind.逆波脉冲)
                };
                AssetDatabase.CreateAsset(schedule, "Assets/BlockNight/Data/SpawnSchedule.asset");
            }
            director.spawnSchedule = schedule; director.routeScenes = true;
            var sourceCamera = source.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).First();
            var events = source.GetRootGameObjects().First(r => r.GetComponent<EventSystem>());
            CreateMenu(director, sourceCamera, events, false, font, after);
            SceneManager.SetActiveScene(source);
            CreateMenu(director, sourceCamera, events, true, font, after);
            SceneManager.SetActiveScene(source);
            UnityEngine.Object.DestroyImmediate(director.hud.titlePanel);
            UnityEngine.Object.DestroyImmediate(director.hud.deathPanel);
            director.hud.titlePanel = director.hud.deathPanel = null;
            director.hud.startButton = director.hud.learnButton = director.hud.retryButton = director.hud.homeButton = null;
            director.hud.deathScore = null;
            director.hud.readouts.SetActive(true);
            director.hud.pausePanel.SetActive(false); director.hud.draftPanel.SetActive(false); director.hud.tutorialPanel.SetActive(false);
            foreach (var pair in Layouts(director.hud.gameObject)) after[pair.Key] = pair.Value;
            EditorUtility.SetDirty(director); EditorUtility.SetDirty(director.hud);
            EditorSceneManager.MarkSceneDirty(source); EditorSceneManager.SaveScene(source);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Root + "MainMenu.unity", true), new EditorBuildSettingsScene(Root + "BlockNight.unity", true), new EditorBuildSettingsScene(Root + "GameOver.unity", true) };
            AssetDatabase.SaveAssets();
            var changes = before.Keys.Where(k => !after.ContainsKey(k) || before[k] != after[k]).ToArray();
            Directory.CreateDirectory("Docs/QA");
            File.WriteAllText("Docs/QA/ui-v3-migration.txt", "Original unique UI layouts: " + before.Count + "\nChanged/missing: " + changes.Length + "\n" + string.Join("\n", changes) + "\nFont: FZXIANGSU12.TTF\nScenes: MainMenu, BlockNight, GameOver");
            if (changes.Length > 0) throw new Exception("UI layout audit found changes: " + string.Join(",", changes));
            EditorSceneManager.OpenScene(Root + "MainMenu.unity");
            Debug.Log("V3 migration PASS: original UI geometry retained, user font assigned, phased SO configured, 3 scenes saved.");
        }

        static SpawnPhase Phase(string name, float duration, int count, float interval, params EnemyKind[] kinds)
            => new SpawnPhase { phaseName = name, duration = duration, spawnCount = count, spawnInterval = interval, enemyTypes = kinds };

        static void CreateMenu(GameDirector source, Camera camera, GameObject events, bool gameOver, Font font, Dictionary<string, string> layouts)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var canvas = UnityEngine.Object.Instantiate(source.hud.gameObject); canvas.name = source.hud.name;
            SceneManager.MoveGameObjectToScene(canvas, scene);
            var hud = canvas.GetComponent<GameHUD>();
            var panel = gameOver ? hud.deathPanel : hud.titlePanel;
            var primary = gameOver ? hud.retryButton : hud.startButton;
            var secondary = gameOver ? hud.homeButton : hud.learnButton;
            var result = hud.deathScore;
            foreach (Transform child in canvas.transform.Cast<Transform>().ToArray()) if (child.gameObject != panel) UnityEngine.Object.DestroyImmediate(child.gameObject);
            panel.SetActive(true);
            var controller = canvas.AddComponent<MenuController>();
            controller.isGameOver = gameOver; controller.primaryButton = primary; controller.secondaryButton = secondary;
            if (gameOver) controller.resultText = result;
            UnityEngine.Object.DestroyImmediate(hud);
            foreach (var label in canvas.GetComponentsInChildren<Text>(true)) label.font = font;
            var audio = UnityEngine.Object.Instantiate(source.audioBus.gameObject); audio.name = "AUDIO — menu";
            SceneManager.MoveGameObjectToScene(audio, scene); controller.audioBus = audio.GetComponent<SynthAudio>();
            var eventObject = UnityEngine.Object.Instantiate(events); eventObject.name = "EventSystem"; SceneManager.MoveGameObjectToScene(eventObject, scene);
            var cam = new GameObject("Main Camera").AddComponent<Camera>(); cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0, 0, -10); cam.orthographic = true; cam.orthographicSize = camera.orthographicSize;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = camera.backgroundColor;
            cam.gameObject.AddComponent<AudioListener>(); SceneManager.MoveGameObjectToScene(cam.gameObject, scene);
            foreach (var pair in Layouts(canvas)) layouts[pair.Key] = pair.Value;
            EditorSceneManager.SaveScene(scene, Root + (gameOver ? "GameOver" : "MainMenu") + ".unity");
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
