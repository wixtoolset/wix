// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.IO;
    using WixToolset.Data.WindowsInstaller;

    internal class DataLoader
    {
        public static bool TryLoadWindowsInstallerData(string path, out WindowsInstallerData data)
        {
            return TryLoadWindowsInstallerData(path, false, out data);
        }

        public static bool TryLoadWindowsInstallerData(string path, bool suppressVersionCheck, out WindowsInstallerData data)
        {
            data = null;

            var extension = Path.GetExtension(path);

            // If the path is _not_ obviously a Windows Installer database, let's try opening it as
            // our own data file format.
            if (!extension.Equals(".msi", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".msm", StringComparison.OrdinalIgnoreCase))
            {
                (data, _) = LoadWindowsInstallerDataSafely(path, suppressVersionCheck);
            }

            return data != null;
        }

        public static (WindowsInstallerData, Exception) LoadWindowsInstallerDataSafely(string path, bool suppressVersionCheck = false)
        {
            WindowsInstallerData data = null;
            Exception exception = null;

            try
            {
                data = WindowsInstallerData.Load(path, suppressVersionCheck);
            }
            catch (Exception e)
            {
                exception = e;
            }

            return (data, exception);
        }
    }
}
