// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.WixCop
{
    using System;
    using System.Linq;
    using WixToolset.Data;
    using Xunit;

    public class WixCopRunnerResult
    {
        public int ExitCode { get; set; }

        public Message[] Messages { get; set; }

        public WixCopRunnerResult AssertSuccess()
        {
            Assert.True(0 == this.ExitCode, $"WixCop failed unexpectedly. Output:\r\n{String.Join("\r\n", this.Messages.Select(m => m.ToString()).ToArray())}");
            return this;
        }
    }
}
