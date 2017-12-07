// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller
{
    /// <summary>
    /// Specifies if the column should be modularized.
    /// </summary>
    public enum ColumnModularizeType
    {
        /// <summary>Column should not be modularized.</summary>
        None,

        /// <summary>Column should be modularized.</summary>
        Column,

        /// <summary>When the column is an primary or foreign key to the Icon table it should be modularized special.</summary>
        Icon,

        /// <summary>When the column is a companion file it should be modularized.</summary>
        CompanionFile,

        /// <summary>Column is a condition and should be modularized.</summary>
        Condition,

        /// <summary>Special modularization type for the ControlEvent table's Argument column.</summary>
        ControlEventArgument,

        /// <summary>Special modularization type for the Control table's Text column.</summary>
        ControlText,

        /// <summary>Any Properties in the column should be modularized.</summary>
        Property,

        /// <summary>Semi-colon list of keys, all of which need to be modularized.</summary>
        SemicolonDelimited,
    }
}
