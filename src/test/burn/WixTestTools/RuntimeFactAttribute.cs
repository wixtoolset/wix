// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using System.Security.Principal;
    using WixInternal.TestSupport.XunitExtensions;
    using System.Runtime.InteropServices;

    public class RuntimeFactAttribute : SkippableFactAttribute
    {
        const string RequiredEnvironmentVariableName = "RuntimeTestsEnabled";

        public static bool RuntimeTestsEnabled { get; }
        public static bool RunningAsAdministrator { get; }
        public static bool RunningOnWindowsServer { get; }

        [DllImport("shlwapi.dll", SetLastError = true, EntryPoint = "#437")]
        private static extern bool IsOS(int os);
        private static bool IsWindowsServer()
        {
            const int OS_ANYSERVER = 29;
            return IsOS(OS_ANYSERVER);
        }


        static RuntimeFactAttribute()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            RunningAsAdministrator = principal.IsInRole(WindowsBuiltInRole.Administrator);

            var testsEnabledString = Environment.GetEnvironmentVariable(RequiredEnvironmentVariableName);
            RuntimeTestsEnabled = Boolean.TryParse(testsEnabledString, out var testsEnabled) && testsEnabled;

            RunningOnWindowsServer = IsWindowsServer();
        }

        private bool _RequireWindowsServer;
        public bool RequireWindowsServer
        {
            get
            {
                return _RequireWindowsServer;
            }
            set
            {
                _RequireWindowsServer = value;
                if (_RequireWindowsServer && !RunningOnWindowsServer)
                {
                    this.Skip = $"These tests are only run on Windows Server";
                }
            }
        }

        public RuntimeFactAttribute()
        {
            if (!RuntimeTestsEnabled || !RunningAsAdministrator)
            {
                this.Skip = $"These tests must run elevated ({(RunningAsAdministrator ? "passed" : "failed")}). These tests affect machine state. To accept the consequences, set the {RequiredEnvironmentVariableName} environment variable to true ({(RuntimeTestsEnabled ? "passed" : "failed")}).";
            }
        }
    }
}
