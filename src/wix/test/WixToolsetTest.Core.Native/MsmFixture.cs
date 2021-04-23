// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreNative
{
    using WixToolset.Core.Native.Msm;
    using Xunit;

    public class MsmFixture
    {
        [Fact]
        public void CanCreateMsmInterface()
        {
            var merge = MsmInterop.GetMsmMerge();
            Assert.NotNull(merge);
        }
    }
}
