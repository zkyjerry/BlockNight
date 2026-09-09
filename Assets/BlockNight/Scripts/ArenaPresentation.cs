using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Cinemachine;
using DG.Tweening;

namespace BlockNight
{
    public class ArenaPresentation : MonoBehaviour
    {
        public Transform player;
        public TrailRenderer trail;
        public SpriteRenderer playerSprite;
        public SpriteRenderer[] bodies, warnings, bullets;
        public LineRenderer[] aims;
        public Sprite[] shapes;
        public ParticleSystem shards;
        public Volume volume;
        public CinemachineImpulseSource impulse; // Kept for existing scene serialization; no longer drives feedback.
        public Light2D playerLight;
        public Transform shakeCamera;
        public SpriteRenderer facingMarker;
        public SpriteRenderer[] shockwaves;
        public float cellSize = 1.375f;

        readonly Color[] colors = { new Color(1, .22f, .32f), new Color(.68f, .35f, 1), new Color(1, .64f, .16f), new Color(1, .25f, .69f) };
        Bloom bloom;
        ChromaticAberration chroma;
        ColorAdjustments grade;
        Vignette vignette;
        Vector3 cameraRest, playerScale;
        Tween cameraTween;
        float kick, feedbackTime;
        int feedbackChain, ringIndex;
        bool initialized;
        static readonly int[] Counts = {64, 110, 180, 280};
        static readonly float[] Strengths = {.10f, .18f, .28f, .40f};
        static readonly float[] Corners = {.16f, .27f, .40f, .55f};
        static readonly string[] TierNames = {"破阵", "连破", "狂斩", "极斩"};
        public static int Tier(int kills) => kills >= 7 ? 3 : kills >= 5 ? 2 : kills >= 3 ? 1 : 0;
        public static int ParticleCount(int kills) => Counts[Tier(kills)];
        public static float ShakeStrength(int kills) => Strengths[Tier(kills)];
        public static float BrightCorners(int kills) => Corners[Tier(kills)];
        public static string TierName(int kills) => TierNames[Tier(kills)];
        public Vector3 World(Vector2 p, float z = -.1f) => new Vector3(p.x * cellSize, p.y * cellSize, z);

        void Awake() => Initialize();
        void Initialize()
        {
            if (initialized) return;
            volume.profile.TryGet(out bloom); volume.profile.TryGet(out chroma);
            volume.profile.TryGet(out grade); volume.profile.TryGet(out vignette);
            cameraRest = shakeCamera.localPosition;
            playerScale = player.localScale;
            initialized = true;
        }

        public void ClearEffects()
        {
            Initialize(); trail.Clear(); shards.Clear();
            cameraTween?.Kill(); shakeCamera.localPosition = cameraRest;
            player.DOKill(); player.localScale = playerScale;
            foreach (var ring in shockwaves) { ring.DOKill(); ring.transform.DOKill(); ring.enabled = false; }
            kick = feedbackTime = 0; feedbackChain = 0;
        }

        public void DashStart()
        {
            Initialize(); trail.Clear();
            player.DOKill(); player.localScale = playerScale;
            player.DOPunchScale(playerScale * .35f, .16f, 1);
        }

        public void Kill(Vector2 p, int n, bool deferred)
        {
            Initialize();
            if (deferred) return;
            int tier = Tier(n);
            if (!shards.isPlaying) shards.Play();
            for (int i = 0; i < ParticleCount(n); i++)
            {
                Vector2 velocity = Random.insideUnitCircle.normalized * Random.Range(2.8f + tier, 5.5f + tier * 1.8f);
                shards.Emit(new ParticleSystem.EmitParams
                {
                    position = World(p, -.3f), velocity = velocity,
                    startColor = Color.Lerp(new Color(.15f, 1, .82f), Color.white, Random.value) * (1.8f + tier * .6f),
                    startSize = Random.Range(.065f + tier * .015f, .13f + tier * .04f),
                    startLifetime = Random.Range(.25f, .52f + tier * .09f)
                }, 1);
            }
            var ring = shockwaves[ringIndex++ % shockwaves.Length];
            ring.DOKill(); ring.transform.DOKill(); ring.enabled = true;
            ring.transform.position = World(p, -.4f);
            ring.transform.localScale = Vector3.one * .25f;
            ring.color = new Color(.35f, 1, .9f, 1) * (1.6f + tier * .35f);
            ring.transform.DOScale(1.4f + tier * .8f, .24f + tier * .05f).SetEase(Ease.OutCubic);
            ring.DOFade(0, .24f + tier * .05f).OnComplete(() => ring.enabled = false);
            feedbackChain = Mathf.Max(feedbackChain, n);
            feedbackTime = .8f; kick = 1;
            cameraTween?.Kill(); shakeCamera.localPosition = cameraRest;
            cameraTween = shakeCamera.DOShakePosition(.14f + Tier(feedbackChain) * .065f,
                new Vector3(ShakeStrength(feedbackChain), ShakeStrength(feedbackChain), 0), 35, 90, false, true)
                .SetUpdate(UpdateType.Late).OnComplete(() => shakeCamera.localPosition = cameraRest);
            player.DOKill(); player.localScale = playerScale;
            player.DOPunchScale(playerScale * (.45f + tier * .15f), .13f, 1);
        }

        public void Render(CombatModel m)
        {
            Initialize();
            player.position = World(m.player, -.2f);
            playerSprite.color = m.grace > 0 ? Color.white : new Color(.25f, 1, .89f) * 1.5f;
            playerLight.transform.position = player.position;
            trail.emitting = m.dashing;
            Vector2 facing = m.queuedDirection != Vector2.zero ? m.queuedDirection : m.direction;
            facingMarker.transform.position = World(m.player + facing * .36f, -.25f);
            facingMarker.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg - 90);
            facingMarker.color = m.dashing ? Color.white : new Color(.25f, 1, .89f);
            for (int i = 0; i < bodies.Length; i++)
            {
                var f = m.foes[i];
                bodies[i].enabled = f.active; warnings[i].enabled = f.active && !f.pending; aims[i].enabled = false;
                if (!f.active) continue;
                bodies[i].transform.position = World(f.pos);
                bodies[i].sprite = shapes[f.type];
                bodies[i].transform.localScale = Vector3.one * (f.type == 3 ? .49f : .56f);
                bodies[i].transform.rotation = Quaternion.Euler(0, 0, f.pending ? 35 : 0);
                Color c = colors[f.type]; bool forming = f.age < m.settings.spawnWarning;
                bodies[i].color = f.pending ? new Color(.65f, .9f, 1, .5f) : forming ? new Color(c.r, c.g, c.b, .18f) : c * 1.7f;
                warnings[i].transform.position = World(f.pos, 0);
                float size = forming ? Mathf.Lerp(1.2f, .7f, f.age / m.settings.spawnWarning) : f.timer < .7f ? Mathf.Lerp(1.3f, .8f, f.timer / .7f) : .74f;
                warnings[i].transform.localScale = Vector3.one * size;
                warnings[i].color = new Color(c.r, c.g, c.b, forming ? .75f : f.timer < .7f ? .9f : .18f);
                if (!forming && !f.pending && f.timer < .7f)
                {
                    var l = aims[i]; l.enabled = true; l.startColor = l.endColor = new Color(c.r, c.g, c.b, .6f);
                    Vector3 p = World(f.pos, -.05f);
                    var rays = CombatModel.AttackDirections(f.type, f.volley);
                    l.positionCount = 1 + rays.Length * 2;
                    l.SetPosition(0, p);
                    for (int ray = 0; ray < rays.Length; ray++)
                    {
                        l.SetPosition(1 + ray * 2, p + (Vector3)rays[ray] * 2.3f);
                        l.SetPosition(2 + ray * 2, p);
                    }
                }
            }
            for (int i = 0; i < bullets.Length; i++)
            {
                var s = m.shots[i]; bullets[i].enabled = s.active;
                if (s.active) { bullets[i].transform.position = World(s.pos, -.2f); bullets[i].color = new Color(1, .44f, .28f) * 2; }
            }
        }

        public void TimeEffect(bool slow, bool rewind, bool death, float dt)
        {
            Initialize();
            feedbackTime = Mathf.Max(0, feedbackTime - dt);
            if (feedbackTime <= 0) feedbackChain = 0;
            kick = Mathf.MoveTowards(kick, 0, dt * 3);
            bloom.intensity.value = 1.1f + kick * (2.2f + Tier(feedbackChain) * .7f) + (slow ? .5f : 0);
            chroma.intensity.value = Mathf.Lerp(chroma.intensity.value, rewind ? .65f : slow ? .2f : kick * .1f, dt * 12);
            grade.colorFilter.value = Color.Lerp(grade.colorFilter.value, rewind ? new Color(.68f, .43f, 1) : slow ? new Color(.43f, .85f, 1) : Color.white, dt * 8);
            bool bright = feedbackTime > 0 && !death && !rewind;
            vignette.color.value = Color.Lerp(vignette.color.value, bright ? new Color(1.5f, 12f, 8f) : Color.black, Mathf.Clamp01(dt * 16));
            float targetIntensity = bright ? BrightCorners(feedbackChain) : death ? .5f : rewind ? .4f : .22f;
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetIntensity, Mathf.Clamp01(dt * 18));
        }

        void OnDestroy() { cameraTween?.Kill(); if (shakeCamera) shakeCamera.localPosition = cameraRest; }
    }
}
