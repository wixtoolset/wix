// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreNative
{
    using System.Linq;
    using WixInternal.TestSupport;
    using WixToolset.Core.Native;
    using Xunit;

    public class CertificateHashesFixture
    {
        [Fact]
        public void CanGetHashesFromSignedFile()
        {
            var cabFile = TestData.Get(@"TestData\test.cab");

            var hashes = CertificateHashes.Read(new[] { cabFile });

            var hash = hashes.Single();
            WixInternal.TestSupport.WixAssert.StringEqual(cabFile, hash.Path);
            WixInternal.TestSupport.WixAssert.StringEqual("7EC90B3FC3D580EB571210011F1095E149DCC6BB", hash.PublicKey);
            WixInternal.TestSupport.WixAssert.StringEqual("0B13494DB50BC185A34389BBBAA01EDD1CF56350", hash.Thumbprint);
            Assert.Null(hash.Exception);
        }

        [Fact]
        public void CannotGetHashesFromUnsignedFile()
        {
            var txtFile = TestData.Get(@"TestData\test.txt");

            var hashes = CertificateHashes.Read(new[] { txtFile });

            var hash = hashes.Single();
            WixInternal.TestSupport.WixAssert.StringEqual(txtFile, hash.Path);
            Assert.Null(hash.Exception);
        }

        [Fact]
        public void CanGetMultipleHashes()
        {
            var cabFile = TestData.Get(@"TestData\test.cab");
            var txtFile = TestData.Get(@"TestData\test.txt");

            var hashes = CertificateHashes.Read(new[] { cabFile, txtFile });

            WixInternal.TestSupport.WixAssert.StringEqual(cabFile, hashes[0].Path);
            WixInternal.TestSupport.WixAssert.StringEqual("7EC90B3FC3D580EB571210011F1095E149DCC6BB", hashes[0].PublicKey);
            WixInternal.TestSupport.WixAssert.StringEqual("0B13494DB50BC185A34389BBBAA01EDD1CF56350", hashes[0].Thumbprint);
            Assert.Null(hashes[0].Exception);

            Assert.Equal(txtFile, hashes[1].Path);
            Assert.Empty(hashes[1].PublicKey);
            Assert.Empty(hashes[1].Thumbprint);
            Assert.Null(hashes[1].Exception);
        }
    }
}
