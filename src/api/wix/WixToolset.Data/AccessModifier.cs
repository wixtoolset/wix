// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;

    public enum AccessModifier
    {
        /// <summary>
        /// Indicates the identifier is globally visible to all other sections.
        /// </summary>
        Global,
        [Obsolete]
        Public = Global,

        /// <summary>
        /// Indicates the identifier is visible only to sections in the same library.
        /// </summary>
        Library,
        [Obsolete]
        Internal = Library,

        /// <summary>
        /// Indicates the identifier is visible only to sections in the same source file.
        /// </summary>
        File,
        [Obsolete]
        Protected = File,

        /// <summary>
        /// Indicates the identifier is visible only to the section where it is defined.
        /// </summary>
        Section,
        [Obsolete]
        Private = Section,

        /// <summary>
        /// Indicates the identifier can be overridden by another symbol.
        /// </summary>
        Virtual,

        /// <summary>
        /// Indicates the identifier overrides a virtual symbol.
        /// </summary>
        Override,
    }

    /// <summary>
    /// Extensions for converting <c>AccessModifier</c> to and from strings optimally.
    /// </summary>
    public static class AccessModifierExtensions
    {
        /// <summary>
        /// Converts a string to an <c>AccessModifier</c>.
        /// </summary>
        /// <param name="access">String value to convert.</param>
        /// <returns>Converted <c>AccessModifier</c>.</returns>
        public static AccessModifier AsAccessModifier(this string access)
        {
            switch (access)
            {
                case "global":
                case "public":
                    return AccessModifier.Global;

                case "library":
                case "internal":
                    return AccessModifier.Library;

                case "file":
                case "protected":
                    return AccessModifier.File;

                case "section":
                case "private":
                    return AccessModifier.Section;

                case "virtual":
                    return AccessModifier.Virtual;

                case "override":
                    return AccessModifier.Override;

                default:
                    throw new ArgumentException($"Unknown AccessModifier: {access}", nameof(access));
            }
        }

        /// <summary>
        /// Converts an <c>AccessModifier</c> to a string.
        /// </summary>
        /// <param name="access"><c>AccessModifier</c> value to convert.</param>
        /// <returns>Converted string.</returns>
        public static string AsString(this AccessModifier access)
        {
            switch (access)
            {
                case AccessModifier.Global:
                    return "global";

                case AccessModifier.Library:
                    return "library";

                case AccessModifier.File:
                    return "file";

                case AccessModifier.Section:
                    return "section";

                case AccessModifier.Virtual:
                    return "virtual";

                case AccessModifier.Override:
                    return "override";

                default:
                    throw new ArgumentException($"Unknown AccessModifier: {access}", nameof(access));
            }
        }
    }
}
