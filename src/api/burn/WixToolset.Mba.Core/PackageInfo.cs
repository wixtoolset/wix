// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.XPath;

    /// <summary>
    /// 
    /// </summary>
    public enum PackageType
    {
        /// <summary>
        /// 
        /// </summary>
        Unknown,

        /// <summary>
        /// 
        /// </summary>
        Exe,

        /// <summary>
        /// 
        /// </summary>
        Msi,

        /// <summary>
        /// 
        /// </summary>
        Msp,

        /// <summary>
        /// 
        /// </summary>
        Msu,

        /// <summary>
        /// 
        /// </summary>
        UpgradeBundle,

        /// <summary>
        /// 
        /// </summary>
        AddonBundle,

        /// <summary>
        /// 
        /// </summary>
        PatchBundle,
    }

    /// <summary>
    /// Default implementation of <see cref="IPackageInfo"/>.
    /// </summary>
    public class PackageInfo : IPackageInfo
    {
        /// <inheritdoc/>
        public string Id { get; internal set; }

        /// <inheritdoc/>
        public string DisplayName { get; internal set; }

        /// <inheritdoc/>
        public string Description { get; internal set; }

        /// <inheritdoc/>
        public PackageType Type { get; internal set; }

        /// <inheritdoc/>
        public bool Permanent { get; internal set; }

        /// <inheritdoc/>
        public bool Vital { get; internal set; }

        /// <inheritdoc/>
        public string DisplayInternalUICondition { get; internal set; }

        /// <inheritdoc/>
        public string ProductCode { get; internal set; }

        /// <inheritdoc/>
        public string UpgradeCode { get; internal set; }

        /// <inheritdoc/>
        public string Version { get; internal set; }

        /// <inheritdoc/>
        public string InstallCondition { get; internal set; }

        /// <inheritdoc/>
        public BOOTSTRAPPER_CACHE_TYPE CacheType { get; internal set; }

        /// <inheritdoc/>
        public bool PrereqPackage { get; internal set; }

        /// <inheritdoc/>
        public string PrereqLicenseFile { get; internal set; }

        /// <inheritdoc/>
        public string PrereqLicenseUrl { get; internal set; }

        /// <inheritdoc/>
        public object CustomData { get; set; }

        internal PackageInfo() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public static BOOTSTRAPPER_CACHE_TYPE? GetCacheTypeAttribute(XPathNavigator node, string attributeName)
        {
            string attributeValue = BootstrapperApplicationData.GetAttribute(node, attributeName);

            if (attributeValue == null)
            {
                return null;
            }

            if (attributeValue.Equals("keep", StringComparison.InvariantCulture))
            {
                return BOOTSTRAPPER_CACHE_TYPE.Keep;
            }
            else if (attributeValue.Equals("force", StringComparison.InvariantCulture))
            {
                return BOOTSTRAPPER_CACHE_TYPE.Force;
            }
            else
            {
                return BOOTSTRAPPER_CACHE_TYPE.Remove;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="relationType"></param>
        /// <param name="perMachine"></param>
        /// <param name="version"></param>
        /// <returns></returns>
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
