// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    public interface IPackageInfo
    {
        CacheType CacheType { get; }
        object CustomData { get; set; }
        string Description { get; }
        string DisplayInternalUICondition { get; }
        string DisplayName { get; }
        string Id { get; }
        string InstallCondition { get; }
        bool Permanent { get; }
        bool PrereqPackage { get; }
        string PrereqLicenseFile { get; }
        string PrereqLicenseUrl { get; }
        string ProductCode { get; }
        PackageType Type { get; }
        string UpgradeCode { get; }
        string Version { get; }
        bool Vital { get; }
    }
}