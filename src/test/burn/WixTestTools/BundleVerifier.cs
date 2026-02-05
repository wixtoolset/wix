// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using System.IO;
    using System.Linq;
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

        public string GetPackageCachePathForCacheId(string cacheId, WixBundleScopeType? scope, bool? plannedPerMachine = null)
        {
            string cachePath;

            if (scope == WixBundleScopeType.PerMachine)
            {
                cachePath = GetPerMachineCacheRoot();
            }
            else if (scope == WixBundleScopeType.PerUser)
            {
                cachePath = GetPerUserCacheRoot();
            }
            else
            {
                cachePath = plannedPerMachine.Value ? GetPerMachineCacheRoot() : GetPerUserCacheRoot();
            }

            return Path.Combine(cachePath, cacheId);

            string GetPerMachineCacheRoot()
            {
                using var policyKey = Registry.LocalMachine.OpenSubKey(this.GetFullBurnPolicyRegistryPath());
                var redirectedCachePath = policyKey?.GetValue("PackageCache") as string;
                return redirectedCachePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), PACKAGE_CACHE_FOLDER_NAME);
            }

            string GetPerUserCacheRoot()
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), PACKAGE_CACHE_FOLDER_NAME);
            }
        }

        public string GetExpectedCachedBundlePath(bool? plannedPerMachine = null)
        {
            var bundleSymbol = this.GetBundleSymbol();
            var cachePath = this.GetPackageCachePathForCacheId(bundleSymbol.BundleCode, bundleSymbol.Scope, plannedPerMachine);

            return Path.Combine(cachePath, Path.GetFileName(this.Bundle));
        }

        public string ManuallyCache(bool? plannedPerMachine = null)
        {
            var expectedCachePath = this.GetExpectedCachedBundlePath(plannedPerMachine);

            Directory.CreateDirectory(Path.GetDirectoryName(expectedCachePath));
            File.Copy(this.Bundle, expectedCachePath);

            return expectedCachePath;
        }

        public void ManuallyUncache(bool? plannedPerMachine = null)
        {
            var expectedCachePath = this.GetExpectedCachedBundlePath(plannedPerMachine);

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
                this.TestContext.TestOutputHelper.WriteLine($"Missing config for ExePackage {packageId}");

                arpId = null;
                arpVersion = null;
                arpWin64 = false;
                perMachine = false;

                return false;
            }

            arpId = exePackageSymbol.ArpId;
            arpVersion = exePackageSymbol.ArpDisplayVersion;
            arpWin64 = exePackageSymbol.ArpWin64;
            perMachine = packageSymbol.Scope == WixBundleScopeType.PerMachine;

            this.TestContext.TestOutputHelper.WriteLine($"Config for ExePackage {packageId}: arpId={arpId}, arpVersion={arpVersion}, arpWin64={arpWin64}, perMachine={perMachine}");

            return true;
        }

        public bool TryGetRegistration(bool? plannedPerMachine, out BundleRegistration registration)
        {
            var bundleSymbol = this.GetBundleSymbol();
            var x64 = bundleSymbol.Platform != Platform.X86;
            var bundleCode = bundleSymbol.BundleCode;

            if (bundleSymbol.Scope == WixBundleScopeType.PerMachine)
            {
                return BundleRegistration.TryGetPerMachineBundleRegistrationById(bundleCode, x64, this.TestContext.TestOutputHelper, out registration);
            }
            else if (bundleSymbol.Scope == WixBundleScopeType.PerUser)
            {
                return BundleRegistration.TryGetPerUserBundleRegistrationById(bundleCode, this.TestContext.TestOutputHelper, out registration);
            }
            else
            {
                return plannedPerMachine.Value ? BundleRegistration.TryGetPerMachineBundleRegistrationById(bundleCode, x64, this.TestContext.TestOutputHelper, out registration)
                    : BundleRegistration.TryGetPerUserBundleRegistrationById(bundleCode, this.TestContext.TestOutputHelper, out registration);
            }
        }

        public bool TryGetUpdateRegistration(bool? plannedPerMachine, out BundleUpdateRegistration registration)
        {
            var bundleSymbol = this.GetBundleSymbol();
            var x64 = bundleSymbol.Platform != Platform.X86;

            var updateRegistrationSymbol = this.GetUpdateRegistrationSymbol();
            var manufacturer = updateRegistrationSymbol.Manufacturer;
            var productFamily = updateRegistrationSymbol.ProductFamily;
            var name = updateRegistrationSymbol.Name;

            if (bundleSymbol.Scope == WixBundleScopeType.PerMachine)
            {
                return BundleUpdateRegistration.TryGetPerMachineBundleUpdateRegistration(manufacturer, productFamily, name, x64, out registration);
            }
            else if (bundleSymbol.Scope == WixBundleScopeType.PerUser)
            {
                return BundleUpdateRegistration.TryGetPerUserBundleUpdateRegistration(manufacturer, productFamily, name, out registration);
            }
            else
            {
                return plannedPerMachine.Value ? BundleUpdateRegistration.TryGetPerMachineBundleUpdateRegistration(manufacturer, productFamily, name, x64, out registration)
                    : BundleUpdateRegistration.TryGetPerUserBundleUpdateRegistration(manufacturer, productFamily, name, out registration);
            }
        }

        public BundleRegistration VerifyRegisteredAndInPackageCache(int? expectedSystemComponent = null, bool? plannedPerMachine = null)
        {
            Assert.True(this.TryGetRegistration(plannedPerMachine, out var registration));

            Assert.Equal(expectedSystemComponent, registration.SystemComponent);

            Assert.NotNull(registration.CachePath);
            Assert.True(File.Exists(registration.CachePath));

            var expectedCachePath = this.GetExpectedCachedBundlePath(plannedPerMachine);
            WixAssert.StringEqual(expectedCachePath, registration.CachePath, true);

            return registration;
        }

        public void VerifyUnregisteredAndRemovedFromPackageCache(bool? plannedPerMachine = null)
        {
            var cachedBundlePath = this.GetExpectedCachedBundlePath(plannedPerMachine);

            this.VerifyUnregisteredAndRemovedFromPackageCache(cachedBundlePath, plannedPerMachine);
        }

        public void VerifyUnregisteredAndRemovedFromPackageCache(string cachedBundlePath, bool? plannedPerMachine = null)
        {
            Assert.False(this.TryGetRegistration(plannedPerMachine, out _), $"Bundle cached at '{cachedBundlePath}' should not still be registered.");
            Assert.False(File.Exists(cachedBundlePath), $"Cached bundle should have been removed from package cache at '{cachedBundlePath}'.");
        }

        public void RemovePackageFromCache(string packageId, bool? plannedPerMachine = null)
        {
            using var wixOutput = WixOutput.Read(this.BundlePdb);
            var intermediate = Intermediate.Load(wixOutput);
            var section = intermediate.Sections.Single();
            var packageSymbol = section.Symbols.OfType<WixBundlePackageSymbol>().Single(p => p.Id.Id == packageId);
            var cachePath = this.GetPackageCachePathForCacheId(packageSymbol.CacheId, packageSymbol.Scope, plannedPerMachine);

            if (Directory.Exists(cachePath))
            {
                Directory.Delete(cachePath, true);
            }
        }

        public string GetPackageEntryPointCachePath(string packageId, bool? plannedPerMachine = null)
        {
            using var wixOutput = WixOutput.Read(this.BundlePdb);
            var intermediate = Intermediate.Load(wixOutput);
            var section = intermediate.Sections.Single();
            var packageSymbol = section.Symbols.OfType<WixBundlePackageSymbol>().Single(p => p.Id.Id == packageId);
            var packagePayloadSymbol = section.Symbols.OfType<WixBundlePayloadSymbol>().Single(p => p.Id.Id == packageSymbol.PayloadRef);
            var cachePath = this.GetPackageCachePathForCacheId(packageSymbol.CacheId, packageSymbol.Scope, plannedPerMachine);

            return Path.Combine(cachePath, packagePayloadSymbol.Name);
        }

        public void VerifyPackageIsCached(string packageId, bool cached = true, bool? plannedPerMachine = null)
        {
            var entryPointCachePath = this.GetPackageEntryPointCachePath(packageId, plannedPerMachine);

            Assert.Equal(cached, File.Exists(entryPointCachePath));
        }

        public void VerifyPackageProviderRemoved(string packageId, bool? plannedPerMachine = null)
        {
            using var wixOutput = WixOutput.Read(this.BundlePdb);
            var intermediate = Intermediate.Load(wixOutput);
            var section = intermediate.Sections.Single();
            var packageSymbol = section.Symbols.OfType<WixBundlePackageSymbol>().Single(p => p.Id.Id == packageId);
            var providerSymbol = section.Symbols.OfType<WixDependencyProviderSymbol>().Single(p => p.ParentRef == packageId);
            var registryRoot = plannedPerMachine.HasValue ? (plannedPerMachine.Value ? Registry.LocalMachine : Registry.CurrentUser) : packageSymbol.Scope == WixBundleScopeType.PerMachine ? Registry.LocalMachine : Registry.CurrentUser;
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
