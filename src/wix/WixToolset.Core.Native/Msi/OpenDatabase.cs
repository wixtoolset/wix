// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msi
{
    /// <summary>
    /// Enum of predefined persist modes used when opening a database.
    /// </summary>
    public enum OpenDatabase
    {
        /// <summary>
        /// Open a database read-only, no persistent changes.
        /// </summary>
        ReadOnly = 0,

        /// <summary>
        /// Open a database read/write in transaction mode.
        /// </summary>
        Transact = 1,

        /// <summary>
        /// Open a database direct read/write without transaction.
        /// </summary>
        Direct = 2,

        /// <summary>
        /// Create a new database, transact mode read/write.
        /// </summary>
        Create = 3,

        /// <summary>
        /// Create a new database, direct mode read/write.
        /// </summary>
        CreateDirect = 4,

        /// <summary>
        /// Indicates a patch file is being opened.
        /// </summary>
        OpenPatchFile = 32
    }
}
