// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using Microsoft.Win32;

    public class BundleRegistration
    {
        public const string BURN_REGISTRATION_REGISTRY_UNINSTALL_KEY = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
        public const string BURN_REGISTRATION_REGISTRY_UNINSTALL_KEY_WOW6432NODE = "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_CACHE_PATH = "BundleCachePath";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_ADDON_CODE = "BundleAddonCode";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_DETECT_CODE = "BundleDetectCode";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_PATCH_CODE = "BundlePatchCode";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE = "BundleUpgradeCode";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_DISPLAY_NAME = "DisplayName";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_VERSION = "BundleVersion";
        public const string BURN_REGISTRATION_REGISTRY_ENGINE_VERSION = "EngineVersion";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_PROVIDER_KEY = "BundleProviderKey";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_TAG = "BundleTag";
        public const string REGISTRY_REBOOT_PENDING_FORMAT = "{0}.RebootRequired";
        public const string REGISTRY_BUNDLE_INSTALLED = "Installed";
        public const string REGISTRY_BUNDLE_DISPLAY_ICON = "DisplayIcon";
        public const string REGISTRY_BUNDLE_DISPLAY_VERSION = "DisplayVersion";
        public const string REGISTRY_BUNDLE_ESTIMATED_SIZE = "EstimatedSize";
        public const string REGISTRY_BUNDLE_PUBLISHER = "Publisher";
        public const string REGISTRY_BUNDLE_HELP_LINK = "HelpLink";
        public const string REGISTRY_BUNDLE_HELP_TELEPHONE = "HelpTelephone";
        public const string REGISTRY_BUNDLE_URL_INFO_ABOUT = "URLInfoAbout";
        public const string REGISTRY_BUNDLE_URL_UPDATE_INFO = "URLUpdateInfo";
        public const string REGISTRY_BUNDLE_PARENT_DISPLAY_NAME = "ParentDisplayName";
        public const string REGISTRY_BUNDLE_PARENT_KEY_NAME = "ParentKeyName";
        public const string REGISTRY_BUNDLE_COMMENTS = "Comments";
        public const string REGISTRY_BUNDLE_CONTACT = "Contact";
        public const string REGISTRY_BUNDLE_NO_MODIFY = "NoModify";
        public const string REGISTRY_BUNDLE_MODIFY_PATH = "ModifyPath";
        public const string REGISTRY_BUNDLE_NO_ELEVATE_ON_MODIFY = "NoElevateOnModify";
        public const string REGISTRY_BUNDLE_NO_REMOVE = "NoRemove";
        public const string REGISTRY_BUNDLE_SYSTEM_COMPONENT = "SystemComponent";
        public const string REGISTRY_BUNDLE_QUIET_UNINSTALL_STRING = "QuietUninstallString";
        public const string REGISTRY_BUNDLE_UNINSTALL_STRING = "UninstallString";
        public const string REGISTRY_BUNDLE_RESUME_COMMAND_LINE = "BundleResumeCommandLine";
        public const string REGISTRY_BUNDLE_VERSION_MAJOR = "VersionMajor";
        public const string REGISTRY_BUNDLE_VERSION_MINOR = "VersionMinor";

        public string[] AddonCodes { get; set; }

        public string CachePath { get; set; }

        public string DisplayName { get; set; }

        public string[] DetectCodes { get; set; }

        public string EngineVersion { get; set; }

        public int? EstimatedSize { get; set; }

        public int? Installed { get; set; }

        public string ModifyPath { get; set; }

        public string[] PatchCodes { get; set; }

        public string ProviderKey { get; set; }

        public string Publisher { get; set; }

        public string QuietUninstallString { get; set; }

        public string QuietUninstallCommand { get; set; }

        public string QuietUninstallCommandArguments { get; set; }

        public string Tag { get; set; }

        public string UninstallCommand { get; set; }

        public string UninstallCommandArguments { get; set; }

        public string UninstallString { get; set; }

        public string[] UpgradeCodes { get; set; }

        public string UrlInfoAbout { get; set; }

        public string UrlUpdateInfo { get; set; }

        public string Version { get; set; }

        public static bool TryGetPerMachineBundleRegistrationById(string bundleId, bool x64, out BundleRegistration registration)
        {
            var baseKeyPath = x64 ? BURN_REGISTRATION_REGISTRY_UNINSTALL_KEY : BURN_REGISTRATION_REGISTRY_UNINSTALL_KEY_WOW6432NODE;
            var registrationKeyPath = $"{baseKeyPath}\\{bundleId}";
            using var registrationKey = Registry.LocalMachine.OpenSubKey(registrationKeyPath);
            var success = registrationKey != null;
            registration = success ? GetBundleRegistration(registrationKey) : null;
            return success;
        }

        public static bool TryGetPerUserBundleRegistrationById(string bundleId, out BundleRegistration registration)
        {
            var registrationKeyPath = $"{BURN_REGISTRATION_REGISTRY_UNINSTALL_KEY}\\{bundleId}";
            using var registrationKey = Registry.CurrentUser.OpenSubKey(registrationKeyPath);
            var success = registrationKey != null;
            registration = success ? GetBundleRegistration(registrationKey) : null;
            return success;
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
            registration.Version = idKey.GetValue(BURN_REGISTRATION_REGISTRY_BUNDLE_VERSION) as string;
            registration.DisplayName = idKey.GetValue(BURN_REGISTRATION_REGISTRY_BUNDLE_DISPLAY_NAME) as string;
            registration.EngineVersion = idKey.GetValue(BURN_REGISTRATION_REGISTRY_ENGINE_VERSION) as string;
            registration.EstimatedSize = idKey.GetValue(REGISTRY_BUNDLE_ESTIMATED_SIZE) as int?;
            registration.Installed = idKey.GetValue(REGISTRY_BUNDLE_INSTALLED) as int?;
            registration.ModifyPath = idKey.GetValue(REGISTRY_BUNDLE_MODIFY_PATH) as string;
            registration.Publisher = idKey.GetValue(REGISTRY_BUNDLE_PUBLISHER) as string;
            registration.UrlInfoAbout = idKey.GetValue(REGISTRY_BUNDLE_URL_INFO_ABOUT) as string;
            registration.UrlUpdateInfo = idKey.GetValue(REGISTRY_BUNDLE_URL_UPDATE_INFO) as string;

            registration.QuietUninstallString = idKey.GetValue(REGISTRY_BUNDLE_QUIET_UNINSTALL_STRING) as string;
            if (!String.IsNullOrEmpty(registration.QuietUninstallString))
            {
                var closeQuote = registration.QuietUninstallString.IndexOf("\"", 1);
                if (closeQuote > 0)
                {
                    registration.QuietUninstallCommand = registration.QuietUninstallString.Substring(1, closeQuote - 1).Trim();
                    registration.QuietUninstallCommandArguments = registration.QuietUninstallString.Substring(closeQuote + 1).Trim();
                }
            }

            registration.UninstallString = idKey.GetValue(REGISTRY_BUNDLE_UNINSTALL_STRING) as string;
            if (!String.IsNullOrEmpty(registration.UninstallString))
            {
                var closeQuote = registration.UninstallString.IndexOf("\"", 1);
                if (closeQuote > 0)
                {
                    registration.UninstallCommand = registration.UninstallString.Substring(1, closeQuote - 1).Trim();
                    registration.UninstallCommandArguments = registration.UninstallString.Substring(closeQuote + 1).Trim();
                }
            }

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
