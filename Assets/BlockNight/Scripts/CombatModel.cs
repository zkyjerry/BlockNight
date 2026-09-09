using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockNight
{
    [Serializable]
    public struct Foe
    {
        public bool active, pending;
        public int type, killChain, volley;
        public Vector2 pos, aim;
        public float age, timer, hopTimer;
    }

    [Serializable]
    public struct Shot
    {
        public bool active;
        public Vector2 pos, vel;
        public float life;
    }

    [Serializable]
    public class Frame
    {
        public Vector2 player, direction, queuedDirection;
        public Vector2Int cell, target;
        public bool moving, dashing;
        public float travel;
        public Foe[] foes;
        public Shot[] shots;
        public float elapsed, spawn, reward;
        public int score, kills, chain, bestChain;
        public uint rng;
        public int phaseIndex;
    }

    public class CombatModel
    {
        public const int GridSize = 8, Capacity = 36, Bullets = 80;
        public Vector2 player;
        public Vector2 direction = Vector2.up;
        public Vector2 queuedDirection;
        public int phaseIndex = -1;
        public SpawnSchedule schedule;
        public string PhaseName => schedule == null ? "默认阶段" : schedule.At(elapsed, out _)?.phaseName ?? "阶段结束";
        public Vector2Int cell = new Vector2Int(3, 3), target;
        public bool moving, dashing;
        public float travel;
        public Foe[] foes = new Foe[Capacity];
        public Shot[] shots = new Shot[Bullets];
        public float elapsed, spawn = 1.5f, reward, slow, slowCD, rewindCD, grace;
        public int score, kills, chain, bestChain;
        public uint rng = 7193;
        public bool hit, draft, tutorial;
        public int[] levels = new int[9];
        public Balance settings;
        public event Action<Vector2, int, bool> Killed;
        public event Action<int> ChainEnded;

        public CombatModel(Balance config, SpawnSchedule spawnSchedule = null)
        {
            settings = config; schedule = spawnSchedule;
            reward = config.upgradeSeconds;
            player = Center(cell);
            target = cell;
        }

        public float Speed => settings.playerSpeed * (1 + .12f * levels[0]);
        public float WalkSpeed => settings.walkSpeed * (1 + .2f * levels[1]);
        public float WorldRate => slow > 0 ? Mathf.Max(.06f, settings.slowRate - .04f * levels[7]) : 1;
        public static Vector2 Center(Vector2Int c) => (Vector2)c - Vector2.one * 3.5f;
        public static Vector2Int CellAt(Vector2 p) => new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(p.x + 3.5f), 0, 7), Mathf.Clamp(Mathf.RoundToInt(p.y + 3.5f), 0, 7));
        public static bool InBounds(Vector2Int c) => c.x >= 0 && c.x < GridSize && c.y >= 0 && c.y < GridSize;
        static Vector2Int Cardinal(Vector2 d) => Mathf.Abs(d.x) > Mathf.Abs(d.y) ? new Vector2Int(d.x > 0 ? 1 : -1, 0) : new Vector2Int(0, d.y > 0 ? 1 : -1);

        public float NextRandom()
        {
            rng ^= rng << 13; rng ^= rng >> 17; rng ^= rng << 5;
            return (rng & 0xffffff) / 16777216f;
        }

        // Different direction: rotate in place. Same direction: exactly one nonlethal step.
        public bool MoveOrTurn(Vector2 requested)
        {
            if (hit || draft || requested == Vector2.zero) return false;
            Vector2Int d = Cardinal(requested);
            if (dashing)
            {
                queuedDirection = d;
                if (travel <= .000001f)
                {
                    direction = queuedDirection; queuedDirection = Vector2.zero;
                    var next = cell + d;
                    if (InBounds(next)) target = next; else FinishDash();
                }
                return true;
            }
            if (moving) return false;
            if (direction != (Vector2)d) { direction = d; return true; }
            if (!InBounds(cell + d)) return false;
            target = cell + d;
            moving = true;
            dashing = false;
            travel = 0;
            return true;
        }

        public void Aim(Vector2 requested) => MoveOrTurn(requested);

        public bool Dash()
        {
            if (moving || hit || draft || !InBounds(cell + Cardinal(direction))) return false;
            chain = 0; queuedDirection = Vector2.zero;
            target = cell + Cardinal(direction);
            moving = dashing = true;
            travel = 0;
            return true;
        }

        public bool Slow()
        {
            if (slow > 0 || slowCD > 0 || hit || draft) return false;
            slow = settings.slowSeconds + .6f * levels[2];
            return true;
        }

        public static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var v = b - a;
            return Vector2.Distance(p, a + v * Mathf.Clamp01(Vector2.Dot(p - a, v) / Mathf.Max(.000001f, v.sqrMagnitude)));
        }

        public bool Occupied(Vector2Int c, int except = -1)
        {
            for (int i = 0; i < foes.Length; i++)
                if (i != except && foes[i].active && CellAt(foes[i].pos) == c) return true;
            return false;
        }

        public bool Spawn(int type, Vector2 pos)
        {
            var c = CellAt(pos);
            if (Occupied(c)) return false;
            for (int i = 0; i < foes.Length; i++)
                if (!foes[i].active)
                {
                    foes[i] = new Foe { active = true, type = type, pos = Center(c), timer = 2.4f, aim = Vector2.down, hopTimer = 1.3f };
                    return true;
                }
            return false;
        }

        void Fire(Vector2 p, Vector2 d)
        {
            for (int i = 0; i < shots.Length; i++)
                if (!shots[i].active)
                {
                    shots[i] = new Shot { active = true, pos = p, vel = d * Mathf.Min(3.5f, 1.6f + elapsed / 140), life = 8 };
                    return;
                }
        }

        public void Release()
        {
            for (int i = 0; i < foes.Length; i++)
                if (foes[i].pending)
                {
                    Killed?.Invoke(foes[i].pos, Mathf.Max(1, foes[i].killChain), false);
                    foes[i].pending = foes[i].active = false;
                }
        }

        void Slash(Vector2 from, Vector2 to)
        {
            for (int i = 0; i < foes.Length; i++)
            {
                var f = foes[i];
                if (!f.active || f.pending || f.age < settings.spawnWarning || SegmentDistance(f.pos, from, to) > .28f) continue;
                chain++; kills++;
                score += Mathf.RoundToInt(100 * (1 + .25f * Mathf.Min(chain - 1, 12)) * (1 + .25f * levels[6]));
                reward -= settings.killSeconds + .35f * levels[5];
                f.killChain = chain;
                f.pending = slow > 0;
                f.active = f.pending;
                foes[i] = f;
                Killed?.Invoke(f.pos, chain, f.pending);
            }
        }

        void AdvancePlayer(float dt)
        {
            if (!moving) return;
            float remaining = dt;
            while (moving && remaining > .000001f)
            {
                float speed = dashing ? Speed : WalkSpeed;
                float slice = Mathf.Min(remaining, (1 - travel) / speed);
                Vector2 before = player;
                travel = Mathf.Min(1, travel + slice * speed);
                player = Vector2.Lerp(Center(cell), Center(target), travel);
                if (dashing) Slash(before, player);
                remaining -= slice;
                if (travel < .99999f) break;
                cell = target;
                player = Center(cell);
                travel = 0;
                if (dashing && queuedDirection != Vector2.zero) { direction = queuedDirection; queuedDirection = Vector2.zero; }
                var next = cell + Cardinal(direction);
                if (dashing && InBounds(next)) { target = next; continue; }
                moving = false;
                if (dashing) FinishDash();
            }
        }

        void FinishDash()
        {
            moving = dashing = false; queuedDirection = Vector2.zero;
            if (chain > 0)
            {
                score += 25 * chain * chain;
                reward -= Mathf.Min(3, .2f * chain);
                bestChain = Mathf.Max(bestChain, chain);
                ChainEnded?.Invoke(chain);
            }
            chain = 0;
        }

        static readonly Vector2[] NorthwestSoutheast = { new Vector2(-1, 1).normalized, new Vector2(1, -1).normalized };
        static readonly Vector2[] NortheastSouthwest = { new Vector2(1, 1).normalized, new Vector2(-1, -1).normalized };
        static readonly Vector2[] FourDiagonals = { new Vector2(-1, 1).normalized, new Vector2(1, -1).normalized, new Vector2(1, 1).normalized, new Vector2(-1, -1).normalized };
        public static Vector2[] AttackDirections(int type, int volley)
        {
            if (type == 1) return FourDiagonals;
            if (type == 2 || (type == 3 && volley % 2 == 1)) return NortheastSouthwest;
            return NorthwestSoutheast;
        }

        void SpawnBatch(SpawnPhase phase)
        {
            if (phase.enemyTypes == null || phase.enemyTypes.Length == 0 || phase.spawnCount <= 0) return;
            var available = new List<Vector2Int>();
            for (int x = 0; x < 8; x++) for (int y = 0; y < 8; y++)
            {
                var c = new Vector2Int(x, y);
                if (Mathf.Abs(x - cell.x) + Mathf.Abs(y - cell.y) >= 2 && !Occupied(c)) available.Add(c);
            }
            int count = Mathf.Min(phase.spawnCount, Capacity, available.Count);
            for (int i = 0; i < count; i++)
            {
                int index = (int)(NextRandom() * available.Count);
                var c = available[index]; available.RemoveAt(index);
                int type = (int)phase.enemyTypes[(int)(NextRandom() * phase.enemyTypes.Length)];
                if (!Spawn(type, Center(c))) break;
            }
        }

        public void Step(float dt)
        {
            if (hit || draft) return;
            grace = Mathf.Max(0, grace - dt);
            slowCD = Mathf.Max(0, slowCD - dt);
            rewindCD = Mathf.Max(0, rewindCD - dt);
            float wd = dt * WorldRate;
            if (slow > 0)
            {
                slow -= dt;
                if (slow <= 0) { slow = 0; slowCD = Mathf.Max(4, settings.slowCooldown - 1.5f * levels[3]); Release(); }
            }
            if (!tutorial)
            {
                elapsed += wd; reward -= dt;
                int nextPhase = 0;
                var phase = schedule == null ? SpawnSchedule.Fallback : schedule.At(elapsed, out nextPhase);
                if (phaseIndex != nextPhase) { phaseIndex = nextPhase; spawn = 0; }
                spawn -= wd;
                if (phase != null && spawn <= 0)
                {
                    spawn = Mathf.Max(.2f, phase.spawnInterval);
                    SpawnBatch(phase);
                }
            }
            for (int i = 0; i < foes.Length; i++) if (foes[i].active && !foes[i].pending) foes[i].age += wd;
            Vector2 old = player;
            AdvancePlayer(dt);
            for (int i = 0; i < foes.Length; i++)
            {
                var f = foes[i];
                if (!f.active || f.pending || f.age < settings.spawnWarning || tutorial) continue;
                f.timer -= wd;
                if (f.timer > .7f)
                {
                    f.hopTimer -= wd;
                    if (f.type == 2 && f.hopTimer <= 0)
                    {
                        var c = CellAt(f.pos);
                        var delta = cell - c;
                        if (delta != Vector2Int.zero)
                        {
                            var next = c + Cardinal(delta);
                            if (InBounds(next) && !Occupied(next, i) && next != cell) f.pos = Center(next);
                        }
                        f.hopTimer = 1.3f;
                    }
                }
                if (f.timer <= 0)
                {
                    foreach (var heading in AttackDirections(f.type, f.volley)) Fire(f.pos, heading);
                    f.volley++;
                    f.timer = Mathf.Max(1.5f, 3.2f - elapsed / 160);
                }
                foes[i] = f;
            }
            for (int i = 0; i < shots.Length; i++)
            {
                var s = shots[i];
                if (!s.active) continue;
                Vector2 before = s.pos;
                s.pos += s.vel * wd; s.life -= wd;
                if (grace <= 0 && SegmentDistance(Vector2.zero, before - old, s.pos - player) < .23f) hit = true;
                if (s.life <= 0 || Mathf.Abs(s.pos.x) > 4.5f || Mathf.Abs(s.pos.y) > 4.5f) s.active = false;
                shots[i] = s;
            }
            // A lethal hit interrupts by death, but cannot be used to steer an ongoing slash.
            if (hit) { moving = dashing = false; }
            if (reward <= 0 && !tutorial && !hit && !dashing) { reward = 0; draft = true; }
        }

        public Frame Capture() => new Frame
        {
            player = player, direction = direction, queuedDirection = queuedDirection, phaseIndex = phaseIndex, cell = cell, target = target, moving = moving, dashing = dashing, travel = travel,
            foes = (Foe[])foes.Clone(), shots = (Shot[])shots.Clone(), elapsed = elapsed, spawn = spawn, reward = reward,
            score = score, kills = kills, chain = chain, bestChain = bestChain, rng = rng
        };

        public void Restore(Frame f)
        {
            player = f.player; direction = f.direction; queuedDirection = f.queuedDirection; phaseIndex = f.phaseIndex; cell = f.cell; target = f.target; moving = f.moving; dashing = f.dashing; travel = f.travel;
            Array.Copy(f.foes, foes, Capacity); Array.Copy(f.shots, shots, Bullets);
            elapsed = f.elapsed; spawn = f.spawn; reward = f.reward; score = f.score; kills = f.kills;
            chain = f.chain; bestChain = f.bestChain; rng = f.rng; hit = draft = false;
        }

        public int[] Offers()
        {
            var bag = new List<int>();
            for (int i = 0; i < 9; i++) if (levels[i] < 3) bag.Add(i);
            var result = new int[3];
            for (int i = 0; i < 3; i++)
            {
                if (bag.Count == 0) { result[i] = 9; continue; }
                int n = (int)(NextRandom() * bag.Count); result[i] = bag[n]; bag.RemoveAt(n);
            }
            return result;
        }
        public void Choose(int id) { if (id < 9) levels[id]++; else score += 2500; reward = settings.upgradeSeconds; draft = false; }
    }
}
