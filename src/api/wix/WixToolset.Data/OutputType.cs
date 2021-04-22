// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    /// <summary>
    /// Various types of output.
    /// </summary>
    public enum OutputType
    {
        /// <summary>Unknown output type.</summary>
        Unknown,

        /// <summary>Bundle output type.</summary>
        Bundle,

        /// <summary>Library output type.</summary>
        Library,

        /// <summary>Module output type.</summary>
        Module,

        /// <summary>Patch output type.</summary>
        Patch,

        /// <summary>Patch Creation output type.</summary>
        PatchCreation,

        /// <summary>Product output type.</summary>
        Product,

        /// <summary>Transform output type.</summary>
        Transform,

        /// <summary>Intermediate representation post-link output type.</summary>
        IntermediatePostLink,
    }
}
