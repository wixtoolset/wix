// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System;
    using WixToolset.Data.Tuples;

    internal static class WixComplexReferenceTupleExtensions
    {
        /// <summary>
        /// Creates a shallow copy of the ComplexReference.
        /// </summary>
        /// <returns>A shallow copy of the ComplexReference.</returns>
        public static WixComplexReferenceTuple Clone(this WixComplexReferenceTuple source)
        {
            var clone = new WixComplexReferenceTuple(source.SourceLineNumbers, source.Id);
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
        public static int CompareToWithoutConsideringPrimary(this WixComplexReferenceTuple tuple, WixComplexReferenceTuple other)
        {
            var comparison = tuple.ChildType - other.ChildType;
            if (0 == comparison)
            {
                comparison = String.Compare(tuple.Child, other.Child, StringComparison.Ordinal);
                if (0 == comparison)
                {
                    comparison = tuple.ParentType - other.ParentType;
                    if (0 == comparison)
                    {
                        string thisParentLanguage = null == tuple.ParentLanguage ? String.Empty : tuple.ParentLanguage;
                        string otherParentLanguage = null == other.ParentLanguage ? String.Empty : other.ParentLanguage;
                        comparison = String.Compare(thisParentLanguage, otherParentLanguage, StringComparison.Ordinal);
                        if (0 == comparison)
                        {
                            comparison = String.Compare(tuple.Parent, other.Parent, StringComparison.Ordinal);
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
        public static void Reparent(this WixComplexReferenceTuple tuple, WixComplexReferenceTuple parent)
        {
            tuple.Parent = parent.Parent;
            tuple.ParentLanguage = parent.ParentLanguage;
            tuple.ParentType = parent.ParentType;

            if (!tuple.IsPrimary)
            {
                tuple.IsPrimary = parent.IsPrimary;
            }
        }
    }
}
