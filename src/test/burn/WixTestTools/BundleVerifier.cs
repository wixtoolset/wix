// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using Microsoft.Win32;
    using WixInternal.TestSupport;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Xunit;

    public partial class BundleInstaller
    {
        public const string DependencyRegistryRoot = "Software\\Classes\\Installer\\Dependencies";
        public const string FULL_BURN_POLICY_REGISTRY_PATH = "SOFTWARE\\Policies\\WiX\\Burn";
        public const string FULL_BURN_POLICY_REGISTRY_PATH_WOW6432NODE = "SOFTWARE\\WOW6432Node\\Policies\\WiX\\Burn";
        public const string PACKAGE_CACHE_FOLDER_NAME = "Package Cache";

        public string BundlePdb { get; }

        private WixBundleSymbol BundleSymbol { get; set; }

        private WixUpdateRegistrationSymbol UpdateRegistrationSymbol { get; set; }

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

        private WixUpdateRegistrationSymbol GetUpdateRegistrationSymbol()
        {
            if (this.UpdateRegistrationSymbol == null)
            {
                using var wixOutput = WixOutput.Read(this.BundlePdb);
                var intermediate = Intermediate.Load(wixOutput);
                var section = intermediate.Sections.Single();
                this.UpdateRegistrationSymbol = section.Symbols.OfType<WixUpdateRegistrationSymbol>().Single();
            }

            return this.UpdateRegistrationSymbol;
        }

        public string GetFullBurnPolicyRegistryPath()
        {
            var bundleSymbol = this.GetBundleSymbol();
            var x64 = bundleSymbol.Platform != Platform.X86;
            return x64 ? FULL_BURN_POLICY_REGISTRY_PATH : FULL_BURN_POLICY_REGISTRY_PATH_WOW6432NODE;
        }

        public string GetPackageCachePathForCacheId(string cacheId, bool perMachine)
        {
            string cachePath;
            if (perMachine)
            {
                using var policyKey = Registry.LocalMachine.OpenSubKey(this.GetFullBurnPolicyRegistryPath());
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
            var cachePath = this.GetPackageCachePathForCacheId(bundleSymbol.BundleCode, bundleSymbol.PerMachine);
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

        public bool TryGetArpEntryExePackageConfiguration(string packageId, out string arpId, out string arpVersion, out bool arpWin64, out bool perMachine)
        {
            using var wixOutput = WixOutput.Read(this.BundlePdb);
            var intermediate = Intermediate.Load(wixOutput);
            var section = intermediate.Sections.Single();
            var packageSymbol = section.Symbols.OfType<WixBundlePackageSymbol>().SingleOrDefault(p => p.Id.Id == packageId);
            var exePackageSymbol = section.Symbols.OfType<WixBundleExePackageSymbol>().SingleOrDefault(p => p.Id.Id == packageId);
            if (packageSymbol == null || exePackageSymbol == null || exePackageSymbol.DetectionType != WixBundleExePackageDetectionType.Arp)
            {
                arpId = null;
                arpVersion = null;
                arpWin64 = false;
                perMachine = false;
                return false;
            }

            arpId = exePackageSymbol.ArpId;
            arpVersion = exePackageSymbol.ArpDisplayVersion;
            arpWin64 = exePackageSymbol.ArpWin64;
            perMachine = packageSymbol.PerMachine == true;
            return true;
        }

        public bool TryGetRegistration(out BundleRegistration registration)
        {
            var bundleSymbol = this.GetBundleSymbol();
            var x64 = bundleSymbol.Platform != Platform.X86;
            var bundleCode = bundleSymbol.BundleCode;
            if (bundleSymbol.PerMachine)
            {
                return BundleRegistration.TryGetPerMachineBundleRegistrationById(bundleCode, x64, out registration);
            }
            else
            {
                return BundleRegistration.TryGetPerUserBundleRegistrationById(bundleCode, out registration);
            }
        }

        public bool TryGetUpdateRegistration(out BundleUpdateRegistration registration)
        {
            var bundleSymbol = this.GetBundleSymbol();
            var x64 = bundleSymbol.Platform != Platform.X86;

            var updateRegistrationSymbol = this.GetUpdateRegistrationSymbol();
            var manufacturer = updateRegistrationSymbol.Manufacturer;
            var productFamily = updateRegistrationSymbol.ProductFamily;
            var name = updateRegistrationSymbol.Name;


            if (bundleSymbol.PerMachine)
            {
                return BundleUpdateRegistration.TryGetPerMachineBundleUpdateRegistration(manufacturer, productFamily, name, x64, out registration);
            }
            else
            {
                return BundleUpdateRegistration.TryGetPerUserBundleUpdateRegistration(manufacturer, productFamily, name, out registration);
            }
        }

        public BundleRegistration VerifyRegisteredAndInPackageCache(int? expectedSystemComponent = null)
        {
            Assert.True(this.TryGetRegistration(out var registration));

            Assert.Equal(expectedSystemComponent, registration.SystemComponent);

            Assert.NotNull(registration.CachePath);
            Assert.True(File.Exists(registration.CachePath));

            var expectedCachePath = this.GetExpectedCachedBundlePath();
            WixAssert.StringEqual(expectedCachePath, registration.CachePath, true);

            return registration;
        }

        public void VerifyUnregisteredAndRemovedFromPackageCache()
        {
            var cachedBundlePath = this.GetExpectedCachedBundlePath();
            this.VerifyUnregisteredAndRemovedFromPackageCache(cachedBundlePath);
        }

        public void VerifyUnregisteredAndRemovedFromPackageCache(string cachedBundlePath)
        {
            Assert.False(this.TryGetRegistration(out _), $"Bundle cached at '{cachedBundlePath}' should not still be registered.");
            Assert.False(File.Exists(cachedBundlePath), $"Cached bundle should have been removed from package cache at '{cachedBundlePath}'.");
        }

        public void RemovePackageFromCache(string packageId)
        {
            using var wixOutput = WixOutput.Read(this.BundlePdb);
            var intermediate = Intermediate.Load(wixOutput);
            var section = intermediate.Sections.Single();
            var packageSymbol = section.Symbols.OfType<WixBundlePackageSymbol>().Single(p => p.Id.Id == packageId);
            var cachePath = this.GetPackageCachePathForCacheId(packageSymbol.CacheId, packageSymbol.PerMachine == true);
            if (Directory.Exists(cachePath))
            {
                Directory.Delete(cachePath, true);
            }
        }

        public string GetPackageEntryPointCachePath(string packageId)
        {
            using var wixOutput = WixOutput.Read(this.BundlePdb);
            var intermediate = Intermediate.Load(wixOutput);
            var section = intermediate.Sections.Single();
            var packageSymbol = section.Symbols.OfType<WixBundlePackageSymbol>().Single(p => p.Id.Id == packageId);
            var packagePayloadSymbol = section.Symbols.OfType<WixBundlePayloadSymbol>().Single(p => p.Id.Id == packageSymbol.PayloadRef);
            var cachePath = this.GetPackageCachePathForCacheId(packageSymbol.CacheId, packageSymbol.PerMachine == true);
            return Path.Combine(cachePath, packagePayloadSymbol.Name);
        }

        public void VerifyPackageIsCached(string packageId, bool cached = true)
        {
            var entryPointCachePath = this.GetPackageEntryPointCachePath(packageId);
            Assert.Equal(cached, File.Exists(entryPointCachePath));
        }

        public void VerifyPackageProviderRemoved(string packageId)
        {
            using var wixOutput = WixOutput.Read(this.BundlePdb);
            var intermediate = Intermediate.Load(wixOutput);
            var section = intermediate.Sections.Single();
            var packageSymbol = section.Symbols.OfType<WixBundlePackageSymbol>().Single(p => p.Id.Id == packageId);
            var providerSymbol = section.Symbols.OfType<WixDependencyProviderSymbol>().Single(p => p.ParentRef == packageId);
            var registryRoot = packageSymbol.PerMachine == true ? Registry.LocalMachine : Registry.CurrentUser;
            var subkeyPath = Path.Combine(DependencyRegistryRoot, providerSymbol.ProviderKey);
            using var registryKey = registryRoot.OpenSubKey(subkeyPath);
            if (registryKey != null)
            {
                WixAssert.StringEqual(null, subkeyPath);
            }
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
