// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    /// <summary>
    /// Types of parents in complex reference.
    /// </summary>
    public enum ComplexReferenceParentType
    {
        /// <summary>Unknown complex reference type, default and invalid.</summary>
        Unknown,

        /// <summary>Feature parent of complex reference.</summary>
        Feature,

        /// <summary>ComponentGroup parent of complex reference.</summary>
        ComponentGroup,

        /// <summary>FeatureGroup parent of complex reference.</summary>
        FeatureGroup,

        /// <summary>Module parent of complex reference.</summary>
        Module,

        /// <summary>Product parent of complex reference.</summary>
        Product,

        /// <summary>PayloadGroup parent of complex reference.</summary>
        PayloadGroup,

        /// <summary>Package parent of complex reference.</summary>
        Package,

        /// <summary>PackageGroup parent of complex reference.</summary>
        PackageGroup,

        /// <summary>Container parent of complex reference.</summary>
        Container,

        /// <summary>Layout parent of complex reference.</summary>
        Layout,

        /// <summary>Patch parent of complex reference.</summary>
        Patch,

        /// <summary>PatchFamilyGroup parent of complex reference.</summary>
        PatchFamilyGroup,
    }
}
