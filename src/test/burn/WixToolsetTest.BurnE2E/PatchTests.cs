// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.IO;
    using System.Xml;
    using Xunit;
    using Xunit.Abstractions;

    public class PatchTests : BurnE2ETests
    {
        public PatchTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public void CanRunDetectMultipleTimesWithPatches()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetRedetectCount(1);

            this.CanInstallBundleWithPatchThenRemoveIt();
        }

        [Fact]
        public void CanInstallBundleWithPatchThenRemoveIt()
        {
            var originalVersion = "1.0.0.0";
            var patchedVersion = "1.0.1.0";
            var testRegistryValue = "PackageA";

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var bundleA = this.CreateBundleInstaller("BundleA");
            var bundlePatchA = this.CreateBundleInstaller("BundlePatchA");

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageAv1.VerifyTestRegistryValue(testRegistryValue, originalVersion);

            bundlePatchA.Install();
            bundlePatchA.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyTestRegistryValue(testRegistryValue, patchedVersion);

            bundlePatchA.Uninstall();
            bundlePatchA.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyTestRegistryValue(testRegistryValue, originalVersion);

            bundleA.Uninstall();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv1.VerifyTestRegistryRootDeleted();
        }

        [Fact(Skip = "https://github.com/wixtoolset/issues/issues/6380")]
        public void CanPatchSwidTag()
        {
            var originalVersion = "1.0.0.0";
            var patchedVersion = "1.0.1.0";
            var packageTagName = "~PatchTests - PackageA";
            var bundleTagName = "~PatchTests - BundleA";
            var bundlePatchTagName = "~PatchTests - BundlePatchA";

            this.CreatePackageInstaller("PackageAv1");
            var bundleA = this.CreateBundleInstaller("BundleA");
            var bundlePatchA = this.CreateBundleInstaller("BundlePatchA");

            bundleA.Install();
            VerifySwidTagVersion(bundleTagName, originalVersion);
            VerifySwidTagVersion(packageTagName, originalVersion);

            bundlePatchA.Install();
            VerifySwidTagVersion(bundlePatchTagName, patchedVersion);
            VerifySwidTagVersion(packageTagName, patchedVersion);

            bundlePatchA.Uninstall();
            VerifySwidTagVersion(packageTagName, originalVersion);

            bundleA.Uninstall();
            VerifySwidTagVersion(bundleTagName, null);
            VerifySwidTagVersion(packageTagName, null);
        }

        [Fact]
        public void CanInstallBundleWithPatchesTargetingSingleProductThenRemoveIt()
        {
            var originalVersion = "1.0.0.0";
            var patchedVersion = "1.0.1.0";
            var testRegistryValue = "PackageA";
            var testRegistryValue2 = "PackageA2";

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var bundlePatchA2 = this.CreateBundleInstaller("BundlePatchA2");

            packageAv1.InstallProduct();
            packageAv1.VerifyInstalled(true);
            packageAv1.VerifyTestRegistryValue(testRegistryValue, originalVersion);
            packageAv1.VerifyTestRegistryValue(testRegistryValue2, originalVersion);

            bundlePatchA2.Install();
            bundlePatchA2.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyTestRegistryValue(testRegistryValue, patchedVersion);
            packageAv1.VerifyTestRegistryValue(testRegistryValue2, patchedVersion);

            bundlePatchA2.Uninstall();
            bundlePatchA2.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyTestRegistryValue(testRegistryValue, originalVersion);
            packageAv1.VerifyTestRegistryValue(testRegistryValue2, originalVersion);
        }

        private static void VerifySwidTagVersion(string tagName, string expectedVersion)
        {
            var regidFolder = Environment.ExpandEnvironmentVariables(@"%ProgramData%\regid.1995-08.com.example");
            var tagPath = Path.Combine(regidFolder, "regid.1995-08.com.example " + tagName + ".swidtag");
            string version = null;

            if (File.Exists(tagPath))
            {
                var doc = new XmlDocument();
                doc.Load(tagPath);

                var ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("s", "http://standards.iso.org/iso/19770/-2/2009/schema.xsd");

                var versionNode = doc.SelectSingleNode("/s:software_identification_tag/s:product_version/s:name", ns);
                version = versionNode?.InnerText ?? String.Empty;
            }

            Assert.Equal(expectedVersion, version);
        }
    }
}
