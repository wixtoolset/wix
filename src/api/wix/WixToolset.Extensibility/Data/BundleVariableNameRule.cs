// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;

    /// <summary>
    /// When validating a bundle variable name, which special restrictions to ignore.
    /// </summary>
    [Flags]
    public enum BundleVariableNameRule
    {
        /// <summary>
        /// Enforce all special restrictions.
        /// </summary>
        EnforceAllRestrictions = 0x0,

        /// <summary>
        /// Allow names of built-in variables.
        /// </summary>
        CanBeBuiltIn = 0x1,

        /// <summary>
        /// Allow names of well-known variables.
        /// </summary>
        CanBeWellKnown = 0x2,

        /// <summary>
        /// Allow names that are not built-in and are not well-known and start with 'Wix'.
        /// </summary>
        CanHaveReservedPrefix = 0x4,
    }
}
