// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    /// <summary>
    /// The row transform operations.
    /// </summary>
    public enum RowOperation
    {
        /// <summary>
        /// No operation.
        /// </summary>
        None,

        /// <summary>
        /// Added row.
        /// </summary>
        Add,

        /// <summary>
        /// Deleted row.
        /// </summary>
        Delete,

        /// <summary>
        /// Modified row.
        /// </summary>
        Modify
    }
}
