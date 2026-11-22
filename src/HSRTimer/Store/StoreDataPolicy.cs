#if STORE_IL2CPP
using System;
using System.IO;

namespace HSRTimer
{
    internal static class StoreDataPolicy
    {
#if STORE_PREVIEW
        internal static bool PbWritesEnabled => false;
#else
        internal static bool PbWritesEnabled => true;
#endif

        internal const string DataDirectory = "HSRTimer-MicrosoftStore";

#if STORE_PREVIEW
        internal static string ResolvePreviewMarkerPath(string dataDirectory, string configuredPath)
        {
            string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(dataDirectory))
                + Path.DirectorySeparatorChar;
            try
            {
                string candidate = Path.GetFullPath(configuredPath, root);
                if (candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase) && candidate.Length > root.Length)
                    return candidate;
            }
            catch (ArgumentException)
            {
                // A copied configuration may contain a path from another PC.
            }

            return Path.Combine(root, "markers");
        }
#endif
    }
}
#endif
