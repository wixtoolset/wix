// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    public enum AccessModifier
    {
        /// <summary>
        /// Indicates the identifier is publicly visible to all other sections.
        /// </summary>
        Public,

        /// <summary>
        /// Indicates the identifier is visible only to sections in the same library.
        /// </summary>
        Internal,

        /// <summary>
        /// Indicates the identifier is visible only to sections in the same source file.
        /// </summary>
        Protected,

        /// <summary>
        /// Indicates the identifiers is visible only to the section where it is defined.
        /// </summary>
        Private,
    }
}
