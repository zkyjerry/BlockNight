using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using Cinemachine;

namespace BlockNight.Editor
{
    public static class GridUpgrade
    {
        [MenuItem("Block Night/Upgrade Current Scene to Grid v2")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before upgrading.");
            var director = Object.FindObjectOfType<GameDirector>();
            if (!director) throw new System.InvalidOperationException("请先打开 BlockNight 战斗场景。");
            var a = director.arena;
            var board = GameObject.Find("BOARD — editable world sprites").transform;
            var oldTiles = board.GetComponentsInChildren<SpriteRenderer>().Where(x => x.name.StartsWith("Tile ")).ToArray();
            Sprite square = oldTiles[0].sprite;
            Material lit = oldTiles[0].sharedMaterial;
            Material neon = a.playerSprite.sharedMaterial;
            // Only replace board-owned tiles and grid lines. No Canvas object is moved or recreated.
            foreach (var r in board.GetComponentsInChildren<SpriteRenderer>())
                if (r.name.StartsWith("Tile ") || r.name.StartsWith("Grid ")) Object.DestroyImmediate(r.gameObject);
            for (int x = 0; x < 8; x++)
                for (int y = 0; y < 8; y++)
                {
                    var tile = Sprite("Tile " + x + " · " + y, board, square, lit, -9);
                    tile.transform.localPosition = new Vector3((x - 3.5f) * 1.375f, (y - 3.5f) * 1.375f, 0);
                    tile.transform.localScale = new Vector3(1.30f, 1.30f, 1);
                    tile.color = new Color(.024f + ((x + y) % 2) * .009f, .058f, .082f);
                }
            for (int i = 0; i <= 8; i++)
            {
                float pos = -5.5f + i * 1.375f;
                var vertical = Sprite("Grid vertical " + i, board, square, neon, -8);
                vertical.transform.localPosition = new Vector3(pos, 0, 0);
                vertical.transform.localScale = new Vector3(.018f, 11, 1);
                vertical.color = new Color(.045f, .17f, .20f);
                var horizontal = Sprite("Grid horizontal " + i, board, square, neon, -8);
                horizontal.transform.localPosition = new Vector3(0, pos, 0);
                horizontal.transform.localScale = new Vector3(11, .018f, 1);
                horizontal.color = vertical.color;
            }
            a.cellSize = 1.375f;
            a.shakeCamera = Camera.main.transform;
            var brain = Camera.main.GetComponent<CinemachineBrain>();
            if (brain) brain.enabled = false;
            foreach (var vcam in Object.FindObjectsOfType<CinemachineVirtualCamera>()) vcam.enabled = false;
            foreach (var listener in Object.FindObjectsOfType<CinemachineImpulseListener>()) listener.enabled = false;
            if (a.impulse) a.impulse.enabled = false;
            if (!a.facingMarker)
            {
                a.facingMarker = Sprite("Player facing — J slash direction", a.transform, a.shapes[2], neon, 6);
                a.facingMarker.transform.localScale = Vector3.one * .22f;
                a.facingMarker.color = new Color(.25f, 1, .89f);
            }
            if (a.shockwaves == null || a.shockwaves.Length == 0)
            {
                var parent = new GameObject("SHOCKWAVES — 12 prebuilt rings").transform;
                parent.SetParent(a.transform, false);
                a.shockwaves = new SpriteRenderer[12];
                for (int i = 0; i < a.shockwaves.Length; i++)
                {
                    a.shockwaves[i] = Sprite("Shockwave " + i, parent, a.warnings[0].sprite, neon, 9);
                    a.shockwaves[i].enabled = false;
                }
            }
            a.player.position = new Vector3(-.5f * a.cellSize, -.5f * a.cellSize, -.2f);
            a.facingMarker.transform.position = a.player.position + Vector3.up * .36f * a.cellSize;
            var main = a.shards.main;
            main.maxParticles = 4096; main.loop = true; main.startLifetime = .7f; main.startSpeed = 6;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            a.shards.GetComponent<ParticleSystemRenderer>().sortingOrder = 8;
            director.balance.boardHalf = 3.5f; director.balance.walkSpeed = 7;
            EditorUtility.SetDirty(director.balance);
            a.volume.sharedProfile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out var vignette);
            vignette.color.Override(Color.black);
            EditorUtility.SetDirty(vignette);
            foreach (var text in director.hud.GetComponentsInChildren<TMP_Text>(true))
            {
                string value = text.text;
                if (text.name == "Slow name") value = "[ K ]\n时间折叠";
                else if (text.name == "Rewind name") value = "[ L ]\n时间回溯";
                else if (text.name == "Footer") value = "WASD 转向 / 走格　J 斩击　K 折叠　L 回溯";
                else if (text.name == "Chain hint") value = "瞄准整行，一斩到底。\n3 / 5 / 7 杀，反馈升级。";
                else if (text.name == "Risk") value = "一击即死。\nL 改写你的时间线。\n\nM / 切换静音";
                if (value != text.text) { text.text = value; EditorUtility.SetDirty(text); }
            }
            EditorUtility.SetDirty(a);
            EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
            EditorSceneManager.SaveScene(director.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("GRID V2: 64 tiles, 18 grid lines; DOTween camera; UI transforms untouched.");
        }

        static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, Material material, int order)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.sharedMaterial = material; renderer.sortingOrder = order;
            return renderer;
        }
    }
}
