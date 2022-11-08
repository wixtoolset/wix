// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using System.Security.Principal;
    using WixInternal.TestSupport.XunitExtensions;

    public class RuntimeFactAttribute : SkippableFactAttribute
    {
        const string RequiredEnvironmentVariableName = "RuntimeTestsEnabled";

        public static bool RuntimeTestsEnabled { get; }
        public static bool RunningAsAdministrator { get; }

        static RuntimeFactAttribute()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            RunningAsAdministrator = principal.IsInRole(WindowsBuiltInRole.Administrator);

            var testsEnabledString = Environment.GetEnvironmentVariable(RequiredEnvironmentVariableName);
            RuntimeTestsEnabled = Boolean.TryParse(testsEnabledString, out var testsEnabled) && testsEnabled;
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
