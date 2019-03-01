// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    public enum ComponentKeyPathType
    {
        /// <summary>
        /// Not a key path.
        /// </summary>
        None,

        /// <summary>
        /// File resource as a key path.
        /// </summary>
        File,

        /// <summary>
        /// Folder as a key path.
        /// </summary>
        Directory,

        /// <summary>
        /// ODBC data source as a key path.
        /// </summary>
        OdbcDataSource,

        /// <summary>
        /// A simple registry key acting as a key path.
        /// </summary>
        Registry,

        /// <summary>
        /// A registry key that contains a formatted property acting as a key path.
        /// </summary>
        RegistryFormatted
    }
}
