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
        public bool attackWindup, moveWindup;
        public Vector2Int heading, attackTarget, moveTarget;
        public Vector2 pos, aim;
        public float age, timer, hopTimer;
    }

    [Serializable]
    public struct Shot
    {
        public bool active;
        public Vector2 pos, vel;
        public float life;
        public Vector2Int cell, sourceCell, heading;
        public bool damaging;
        public int remaining;
        public float timer, warning, impactDuration;
    }

    [Serializable]
    public class Frame
    {
        public Vector2 player, direction, queuedDirection;
        public Vector2Int cell, target;
        public bool moving, dashing, shieldArmed;
        public float travel;
        public Foe[] foes;
        public Shot[] shots;
        public float elapsed, spawn, reward;
        public int score, kills, chain, bestChain;
        public bool upgradeWaveActive;
        public Vector2 upgradeWaveOrigin;
        public float upgradeWaveRadius, upgradeWaveDuration, upgradeWaveMaxRadius;
        public int upgradeWaveKills;
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
        public bool hit, draft, tutorial, tutorialEnemyActions;
        public int[] levels = new int[10];
        public bool upgradeWaveActive;
        public Vector2 upgradeWaveOrigin;
        public float upgradeWaveRadius, upgradeWaveDuration, upgradeWaveMaxRadius;
        public int upgradeWaveKills;
        public bool shieldArmed;
        public EnemyDefinition[] enemyDefinitions;
        public event Action<Vector2> ShieldBlocked;
        public event Action<Vector2,float> UpgradeWaveEnded;
        bool dashDuringStep, shieldAtStepStart;
        readonly List<Vector2> movementPath = new List<Vector2>();
        static readonly EnemyRules[] Defaults = {
            new EnemyRules {direction=Vector2Int.right},
            new EnemyRules {action=EnemyAction.斜向冲击波,direction=new Vector2Int(1,1),attackInterval=2.8f},
            new EnemyRules {direction=Vector2Int.up,attackInterval=2,attackWarning=.55f},
            new EnemyRules {action=EnemyAction.斜向冲击波,direction=new Vector2Int(-1,1),attackInterval=3.2f,moveInterval=4,waveRange=5}
        };
        public EnemyRules RulesFor(int type) => enemyDefinitions!=null && type>=0 && type<enemyDefinitions.Length && enemyDefinitions[type] ? enemyDefinitions[type].rules : Defaults[Mathf.Clamp(type,0,Defaults.Length-1)];
        public static int MaxLevel(int id) => id==9?1:3;
        public Balance settings;
        public event Action<Vector2, int, bool> Killed;
        public event Action<int> ChainEnded;

        public CombatModel(Balance config, SpawnSchedule spawnSchedule = null, EnemyDefinition[] definitions = null)
        {
            settings = config; schedule = spawnSchedule; enemyDefinitions = definitions;
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
            moving = dashing = true; shieldArmed = levels[9]>0;
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
                    var rules=RulesFor(type);
                    foes[i] = new Foe { active = true, type = type, pos = Center(c), timer = Mathf.Max(rules.attackWarning+.1f,rules.attackInterval), heading=rules.Heading, hopTimer = Mathf.Max(rules.moveWarning+.1f,rules.moveInterval) };
                    return true;
                }
            return false;
        }

        void GridImpact(Vector2Int source, Vector2Int heading, int range, EnemyRules rules)
        {
            var targetCell=source+heading;if(!InBounds(targetCell))return;
            for(int i=0;i<shots.Length;i++)if(!shots[i].active){shots[i]=new Shot{active=true,cell=targetCell,sourceCell=source,heading=heading,remaining=range,damaging=true,timer=rules.impactDuration,warning=rules.waveCellWarning,impactDuration=rules.impactDuration,pos=Center(targetCell)};return;}
        }

        Vector2Int PlanTarget(ref Foe f, EnemyRules rules)
        {
            var c=CellAt(f.pos);var targetCell=c+f.heading;
            if(!InBounds(targetCell)&&rules.reverseAtEdge){f.heading=-f.heading;targetCell=c+f.heading;}
            return targetCell;
        }

        void StepEnemy(int index,float dt)
        {
            var f=foes[index];var rules=RulesFor(f.type);
            if(!f.active||f.pending||f.age<rules.spawnWarning||(tutorial&&!tutorialEnemyActions))return;
            if(!f.moveWindup)f.timer-=dt;
            if(!f.moveWindup&&!f.attackWindup&&f.timer<=rules.attackWarning){f.attackWindup=true;f.timer=Mathf.Max(f.timer,rules.attackWarning);f.attackTarget=PlanTarget(ref f,rules);}
            if(f.attackWindup&&f.timer<=0){
                var c=CellAt(f.pos);var attackHeading=f.attackTarget-c;
                if(InBounds(f.attackTarget)&&attackHeading!=Vector2Int.zero){
                    if(rules.action==EnemyAction.逐格撞击){if(!Occupied(f.attackTarget,index)){GridImpact(c,attackHeading,1,rules);f.pos=Center(f.attackTarget);}}
                    else GridImpact(c,attackHeading,Mathf.Clamp(rules.waveRange,1,8),rules);
                }
                f.attackWindup=false;f.timer=Mathf.Max(rules.attackWarning+.1f,rules.attackInterval);f.volley++;
            }
            if(rules.action==EnemyAction.斜向冲击波){
                // Movement waits while the wave attack is committed, so its warning never moves.
                if(!f.attackWindup){
                    f.hopTimer-=dt;
                    if(!f.moveWindup&&f.hopTimer<=rules.moveWarning){f.moveWindup=true;f.hopTimer=Mathf.Max(f.hopTimer,rules.moveWarning);f.moveTarget=PlanTarget(ref f,rules);}
                    if(f.moveWindup&&f.hopTimer<=0){var c=CellAt(f.pos);if(InBounds(f.moveTarget)&&!Occupied(f.moveTarget,index)){GridImpact(c,f.moveTarget-c,1,rules);f.pos=Center(f.moveTarget);}f.moveWindup=false;f.hopTimer=Mathf.Max(rules.moveWarning+.1f,rules.moveInterval);}
                }
            }
            foes[index]=f;
        }

        static bool CrossesTile(Vector2 from,Vector2 to,Vector2 center)
        {
            float lo=0,hi=1;Vector2 d=to-from;
            for(int axis=0;axis<2;axis++){
                float start=axis==0?from.x:from.y,delta=axis==0?d.x:d.y,c=axis==0?center.x:center.y;
                if(Mathf.Abs(delta)<.000001f){if(Mathf.Abs(start-c)>.45f)return false;continue;}
                float a=(c-.45f-start)/delta,b=(c+.45f-start)/delta;if(a>b){float t=a;a=b;b=t;}lo=Mathf.Max(lo,a);hi=Mathf.Min(hi,b);if(lo>hi)return false;
            }
            return true;
        }

        void ResolveImpact(ref Shot strike)
        {
            if(!strike.damaging||grace>0||hit)return;
            bool touches=false;
            for(int i=1;i<movementPath.Count;i++)if(CrossesTile(movementPath[i-1],movementPath[i],Center(strike.cell))){touches=true;break;}
            if(!touches)return;
            if((dashing||dashDuringStep)&&(shieldArmed||shieldAtStepStart)&&levels[9]>0){shieldArmed=false;levels[9]=0;strike.active=false;ShieldBlocked?.Invoke(player);return;}
            hit=true;
        }

        void StepStrike(ref Shot strike,float dt)
        {
            float remainingTime=dt;
            while(strike.active&&remainingTime>0){
                ResolveImpact(ref strike);if(!strike.active||hit)return;
                float part=Mathf.Min(remainingTime,Mathf.Max(0,strike.timer));strike.timer-=part;remainingTime-=part;
                if(strike.timer>.000001f)break;
                if(!strike.damaging){strike.damaging=true;strike.timer=strike.impactDuration;ResolveImpact(ref strike);}
                else {strike.remaining--;var next=strike.cell+strike.heading;if(strike.remaining<=0||!InBounds(next)){strike.active=false;break;}strike.sourceCell=strike.cell;strike.cell=next;strike.pos=Center(next);strike.damaging=false;strike.timer=strike.warning;}
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
                if (!f.active || f.pending || f.age < RulesFor(f.type).spawnWarning || SegmentDistance(f.pos, from, to) > .28f) continue;
                chain++; KillEnemy(i,chain,slow>0);
            }
        }

        void KillEnemy(int index,int count,bool deferred){
            var f=foes[index];if(!f.active||f.pending)return;kills++;
            score+=Mathf.RoundToInt(100*(1+.25f*Mathf.Min(count-1,12))*(1+.25f*levels[6]));
            reward-=settings.killSeconds+.35f*levels[5];f.killChain=count;f.pending=deferred;f.active=deferred;foes[index]=f;Killed?.Invoke(f.pos,count,deferred);
        }
        public void StartUpgradeWave(float seconds,float radius=11){upgradeWaveMaxRadius=Mathf.Max(.1f,radius);upgradeWaveActive=true;upgradeWaveOrigin=player;upgradeWaveRadius=0;upgradeWaveKills=0;upgradeWaveDuration=Mathf.Max(.1f,seconds);grace=Mathf.Max(grace,upgradeWaveDuration);}
        void StepUpgradeWave(float dt){
            if(!upgradeWaveActive)return;upgradeWaveRadius=Mathf.Min(upgradeWaveMaxRadius,upgradeWaveRadius+upgradeWaveMaxRadius*dt/upgradeWaveDuration);
            for(int i=0;i<foes.Length;i++)if(foes[i].active&&Vector2.Distance(foes[i].pos,upgradeWaveOrigin)<=upgradeWaveRadius){if(foes[i].pending){Killed?.Invoke(foes[i].pos,foes[i].killChain,false);foes[i].active=foes[i].pending=false;}else KillEnemy(i,++upgradeWaveKills,false);}
            for(int i=0;i<shots.Length;i++)if(shots[i].active&&Vector2.Distance(shots[i].pos,upgradeWaveOrigin)<=upgradeWaveRadius)shots[i].active=false;
            if(upgradeWaveRadius>=upgradeWaveMaxRadius){upgradeWaveActive=false;UpgradeWaveEnded?.Invoke(upgradeWaveOrigin,upgradeWaveMaxRadius);}
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
                movementPath.Add(player);
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
            moving = dashing = false; shieldArmed=false; queuedDirection = Vector2.zero;
            if (chain > 0)
            {
                score += 25 * chain * chain;
                reward -= Mathf.Min(3, .2f * chain);
                bestChain = Mathf.Max(bestChain, chain);
                ChainEnded?.Invoke(chain);
            }
            chain = 0;
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
            StepUpgradeWave(dt);
            for (int i = 0; i < foes.Length; i++) if (foes[i].active && !foes[i].pending) foes[i].age += wd;
            dashDuringStep=dashing;shieldAtStepStart=shieldArmed;
            movementPath.Clear();movementPath.Add(player);
            AdvancePlayer(dt);if(movementPath.Count==1)movementPath.Add(player);
            for(int i=0;i<foes.Length;i++)StepEnemy(i,wd);
            for(int i=0;i<shots.Length;i++)if(shots[i].active){var strike=shots[i];StepStrike(ref strike,wd);shots[i]=strike;}
            // A lethal hit interrupts by death, but cannot be used to steer an ongoing slash.
            if (hit) { moving = dashing = false; shieldArmed=false; }
            if (reward <= 0 && !tutorial && !hit && !dashing && !upgradeWaveActive) { reward = 0; draft = true; }
        }

        public Frame Capture() => new Frame
        {
            player = player, direction = direction, queuedDirection = queuedDirection, phaseIndex = phaseIndex, shieldArmed=shieldArmed, cell = cell, target = target, moving = moving, dashing = dashing, travel = travel,
            upgradeWaveActive=upgradeWaveActive, upgradeWaveOrigin=upgradeWaveOrigin, upgradeWaveRadius=upgradeWaveRadius, upgradeWaveDuration=upgradeWaveDuration, upgradeWaveMaxRadius=upgradeWaveMaxRadius, upgradeWaveKills=upgradeWaveKills,
            foes = (Foe[])foes.Clone(), shots = (Shot[])shots.Clone(), elapsed = elapsed, spawn = spawn, reward = reward,
            score = score, kills = kills, chain = chain, bestChain = bestChain, rng = rng
        };

        public void Restore(Frame f)
        {
            player = f.player; direction = f.direction; queuedDirection = f.queuedDirection; phaseIndex = f.phaseIndex; cell = f.cell; target = f.target; moving = f.moving; dashing = f.dashing; travel = f.travel; shieldArmed=f.shieldArmed && levels[9]>0 && dashing;
            upgradeWaveActive=f.upgradeWaveActive;upgradeWaveOrigin=f.upgradeWaveOrigin;upgradeWaveRadius=f.upgradeWaveRadius;upgradeWaveDuration=f.upgradeWaveDuration;upgradeWaveMaxRadius=f.upgradeWaveMaxRadius;upgradeWaveKills=f.upgradeWaveKills;
            Array.Copy(f.foes, foes, Capacity); Array.Copy(f.shots, shots, Bullets);
            elapsed = f.elapsed; spawn = f.spawn; reward = f.reward; score = f.score; kills = f.kills;
            chain = f.chain; bestChain = f.bestChain; rng = f.rng; hit = draft = false;
        }

        public int[] Offers()
        {
            var bag = new List<int>();
            for (int i = 0; i < levels.Length; i++) if (levels[i] < MaxLevel(i)) bag.Add(i);
            var result = new int[3];
            for (int i = 0; i < 3; i++)
            {
                if (bag.Count == 0) { result[i] = 10; continue; }
                int n = (int)(NextRandom() * bag.Count); result[i] = bag[n]; bag.RemoveAt(n);
            }
            return result;
        }
        public void Choose(int id) { if (id < levels.Length) levels[id]=Mathf.Min(MaxLevel(id),levels[id]+1); else score += 2500; reward = settings.upgradeSeconds; draft = false; }
    }
}
