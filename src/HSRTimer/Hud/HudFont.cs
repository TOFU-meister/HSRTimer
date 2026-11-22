using UnityEngine;

namespace HSRTimer
{
    internal static class HudFont
    {
#if !STORE_IL2CPP
        private static bool _warningLogged;
#endif

        internal static Font Create(int size)
        {
#if STORE_IL2CPP
            // A null style font inherits the current IMGUI skin font. The
            // Store player strips the OS font constructor used by Unity's
            // dynamic font factory. The skin already renders the Store HUD.
            return null;
#else
            try
            {
                return Font.CreateDynamicFontFromOSFont(new[]
                {
                    "PingFang SC", "Microsoft YaHei", "Noto Sans CJK SC",
                    "Noto Sans CJK", "Heiti SC", "Arial Unicode MS", "Arial",
                }, size);
            }
            catch (System.Exception ex)
            {
                if (!_warningLogged)
                {
                    _warningLogged = true;
                    Plugin.Logger.LogWarning($"HSRTimer: dynamic font creation failed: {ex.Message}");
                }
                return null;
            }
#endif
        }
    }
}
