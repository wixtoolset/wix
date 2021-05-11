// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    /// <summary>
    /// Package information from the BA manifest.
    /// </summary>
    public interface IPackageInfo
    {
        /// <summary>
        /// 
        /// </summary>
        BOOTSTRAPPER_CACHE_TYPE CacheType { get; }

        /// <summary>
        /// Place for the BA to store it's own custom data for this package.
        /// </summary>
        object CustomData { get; set; }

        /// <summary>
        /// 
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 
        /// </summary>
        string DisplayInternalUICondition { get; }

        /// <summary>
        /// 
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 
        /// </summary>
        string InstallCondition { get; }

        /// <summary>
        /// 
        /// </summary>
        bool Permanent { get; }

        /// <summary>
        /// 
        /// </summary>
        bool PrereqPackage { get; }

        /// <summary>
        /// 
        /// </summary>
        string PrereqLicenseFile { get; }

        /// <summary>
        /// 
        /// </summary>
        string PrereqLicenseUrl { get; }

        /// <summary>
        /// 
        /// </summary>
        string ProductCode { get; }

        /// <summary>
        /// 
        /// </summary>
        PackageType Type { get; }

        /// <summary>
        /// 
        /// </summary>
        string UpgradeCode { get; }

        /// <summary>
        /// 
        /// </summary>
        string Version { get; }

        /// <summary>
        /// 
        /// </summary>
        bool Vital { get; }
    }
}