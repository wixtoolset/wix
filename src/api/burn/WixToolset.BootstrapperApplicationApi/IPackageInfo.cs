// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplicationApi
{
    /// <summary>
    /// Package information from the BA manifest.
    /// </summary>
    public interface IPackageInfo
    {
        /// <summary>
        /// The authored cache strategy for this package.
        /// </summary>
        BOOTSTRAPPER_CACHE_TYPE CacheType { get; }

        /// <summary>
        /// Place for the BA to store it's own custom data for this package.
        /// </summary>
        object CustomData { get; set; }

        /// <summary>
        /// The package's description.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The authored bal:DisplayInternalUICondition.
        /// </summary>
        string DisplayInternalUICondition { get; }

        /// <summary>
        /// The authored bal:DisplayFilesInUseDialogCondition.
        /// </summary>
        string DisplayFilesInUseDialogCondition { get; }

        /// <summary>
        /// The package's display name.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// The package's Id.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The authored InstallCondition.
        /// </summary>
        string InstallCondition { get; }

        /// <summary>
        /// The authored RepairCondition.
        /// </summary>
        string RepairCondition { get; }

        /// <summary>
        /// Whether the bundle should ever recommend the package to be uninstalled.
        /// </summary>
        bool Permanent { get; }

        /// <summary>
        /// Whether the package should be installed by the prereq BA for managed bootstrapper applications.
        /// </summary>
        bool PrereqPackage { get; }

        /// <summary>
        /// The file name of the license file to be shown by the prereq BA.
        /// </summary>
        string PrereqLicenseFile { get; }

        /// <summary>
        /// The URL of the license to be shown by the prereq BA.
        /// </summary>
        string PrereqLicenseUrl { get; }

        /// <summary>
        /// See <see cref="WixToolset.BootstrapperApplicationApi.PrimaryPackageType"/>
        /// </summary>
        PrimaryPackageType PrimaryPackageType { get; }

        /// <summary>
        /// The package's ProductCode.
        /// </summary>
        string ProductCode { get; }

        /// <summary>
        /// The type of the package.
        /// </summary>
        PackageType Type { get; }

        /// <summary>
        /// The package's UpgradeCode.
        /// </summary>
        string UpgradeCode { get; }

        /// <summary>
        /// The package's version.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Whether the package's failure can be ignored while executing the chain.
        /// </summary>
        bool Vital { get; }
    }
}
