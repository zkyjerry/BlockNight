using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockNight.Editor
{
    public static class PrefabCatalogSetup
    {
        const string EnemyDir = "Assets/BlockNight/Prefabs/Enemies";
        const string ItemDir = "Assets/BlockNight/Prefabs/Items";
        const string PlayerDir = "Assets/BlockNight/Prefabs/Player";
        static readonly string[] EnemyNames = { "突进方卫", "斜波棱镜", "巡格猎手", "逆波脉冲" };
        static readonly Color[] EnemyColors = { new Color(1, .66f, .1f), new Color(.58f, .35f, 1), new Color(.95f, .9f, .5f), new Color(.3f, 1, .42f) };

        [MenuItem("Block Night/Apply Prefab Catalog")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.OpenScene("Assets/BlockNight/Scenes/BlockNight.unity");
            var director = Object.FindObjectOfType<GameDirector>();
            if (!director || !director.arena) throw new System.InvalidOperationException("打开 BlockNight 战斗场景。");
            var a = director.arena;
            EnsureFolder("Assets/BlockNight/Prefabs");
            EnsureFolder(EnemyDir);
            EnsureFolder(ItemDir);
            EnsureFolder(PlayerDir);

            var enemies = new EnemyView[4];
            for (int i = 0; i < 4; i++) enemies[i] = BuildEnemy(a, i);
            var items = new[] { BuildItem(a, PickupKind.时间暂停, new Color(.12f, 1, .82f)), BuildItem(a, PickupKind.时间回溯, new Color(.72f, .32f, 1)) };

            var live = FindOrCreate("ENEMIES — instantiated").transform;
            var pickups = FindOrCreate("ITEMS — instantiated").transform;
            var pool = GameObject.Find("ENEMIES — 36 prebuilt slots");
            if (pool) pool.SetActive(false);

            if (a.facingMarker && a.facingMarker.transform.parent != a.player) a.facingMarker.transform.SetParent(a.player, true);
            if (a.playerLight && a.playerLight.transform.parent != a.player) a.playerLight.transform.SetParent(a.player, true);
            var marks = EnsureChargeMarks(a);
            string playerPath = PlayerDir + "/玩家.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(a.player.gameObject, playerPath, InteractionMode.AutomatedAction);

            a.enemyPrefabs = enemies;
            a.pickupPrefabs = items;
            a.enemyRoot = live;
            a.pickupRoot = pickups;
            a.chargeMarks = marks;
            EditorUtility.SetDirty(a);

            Hide("Slow bar"); Hide("Rewind bar");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("PREFAB CATALOG: enemies, items, player prefab instance, CD bars hidden.");
        }

        static EnemyView BuildEnemy(ArenaPresentation a, int type)
        {
            var root = new GameObject(EnemyNames[type]);
            var view = root.AddComponent<EnemyView>();
            view.type = type;
            view.body = Sprite("Body", root.transform, a.shapes[type], a.playerSprite.sharedMaterial, 2, EnemyColors[type] * 1.7f);
            view.body.transform.localScale = Vector3.one * (type == 3 ? .49f : .56f);
            view.warning = Sprite("Warning", root.transform, a.warnings[0].sprite, a.warnings[0].sharedMaterial, 1, new Color(1, .035f, .055f, .32f));
            view.warning.enabled = false;
            var aimGo = new GameObject("Aim", typeof(LineRenderer));
            aimGo.transform.SetParent(root.transform, false);
            view.aim = aimGo.GetComponent<LineRenderer>();
            view.aim.positionCount = 5;
            view.aim.useWorldSpace = true;
            view.aim.loop = false;
            view.aim.widthMultiplier = .04f;
            view.aim.material = a.aims[0].sharedMaterial;
            view.aim.enabled = false;
            string path = EnemyDir + "/" + EnemyNames[type] + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<EnemyView>();
            Object.DestroyImmediate(root);
            return prefab;
        }

        static PickupView BuildItem(ArenaPresentation a, PickupKind kind, Color color)
        {
            var root = new GameObject(kind.ToString());
            var view = root.AddComponent<PickupView>();
            view.kind = kind;
            view.body = Sprite("Body", root.transform, a.shapes[kind == PickupKind.时间回溯 ? 1 : 2], a.playerSprite.sharedMaterial, 5, color * 1.8f);
            view.body.transform.localScale = Vector3.one * .32f;
            string path = ItemDir + "/" + kind + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<PickupView>();
            Object.DestroyImmediate(root);
            return prefab;
        }

        static SpriteRenderer[] EnsureChargeMarks(ArenaPresentation a)
        {
            var marks = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                string name = "Charge mark " + i;
                var existing = a.player.Find(name);
                SpriteRenderer mark = existing ? existing.GetComponent<SpriteRenderer>() : Sprite(name, a.player, a.shapes[0], a.playerSprite.sharedMaterial, 7, Color.white);
                mark.transform.localScale = Vector3.one * .22f;
                mark.enabled = false;
                marks[i] = mark;
            }
            return marks;
        }

        static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, Material material, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite; r.sharedMaterial = material; r.sortingOrder = order; r.color = color;
            return r;
        }

        static GameObject FindOrCreate(string name)
        {
            var go = GameObject.Find(name);
            if (go) return go;
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, name);
            return go;
        }

        static void Hide(string name)
        {
            var go = GameObject.Find(name);
            if (go) go.SetActive(false);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            var leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
