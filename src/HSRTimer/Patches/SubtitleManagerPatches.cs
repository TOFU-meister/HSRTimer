using HarmonyLib;
using UnityEngine;

namespace HSRTimer
{
    /// <summary>
    /// Harmony postfix on <c>SubtitleManager.PlayNarrative(AudioClip)</c>: a
    /// robust second signal that a narrative clip actually started playing.
    /// This is what flips the Voiceline tracker back to "satisfied" when the
    /// special Easter clip plays, defeating the common voiceline-skip trick
    /// that leaves the Easter AudioSource present-but-silent (R3.6 / R5.5).
    /// Guarded so a failure logs rather than crashes the game (N7).
    /// </summary>
    [HarmonyPatch(typeof(SubtitleManager), nameof(SubtitleManager.PlayNarrative))]
    internal static class SubtitleManagerPlayNarrativePatch
    {
        private static void Postfix(AudioClip __0)
        {
            try
            {
                var tracker = VoicelineTracker.Current;
                if (tracker != null)
                    tracker.MarkNarrativeClip(__0);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogWarning($"HSRTimer: SubtitleManager.PlayNarrative postfix failed: {ex.Message}");
            }
        }
    }
}
