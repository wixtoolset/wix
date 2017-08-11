// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    /// <summary>
    /// Types of children in complex refernece.
    /// </summary>
    public enum ComplexReferenceChildType
    {
        /// <summary>Unknown complex reference type, default and invalid.</summary>
        Unknown,

        /// <summary>Component child of complex reference.</summary>
        Component,

        /// <summary>Feature child of complex reference.</summary>
        Feature,

        /// <summary>ComponentGroup child of complex reference.</summary>
        ComponentGroup,

        /// <summary>FeatureGroup child of complex reference.</summary>
        FeatureGroup,

        /// <summary>Module child of complex reference.</summary>
        Module,

        /// <summary>Payload child of complex reference.</summary>
        Payload,

        /// <summary>PayloadGroup child of complex reference.</summary>
        PayloadGroup,

        /// <summary>Package child of complex reference.</summary>
        Package,

        /// <summary>PackageGroup child of complex reference.</summary>
        PackageGroup,

        /// <summary>PatchFamily child of complex reference.</summary>
        PatchFamily,

        /// <summary>PatchFamilyGroup child of complex reference.</summary>
        PatchFamilyGroup,
    }
}
