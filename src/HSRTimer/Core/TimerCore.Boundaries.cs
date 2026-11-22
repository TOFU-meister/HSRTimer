using Multiplayer;
using UnityEngine;

namespace HSRTimer
{
    public partial class TimerCore
    {
        /// <summary>
        /// True when this plugin's <see cref="FixedUpdate"/> has already run for
        /// the current physics step (used by the boundary hooks to decide
        /// whether the current step is already in <see cref="RunState.PlayableTicks"/>).
        /// </summary>
        public static bool HasProcessedCurrentPhysicsStep
            => Instance != null && Instance._lastFixedTime == Time.fixedTime;

        // ── Precise boundary hooks (TB-3) ─────────────────────────────────
        /// <summary>
        /// Latch the authoritative segment-start tick. Called from the
        /// <c>Game.AfterLoad</c> postfix — the moment the game assigns
        /// <c>state = PlayingLevel</c> (R1.2.2). The value is consumed by
        /// <see cref="StartSegment"/> on the next physics step. Recording the
        /// tick here makes the boundary independent of the script execution
        /// order that used to add a random ±1 tick.
        /// </summary>
        public static void RecordSegmentStart()
        {
            var core = Instance;
            var st = State;
            if (core == null || st == null)
                return;
            core._segmentStartLatched = true;
            core._segmentStartTicks = st.PlayableTicks;
            core._segmentStartPause = st.PauseAccum;
            long loadLevel = Game.instance != null ? Game.instance.currentLevelNumber : -1;
            LogBoundary($"load  level={loadLevel} tick={st.PlayableTicks} step={GameClock.CurrentTick} processed={HasProcessedCurrentPhysicsStep}");
        }

        /// <summary>
        /// Latch the completion flag and the authoritative segment-end tick when
        /// the game detects a genuine level pass (<c>Game.Fall</c> taking the
        /// <c>passedLevel</c> branch, R1.4.2). The end tick is latched here, at
        /// the <c>Game.Fall</c> pass detection — the sole authoritative segment
        /// end (R1.4.2/R1.11.3) — so the exact pass step is the segment end,
        /// including the pass frame itself ("终点含最后一帧", TB-4).
        /// <para>
        /// The <c>Game.Fall</c> tick is logged as a <c>pass</c> boundary line
        /// carrying the end tick, so <c>hsr clock history</c> shows the pass
        /// detection as the segment end.
        /// </para>
        /// Suppressed under <c>Retrying</c> (R6) and outside a segment, so an
        /// abandoned attempt never ends a segment (TB-7).
        /// </summary>
        public static void RecordLevelPass()
        {
            var st = State;
            if (st == null)
                return;
            if (!st.InSegment || st.Retrying)
                return;

            // Game.Fall is the authoritative segment end. The tick is the step's
            // exact index, including the pass step itself (TB-4); the poll then
            // freezes accumulation at it and records the segment on the observed
            // state flip.
            if (st.PendingEndTicks.HasValue)
                return;
            st.LevelPassed = true;
            // The hook can run before or after this plugin's FixedUpdate within
            // the same physics step. When it runs first, the pass step has not
            // been counted yet — include it explicitly (but only if this step is
            // actually a playable one, so a boundary in a non-accumulating
            // context cannot over-count). Accumulation is frozen from here so it
            // is never double-counted.
            ulong endTicks = st.PlayableTicks;
            if (!HasProcessedCurrentPhysicsStep
                && Game.instance != null
                && SegmentLogic.ShouldAccumulateFixed(Game.instance.state, App.state, st.TimingActive))
            {
                endTicks++;
            }
            st.PlayableTicks = endTicks;
            st.PendingEndTicks = endTicks;
            st.PendingEndPause = st.PauseAccum;
            st.GameTimeSeconds = GameClock.Seconds(st.PlayableTicks, st.PauseAccum);
            LogBoundary($"pass  level={st.CurrentLevelNumber} tick={endTicks} step={GameClock.CurrentTick}");
        }

        /// <summary>
        /// Latch only the completion flag (used by the <c>Game.EnterPassZone</c>
        /// postfix, R1.4.2).
        /// </summary>
        public static void LatchLevelPassed()
        {
            var st = State;
            if (st == null || !st.InSegment || st.LevelPassed)
                return;
            st.LevelPassed = true;
            LogBoundary($"zone  level={st.CurrentLevelNumber} tick={st.PlayableTicks} step={GameClock.CurrentTick}");
        }
    }
}
