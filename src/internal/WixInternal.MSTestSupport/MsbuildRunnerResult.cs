// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixInternal.MSTestSupport
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class MsbuildRunnerResult
    {
        public int ExitCode { get; set; }

        public string[] Output { get; set; }

        public void AssertSuccess()
        {
            Assert.IsTrue(0 == this.ExitCode, $"MSBuild failed unexpectedly. Output:{Environment.NewLine}{String.Join(Environment.NewLine, this.Output)}");
        }
    }
}
