// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.XPath;

    public enum CacheType
    {
        No,
        Yes,
        Always,
    }

    public enum PackageType
    {
        Unknown,
        Exe,
        Msi,
        Msp,
        Msu,
        UpgradeBundle,
        AddonBundle,
        PatchBundle,
    }

    public class PackageInfo : IPackageInfo
    {
        public string Id { get; internal set; }
        public string DisplayName { get; internal set; }
        public string Description { get; internal set; }
        public PackageType Type { get; internal set; }
        public bool Permanent { get; internal set; }
        public bool Vital { get; internal set; }
        public string DisplayInternalUICondition { get; internal set; }
        public string ProductCode { get; internal set; }
        public string UpgradeCode { get; internal set; }
        public string Version { get; internal set; }
        public string InstallCondition { get; internal set; }
        public CacheType CacheType { get; internal set; }
        public bool PrereqPackage { get; internal set; }
        public string PrereqLicenseFile { get; internal set; }
        public string PrereqLicenseUrl { get; internal set; }
        public object CustomData { get; set; }

        internal PackageInfo() { }

        public static IDictionary<string, IPackageInfo> ParsePackagesFromXml(XPathNavigator root)
        {
            var packagesById = new Dictionary<string, IPackageInfo>();
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(root.NameTable);
            namespaceManager.AddNamespace("p", BootstrapperApplicationData.XMLNamespace);
            XPathNodeIterator nodes = root.Select("/p:BootstrapperApplicationData/p:WixPackageProperties", namespaceManager);

            foreach (XPathNavigator node in nodes)
            {
                var package = new PackageInfo();

                string id = BootstrapperApplicationData.GetAttribute(node, "Package");
                if (id == null)
                {
                    throw new Exception("Failed to get package identifier for package.");
                }
                package.Id = id;

                package.DisplayName = BootstrapperApplicationData.GetAttribute(node, "DisplayName");

                package.Description = BootstrapperApplicationData.GetAttribute(node, "Description");

                PackageType? packageType = GetPackageTypeAttribute(node, "PackageType");
                if (!packageType.HasValue)
                {
                    throw new Exception("Failed to get package type for package.");
                }
                package.Type = packageType.Value;

                bool? permanent = BootstrapperApplicationData.GetYesNoAttribute(node, "Permanent");
                if (!permanent.HasValue)
                {
                    throw new Exception("Failed to get permanent settings for package.");
                }
                package.Permanent = permanent.Value;

                bool? vital = BootstrapperApplicationData.GetYesNoAttribute(node, "Vital");
                if (!vital.HasValue)
                {
                    throw new Exception("Failed to get vital setting for package.");
                }
                package.Vital = vital.Value;

                package.ProductCode = BootstrapperApplicationData.GetAttribute(node, "ProductCode");

                package.UpgradeCode = BootstrapperApplicationData.GetAttribute(node, "UpgradeCode");

                package.Version = BootstrapperApplicationData.GetAttribute(node, "Version");

                package.InstallCondition = BootstrapperApplicationData.GetAttribute(node, "InstallCondition");

                packagesById.Add(package.Id, package);
            }

            ParseBalPackageInfoFromXml(root, namespaceManager, packagesById);
            return packagesById;
        }

        public static CacheType? GetCacheTypeAttribute(XPathNavigator node, string attributeName)
        {
            string attributeValue = BootstrapperApplicationData.GetAttribute(node, attributeName);

            if (attributeValue == null)
            {
                return null;
            }

            if (attributeValue.Equals("yes", StringComparison.InvariantCulture))
            {
                return CacheType.Yes;
            }
            else if (attributeValue.Equals("always", StringComparison.InvariantCulture))
            {
                return CacheType.Always;
            }
            else
            {
                return CacheType.No;
            }
        }

        public static PackageType? GetPackageTypeAttribute(XPathNavigator node, string attributeName)
        {
            string attributeValue = BootstrapperApplicationData.GetAttribute(node, attributeName);

            if (attributeValue == null)
            {
                return null;
            }

            if (attributeValue.Equals("Exe", StringComparison.InvariantCulture))
            {
                return PackageType.Exe;
            }
            else if (attributeValue.Equals("Msi", StringComparison.InvariantCulture))
            {
                return PackageType.Msi;
            }
            else if (attributeValue.Equals("Msp", StringComparison.InvariantCulture))
            {
                return PackageType.Msp;
            }
            else if (attributeValue.Equals("Msu", StringComparison.InvariantCulture))
            {
                return PackageType.Msu;
            }
            else
            {
                return PackageType.Unknown;
            }
        }

        public static IPackageInfo GetRelatedBundleAsPackage(string id, RelationType relationType, bool perMachine, string version)
        {
            PackageInfo package = new PackageInfo();
            package.Id = id;
            package.Version = version;

            switch (relationType)
            {
                case RelationType.Addon:
                    package.Type = PackageType.AddonBundle;
                    break;
                case RelationType.Patch:
                    package.Type = PackageType.PatchBundle;
                    break;
                case RelationType.Upgrade:
                    package.Type = PackageType.UpgradeBundle;
                    break;
                default:
                    throw new Exception(string.Format("Unknown related bundle type: {0}", relationType));
            }

            return package;
        }

        internal static void ParseBalPackageInfoFromXml(XPathNavigator root, XmlNamespaceManager namespaceManager, Dictionary<string, IPackageInfo> packagesById)
        {
            XPathNodeIterator nodes = root.Select("/p:BootstrapperApplicationData/p:WixBalPackageInfo", namespaceManager);

            foreach (XPathNavigator node in nodes)
            {
                string id = BootstrapperApplicationData.GetAttribute(node, "PackageId");
                if (id == null)
                {
                    throw new Exception("Failed to get package identifier for WixBalPackageInfo.");
                }

                if (!packagesById.TryGetValue(id, out var ipackage))
                {
                    throw new Exception(string.Format("Failed to find package specified in WixBalPackageInfo: {0}", id));
                }

                var package = (PackageInfo)ipackage;

                package.DisplayInternalUICondition = BootstrapperApplicationData.GetAttribute(node, "DisplayInternalUICondition");
            }

            nodes = root.Select("/p:BootstrapperApplicationData/p:WixMbaPrereqInformation", namespaceManager);

            foreach (XPathNavigator node in nodes)
            {
                string id = BootstrapperApplicationData.GetAttribute(node, "PackageId");
                if (id == null)
                {
                    throw new Exception("Failed to get package identifier for WixMbaPrereqInformation.");
                }

                if (!packagesById.TryGetValue(id, out var ipackage))
                {
                    throw new Exception(string.Format("Failed to find package specified in WixMbaPrereqInformation: {0}", id));
                }

                var package = (PackageInfo)ipackage;

                package.PrereqPackage = true;
                package.PrereqLicenseFile = BootstrapperApplicationData.GetAttribute(node, "LicenseFile");
                package.PrereqLicenseUrl = BootstrapperApplicationData.GetAttribute(node, "LicenseUrl");
            }
        }
    }
}
