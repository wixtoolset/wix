// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;

    public class LongRuntimeFactAttribute : RuntimeFactAttribute
    {
        const string RequiredEnvironmentVariableName = "LongRuntimeTestsEnabled";

        public static bool LongRuntimeTestsEnabled { get; }

        static LongRuntimeFactAttribute()
        {
            var testsEnabledString = Environment.GetEnvironmentVariable(RequiredEnvironmentVariableName);
            LongRuntimeTestsEnabled = Boolean.TryParse(testsEnabledString, out var testsEnabled) && testsEnabled;
        }

        public LongRuntimeFactAttribute()
        {
            if (!LongRuntimeTestsEnabled)
            {
                this.Skip = $"These tests take a long time to run, so the {RequiredEnvironmentVariableName} environment variable must be set to true.";
            }
        }
    }
}
