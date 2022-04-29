// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MsiE2E
{
    using System;
    using System.Security.Principal;

    public class MsiE2EFixture
    {
        const string RequiredEnvironmentVariableName = "RuntimeTestsEnabled";

        public MsiE2EFixture()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                throw new InvalidOperationException("These tests must run elevated.");
            }

            var testsEnabledString = Environment.GetEnvironmentVariable(RequiredEnvironmentVariableName);
            if (!bool.TryParse(testsEnabledString, out var testsEnabled) || !testsEnabled)
            {
                throw new InvalidOperationException($"These tests affect machine state. Set the {RequiredEnvironmentVariableName} environment variable to true to accept the consequences.");
            }
        }
    }
}
