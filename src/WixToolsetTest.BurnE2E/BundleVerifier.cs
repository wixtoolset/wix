// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
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

        public string GetPackageCachePathForCacheId(string cacheId)
        {
            using var policyKey = Registry.LocalMachine.OpenSubKey(FULL_BURN_POLICY_REGISTRY_PATH);
            var redirectedCachePath = policyKey?.GetValue("PackageCache") as string;
            var cachePath = redirectedCachePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), PACKAGE_CACHE_FOLDER_NAME);
            return Path.Combine(cachePath, cacheId);
        }

        public string GetExpectedCachedBundlePath()
        {
            var bundleSymbol = this.GetBundleSymbol();
            var cachePath = this.GetPackageCachePathForCacheId(bundleSymbol.BundleId);
            return Path.Combine(cachePath, Path.GetFileName(this.Bundle));
        }

        public bool TryGetPerMachineRegistration(out BundleRegistration registration)
        {
            var bundleSymbol = this.GetBundleSymbol();
            var bundleId = bundleSymbol.BundleId;
            return BundleRegistration.TryGetPerMachineBundleRegistrationById(bundleId, out registration);
        }

        public string VerifyRegisteredAndInPackageCache()
        {
            Assert.True(this.TryGetPerMachineRegistration(out var registration));

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
            Assert.False(this.TryGetPerMachineRegistration(out _));
            Assert.False(File.Exists(cachedBundlePath));
        }

        public void RemovePackageFromCache(string packageId)
        {
            using var wixOutput = WixOutput.Read(this.BundlePdb);
            var intermediate = Intermediate.Load(wixOutput);
            var section = intermediate.Sections.Single();
            var packageSymbol = section.Symbols.OfType<WixBundlePackageSymbol>().Single(p => p.Id.Id == packageId);
            var cachePath = this.GetPackageCachePathForCacheId(packageSymbol.CacheId);
            if (Directory.Exists(cachePath))
            {
                Directory.Delete(cachePath, true);
            }
        }
    }
}
