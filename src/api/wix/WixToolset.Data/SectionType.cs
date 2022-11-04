// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;

    /// <summary>
    /// Type of section.
    /// </summary>
    public enum SectionType
    {
        /// <summary>Unknown section type, default and invalid.</summary>
        Unknown,

        /// <summary>Bundle section type.</summary>
        Bundle,

        /// <summary>Fragment section type.</summary>
        Fragment,

        /// <summary>Module section type.</summary>
        Module,

        /// <summary>Package section type.</summary>
        Package,

        /// <summary>Package output type.</summary>
        [Obsolete]
        Product = Package,

        /// <summary>Patch creation section type.</summary>
        PatchCreation,

        /// <summary>Patch section type.</summary>
        Patch
    }
}
