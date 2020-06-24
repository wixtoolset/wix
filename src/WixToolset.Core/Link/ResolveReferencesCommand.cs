// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Resolves all the simple references in a section.
    /// </summary>
    internal class ResolveReferencesCommand
    {
        private readonly IntermediateSection entrySection;
        private readonly IDictionary<string, TupleWithSection> tuplesWithSections;
        private HashSet<TupleWithSection> referencedTuples;
        private HashSet<IntermediateSection> resolvedSections;

        public ResolveReferencesCommand(IMessaging messaging, IntermediateSection entrySection, IDictionary<string, TupleWithSection> tuplesWithSections)
        {
            this.Messaging = messaging;
            this.entrySection = entrySection;
            this.tuplesWithSections = tuplesWithSections;
            this.BuildingMergeModule = (SectionType.Module == entrySection.Type);
        }

        public IEnumerable<TupleWithSection> ReferencedTupleWithSections => this.referencedTuples;

        public IEnumerable<IntermediateSection> ResolvedSections => this.resolvedSections;

        private bool BuildingMergeModule { get; }

        private IMessaging Messaging { get; }

        /// <summary>
        /// Resolves all the simple references in a section.
        /// </summary>
        public void Execute()
        {
            this.resolvedSections = new HashSet<IntermediateSection>();
            this.referencedTuples = new HashSet<TupleWithSection>();

            this.RecursivelyResolveReferences(this.entrySection);
        }

        /// <summary>
        /// Recursive helper function to resolve all references of passed in section.
        /// </summary>
        /// <param name="section">Section with references to resolve.</param>
        /// <remarks>Note: recursive function.</remarks>
        private void RecursivelyResolveReferences(IntermediateSection section)
        {
            // If we already resolved this section, move on to the next.
            if (!this.resolvedSections.Add(section))
            {
                return;
            }

            // Process all of the references contained in this section using the collection of
            // tuples provided.  Then recursively call this method to process the
            // located tuple's section.  All in all this is a very simple depth-first
            // search of the references per-section.
            foreach (var wixSimpleReferenceRow in section.Tuples.OfType<WixSimpleReferenceTuple>())
            {
                // If we're building a Merge Module, ignore all references to the Media table
                // because Merge Modules don't have Media tables.
                if (this.BuildingMergeModule && wixSimpleReferenceRow.Table == "Media")
                {
                    continue;
                }

                if (!this.tuplesWithSections.TryGetValue(wixSimpleReferenceRow.SymbolicName, out var tupleWithSection))
                {
                    this.Messaging.Write(ErrorMessages.UnresolvedReference(wixSimpleReferenceRow.SourceLineNumbers, wixSimpleReferenceRow.SymbolicName));
                }
                else // see if the tuple (and any of its duplicates) are appropriately accessible.
                {
                    var accessible = this.DetermineAccessibleTuples(section, tupleWithSection);
                    if (!accessible.Any())
                    {
                        this.Messaging.Write(ErrorMessages.UnresolvedReference(wixSimpleReferenceRow.SourceLineNumbers, wixSimpleReferenceRow.SymbolicName, tupleWithSection.Access));
                    }
                    else if (1 == accessible.Count)
                    {
                        var accessibleTuple = accessible[0];
                        this.referencedTuples.Add(accessibleTuple);

                        if (null != accessibleTuple.Section)
                        {
                            this.RecursivelyResolveReferences(accessibleTuple.Section);
                        }
                    }
                    else // display errors for the duplicate tuples.
                    {
                        var accessibleTuple = accessible[0];
                        var referencingSourceLineNumber = wixSimpleReferenceRow.SourceLineNumbers?.ToString();

                        if (String.IsNullOrEmpty(referencingSourceLineNumber))
                        {
                            this.Messaging.Write(ErrorMessages.DuplicateSymbol(accessibleTuple.Tuple.SourceLineNumbers, accessibleTuple.Name));
                        }
                        else
                        {
                            this.Messaging.Write(ErrorMessages.DuplicateSymbol(accessibleTuple.Tuple.SourceLineNumbers, accessibleTuple.Name, referencingSourceLineNumber));
                        }

                        foreach (var accessibleDuplicate in accessible.Skip(1))
                        {
                            this.Messaging.Write(ErrorMessages.DuplicateSymbol2(accessibleDuplicate.Tuple.SourceLineNumbers));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determine if the tuple and any of its duplicates are accessbile by referencing section.
        /// </summary>
        /// <param name="referencingSection">Section referencing the tuple.</param>
        /// <param name="tupleWithSection">Tuple being referenced.</param>
        /// <returns>List of tuples accessible by referencing section.</returns>
        private List<TupleWithSection> DetermineAccessibleTuples(IntermediateSection referencingSection, TupleWithSection tupleWithSection)
        {
            var accessibleTuples = new List<TupleWithSection>();

            if (this.AccessibleTuple(referencingSection, tupleWithSection))
            {
                accessibleTuples.Add(tupleWithSection);
            }

            foreach (var dupe in tupleWithSection.PossiblyConflicts)
            {
                // don't count overridable WixActionTuples
                var tupleAction = tupleWithSection.Tuple as WixActionTuple;
                var dupeAction = dupe.Tuple as WixActionTuple;
                if (tupleAction?.Overridable != dupeAction?.Overridable)
                {
                    continue;
                }

                if (this.AccessibleTuple(referencingSection, dupe))
                {
                    accessibleTuples.Add(dupe);
                }
            }

            foreach (var dupe in tupleWithSection.Redundants)
            {
                if (this.AccessibleTuple(referencingSection, dupe))
                {
                    accessibleTuples.Add(dupe);
                }
            }

            return accessibleTuples;
        }

        /// <summary>
        /// Determine if a single tuple is accessible by the referencing section.
        /// </summary>
        /// <param name="referencingSection">Section referencing the tuple.</param>
        /// <param name="tupleWithSection">Tuple being referenced.</param>
        /// <returns>True if tuple is accessible.</returns>
        private bool AccessibleTuple(IntermediateSection referencingSection, TupleWithSection tupleWithSection)
        {
            switch (tupleWithSection.Access)
            {
                case AccessModifier.Public:
                    return true;
                case AccessModifier.Internal:
                    return tupleWithSection.Section.CompilationId.Equals(referencingSection.CompilationId) || (null != tupleWithSection.Section.LibraryId && tupleWithSection.Section.LibraryId.Equals(referencingSection.LibraryId));
                case AccessModifier.Protected:
                    return tupleWithSection.Section.CompilationId.Equals(referencingSection.CompilationId);
                case AccessModifier.Private:
                    return referencingSection == tupleWithSection.Section;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tupleWithSection.Access));
            }
        }
    }
}
