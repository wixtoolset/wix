// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Symbol with section representing a single unique symbol.
    /// </summary>
    public sealed class SymbolWithSection
    {
        private HashSet<SymbolWithSection> possibleConflicts;
        private HashSet<SymbolWithSection> redundants;

        /// <summary>
        /// Creates a symbol for a symbol.
        /// </summary>
        /// <param name="symbol">Symbol for the symbol</param>
        public SymbolWithSection(IntermediateSection section, IntermediateSymbol symbol)
        {
            this.Symbol = symbol;
            this.Section = section;
            this.Name = String.Concat(this.Symbol.Definition.Name, ":", this.Symbol.Id.Id);
        }

        /// <summary>
        /// Gets the accessibility of the symbol which is a direct reflection of the accessibility of the row's accessibility.
        /// </summary>
        /// <value>Accessbility of the symbol.</value>
        public AccessModifier Access => this.Symbol.Id.Access;

        /// <summary>
        /// Gets the name of the symbol.
        /// </summary>
        /// <value>Name of the symbol.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the symbol for this symbol.
        /// </summary>
        /// <value>Symbol for this symbol.</value>
        public IntermediateSymbol Symbol { get; }

        /// <summary>
        /// Gets the section for the symbol.
        /// </summary>
        /// <value>Section for the symbol.</value>
        public IntermediateSection Section { get; }

        /// <summary>
        /// Gets any duplicates of this symbol with sections that are possible conflicts.
        /// </summary>
        public IEnumerable<SymbolWithSection> PossiblyConflicts => this.possibleConflicts ?? Enumerable.Empty<SymbolWithSection>();

        /// <summary>
        /// Gets any duplicates of this symbol with sections that are redundant.
        /// </summary>
        public IEnumerable<SymbolWithSection> Redundants => this.redundants ?? Enumerable.Empty<SymbolWithSection>();

        /// <summary>
        /// Adds a duplicate symbol with sections that is a possible conflict.
        /// </summary>
        /// <param name="symbolWithSection">Symbol with section that is a possible conflict of this symbol.</param>
        public void AddPossibleConflict(SymbolWithSection symbolWithSection)
        {
            if (null == this.possibleConflicts)
            {
                this.possibleConflicts = new HashSet<SymbolWithSection>();
            }

            this.possibleConflicts.Add(symbolWithSection);
        }

        /// <summary>
        /// Adds a duplicate symbol that is redundant.
        /// </summary>
        /// <param name="symbolWithSection">Symbol with section that is redundant of this symbol.</param>
        public void AddRedundant(SymbolWithSection symbolWithSection)
        {
            if (null == this.redundants)
            {
                this.redundants = new HashSet<SymbolWithSection>();
            }

            this.redundants.Add(symbolWithSection);
        }
    }
}
