// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Holds a symbol and field that contain binder variables, which need to be resolved
    /// later, once the files have been resolved.
    /// </summary>
    internal class DelayedField : IDelayedField
    {
        /// <summary>
        /// Creates a delayed field.
        /// </summary>
        /// <param name="symbol">Symbol for the field.</param>
        /// <param name="field">Field needing further resolution.</param>
        public DelayedField(IntermediateSymbol symbol, IntermediateField field)
        {
            this.Symbol = symbol;
            this.Field = field;
        }

        /// <summary>
        /// The row containing the field.
        /// </summary>
        public IntermediateSymbol Symbol { get; }

        /// <summary>
        /// The field needing further resolving.
        /// </summary>
        public IntermediateField Field { get; }
    }
}
