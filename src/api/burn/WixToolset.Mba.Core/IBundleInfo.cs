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
        /// 
        /// </summary>
        string LogVariable { get; }

        /// <summary>
        /// 
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        IDictionary<string, IPackageInfo> Packages { get; }

        /// <summary>
        /// 
        /// </summary>
        bool PerMachine { get; }

        /// <summary>
        /// Adds a related bundle as a package.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>The created <see cref="IPackageInfo"/>.</returns>
        IPackageInfo AddRelatedBundleAsPackage(DetectRelatedBundleEventArgs e);
    }
}