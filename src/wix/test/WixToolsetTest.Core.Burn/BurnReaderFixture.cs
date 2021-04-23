// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Core.Burn
{
    using System;
    using WixToolset.Core.Burn.Bundles;
    using Xunit;

    public class BurnReaderFixture
    {
        [Fact]
        public void CanReadUInt16Max()
        {
            var bytes = new byte[] { 0xFF, 0xFF };
            var offset = 0u;

            var result = BurnCommon.ReadUInt16(bytes, offset);

            Assert.Equal(UInt16.MaxValue, result);
        }

        [Fact]
        public void CanReadUInt32Max()
        {
            var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var offset = 0u;

            var result = BurnCommon.ReadUInt32(bytes, offset);

            Assert.Equal(UInt32.MaxValue, result);
        }

        [Fact]
        public void CanReadUInt64Max()
        {
            var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            var offset = 0u;

            var result = BurnCommon.ReadUInt64(bytes, offset);

            Assert.Equal(UInt64.MaxValue, result);
        }
    }
}
