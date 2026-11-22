using System;
using System.IO;
using HSRTimer;
using Multiplayer;
using UnityEngine;

static void Equal<T>(T expected, T actual, string message)
{
    if (!Equals(expected, actual))
        throw new InvalidOperationException($"{message}: expected {expected}, got {actual}");
}

long initialTick = GameClock.CurrentTick;
for (int step = 1; step <= 180; step++)
{
    Time.fixedTime = step * Time.fixedDeltaTime;
    if (step % 2 == 0)
        GameClock.Advance();
    long hookTick = GameClock.CurrentTick;
    GameClock.Advance();
    Equal(initialTick + step, hookTick, "Boundary hook and FixedUpdate share one tick");
    Equal(hookTick, GameClock.CurrentTick, "Repeated observation cannot advance the clock");
}
Equal(1000L, GameClock.ToMs(GameClock.Seconds(60, 0)), "Sixty playable ticks");
Equal(1250L, GameClock.ToMs(GameClock.Seconds(60, 0.25)), "Pause supplement");
Equal(60L, GameClock.MsToFrames(1000), "Persisted milliseconds convert to frames");
Equal(true, SegmentLogic.IsSegmentStart(GameState.LoadingLevel, GameState.PlayingLevel, AppSate.LoadLevel), "Load to play");
Equal(false, SegmentLogic.ShouldAccumulateFixed(GameState.PlayingLevel, AppSate.ClientWaitServerLoad, true), "Client wait");
Equal(true, SegmentLogic.ShouldAccumulatePause(GameState.Paused, true), "Active paused run");
Equal(false, SegmentLogic.IsAutoReset(GameState.PlayingLevel, GameState.Inactive, AppSate.PlayLevel, AppSate.LoadLevel, true, true), "Retry retains run");

var run = new RunState();
run.BeginSegment(20, 0.25, 0, WorkshopItemSource.BuiltIn, 0);
run.PendingEndTicks = 80;
run.PendingEndPause = 0.25;
run.EndSegment(run.PendingEndTicks.Value, run.PendingEndPause, true);
Equal<ulong?>(60, run.LastSegmentTicks, "Segment snapshots boundary tick difference");
run.BeginSegment(80, 0.25, 1, WorkshopItemSource.BuiltIn, 0);
Equal<ulong?>(null, run.PendingEndTicks, "New segment drops the old endpoint");

Game.instance = new Game { state = GameState.PlayingLevel };
App.state = AppSate.PlayLevel;
foreach (bool timerRanFirst in new[] { false, true })
{
    Time.fixedTime += Time.fixedDeltaTime;
    var boundaryRun = new RunState
    {
        InSegment = true,
        TimingActive = true,
        PlayableTicks = timerRanFirst ? 101UL : 100UL,
        PauseAccum = 0.25,
    };
    TimerCore.SetUpBoundaryStep(boundaryRun, timerRanFirst);
    Equal(timerRanFirst, TimerCore.HasProcessedCurrentPhysicsStep, "Physics callback order");
    TimerCore.RecordLevelPass();
    Equal<ulong?>(101, boundaryRun.PendingEndTicks, "Both callback orders include the pass step exactly once");
    Equal(0.25, boundaryRun.PendingEndPause, "Pause total frozen at pass");
    Equal(true, boundaryRun.LevelPassed, "Authoritative pass flag");

    Time.fixedTime += Time.fixedDeltaTime;
    TimerCore.RecordLevelPass();
    Equal<ulong?>(101, boundaryRun.PendingEndTicks, "Duplicate pass cannot move the end tick");
}

var waiting = new RunState { InSegment = true, TimingActive = true, PlayableTicks = 100 };
App.state = AppSate.ClientWaitServerLoad;
TimerCore.SetUpBoundaryStep(waiting, fixedUpdateAlreadyRan: false);
TimerCore.RecordLevelPass();
Equal<ulong?>(100, waiting.PendingEndTicks, "A non-playable boundary cannot add a step");
var retry = new RunState { InSegment = true, TimingActive = true, Retrying = true, PlayableTicks = 40, PauseAccum = 0.5 };
TimerCore.SetUpBoundaryStep(retry, fixedUpdateAlreadyRan: false);
TimerCore.RecordLevelPass();
Equal<ulong?>(null, retry.PendingEndTicks, "Abandoned retry attempt has no finish");
TimerCore.RecordSegmentStart();
Equal((true, 40UL, 0.5), TimerCore.Instance.StartBoundary, "AfterLoad snapshot captures ticks and pause");

string directory = Path.Combine(Path.GetTempPath(), "hsrtimer-store-" + Guid.NewGuid());
Equal(false, StoreDataPolicy.PbWritesEnabled, "Preview forbids PB writes");
Equal(Path.Combine(directory, "markers"), StoreDataPolicy.ResolvePreviewMarkerPath(directory, Path.Combine("..", "HSRTimer")), "Parent paths cannot reach legacy marker data");
Equal(Path.Combine(directory, "markers"), StoreDataPolicy.ResolvePreviewMarkerPath(directory, Path.GetTempPath()), "Absolute paths cannot reach legacy marker data");
Equal(Path.Combine(directory, "custom"), StoreDataPolicy.ResolvePreviewMarkerPath(directory, "custom"), "Preview marker path can remain inside the isolated directory");
Directory.CreateDirectory(directory);
try
{
    string meta = Path.Combine(directory, "meta.json");
    string samples = Path.Combine(directory, "sample.jsonl");
    File.WriteAllText(meta, SubsegmentFileStore.MakeMetaJson("IL", "Mansion", null, "Any", new[] { "Mansion" }, 1250, 1));
    Equal(true, SubsegmentFileStore.TryLoadMeta(meta, out var loadedMeta), "Metadata round trip");
    Equal(1250L, loadedMeta.total_ms, "Metadata timing units");
    Equal("Mansion", loadedMeta.level_ids[0], "Metadata array");
    string sampleJson = SubsegmentFileStore.MakeSampleJson(new[] { new SubsegmentSample { seq = 1, t_ms = 1250, px = 2.5f } });
    Equal(false, sampleJson.Contains("Position"), "Runtime-only property excluded");
    File.WriteAllText(samples, sampleJson + "{\"seq\":2,\"t_ms\":1300,\"px\":3.5,\"plane_radius\":0.4}\n{\n");
    Equal(true, SubsegmentFileStore.TryLoadSamples(samples, out var loaded), "Sample round trip and legacy import");
    Equal(2, loaded.Count, "Malformed JSONL skipped without discarding good rows");
    Equal(1300L, loaded[1].t_ms, "Legacy sample time");
    Equal(3.5f, loaded[1].px, "Legacy sample position");
}
finally
{
    Directory.Delete(directory, recursive: true);
}

Console.WriteLine("Store logic checks passed.");
