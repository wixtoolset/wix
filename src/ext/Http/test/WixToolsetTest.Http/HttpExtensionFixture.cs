// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Http
{
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Http;
    using Xunit;

    public class HttpExtensionFixture
    {
        [Fact]
        public void CanBuildUsingSsl()
        {
            var folder = TestData.Get("TestData", "Ssl");
            var build = new Builder(folder, typeof(HttpExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "CustomAction", "Wix6HttpCertificate");
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:Wix6ExecHttpCertificatesInstall_X86\t3073\tWix6HttpCA_X86\tExecHttpCertificates\t",
                "CustomAction:Wix6ExecHttpCertificatesUninstall_X86\t3073\tWix6HttpCA_X86\tExecHttpCertificates\t",
                "CustomAction:Wix6RollbackHttpCertificatesInstall_X86\t3329\tWix6HttpCA_X86\tExecHttpCertificates\t",
                "CustomAction:Wix6RollbackHttpCertificatesUninstall_X86\t3329\tWix6HttpCA_X86\tExecHttpCertificates\t",
                "CustomAction:Wix6SchedHttpCertificatesInstall_X86\t1\tWix6HttpCA_X86\tSchedHttpCertificatesInstall\t",
                "CustomAction:Wix6SchedHttpCertificatesUninstall_X86\t1\tWix6HttpCA_X86\tSchedHttpCertificatesUninstall\t",
                "Wix6HttpCertificate:ipsFO5EwsJKZPxl2W2V1nI59m1pDQs\t\t[PORTMANTEAU]\t[SOME_OTHER_THUMBPRINT]\t\t\t0\t1\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
                "Wix6HttpCertificate:sniC9YX6_H7UL_WGBx4DoDGI.Sj.D0\texample.com\t8080\t[SOME_THUMBPRINT]\t\t\t2\t0\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
            }, results);
        }

        [Fact]
        public void CanBuildUsingUrlReservation()
        {
            var folder = TestData.Get(@"TestData\UsingUrlReservation");
            var build = new Builder(folder, typeof(HttpExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "CustomAction", "Wix4HttpUrlAce", "Wix4HttpUrlReservation");
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:Wix4ExecHttpUrlReservationsInstall_X86\t3073\tWix6HttpCA_X86\tExecHttpUrlReservations\t",
                "CustomAction:Wix4ExecHttpUrlReservationsUninstall_X86\t3073\tWix6HttpCA_X86\tExecHttpUrlReservations\t",
                "CustomAction:Wix4RollbackHttpUrlReservationsInstall_X86\t3329\tWix6HttpCA_X86\tExecHttpUrlReservations\t",
                "CustomAction:Wix4RollbackHttpUrlReservationsUninstall_X86\t3329\tWix6HttpCA_X86\tExecHttpUrlReservations\t",
                "CustomAction:Wix4SchedHttpUrlReservationsInstall_X86\t1\tWix6HttpCA_X86\tSchedHttpUrlReservationsInstall\t",
                "CustomAction:Wix4SchedHttpUrlReservationsUninstall_X86\t1\tWix6HttpCA_X86\tSchedHttpUrlReservationsUninstall\t",
                "Wix4HttpUrlAce:aceu5os2gQoblRmzwjt85LQf997uD4\turlO23FkY2xzEY54lY6E6sXFW6glXc\tNT SERVICE\\TestService\t268435456",
                "Wix4HttpUrlReservation:urlO23FkY2xzEY54lY6E6sXFW6glXc\t0\t\thttp://+:80/vroot/\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
            }, results);
        }

        private static void Build(string[] args)
        {
            /*var result =*/ WixRunner.Execute(args).AssertSuccess();
        }
    }
}
