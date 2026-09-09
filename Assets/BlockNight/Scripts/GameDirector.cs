using System.Collections.Generic;
using UnityEngine;

namespace BlockNight
{
    public enum Mode { Title, Playing, Paused, Draft, Dying, Dead, Rewinding }

    public class GameDirector : MonoBehaviour
    {
        public Balance balance;
        public SpawnSchedule spawnSchedule;
        [HideInInspector] public bool routeScenes = true;
        bool started;
        public ArenaPresentation arena;
        public GameHUD hud;
        public SynthAudio audioBus;
        public CombatModel model;
        public Mode mode;
        public int tutorialStep = -1;
        public float danger;
        readonly List<Frame> history = new List<Frame>();
        float sample, rewindClock;
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

        void Start() { if (!started) Begin(SceneFlow.NextRunTutorial); }

        void NewModel()
        {
            model = new CombatModel(balance, spawnSchedule);
            model.rng = (uint)System.Environment.TickCount | 1u;
            model.Killed += (p, n, deferred) =>
            {
                arena.Kill(p, n, deferred);
                if (!deferred) audioBus.Cue(2, n);
                hud.RewardPunch();
            };
            model.ChainEnded += n => { hud.Banner("连斩 " + n.ToString("00") + " / " + ArenaPresentation.TierName(n)); audioBus.Cue(3, n); };
            history.Clear(); sample = 0;
        }

        public void Begin(bool tutorial)
        {
            started = true;
            NewModel(); tutorialStep = tutorial ? 0 : -1;
            model.tutorial = tutorial; mode = Mode.Playing;
            if (tutorial) PlacePlayer(new Vector2Int(3, 1));
            tutorialStart = model.cell;
            arena.ClearEffects(); audioBus.Cue(0);
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
            mode = Mode.Playing; audioBus.Cue(4);
        }

        public void MoveOrTurn(Vector2 direction)
        {
            if (mode != Mode.Playing) return;
            Vector2 oldDirection = model.direction;
            if (!model.MoveOrTurn(direction)) return;
            audioBus.Cue(1);
            if (tutorialStep == 0 && oldDirection != model.direction) tutorialStep = 1;
        }

        public bool StartSlash()
        {
            if (mode != Mode.Playing || !model.Dash()) return false;
            arena.DashStart(); audioBus.Cue(1, 4); return true;
        }

        public bool ActivateSlow()
        {
            if (mode != Mode.Playing || !model.Slow()) return false;
            audioBus.Cue(4);
            if (tutorialStep == 3)
            {
                PlacePlayer(new Vector2Int(3, 0));
                for (int j = 2; j <= 6; j++) model.Spawn(0, CombatModel.Center(new Vector2Int(3, j)));
                for (int j = 0; j < model.foes.Length; j++) if (model.foes[j].active) model.foes[j].age = balance.spawnWarning;
                tutorialStep = 4; StartSlash();
            }
            return true;
        }

        public bool Rewind()
        {
            if ((mode != Mode.Playing && mode != Mode.Dying) || model.dashing || model.rewindCD > 0 || history.Count < 11) return false;
            returnTutorial = model.tutorial;
            rewindFrom = history.Count - 1; rewindClock = 0; mode = Mode.Rewinding;
            if (model.slow > 0) model.slowCD = Mathf.Max(model.slowCD, balance.slowCooldown - 1.5f * model.levels[3]);
            model.slow = 0; audioBus.Cue(5); arena.ClearEffects(); return true;
        }

        void Update() { GameInput.Poll(); Tick(Mathf.Min(Time.unscaledDeltaTime, .05f)); }

        public void Tick(float dt)
        {
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
                    if (returnTutorial && tutorialStep == 5) tutorialStep = 6;
                }
            }
            else if (mode == Mode.Dying)
            {
                danger -= dt;
                if (GameInput.Down(KeyCode.L) && Rewind()) { }
                else if (danger <= 0) { mode = Mode.Dead; audioBus.Cue(6); }
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
                    model.Step(dt);
                    sample += dt;
                    if (sample >= .05f)
                    {
                        sample -= .05f; history.Add(model.Capture());
                        if (history.Count > Mathf.CeilToInt(balance.rewindSeconds / .05f) + 1) history.RemoveAt(0);
                    }
                    AdvanceTutorial();
                    if (tutorialStep == 6 && GameInput.Down(KeyCode.Return)) Begin(false);
                    if (model.hit) { mode = Mode.Dying; danger = .6f; audioBus.Cue(6); }
                    else if (model.draft) { offers = model.Offers(); hud.SetOffers(offers, model.levels); mode = Mode.Draft; audioBus.Cue(4); }
                }
            }
            else if (mode == Mode.Draft)
            {
                if (GameInput.Down(KeyCode.Alpha1)) Choose(0);
                if (GameInput.Down(KeyCode.Alpha2)) Choose(1);
                if (GameInput.Down(KeyCode.Alpha3)) Choose(2);
            }
            if (mode == Mode.Dead && routeScenes) { SceneFlow.Finish(model); return; }
            arena.Render(model);
            arena.TimeEffect(model.slow > 0, mode == Mode.Rewinding, mode == Mode.Dying, dt);
            hud.Render(this, history.Count >= 11);
            audioBus.Tick(model.elapsed, model.WorldRate, mode == Mode.Playing, dt);
        }

        void AdvanceTutorial()
        {
            if (tutorialStep == 1 && !model.moving && model.cell != tutorialStart)
            {
                tutorialStep = 2;
                for (int j = 3; j <= 5; j++) model.Spawn(0, CombatModel.Center(new Vector2Int(model.cell.x, j)));
            }
            if (tutorialStep == 2 && model.kills > 0 && !model.dashing) tutorialStep = 3;
            if (tutorialStep == 4 && model.slow <= 0) tutorialStep = 5;
        }
    }
}
