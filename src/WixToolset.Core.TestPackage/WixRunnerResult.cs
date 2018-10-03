// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.TestPackage
{
    using System;
    using System.Linq;
    using WixToolset.Data;
    using Xunit;

    public class WixRunnerResult
    {
        public int ExitCode { get; set; }

        public Message[] Messages { get; set; }

        public WixRunnerResult AssertSuccess()
        {
            Assert.True(0 == this.ExitCode, $"MSBuild failed unexpectedly. Output:\r\n{String.Join("\r\n", this.Messages.Select(m => m.ToString()).ToArray())}");
            return this;
        }
    }
}
