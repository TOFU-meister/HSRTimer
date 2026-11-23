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

        internal static string DataRoot { get; } = GetDataRoot();

        private static string GetDataRoot()
        {
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(userFolder))
                userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(userFolder))
                throw new IOException("Could not locate the current user's data directory.");
            return Path.Combine(userFolder, DataDirectory);
        }

        internal static string ResolveRecordPath(string dataDirectory, string configuredPath, string fallback)
        {
            try
            {
                if (!Path.IsPathRooted(configuredPath))
                {
                    string candidate = Path.GetFullPath(configuredPath, dataDirectory);
                    if (IsInside(dataDirectory, candidate))
                        return candidate;
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is UnauthorizedAccessException)
            {
                // Paths copied from another installation may be invalid here.
            }
            return Path.Combine(dataDirectory, fallback);
        }

        private static bool IsInside(string dataDirectory, string path)
        {
            string prefix = Path.TrimEndingDirectorySeparator(Path.GetFullPath(dataDirectory)) + Path.DirectorySeparatorChar;
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return path.Length > prefix.Length && path.StartsWith(prefix, comparison);
        }

        internal static void EnsureSafePath(string dataDirectory, string path)
        {
            string current = Path.GetFullPath(path);
            if (!IsInside(dataDirectory, current))
                throw new IOException("The Store write path must be inside " + dataDirectory);

            // Reject links/junctions, including ancestors of the Store root.
            // An otherwise isolated path must not redirect writes into Steam.
            while (current != null)
            {
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("Store write path crosses a link: " + current);
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                current = Path.GetDirectoryName(current);
            }
        }
    }
}
#endif
