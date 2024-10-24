// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplicationApi
{
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.XPath;

    /// <summary>
    /// The type of package.
    /// </summary>
    public enum PackageType
    {
        /// <summary>
        /// Invalid type.
        /// </summary>
        Unknown,

        /// <summary>
        /// ExePackage
        /// </summary>
        Exe,

        /// <summary>
        /// MsiPackage
        /// </summary>
        Msi,

        /// <summary>
        /// MspPackage
        /// </summary>
        Msp,

        /// <summary>
        /// MsuPackage
        /// </summary>
        Msu,

        /// <summary>
        /// Related bundle of type Upgrade
        /// </summary>
        UpgradeBundle,

        /// <summary>
        /// Related bundle of type Addon
        /// </summary>
        AddonBundle,

        /// <summary>
        /// Related bundle of type Patch
        /// </summary>
        PatchBundle,

        /// <summary>
        /// Related bundle of type Update
        /// </summary>
        UpdateBundle,

        /// <summary>
        /// BundlePackage
        /// </summary>
        ChainBundle,
    }

    /// <summary>
    /// Metadata for BAs like WixInternalUIBootstrapperApplication that only support one main package.
    /// </summary>
    public enum PrimaryPackageType
    {
        /// <summary>
        /// Not a primary package.
        /// </summary>
        None,

        /// <summary>
        /// The default package if no architecture specific package is available for the current architecture.
        /// </summary>
        Default,

        /// <summary>
        /// The package to use on x86 machines.
        /// </summary>
        X86,

        /// <summary>
        /// The package to use on x64 machines.
        /// </summary>
        X64,

        /// <summary>
        /// The package to use on ARM64 machines.
        /// </summary>
        ARM64,
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
        public string DisplayFilesInUseDialogCondition { get; internal set; }

        /// <inheritdoc/>
        public string ProductCode { get; internal set; }

        /// <inheritdoc/>
        public string UpgradeCode { get; internal set; }

        /// <inheritdoc/>
        public string Version { get; internal set; }

        /// <inheritdoc/>
        public string InstallCondition { get; internal set; }

        /// <inheritdoc/>
        public string RepairCondition { get; internal set; }

        /// <inheritdoc/>
        public BOOTSTRAPPER_CACHE_TYPE CacheType { get; internal set; }

        /// <inheritdoc/>
        public bool PrereqPackage { get; internal set; }

        /// <inheritdoc/>
        public string PrereqLicenseFile { get; internal set; }

        /// <inheritdoc/>
        public string PrereqLicenseUrl { get; internal set; }

        /// <inheritdoc/>
        public PrimaryPackageType PrimaryPackageType { get; internal set; }

        /// <inheritdoc/>
        public object CustomData { get; set; }

        internal PackageInfo() { }

        /// <summary>
        /// Parse packages from BootstrapperApplicationData.xml.
        /// </summary>
        /// <param name="root">The root node.</param>
        /// <returns>A dictionary of the packages by Id.</returns>
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

                package.RepairCondition = BootstrapperApplicationData.GetAttribute(node, "RepairCondition");

                BOOTSTRAPPER_CACHE_TYPE? cacheType = GetCacheTypeAttribute(node, "Cache");
                if (!cacheType.HasValue)
                {
                    throw new Exception("Failed to get cache type for package.");
                }
                package.CacheType = cacheType.Value;

                packagesById.Add(package.Id, package);
            }

            ParseBalPackageInfoFromXml(root, namespaceManager, packagesById);
            return packagesById;
        }

        /// <summary>
        /// Parse the cache type attribute.
        /// </summary>
        /// <param name="node">Package node</param>
        /// <param name="attributeName">Attribute name</param>
        /// <returns>The cache type</returns>
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
        /// Parse the package type attribute
        /// </summary>
        /// <param name="node">Package node</param>
        /// <param name="attributeName">Attribute name</param>
        /// <returns>The package type</returns>
        public static PackageType? GetPackageTypeAttribute(XPathNavigator node, string attributeName)
        {
            string attributeValue = BootstrapperApplicationData.GetAttribute(node, attributeName);

            if (attributeValue == null)
            {
                return null;
            }

            if (attributeValue.Equals("Bundle", StringComparison.InvariantCulture))
            {
                return PackageType.ChainBundle;
            }
            else if (attributeValue.Equals("Exe", StringComparison.InvariantCulture))
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
        /// Create <see cref="IPackageInfo"/> from a related bundle.
        /// </summary>
        /// <param name="id">Package id</param>
        /// <param name="relationType">Relation type</param>
        /// <param name="perMachine">Whether the related bundle is per-machine</param>
        /// <param name="version">The related bundle's version</param>
        /// <returns>The package info</returns>
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
                    throw new Exception(String.Format("Unknown related bundle type: {0}", relationType));
            }

            return package;
        }

        /// <summary>
        /// Create <see cref="IPackageInfo"/> from an update bundle.
        /// </summary>
        /// <param name="id">Package id</param>
        /// <returns>The package info</returns>
        public static IPackageInfo GetUpdateBundleAsPackage(string id)
        {
            PackageInfo package = new PackageInfo();
            package.Id = id;
            package.Type = PackageType.UpdateBundle;

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
                    throw new Exception(String.Format("Failed to find package specified in WixBalPackageInfo: {0}", id));
                }

                var package = (PackageInfo)ipackage;

                package.DisplayInternalUICondition = BootstrapperApplicationData.GetAttribute(node, "DisplayInternalUICondition");
                package.DisplayFilesInUseDialogCondition = BootstrapperApplicationData.GetAttribute(node, "DisplayFilesInUseDialogCondition");
            }

            nodes = root.Select("/p:BootstrapperApplicationData/p:WixPrereqInformation", namespaceManager);

            foreach (XPathNavigator node in nodes)
            {
                string id = BootstrapperApplicationData.GetAttribute(node, "PackageId");
                if (id == null)
                {
                    throw new Exception("Failed to get package identifier for WixPrereqInformation.");
                }

                if (!packagesById.TryGetValue(id, out var ipackage))
                {
                    throw new Exception(String.Format("Failed to find package specified in WixPrereqInformation: {0}", id));
                }

                var package = (PackageInfo)ipackage;

                package.PrereqPackage = true;
                package.PrereqLicenseFile = BootstrapperApplicationData.GetAttribute(node, "LicenseFile");
                package.PrereqLicenseUrl = BootstrapperApplicationData.GetAttribute(node, "LicenseUrl");
            }
        }
    }
}
