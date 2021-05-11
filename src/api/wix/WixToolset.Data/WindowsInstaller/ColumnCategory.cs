// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller
{
    /// <summary>
    /// Column validation category type
    /// </summary>
    public enum ColumnCategory
    {
        /// <summary>Unknown category, default and invalid.</summary>
        Unknown,

        /// <summary>Text category.</summary>
        Text,

        /// <summary>UpperCase category.</summary>
        UpperCase,

        /// <summary>LowerCase category.</summary>
        LowerCase,

        /// <summary>Integer category.</summary>
        Integer,

        /// <summary>DoubleInteger category.</summary>
        DoubleInteger,

        /// <summary>TimeDate category.</summary>
        TimeDate,

        /// <summary>Identifier category.</summary>
        Identifier,

        /// <summary>Property category.</summary>
        Property,

        /// <summary>Filename category.</summary>
        Filename,

        /// <summary>WildCardFilename category.</summary>
        WildCardFilename,

        /// <summary>Path category.</summary>
        Path,

        /// <summary>Paths category.</summary>
        Paths,

        /// <summary>AnyPath category.</summary>
        AnyPath,

        /// <summary>DefaultDir category.</summary>
        DefaultDir,

        /// <summary>RegPath category.</summary>
        RegPath,

        /// <summary>Formatted category.</summary>
        Formatted,

        /// <summary>Template category.</summary>
        Template,

        /// <summary>Condition category.</summary>
        Condition,

        /// <summary>Guid category.</summary>
        Guid,

        /// <summary>Version category.</summary>
        Version,

        /// <summary>Language category.</summary>
        Language,

        /// <summary>Binary category.</summary>
        Binary,

        /// <summary>CustomSource category.</summary>
        CustomSource,

        /// <summary>Cabinet category.</summary>
        Cabinet,

        /// <summary>Shortcut category.</summary>
        Shortcut,

        /// <summary>Formatted SDDL category.</summary>
        FormattedSDDLText,
    }
}
