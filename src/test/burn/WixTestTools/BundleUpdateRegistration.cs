// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using Microsoft.Win32;

    public class BundleUpdateRegistration
    {
        public const string BURN_UPDATE_REGISTRATION_REGISTRY_BUNDLE_PACKAGE_NAME = "PackageName";
        public const string BURN_UPDATE_REGISTRATION_REGISTRY_BUNDLE_PACKAGE_VERSION = "PackageVersion";
        public const string BURN_UPDATE_REGISTRATION_REGISTRY_BUNDLE_PUBLISHER = "Publisher";
        public const string BURN_UPDATE_REGISTRATION_REGISTRY_BUNDLE_PUBLISHING_GROUP = "PublishingGroup";

        public string PackageName { get; set; }

        public string PackageVersion { get; set; }

        public string Publisher { get; set; }

        public string PublishingGroup { get; set; }

        public static bool TryGetPerMachineBundleUpdateRegistration(string manufacturer, string productFamily, string name, bool x64, out BundleUpdateRegistration registration)
        {
            return TryGetUpdateRegistration(manufacturer, productFamily, name, x64, perUser: false, out registration);
        }

        public static bool TryGetPerUserBundleUpdateRegistration(string manufacturer, string productFamily, string name, out BundleUpdateRegistration registration)
        {
            return TryGetUpdateRegistration(manufacturer, productFamily, name, x64: true, perUser: true, out registration);
        }

        private static bool TryGetUpdateRegistration(string manufacturer, string productFamily, string name, bool x64, bool perUser, out BundleUpdateRegistration registration)
        {
            var baseKey = perUser ? Registry.CurrentUser : Registry.LocalMachine;
            var baseKeyPath = x64 ? @$"SOFTWARE\{manufacturer}\Updates\{productFamily}\{name}"
                : @$"SOFTWARE\WOW6432Node\{manufacturer}\Updates\{productFamily}\{name}";
            using var idKey = baseKey.OpenSubKey(baseKeyPath);

            if (idKey == null)
            {
                registration = null;
                return false;
            }

            registration = new BundleUpdateRegistration()
            {
                PackageName = idKey.GetValue(BURN_UPDATE_REGISTRATION_REGISTRY_BUNDLE_PACKAGE_NAME) as string,
                PackageVersion = idKey.GetValue(BURN_UPDATE_REGISTRATION_REGISTRY_BUNDLE_PACKAGE_VERSION) as string,
                Publisher = idKey.GetValue(BURN_UPDATE_REGISTRATION_REGISTRY_BUNDLE_PUBLISHER) as string,
                PublishingGroup = idKey.GetValue(BURN_UPDATE_REGISTRATION_REGISTRY_BUNDLE_PUBLISHING_GROUP) as string,
            };

            return true;
        }
    }
}
