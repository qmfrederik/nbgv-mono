using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.DotNet.PlatformAbstractions.Native;

namespace NerdBank.GitVersioning.Mono
{
    public class LoadLibGit2Task : Task
    {
        private const string libdl = "libdl.so.2";

        [DllImport(libdl)]
        public static unsafe extern void* dlopen(byte* filename, int flags);

        public static int RTLD_LAZY = 1;

        public unsafe override bool Execute()
        {
            if (Type.GetType ("Mono.Runtime") == null)
            {
                this.Log.LogWarning("Not running on Mono. Skipping LoadLibGit2Task.");
                return true;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                this.Log.LogWarning("Not running on Linux. Skipping LoadLibGit2Task.");
                return true;
            }

            var distroId = PlatformApis.GetDistroId();
            var versionId = PlatformApis.GetDistroVersionId();

            if (!Version.TryParse(versionId, out Version version))
            {
                this.Log.LogWarning($"Could not distro parse version '{versionId}'. Skipping LoadLibGit2Task.");
                return false;
            }

            string rid = "linux-x64";

            if (distroId == "ubuntu")
            {
                if (version < new Version(18, 04))
                {
                    this.Log.LogMessage($"Running on '{distroId} {versionId}', which predates Ubuntu 18.04. Skipping LoadLibGit2Task.");
                    return true;
                }

                rid = "ubuntu.18.04-x64";
            }
            else if(distroId == "debian")
            {
                if (version < new Version(9, 0))
                {
                    this.Log.LogMessage($"Running on '{distroId} {versionId}', which predates Debian 9. Skipping LoadLibGit2Task.");
                    return true;
                }

                rid = "debian.9-x64";
            }
            else
            {
                this.Log.LogWarning($"Running on '{distroId}' which is not Ubuntu nor Debian. Skipping LoadLibGit2Task.");
                return true;
            }

            // Get the path to the native library. Start with the path to the
            // current assembly, which lives in lib/netstandard2.0/, and work our
            // way up to build/runtimes/{rid}/native/libgit2-572e4d8.so
            // from there.
            var path = typeof(LoadLibGit2Task).Assembly.Location;
            path = Path.GetDirectoryName(path);
            path = Path.Combine(path, $"../../build/runtimes/{rid}/native/libgit2-572e4d8.so");
            path = Path.GetFullPath(path);

            this.Log.LogMessage($"Loading libgit2.so from {path}");

            fixed (byte* filename = Encoding.UTF8.GetBytes(path))
            {
                var libHandle = new IntPtr(dlopen(filename, RTLD_LAZY));
                this.Log.LogMessage($"Loaded libgit2.so as {libHandle}");

                if (libHandle == IntPtr.Zero)
                {
                    this.Log.LogError($"Failed to load libgit2.so from {path}");
                }

                return libHandle != IntPtr.Zero;
            }
        }
    }
}
