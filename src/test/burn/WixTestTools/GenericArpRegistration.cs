// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using Microsoft.Win32;

    public class GenericArpRegistration
    {
        public const string UNINSTALL_KEY = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
        public const string UNINSTALL_KEY_WOW6432NODE = "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall";

        public const string REGISTRY_ARP_INSTALLED = "Installed";
        public const string REGISTRY_ARP_DISPLAY_ICON = "DisplayIcon";
        public const string REGISTRY_ARP_DISPLAY_NAME = "DisplayName";
        public const string REGISTRY_ARP_DISPLAY_VERSION = "DisplayVersion";
        public const string REGISTRY_ARP_ESTIMATED_SIZE = "EstimatedSize";
        public const string REGISTRY_ARP_PUBLISHER = "Publisher";
        public const string REGISTRY_ARP_HELP_LINK = "HelpLink";
        public const string REGISTRY_ARP_HELP_TELEPHONE = "HelpTelephone";
        public const string REGISTRY_ARP_URL_INFO_ABOUT = "URLInfoAbout";
        public const string REGISTRY_ARP_URL_UPDATE_INFO = "URLUpdateInfo";
        public const string REGISTRY_ARP_COMMENTS = "Comments";
        public const string REGISTRY_ARP_CONTACT = "Contact";
        public const string REGISTRY_ARP_NO_MODIFY = "NoModify";
        public const string REGISTRY_ARP_MODIFY_PATH = "ModifyPath";
        public const string REGISTRY_ARP_NO_ELEVATE_ON_MODIFY = "NoElevateOnModify";
        public const string REGISTRY_ARP_NO_REMOVE = "NoRemove";
        public const string REGISTRY_ARP_SYSTEM_COMPONENT = "SystemComponent";
        public const string REGISTRY_ARP_QUIET_UNINSTALL_STRING = "QuietUninstallString";
        public const string REGISTRY_ARP_UNINSTALL_STRING = "UninstallString";
        public const string REGISTRY_ARP_VERSION_MAJOR = "VersionMajor";
        public const string REGISTRY_ARP_VERSION_MINOR = "VersionMinor";

        public RegistryKey BaseKey { get; set; }

        public string KeyPath { get; set; }

        public string DisplayName { get; set; }

        public string DisplayVersion { get; set; }

        public int? EstimatedSize { get; set; }

        public int? Installed { get; set; }

        public string ModifyPath { get; set; }

        public string Publisher { get; set; }

        public int? SystemComponent { get; set; }

        public string QuietUninstallString { get; set; }

        public string QuietUninstallCommand { get; set; }

        public string QuietUninstallCommandArguments { get; set; }

        public string UninstallCommand { get; set; }

        public string UninstallCommandArguments { get; set; }

        public string UninstallString { get; set; }

        public string UrlInfoAbout { get; set; }

        public string UrlUpdateInfo { get; set; }

        public static bool TryGetPerMachineRegistrationById(string id, bool x64, out GenericArpRegistration registration)
        {
            return TryGetRegistrationById(id, x64, false, out registration);
        }

        public static bool TryGetPerUserRegistrationById(string id, out GenericArpRegistration registration)
        {
            return TryGetRegistrationById(id, true, true, out registration);
        }

        private static bool TryGetRegistrationById(string id, bool x64, bool perUser, out GenericArpRegistration registration)
        {
            registration = GetGenericArpRegistration(id, x64, perUser, key => new GenericArpRegistration());
            return registration != null;
        }

        protected static T GetGenericArpRegistration<T>(string id, bool x64, bool perUser, Func<RegistryKey, T> fnCreate)
            where T : GenericArpRegistration
        {
            var baseKey = perUser ? Registry.CurrentUser : Registry.LocalMachine;
            var baseKeyPath = x64 ? UNINSTALL_KEY : UNINSTALL_KEY_WOW6432NODE;
            var registrationKeyPath = $"{baseKeyPath}\\{id}";
            using var idKey = baseKey.OpenSubKey(registrationKeyPath);

            if (idKey == null)
            {
                return null;
            }

            var registration = fnCreate(idKey);

            registration.BaseKey = baseKey;
            registration.KeyPath = registrationKeyPath;

            registration.DisplayName = idKey.GetValue(REGISTRY_ARP_DISPLAY_NAME) as string;
            registration.DisplayVersion = idKey.GetValue(REGISTRY_ARP_DISPLAY_VERSION) as string;
            registration.EstimatedSize = idKey.GetValue(REGISTRY_ARP_ESTIMATED_SIZE) as int?;
            registration.Installed = idKey.GetValue(REGISTRY_ARP_INSTALLED) as int?;
            registration.ModifyPath = idKey.GetValue(REGISTRY_ARP_MODIFY_PATH) as string;
            registration.Publisher = idKey.GetValue(REGISTRY_ARP_PUBLISHER) as string;
            registration.SystemComponent = idKey.GetValue(REGISTRY_ARP_SYSTEM_COMPONENT) as int?;
            registration.UrlInfoAbout = idKey.GetValue(REGISTRY_ARP_URL_INFO_ABOUT) as string;
            registration.UrlUpdateInfo = idKey.GetValue(REGISTRY_ARP_URL_UPDATE_INFO) as string;

            registration.QuietUninstallString = idKey.GetValue(REGISTRY_ARP_QUIET_UNINSTALL_STRING) as string;
            if (!String.IsNullOrEmpty(registration.QuietUninstallString))
            {
                var closeQuote = registration.QuietUninstallString.IndexOf("\"", 1);
                if (closeQuote > 0)
                {
                    registration.QuietUninstallCommand = registration.QuietUninstallString.Substring(1, closeQuote - 1).Trim();
                    registration.QuietUninstallCommandArguments = registration.QuietUninstallString.Substring(closeQuote + 1).Trim();
                }
            }

            registration.UninstallString = idKey.GetValue(REGISTRY_ARP_UNINSTALL_STRING) as string;
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

        public void Delete()
        {
            this.BaseKey.DeleteSubKeyTree(this.KeyPath);
        }
    }
}
