// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.IO;
    using System.Xml;
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class PatchTests : BurnE2ETests
    {
        public PatchTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void CanRunDetectMultipleTimesWithPatches()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetRedetectCount(1);

            this.CanInstallBundleThenPatchThenRemovePatch();
        }

        [RuntimeFact]
        public void CanInstallBundleThenPatchThenRemoveBase()
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

            bundleA.Uninstall();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
            bundlePatchA.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv1.VerifyTestRegistryRootDeleted();
        }

        [RuntimeFact]
        public void CanInstallBundleThenPatchThenRemovePatch()
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

        [RuntimeFact]
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

        [RuntimeFact]
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
            var tagPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TestingSwidTags", "swidtag", tagName + ".swidtag");
            string version = null;

            if (File.Exists(tagPath))
            {
                var doc = new XmlDocument();
                doc.Load(tagPath);

                var ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("s", "http://standards.iso.org/iso/19770/-2/2015/schema.xsd");

                var versionNode = doc.SelectSingleNode("/s:SoftwareIdentity/@version", ns);
                version = versionNode?.InnerText ?? String.Empty;
            }
            else
            {
                Assert.True(expectedVersion == null, $"Did not find SWID tag with expected version {expectedVersion} at: {tagPath}");
            }

            Assert.Equal(expectedVersion, version);
        }
    }
}
