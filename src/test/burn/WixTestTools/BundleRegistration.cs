// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using Microsoft.Win32;

    public class BundleRegistration : GenericArpRegistration
    {
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_ADDON_CODE = "BundleAddonCode";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_CACHE_PATH = "BundleCachePath";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_DETECT_CODE = "BundleDetectCode";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_PATCH_CODE = "BundlePatchCode";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_PROVIDER_KEY = "BundleProviderKey";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_RESUME_COMMAND_LINE = "BundleResumeCommandLine";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_TAG = "BundleTag";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE = "BundleUpgradeCode";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_VERSION = "BundleVersion";
        public const string BURN_REGISTRATION_REGISTRY_ENGINE_VERSION = "EngineVersion";

        public string[] AddonCodes { get; set; }

        public string BundleVersion { get; set; }

        public string CachePath { get; set; }

        public string[] DetectCodes { get; set; }

        public string EngineVersion { get; set; }

        public string[] PatchCodes { get; set; }

        public string ProviderKey { get; set; }

        public string Tag { get; set; }

        public string[] UpgradeCodes { get; set; }

        public static bool TryGetPerMachineBundleRegistrationById(string id, bool x64, out BundleRegistration registration)
        {
            return TryGetRegistrationById(id, x64, false, out registration);
        }

        public static bool TryGetPerUserBundleRegistrationById(string id, out BundleRegistration registration)
        {
            return TryGetRegistrationById(id, true, true, out registration);
        }

        private static bool TryGetRegistrationById(string id, bool x64, bool perUser, out BundleRegistration registration)
        {
            registration = GetGenericArpRegistration(id, x64, perUser, key => GetBundleRegistration(key));
            return registration != null;
        }

        private static BundleRegistration GetBundleRegistration(RegistryKey idKey)
        {
            var registration = new BundleRegistration();

            registration.AddonCodes = idKey.GetValue(BURN_REGISTRATION_REGISTRY_BUNDLE_ADDON_CODE) as string[];
            registration.CachePath = idKey.GetValue(BURN_REGISTRATION_REGISTRY_BUNDLE_CACHE_PATH) as string;
            registration.DetectCodes = idKey.GetValue(BURN_REGISTRATION_REGISTRY_BUNDLE_DETECT_CODE) as string[];
            registration.PatchCodes = idKey.GetValue(BURN_REGISTRATION_REGISTRY_BUNDLE_PATCH_CODE) as string[];
            registration.ProviderKey = idKey.GetValue(BURN_REGISTRATION_REGISTRY_BUNDLE_PROVIDER_KEY) as string;
            registration.Tag = idKey.GetValue(BURN_REGISTRATION_REGISTRY_BUNDLE_TAG) as string;
            registration.UpgradeCodes = idKey.GetValue(BURN_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE) as string[];
            registration.BundleVersion = idKey.GetValue(BURN_REGISTRATION_REGISTRY_BUNDLE_VERSION) as string;
            registration.EngineVersion = idKey.GetValue(BURN_REGISTRATION_REGISTRY_ENGINE_VERSION) as string;

            return registration;
        }

        public static bool TryGetDependencyProviderValue(string providerId, string name, out string value)
        {
            value = null;

            string key = String.Format(@"Installer\Dependencies\{0}", providerId);
            using (RegistryKey providerKey = Registry.ClassesRoot.OpenSubKey(key))
            {
                if (null == providerKey)
                {
                    return false;
                }

                value = providerKey.GetValue(name) as string;
                return value != null;
            }
        }

        public static bool DependencyDependentExists(string providerId, string dependentId)
        {
            string key = String.Format(@"Installer\Dependencies\{0}\Dependents\{1}", providerId, dependentId);
            using (RegistryKey dependentKey = Registry.ClassesRoot.OpenSubKey(key))
            {
                return null != dependentKey;
            }
        }
    }
}
