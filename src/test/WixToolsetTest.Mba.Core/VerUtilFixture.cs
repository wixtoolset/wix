// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Mba.Core
{
    using System;
    using WixToolset.Mba.Core;
    using Xunit;

    public class VerUtilFixture
    {
        [Fact]
        public void CanCompareStringVersions()
        {
            var version1 = "1.2.3.4+abcd";
            var version2 = "1.2.3.4+zyxw";

            Assert.Equal(0, VerUtil.CompareStringVersions(version1, version2, strict: false));
        }

        [Fact]
        public void CanCopyVersion()
        {
            var version = "1.2.3.4-5.6.7.8.9.0";

            VerUtilVersion copiedVersion = null;
            try
            {
                using (var parsedVersion = VerUtil.ParseVersion(version, strict: true))
                {
                    copiedVersion = VerUtil.CopyVersion(parsedVersion);
                }

                using (var secondVersion = VerUtil.ParseVersion(version, strict: true))
                {
                    Assert.Equal(0, VerUtil.CompareParsedVersions(copiedVersion, secondVersion));
                }
            }
            finally
            {
                copiedVersion?.Dispose();
            }
        }

        [Fact]
        public void CanCreateFromQword()
        {
            var version = new Version(100, 200, 300, 400);
            var qwVersion = Engine.VersionToLong(version);

            using var parsedVersion = VerUtil.VersionFromQword(qwVersion);
            Assert.Equal("100.200.300.400", parsedVersion.Version);
            Assert.Equal(100u, parsedVersion.Major);
            Assert.Equal(200u, parsedVersion.Minor);
            Assert.Equal(300u, parsedVersion.Patch);
            Assert.Equal(400u, parsedVersion.Revision);
            Assert.Empty(parsedVersion.ReleaseLabels);
            Assert.Equal("", parsedVersion.Metadata);
            Assert.False(parsedVersion.IsInvalid);
        }

        [Fact]
        public void CanParseVersion()
        {
            var version = "1.2.3.4-a.b.c.d.5.+abc123";

            using var parsedVersion = VerUtil.ParseVersion(version, strict: false);
            Assert.Equal(version, parsedVersion.Version);
            Assert.Equal(1u, parsedVersion.Major);
            Assert.Equal(2u, parsedVersion.Minor);
            Assert.Equal(3u, parsedVersion.Patch);
            Assert.Equal(4u, parsedVersion.Revision);
            Assert.Equal(5, parsedVersion.ReleaseLabels.Length);
            Assert.Equal("+abc123", parsedVersion.Metadata);
            Assert.True(parsedVersion.IsInvalid);

            Assert.Equal("a", parsedVersion.ReleaseLabels[0].Label);
            Assert.False(parsedVersion.ReleaseLabels[0].IsNumeric);

            Assert.Equal("b", parsedVersion.ReleaseLabels[1].Label);
            Assert.False(parsedVersion.ReleaseLabels[1].IsNumeric);

            Assert.Equal("c", parsedVersion.ReleaseLabels[2].Label);
            Assert.False(parsedVersion.ReleaseLabels[2].IsNumeric);

            Assert.Equal("d", parsedVersion.ReleaseLabels[3].Label);
            Assert.False(parsedVersion.ReleaseLabels[3].IsNumeric);

            Assert.Equal("5", parsedVersion.ReleaseLabels[4].Label);
            Assert.True(parsedVersion.ReleaseLabels[4].IsNumeric);
            Assert.Equal(5u, parsedVersion.ReleaseLabels[4].Value);
        }
    }
}
