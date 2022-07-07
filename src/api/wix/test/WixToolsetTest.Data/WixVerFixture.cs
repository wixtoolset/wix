// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Data
{
    using System;
    using System.Linq;
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
        public void CanParseEmptyStringAsInvalidVersion()
        {
            var version = WixVersion.Parse(String.Empty);
            Assert.Empty(version.Metadata);
            Assert.True(version.Invalid);
        }

        [Fact]
        public void CannotParseInvalidStringAsVersion()
        {
            Assert.False(WixVersion.TryParse("invalid", out var version));
            Assert.Null(version);
        }

        [Fact]
        public void CanParseInvalidStringAsInvalidVersion()
        {
            var version = WixVersion.Parse("invalid");
            Assert.Equal("invalid", version.Metadata);
            Assert.True(version.Invalid);
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
            Assert.False(version.Invalid);
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
            Assert.False(version.Invalid);
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
            Assert.False(version.Invalid);
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
            Assert.Equal(new[] { "19" }, version.Labels.Select(l => l.Label).ToArray());
            Assert.Equal(new uint?[] { 19 }, version.Labels.Select(l => l.Numeric).ToArray());
            Assert.Null(version.Metadata);
            Assert.False(version.Invalid);
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
            Assert.Equal(new[] { "2", "0" }, version.Labels.Select(l => l.Label).ToArray());
            Assert.Equal(new uint?[] { 2, 0 }, version.Labels.Select(l => l.Numeric).ToArray());
            Assert.Null(version.Metadata);
            Assert.False(version.Invalid);
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
            Assert.Equal(new[] { "a" }, version.Labels.Select(l => l.Label).ToArray());
            Assert.Equal(new uint?[] { null }, version.Labels.Select(l => l.Numeric).ToArray());
            Assert.Null(version.Metadata);
            Assert.False(version.Invalid);
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
            Assert.Equal(new[] { "a", "000" }, version.Labels.Select(l => l.Label).ToArray());
            Assert.Equal(new uint?[] { null, 0 }, version.Labels.Select(l => l.Numeric).ToArray());
            Assert.Null(version.Metadata);
            Assert.False(version.Invalid);
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
            Assert.False(version.Invalid);
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
        public void CanParseUnexpectedContentAsInvalidMetadata()
        {
            var version = WixVersion.Parse("1.2.3.abcd");
            Assert.Equal("abcd", version.Metadata);
            Assert.True(version.Invalid);

            version = WixVersion.Parse("1.2.3.-abcd");
            Assert.Equal("-abcd", version.Metadata);
            Assert.True(version.Invalid);
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
            Assert.False(version.Invalid);

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
            Assert.False(version.Invalid);
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
            Assert.False(version.Invalid);
        }

        [Fact]
        public void CannotParseTooLargeNumbers()
        {
            Assert.False(WixVersion.TryParse("4294967296.4294967296.4294967296.4294967296", out var version));
            Assert.Null(version);
        }

        [Fact]
        public void CanParseInvalidTooLargeNumbers()
        {
            var version = WixVersion.Parse("4294967296.4294967296.4294967296.4294967296");
            Assert.Equal(0U, version.Major);
            Assert.Equal("4294967296.4294967296.4294967296.4294967296", version.Metadata);
            Assert.True(version.Invalid);
        }

        [Fact]
        public void CanParseInvalidTooLargeNumbersWithPrefix()
        {
            var version = WixVersion.Parse("v4294967296.4294967296.4294967296.4294967296");
            Assert.Equal("v4294967296.4294967296.4294967296.4294967296", version.Metadata);
            Assert.Null(version.Prefix);
            Assert.True(version.Invalid);
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
            Assert.Equal(new[] { "a", "b", "c", "d", "5" }, version.Labels.Select(l => l.Label).ToArray());
            Assert.Equal(new uint?[] { null, null, null, null, 5 }, version.Labels.Select(l => l.Numeric).ToArray());
            Assert.Equal("abc123", version.Metadata);
            Assert.False(version.Invalid);
        }

        [Fact]
        public void CanParseVersionWithTrailingDotsAsInvalid()
        {
            var version = WixVersion.Parse(".");
            Assert.Null(version.Prefix);
            Assert.Equal(0U, version.Major);
            Assert.Equal(0U, version.Minor);
            Assert.Equal(0U, version.Patch);
            Assert.Equal(0U, version.Revision);
            Assert.Null(version.Labels);
            Assert.False(version.HasMajor);
            Assert.False(version.HasMinor);
            Assert.False(version.HasPatch);
            Assert.False(version.HasRevision);
            Assert.Equal(".", version.Metadata);
            Assert.True(version.Invalid);

            version = WixVersion.Parse("1.");
            Assert.Null(version.Prefix);
            Assert.Equal(1U, version.Major);
            Assert.Equal(0U, version.Minor);
            Assert.Equal(0U, version.Patch);
            Assert.Equal(0U, version.Revision);
            Assert.Null(version.Labels);
            Assert.True(version.HasMajor);
            Assert.False(version.HasMinor);
            Assert.False(version.HasPatch);
            Assert.False(version.HasRevision);
            Assert.Equal(String.Empty, version.Metadata);
            Assert.True(version.Invalid);

            version = WixVersion.Parse("2.1.");
            Assert.Null(version.Prefix);
            Assert.Equal(2U, version.Major);
            Assert.Equal(1U, version.Minor);
            Assert.Equal(0U, version.Patch);
            Assert.Equal(0U, version.Revision);
            Assert.Null(version.Labels);
            Assert.True(version.HasMajor);
            Assert.True(version.HasMinor);
            Assert.False(version.HasPatch);
            Assert.False(version.HasRevision);
            Assert.Equal(String.Empty, version.Metadata);
            Assert.True(version.Invalid);

            version = WixVersion.Parse("3.2.1.");
            Assert.Null(version.Prefix);
            Assert.Equal(3U, version.Major);
            Assert.Equal(2U, version.Minor);
            Assert.Equal(1U, version.Patch);
            Assert.Equal(0U, version.Revision);
            Assert.Null(version.Labels);
            Assert.True(version.HasMajor);
            Assert.True(version.HasMinor);
            Assert.True(version.HasPatch);
            Assert.False(version.HasRevision);
            Assert.Equal(String.Empty, version.Metadata);
            Assert.True(version.Invalid);

            version = WixVersion.Parse("4.3.2.1.");
            Assert.Null(version.Prefix);
            Assert.Equal(4U, version.Major);
            Assert.Equal(3U, version.Minor);
            Assert.Equal(2U, version.Patch);
            Assert.Equal(1U, version.Revision);
            Assert.Null(version.Labels);
            Assert.True(version.HasMajor);
            Assert.True(version.HasMinor);
            Assert.True(version.HasPatch);
            Assert.True(version.HasRevision);
            Assert.Equal(String.Empty, version.Metadata);
            Assert.True(version.Invalid);

            version = WixVersion.Parse("5-.");
            Assert.Null(version.Prefix);
            Assert.Equal(5U, version.Major);
            Assert.Equal(0U, version.Minor);
            Assert.Equal(0U, version.Patch);
            Assert.Equal(0U, version.Revision);
            Assert.Null(version.Labels);
            Assert.True(version.HasMajor);
            Assert.False(version.HasMinor);
            Assert.False(version.HasPatch);
            Assert.False(version.HasRevision);
            Assert.Equal(".", version.Metadata);
            Assert.True(version.Invalid);

            version = WixVersion.Parse("6-a.");
            Assert.Null(version.Prefix);
            Assert.Equal(6U, version.Major);
            Assert.Equal(0U, version.Minor);
            Assert.Equal(0U, version.Patch);
            Assert.Equal(0U, version.Revision);
            Assert.Equal(new[] { "a" }, version.Labels.Select(l => l.Label).ToArray());
            Assert.Equal(new uint?[] { null }, version.Labels.Select(l => l.Numeric).ToArray());
            Assert.True(version.HasMajor);
            Assert.False(version.HasMinor);
            Assert.False(version.HasPatch);
            Assert.False(version.HasRevision);
            Assert.Equal(String.Empty, version.Metadata);
            Assert.True(version.Invalid);
        }
    }
}
