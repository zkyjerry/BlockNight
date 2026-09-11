using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockNight
{
    /// <summary>
    /// Drives the SceneShift material already assigned to the active URP renderer feature.
    /// The controller is independent from scene UI, so menu, gameplay, and game-over
    /// transitions share one cover/load/reveal sequence.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class SceneTransition : MonoBehaviour
    {
        const string MaterialName = "SceneShift";
        const string DurationProperty = "_Duration";
        const string InvertProperty = "_IsInvert";
        const string ColorProperty = "_MainColor";
        const string SettingsResource = "SceneTransitionSettings";
        const float DefaultCoverSeconds = 1f;
        const float DefaultRevealSeconds = 2f;

        static SceneTransition instance;
        Material shiftMaterial;
        SceneTransitionSettings settings;
        string queuedScene;
        bool transitioning;

        float CoverSeconds => settings ? settings.coverSeconds : DefaultCoverSeconds;
        float RevealSeconds => settings ? settings.revealSeconds : DefaultRevealSeconds;

        public static bool IsTransitioning => instance && instance.transitioning;
        public static float Duration => instance && instance.shiftMaterial ? instance.shiftMaterial.GetFloat(DurationProperty) : 0f;
        public static bool IsInverted => instance && instance.shiftMaterial && instance.shiftMaterial.GetFloat(InvertProperty) > .5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap() => Ensure();

        public static void Load(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) || SceneManager.GetActiveScene().name == sceneName && !IsTransitioning) return;
            Ensure().Request(sceneName);
        }

        static SceneTransition Ensure()
        {
            if (instance) return instance;
            var root = new GameObject("Scene transition controller (runtime)");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<SceneTransition>();
            return instance;
        }

        void Awake()
        {
            if (instance && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            settings = Resources.Load<SceneTransitionSettings>(SettingsResource);
            ResolveMaterial();
            ShowCurrentScene();
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void Request(string sceneName)
        {
            if (transitioning)
            {
                queuedScene = sceneName;
                return;
            }

            StartCoroutine(Transition(sceneName));
        }

        IEnumerator Transition(string sceneName)
        {
            transitioning = true;
            if (!ResolveMaterial())
            {
                Debug.LogError("SceneTransition: 未找到已挂到 URP 渲染器的 SceneShift 材质，已直接切换场景。");
                SceneManager.LoadScene(sceneName);
                transitioning = false;
                yield break;
            }

            float cover = CoverSeconds, reveal = RevealSeconds;
            ApplyColor(sceneName);
            shiftMaterial.SetFloat(InvertProperty, 0f);
            if (SynthAudio.Current) SynthAudio.Current.FadeMusic(0f, cover);
            yield return Animate(0f, 1f, cover);

            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!operation.isDone) yield return null;

            shiftMaterial.SetFloat(InvertProperty, 1f);
            if (SynthAudio.Current) SynthAudio.Current.FadeMusic(1f, reveal);
            yield return Animate(1f, 0f, reveal);
            transitioning = false;

            if (!string.IsNullOrEmpty(queuedScene) && queuedScene != SceneManager.GetActiveScene().name)
            {
                var next = queuedScene;
                queuedScene = null;
                Request(next);
            }
            else queuedScene = null;
        }

        IEnumerator Animate(float from, float to, float seconds)
        {
            float elapsed = 0f;
            shiftMaterial.SetFloat(DurationProperty, from);
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                shiftMaterial.SetFloat(DurationProperty, Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / seconds)));
                yield return null;
            }
            shiftMaterial.SetFloat(DurationProperty, to);
        }

        bool ResolveMaterial()
        {
            if (shiftMaterial && shiftMaterial.HasProperty(DurationProperty) && shiftMaterial.HasProperty(InvertProperty)) return true;

            var materials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var material in materials)
            {
                if (material && material.name == MaterialName && material.HasProperty(DurationProperty) && material.HasProperty(InvertProperty))
                {
                    shiftMaterial = material;
                    return true;
                }
            }

            return false;
        }

        void ShowCurrentScene()
        {
            if (!ResolveMaterial()) return;
            ApplyColor(SceneManager.GetActiveScene().name);
            shiftMaterial.SetFloat(InvertProperty, 1f);
            shiftMaterial.SetFloat(DurationProperty, 0f);
        }

        void ApplyColor(string sceneName)
        {
            if (!settings || !shiftMaterial.HasProperty(ColorProperty)) return;
            shiftMaterial.SetColor(ColorProperty, settings.ColorFor(sceneName));
        }
    }
}
