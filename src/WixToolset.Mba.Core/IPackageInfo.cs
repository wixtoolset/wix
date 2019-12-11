// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperCore
{
    public interface IPackageInfo
    {
        CacheType CacheType { get; }
        string Description { get; }
        bool DisplayInternalUI { get; }
        string DisplayName { get; }
        string Id { get; }
        string InstallCondition { get; }
        bool Permanent { get; }
        string ProductCode { get; }
        PackageType Type { get; }
        string UpgradeCode { get; }
        string Version { get; }
        bool Vital { get; }
    }
}