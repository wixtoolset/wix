// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Symbol representing a single row in a database.
    /// </summary>
    public sealed class Symbol
    {
        private HashSet<Symbol> possibleConflictSymbols;
        private HashSet<Symbol> redundantSymbols;

        /// <summary>
        /// Creates a symbol for a row.
        /// </summary>
        /// <param name="row">Row for the symbol</param>
        public Symbol(Row row)
        {
            this.Row = row;
            this.Name = String.Concat(this.Row.TableDefinition.Name, ":", this.Row.GetPrimaryKey());
        }

        /// <summary>
        /// Gets the accessibility of the symbol which is a direct reflection of the accessibility of the row's accessibility.
        /// </summary>
        /// <value>Accessbility of the symbol.</value>
        public AccessModifier Access { get { return this.Row.Access; } }

        /// <summary>
        /// Gets the name of the symbol.
        /// </summary>
        /// <value>Name of the symbol.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the row for this symbol.
        /// </summary>
        /// <value>Row for this symbol.</value>
        public Row Row { get; private set; }

        /// <summary>
        /// Gets the section for the symbol.
        /// </summary>
        /// <value>Section for the symbol.</value>
        public Section Section { get { return this.Row.Section; } }

        /// <summary>
        /// Gets any duplicates of this symbol that are possible conflicts.
        /// </summary>
        public IEnumerable<Symbol> PossiblyConflictingSymbols { get { return this.possibleConflictSymbols ?? Enumerable.Empty<Symbol>(); } }

        /// <summary>
        /// Gets any duplicates of this symbol that are redundant.
        /// </summary>
        public IEnumerable<Symbol> RedundantSymbols { get { return this.redundantSymbols ?? Enumerable.Empty<Symbol>(); } }

        /// <summary>
        /// Adds a duplicate symbol that is a possible conflict.
        /// </summary>
        /// <param name="symbol">Symbol that is a possible conflict of this symbol.</param>
        public void AddPossibleConflict(Symbol symbol)
        {
            if (null == this.possibleConflictSymbols)
            {
                this.possibleConflictSymbols = new HashSet<Symbol>();
            }

            this.possibleConflictSymbols.Add(symbol);
        }

        /// <summary>
        /// Adds a duplicate symbol that is redundant.
        /// </summary>
        /// <param name="symbol">Symbol that is redundant of this symbol.</param>
        public void AddRedundant(Symbol symbol)
        {
            if (null == this.redundantSymbols)
            {
                this.redundantSymbols = new HashSet<Symbol>();
            }

            this.redundantSymbols.Add(symbol);
        }
    }
}
