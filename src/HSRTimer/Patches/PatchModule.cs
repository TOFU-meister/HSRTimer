using HarmonyLib;

namespace HSRTimer
{
    /// <summary>
    /// Applies all HSRTimer Harmony patches with a stable instance id. Only the
    /// voiceline hooks (NarrativeBlock.Play, SubtitleManager.PlayNarrative),
    /// the pause-menu load/restart hooks (PauseMenu.LoadClick,
    /// PauseMenu.RestartClick), the Jumpless jump-key suppression
    /// (HumanControls.HandleInput), and the precise timing-boundary hooks
    /// (Game.AfterLoad, Game.EnterPassZone, Game.Fall — TB-3) are patched —
    /// everything else is polled from public game fields for resilience.
    /// </summary>
    internal static class PatchModule
    {
        private const string InstanceId = PluginInfo.PLUGIN_GUID;

        public static void Apply()
        {
            try
            {
                var harmony = new Harmony(InstanceId);
#if STORE_IL2CPP
                // Optional hooks can differ between game builds. Never let a
                // tag or menu hook prevent the timing hooks from installing.
                if (!ApplyStorePatch(harmony, typeof(GameAfterLoadBoundaryPatch)) ||
                    !ApplyStorePatch(harmony, typeof(GameFallBoundaryPatch)))
                {
                    harmony.UnpatchSelf();
                    throw new System.InvalidOperationException("Required Store timing hooks could not be installed.");
                }
                ApplyStorePatch(harmony, typeof(GameEnterPassZoneBoundaryPatch));
                ApplyStorePatch(harmony, typeof(PauseMenuRestartPatch));
                ApplyStorePatch(harmony, typeof(PauseMenuLoadPatch));
                ApplyStorePatch(harmony, typeof(HumanControlsHandleInputPatch));
                ApplyStorePatch(harmony, typeof(NarrativeBlockPlayPatch));
                ApplyStorePatch(harmony, typeof(SubtitleManagerPlayNarrativePatch));
#else
                harmony.PatchAll(typeof(PatchModule).Assembly);
#endif
                Plugin.Logger.LogInfo("HSRTimer: Harmony patches applied.");
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"HSRTimer: failed to apply Harmony patches: {ex}");
#if STORE_IL2CPP
                throw;
#endif
            }
        }

#if STORE_IL2CPP
        private static bool ApplyStorePatch(Harmony harmony, System.Type patch)
        {
            try
            {
                var methods = harmony.CreateClassProcessor(patch).Patch();
                if (methods == null || methods.Count == 0)
                    throw new System.InvalidOperationException("No patch method was installed.");
                Plugin.Logger.LogInfo($"HSRTimer: installed {patch.Name}.");
                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogWarning($"HSRTimer: {patch.Name} unavailable: {ex}");
                return false;
            }
        }
#endif
    }
}
