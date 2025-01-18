// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Iis
{
    using System.Linq;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Iis;
    using Xunit;

    public class IisExtensionFixture
    {
        [Fact]
        public void CanBuildUsingIIs()
        {
            var folder = TestData.Get(@"TestData\UsingIis");
            var build = new Builder(folder, typeof(IisExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, validate: true, "Wix4Certificate", "Wix4CertificateHash", "Wix4IIsWebSite", "Wix4IIsWebAddress");
            WixAssert.CompareLineByLine(new[]
            {
                "Wix4Certificate:Certificate.MyCert\tMyCert\tMyCert certificate\t2\tTrustedPublisher\t14\tMyCertBits\t\t",
                "Wix4IIsWebAddress:TestAddress\tTest\t\t[PORT]\t\t0",
                "Wix4IIsWebSite:Test\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest web server\t\tTestWebSiteProductDirectory\t2\t2\tTestAddress\tReadAndExecute\t\t\t\t",
            }, results);
        }

        [Fact]
        public void CanBuildWebDirProperties()
        {
            var folder = TestData.Get(@"TestData\WebDirProperties");
            var build = new Builder(folder, typeof(IisExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, validate: true, "Wix4IIsWebSite", "Wix4IIsWebDir", "Wix4IIsWebDirProperties");
            WixAssert.CompareLineByLine(new[]
            {
                "Wix4IIsWebDir:TestDirAccessSSL\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestAccessSSL\tTestAccessSSL\t",
                "Wix4IIsWebDir:TestDirAccessSSL128\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestAccessSSL128\tTestAccessSSL128\t",
                "Wix4IIsWebDir:TestDirAccessSSLMapCert\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestAccessSSLMapCert\tTestAccessSSLMapCert\t",
                "Wix4IIsWebDir:TestDirAccessSSLNegotiateCert\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestAccessSSLNegotiateCert\tTestAccessSSLNegotiateCert\t",
                "Wix4IIsWebDir:TestDirAccessSSLRequireCert\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestAccessSSLRequireCert\tTestAccessSSLRequireCert\t",
                "Wix4IIsWebDir:TestDirAnonymousAccess\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestAnonymousAccess\tTestAnonymousAccess\t",
                "Wix4IIsWebDir:TestDirAspDetailedError\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestAspDetailedError\tTestAspDetailedError\t",
                "Wix4IIsWebDir:TestDirAuthenticationProviders\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestAuthenticationProviders\tTestAuthenticationProviders\t",
                "Wix4IIsWebDir:TestDirBasicAuthentication\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestBasicAuthentication\tTestBasicAuthentication\t",
                "Wix4IIsWebDir:TestDirCacheControlCustom\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestCacheControlCustom\tTestCacheControlCustom\t",
                "Wix4IIsWebDir:TestDirCacheControlMaxAge\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestCacheControlMaxAge\tTestCacheControlMaxAge\t",
                //"Wix4IIsWebDir:TestDirCacheControlMaxAgeNull\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestCacheControlMaxAgeNull\tTestCacheControlMaxAgeNull\t",
                "Wix4IIsWebDir:TestDirClearCustomError\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestClearCustomError\tTestClearCustomError\t",
                "Wix4IIsWebDir:TestDirDefaultDocuments\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestDefaultDocuments\tTestDefaultDocuments\t",
                "Wix4IIsWebDir:TestDirDigestAuthentication\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestDigestAuthentication\tTestDigestAuthentication\t",
                "Wix4IIsWebDir:TestDirDirBrowseShowDate\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestDirBrowseShowDate\tTestDirBrowseShowDate\t",
                "Wix4IIsWebDir:TestDirDirBrowseShowExtension\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestDirBrowseShowExtension\tTestDirBrowseShowExtension\t",
                "Wix4IIsWebDir:TestDirDirBrowseShowLongDate\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestDirBrowseShowLongDate\tTestDirBrowseShowLongDate\t",
                "Wix4IIsWebDir:TestDirDirBrowseShowSize\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestDirBrowseShowSize\tTestDirBrowseShowSize\t",
                "Wix4IIsWebDir:TestDirDirBrowseShowTime\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestDirBrowseShowTime\tTestDirBrowseShowTime\t",
                "Wix4IIsWebDir:TestDirEnableDefaultDoc\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestEnableDefaultDoc\tTestEnableDefaultDoc\t",
                "Wix4IIsWebDir:TestDirEnableDirBrowsing\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestEnableDirBrowsing\tTestEnableDirBrowsing\t",
                "Wix4IIsWebDir:TestDirExecute\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestExecute\tTestExecute\t",
                "Wix4IIsWebDir:TestDirHttpExpires\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestHttpExpires\tTestHttpExpires\t",
                "Wix4IIsWebDir:TestDirIIsControlledPassword\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestIIsControlledPassword\tTestIIsControlledPassword\t",
                "Wix4IIsWebDir:TestDirIndex\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestIndex\tTestIndex\t",
                "Wix4IIsWebDir:TestDirLogVisits\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestLogVisits\tTestLogVisits\t",
                "Wix4IIsWebDir:TestDirPassportAuthentication\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestPassportAuthentication\tTestPassportAuthentication\t",
                "Wix4IIsWebDir:TestDirRead\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestRead\tTestRead\t",
                "Wix4IIsWebDir:TestDirScript\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestScript\tTestScript\t",
                "Wix4IIsWebDir:TestDirWindowsAuthentication\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestWindowsAuthentication\tTestWindowsAuthentication\t",
                "Wix4IIsWebDir:TestDirWrite\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest\tTestWrite\tTestWrite\t",
                "Wix4IIsWebDirProperties:TestAccessSSL\t\t\t\t0\t\t\t\t\t\t\t\t\t8\t\t",
                "Wix4IIsWebDirProperties:TestAccessSSL128\t\t\t\t0\t\t\t\t\t\t\t\t\t256\t\t",
                "Wix4IIsWebDirProperties:TestAccessSSLMapCert\t\t\t\t0\t\t\t\t\t\t\t\t\t128\t\t",
                "Wix4IIsWebDirProperties:TestAccessSSLNegotiateCert\t\t\t\t0\t\t\t\t\t\t\t\t\t32\t\t",
                "Wix4IIsWebDirProperties:TestAccessSSLRequireCert\t\t\t\t0\t\t\t\t\t\t\t\t\t64\t\t",
                "Wix4IIsWebDirProperties:TestAnonymousAccess\t\t1\t\t0\t\t\t\t\t\t\t\t\t\t\t",
                "Wix4IIsWebDirProperties:TestAspDetailedError\t\t\t\t0\t\t\t\t1\t\t\t\t\t\t\t",
                "Wix4IIsWebDirProperties:TestAuthenticationProviders\t\t\t\t0\t\t\t\t\t\t\t\t\t\tNTLM\t",
                "Wix4IIsWebDirProperties:TestBasicAuthentication\t\t2\t\t0\t\t\t\t\t\t\t\t\t\t\t",
                "Wix4IIsWebDirProperties:TestCacheControlCustom\t\t\t\t0\t\t\t\t\t\t\tCacheControl\t\t\t\t",
                "Wix4IIsWebDirProperties:TestCacheControlMaxAge\t\t\t\t0\t\t\t\t\t\t-1\t\t\t\t\t",
                //"Wix4IIsWebDirProperties:TestCacheControlMaxAgeNull\t\t\t\t0\t\t\t\t\t\t4294967295\t\t\t\t\t",
                "Wix4IIsWebDirProperties:TestClearCustomError\t\t\t\t0\t\t\t\t\t\t\t\t1\t\t\t",
                "Wix4IIsWebDirProperties:TestDefaultDocuments\t\t\t\t0\t\t\tDefaultDocument.html,index.html,index.htm\t\t\t\t\t\t\t\t",
                "Wix4IIsWebDirProperties:TestDigestAuthentication\t\t16\t\t0\t\t\t\t\t\t\t\t\t\t\t",
                "Wix4IIsWebDirProperties:TestDirBrowseShowDate\t\t\t\t0\t\t\t\t\t\t\t\t\t\t\t1",
                "Wix4IIsWebDirProperties:TestDirBrowseShowExtension\t\t\t\t0\t\t\t\t\t\t\t\t\t\t\t2",
                "Wix4IIsWebDirProperties:TestDirBrowseShowLongDate\t\t\t\t0\t\t\t\t\t\t\t\t\t\t\t4",
                "Wix4IIsWebDirProperties:TestDirBrowseShowSize\t\t\t\t0\t\t\t\t\t\t\t\t\t\t\t8",
                "Wix4IIsWebDirProperties:TestDirBrowseShowTime\t\t\t\t0\t\t\t\t\t\t\t\t\t\t\t16",
                "Wix4IIsWebDirProperties:TestEnableDefaultDoc\t\t\t\t0\t\t\t\t\t\t\t\t\t\t\t32",
                "Wix4IIsWebDirProperties:TestEnableDirBrowsing\t\t\t\t0\t\t\t\t\t\t\t\t\t\t\t64",
                "Wix4IIsWebDirProperties:TestExecute\t4\t\t\t0\t\t\t\t\t\t\t\t\t\t\t",
                "Wix4IIsWebDirProperties:TestHttpExpires\t\t\t\t0\t\t\t\t\tyes\t\t\t\t\t\t",
                "Wix4IIsWebDirProperties:TestIIsControlledPassword\t\t\t\t1\t\t\t\t\t\t\t\t\t\t\t",
                "Wix4IIsWebDirProperties:TestIndex\t\t\t\t0\t\t1\t\t\t\t\t\t\t\t\t",
                "Wix4IIsWebDirProperties:TestLogVisits\t\t\t\t0\t1\t\t\t\t\t\t\t\t\t\t",
                "Wix4IIsWebDirProperties:TestPassportAuthentication\t\t64\t\t0\t\t\t\t\t\t\t\t\t\t\t",
                "Wix4IIsWebDirProperties:TestRead\t1\t\t\t0\t\t\t\t\t\t\t\t\t\t\t",
                "Wix4IIsWebDirProperties:TestScript\t512\t\t\t0\t\t\t\t\t\t\t\t\t\t\t",
                "Wix4IIsWebDirProperties:TestWindowsAuthentication\t\t4\t\t0\t\t\t\t\t\t\t\t\t\t\t",
                "Wix4IIsWebDirProperties:TestWrite\t2\t\t\t0\t\t\t\t\t\t\t\t\t\t\t",
                "Wix4IIsWebSite:Test\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest web server\t\tTestWebSiteProductDirectory\t2\t2\tTestAddress\t\t\t\t\t",
            }, results);
        }

        private static void Build(string[] args)
        {
            var newArgs = args.ToList();

            if (args.First() == "build")
            {
                newArgs.AddRange(new[] { "-arch", "x64" });
            }

            WixRunner.Execute(newArgs.ToArray()).AssertSuccess();
        }
    }
}
