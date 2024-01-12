// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System.Collections.Generic;

    /// <summary>
    /// BA manifest data.
    /// </summary>
    public interface IBundleInfo
    {
        /// <summary>
        /// The name of the variable that contains the path to the bundle's log.
        /// </summary>
        string LogVariable { get; }

        /// <summary>
        /// Bundle/@Name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Variables that were marked with bal:Overridable="yes".
        /// </summary>
        IOverridableVariables OverridableVariables { get; }

        /// <summary>
        /// The packages in the bundle's chain.
        /// </summary>
        IDictionary<string, IPackageInfo> Packages { get; }

        /// <summary>
        /// Whether the bundle is per-machine or per-user.
        /// </summary>
        bool PerMachine { get; }

        /// <summary>
        /// Adds a related bundle as a package.
        /// </summary>
        /// <param name="productCode"></param>
        /// <param name="relationType"></param>
        /// <param name="perMachine"></param>
        /// <param name="version"></param>
        /// <returns>The created <see cref="IPackageInfo"/>.</returns>
        IPackageInfo AddRelatedBundleAsPackage(string productCode, RelationType relationType, bool perMachine, string version);

        /// <summary>
        /// Adds an update bundle as a package.
        /// </summary>
        /// <param name="packageId">Package id added as update bundle.</param>
        /// <returns>The created <see cref="IPackageInfo"/>.</returns>
        IPackageInfo AddUpdateBundleAsPackage(string packageId);
    }
}
