using System.Collections.Generic;
using UnityEngine;

namespace BlockNight
{
    public enum Mode { Title, Playing, Paused, Draft, Dying, Dead, Rewinding, Countdown }

    public class GameDirector : MonoBehaviour
    {
        public Balance balance;
        public SpawnSchedule spawnSchedule;
        public EnemyDefinition[] enemyDefinitions;
        [HideInInspector] public bool routeScenes = true;
        bool started;
        public ArenaPresentation arena;
        public GameHUD hud;
        public CombatFeedback feedback;
        public LessonFlow lessons;
        public SynthAudio audioBus;
        public CombatModel model;
        public Mode mode;
        public int tutorialStep = -1;
        public float danger;
        readonly List<Frame> history = new List<Frame>();
        float sample, rewindClock, deathClock;
        int rewindFrom;
        int[] offers;
        bool returnTutorial;
        Vector2Int tutorialStart;

        public void Awake()
        {
            Application.runInBackground = true;
            NewModel(); mode = Mode.Title;
            hud.Bind(this); arena.Render(model);
        }

        void Start() { if (!started) Begin(SceneFlow.NextRunTutorial,true); }

        void NewModel()
        {
            model = new CombatModel(balance, spawnSchedule, enemyDefinitions);
            model.rng = (uint)System.Environment.TickCount | 1u;
            model.Killed += (p, n, deferred) =>
            {
                arena.Kill(p, n, deferred);
                if (!deferred) audioBus.Cue(2, n);
                hud.RewardPunch();if(feedback)feedback.RewardPulse();
            };
            model.UpgradeWaveEnded += (p,radius) => {if(feedback)feedback.WaveDissolve(p,radius);};
            model.ShieldBlocked += p => { arena.ShieldBreak(p);audioBus.Cue(3);hud.Banner("护盾已消耗 / 强化可再次获得"); };
            model.ChainEnded += n => { hud.Banner("连斩 " + n.ToString("00") + " / " + ArenaPresentation.TierName(n)); audioBus.Cue(3, n); };
            history.Clear(); sample = 0;
        }

        public void Begin(bool tutorial,bool showIntro=false)
        {
            if(lessons)lessons.ResetFlow();
            started = true;
            NewModel(); tutorialStep = tutorial ? 0 : -1;
            model.tutorial = tutorial; mode = Mode.Playing;
            if (tutorial) PlacePlayer(new Vector2Int(3, 1));
            tutorialStart = model.cell;
            arena.ClearEffects();if(feedback)feedback.ResetEffects(); audioBus.Cue(0);
            if(lessons){if(tutorial)lessons.StartLesson();else if(showIntro)lessons.StartCountdown();}
            Present(0);
        }

        public void ClearHistory(){history.Clear();sample=0;}

        // Rewind window expired: release the camera back to rest, then blow the player apart before the result scene.
        void Shatter()
        {
            mode = Mode.Dead; audioBus.Explode();
            var settings = feedback ? feedback.settings : null;
            deathClock = settings ? settings.shatterSeconds : 1.7f;
            if (feedback) feedback.Focus(false, model.player);
            arena.PlayerShatter(model.player, settings ? settings.shatterParticles : 760);
        }

        void PlacePlayer(Vector2Int c)
        {
            model.cell = model.target = c;
            model.player = CombatModel.Center(c);
            model.direction = Vector2.up;
            model.moving = model.dashing = false; model.travel = 0;
        }

        public void Home() { if (routeScenes) { SceneFlow.ToMenu(); return; } NewModel(); mode = Mode.Title; tutorialStep = -1; arena.ClearEffects(); }
        public void Pause() { if (mode == Mode.Playing) mode = Mode.Paused; else if (mode == Mode.Paused) mode = Mode.Playing; }
        public void Choose(int index)
        {
            if (mode != Mode.Draft) return;
            model.Choose(offers[index]); history.Clear(); sample = 0;
            if(feedback&&feedback.settings.upgradeShockwave)model.StartUpgradeWave(feedback.settings.shockwaveSeconds,feedback.settings.shockwaveRadius);
            mode = Mode.Playing; audioBus.Cue(4);
        }

        public void MoveOrTurn(Vector2 direction)
        {
            if (mode != Mode.Playing || (model.tutorial&&lessons&&!lessons.CanMove)) return;
            Vector2 oldDirection = model.direction;
            if (!model.MoveOrTurn(direction)) return;
            audioBus.Cue(1);

        }

        public bool StartSlash()
        {
            if (mode != Mode.Playing || (model.tutorial&&lessons&&!lessons.CanSlash) || !model.Dash()) return false;
            arena.DashStart(); audioBus.Cue(1, 4); return true;
        }

        public bool ActivateSlow()
        {
            if (mode != Mode.Playing || (model.tutorial&&lessons&&!lessons.CanSlow) || !model.Slow()) return false;
            audioBus.Cue(4);
            if(model.tutorial&&lessons)lessons.SlowActivated();
            return true;
        }

        public bool Rewind()
        {
            if(model.tutorial&&lessons&&!lessons.CanRewind)return false;
            if ((mode != Mode.Playing && mode != Mode.Dying) || model.dashing || model.rewindCD > 0 || history.Count < 11) return false;
            returnTutorial = model.tutorial;
            rewindFrom = history.Count - 1; rewindClock = 0; mode = Mode.Rewinding;
            if (model.slow > 0) model.slowCD = Mathf.Max(model.slowCD, balance.slowCooldown - 1.5f * model.levels[3]);
            model.slow = 0; audioBus.Cue(5); arena.ClearEffects();if(feedback)feedback.ClearWaveParticles();if(feedback)feedback.Focus(false,model.player); return true;
        }

        void Update() { GameInput.Poll(); Tick(Mathf.Min(Time.unscaledDeltaTime, .05f)); }

        public void Tick(float dt)
        {
            if(mode==Mode.Countdown){lessons.TickCountdown(dt);Present(dt);return;}
            if(model.tutorial&&lessons&&lessons.BeforeTick(dt)){Present(dt);return;}
            if (GameInput.Down(KeyCode.M)) audioBus.ToggleMute();
            if (GameInput.Down(KeyCode.Escape)) Pause();
            if (mode == Mode.Rewinding)
            {
                rewindClock += dt;
                int index = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(rewindFrom, 0, rewindClock / .85f)), 0, rewindFrom);
                model.Restore(history[index]);
                if (rewindClock >= .85f)
                {
                    history.Clear(); sample = 0; model.Release();
                    model.grace = .55f + .25f * model.levels[8];
                    model.rewindCD = Mathf.Max(6, balance.rewindCooldown - 2 * model.levels[4]);
                    mode = Mode.Playing;

                }
            }
            else if (mode == Mode.Dying)
            {
                if(!(model.tutorial&&lessons&&lessons.HoldDeath))danger -= dt;
                if (GameInput.Down(KeyCode.L) && Rewind()) { }
                else if (danger <= 0) Shatter();
            }
            else if (mode == Mode.Playing)
            {
                if (!(GameInput.Down(KeyCode.L) && Rewind()))
                {
                    if (GameInput.Down(KeyCode.W) || GameInput.Down(KeyCode.UpArrow)) MoveOrTurn(Vector2.up);
                    if (GameInput.Down(KeyCode.S) || GameInput.Down(KeyCode.DownArrow)) MoveOrTurn(Vector2.down);
                    if (GameInput.Down(KeyCode.A) || GameInput.Down(KeyCode.LeftArrow)) MoveOrTurn(Vector2.left);
                    if (GameInput.Down(KeyCode.D) || GameInput.Down(KeyCode.RightArrow)) MoveOrTurn(Vector2.right);
                    if (GameInput.Down(KeyCode.J)) StartSlash();
                    if (GameInput.Down(KeyCode.K)) ActivateSlow();
                    float oldSlowCD=model.slowCD,oldRewindCD=model.rewindCD;
                    model.Step(dt);
                    if(feedback){if(oldSlowCD>0&&model.slowCD<=0&&model.slow<=0)feedback.Ready(false);if(oldRewindCD>0&&model.rewindCD<=0)feedback.Ready(true);}
                    sample += dt;
                    if (sample >= .05f)
                    {
                        sample -= .05f; history.Add(model.Capture());
                        if (history.Count > Mathf.CeilToInt(balance.rewindSeconds / .05f) + 1) history.RemoveAt(0);
                    }

                    if (model.hit) { mode = Mode.Dying; danger = feedback?feedback.settings.dyingSeconds:.6f; audioBus.Cue(6); }
                    else if (model.draft) { offers = model.Offers(); hud.SetOffers(offers, model.levels); mode = Mode.Draft; audioBus.Cue(4); }
                }
            }
            else if (mode == Mode.Draft)
            {
                if (GameInput.Down(KeyCode.Alpha1)) Choose(0);
                if (GameInput.Down(KeyCode.Alpha2)) Choose(1);
                if (GameInput.Down(KeyCode.Alpha3)) Choose(2);
            }
            if(model.tutorial&&lessons)lessons.AfterTick(dt);
            if (mode == Mode.Dead)
            {
                if (deathClock > 0) deathClock -= dt;
                if (routeScenes && deathClock <= 0) { SceneFlow.Finish(model); return; }
            }
            Present(dt);
        }

        void Present(float dt){
            if(feedback)feedback.Focus(mode==Mode.Dying,model.player);
            arena.Render(model, mode == Mode.Rewinding);
            arena.TimeEffect(model.slow > 0, mode == Mode.Rewinding, mode == Mode.Dying, dt);
            if(feedback)feedback.Render(model,dt);
            hud.Render(this, history.Count >= 11);
            audioBus.Tick(model.elapsed, model.WorldRate, mode == Mode.Playing, dt);
        }

    }
}
