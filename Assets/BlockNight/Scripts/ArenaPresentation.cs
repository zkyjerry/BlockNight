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
        public FeedbackSettings feedbackSettings;
        public CinemachineImpulseSource impulse; // Kept for existing scene serialization; no longer drives feedback.
        public Light2D playerLight;
        public Transform shakeCamera;
        public SpriteRenderer facingMarker;
        public LineRenderer shieldArc;
        public SpriteRenderer[] shockwaves;
        public EnemyView[] enemyPrefabs;
        public PickupView[] pickupPrefabs;
        public Transform enemyRoot, pickupRoot;
        public SpriteRenderer[] chargeMarks;
        public float cellSize = 1.375f;
        [Min(.08f)] public float enemyMoveSeconds = .18f;

        readonly Color[] colors = { new Color(1, .66f, .1f), new Color(.58f, .35f, 1), new Color(.95f, .9f, .5f), new Color(.3f, 1, .42f) };
        Bloom bloom;
        ChromaticAberration chroma;
        ColorAdjustments grade;
        Vignette vignette;
        Vector3 cameraRest, playerScale;
        Tween cameraTween;
        float kick, feedbackTime;
        int feedbackChain, ringIndex;
        bool initialized;
        Vector2[] visualFoePositions;
        bool[] visualFoeVisible;
        EnemyView[] foeViews;
        PickupView[] itemViews;
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
            playerSprite.enabled = true; facingMarker.enabled = true;
            if (playerLight) playerLight.enabled = true;
            foreach (var body in bodies) if (body) body.transform.DOKill();
            DespawnAll();
            if (visualFoeVisible != null) System.Array.Clear(visualFoeVisible, 0, visualFoeVisible.Length);
            foreach (var ring in shockwaves) { ring.DOKill(); ring.transform.DOKill(); ring.enabled = false; }
            kick = feedbackTime = 0; feedbackChain = 0;
            if(shieldArc)shieldArc.enabled=false;
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

        public void StopShake(){Initialize();cameraTween?.Kill();shakeCamera.localPosition=cameraRest;}

        // Missed the last rewind window: the camera is already restoring, so the burst carries the moment alone.
        public void PlayerShatter(Vector2 p, int count)
        {
            Initialize();
            StopShake();
            player.DOKill(); player.localScale = playerScale;
            playerSprite.enabled = false; facingMarker.enabled = false;
            if (playerLight) playerLight.enabled = false;
            trail.emitting = false; trail.Clear();
            if (shieldArc) shieldArc.enabled = false;
            if (!shards.isPlaying) shards.Play();
            count = Mathf.Max(120, count);
            for (int i = 0; i < count; i++)
            {
                float angle = 2 * Mathf.PI * i / count + Random.value * .2f;
                var outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float speed = Mathf.Lerp(1.4f, 12f, Random.value * Random.value);
                shards.Emit(new ParticleSystem.EmitParams
                {
                    position = World(p + outward * Random.Range(0f, .45f), -.3f),
                    velocity = outward * speed + Random.insideUnitCircle * 1.4f,
                    startColor = Color.Lerp(new Color(.25f, 1, .89f), Color.white, Random.value * Random.value) * Random.Range(2f, 3.6f),
                    startSize = Random.Range(.05f, .27f),
                    startLifetime = Random.Range(.45f, 1.5f)
                }, 1);
            }
            for (int i = 0; i < 3; i++)
            {
                var ring = shockwaves[ringIndex++ % shockwaves.Length];
                ring.DOKill(); ring.transform.DOKill(); ring.enabled = true;
                ring.transform.position = World(p, -.4f);
                ring.transform.localScale = Vector3.one * (.3f + i * .35f);
                ring.color = new Color(.35f, 1, .92f) * (2.6f - i * .5f);
                ring.transform.DOScale(5.5f + i * 2.2f, .55f + i * .18f).SetEase(Ease.OutCubic).SetDelay(i * .09f);
                ring.DOFade(0, .6f + i * .18f).SetDelay(i * .09f).OnComplete(() => ring.enabled = false);
            }
            kick = 1; feedbackTime = 0; feedbackChain = 0;
        }

        public void ShieldBreak(Vector2 p)
        {
            Initialize();shards.Play();
            for(int i=0;i<220;i++){float angle=2*Mathf.PI*i/220f;Vector2 normal=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));shards.Emit(new ParticleSystem.EmitParams{position=World(p+normal*.58f,-.3f),velocity=normal*Random.Range(4f,9f),startSize=Random.Range(.1f,.22f),startLifetime=Random.Range(.35f,.7f),startColor=Color.Lerp(new Color(.3f,1,.85f),Color.white,Random.value)*2.5f},1);}
            for(int i=0;i<2;i++){var ring=shockwaves[ringIndex++%shockwaves.Length];ring.DOKill();ring.transform.DOKill();ring.enabled=true;ring.transform.position=World(p,-.4f);ring.transform.localScale=Vector3.one*(1+i*.3f);ring.color=new Color(.5f,1,.85f)*2.5f;ring.transform.DOScale(3.8f+i*.8f,.38f+i*.08f).SetEase(Ease.OutCubic);ring.DOFade(0,.4f+i*.08f).OnComplete(()=>ring.enabled=false);}
            StopShake();cameraTween=shakeCamera.DOShakePosition(.32f,new Vector3(.28f,.28f,0),35,90,false,true).SetUpdate(UpdateType.Late).OnComplete(()=>shakeCamera.localPosition=cameraRest);kick=1;feedbackTime=.65f;feedbackChain=Mathf.Max(feedbackChain,5);
        }

        public void Render(CombatModel m, bool snapEnemyMotion = false)
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
            PaintCharges(m);
            if (UsesPrefabs()) RenderPrefabFoes(m, snapEnemyMotion);
            else RenderPooledFoes(m, snapEnemyMotion);
            RenderPickups(m);
            for (int i = 0; i < bullets.Length; i++)
            {
                var s = m.shots[i]; bullets[i].enabled = s.active;
                if(s.active){bullets[i].transform.position=World(CombatModel.Center(s.cell),-.15f);bullets[i].sprite=shapes[0];bullets[i].transform.localScale=Vector3.one*cellSize*(s.damaging?.88f:Mathf.Lerp(.18f,.88f,1-Mathf.Clamp01(s.timer/Mathf.Max(.001f,s.warning))));bullets[i].color=s.damaging?new Color(1,.035f,.055f,.75f)*1.7f:new Color(1,.035f,.055f,.3f);}

            }
            if(shieldArc){shieldArc.enabled=m.dashing&&m.shieldArmed;if(shieldArc.enabled){shieldArc.loop=true;shieldArc.positionCount=48;for(int j=0;j<48;j++){float angle=2*Mathf.PI*j/48f;shieldArc.SetPosition(j,World(m.player+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*.6f,-.35f));}}}
        }

        bool UsesPrefabs() => enemyPrefabs != null && enemyPrefabs.Length > 0 && enemyPrefabs[0];
        public Transform FoeTransform(int i)
        {
            if (foeViews != null && i < foeViews.Length && foeViews[i] && foeViews[i].body) return foeViews[i].body.transform;
            return bodies[i].transform;
        }
        public LineRenderer FoeAim(int i)
        {
            if (foeViews != null && i < foeViews.Length && foeViews[i] && foeViews[i].aim) return foeViews[i].aim;
            return aims[i];
        }

        void PaintCharges(CombatModel m)
        {
            if (chargeMarks == null) return;
            for (int i = 0; i < chargeMarks.Length; i++)
            {
                var mark = chargeMarks[i];
                if (!mark) continue;
                bool on = i < m.chargeCount;
                mark.enabled = on && playerSprite.enabled;
                if (!on) continue;
                bool rewind = m.charges[i] == (int)PickupKind.时间回溯;
                mark.color = rewind ? new Color(.72f, .32f, 1, 1) * 1.6f : new Color(.12f, 1, .82f, 1) * 1.6f;
                float angle = (90 - i * 40) * Mathf.Deg2Rad;
                mark.transform.localPosition = new Vector3(Mathf.Cos(angle) * 1.15f, Mathf.Sin(angle) * 1.15f, -.05f);
            }
        }

        void RenderPooledFoes(CombatModel m, bool snapEnemyMotion)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                var f = m.foes[i];
                bodies[i].enabled = f.active; warnings[i].enabled = f.active && !f.pending; aims[i].enabled = false;
                if (!f.active)
                {
                    if (visualFoeVisible != null && i < visualFoeVisible.Length) visualFoeVisible[i] = false;
                    continue;
                }
                PaintFoe(bodies[i], warnings[i], aims[i], f, m, snapEnemyMotion, i);
            }
        }

        void RenderPrefabFoes(CombatModel m, bool snapEnemyMotion)
        {
            if (bodies != null) foreach (var body in bodies) if (body) body.enabled = false;
            if (warnings != null) foreach (var w in warnings) if (w) w.enabled = false;
            if (aims != null) foreach (var a in aims) if (a) a.enabled = false;
            if (foeViews == null || foeViews.Length != CombatModel.Capacity) foeViews = new EnemyView[CombatModel.Capacity];
            var parent = enemyRoot ? enemyRoot : transform;
            for (int i = 0; i < CombatModel.Capacity; i++)
            {
                var f = m.foes[i];
                if (!f.active)
                {
                    DespawnFoe(i);
                    if (visualFoeVisible != null && i < visualFoeVisible.Length) visualFoeVisible[i] = false;
                    continue;
                }
                int type = Mathf.Clamp(f.type, 0, enemyPrefabs.Length - 1);
                if (!foeViews[i] || foeViews[i].type != type)
                {
                    DespawnFoe(i);
                    foeViews[i] = Object.Instantiate(enemyPrefabs[type], parent);
                    foeViews[i].type = type;
                }
                PaintFoe(foeViews[i].body, foeViews[i].warning, foeViews[i].aim, f, m, snapEnemyMotion, i);
            }
        }

        void RenderPickups(CombatModel m)
        {
            if (pickupPrefabs == null || pickupPrefabs.Length == 0) return;
            if (itemViews == null || itemViews.Length != CombatModel.PickupCapacity) itemViews = new PickupView[CombatModel.PickupCapacity];
            var parent = pickupRoot ? pickupRoot : transform;
            for (int i = 0; i < CombatModel.PickupCapacity; i++)
            {
                var p = m.pickups[i];
                if (!p.active)
                {
                    if (itemViews[i]) { Kill(itemViews[i].gameObject); itemViews[i] = null; }
                    continue;
                }
                int kind = Mathf.Clamp((int)p.kind, 0, pickupPrefabs.Length - 1);
                if (!itemViews[i] || itemViews[i].kind != p.kind)
                {
                    if (itemViews[i]) { Kill(itemViews[i].gameObject); itemViews[i] = null; }
                    itemViews[i] = Object.Instantiate(pickupPrefabs[kind], parent);
                    itemViews[i].kind = p.kind;
                }
                var view = itemViews[i];
                view.transform.position = World(CombatModel.Center(p.cell), -.18f);
                if (view.body) view.body.enabled = true;
            }
        }

        void PaintFoe(SpriteRenderer body, SpriteRenderer warning, LineRenderer aim, Foe f, CombatModel m, bool snap, int index)
        {
            body.enabled = true;
            if (warning) warning.enabled = !f.pending;
            if (aim) aim.enabled = false;
            MoveFoe(body, index, f.pos, m, snap);
            if (shapes != null && f.type < shapes.Length) body.sprite = shapes[f.type];
            body.transform.localScale = Vector3.one * (f.type == 3 ? .49f : .56f);
            body.transform.rotation = Quaternion.Euler(0, 0, f.pending ? 35 : 0);
            Color c = colors[Mathf.Clamp(f.type, 0, colors.Length - 1)]; bool forming = f.age < m.RulesFor(f.type).spawnWarning;
            body.color = f.pending ? new Color(.65f, .9f, 1, .5f) : forming ? new Color(c.r, c.g, c.b, .18f) : c * 1.7f;
            var rules = m.RulesFor(f.type);
            bool committed = f.attackWindup || f.moveWindup;
            Vector2Int target = f.attackWindup ? f.attackTarget : f.moveTarget;
            bool targetValid = committed && CombatModel.InBounds(target);
            float warningProgress = 1 - Mathf.Clamp01((f.attackWindup ? f.timer : f.hopTimer) / Mathf.Max(.001f, f.attackWindup ? rules.attackWarning : rules.moveWarning));
            float growth = Mathf.Lerp(.2f, 1, warningProgress);
            if (warning)
            {
                warning.sprite = forming ? (shapes != null && f.type < shapes.Length ? shapes[f.type] : warning.sprite) : shapes[0];
                warning.transform.position = targetValid ? World(CombatModel.Center(target), 0) : new Vector3(body.transform.position.x, body.transform.position.y, 0);
                warning.transform.localScale = Vector3.one * (forming ? 1.15f : targetValid ? cellSize * .88f * growth : .7f);
                warning.color = !forming && targetValid ? new Color(1, .035f, .055f, .32f) : new Color(c.r, c.g, c.b, forming ? .45f : .08f);
            }
            if (aim && !forming && !f.pending && targetValid)
            {
                aim.enabled = true; aim.startColor = aim.endColor = new Color(1, .035f, .055f, .95f);
                Vector3 p = World(CombatModel.Center(target), -.05f); float half = cellSize * .45f * growth;
                aim.positionCount = 5; aim.SetPosition(0, p + new Vector3(-half, -half)); aim.SetPosition(1, p + new Vector3(-half, half)); aim.SetPosition(2, p + new Vector3(half, half)); aim.SetPosition(3, p + new Vector3(half, -half)); aim.SetPosition(4, p + new Vector3(-half, -half));
            }
        }

        void Kill(GameObject go)
        {
            if (!go) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        void DespawnFoe(int i)
        {
            if (foeViews == null || i >= foeViews.Length || !foeViews[i]) return;
            if (foeViews[i].body) foeViews[i].body.transform.DOKill();
            Kill(foeViews[i].gameObject);
            foeViews[i] = null;
        }

        void DespawnAll()
        {
            if (foeViews != null) for (int i = 0; i < foeViews.Length; i++) DespawnFoe(i);
            if (itemViews != null)
                for (int i = 0; i < itemViews.Length; i++)
                    if (itemViews[i]) { Kill(itemViews[i].gameObject); itemViews[i] = null; }
        }

        void MoveFoe(SpriteRenderer body, int index, Vector2 logicPosition, CombatModel model, bool snap)
        {
            int size = CombatModel.Capacity;
            if (visualFoePositions == null || visualFoePositions.Length != size)
            {
                visualFoePositions = new Vector2[size];
                visualFoeVisible = new bool[size];
            }

            var target = World(logicPosition);
            if (!visualFoeVisible[index] || snap)
            {
                body.transform.DOKill();
                body.transform.position = target;
            }
            else if ((visualFoePositions[index] - logicPosition).sqrMagnitude > .000001f)
            {
                body.transform.DOKill();
                float duration = Mathf.Clamp(enemyMoveSeconds / Mathf.Max(.06f, model.WorldRate), .12f, .7f);
                body.transform.DOMove(target, duration).SetEase(Ease.OutQuad);
            }

            visualFoePositions[index] = logicPosition;
            visualFoeVisible[index] = true;
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
            vignette.color.value = Color.Lerp(vignette.color.value, death ? (feedbackSettings?feedbackSettings.dyingColor:new Color(.23f,.005f,.018f)) : bright ? new Color(1.5f, 12f, 8f) : Color.black, Mathf.Clamp01(dt * 16));
            float targetIntensity = bright ? BrightCorners(feedbackChain) : death ? (feedbackSettings?feedbackSettings.dyingVignette:.62f) : rewind ? .4f : .22f;
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetIntensity, Mathf.Clamp01(dt * 18));
        }

        void OnDestroy() { cameraTween?.Kill(); DespawnAll(); foreach (var body in bodies) if (body) body.transform.DOKill(); if (shakeCamera) shakeCamera.localPosition = cameraRest; }
    }
}
