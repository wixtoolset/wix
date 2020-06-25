// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System;
    using WixToolset.Data.Symbols;

    internal static class WixComplexReferenceSymbolExtensions
    {
        /// <summary>
        /// Creates a shallow copy of the ComplexReference.
        /// </summary>
        /// <returns>A shallow copy of the ComplexReference.</returns>
        public static WixComplexReferenceSymbol Clone(this WixComplexReferenceSymbol source)
        {
            var clone = new WixComplexReferenceSymbol(source.SourceLineNumbers, source.Id);
            clone.ParentType = source.ParentType;
            clone.Parent = source.Parent;
            clone.ParentLanguage = source.ParentLanguage;
            clone.ChildType = source.ChildType;
            clone.Child = source.Child;
            clone.IsPrimary = source.IsPrimary;

            return clone;
        }

        /// <summary>
        /// Compares two complex references without considering the primary bit.
        /// </summary>
        /// <param name="obj">Complex reference to compare to.</param>
        /// <returns>Zero if the objects are equivalent, negative number if the provided object is less, positive if greater.</returns>
        public static int CompareToWithoutConsideringPrimary(this WixComplexReferenceSymbol symbol, WixComplexReferenceSymbol other)
        {
            var comparison = symbol.ChildType - other.ChildType;
            if (0 == comparison)
            {
                comparison = String.Compare(symbol.Child, other.Child, StringComparison.Ordinal);
                if (0 == comparison)
                {
                    comparison = symbol.ParentType - other.ParentType;
                    if (0 == comparison)
                    {
                        string thisParentLanguage = null == symbol.ParentLanguage ? String.Empty : symbol.ParentLanguage;
                        string otherParentLanguage = null == other.ParentLanguage ? String.Empty : other.ParentLanguage;
                        comparison = String.Compare(thisParentLanguage, otherParentLanguage, StringComparison.Ordinal);
                        if (0 == comparison)
                        {
                            comparison = String.Compare(symbol.Parent, other.Parent, StringComparison.Ordinal);
                        }
                    }
                }
            }

            return comparison;
        }

        /// <summary>
        /// Changes all of the parent references to point to the passed in parent reference.
        /// </summary>
        /// <param name="parent">New parent complex reference.</param>
        public static void Reparent(this WixComplexReferenceSymbol symbol, WixComplexReferenceSymbol parent)
        {
            symbol.Parent = parent.Parent;
            symbol.ParentLanguage = parent.ParentLanguage;
            symbol.ParentType = parent.ParentType;

            if (!symbol.IsPrimary)
            {
                symbol.IsPrimary = parent.IsPrimary;
            }
        }
    }
}
