// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Win32;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Xunit;

    public partial class BundleInstaller
    {
        public const string FULL_BURN_POLICY_REGISTRY_PATH = "SOFTWARE\\WOW6432Node\\Policies\\WiX\\Burn";
        public const string PACKAGE_CACHE_FOLDER_NAME = "Package Cache";

        public string BundlePdb { get; }

        private WixBundleSymbol BundleSymbol { get; set; }

        private WixBundleSymbol GetBundleSymbol()
        {
            if (this.BundleSymbol == null)
            {
                using var wixOutput = WixOutput.Read(this.BundlePdb);
                var intermediate = Intermediate.Load(wixOutput);
                var section = intermediate.Sections.Single();
                this.BundleSymbol = section.Symbols.OfType<WixBundleSymbol>().Single();
            }

            return this.BundleSymbol;
        }

        public string GetPackageCachePathForCacheId(string cacheId, bool perMachine)
        {
            string cachePath;
            if (perMachine)
            {
                using var policyKey = Registry.LocalMachine.OpenSubKey(FULL_BURN_POLICY_REGISTRY_PATH);
                var redirectedCachePath = policyKey?.GetValue("PackageCache") as string;
                cachePath = redirectedCachePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), PACKAGE_CACHE_FOLDER_NAME);
            }
            else
            {
                cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), PACKAGE_CACHE_FOLDER_NAME);
            }
            return Path.Combine(cachePath, cacheId);
        }

        public string GetExpectedCachedBundlePath()
        {
            var bundleSymbol = this.GetBundleSymbol();
            var cachePath = this.GetPackageCachePathForCacheId(bundleSymbol.BundleId, bundleSymbol.PerMachine);
            return Path.Combine(cachePath, Path.GetFileName(this.Bundle));
        }

        public string ManuallyCache()
        {
            var expectedCachePath = this.GetExpectedCachedBundlePath();
            Directory.CreateDirectory(Path.GetDirectoryName(expectedCachePath));
            File.Copy(this.Bundle, expectedCachePath);
            return expectedCachePath;
        }

        public void ManuallyUncache()
        {
            var expectedCachePath = this.GetExpectedCachedBundlePath();
            File.Delete(expectedCachePath);
        }

        public bool TryGetRegistration(out BundleRegistration registration)
        {
            var bundleSymbol = this.GetBundleSymbol();
            var x64 = bundleSymbol.Platform != Platform.X86;
            var bundleId = bundleSymbol.BundleId;
            if (bundleSymbol.PerMachine)
            {
                return BundleRegistration.TryGetPerMachineBundleRegistrationById(bundleId, x64, out registration);
            }
            else
            {
                return BundleRegistration.TryGetPerUserBundleRegistrationById(bundleId, out registration);
            }
        }

        public string VerifyRegisteredAndInPackageCache()
        {
            Assert.True(this.TryGetRegistration(out var registration));

            Assert.NotNull(registration.CachePath);
            Assert.True(File.Exists(registration.CachePath));

            var expectedCachePath = this.GetExpectedCachedBundlePath();
            Assert.Equal(expectedCachePath, registration.CachePath, StringComparer.OrdinalIgnoreCase);

            return registration.CachePath;
        }

        public void VerifyUnregisteredAndRemovedFromPackageCache()
        {
            var cachedBundlePath = this.GetExpectedCachedBundlePath();
            this.VerifyUnregisteredAndRemovedFromPackageCache(cachedBundlePath);
        }

        public void VerifyUnregisteredAndRemovedFromPackageCache(string cachedBundlePath)
        {
            Assert.False(this.TryGetRegistration(out _));
            Assert.False(File.Exists(cachedBundlePath));
        }

        public void RemovePackageFromCache(string packageId)
        {
            using var wixOutput = WixOutput.Read(this.BundlePdb);
            var intermediate = Intermediate.Load(wixOutput);
            var section = intermediate.Sections.Single();
            var packageSymbol = section.Symbols.OfType<WixBundlePackageSymbol>().Single(p => p.Id.Id == packageId);
            var cachePath = this.GetPackageCachePathForCacheId(packageSymbol.CacheId, packageSymbol.PerMachine == YesNoDefaultType.Yes);
            if (Directory.Exists(cachePath))
            {
                Directory.Delete(cachePath, true);
            }
        }

        public void VerifyPackageIsCached(string packageId)
        {
            using var wixOutput = WixOutput.Read(this.BundlePdb);
            var intermediate = Intermediate.Load(wixOutput);
            var section = intermediate.Sections.Single();
            var packageSymbol = section.Symbols.OfType<WixBundlePackageSymbol>().Single(p => p.Id.Id == packageId);
            var cachePath = this.GetPackageCachePathForCacheId(packageSymbol.CacheId, packageSymbol.PerMachine == YesNoDefaultType.Yes);
            Assert.True(Directory.Exists(cachePath));
        }

        public void VerifyExeTestRegistryRootDeleted(string name, bool x64 = false)
        {
            using var testRegistryRoot = this.TestContext.GetTestRegistryRoot(x64, name);
            if (testRegistryRoot != null)
            {
                var actualValue = testRegistryRoot.GetValue("Version") as string;
                Assert.Null(actualValue);
            }
        }

        public void VerifyExeTestRegistryValue(string name, string expectedValue, bool x64 = false)
        {
            using (var root = this.TestContext.GetTestRegistryRoot(x64, name))
            {
                Assert.NotNull(root);
                var actualValue = root.GetValue("Version") as string;
                Assert.Equal(expectedValue, actualValue);
            }
        }
    }
}
