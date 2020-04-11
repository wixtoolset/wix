// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    /// <summary>
    /// Platforms supported by compiler.
    /// </summary>
    public enum PdbType
    {
        /// <summary>A .wixpdb file matching the generated output (default).</summary>
        Full,

        /// <summary>No .wixpdb file.</summary>
        None,
    }
}
