#if STORE_IL2CPP
using Il2CppInterop.Runtime.Injection;

namespace HSRTimer
{
    internal static class StoreRuntime
    {
        internal static void InitialiseComponents()
        {
            ClassInjector.RegisterTypeInIl2Cpp<TimerCore>();
            ClassInjector.RegisterTypeInIl2Cpp<TimerHud>();
            ClassInjector.RegisterTypeInIl2Cpp<ProgressIndicatorMover>();
            ClassInjector.RegisterTypeInIl2Cpp<SettingsPanel>();
            ClassInjector.RegisterTypeInIl2Cpp<UpdaterService>();
            ClassInjector.RegisterTypeInIl2Cpp<SubsegmentManager>();
            ClassInjector.RegisterTypeInIl2Cpp<LeaderboardHud>();
            ClassInjector.RegisterTypeInIl2Cpp<MarkersManager>();
            ClassInjector.RegisterTypeInIl2Cpp<MarkerOverlay>();
        }
    }
}
#endif
