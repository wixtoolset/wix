// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msi
{
    using System;

    /// <summary>
    /// The errors to suppress when applying a transform.
    /// </summary>
    [Flags]
    public enum TransformErrorConditions
    {
        /// <summary>
        /// None of the following conditions.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Suppress error when adding a row that exists.
        /// </summary>
        AddExistingRow = 0x1,

        /// <summary>
        /// Suppress error when deleting a row that does not exist.
        /// </summary>
        DeleteMissingRow = 0x2,

        /// <summary>
        /// Suppress error when adding a table that exists.
        /// </summary>
        AddExistingTable = 0x4,

        /// <summary>
        /// Suppress error when deleting a table that does not exist.
        /// </summary>
        DeleteMissingTable = 0x8,

        /// <summary>
        /// Suppress error when updating a row that does not exist.
        /// </summary>
        UpdateMissingRow = 0x10,

        /// <summary>
        /// Suppress error when transform and database code pages do not match, and their code pages are neutral.
        /// </summary>
        ChangeCodepage = 0x20,

        /// <summary>
        /// Create the temporary _TransformView table when applying a transform.
        /// </summary>
        ViewTransform = 0x100,

        /// <summary>
        /// Suppress all errors but the option to create the temporary _TransformView table.
        /// </summary>
        All = 0x3F
    }
}
