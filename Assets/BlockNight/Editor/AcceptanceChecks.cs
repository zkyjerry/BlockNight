using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DG.Tweening;

namespace BlockNight.Editor
{
    public static class AcceptanceChecks
    {
        static int passed;
        static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL: " + label); passed++; }
        static void Ready(CombatModel m) { for (int i = 0; i < m.foes.Length; i++) if (m.foes[i].active) m.foes[i].age = 2; }
        static void Place(CombatModel m, int x, int y, Vector2 facing)
        { m.cell = m.target = new Vector2Int(x, y); m.player = CombatModel.Center(m.cell); m.direction = facing; m.moving = m.dashing = false; m.travel = 0; }

        [MenuItem("Block Night/QA/Run Logic Checks")]
        public static void Run()
        {
            passed = 0;
            var b = AssetDatabase.LoadAssetAtPath<Balance>("Assets/BlockNight/Data/Balance.asset");
            var m = new CombatModel(b) { tutorial = true };
            Vector2 old = m.player;
            Check(m.MoveOrTurn(Vector2.right) && m.player == old && !m.moving && m.direction == Vector2.right, "different direction only turns");
            m.Spawn(0, CombatModel.Center(new Vector2Int(4, 3))); Ready(m);
            Check(m.MoveOrTurn(Vector2.right), "same direction starts one step"); m.Step(.5f);
            Check(m.cell == new Vector2Int(4, 3) && !m.moving && m.kills == 0, "walk exactly one cell without killing");
            m = new CombatModel(b) { tutorial = true }; Place(m, 0, 3, Vector2.right);
            for (int x = 1; x < 8; x++) m.Spawn(0, CombatModel.Center(new Vector2Int(x, 3)));
            Ready(m); int settlements = 0; m.ChainEnded += n => { Check(n == 7, "seven-kill chain"); settlements++; };
            Check(m.Dash(), "J starts dash");
            Check(!m.Dash(), "dash rejects retrigger");
            m.Step(.035f); var frame = m.Capture();
            Check(frame.dashing && frame.travel > 0 && frame.travel < 1, "snapshot mid-cell dash");
            m.Step(.1f); m.Restore(frame);
            Check(m.cell == frame.cell && m.target == frame.target && m.travel == frame.travel && m.player == frame.player && m.dashing, "full grid motion restored");
            m.Step(1); Check(m.cell == new Vector2Int(7, 3) && !m.moving && m.kills == 7 && m.score == 2450 && settlements == 1, "dash to edge and exact payout");
            m.Step(1); Check(settlements == 1 && !m.Dash(), "no duplicate bank or out-of-bounds dash");
            m = new CombatModel(b) { tutorial = true }; Place(m, 0, 1, Vector2.right);
            m.Spawn(0, CombatModel.Center(new Vector2Int(1,1))); m.Spawn(1, CombatModel.Center(new Vector2Int(1,3))); Ready(m);
            m.Dash(); m.Step(.035f); Check(m.MoveOrTurn(Vector2.up) && m.direction == Vector2.right && m.queuedDirection == Vector2.up, "airborne steering queued until cell center");
            frame=m.Capture(); m.Step(.1f); m.Restore(frame); Check(m.queuedDirection==Vector2.up,"rewind restores queued steering");
            m.Step(1); Check(m.cell==new Vector2Int(1,7) && m.kills==2 && m.bestChain==2 && !m.dashing,"L-shaped dash banks one chain");
            var schedule=ScriptableObject.CreateInstance<SpawnSchedule>();
            schedule.phases=new[]{new SpawnPhase{duration=2,spawnCount=2,spawnInterval=1,enemyTypes=new[]{EnemyKind.双斜射手}},new SpawnPhase{duration=2,spawnCount=3,spawnInterval=1,enemyTypes=new[]{EnemyKind.四斜棱镜}},new SpawnPhase{duration=1,spawnCount=0}};
            schedule.repeatLastPhase=false;
            m=new CombatModel(b,schedule){grace=20};m.Step(.01f);
            Check(m.foes.Count(f=>f.active)==2 && m.foes.Where(f=>f.active).All(f=>f.type==0),"phase batch and allowed type");
            m.Step(.5f);Check(m.foes.Count(f=>f.active)==2,"spawn interval waits");m.Step(.51f);Check(m.foes.Count(f=>f.active)==4,"second batch on interval");
            m.Step(1);Check(m.phaseIndex==1 && m.foes.Count(f=>f.active&&f.type==1)==3,"phase boundary changes count and type");
            frame=m.Capture();m.Step(.5f);m.Restore(frame);Check(m.phaseIndex==frame.phaseIndex&&m.spawn==frame.spawn,"rewind restores phase timer");
            m.elapsed=4.01f;int count=m.foes.Count(f=>f.active);m.Step(.01f);Check(m.phaseIndex==2&&m.foes.Count(f=>f.active)==count,"zero count rest stage");
            Check(schedule.At(5.1f,out int ended)==null&&ended==-1,"finite schedule ends");schedule.repeatLastPhase=true;Check(schedule.At(999,out ended)==schedule.phases[2]&&ended==2,"repeat final stage");
            UnityEngine.Object.DestroyImmediate(schedule);
            for(int type=0;type<4;type++)for(int volley=0;volley<4;volley++)foreach(var ray in CombatModel.AttackDirections(type,volley))Check(Mathf.Abs(ray.x)>.7f&&Mathf.Abs(ray.y)>.7f&&Mathf.Abs(ray.magnitude-1)<.0001f,"all enemy attacks diagonal");
            for(int type=0;type<4;type++) {
                m=new CombatModel(b){grace=20};m.Spawn(type,CombatModel.Center(new Vector2Int(0,7)));m.foes[0].age=3;m.foes[0].timer=.01f;m.Step(.02f);
                Check(m.shots.Count(x=>x.active)==(type==1?4:2),"actual volley count");
                foreach(var shot in m.shots.Where(x=>x.active))Check(Mathf.Abs(Mathf.Abs(shot.vel.x)-Mathf.Abs(shot.vel.y))<.0001f,"actual bullets never target player or cardinal axes");
            }
            var normal = new CombatModel(b) { tutorial = true }; var slow = new CombatModel(b) { tutorial = true };
            normal.Dash(); slow.Dash(); slow.Slow(); normal.Step(.04f); slow.Step(.04f);
            Check(normal.player == slow.player, "slow preserves player motion");
            m = new CombatModel(b) { tutorial = true }; Place(m, 0, 3, Vector2.right); m.Spawn(0, CombatModel.Center(new Vector2Int(1, 3))); Ready(m);
            m.Slow(); m.Dash(); int bursts = 0; m.Killed += (p, n, deferred) => { if (!deferred) bursts++; };
            m.Step(.1f); Check(m.kills == 1 && m.foes[0].pending && bursts == 0, "deferred kill");
            m.Step(3); Check(!m.foes[0].active && bursts == 1 && !m.Slow(), "release exactly once and cooldown");
            m = new CombatModel(b); m.Spawn(1, Vector2.one); m.shots[0] = new Shot { active = true, pos = Vector2.one, vel = Vector2.right, life = 3 };
            frame = m.Capture(); float random = m.NextRandom(); m.foes[0].active = false; m.shots[0].active = false; m.rewindCD = 9; m.Restore(frame);
            Check(m.foes[0].active && m.shots[0].active && m.NextRandom() == random && m.rewindCD == 9, "entities RNG and external cooldown");
            Check(!m.Spawn(2, Vector2.one), "duplicate enemy cell denied");
            m = new CombatModel(b); m.shots[0] = new Shot { active = true, pos = m.player + Vector2.left, vel = Vector2.right * 20, life = 1 };
            m.Step(.1f); Check(m.hit, "one swept bullet kills");
            m = new CombatModel(b); for (int i = 0; i < 100; i++) Check(m.Offers().Distinct().Count() == 3, "distinct offers");
            m.draft = true; m.Step(10); Check(m.elapsed == 0, "draft freeze"); m.Choose(1); Check(m.WalkSpeed > b.walkSpeed && !m.draft, "light step upgrade");
            for (int i = 0; i < 9; i++) m.levels[i] = 3;
            Check(m.Offers().All(x => x == 9), "max-level fallback");
            m = new CombatModel(b);
            for (int i = 0; i < 10000; i++)
            {
                m.grace = 1;
                if (i % 19 == 0) m.MoveOrTurn(new[] { Vector2.up, Vector2.right, Vector2.down, Vector2.left }[(i / 19) % 4]);
                if (i % 83 == 0) m.Dash();
                var prior = (Foe[])m.foes.Clone();
                m.Step(1f / 60);
                if (m.draft) m.Choose(m.Offers()[0]);
                Check(CombatModel.InBounds(m.cell) && CombatModel.InBounds(m.target) && !float.IsNaN(m.player.x), "grid bounds");
                Check(m.moving || m.player == CombatModel.Center(m.cell), "idle exactly at cell center");
                var active = m.foes.Where(f => f.active).ToArray();
                Check(active.Select(f => f.pos).Distinct().Count() == active.Length, "unique enemy occupancy");
                foreach (var f in active) Check(f.pos == CombatModel.Center(CombatModel.CellAt(f.pos)), "enemy at cell center");
                for (int j = 0; j < m.foes.Length; j++)
                    if (prior[j].active && m.foes[j].active && !prior[j].pending && m.foes[j].age >= prior[j].age)
                        Check(Mathf.Abs(prior[j].pos.x - m.foes[j].pos.x) + Mathf.Abs(prior[j].pos.y - m.foes[j].pos.y) <= 1.001f, "hunter one-cell hop");
            }
            int[] tiers = { 1, 3, 5, 7 };
            for (int i = 1; i < 4; i++)
                Check(ArenaPresentation.ParticleCount(tiers[i]) > ArenaPresentation.ParticleCount(tiers[i - 1]) && ArenaPresentation.ShakeStrength(tiers[i]) > ArenaPresentation.ShakeStrength(tiers[i - 1]) && ArenaPresentation.BrightCorners(tiers[i]) > ArenaPresentation.BrightCorners(tiers[i - 1]), "monotonic feedback tiers");
            Directory.CreateDirectory("Docs/QA");
            File.WriteAllText("Docs/QA/logic-checks.txt", "GRID V3 PASS — " + passed + " assertions; 10,000 simulated frames.\nTurn/step, no walk kills, dash steering, phases, diagonal volleys, grid snapshot, payout, deferred release, collision, occupancy, hunter hops, upgrades and four feedback tiers.\n" + DateTime.Now.ToString("s"));
            Debug.Log("GRID V3 QA: PASS " + passed);
        }

        [MenuItem("Block Night/QA/Combat Fixture")]
        public static void Combat()
        {
            var d = UnityEngine.Object.FindObjectOfType<GameDirector>(); d.Begin(false); d.enabled = false;
            d.model.elapsed = 72; d.model.reward = 12.8f; d.model.score = 4250; d.model.chain = 6;
            int[,] cells = { {1,6},{5,5},{6,2},{2,1},{3,4},{7,7} };
            for (int i = 0; i < 6; i++) { d.model.Spawn(i % 4, CombatModel.Center(new Vector2Int(cells[i,0],cells[i,1]))); d.model.foes[i].age = 3; d.model.foes[i].timer = i % 2 == 0 ? .45f : 1.5f; d.model.foes[i].aim = (d.model.player-d.model.foes[i].pos).normalized; }
            d.arena.Render(d.model); d.arena.TimeEffect(false,false,false,.05f); d.hud.Render(d,true); Canvas.ForceUpdateCanvases();
        }
        [MenuItem("Block Night/QA/Slow Fixture")]
        public static void Slow() { Combat(); var d = UnityEngine.Object.FindObjectOfType<GameDirector>(); d.model.Slow(); d.model.foes[0].pending = d.model.foes[1].pending = true; d.arena.Render(d.model); d.arena.TimeEffect(true,false,false,1); d.hud.Render(d,true); }
        [MenuItem("Block Night/QA/Rewind Fixture")]
        public static void Rewind() { Combat(); var d = UnityEngine.Object.FindObjectOfType<GameDirector>(); d.mode = Mode.Rewinding; d.arena.TimeEffect(false,true,false,1); d.hud.Render(d,true); }
        [MenuItem("Block Night/QA/Draft Fixture")]
        public static void Draft() { Combat(); var d = UnityEngine.Object.FindObjectOfType<GameDirector>(); d.mode = Mode.Draft; d.hud.SetOffers(new[]{0,1,5},d.model.levels); d.hud.Render(d,true); }
        [MenuItem("Block Night/QA/Title Fixture")]
        public static void Title() { SceneFlow.ToMenu(); }
        [MenuItem("Block Night/QA/Feedback 1")]
        public static void Feedback1() => Feedback(1);
        [MenuItem("Block Night/QA/Feedback 7")]
        public static void Feedback7() => Feedback(7);
        static void Feedback(int count)
        {
            Combat(); var d = UnityEngine.Object.FindObjectOfType<GameDirector>();
            d.model.chain = count; d.arena.Kill(d.model.player, count, false);
            d.arena.shards.Simulate(.16f, false, false); d.arena.shards.Pause();
            for(int i=0;i<8;i++) d.arena.TimeEffect(false,false,false,.016f);
            d.hud.Banner(ArenaPresentation.TierName(count)); d.hud.Render(d,true);
            foreach(var r in d.arena.shockwaves) if(r.enabled) {r.DOKill();r.transform.DOKill();r.transform.localScale=Vector3.one*(1+ArenaPresentation.Tier(count)*.45f);}
        }
    }
}
