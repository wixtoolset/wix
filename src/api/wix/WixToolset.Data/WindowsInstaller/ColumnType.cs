// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller
{
    /// <summary>
    /// Defines MSI column types.
    /// </summary>
    public enum ColumnType
    {
        /// <summary>Unknown column type, default and invalid.</summary>
        Unknown,

        /// <summary>Column is a string.</summary>
        String,

        /// <summary>Column is a localizable string.</summary>
        Localized,

        /// <summary>Column is a number.</summary>
        Number,

        /// <summary>Column is a binary stream.</summary>
        Object,

        /// <summary>Column is a string that is preserved in transforms (like Object).</summary>
        Preserved,
    }
}
