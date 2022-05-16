// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Data
{
    using System;
    using WixToolset.Data;
    using Xunit;

    public class WixVerFixture
    {
        [Fact]
        public void CannotParseEmptyStringAsVersion()
        {
            Assert.False(WixVersion.TryParse(String.Empty, out var version));
            Assert.Null(version);
        }

        [Fact]
        public void CannotParseInvalidStringAsVersion()
        {
            Assert.False(WixVersion.TryParse("invalid", out var version));
            Assert.Null(version);
        }

        [Fact]
        public void CanParseFourPartVersion()
        {
            Assert.True(WixVersion.TryParse("1.2.3.4", out var version));
            Assert.Null(version.Prefix);
            Assert.Equal((uint)1, version.Major);
            Assert.Equal((uint)2, version.Minor);
            Assert.Equal((uint)3, version.Patch);
            Assert.Equal((uint)4, version.Revision);
            Assert.True(version.HasMajor);
            Assert.True(version.HasMinor);
            Assert.True(version.HasPatch);
            Assert.True(version.HasRevision);
            Assert.Null(version.Labels);
            Assert.Null(version.Metadata);
        }

        [Fact]
        public void CanParseThreePartVersion()
        {
            Assert.True(WixVersion.TryParse("1.2.3", out var version));
            Assert.Null(version.Prefix);
            Assert.Equal((uint)1, version.Major);
            Assert.Equal((uint)2, version.Minor);
            Assert.Equal((uint)3, version.Patch);
            Assert.Equal((uint)0, version.Revision);
            Assert.True(version.HasMajor);
            Assert.True(version.HasMinor);
            Assert.True(version.HasPatch);
            Assert.False(version.HasRevision);
            Assert.Null(version.Labels);
            Assert.Null(version.Metadata);
        }

        [Fact]
        public void CanParseFourPartVersionWithTrailingZero()
        {
            Assert.True(WixVersion.TryParse("1.2.3.0", out var version));
            Assert.Null(version.Prefix);
            Assert.Equal((uint)1, version.Major);
            Assert.Equal((uint)2, version.Minor);
            Assert.Equal((uint)3, version.Patch);
            Assert.Equal((uint)0, version.Revision);
            Assert.True(version.HasMajor);
            Assert.True(version.HasMinor);
            Assert.True(version.HasPatch);
            Assert.True(version.HasRevision);
            Assert.Null(version.Labels);
            Assert.Null(version.Metadata);
        }

        [Fact]
        public void CanParseNumericReleaseLabels()
        {
            Assert.True(WixVersion.TryParse("1.2-19", out var version));
            Assert.Null(version.Prefix);
            Assert.Equal((uint)1, version.Major);
            Assert.Equal((uint)2, version.Minor);
            Assert.Equal((uint)0, version.Patch);
            Assert.Equal((uint)0, version.Revision);
            Assert.True(version.HasMajor);
            Assert.True(version.HasMinor);
            Assert.False(version.HasPatch);
            Assert.False(version.HasRevision);
            Assert.Equal("19", version.Labels[0].Label);
            Assert.Equal((uint)19, version.Labels[0].Numeric);
            Assert.Null(version.Metadata);
        }

        [Fact]
        public void CanParseDottedNumericReleaseLabels()
        {
            Assert.True(WixVersion.TryParse("1.2-2.0", out var version));
            Assert.Null(version.Prefix);
            Assert.Equal((uint)1, version.Major);
            Assert.Equal((uint)2, version.Minor);
            Assert.Equal((uint)0, version.Patch);
            Assert.Equal((uint)0, version.Revision);
            Assert.True(version.HasMajor);
            Assert.True(version.HasMinor);
            Assert.False(version.HasPatch);
            Assert.False(version.HasRevision);
            Assert.Equal("2", version.Labels[0].Label);
            Assert.Equal((uint)2, version.Labels[0].Numeric);
            Assert.Equal("0", version.Labels[1].Label);
            Assert.Equal((uint)0, version.Labels[1].Numeric);
            Assert.Null(version.Metadata);
        }

        [Fact]
        public void CanParseHyphenAsVersionSeparator()
        {
            Assert.True(WixVersion.TryParse("0.0.1-a", out var version));
            Assert.Null(version.Prefix);
            Assert.Equal((uint)0, version.Major);
            Assert.Equal((uint)0, version.Minor);
            Assert.Equal((uint)1, version.Patch);
            Assert.Equal((uint)0, version.Revision);
            Assert.True(version.HasMajor);
            Assert.True(version.HasMinor);
            Assert.True(version.HasPatch);
            Assert.False(version.HasRevision);
            Assert.Equal("a", version.Labels[0].Label);
            Assert.Null(version.Labels[0].Numeric);
            Assert.Null(version.Metadata);
        }

        [Fact]
        public void CanParseIgnoringLeadingZeros()
        {
            Assert.True(WixVersion.TryParse("0.01-a.000", out var version));
            Assert.Null(version.Prefix);
            Assert.Equal((uint)0, version.Major);
            Assert.Equal((uint)1, version.Minor);
            Assert.Equal((uint)0, version.Patch);
            Assert.Equal((uint)0, version.Revision);
            Assert.True(version.HasMajor);
            Assert.True(version.HasMinor);
            Assert.False(version.HasPatch);
            Assert.False(version.HasRevision);
            Assert.Equal("a", version.Labels[0].Label);
            Assert.Null(version.Labels[0].Numeric);
            Assert.Equal("000", version.Labels[1].Label);
            Assert.Equal((uint)0, version.Labels[1].Numeric);
            Assert.Null(version.Metadata);
        }

        [Fact]
        public void CanParseMetadata()
        {
            Assert.True(WixVersion.TryParse("1.2.3+abcd", out var version));
            Assert.Null(version.Prefix);
            Assert.Equal((uint)1, version.Major);
            Assert.Equal((uint)2, version.Minor);
            Assert.Equal((uint)3, version.Patch);
            Assert.Equal((uint)0, version.Revision);
            Assert.True(version.HasMajor);
            Assert.True(version.HasMinor);
            Assert.True(version.HasPatch);
            Assert.False(version.HasRevision);
            Assert.Null(version.Labels);
            Assert.Equal("abcd", version.Metadata);
        }

        [Fact]
        public void CannotParseUnexpectedContentAsMetadata()
        {
            Assert.False(WixVersion.TryParse("1.2.3.abcd", out var version));
            Assert.Null(version);
            Assert.False(WixVersion.TryParse("1.2.3.-abcd", out version));
            Assert.Null(version);
        }

        [Fact]
        public void CanParseLeadingPrefix()
        {
            Assert.True(WixVersion.TryParse("v10.20.30.40", out var version));
            Assert.Equal('v', version.Prefix);
            Assert.Equal((uint)10, version.Major);
            Assert.Equal((uint)20, version.Minor);
            Assert.Equal((uint)30, version.Patch);
            Assert.Equal((uint)40, version.Revision);
            Assert.True(version.HasMajor);
            Assert.True(version.HasMinor);
            Assert.True(version.HasPatch);
            Assert.True(version.HasRevision);
            Assert.Null(version.Labels);
            Assert.Null(version.Metadata);

            Assert.True(WixVersion.TryParse("V100.200.300.400", out var version2));
            Assert.Equal('V', version2.Prefix);
            Assert.Equal((uint)100, version2.Major);
            Assert.Equal((uint)200, version2.Minor);
            Assert.Equal((uint)300, version2.Patch);
            Assert.Equal((uint)400, version2.Revision);
            Assert.True(version.HasMajor);
            Assert.True(version.HasMinor);
            Assert.True(version.HasPatch);
            Assert.True(version.HasRevision);
            Assert.Null(version2.Labels);
            Assert.Null(version2.Metadata);
        }

        [Fact]
        public void CanParseVeryLargeNumbers()
        {
            Assert.True(WixVersion.TryParse("4294967295.4294967295.4294967295.4294967295", out var version));
            Assert.Null(version.Prefix);
            Assert.Equal(4294967295, version.Major);
            Assert.Equal(4294967295, version.Minor);
            Assert.Equal(4294967295, version.Patch);
            Assert.Equal(4294967295, version.Revision);
            Assert.True(version.HasMajor);
            Assert.True(version.HasMinor);
            Assert.True(version.HasPatch);
            Assert.True(version.HasRevision);
            Assert.Null(version.Labels);
            Assert.Null(version.Metadata);
        }

        [Fact]
        public void CannotParseTooLargeNumbers()
        {
            Assert.False(WixVersion.TryParse("4294967296.4294967296.4294967296.4294967296", out var version));
            Assert.Null(version);
        }

        [Fact]
        public void CanParseLabelsWithMetadata()
        {
            Assert.True(WixVersion.TryParse("1.2.3.4-a.b.c.d.5+abc123", out var version));
            Assert.Null(version.Prefix);
            Assert.Equal((uint)1, version.Major);
            Assert.Equal((uint)2, version.Minor);
            Assert.Equal((uint)3, version.Patch);
            Assert.Equal((uint)4, version.Revision);
            Assert.True(version.HasMajor);
            Assert.True(version.HasMinor);
            Assert.True(version.HasPatch);
            Assert.True(version.HasRevision);
            Assert.Equal("a", version.Labels[0].Label);
            Assert.Null(version.Labels[0].Numeric);
            Assert.Equal("b", version.Labels[1].Label);
            Assert.Null(version.Labels[1].Numeric);
            Assert.Equal("c", version.Labels[2].Label);
            Assert.Null(version.Labels[2].Numeric);
            Assert.Equal("d", version.Labels[3].Label);
            Assert.Null(version.Labels[3].Numeric);
            Assert.Equal("5", version.Labels[4].Label);
            Assert.Equal((uint)5, version.Labels[4].Numeric);
            Assert.Equal("abc123", version.Metadata);
        }
    }
}
