// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Versioning
{
    using System;
    using WixToolset.Versioning;
    using Xunit;

    public class VerUtilTestsFixture
    {
        [Fact]
        public void VerCompareVersionsTreatsMissingRevisionAsZero()
        {
            var version1 = WixVersion.Parse("1.2.3.4");
            var version2 = WixVersion.Parse("1.2.3");
            var version3 = WixVersion.Parse("1.2.3.0");

            Assert.Null(version1.Prefix);
            Assert.Equal(1U, version1.Major);
            Assert.Equal(2U, version1.Minor);
            Assert.Equal(3U, version1.Patch);
            Assert.Equal(4U, version1.Revision);
            Assert.Null(version1.Labels);
            Assert.Null(version1.Metadata);
            Assert.False(version1.Invalid);
            Assert.True(version1.HasMajor);
            Assert.True(version1.HasMinor);
            Assert.True(version1.HasPatch);
            Assert.True(version1.HasRevision);

            Assert.Null(version2.Prefix);
            Assert.Equal(1U, version2.Major);
            Assert.Equal(2U, version2.Minor);
            Assert.Equal(3U, version2.Patch);
            Assert.Equal(0U, version2.Revision);
            Assert.Null(version2.Labels);
            Assert.Null(version2.Metadata);
            Assert.False(version2.Invalid);
            Assert.True(version2.HasMajor);
            Assert.True(version2.HasMinor);
            Assert.True(version2.HasPatch);
            Assert.False(version2.HasRevision);

            Assert.Null(version3.Prefix);
            Assert.Equal(1U, version3.Major);
            Assert.Equal(2U, version3.Minor);
            Assert.Equal(3U, version3.Patch);
            Assert.Equal(0U, version3.Revision);
            Assert.Null(version3.Labels);
            Assert.Null(version3.Metadata);
            Assert.False(version3.Invalid);
            Assert.True(version3.HasMajor);
            Assert.True(version3.HasMinor);
            Assert.True(version3.HasPatch);
            Assert.True(version3.HasRevision);

            Assert.Equal(1, version1.CompareTo(version2));
            Assert.Equal(0, version3.CompareTo(version2));
        }

        [Fact]
        public void VerCompareVersionsTreatsNumericReleaseLabelsAsNumbers()
        {
            var version1 = WixVersion.Parse("1.0-2.0");
            var version2 = WixVersion.Parse("1.0-19");

            Assert.Null(version1.Prefix);
            Assert.Equal(1U, version1.Major);
            Assert.Equal(0U, version1.Minor);
            Assert.Equal(0U, version1.Patch);
            Assert.Equal(0U, version1.Revision);
            Assert.Equal(2, version1.Labels.Length);

            Assert.Equal(2U, version1.Labels[0].Numeric);
            Assert.Equal("2", version1.Labels[0].Label);

            Assert.Equal(0U, version1.Labels[1].Numeric);
            Assert.Equal("0", version1.Labels[1].Label);

            Assert.Null(version1.Metadata);
            Assert.False(version1.Invalid);
            Assert.True(version1.HasMajor);
            Assert.True(version1.HasMinor);
            Assert.False(version1.HasPatch);
            Assert.False(version1.HasRevision);


            Assert.Null(version2.Prefix);
            Assert.Equal(1U, version2.Major);
            Assert.Equal(0U, version2.Minor);
            Assert.Equal(0U, version2.Patch);
            Assert.Equal(0U, version2.Revision);
            Assert.Single(version2.Labels);

            Assert.Equal(19U, version2.Labels[0].Numeric);
            Assert.Equal("19", version2.Labels[0].Label);

            Assert.Null(version2.Metadata);
            Assert.False(version2.Invalid);
            Assert.True(version2.HasMajor);
            Assert.True(version2.HasMinor);
            Assert.False(version2.HasPatch);
            Assert.False(version2.HasRevision);

            TestVerutilCompareParsedVersions(version1, version2, -1);
        }

        [Fact]
        public void VerCompareVersionsHandlesNormallyInvalidVersions()
        {
            var version1 = WixVersion.Parse("10.-4.0");
            var version2 = WixVersion.Parse("10.-2.0");
            var version3 = WixVersion.Parse("0");
            var version4 = WixVersion.Parse("");
            var version5 = WixVersion.Parse("10-2");
            var version6 = WixVersion.Parse("10-4.@");


            Assert.Null(version1.Prefix);
            Assert.Equal(10U, version1.Major);
            Assert.Equal(0U, version1.Minor);
            Assert.Equal(0U, version1.Patch);
            Assert.Equal(0U, version1.Revision);
            Assert.Null(version1.Labels);
            Assert.Equal("-4.0", version1.Metadata);
            Assert.True(version1.Invalid);
            Assert.True(version1.HasMajor);
            Assert.False(version1.HasMinor);
            Assert.False(version1.HasPatch);
            Assert.False(version1.HasRevision);


            Assert.Null(version2.Prefix);
            Assert.Equal(10U, version2.Major);
            Assert.Equal(0U, version2.Minor);
            Assert.Equal(0U, version2.Patch);
            Assert.Equal(0U, version2.Revision);
            Assert.Null(version2.Labels);
            Assert.Equal("-2.0", version2.Metadata);
            Assert.True(version2.Invalid);
            Assert.True(version2.HasMajor);
            Assert.False(version2.HasMinor);
            Assert.False(version2.HasPatch);
            Assert.False(version2.HasRevision);


            Assert.Null(version3.Prefix);
            Assert.Equal(0U, version3.Major);
            Assert.Equal(0U, version3.Minor);
            Assert.Equal(0U, version3.Patch);
            Assert.Equal(0U, version3.Revision);
            Assert.Null(version3.Labels);
            Assert.Null(version3.Metadata);
            Assert.False(version3.Invalid);
            Assert.True(version3.HasMajor);
            Assert.False(version3.HasMinor);
            Assert.False(version3.HasPatch);
            Assert.False(version3.HasRevision);

            Assert.Null(version4.Prefix);
            Assert.Equal(0U, version4.Major);
            Assert.Equal(0U, version4.Minor);
            Assert.Equal(0U, version4.Patch);
            Assert.Equal(0U, version4.Revision);
            Assert.Null(version4.Labels);
            Assert.Equal(String.Empty, version4.Metadata);
            Assert.True(version4.Invalid);
            Assert.False(version4.HasMajor);
            Assert.False(version4.HasMinor);
            Assert.False(version4.HasPatch);
            Assert.False(version4.HasRevision);

            Assert.Null(version5.Prefix);
            Assert.Equal(10U, version5.Major);
            Assert.Equal(0U, version5.Minor);
            Assert.Equal(0U, version5.Patch);
            Assert.Equal(0U, version5.Revision);
            Assert.Single(version5.Labels);
            Assert.Equal(2U, version5.Labels[0].Numeric);
            Assert.Equal("2", version5.Labels[0].Label);

            Assert.Null(version5.Metadata);
            Assert.False(version5.Invalid);
            Assert.True(version5.HasMajor);
            Assert.False(version5.HasMinor);
            Assert.False(version5.HasPatch);
            Assert.False(version5.HasRevision);

            Assert.Null(version6.Prefix);
            Assert.Equal(10U, version6.Major);
            Assert.Equal(0U, version6.Minor);
            Assert.Equal(0U, version6.Patch);
            Assert.Equal(0U, version6.Revision);
            Assert.Single(version6.Labels);
            Assert.Equal(4U, version6.Labels[0].Numeric);
            Assert.Equal("4", version6.Labels[0].Label);

            Assert.Equal("@", version6.Metadata);
            Assert.True(version6.Invalid);
            Assert.True(version6.HasMajor);
            Assert.False(version6.HasMinor);
            Assert.False(version6.HasPatch);
            Assert.False(version6.HasRevision);

            TestVerutilCompareParsedVersions(version1, version2, 1);
            TestVerutilCompareParsedVersions(version3, version4, 1);
            TestVerutilCompareParsedVersions(version5, version6, -1);
        }

        [Fact]
        public void VerCompareVersionsTreatsHyphenAsVersionSeparator()
        {
            var version1 = WixVersion.Parse("0.0.1-a");
            var version2 = WixVersion.Parse("0-2");
            var version3 = WixVersion.Parse("1-2");


            Assert.Null(version1.Prefix);
            Assert.Equal(0U, version1.Major);
            Assert.Equal(0U, version1.Minor);
            Assert.Equal(1U, version1.Patch);
            Assert.Equal(0U, version1.Revision);
            Assert.Single(version1.Labels);
            Assert.Null(version1.Labels[0].Numeric);
            Assert.Equal("a", version1.Labels[0].Label);

            Assert.Null(version1.Metadata);
            Assert.False(version1.Invalid);
            Assert.True(version1.HasMajor);
            Assert.True(version1.HasMinor);
            Assert.True(version1.HasPatch);
            Assert.False(version1.HasRevision);

            Assert.Null(version2.Prefix);
            Assert.Equal(0U, version2.Major);
            Assert.Equal(0U, version2.Minor);
            Assert.Equal(0U, version2.Patch);
            Assert.Equal(0U, version2.Revision);
            Assert.Single(version2.Labels);
            Assert.Equal(2U, version2.Labels[0].Numeric);
            Assert.Equal("2", version2.Labels[0].Label);

            Assert.Null(version2.Metadata);
            Assert.False(version2.Invalid);
            Assert.True(version2.HasMajor);
            Assert.False(version2.HasMinor);
            Assert.False(version2.HasPatch);
            Assert.False(version2.HasRevision);

            Assert.Null(version3.Prefix);
            Assert.Equal(1U, version3.Major);
            Assert.Equal(0U, version3.Minor);
            Assert.Equal(0U, version3.Patch);
            Assert.Equal(0U, version3.Revision);
            Assert.Single(version3.Labels);
            Assert.Equal(2U, version3.Labels[0].Numeric);
            Assert.Equal("2", version3.Labels[0].Label);

            Assert.Null(version3.Metadata);
            Assert.False(version3.Invalid);
            Assert.True(version3.HasMajor);
            Assert.False(version3.HasMinor);
            Assert.False(version3.HasPatch);
            Assert.False(version3.HasRevision);

            TestVerutilCompareParsedVersions(version1, version2, 1);
            TestVerutilCompareParsedVersions(version1, version3, -1);
        }

        [Fact]
        public void VerCompareVersionsIgnoresLeadingZeroes()
        {
            var version1 = WixVersion.Parse("0.01-a.1");
            var version2 = WixVersion.Parse("0.1.0-a.1");
            var version3 = WixVersion.Parse("0.1-a.b.0");
            var version4 = WixVersion.Parse("0.1.0-a.b.000");

            Assert.Null(version1.Prefix);
            Assert.Equal(0U, version1.Major);
            Assert.Equal(1U, version1.Minor);
            Assert.Equal(0U, version1.Patch);
            Assert.Equal(0U, version1.Revision);
            Assert.Equal(2, version1.Labels.Length);
            Assert.Null(version1.Labels[0].Numeric);
            Assert.Equal("a", version1.Labels[0].Label);
            Assert.Equal(1U, version1.Labels[1].Numeric);
            Assert.Equal("1", version1.Labels[1].Label);

            Assert.Null(version1.Metadata);
            Assert.False(version1.Invalid);
            Assert.True(version1.HasMajor);
            Assert.True(version1.HasMinor);
            Assert.False(version1.HasPatch);
            Assert.False(version1.HasRevision);

            Assert.Null(version2.Prefix);
            Assert.Equal(0U, version2.Major);
            Assert.Equal(1U, version2.Minor);
            Assert.Equal(0U, version2.Patch);
            Assert.Equal(0U, version2.Revision);
            Assert.Equal(2, version2.Labels.Length);

            Assert.Null(version2.Labels[0].Numeric);
            Assert.Equal("a", version2.Labels[0].Label);
            Assert.Equal(1U, version2.Labels[1].Numeric);
            Assert.Equal("1", version2.Labels[1].Label);

            Assert.Null(version2.Metadata);
            Assert.False(version2.Invalid);
            Assert.True(version2.HasMajor);
            Assert.True(version2.HasMinor);
            Assert.True(version2.HasPatch);
            Assert.False(version2.HasRevision);

            Assert.Null(version3.Prefix);
            Assert.Equal(0U, version3.Major);
            Assert.Equal(1U, version3.Minor);
            Assert.Equal(0U, version3.Patch);
            Assert.Equal(0U, version3.Revision);
            Assert.Equal(3, version3.Labels.Length);
            Assert.Null(version3.Labels[0].Numeric);
            Assert.Equal("a", version3.Labels[0].Label);
            Assert.Null(version3.Labels[1].Numeric);
            Assert.Equal("b", version3.Labels[1].Label);
            Assert.Equal(0U, version3.Labels[2].Numeric);
            Assert.Equal("0", version3.Labels[2].Label);

            Assert.Null(version3.Metadata);
            Assert.False(version3.Invalid);
            Assert.True(version3.HasMajor);
            Assert.True(version3.HasMinor);
            Assert.False(version3.HasPatch);
            Assert.False(version3.HasRevision);

            Assert.Null(version4.Prefix);
            Assert.Equal(0U, version4.Major);
            Assert.Equal(1U, version4.Minor);
            Assert.Equal(0U, version4.Patch);
            Assert.Equal(0U, version4.Revision);
            Assert.Equal(3, version4.Labels.Length);
            Assert.Null(version4.Labels[0].Numeric);
            Assert.Equal("a", version4.Labels[0].Label);
            Assert.Null(version4.Labels[1].Numeric);
            Assert.Equal("b", version4.Labels[1].Label);
            Assert.Equal(0U, version4.Labels[2].Numeric);
            Assert.Equal("000", version4.Labels[2].Label);

            Assert.Null(version4.Metadata);
            Assert.False(version4.Invalid);
            Assert.True(version4.HasMajor);
            Assert.True(version4.HasMinor);
            Assert.True(version4.HasPatch);
            Assert.False(version4.HasRevision);

            TestVerutilCompareParsedVersions(version1, version2, 0);
            TestVerutilCompareParsedVersions(version3, version4, 0);
        }

        [Fact]
        public void VerCompareVersionsTreatsUnexpectedContentAsMetadata()
        {
            var version1 = WixVersion.Parse("1.2.3+abcd");
            var version2 = WixVersion.Parse("1.2.3.abcd");
            var version3 = WixVersion.Parse("1.2.3.-abcd");

            Assert.Null(version1.Prefix);
            Assert.Equal(1U, version1.Major);
            Assert.Equal(2U, version1.Minor);
            Assert.Equal(3U, version1.Patch);
            Assert.Equal(0U, version1.Revision);
            Assert.Null(version1.Labels);
            Assert.Equal("abcd", version1.Metadata);
            Assert.False(version1.Invalid);
            Assert.True(version1.HasMajor);
            Assert.True(version1.HasMinor);
            Assert.True(version1.HasPatch);
            Assert.False(version1.HasRevision);

            Assert.Null(version2.Prefix);
            Assert.Equal(1U, version2.Major);
            Assert.Equal(2U, version2.Minor);
            Assert.Equal(3U, version2.Patch);
            Assert.Equal(0U, version2.Revision);
            Assert.Null(version2.Labels);
            Assert.Equal("abcd", version2.Metadata);
            Assert.True(version2.Invalid);
            Assert.True(version2.HasMajor);
            Assert.True(version2.HasMinor);
            Assert.True(version2.HasPatch);
            Assert.False(version2.HasRevision);


            Assert.Null(version3.Prefix);
            Assert.Equal(1U, version3.Major);
            Assert.Equal(2U, version3.Minor);
            Assert.Equal(3U, version3.Patch);
            Assert.Equal(0U, version3.Revision);
            Assert.Null(version3.Labels);
            Assert.Equal("-abcd", version3.Metadata);
            Assert.True(version3.Invalid);
            Assert.True(version3.HasMajor);
            Assert.True(version3.HasMinor);
            Assert.True(version3.HasPatch);
            Assert.False(version3.HasRevision);

            TestVerutilCompareParsedVersions(version1, version2, 1);
            TestVerutilCompareParsedVersions(version1, version3, 1);
            TestVerutilCompareParsedVersions(version2, version3, 1);
        }

        [Fact]
        public void VerCompareVersionsIgnoresLeadingV()
        {
            var version1 = WixVersion.Parse("10.20.30.40");
            var version2 = WixVersion.Parse("v10.20.30.40");
            var version3 = WixVersion.Parse("V10.20.30.40");
            var version4 = WixVersion.Parse("v10.20.30.40-abc");
            var version5 = WixVersion.Parse("vvv");

            Assert.Null(version1.Prefix);
            Assert.Equal(10U, version1.Major);
            Assert.Equal(20U, version1.Minor);
            Assert.Equal(30U, version1.Patch);
            Assert.Equal(40U, version1.Revision);
            Assert.Null(version1.Labels);
            Assert.Null(version1.Metadata);
            Assert.False(version1.Invalid);
            Assert.True(version1.HasMajor);
            Assert.True(version1.HasMinor);
            Assert.True(version1.HasPatch);
            Assert.True(version1.HasRevision);

            Assert.Equal('v', version2.Prefix);
            Assert.Equal(10U, version2.Major);
            Assert.Equal(20U, version2.Minor);
            Assert.Equal(30U, version2.Patch);
            Assert.Equal(40U, version2.Revision);
            Assert.Null(version2.Labels);
            Assert.Null(version2.Metadata);
            Assert.False(version2.Invalid);
            Assert.True(version2.HasMajor);
            Assert.True(version2.HasMinor);
            Assert.True(version2.HasPatch);
            Assert.True(version2.HasRevision);

            Assert.Equal('V', version3.Prefix);
            Assert.Equal(10U, version3.Major);
            Assert.Equal(20U, version3.Minor);
            Assert.Equal(30U, version3.Patch);
            Assert.Equal(40U, version3.Revision);
            Assert.Null(version3.Labels);
            Assert.Null(version3.Metadata);
            Assert.False(version3.Invalid);
            Assert.True(version3.HasMajor);
            Assert.True(version3.HasMinor);
            Assert.True(version3.HasPatch);
            Assert.True(version3.HasRevision);

            Assert.Equal('v', version4.Prefix);
            Assert.Equal(10U, version4.Major);
            Assert.Equal(20U, version4.Minor);
            Assert.Equal(30U, version4.Patch);
            Assert.Equal(40U, version4.Revision);
            Assert.Single(version4.Labels);
            Assert.Null(version4.Labels[0].Numeric);
            Assert.Equal("abc", version4.Labels[0].Label);

            Assert.Null(version4.Metadata);
            Assert.False(version4.Invalid);
            Assert.True(version4.HasMajor);
            Assert.True(version4.HasMinor);
            Assert.True(version4.HasPatch);
            Assert.True(version4.HasRevision);

            Assert.Null(version5.Prefix);
            Assert.Equal(0U, version5.Major);
            Assert.Equal(0U, version5.Minor);
            Assert.Equal(0U, version5.Patch);
            Assert.Equal(0U, version5.Revision);
            Assert.Null(version5.Labels);
            Assert.Equal("vvv", version5.Metadata);
            Assert.True(version5.Invalid);
            Assert.False(version5.HasMajor);
            Assert.False(version5.HasMinor);
            Assert.False(version5.HasPatch);
            Assert.False(version5.HasRevision);

            TestVerutilCompareParsedVersions(version1, version2, 0);
            TestVerutilCompareParsedVersions(version1, version3, 0);
            TestVerutilCompareParsedVersions(version1, version4, 1);
        }

        [Fact]
        public void VerCompareVersionsHandlesTooLargeNumbers()
        {
            var version1 = WixVersion.Parse("4294967295.4294967295.4294967295.4294967295");
            var version2 = WixVersion.Parse("4294967296.4294967296.4294967296.4294967296");

            Assert.Null(version1.Prefix);
            Assert.Equal(4294967295, version1.Major);
            Assert.Equal(4294967295, version1.Minor);
            Assert.Equal(4294967295, version1.Patch);
            Assert.Equal(4294967295, version1.Revision);
            Assert.Null(version1.Labels);
            Assert.Null(version1.Metadata);
            Assert.False(version1.Invalid);
            Assert.True(version1.HasMajor);
            Assert.True(version1.HasMinor);
            Assert.True(version1.HasPatch);
            Assert.True(version1.HasRevision);

            Assert.Null(version2.Prefix);
            Assert.Equal(0U, version2.Major);
            Assert.Equal(0U, version2.Minor);
            Assert.Equal(0U, version2.Patch);
            Assert.Equal(0U, version2.Revision);
            Assert.Null(version2.Labels);
            Assert.Equal("4294967296.4294967296.4294967296.4294967296", version2.Metadata);
            Assert.True(version2.Invalid);
            Assert.False(version2.HasMajor);
            Assert.False(version2.HasMinor);
            Assert.False(version2.HasPatch);
            Assert.False(version2.HasRevision);

            TestVerutilCompareParsedVersions(version1, version2, 1);
        }

        [Fact]
        public void VerCompareVersionsIgnoresMetadataForValidVersions()
        {
            var version1 = WixVersion.Parse("1.2.3+abc");
            var version2 = WixVersion.Parse("1.2.3+xyz");

            Assert.Null(version1.Prefix);
            Assert.Equal(1U, version1.Major);
            Assert.Equal(2U, version1.Minor);
            Assert.Equal(3U, version1.Patch);
            Assert.Equal(0U, version1.Revision);
            Assert.Null(version1.Labels);
            Assert.Equal("abc", version1.Metadata);
            Assert.False(version1.Invalid);
            Assert.True(version1.HasMajor);
            Assert.True(version1.HasMinor);
            Assert.True(version1.HasPatch);
            Assert.False(version1.HasRevision);


            Assert.Null(version2.Prefix);
            Assert.Equal(1U, version2.Major);
            Assert.Equal(2U, version2.Minor);
            Assert.Equal(3U, version2.Patch);
            Assert.Equal(0U, version2.Revision);
            Assert.Null(version2.Labels);
            Assert.Equal("xyz", version2.Metadata);
            Assert.False(version2.Invalid);
            Assert.True(version2.HasMajor);
            Assert.True(version2.HasMinor);
            Assert.True(version2.HasPatch);
            Assert.False(version2.HasRevision);

            TestVerutilCompareParsedVersions(version1, version2, 0);
        }

        [Fact]
        public void VerParseVersionTreatsTrailingDotsAsInvalid()
        {
            var version1 = WixVersion.Parse(".");
            var version2 = WixVersion.Parse("1.");
            var version3 = WixVersion.Parse("2.1.");
            var version4 = WixVersion.Parse("3.2.1.");
            var version5 = WixVersion.Parse("4.3.2.1.");
            var version6 = WixVersion.Parse("5-.");
            var version7 = WixVersion.Parse("6-a.");

            Assert.Null(version1.Prefix);
            Assert.Equal(0U, version1.Major);
            Assert.Equal(0U, version1.Minor);
            Assert.Equal(0U, version1.Patch);
            Assert.Equal(0U, version1.Revision);
            Assert.Null(version1.Labels);
            Assert.Equal(".", version1.Metadata);
            Assert.True(version1.Invalid);
            Assert.False(version1.HasMajor);
            Assert.False(version1.HasMinor);
            Assert.False(version1.HasPatch);
            Assert.False(version1.HasRevision);


            Assert.Null(version2.Prefix);
            Assert.Equal(1U, version2.Major);
            Assert.Equal(0U, version2.Minor);
            Assert.Equal(0U, version2.Patch);
            Assert.Equal(0U, version2.Revision);
            Assert.Null(version2.Labels);
            Assert.Empty(version2.Metadata);
            Assert.True(version2.Invalid);
            Assert.True(version2.HasMajor);
            Assert.False(version2.HasMinor);
            Assert.False(version2.HasPatch);
            Assert.False(version2.HasRevision);


            Assert.Null(version3.Prefix);
            Assert.Equal(2U, version3.Major);
            Assert.Equal(1U, version3.Minor);
            Assert.Equal(0U, version3.Patch);
            Assert.Equal(0U, version3.Revision);
            Assert.Null(version3.Labels);
            Assert.Empty(version3.Metadata);
            Assert.True(version3.Invalid);
            Assert.True(version3.HasMajor);
            Assert.True(version3.HasMinor);
            Assert.False(version3.HasPatch);
            Assert.False(version3.HasRevision);

            Assert.Null(version4.Prefix);
            Assert.Equal(3U, version4.Major);
            Assert.Equal(2U, version4.Minor);
            Assert.Equal(1U, version4.Patch);
            Assert.Equal(0U, version4.Revision);
            Assert.Null(version4.Labels);
            Assert.Empty(version4.Metadata);
            Assert.True(version4.Invalid);
            Assert.True(version4.HasMajor);
            Assert.True(version4.HasMinor);
            Assert.True(version4.HasPatch);
            Assert.False(version4.HasRevision);

            Assert.Null(version5.Prefix);
            Assert.Equal(4U, version5.Major);
            Assert.Equal(3U, version5.Minor);
            Assert.Equal(2U, version5.Patch);
            Assert.Equal(1U, version5.Revision);
            Assert.Null(version5.Labels);
            Assert.Empty(version5.Metadata);
            Assert.True(version5.Invalid);
            Assert.True(version5.HasMajor);
            Assert.True(version5.HasMinor);
            Assert.True(version5.HasPatch);
            Assert.True(version5.HasRevision);

            Assert.Null(version6.Prefix);
            Assert.Equal(5U, version6.Major);
            Assert.Equal(0U, version6.Minor);
            Assert.Equal(0U, version6.Patch);
            Assert.Equal(0U, version6.Revision);
            Assert.Null(version6.Labels);
            Assert.Equal(".", version6.Metadata);
            Assert.True(version6.Invalid);
            Assert.True(version6.HasMajor);
            Assert.False(version6.HasMinor);
            Assert.False(version6.HasPatch);
            Assert.False(version6.HasRevision);

            Assert.Null(version7.Prefix);
            Assert.Equal(6U, version7.Major);
            Assert.Equal(0U, version7.Minor);
            Assert.Equal(0U, version7.Patch);
            Assert.Equal(0U, version7.Revision);
            Assert.Single(version7.Labels);
            Assert.Null(version7.Labels[0].Numeric);
            Assert.Equal("a", version7.Labels[0].Label);
            Assert.Empty(version7.Metadata);
            Assert.True(version7.Invalid);
            Assert.True(version7.HasMajor);
            Assert.False(version7.HasMinor);
            Assert.False(version7.HasPatch);
            Assert.False(version7.HasRevision);
        }

        private static void TestVerutilCompareParsedVersions(WixVersion version1, WixVersion version2, int expectedResult)
        {
            var result = version1.CompareTo(version2);
            Assert.Equal(expectedResult, result);

            result = version2.CompareTo(version1);
            Assert.Equal(expectedResult, -result);

            var equal = version1.Equals(version2);
            Assert.Equal(expectedResult == 0, equal);
        }
    }
}
