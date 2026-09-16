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
        public static void Run() => GridCombatChecks.Menu();

        [MenuItem("Block Night/QA/Combat Fixture")]
        public static void Combat()
        {
            var d = UnityEngine.Object.FindObjectOfType<GameDirector>(); d.Begin(false); d.enabled = false;
            d.model.elapsed = 72; d.model.reward = 12.8f; d.model.score = 4250; d.model.chain = 6;
            int[,] cells = { {1,6},{5,5},{6,2},{2,1},{3,4},{7,7} };
            for (int i = 0; i < 6; i++) { d.model.Spawn(i % 4, CombatModel.Center(new Vector2Int(cells[i,0],cells[i,1]))); d.model.foes[i].age = 3; d.model.foes[i].timer = i % 2 == 0 ? .45f : 1.5f; var c=CombatModel.CellAt(d.model.foes[i].pos);var heading=d.model.foes[i].heading;if(!CombatModel.InBounds(c+heading))heading=-heading;d.model.foes[i].attackTarget=c+heading;d.model.foes[i].attackWindup=i%2==0; }
            d.arena.Render(d.model); d.arena.TimeEffect(false,false,false,.05f); d.hud.Render(d,true); Canvas.ForceUpdateCanvases();
        }
        [MenuItem("Block Night/QA/Slow Fixture")]
        public static void Slow() { Combat(); var d = UnityEngine.Object.FindObjectOfType<GameDirector>(); d.model.GrantCharge(PickupKind.时间暂停); d.model.Slow(); d.model.foes[0].pending = d.model.foes[1].pending = true; d.arena.Render(d.model); d.arena.TimeEffect(true,false,false,1); d.hud.Render(d,true); }
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
