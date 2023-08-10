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
        public void CanBuildUsingSniSsl()
        {
            var folder = TestData.Get("TestData", "SniSsl");
            var build = new Builder(folder, typeof(HttpExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "CustomAction", "Wix4HttpSniSslCert");
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:Wix4ExecHttpSniSslCertsInstall_X86\t3073\tWix4HttpCA_X86\tExecHttpSniSslCerts\t",
                "CustomAction:Wix4ExecHttpSniSslCertsUninstall_X86\t3073\tWix4HttpCA_X86\tExecHttpSniSslCerts\t",
                "CustomAction:Wix4RollbackHttpSniSslCertsInstall_X86\t3329\tWix4HttpCA_X86\tExecHttpSniSslCerts\t",
                "CustomAction:Wix4RollbackHttpSniSslCertsUninstall_X86\t3329\tWix4HttpCA_X86\tExecHttpSniSslCerts\t",
                "CustomAction:Wix4SchedHttpSniSslCertsInstall_X86\t1\tWix4HttpCA_X86\tSchedHttpSniSslCertsInstall\t",
                "CustomAction:Wix4SchedHttpSniSslCertsUninstall_X86\t1\tWix4HttpCA_X86\tSchedHttpSniSslCertsUninstall\t",
                "Wix4HttpSniSslCert:sslC9YX6_H7UL_WGBx4DoDGI.Sj.D0\texample.com\t8080\t[SOME_THUMBPRINT]\t\t\t2\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
            }, results);
        }

        [Fact]
        public void CanBuildUsingSslBinding()
        {
            var folder = TestData.Get("TestData", "Ssl");
            var build = new Builder(folder, typeof(HttpExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "CustomAction", "Wix4HttpSslCertificate", "Wix4HttpSslBinding", "Wix4HttpSslBindingCertificates");
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:Wix4AddMachineHttpCertificate_X86\t11265\tWix4HttpCA_X86\tAddMachineHttpCertificate\t",
                "CustomAction:Wix4AddUserHttpCertificate_X86\t25601\tWix4HttpCA_X86\tAddUserHttpCertificate\t",
                "CustomAction:Wix4DeleteMachineHttpCertificate_X86\t11265\tWix4HttpCA_X86\tDeleteMachineHttpCertificate\t",
                "CustomAction:Wix4DeleteUserHttpCertificate_X86\t25601\tWix4HttpCA_X86\tDeleteUserHttpCertificate\t",
                "CustomAction:Wix4ExecHttpSslBindingsInstall_X86\t11265\tWix4HttpCA_X86\tExecHttpSslBindings\t",
                "CustomAction:Wix4ExecHttpSslBindingsUninstall_X86\t11265\tWix4HttpCA_X86\tExecHttpSslBindings\t",
                "CustomAction:Wix4InstallHttpCertificates_X86\t1\tWix4HttpCA_X86\tInstallHttpCertificates\t",
                "CustomAction:Wix4RollbackAddMachineHttpCertificate_X86\t11521\tWix4HttpCA_X86\tDeleteMachineHttpCertificate\t",
                "CustomAction:Wix4RollbackAddUserHttpCertificate_X86\t25857\tWix4HttpCA_X86\tDeleteUserHttpCertificate\t",
                "CustomAction:Wix4RollbackDeleteMachineHttpCertificate_X86\t11521\tWix4HttpCA_X86\tAddMachineHttpCertificate\t",
                "CustomAction:Wix4RollbackDeleteUserHttpCertificate_X86\t25857\tWix4HttpCA_X86\tAddUserHttpCertificate\t",
                "CustomAction:Wix4RollbackHttpSslBindingsInstall_X86\t11521\tWix4HttpCA_X86\tExecHttpSslBindings\t",
                "CustomAction:Wix4RollbackHttpSslBindingsUninstall_X86\t11521\tWix4HttpCA_X86\tExecHttpSslBindings\t",
                "CustomAction:Wix4SchedHttpSslBindingsInstall_X86\t8193\tWix4HttpCA_X86\tSchedHttpSslBindingsInstall\t",
                "CustomAction:Wix4SchedHttpSslBindingsUninstall_X86\t8193\tWix4HttpCA_X86\tSchedHttpSslBindingsUninstall\t",
                //"CustomAction:Wix4RollbackDeleteMachineHttpCertificate_X86\t1\tWix4HttpCA_X86\tAddMachineHttpCertificate\t",
                "CustomAction:Wix4UninstallHttpCertificates_X86\t1\tWix4HttpCA_X86\tUninstallHttpCertificates\t",
                "Wix4HttpSslBinding:ssltjEpdUFkxO7rNF2TrXuGLJg5NwE\t0.0.0.0\t8081\t\t\t\t2\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
                "Wix4HttpSslBindingCertificates:ssltjEpdUFkxO7rNF2TrXuGLJg5NwE\tSomeCertificate",
                "Wix4HttpSslCertificate:SomeCertificate\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tSome Certificate\t2\tMY\t8\t\t[SOME_PATH]\t[PFX_PASS]",
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
                "CustomAction:Wix4ExecHttpUrlReservationsInstall_X86\t3073\tWix4HttpCA_X86\tExecHttpUrlReservations\t",
                "CustomAction:Wix4ExecHttpUrlReservationsUninstall_X86\t3073\tWix4HttpCA_X86\tExecHttpUrlReservations\t",
                "CustomAction:Wix4RollbackHttpUrlReservationsInstall_X86\t3329\tWix4HttpCA_X86\tExecHttpUrlReservations\t",
                "CustomAction:Wix4RollbackHttpUrlReservationsUninstall_X86\t3329\tWix4HttpCA_X86\tExecHttpUrlReservations\t",
                "CustomAction:Wix4SchedHttpUrlReservationsInstall_X86\t1\tWix4HttpCA_X86\tSchedHttpUrlReservationsInstall\t",
                "CustomAction:Wix4SchedHttpUrlReservationsUninstall_X86\t1\tWix4HttpCA_X86\tSchedHttpUrlReservationsUninstall\t",
                "Wix4HttpUrlAce:aceu5os2gQoblRmzwjt85LQf997uD4\turlO23FkY2xzEY54lY6E6sXFW6glXc\tNT SERVICE\\TestService\t268435456",
                "Wix4HttpUrlReservation:urlO23FkY2xzEY54lY6E6sXFW6glXc\t0\t\thttp://+:80/vroot/\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
            }, results);
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
