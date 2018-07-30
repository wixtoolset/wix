// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
{
    using System;
    using Xunit;

    public class MsbuildRunnerResult
    {
        public int ExitCode { get; set; }

        public string[] Output { get; set; }

        public void AssertSuccess()
        {
            Assert.True(0 == this.ExitCode, $"MSBuild failed unexpectedly. Output:\r\n{String.Join("\r\n", this.Output)}");
        }
    }
}
