// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Sources from https://github.com/dotnet/core-setup/blob/master/src/managed/Microsoft.DotNet.PlatformAbstractions/Native/PlatformApis.cs

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.PlatformAbstractions.Native
{
    internal static class PlatformApis
    {
        private class DistroInfo
        {
            public string Id;
            public string VersionId;
        }

        private static readonly Lazy<DistroInfo> _distroInfo = new Lazy<DistroInfo>(LoadDistroInfo);

        public static string GetDistroId()
        {
            return _distroInfo.Value?.Id;
        }

        public static string GetDistroVersionId()
        {
            return _distroInfo.Value?.VersionId;
        }

        private static DistroInfo LoadDistroInfo()
        {
            DistroInfo result = null;

            // Sample os-release file:
            //   NAME="Ubuntu"
            //   VERSION = "14.04.3 LTS, Trusty Tahr"
            //   ID = ubuntu
            //   ID_LIKE = debian
            //   PRETTY_NAME = "Ubuntu 14.04.3 LTS"
            //   VERSION_ID = "14.04"
            //   HOME_URL = "http://www.ubuntu.com/"
            //   SUPPORT_URL = "http://help.ubuntu.com/"
            //   BUG_REPORT_URL = "http://bugs.launchpad.net/ubuntu/"
            // We use ID and VERSION_ID

            if (File.Exists("/etc/os-release"))
            {
                var lines = File.ReadAllLines("/etc/os-release");
                result = new DistroInfo();
                foreach (var line in lines)
                {
                    if (line.StartsWith("ID=", StringComparison.Ordinal))
                    {
                        result.Id = line.Substring(3).Trim('"', '\'');
                    }
                    else if (line.StartsWith("VERSION_ID=", StringComparison.Ordinal))
                    {
                        result.VersionId = line.Substring(11).Trim('"', '\'');
                    }
                }
            }
            else if (File.Exists("/etc/redhat-release"))
            {
                var lines = File.ReadAllLines("/etc/redhat-release");

                if (lines.Length >= 1)
                {
                    string line = lines[0];
                    if (line.StartsWith("Red Hat Enterprise Linux Server release 6.") ||
                        line.StartsWith("CentOS release 6."))
                    {
                        result = new DistroInfo();
                        result.Id = "rhel";
                        result.VersionId = "6";
                    }
                }
            }

            if (result != null)
            {
                result = NormalizeDistroInfo(result);
            }
            
            return result;
        }

        // For some distros, we don't want to use the full version from VERSION_ID. One example is
        // Red Hat Enterprise Linux, which includes a minor version in their VERSION_ID but minor
        // versions are backwards compatable.
        //
        // In this case, we'll normalized RIDs like 'rhel.7.2' and 'rhel.7.3' to a generic
        // 'rhel.7'. This brings RHEL in line with other distros like CentOS or Debian which
        // don't put minor version numbers in their VERSION_ID fields because all minor versions
        // are backwards compatible.
        private static DistroInfo NormalizeDistroInfo(DistroInfo distroInfo)
        {
            // Handle if VersionId is null by just setting the index to -1.
            int lastVersionNumberSeparatorIndex = distroInfo.VersionId?.IndexOf('.') ?? -1;

            if (lastVersionNumberSeparatorIndex != -1 && distroInfo.Id == "alpine")
            {
                // For Alpine, the version reported has three components, so we need to find the second version separator
                lastVersionNumberSeparatorIndex = distroInfo.VersionId.IndexOf('.', lastVersionNumberSeparatorIndex + 1);
            }

            if (lastVersionNumberSeparatorIndex != -1 && (distroInfo.Id == "rhel" || distroInfo.Id == "alpine"))
            {
                distroInfo.VersionId = distroInfo.VersionId.Substring(0, lastVersionNumberSeparatorIndex);
            }

            return distroInfo;
        }
    }
}