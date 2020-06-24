// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Tuple with section representing a single unique tuple.
    /// </summary>
    public sealed class TupleWithSection
    {
        private HashSet<TupleWithSection> possibleConflicts;
        private HashSet<TupleWithSection> redundants;

        /// <summary>
        /// Creates a symbol for a tuple.
        /// </summary>
        /// <param name="tuple">Tuple for the symbol</param>
        public TupleWithSection(IntermediateSection section, IntermediateTuple tuple)
        {
            this.Tuple = tuple;
            this.Section = section;
            this.Name = String.Concat(this.Tuple.Definition.Name, ":", this.Tuple.Id.Id);
        }

        /// <summary>
        /// Gets the accessibility of the symbol which is a direct reflection of the accessibility of the row's accessibility.
        /// </summary>
        /// <value>Accessbility of the symbol.</value>
        public AccessModifier Access => this.Tuple.Id.Access;

        /// <summary>
        /// Gets the name of the symbol.
        /// </summary>
        /// <value>Name of the symbol.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the tuple for this symbol.
        /// </summary>
        /// <value>Tuple for this symbol.</value>
        public IntermediateTuple Tuple { get; }

        /// <summary>
        /// Gets the section for the symbol.
        /// </summary>
        /// <value>Section for the symbol.</value>
        public IntermediateSection Section { get; }

        /// <summary>
        /// Gets any duplicates of this tuple with sections that are possible conflicts.
        /// </summary>
        public IEnumerable<TupleWithSection> PossiblyConflicts => this.possibleConflicts ?? Enumerable.Empty<TupleWithSection>();

        /// <summary>
        /// Gets any duplicates of this tuple with sections that are redundant.
        /// </summary>
        public IEnumerable<TupleWithSection> Redundants => this.redundants ?? Enumerable.Empty<TupleWithSection>();

        /// <summary>
        /// Adds a duplicate tuple with sections that is a possible conflict.
        /// </summary>
        /// <param name="tupleWithSection">Tuple with section that is a possible conflict of this symbol.</param>
        public void AddPossibleConflict(TupleWithSection tupleWithSection)
        {
            if (null == this.possibleConflicts)
            {
                this.possibleConflicts = new HashSet<TupleWithSection>();
            }

            this.possibleConflicts.Add(tupleWithSection);
        }

        /// <summary>
        /// Adds a duplicate tuple that is redundant.
        /// </summary>
        /// <param name="tupleWithSection">Tuple with section that is redundant of this tuple.</param>
        public void AddRedundant(TupleWithSection tupleWithSection)
        {
            if (null == this.redundants)
            {
                this.redundants = new HashSet<TupleWithSection>();
            }

            this.redundants.Add(tupleWithSection);
        }
    }
}
